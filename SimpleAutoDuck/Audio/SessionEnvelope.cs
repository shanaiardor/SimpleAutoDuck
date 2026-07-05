using System;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.CoreAudioApi;

namespace SimpleAutoDuck.Audio
{
    public sealed class SessionEnvelope : IDuckSession, IDisposable
    {
        private readonly AudioSessionControl _session;
        private double _userVolumeSnapshot = 1.0;
        private double _currentVolume = 1.0;
        private const int ProcessQueryLimitedInformation = 0x1000;

        public string ProcessName { get; }
        public int ProcessId { get; }
        public string ExecutablePath { get; }
        public bool IsMainApp { get; set; }
        public bool IsExcluded { get; set; }

        public SessionEnvelope(AudioSessionControl session)
        {
            _session = session;
            try
            {
                ProcessId = (int)_session.GetProcessID;
                ProcessName = TryGetProcessName(ProcessId);
                ExecutablePath = TryGetExecutablePath(ProcessId);
                _currentVolume = GetVolume();
                _userVolumeSnapshot = _currentVolume;
            }
            catch
            {
                ProcessName = "<unknown>.exe";
                ProcessId = 0;
                ExecutablePath = null;
            }
        }

        private string TryGetProcessName(int processId)
        {
            try
            {
                using (var proc = System.Diagnostics.Process.GetProcessById(processId))
                    return proc.ProcessName + ".exe";
            }
            catch
            {
                return "<unknown>.exe";
            }
        }

        private string TryGetExecutablePath(int processId)
        {
            string path = TryQueryProcessImageName(processId);
            if (!string.IsNullOrEmpty(path))
                return path;

            try
            {
                using (var proc = System.Diagnostics.Process.GetProcessById(processId))
                    return proc.MainModule.FileName;
            }
            catch
            {
                return null;
            }
        }

        private string TryQueryProcessImageName(int processId)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
                if (handle == IntPtr.Zero)
                    return null;

                var buffer = new StringBuilder(1024);
                int size = buffer.Capacity;
                return QueryFullProcessImageName(handle, 0, buffer, ref size)
                    ? buffer.ToString()
                    : null;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    CloseHandle(handle);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder exeName, ref int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

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
