using System;
using System.Collections.Generic;
using SimpleAutoDuck;
using SimpleAutoDuck.Audio;
using SimpleAutoDuck.Config;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    internal class FakeSession : IDuckSession
    {
        public string ProcessName { get; set; }
        public bool IsMainApp { get; set; }
        public double PeakLevel { get; set; }
        public double CurrentVolume { get; private set; } = 1.0;
        public int SetVolumeCalls { get; private set; }
        public double LastSetVolume { get; private set; }
        public double UserVolumeSnapshot { get; private set; } = 1.0;
        public bool IsExcluded { get; set; }

        public double GetPeakLevel() => PeakLevel;
        public double GetVolume() => CurrentVolume;
        public void SetVolume(double v)
        {
            CurrentVolume = v;
            SetVolumeCalls++;
            LastSetVolume = v;
        }
        public void SnapshotUserVolume() => UserVolumeSnapshot = CurrentVolume;
        public double GetUserVolume() => UserVolumeSnapshot;
    }

    public class DuckEngineTests
    {
        private static DuckConfig Cfg(double threshold = 0.02, int holdMs = 50, int releaseDelayMs = 300,
            double duckDepth = 0.3, int attackMs = 200, int releaseMs = 800) =>
            new DuckConfig
            {
                Threshold = threshold, HoldMs = holdMs, ReleaseDelayMs = releaseDelayMs,
                DuckDepth = duckDepth, AttackMs = attackMs, ReleaseMs = releaseMs,
                Enabled = true
            };

        [Fact]
        public void StartsIn_Monitoring()
        {
            var eng = new DuckEngine(Cfg());
            Assert.Equal(DuckingState.Monitoring, eng.State);
        }

        [Fact]
        public void MainAppAboveThreshold_BelowHoldMs_StaysMonitoring()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 50));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(40);
            Assert.Equal(DuckingState.Monitoring, eng.State);
        }

        [Fact]
        public void MainAppAboveThreshold_AtHoldMs_EntersDucking()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 50));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(40);
            eng.Tick(40);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void EnteringDucking_SnapshotsUserVolumeAndBeginsRampTowardDuckDepth()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, attackMs: 1000));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(10);
            Assert.Equal(1.0, bg.UserVolumeSnapshot, 4);
            double expectedAfter10 = 1.0 + (0.3 - 1.0) * (10.0 / 1000.0);
            Assert.Equal(expectedAfter10, bg.LastSetVolume, 4);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void ReleaseDelay_KeepsDucking_WhenBriefDip()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, releaseDelayMs: 300, releaseMs: 1000));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(10);
            main.PeakLevel = 0;
            eng.Tick(100);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void ReleaseDelay_Passed_ReturnsToMonitoring()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, releaseDelayMs: 300, releaseMs: 1000));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(10);
            main.PeakLevel = 0;
            eng.Tick(350);
            Assert.Equal(DuckingState.Monitoring, eng.State);
        }

        [Fact]
        public void ThresholdBoundary_InclusiveAtOrAbove()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.02 };
            var eng = new DuckEngine(Cfg(threshold: 0.02, holdMs: 0));
            eng.Sessions = new List<IDuckSession> { main };
            eng.Tick(10);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void ExcludedBackground_NotDucked()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var excluded = new FakeSession { ProcessName = "x.exe", IsMainApp = false, IsExcluded = true, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, attackMs: 0));
            eng.Sessions = new List<IDuckSession> { main, excluded };
            eng.Tick(10);
            Assert.Equal(0, excluded.SetVolumeCalls);
        }

        [Fact]
        public void Disabled_DoesNothing()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var cfg = Cfg(holdMs: 0);
            cfg.Enabled = false;
            var eng = new DuckEngine(cfg);
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(100);
            Assert.Equal(DuckingState.Monitoring, eng.State);
            Assert.Equal(0, bg.SetVolumeCalls);
        }
    }
}