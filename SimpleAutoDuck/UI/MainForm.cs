using System;
using System.Linq;
using System.Windows.Forms;
using SimpleAutoDuck.Audio;
using SimpleAutoDuck.Config;
using SimpleAutoDuck.Hotkey;

namespace SimpleAutoDuck.UI
{
    public partial class MainForm : Form
    {
        private readonly DuckConfig _config;
        private AudioSessionManager _sessionManager;
        private readonly DuckEngine _engine;
        private readonly TrayIcon _tray;
        private readonly GlobalHotkeyRegistrar _hotkey;

        public MainForm()
        {
            InitializeComponent();
            _config = DuckConfig.Load();
            _config.Clamp();

            try
            {
                _sessionManager = new AudioSessionManager();
                _sessionManager.SessionCreated += OnSessionCreated;
                _sessionManager.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法初始化音频会话管理器: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _engine = new DuckEngine(_config);
            LinkSessionsToEngine();

            _tray = new TrayIcon();
            _tray.ShowRequested += ShowFromTray;
            _tray.ExitRequested += ExitApp;
            _tray.ToggleRequested += ToggleEnabled;
            _tray.Show();

            _hotkey = new GlobalHotkeyRegistrar();
            _hotkey.HotkeyPressed += ToggleEnabled;
            RegisterHotkey();

            BindConfigToControls();
            PopulateSessionList();
            tickTimer.Start();
        }

        private void LinkSessionsToEngine()
        {
            if (_sessionManager == null) return;
            var list = _sessionManager.Sessions.ToList();
            foreach (var s in list)
            {
                s.IsMainApp = _config.MainAppProcessNames.Contains(s.ProcessName);
                s.IsExcluded = _config.BackgroundBlacklist.Contains(s.ProcessName);
            }
            _engine.Sessions = list.Cast<IDuckSession>().ToList();
        }

        private void OnSessionCreated(SessionEnvelope s)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    s.IsMainApp = _config.MainAppProcessNames.Contains(s.ProcessName);
                    s.IsExcluded = _config.BackgroundBlacklist.Contains(s.ProcessName);
                    LinkSessionsToEngine();
                    PopulateSessionList();
                }));
            }
            catch { }
        }

        private void BindConfigToControls()
        {
            tbThreshold.Value = (int)(_config.Threshold * 100);
            tbDuckDepth.Value = (int)(_config.DuckDepth * 100);
            tbAttack.Value = _config.AttackMs;
            tbRelease.Value = _config.ReleaseMs;
            tbHold.Value = _config.HoldMs;
            tbReleaseDelay.Value = _config.ReleaseDelayMs;
            chkEnabled.Checked = _config.Enabled;
            _tray.SetToggleState(_config.Enabled);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            lblThresholdVal.Text = _config.Threshold.ToString("0.00");
            lblDuckDepthVal.Text = _config.DuckDepth.ToString("0.00");
            lblAttackVal.Text = _config.AttackMs + " ms";
            lblReleaseVal.Text = _config.ReleaseMs + " ms";
            lblHoldVal.Text = _config.HoldMs + " ms";
            lblReleaseDelayVal.Text = _config.ReleaseDelayMs + " ms";
        }

        private void PopulateSessionList()
        {
            if (_sessionManager == null) return;
            clbSessions.Items.Clear();
            foreach (var s in _sessionManager.Sessions)
            {
                int idx = clbSessions.Items.Add(s.ProcessName);
                clbSessions.SetItemChecked(idx, _config.MainAppProcessNames.Contains(s.ProcessName));
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try { _sessionManager?.Refresh(); LinkSessionsToEngine(); PopulateSessionList(); }
            catch (Exception ex) { MessageBox.Show("刷新失败: " + ex.Message); }
        }

        private void clbSessions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= clbSessions.Items.Count) return;
            string proc = (string)clbSessions.Items[e.Index];
            if (e.NewValue == CheckState.Checked)
            {
                if (!_config.MainAppProcessNames.Contains(proc)) _config.MainAppProcessNames.Add(proc);
            }
            else
            {
                _config.MainAppProcessNames.Remove(proc);
            }
            LinkSessionsToEngine();
        }

        private void tbThreshold_Scroll(object sender, EventArgs e) { _config.Threshold = tbThreshold.Value / 100.0; UpdateValueLabels(); }
        private void tbDuckDepth_Scroll(object sender, EventArgs e) { _config.DuckDepth = tbDuckDepth.Value / 100.0; UpdateValueLabels(); }
        private void tbAttack_Scroll(object sender, EventArgs e) { _config.AttackMs = tbAttack.Value; UpdateValueLabels(); }
        private void tbRelease_Scroll(object sender, EventArgs e) { _config.ReleaseMs = tbRelease.Value; UpdateValueLabels(); }
        private void tbHold_Scroll(object sender, EventArgs e) { _config.HoldMs = tbHold.Value; UpdateValueLabels(); }
        private void tbReleaseDelay_Scroll(object sender, EventArgs e) { _config.ReleaseDelayMs = tbReleaseDelay.Value; UpdateValueLabels(); }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            _config.Enabled = chkEnabled.Checked;
            _tray.SetToggleState(_config.Enabled);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _config.Clamp();
            _config.Save();
            MessageBox.Show("配置已保存", "SimpleAutoDuck", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tickTimer_Tick(object sender, EventArgs e)
        {
            _engine.Tick(50);
            lblState.Text = "状态: " + (_engine.State == DuckingState.Ducking ? "鸭子中" : "监测中");
            double maxLevel = 0;
            foreach (var s in _engine.Sessions)
            {
                if (s.IsMainApp)
                {
                    var lvl = s.GetPeakLevel();
                    if (lvl > maxLevel) maxLevel = lvl;
                }
            }
            pbMainLevel.Value = Math.Min(1000, (int)(maxLevel * 1000));
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                _tray.ShowBalloon("SimpleAutoDuck", "已最小化到托盘", ToolTipIcon.Info);
            }
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void ToggleEnabled()
        {
            chkEnabled.Checked = !chkEnabled.Checked;
        }

        private void RegisterHotkey()
        {
            var hk = HotkeyDefinition.Parse(_config.Hotkey);
            if (!_hotkey.Register(hk))
            {
                MessageBox.Show(_hotkey.LastError ?? "热键注册失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExitApp()
        {
            _tray.Hide();
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _tray.ShowBalloon("SimpleAutoDuck", "仍在后台运行", ToolTipIcon.Info);
            }
        }
    }
}