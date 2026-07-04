using System.Collections.Generic;
using SimpleAutoDuck.Config;

namespace SimpleAutoDuck.Audio
{
    public sealed class DuckEngine
    {
        private readonly DuckConfig _cfg;
        private int _holdAccumMs;
        private int _releaseAccumMs;

        public DuckingState State { get; private set; } = DuckingState.Monitoring;
        public IList<IDuckSession> Sessions { get; set; } = new List<IDuckSession>();

        public DuckEngine(DuckConfig cfg)
        {
            _cfg = cfg;
        }

        public void Tick(int dtMs)
        {
            if (!_cfg.Enabled) return;

            bool mainActive = AnyMainAppActive();

            if (State == DuckingState.Monitoring)
            {
                if (mainActive)
                {
                    _holdAccumMs += dtMs;
                    if (_holdAccumMs >= _cfg.HoldMs)
                        EnterDucking();
                }
                else
                {
                    _holdAccumMs = 0;
                }
            }
            else
            {
                if (!mainActive)
                {
                    _releaseAccumMs += dtMs;
                    if (_releaseAccumMs >= _cfg.ReleaseDelayMs)
                        ExitDucking();
                }
                else
                {
                    _releaseAccumMs = 0;
                }
            }

            ApplyRampForState(dtMs);
        }

        private bool AnyMainAppActive()
        {
            foreach (var s in Sessions)
                if (s.IsMainApp && s.GetPeakLevel() >= _cfg.Threshold)
                    return true;
            return false;
        }

        private void EnterDucking()
        {
            State = DuckingState.Ducking;
            _holdAccumMs = 0;
            _releaseAccumMs = 0;
            foreach (var s in Sessions)
            {
                if (s.IsMainApp || s.IsExcluded) continue;
                s.SnapshotUserVolume();
            }
        }

        private void ExitDucking()
        {
            State = DuckingState.Monitoring;
            _releaseAccumMs = 0;
            _holdAccumMs = 0;
        }

        private void ApplyRampForState(int dtMs)
        {
            foreach (var s in Sessions)
            {
                if (s.IsMainApp || s.IsExcluded) continue;
                double userVol = s.GetUserVolume();
                double tgt = State == DuckingState.Ducking ? _cfg.DuckDepth : userVol;
                int dur = State == DuckingState.Ducking ? _cfg.AttackMs : _cfg.ReleaseMs;
                if (dur <= 0)
                {
                    s.SetVolume(tgt);
                    continue;
                }
                double ratio = (double)dtMs / dur;
                double current = s.GetVolume();
                double next = current + (tgt - current) * ratio;
                if ((tgt > current && next > tgt) || (tgt < current && next < tgt))
                    next = tgt;
                s.SetVolume(next);
            }
        }
    }
}