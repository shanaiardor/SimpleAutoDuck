using System;
using NAudio.CoreAudioApi;

namespace SimpleAutoDuck.Audio
{
    public sealed class SessionEnvelope : IDuckSession, IDisposable
    {
        private readonly AudioSessionControl _session;
        private double _userVolumeSnapshot = 1.0;
        private double _currentVolume = 1.0;

        public string ProcessName { get; }
        public bool IsMainApp { get; set; }
        public bool IsExcluded { get; set; }

        public SessionEnvelope(AudioSessionControl session)
        {
            _session = session;
            try
            {
                ProcessName = TryGetProcessName();
                _currentVolume = GetVolume();
                _userVolumeSnapshot = _currentVolume;
            }
            catch
            {
                ProcessName = "<unknown>.exe";
            }
        }

        private string TryGetProcessName()
        {
            try
            {
                int pid = (int)_session.GetProcessID;
                using (var proc = System.Diagnostics.Process.GetProcessById(pid))
                    return proc.ProcessName + ".exe";
            }
            catch
            {
                return "<unknown>.exe";
            }
        }

        public double GetPeakLevel()
        {
            try { return _session.AudioMeterInformation.MasterPeakValue; }
            catch { return 0; }
        }

        public double GetVolume() => _currentVolume;

        public void SetVolume(double v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            if (Math.Abs(v - _currentVolume) < 0.001) return;
            try
            {
                _session.SimpleAudioVolume.Volume = (float)v;
                _currentVolume = v;
            }
            catch
            {
            }
        }

        public void SnapshotUserVolume()
        {
            try
            {
                float live = _session.SimpleAudioVolume.Volume;
                _userVolumeSnapshot = ComputeUserSnapshot(live, _currentVolume, _userVolumeSnapshot);
            }
            catch { _userVolumeSnapshot = 1.0; }
        }

        internal static double ComputeUserSnapshot(double liveVolume, double engineKnownVolume, double currentSnapshot)
        {
            if (Math.Abs(liveVolume - engineKnownVolume) > 0.01)
                return liveVolume;
            return currentSnapshot;
        }

        public double GetUserVolume() => _userVolumeSnapshot;

        public void Dispose()
        {
            try { _session.Dispose(); } catch { }
        }
    }
}