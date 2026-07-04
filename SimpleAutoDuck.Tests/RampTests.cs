using SimpleAutoDuck.Audio;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    public class RampTests
    {
        [Fact]
        public void Advance_Attack_MovesQuarterWayPerQuarterTime()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 200);
            ramp.Advance(50);
            Assert.Equal(1.0 - (1.0 - 0.3) * (50.0 / 200.0), ramp.Current, 4);
        }

        [Fact]
        public void Advance_Release_MovesSixteenthWayPerTick()
        {
            var ramp = new RampInterpolator(current: 0.3, target: 1.0, durationMs: 800);
            ramp.Advance(50);
            Assert.Equal(0.3 + (1.0 - 0.3) * (50.0 / 800.0), ramp.Current, 4);
        }

        [Fact]
        public void Advance_PastDuration_ClampsToTarget()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 200);
            ramp.Advance(300);
            Assert.Equal(0.3, ramp.Current, 4);
        }

        [Fact]
        public void IsAtTarget_TrueWhenCurrentEqualsTarget()
        {
            var ramp = new RampInterpolator(current: 0.5, target: 0.5, durationMs: 200);
            Assert.True(ramp.IsAtTarget);
        }

        [Fact]
        public void SetTarget_ResetsDurationAndPreservesCurrent()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 200);
            ramp.Advance(50);
            var midCurrent = ramp.Current;
            ramp.SetTarget(0.8, 1000);
            Assert.Equal(midCurrent, ramp.Current, 4);
            Assert.Equal(0.8, ramp.Target, 4);
            Assert.Equal(1000, ramp.DurationMs);
        }

        [Fact]
        public void Advance_ZeroDuration_JumpsToTarget()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 0);
            ramp.Advance(50);
            Assert.Equal(0.3, ramp.Current, 4);
        }
    }
}