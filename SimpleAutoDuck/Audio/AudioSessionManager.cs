using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudioAudioSessionManager = NAudio.CoreAudioApi.AudioSessionManager;

namespace SimpleAutoDuck.Audio
{
    public sealed class AudioSessionManager : IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new MMDeviceEnumerator();
        private MMDevice _device;
        private NAudioAudioSessionManager _sessionManager;
        private readonly Dictionary<string, SessionEnvelope> _sessions = new Dictionary<string, SessionEnvelope>();

        public event Action<SessionEnvelope> SessionCreated;
        public IReadOnlyCollection<SessionEnvelope> Sessions => _sessions.Values;

        public AudioSessionManager()
        {
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _sessionManager = _device.AudioSessionManager;
            _sessionManager.OnSessionCreated += OnSessionCreated;
        }

        private void OnSessionCreated(object sender, NAudio.CoreAudioApi.Interfaces.IAudioSessionControl newSession)
        {
            var envelope = AddSession(new AudioSessionControl(newSession));
            if (envelope != null) SessionCreated?.Invoke(envelope);
        }

        public void Refresh()
        {
            Clear();
            _sessionManager.RefreshSessions();
            var sessions = _sessionManager.Sessions;
            for (int i = 0; i < sessions.Count; i++)
            {
                AddSession(sessions[i]);
            }
        }

        private SessionEnvelope AddSession(AudioSessionControl session)
        {
            var envelope = new SessionEnvelope(session);
            if (string.IsNullOrEmpty(envelope.ProcessName) || envelope.ProcessName == "<unknown>.exe")
            {
                envelope.Dispose();
                return null;
            }
            if (_sessions.ContainsKey(envelope.ProcessName))
            {
                envelope.Dispose();
                return null;
            }
            envelope.SetVolume(1.0);
            _sessions[envelope.ProcessName] = envelope;
            return envelope;
        }

        public void Clear()
        {
            foreach (var s in _sessions.Values)
                try { s.Dispose(); } catch { }
            _sessions.Clear();
        }

        public void Dispose()
        {
            Clear();
            try { _sessionManager?.Dispose(); } catch { }
            try { _device?.Dispose(); } catch { }
            try { _enumerator?.Dispose(); } catch { }
        }
    }
}