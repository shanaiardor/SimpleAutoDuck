using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleAutoDuck.Hotkey
{
    public sealed class GlobalHotkeyRegistrar : NativeWindow, IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int WM_HOTKEY = 0x0312;

        public event Action HotkeyPressed;
        public bool IsRegistered { get; private set; }
        public string LastError { get; private set; }

        private int _id;

        public GlobalHotkeyRegistrar()
        {
            var cp = new CreateParams();
            cp.Caption = "SimpleAutoDuckHotkeyHelper";
            cp.ClassName = "Static";
            CreateHandle(cp);
        }

        public bool Register(HotkeyDefinition hk)
        {
            if (IsRegistered) Unregister();
            if (!hk.IsValid) return false;

            uint mods = 0;
            if (hk.HasModifier(Keys.Control)) mods |= MOD_CONTROL;
            if (hk.HasModifier(Keys.Alt)) mods |= MOD_ALT;
            if (hk.HasModifier(Keys.Shift)) mods |= MOD_SHIFT;
            if (hk.HasModifier(Keys.LWin) || hk.HasModifier(Keys.RWin)) mods |= MOD_WIN;

            _id = hk.Id;
            if (RegisterHotKey(Handle, _id, mods, (uint)hk.Key))
            {
                IsRegistered = true;
                LastError = null;
                return true;
            }
            int err = Marshal.GetLastWin32Error();
            LastError = "热键注册失败 (错误码 " + err + ")，可能被其他程序占用";
            return false;
        }

        public void Unregister()
        {
            if (IsRegistered)
            {
                UnregisterHotKey(Handle, _id);
                IsRegistered = false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _id)
            {
                HotkeyPressed?.Invoke();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }
}