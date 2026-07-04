using System;
using System.Collections.Generic;
using SimpleAutoDuck;
using SimpleAutoDuck.Audio;
using SimpleAutoDuck.Config;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    internal class RealisticSession : IDuckSession
    {
        public string ProcessName { get; set; }
        public bool IsMainApp { get; set; }
        public bool IsExcluded { get; set; }
        public double PeakLevel { get; set; }

        private double _liveVolume = 1.0;
        private double _engineKnown = 1.0;
        private double _snapshot = 1.0;

        public double Snapshot => _snapshot;
        public double EngineKnownVolume => _engineKnown;
        public int SetVolumeCalls { get; private set; }
        public int SnapshotCalls { get; private set; }

        public void UserAdjust(double v) => _liveVolume = v;

        public double GetPeakLevel() => PeakLevel;
        public double GetVolume() => _engineKnown;
        public void SetVolume(double v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            if (Math.Abs(v - _engineKnown) < 0.001) return;
            _liveVolume = v;
            _engineKnown = v;
            SetVolumeCalls++;
        }
        public void SnapshotUserVolume()
        {
            SnapshotCalls++;
            if (Math.Abs(_liveVolume - _engineKnown) > 0.01)
                _snapshot = _liveVolume;
        }
        public double GetUserVolume() => _snapshot;
    }

    public class RapidToggleTests
    {
        private static DuckConfig Cfg(int holdMs = 0, int releaseDelayMs = 0,
            double duckDepth = 0.3, int attackMs = 200, int releaseMs = 800) =>
            new DuckConfig
            {
                Threshold = 0.02, HoldMs = holdMs, ReleaseDelayMs = releaseDelayMs,
                DuckDepth = duckDepth, AttackMs = attackMs, ReleaseMs = releaseMs,
                Enabled = true
            };

        [Fact]
        public void RapidToggle_DoesNotCorruptSnapshot_RestoresToUserVolume()
        {
            var main = new RealisticSession { ProcessName = "a.exe", IsMainApp = true };
            var bg = new RealisticSession { ProcessName = "b.exe", IsMainApp = false };

            var eng = new DuckEngine(Cfg());
            eng.Sessions = new List<IDuckSession> { main, bg };

            for (int i = 0; i < 40; i++)
            {
                main.PeakLevel = i % 2 == 0 ? 0.5 : 0;
                eng.Tick(50);
            }

            Assert.Equal(1.0, bg.Snapshot, 4);

            main.PeakLevel = 0;
            for (int i = 0; i < 40; i++) eng.Tick(50);

            Assert.True(bg.EngineKnownVolume > 0.9, $"Expected >0.9, got {bg.EngineKnownVolume}");
        }

        [Fact]
        public void UserAdjustDuringMonitoring_NextDuckingSnapshotCapturesIt()
        {
            var main = new RealisticSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0 };
            var bg = new RealisticSession { ProcessName = "b.exe", IsMainApp = false };

            var eng = new DuckEngine(Cfg());
            eng.Sessions = new List<IDuckSession> { main, bg };

            eng.Tick(10);
            eng.Tick(10);

            bg.UserAdjust(0.6);

            eng.Tick(10);
            eng.Tick(10);

            Assert.Equal(0.6, bg.Snapshot, 4);

            main.PeakLevel = 0.5;
            eng.Tick(10);
            eng.Tick(10);

            main.PeakLevel = 0;
            for (int i = 0; i < 40; i++) eng.Tick(50);

            Assert.Equal(0.6, bg.EngineKnownVolume, 2);
        }
    }
}