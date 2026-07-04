namespace SimpleAutoDuck.Audio
{
    public sealed class RampInterpolator
    {
        public double Current { get; private set; }
        public double Target { get; private set; }
        public int DurationMs { get; private set; }

        public RampInterpolator(double current, double target, int durationMs)
        {
            Current = current;
            Target = target;
            DurationMs = durationMs;
        }

        public void SetTarget(double target, int durationMs)
        {
            Target = target;
            DurationMs = durationMs;
        }

        public void Advance(int dtMs)
        {
            if (DurationMs <= 0 || dtMs <= 0)
            {
                Current = Target;
                return;
            }
            double ratio = (double)dtMs / DurationMs;
            if (ratio >= 1.0)
            {
                Current = Target;
                return;
            }
            double delta = (Target - Current) * ratio;
            Current += delta;
            if ((Target > Current && Current > Target) || (Target < Current && Current < Target))
                Current = Target;
        }

        public bool IsAtTarget => System.Math.Abs(Current - Target) < 0.0001;
    }
}