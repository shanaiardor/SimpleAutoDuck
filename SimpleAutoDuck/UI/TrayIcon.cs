using System;
using System.Windows.Forms;

namespace SimpleAutoDuck.UI
{
    public sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notify;
        private readonly ToolStripMenuItem _toggleItem;

        public event Action ShowRequested;
        public event Action ExitRequested;
        public event Action ToggleRequested;

        public TrayIcon()
        {
            _notify = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "SimpleAutoDuck"
            };
            var menu = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem("显示(&S)");
            showItem.Click += (s, e) => ShowRequested?.Invoke();
            _toggleItem = new ToolStripMenuItem("启用鸭子(&E)");
            _toggleItem.CheckOnClick = true;
            _toggleItem.Click += (s, e) => ToggleRequested?.Invoke();
            var exitItem = new ToolStripMenuItem("退出(&X)");
            exitItem.Click += (s, e) => ExitRequested?.Invoke();
            menu.Items.AddRange(new ToolStripItem[] { showItem, _toggleItem, new ToolStripSeparator(), exitItem });
            _notify.ContextMenuStrip = menu;
            _notify.DoubleClick += (s, e) => ShowRequested?.Invoke();
        }

        public void SetToggleState(bool enabled)
        {
            if (_toggleItem.Checked != enabled) _toggleItem.Checked = enabled;
        }

        public void Show() => _notify.Visible = true;
        public void Hide() => _notify.Visible = false;
        public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 2000)
            => _notify.ShowBalloonTip(timeout, title, text, icon);

        public void Dispose()
        {
            _notify?.Dispose();
        }
    }
}