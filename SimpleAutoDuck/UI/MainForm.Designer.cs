using System.Windows.Forms;

namespace SimpleAutoDuck.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private CheckedListBox clbSessions;
        private Button btnRefresh;
        private Label lblState;
        private ProgressBar pbMainLevel;
        private TrackBar tbThreshold;
        private TrackBar tbDuckDepth;
        private TrackBar tbAttack;
        private TrackBar tbRelease;
        private TrackBar tbHold;
        private TrackBar tbReleaseDelay;
        private Label lblThresholdVal;
        private Label lblDuckDepthVal;
        private Label lblAttackVal;
        private Label lblReleaseVal;
        private Label lblHoldVal;
        private Label lblReleaseDelayVal;
        private Label lblThreshold;
        private Label lblDuckDepth;
        private Label lblAttack;
        private Label lblRelease;
        private Label lblHold;
        private Label lblReleaseDelay;
        private CheckBox chkEnabled;
        private Button btnSave;
        private System.Windows.Forms.Timer tickTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.clbSessions = new CheckedListBox();
            this.btnRefresh = new Button();
            this.lblState = new Label();
            this.pbMainLevel = new ProgressBar();
            this.tbThreshold = new TrackBar();
            this.tbDuckDepth = new TrackBar();
            this.tbAttack = new TrackBar();
            this.tbRelease = new TrackBar();
            this.tbHold = new TrackBar();
            this.tbReleaseDelay = new TrackBar();
            this.lblThresholdVal = new Label();
            this.lblDuckDepthVal = new Label();
            this.lblAttackVal = new Label();
            this.lblReleaseVal = new Label();
            this.lblHoldVal = new Label();
            this.lblReleaseDelayVal = new Label();
            this.lblThreshold = new Label();
            this.lblDuckDepth = new Label();
            this.lblAttack = new Label();
            this.lblRelease = new Label();
            this.lblHold = new Label();
            this.lblReleaseDelay = new Label();
            this.chkEnabled = new CheckBox();
            this.btnSave = new Button();
            this.tickTimer = new System.Windows.Forms.Timer(this.components);

            this.clbSessions.FormattingEnabled = true;
            this.clbSessions.Location = new System.Drawing.Point(12, 33);
            this.clbSessions.Name = "clbSessions";
            this.clbSessions.Size = new System.Drawing.Size(240, 250);
            this.clbSessions.CheckOnClick = true;
            this.clbSessions.ItemCheck += new ItemCheckEventHandler(this.clbSessions_ItemCheck);

            this.btnRefresh.Location = new System.Drawing.Point(12, 6);
            this.btnRefresh.Size = new System.Drawing.Size(120, 23);
            this.btnRefresh.Text = "刷新会话";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.lblState.Location = new System.Drawing.Point(270, 6);
            this.lblState.Size = new System.Drawing.Size(200, 20);
            this.lblState.Text = "状态: 监测中";

            this.pbMainLevel.Location = new System.Drawing.Point(270, 30);
            this.pbMainLevel.Size = new System.Drawing.Size(200, 18);
            this.pbMainLevel.Maximum = 1000;

            this.lblThreshold.Location = new System.Drawing.Point(270, 55);
            this.lblThreshold.Size = new System.Drawing.Size(80, 20);
            this.lblThreshold.Text = "阈值";
            this.tbThreshold.Location = new System.Drawing.Point(350, 55);
            this.tbThreshold.Size = new System.Drawing.Size(120, 45);
            this.tbThreshold.Minimum = 0; this.tbThreshold.Maximum = 100;
            this.tbThreshold.TickFrequency = 10;
            this.tbThreshold.Scroll += new System.EventHandler(this.tbThreshold_Scroll);
            this.lblThresholdVal.Location = new System.Drawing.Point(475, 55);
            this.lblThresholdVal.Size = new System.Drawing.Size(45, 20);

            this.lblDuckDepth.Location = new System.Drawing.Point(270, 100);
            this.lblDuckDepth.Size = new System.Drawing.Size(80, 20);
            this.lblDuckDepth.Text = "Duck深度";
            this.tbDuckDepth.Location = new System.Drawing.Point(350, 100);
            this.tbDuckDepth.Size = new System.Drawing.Size(120, 45);
            this.tbDuckDepth.Minimum = 0; this.tbDuckDepth.Maximum = 100;
            this.tbDuckDepth.TickFrequency = 10;
            this.tbDuckDepth.Scroll += new System.EventHandler(this.tbDuckDepth_Scroll);
            this.lblDuckDepthVal.Location = new System.Drawing.Point(475, 100);
            this.lblDuckDepthVal.Size = new System.Drawing.Size(45, 20);

            this.lblAttack.Location = new System.Drawing.Point(270, 145);
            this.lblAttack.Size = new System.Drawing.Size(80, 20);
            this.lblAttack.Text = "Attack(ms)";
            this.tbAttack.Location = new System.Drawing.Point(350, 145);
            this.tbAttack.Size = new System.Drawing.Size(120, 45);
            this.tbAttack.Minimum = 1; this.tbAttack.Maximum = 2000;
            this.tbAttack.TickFrequency = 200;
            this.tbAttack.Scroll += new System.EventHandler(this.tbAttack_Scroll);
            this.lblAttackVal.Location = new System.Drawing.Point(475, 145);
            this.lblAttackVal.Size = new System.Drawing.Size(60, 20);

            this.lblRelease.Location = new System.Drawing.Point(270, 190);
            this.lblRelease.Size = new System.Drawing.Size(80, 20);
            this.lblRelease.Text = "Release(ms)";
            this.tbRelease.Location = new System.Drawing.Point(350, 190);
            this.tbRelease.Size = new System.Drawing.Size(120, 45);
            this.tbRelease.Minimum = 1; this.tbRelease.Maximum = 5000;
            this.tbRelease.TickFrequency = 500;
            this.tbRelease.Scroll += new System.EventHandler(this.tbRelease_Scroll);
            this.lblReleaseVal.Location = new System.Drawing.Point(475, 190);
            this.lblReleaseVal.Size = new System.Drawing.Size(60, 20);

            this.lblHold.Location = new System.Drawing.Point(270, 235);
            this.lblHold.Size = new System.Drawing.Size(80, 20);
            this.lblHold.Text = "Hold(ms)";
            this.tbHold.Location = new System.Drawing.Point(350, 235);
            this.tbHold.Size = new System.Drawing.Size(120, 45);
            this.tbHold.Minimum = 0; this.tbHold.Maximum = 2000;
            this.tbHold.TickFrequency = 100;
            this.tbHold.Scroll += new System.EventHandler(this.tbHold_Scroll);
            this.lblHoldVal.Location = new System.Drawing.Point(475, 235);
            this.lblHoldVal.Size = new System.Drawing.Size(60, 20);

            this.lblReleaseDelay.Location = new System.Drawing.Point(270, 280);
            this.lblReleaseDelay.Size = new System.Drawing.Size(80, 20);
            this.lblReleaseDelay.Text = "释放延迟(ms)";
            this.tbReleaseDelay.Location = new System.Drawing.Point(350, 280);
            this.tbReleaseDelay.Size = new System.Drawing.Size(120, 45);
            this.tbReleaseDelay.Minimum = 0; this.tbReleaseDelay.Maximum = 5000;
            this.tbReleaseDelay.TickFrequency = 500;
            this.tbReleaseDelay.Scroll += new System.EventHandler(this.tbReleaseDelay_Scroll);
            this.lblReleaseDelayVal.Location = new System.Drawing.Point(475, 280);
            this.lblReleaseDelayVal.Size = new System.Drawing.Size(60, 20);

            this.chkEnabled.Location = new System.Drawing.Point(12, 290);
            this.chkEnabled.Size = new System.Drawing.Size(140, 24);
            this.chkEnabled.Text = "启用自动鸭子";
            this.chkEnabled.CheckedChanged += new System.EventHandler(this.chkEnabled_CheckedChanged);

            this.btnSave.Location = new System.Drawing.Point(12, 320);
            this.btnSave.Size = new System.Drawing.Size(120, 23);
            this.btnSave.Text = "保存配置";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.tickTimer.Interval = 50;
            this.tickTimer.Tick += new System.EventHandler(this.tickTimer_Tick);

            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 360);
            this.Controls.Add(this.clbSessions);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.pbMainLevel);
            this.Controls.Add(this.lblThreshold); this.Controls.Add(this.tbThreshold); this.Controls.Add(this.lblThresholdVal);
            this.Controls.Add(this.lblDuckDepth); this.Controls.Add(this.tbDuckDepth); this.Controls.Add(this.lblDuckDepthVal);
            this.Controls.Add(this.lblAttack); this.Controls.Add(this.tbAttack); this.Controls.Add(this.lblAttackVal);
            this.Controls.Add(this.lblRelease); this.Controls.Add(this.tbRelease); this.Controls.Add(this.lblReleaseVal);
            this.Controls.Add(this.lblHold); this.Controls.Add(this.tbHold); this.Controls.Add(this.lblHoldVal);
            this.Controls.Add(this.lblReleaseDelay); this.Controls.Add(this.tbReleaseDelay); this.Controls.Add(this.lblReleaseDelayVal);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.btnSave);
            this.Name = "MainForm";
            this.Text = "SimpleAutoDuck";
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
        }
    }
}