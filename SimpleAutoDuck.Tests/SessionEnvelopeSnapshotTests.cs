using System;
using SimpleAutoDuck.Audio;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    public class SessionEnvelopeSnapshotTests
    {
        [Fact]
        public void Snapshot_PreservesExisting_WhenLiveEqualsEngineKnown()
        {
            double snapshot = 1.0;
            double result = SessionEnvelope.ComputeUserSnapshot(
                liveVolume: 0.3, engineKnownVolume: 0.3, currentSnapshot: snapshot);
            Assert.Equal(1.0, result);
        }

        [Fact]
        public void Snapshot_Updates_WhenUserManuallyChangedVolume()
        {
            double snapshot = 1.0;
            double result = SessionEnvelope.ComputeUserSnapshot(
                liveVolume: 0.5, engineKnownVolume: 0.3, currentSnapshot: snapshot);
            Assert.Equal(0.5, result);
        }

        [Fact]
        public void Snapshot_PreservesExisting_WhenLiveWithinToleranceOfEngineKnown()
        {
            double snapshot = 0.8;
            double result = SessionEnvelope.ComputeUserSnapshot(
                liveVolume: 0.305, engineKnownVolume: 0.3, currentSnapshot: snapshot);
            Assert.Equal(0.8, result);
        }

        [Fact]
        public void Snapshot_Updates_WhenLiveFarFromEngineKnown_OnFirstEverSnapshot()
        {
            double result = SessionEnvelope.ComputeUserSnapshot(
                liveVolume: 0.5, engineKnownVolume: 1.0, currentSnapshot: 1.0);
            Assert.Equal(0.5, result);
        }
    }
}