using System;
using System.Collections.Generic;
using SimpleAutoDuck.Config;

namespace SimpleAutoDuck.Audio
{
    public sealed class DuckEngine
    {
        private readonly DuckConfig _cfg;
        private int _holdAccumMs;
        private int _releaseAccumMs;
        private readonly Dictionary<string, (double start, int elapsed)> _ramp =
            new Dictionary<string, (double, int)>();

        public DuckingState State { get; private set; } = DuckingState.Monitoring;
        public IList<IDuckSession> Sessions { get; set; } = new List<IDuckSession>();
        public event Action EnterDucking;

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
                        BeginDucking();
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

        private void BeginDucking()
        {
            State = DuckingState.Ducking;
            _holdAccumMs = 0;
            _releaseAccumMs = 0;
            foreach (var s in Sessions)
            {
                if (s.IsMainApp || s.IsExcluded) continue;
                s.SnapshotUserVolume();
                _ramp[s.ProcessName] = (s.GetVolume(), 0);
            }
            EnterDucking?.Invoke();
        }

        private void ExitDucking()
        {
            State = DuckingState.Monitoring;
            _releaseAccumMs = 0;
            _holdAccumMs = 0;
            foreach (var s in Sessions)
            {
                if (s.IsMainApp || s.IsExcluded) continue;
                _ramp[s.ProcessName] = (s.GetVolume(), 0);
            }
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
                }
                else
                {
                    string key = s.ProcessName;
                    if (!_ramp.TryGetValue(key, out var info))
                        info = (s.GetVolume(), 0);
                    info.elapsed += dtMs;
                    double t = (double)info.elapsed / dur;
                    if (t >= 1.0) t = 1.0;
                    double next = info.start + (tgt - info.start) * t;
                    s.SetVolume(next);
                    _ramp[key] = info;
                }
                if (State == DuckingState.Monitoring)
                    s.SnapshotUserVolume();
            }
        }
    }
}