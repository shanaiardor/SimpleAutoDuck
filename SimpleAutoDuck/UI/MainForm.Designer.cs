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

        private GroupBox grpSessions;
        private CheckedListBox clbSessions;
        private Button btnRefresh;

        private GroupBox grpStatus;
        private Label lblState;
        private ProgressBar pbMainLevel;

        private GroupBox grpParams;
        private Label lblThreshold;
        private TrackBar tbThreshold;
        private Label lblThresholdVal;
        private Label lblDuckDepth;
        private TrackBar tbDuckDepth;
        private Label lblDuckDepthVal;
        private Label lblAttack;
        private TrackBar tbAttack;
        private Label lblAttackVal;
        private Label lblRelease;
        private TrackBar tbRelease;
        private Label lblReleaseVal;
        private Label lblHold;
        private TrackBar tbHold;
        private Label lblHoldVal;
        private Label lblReleaseDelay;
        private TrackBar tbReleaseDelay;
        private Label lblReleaseDelayVal;

        private GroupBox grpActions;
        private CheckBox chkEnabled;
        private Label lblPreset;
        private ComboBox cbPreset;
        private Button btnSave;

        private System.Windows.Forms.Timer tickTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.grpSessions = new GroupBox();
            this.clbSessions = new CheckedListBox();
            this.btnRefresh = new Button();
            this.grpStatus = new GroupBox();
            this.lblState = new Label();
            this.pbMainLevel = new ProgressBar();
            this.grpParams = new GroupBox();
            this.lblThreshold = new Label();
            this.tbThreshold = new TrackBar();
            this.lblThresholdVal = new Label();
            this.lblDuckDepth = new Label();
            this.tbDuckDepth = new TrackBar();
            this.lblDuckDepthVal = new Label();
            this.lblAttack = new Label();
            this.tbAttack = new TrackBar();
            this.lblAttackVal = new Label();
            this.lblRelease = new Label();
            this.tbRelease = new TrackBar();
            this.lblReleaseVal = new Label();
            this.lblHold = new Label();
            this.tbHold = new TrackBar();
            this.lblHoldVal = new Label();
            this.lblReleaseDelay = new Label();
            this.tbReleaseDelay = new TrackBar();
            this.lblReleaseDelayVal = new Label();
            this.grpActions = new GroupBox();
            this.chkEnabled = new CheckBox();
            this.lblPreset = new Label();
            this.cbPreset = new ComboBox();
            this.btnSave = new Button();
            this.tickTimer = new System.Windows.Forms.Timer(this.components);

            // grpSessions
            this.grpSessions.Location = new System.Drawing.Point(12, 12);
            this.grpSessions.Size = new System.Drawing.Size(280, 398);
            this.grpSessions.Text = "主应用";
            this.grpSessions.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);

            this.btnRefresh.Location = new System.Drawing.Point(10, 22);
            this.btnRefresh.Size = new System.Drawing.Size(120, 26);
            this.btnRefresh.Text = "刷新会话";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.clbSessions.FormattingEnabled = true;
            this.clbSessions.Location = new System.Drawing.Point(10, 54);
            this.clbSessions.Name = "clbSessions";
            this.clbSessions.Size = new System.Drawing.Size(258, 328);
            this.clbSessions.CheckOnClick = true;
            this.clbSessions.ItemCheck += new ItemCheckEventHandler(this.clbSessions_ItemCheck);

            this.grpSessions.Controls.Add(this.btnRefresh);
            this.grpSessions.Controls.Add(this.clbSessions);

            // grpStatus
            this.grpStatus.Location = new System.Drawing.Point(308, 12);
            this.grpStatus.Size = new System.Drawing.Size(510, 70);
            this.grpStatus.Text = "状态";
            this.grpStatus.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);

            this.lblState.Location = new System.Drawing.Point(12, 20);
            this.lblState.Size = new System.Drawing.Size(480, 18);
            this.lblState.Text = "状态: 监测中";

            this.pbMainLevel.Location = new System.Drawing.Point(12, 40);
            this.pbMainLevel.Size = new System.Drawing.Size(480, 18);
            this.pbMainLevel.Maximum = 1000;

            this.grpStatus.Controls.Add(this.lblState);
            this.grpStatus.Controls.Add(this.pbMainLevel);

            // grpParams
            this.grpParams.Location = new System.Drawing.Point(308, 92);
            this.grpParams.Size = new System.Drawing.Size(510, 220);
            this.grpParams.Text = "参数";
            this.grpParams.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);

            // 阈值 (col 1, row 1)
            this.lblThreshold.Location = new System.Drawing.Point(12, 26);
            this.lblThreshold.Size = new System.Drawing.Size(70, 20);
            this.lblThreshold.Text = "阈值";
            this.tbThreshold.Location = new System.Drawing.Point(80, 22);
            this.tbThreshold.Size = new System.Drawing.Size(110, 30);
            this.tbThreshold.Minimum = 0; this.tbThreshold.Maximum = 100;
            this.tbThreshold.TickFrequency = 10;
            this.tbThreshold.Scroll += new System.EventHandler(this.tbThreshold_Scroll);
            this.lblThresholdVal.Location = new System.Drawing.Point(192, 26);
            this.lblThresholdVal.Size = new System.Drawing.Size(55, 20);

            // Duck深度 (col 2, row 1)
            this.lblDuckDepth.Location = new System.Drawing.Point(255, 26);
            this.lblDuckDepth.Size = new System.Drawing.Size(70, 20);
            this.lblDuckDepth.Text = "Duck深度";
            this.tbDuckDepth.Location = new System.Drawing.Point(323, 22);
            this.tbDuckDepth.Size = new System.Drawing.Size(110, 30);
            this.tbDuckDepth.Minimum = 0; this.tbDuckDepth.Maximum = 100;
            this.tbDuckDepth.TickFrequency = 10;
            this.tbDuckDepth.Scroll += new System.EventHandler(this.tbDuckDepth_Scroll);
            this.lblDuckDepthVal.Location = new System.Drawing.Point(435, 26);
            this.lblDuckDepthVal.Size = new System.Drawing.Size(55, 20);

            // Attack (col 1, row 2)
            this.lblAttack.Location = new System.Drawing.Point(12, 70);
            this.lblAttack.Size = new System.Drawing.Size(70, 20);
            this.lblAttack.Text = "Attack";
            this.tbAttack.Location = new System.Drawing.Point(80, 66);
            this.tbAttack.Size = new System.Drawing.Size(110, 30);
            this.tbAttack.Minimum = 1; this.tbAttack.Maximum = 2000;
            this.tbAttack.TickFrequency = 200;
            this.tbAttack.Scroll += new System.EventHandler(this.tbAttack_Scroll);
            this.lblAttackVal.Location = new System.Drawing.Point(192, 70);
            this.lblAttackVal.Size = new System.Drawing.Size(55, 20);

            // Release (col 2, row 2)
            this.lblRelease.Location = new System.Drawing.Point(255, 70);
            this.lblRelease.Size = new System.Drawing.Size(70, 20);
            this.lblRelease.Text = "Release";
            this.tbRelease.Location = new System.Drawing.Point(323, 66);
            this.tbRelease.Size = new System.Drawing.Size(110, 30);
            this.tbRelease.Minimum = 1; this.tbRelease.Maximum = 5000;
            this.tbRelease.TickFrequency = 500;
            this.tbRelease.Scroll += new System.EventHandler(this.tbRelease_Scroll);
            this.lblReleaseVal.Location = new System.Drawing.Point(435, 70);
            this.lblReleaseVal.Size = new System.Drawing.Size(55, 20);

            // Hold (col 1, row 3)
            this.lblHold.Location = new System.Drawing.Point(12, 114);
            this.lblHold.Size = new System.Drawing.Size(70, 20);
            this.lblHold.Text = "Hold";
            this.tbHold.Location = new System.Drawing.Point(80, 110);
            this.tbHold.Size = new System.Drawing.Size(110, 30);
            this.tbHold.Minimum = 0; this.tbHold.Maximum = 2000;
            this.tbHold.TickFrequency = 100;
            this.tbHold.Scroll += new System.EventHandler(this.tbHold_Scroll);
            this.lblHoldVal.Location = new System.Drawing.Point(192, 114);
            this.lblHoldVal.Size = new System.Drawing.Size(55, 20);

            // 释放延迟 (col 2, row 3)
            this.lblReleaseDelay.Location = new System.Drawing.Point(255, 114);
            this.lblReleaseDelay.Size = new System.Drawing.Size(70, 20);
            this.lblReleaseDelay.Text = "释放延迟";
            this.tbReleaseDelay.Location = new System.Drawing.Point(323, 110);
            this.tbReleaseDelay.Size = new System.Drawing.Size(110, 30);
            this.tbReleaseDelay.Minimum = 0; this.tbReleaseDelay.Maximum = 5000;
            this.tbReleaseDelay.TickFrequency = 500;
            this.tbReleaseDelay.Scroll += new System.EventHandler(this.tbReleaseDelay_Scroll);
            this.lblReleaseDelayVal.Location = new System.Drawing.Point(435, 114);
            this.lblReleaseDelayVal.Size = new System.Drawing.Size(55, 20);

            this.grpParams.Controls.Add(this.lblThreshold); this.grpParams.Controls.Add(this.tbThreshold); this.grpParams.Controls.Add(this.lblThresholdVal);
            this.grpParams.Controls.Add(this.lblDuckDepth); this.grpParams.Controls.Add(this.tbDuckDepth); this.grpParams.Controls.Add(this.lblDuckDepthVal);
            this.grpParams.Controls.Add(this.lblAttack); this.grpParams.Controls.Add(this.tbAttack); this.grpParams.Controls.Add(this.lblAttackVal);
            this.grpParams.Controls.Add(this.lblRelease); this.grpParams.Controls.Add(this.tbRelease); this.grpParams.Controls.Add(this.lblReleaseVal);
            this.grpParams.Controls.Add(this.lblHold); this.grpParams.Controls.Add(this.tbHold); this.grpParams.Controls.Add(this.lblHoldVal);
            this.grpParams.Controls.Add(this.lblReleaseDelay); this.grpParams.Controls.Add(this.tbReleaseDelay); this.grpParams.Controls.Add(this.lblReleaseDelayVal);

            // grpActions
            this.grpActions.Location = new System.Drawing.Point(308, 330);
            this.grpActions.Size = new System.Drawing.Size(510, 80);
            this.grpActions.Text = "操作";
            this.grpActions.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);

            this.chkEnabled.Location = new System.Drawing.Point(12, 34);
            this.chkEnabled.Size = new System.Drawing.Size(120, 24);
            this.chkEnabled.Text = "启用自动鸭子";
            this.chkEnabled.CheckedChanged += new System.EventHandler(this.chkEnabled_CheckedChanged);

            this.lblPreset.Location = new System.Drawing.Point(140, 38);
            this.lblPreset.Size = new System.Drawing.Size(42, 18);
            this.lblPreset.Text = "预设";

            this.cbPreset.Location = new System.Drawing.Point(185, 34);
            this.cbPreset.Size = new System.Drawing.Size(120, 23);
            this.cbPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbPreset.SelectedIndexChanged += new System.EventHandler(this.cbPreset_SelectedIndexChanged);

            this.btnSave.Location = new System.Drawing.Point(315, 32);
            this.btnSave.Size = new System.Drawing.Size(75, 26);
            this.btnSave.Text = "保存配置";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.grpActions.Controls.Add(this.chkEnabled);
            this.grpActions.Controls.Add(this.lblPreset);
            this.grpActions.Controls.Add(this.cbPreset);
            this.grpActions.Controls.Add(this.btnSave);

            // tickTimer
            this.tickTimer.Interval = 50;
            this.tickTimer.Tick += new System.EventHandler(this.tickTimer_Tick);

            // MainForm
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(860, 460);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(860, 460);
            this.MaximumSize = new System.Drawing.Size(860, 460);
            this.Controls.Add(this.grpSessions);
            this.Controls.Add(this.grpStatus);
            this.Controls.Add(this.grpParams);
            this.Controls.Add(this.grpActions);
            this.Name = "MainForm";
            this.Text = "SimpleAutoDuck";
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
        }
    }
}