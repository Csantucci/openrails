namespace Orts.Viewer3D.Debugging
{
    partial class DispatchViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                grayPen.Dispose();
                greenPen.Dispose();
                orangePen.Dispose();
                redPen.Dispose();
                pathPen.Dispose();
                trainPen.Dispose();
                trainBrush.Dispose();
                trainFont.Dispose();
                sidingBrush.Dispose();
                sidingFont.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbCanvas = new System.Windows.Forms.PictureBox();
            this.refreshButton = new System.Windows.Forms.Button();
            this.windowSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.resLabel = new System.Windows.Forms.Label();
            this.AvatarView = new System.Windows.Forms.ListView();
            this.rmvButton = new System.Windows.Forms.Button();
            this.chkAllowUserSwitch = new System.Windows.Forms.CheckBox();
            this.chkShowAvatars = new System.Windows.Forms.CheckBox();
            this.MSG = new System.Windows.Forms.TextBox();
            this.msgSelected = new System.Windows.Forms.Button();
            this.msgAll = new System.Windows.Forms.Button();
            this.composeMSG = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.reply2Selected = new System.Windows.Forms.Button();
            this.chkDrawPath = new System.Windows.Forms.CheckBox();
            this.boxSetSignal = new System.Windows.Forms.ListBox();
            this.boxSetSwitch = new System.Windows.Forms.ListBox();
            this.chkPickSignals = new System.Windows.Forms.CheckBox();
            this.chkPickSwitches = new System.Windows.Forms.CheckBox();
            this.chkAllowNew = new System.Windows.Forms.CheckBox();
            this.messages = new System.Windows.Forms.ListBox();
            this.btnAssist = new System.Windows.Forms.Button();
            this.btnNormal = new System.Windows.Forms.Button();
            this.btnFollow = new System.Windows.Forms.Button();
            this.chkBoxPenalty = new System.Windows.Forms.CheckBox();
            this.chkPreferGreen = new System.Windows.Forms.CheckBox();
            this.btnSeeInGame = new System.Windows.Forms.Button();
            this.lblSimulationTimeText = new System.Windows.Forms.Label();
            this.lblSimulationTime = new System.Windows.Forms.Label();
            this.uiRouteLabel = new System.Windows.Forms.Label();
            this.uiShowPlatformLabels = new System.Windows.Forms.CheckBox();
            this.uiShowSidings = new System.Windows.Forms.CheckBox();
            this.uiShowSignals = new System.Windows.Forms.CheckBox();
            this.uiShowSignalState = new System.Windows.Forms.CheckBox();
            this.uiTrainLabel = new System.Windows.Forms.Label();
            this.uibTrainKey = new System.Windows.Forms.Button();
            this.uiShowActiveTrainLabels = new System.Windows.Forms.RadioButton();
            this.uiShowAllTrainLabels = new System.Windows.Forms.RadioButton();
            this.nudDaylightOffsetHrs = new System.Windows.Forms.NumericUpDown();
            this.uilblDayLightOffsetHrs = new System.Windows.Forms.Label();
            this.cdBackground = new System.Windows.Forms.ColorDialog();
            this.uibThemeChance = new System.Windows.Forms.Button();
            this.uiShowSwitches = new System.Windows.Forms.CheckBox();
            this.lblInstruction1 = new System.Windows.Forms.Label();
            this.uiShowTrainLabels = new System.Windows.Forms.CheckBox();
            this.tWindow = new System.Windows.Forms.TabControl();
            this.tDispatch = new System.Windows.Forms.TabPage();
            this.tTimetable = new System.Windows.Forms.TabPage();
            this.uiShowTrainState = new System.Windows.Forms.CheckBox();
            this.lblInstruction2 = new System.Windows.Forms.Label();
            this.lblInstruction3 = new System.Windows.Forms.Label();
            this.lblInstruction4 = new System.Windows.Forms.Label();
            this.uiShowPlatforms = new System.Windows.Forms.CheckBox();
            this.uiTrackLabel = new System.Windows.Forms.Label();
            this.AnchorPoint = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDaylightOffsetHrs)).BeginInit();
            this.tWindow.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbCanvas
            // 
            this.pbCanvas.Location = new System.Drawing.Point(5, 124);
            this.pbCanvas.Name = "pbCanvas";
            this.pbCanvas.Size = new System.Drawing.Size(961, 731);
            this.pbCanvas.TabIndex = 0;
            this.pbCanvas.TabStop = false;
            this.pbCanvas.SizeChanged += new System.EventHandler(this.pbCanvas_SizeChanged);
            this.pbCanvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseDown);
            this.pbCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseMove);
            this.pbCanvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxMouseUp);
            // 
            // refreshButton
            // 
            this.refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refreshButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refreshButton.Location = new System.Drawing.Point(1061, 104);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(112, 28);
            this.refreshButton.TabIndex = 1;
            this.refreshButton.Text = "View Train";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // windowSizeUpDown
            // 
            this.windowSizeUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.windowSizeUpDown.BackColor = System.Drawing.SystemColors.HotTrack;
            this.windowSizeUpDown.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.windowSizeUpDown.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.windowSizeUpDown.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.windowSizeUpDown.Location = new System.Drawing.Point(563, 14);
            this.windowSizeUpDown.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.windowSizeUpDown.Minimum = new decimal(new int[] {
            80,
            0,
            0,
            0});
            this.windowSizeUpDown.Name = "windowSizeUpDown";
            this.windowSizeUpDown.Size = new System.Drawing.Size(101, 23);
            this.windowSizeUpDown.TabIndex = 6;
            this.windowSizeUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.windowSizeUpDown.Value = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.windowSizeUpDown.ValueChanged += new System.EventHandler(this.windowSizeUpDown_ValueChanged);
            // 
            // resLabel
            // 
            this.resLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resLabel.AutoSize = true;
            this.resLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.resLabel.Location = new System.Drawing.Point(673, 15);
            this.resLabel.Name = "resLabel";
            this.resLabel.Size = new System.Drawing.Size(20, 17);
            this.resLabel.TabIndex = 8;
            this.resLabel.Text = "m";
            // 
            // AvatarView
            // 
            this.AvatarView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AvatarView.HideSelection = false;
            this.AvatarView.Location = new System.Drawing.Point(983, 166);
            this.AvatarView.Name = "AvatarView";
            this.AvatarView.Size = new System.Drawing.Size(190, 576);
            this.AvatarView.TabIndex = 14;
            this.AvatarView.UseCompatibleStateImageBehavior = false;
            this.AvatarView.SelectedIndexChanged += new System.EventHandler(this.AvatarView_SelectedIndexChanged);
            // 
            // rmvButton
            // 
            this.rmvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rmvButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rmvButton.Location = new System.Drawing.Point(983, 133);
            this.rmvButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.rmvButton.Name = "rmvButton";
            this.rmvButton.Size = new System.Drawing.Size(75, 29);
            this.rmvButton.TabIndex = 15;
            this.rmvButton.Text = "Remove";
            this.rmvButton.UseVisualStyleBackColor = true;
            this.rmvButton.Click += new System.EventHandler(this.rmvButton_Click);
            // 
            // chkAllowUserSwitch
            // 
            this.chkAllowUserSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAllowUserSwitch.AutoSize = true;
            this.chkAllowUserSwitch.Checked = true;
            this.chkAllowUserSwitch.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAllowUserSwitch.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAllowUserSwitch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkAllowUserSwitch.Location = new System.Drawing.Point(876, 50);
            this.chkAllowUserSwitch.Name = "chkAllowUserSwitch";
            this.chkAllowUserSwitch.Size = new System.Drawing.Size(90, 19);
            this.chkAllowUserSwitch.TabIndex = 16;
            this.chkAllowUserSwitch.Text = "Auto Switch";
            this.chkAllowUserSwitch.UseVisualStyleBackColor = true;
            this.chkAllowUserSwitch.CheckedChanged += new System.EventHandler(this.chkAllowUserSwitch_CheckedChanged);
            // 
            // chkShowAvatars
            // 
            this.chkShowAvatars.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkShowAvatars.AutoSize = true;
            this.chkShowAvatars.Checked = true;
            this.chkShowAvatars.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowAvatars.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkShowAvatars.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkShowAvatars.Location = new System.Drawing.Point(876, 31);
            this.chkShowAvatars.Name = "chkShowAvatars";
            this.chkShowAvatars.Size = new System.Drawing.Size(97, 19);
            this.chkShowAvatars.TabIndex = 17;
            this.chkShowAvatars.Text = "Show Avatars";
            this.chkShowAvatars.UseVisualStyleBackColor = true;
            this.chkShowAvatars.CheckedChanged += new System.EventHandler(this.chkShowAvatars_CheckedChanged);
            // 
            // MSG
            // 
            this.MSG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.MSG.Enabled = false;
            this.MSG.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MSG.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.MSG.Location = new System.Drawing.Point(1, 36);
            this.MSG.Name = "MSG";
            this.MSG.Size = new System.Drawing.Size(719, 26);
            this.MSG.TabIndex = 18;
            this.MSG.WordWrap = false;
            this.MSG.Enter += new System.EventHandler(this.MSGEnter);
            this.MSG.Leave += new System.EventHandler(this.MSGLeave);
            this.MSG.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.checkKeys);
            // 
            // msgSelected
            // 
            this.msgSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.msgSelected.Enabled = false;
            this.msgSelected.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.msgSelected.Location = new System.Drawing.Point(731, 60);
            this.msgSelected.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.msgSelected.MaximumSize = new System.Drawing.Size(257, 29);
            this.msgSelected.MinimumSize = new System.Drawing.Size(134, 29);
            this.msgSelected.Name = "msgSelected";
            this.msgSelected.Size = new System.Drawing.Size(134, 29);
            this.msgSelected.TabIndex = 19;
            this.msgSelected.Text = "MSG to Selected";
            this.msgSelected.UseVisualStyleBackColor = true;
            this.msgSelected.Click += new System.EventHandler(this.msgSelected_Click);
            // 
            // msgAll
            // 
            this.msgAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.msgAll.Enabled = false;
            this.msgAll.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.msgAll.Location = new System.Drawing.Point(731, 30);
            this.msgAll.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.msgAll.MaximumSize = new System.Drawing.Size(257, 29);
            this.msgAll.MinimumSize = new System.Drawing.Size(134, 29);
            this.msgAll.Name = "msgAll";
            this.msgAll.Size = new System.Drawing.Size(134, 29);
            this.msgAll.TabIndex = 20;
            this.msgAll.Text = "MSG to All";
            this.msgAll.UseVisualStyleBackColor = true;
            this.msgAll.Click += new System.EventHandler(this.msgAll_Click);
            // 
            // composeMSG
            // 
            this.composeMSG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.composeMSG.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.composeMSG.Location = new System.Drawing.Point(731, 0);
            this.composeMSG.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.composeMSG.MaximumSize = new System.Drawing.Size(257, 29);
            this.composeMSG.MinimumSize = new System.Drawing.Size(134, 29);
            this.composeMSG.Name = "composeMSG";
            this.composeMSG.Size = new System.Drawing.Size(134, 29);
            this.composeMSG.TabIndex = 21;
            this.composeMSG.Text = "Compose MSG";
            this.composeMSG.UseVisualStyleBackColor = true;
            this.composeMSG.Click += new System.EventHandler(this.composeMSG_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label1.Location = new System.Drawing.Point(528, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 17);
            this.label1.TabIndex = 7;
            this.label1.Text = "Res";
            // 
            // reply2Selected
            // 
            this.reply2Selected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.reply2Selected.Enabled = false;
            this.reply2Selected.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.reply2Selected.Location = new System.Drawing.Point(731, 90);
            this.reply2Selected.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.reply2Selected.MaximumSize = new System.Drawing.Size(257, 29);
            this.reply2Selected.MinimumSize = new System.Drawing.Size(134, 29);
            this.reply2Selected.Name = "reply2Selected";
            this.reply2Selected.Size = new System.Drawing.Size(134, 29);
            this.reply2Selected.TabIndex = 23;
            this.reply2Selected.Text = "Reply to Selected";
            this.reply2Selected.UseVisualStyleBackColor = true;
            this.reply2Selected.Click += new System.EventHandler(this.replySelected);
            // 
            // chkDrawPath
            // 
            this.chkDrawPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDrawPath.AutoSize = true;
            this.chkDrawPath.Checked = true;
            this.chkDrawPath.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDrawPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDrawPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkDrawPath.Location = new System.Drawing.Point(1061, 8);
            this.chkDrawPath.Name = "chkDrawPath";
            this.chkDrawPath.Size = new System.Drawing.Size(80, 19);
            this.chkDrawPath.TabIndex = 24;
            this.chkDrawPath.Text = "Draw Path";
            this.chkDrawPath.UseVisualStyleBackColor = true;
            this.chkDrawPath.CheckedChanged += new System.EventHandler(this.chkDrawPathChanged);
            // 
            // boxSetSignal
            // 
            this.boxSetSignal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(34)))));
            this.boxSetSignal.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.boxSetSignal.Enabled = false;
            this.boxSetSignal.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxSetSignal.ForeColor = System.Drawing.Color.SkyBlue;
            this.boxSetSignal.FormattingEnabled = true;
            this.boxSetSignal.ItemHeight = 20;
            this.boxSetSignal.Location = new System.Drawing.Point(269, 246);
            this.boxSetSignal.MinimumSize = new System.Drawing.Size(160, 100);
            this.boxSetSignal.Name = "boxSetSignal";
            this.boxSetSignal.Size = new System.Drawing.Size(211, 100);
            this.boxSetSignal.TabIndex = 25;
            this.boxSetSignal.Visible = false;
            this.boxSetSignal.SelectedIndexChanged += new System.EventHandler(this.boxSetSignalChosen);
            // 
            // boxSetSwitch
            // 
            this.boxSetSwitch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(34)))));
            this.boxSetSwitch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.boxSetSwitch.Enabled = false;
            this.boxSetSwitch.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxSetSwitch.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.boxSetSwitch.FormattingEnabled = true;
            this.boxSetSwitch.ItemHeight = 20;
            this.boxSetSwitch.Items.AddRange(new object[] {
            "To Main Route",
            "To Side Route"});
            this.boxSetSwitch.Location = new System.Drawing.Point(512, 250);
            this.boxSetSwitch.Margin = new System.Windows.Forms.Padding(5);
            this.boxSetSwitch.MinimumSize = new System.Drawing.Size(154, 61);
            this.boxSetSwitch.Name = "boxSetSwitch";
            this.boxSetSwitch.Size = new System.Drawing.Size(161, 80);
            this.boxSetSwitch.TabIndex = 26;
            this.boxSetSwitch.Visible = false;
            this.boxSetSwitch.SelectedIndexChanged += new System.EventHandler(this.boxSetSwitchChosen);
            // 
            // chkPickSignals
            // 
            this.chkPickSignals.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkPickSignals.AutoSize = true;
            this.chkPickSignals.Checked = true;
            this.chkPickSignals.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPickSignals.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPickSignals.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkPickSignals.Location = new System.Drawing.Point(1061, 28);
            this.chkPickSignals.Name = "chkPickSignals";
            this.chkPickSignals.Size = new System.Drawing.Size(88, 19);
            this.chkPickSignals.TabIndex = 27;
            this.chkPickSignals.Text = "Pick Signals";
            this.chkPickSignals.UseVisualStyleBackColor = true;
            this.chkPickSignals.Visible = false;
            // 
            // chkPickSwitches
            // 
            this.chkPickSwitches.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkPickSwitches.AutoSize = true;
            this.chkPickSwitches.Checked = true;
            this.chkPickSwitches.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPickSwitches.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPickSwitches.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkPickSwitches.Location = new System.Drawing.Point(1061, 47);
            this.chkPickSwitches.Name = "chkPickSwitches";
            this.chkPickSwitches.Size = new System.Drawing.Size(97, 19);
            this.chkPickSwitches.TabIndex = 28;
            this.chkPickSwitches.Text = "Pick Switches";
            this.chkPickSwitches.UseVisualStyleBackColor = true;
            // 
            // chkAllowNew
            // 
            this.chkAllowNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAllowNew.AutoSize = true;
            this.chkAllowNew.Checked = true;
            this.chkAllowNew.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAllowNew.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAllowNew.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkAllowNew.Location = new System.Drawing.Point(876, 12);
            this.chkAllowNew.Name = "chkAllowNew";
            this.chkAllowNew.Size = new System.Drawing.Size(71, 19);
            this.chkAllowNew.TabIndex = 29;
            this.chkAllowNew.Text = "Can Join";
            this.chkAllowNew.UseVisualStyleBackColor = true;
            this.chkAllowNew.CheckedChanged += new System.EventHandler(this.chkAllowNewCheck);
            // 
            // messages
            // 
            this.messages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.messages.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.messages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.messages.FormattingEnabled = true;
            this.messages.ItemHeight = 18;
            this.messages.Location = new System.Drawing.Point(1, 66);
            this.messages.Name = "messages";
            this.messages.Size = new System.Drawing.Size(719, 22);
            this.messages.TabIndex = 22;
            this.messages.SelectedIndexChanged += new System.EventHandler(this.msgSelectedChanged);
            // 
            // btnAssist
            // 
            this.btnAssist.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAssist.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAssist.Location = new System.Drawing.Point(983, 74);
            this.btnAssist.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnAssist.Name = "btnAssist";
            this.btnAssist.Size = new System.Drawing.Size(75, 28);
            this.btnAssist.TabIndex = 30;
            this.btnAssist.Text = "Assist";
            this.btnAssist.UseVisualStyleBackColor = true;
            this.btnAssist.Click += new System.EventHandler(this.AssistClick);
            // 
            // btnNormal
            // 
            this.btnNormal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNormal.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNormal.Location = new System.Drawing.Point(983, 103);
            this.btnNormal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnNormal.Name = "btnNormal";
            this.btnNormal.Size = new System.Drawing.Size(75, 28);
            this.btnNormal.TabIndex = 31;
            this.btnNormal.Text = "Normal";
            this.btnNormal.UseVisualStyleBackColor = true;
            this.btnNormal.Click += new System.EventHandler(this.btnNormalClick);
            // 
            // btnFollow
            // 
            this.btnFollow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFollow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFollow.Location = new System.Drawing.Point(1061, 134);
            this.btnFollow.Name = "btnFollow";
            this.btnFollow.Size = new System.Drawing.Size(112, 28);
            this.btnFollow.TabIndex = 32;
            this.btnFollow.Text = "Follow";
            this.btnFollow.UseVisualStyleBackColor = true;
            this.btnFollow.Click += new System.EventHandler(this.btnFollowClick);
            // 
            // chkBoxPenalty
            // 
            this.chkBoxPenalty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkBoxPenalty.AutoSize = true;
            this.chkBoxPenalty.Checked = true;
            this.chkBoxPenalty.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBoxPenalty.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkBoxPenalty.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkBoxPenalty.Location = new System.Drawing.Point(876, 89);
            this.chkBoxPenalty.Name = "chkBoxPenalty";
            this.chkBoxPenalty.Size = new System.Drawing.Size(65, 19);
            this.chkBoxPenalty.TabIndex = 33;
            this.chkBoxPenalty.Text = "Penalty";
            this.chkBoxPenalty.UseVisualStyleBackColor = true;
            this.chkBoxPenalty.CheckedChanged += new System.EventHandler(this.chkOPenaltyHandle);
            // 
            // chkPreferGreen
            // 
            this.chkPreferGreen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkPreferGreen.AutoSize = true;
            this.chkPreferGreen.Checked = true;
            this.chkPreferGreen.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPreferGreen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPreferGreen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.chkPreferGreen.Location = new System.Drawing.Point(876, 70);
            this.chkPreferGreen.Name = "chkPreferGreen";
            this.chkPreferGreen.Size = new System.Drawing.Size(91, 19);
            this.chkPreferGreen.TabIndex = 34;
            this.chkPreferGreen.Text = "Prefer Green";
            this.chkPreferGreen.UseVisualStyleBackColor = true;
            this.chkPreferGreen.Visible = false;
            this.chkPreferGreen.CheckedChanged += new System.EventHandler(this.chkPreferGreenHandle);
            // 
            // btnSeeInGame
            // 
            this.btnSeeInGame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSeeInGame.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSeeInGame.Location = new System.Drawing.Point(1061, 74);
            this.btnSeeInGame.Name = "btnSeeInGame";
            this.btnSeeInGame.Size = new System.Drawing.Size(112, 28);
            this.btnSeeInGame.TabIndex = 35;
            this.btnSeeInGame.Text = "See in Game";
            this.btnSeeInGame.UseVisualStyleBackColor = true;
            this.btnSeeInGame.Click += new System.EventHandler(this.btnSeeInGameClick);
            // 
            // lblSimulationTimeText
            // 
            this.lblSimulationTimeText.AutoSize = true;
            this.lblSimulationTimeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSimulationTimeText.ForeColor = System.Drawing.Color.White;
            this.lblSimulationTimeText.Location = new System.Drawing.Point(190, 10);
            this.lblSimulationTimeText.Name = "lblSimulationTimeText";
            this.lblSimulationTimeText.Size = new System.Drawing.Size(145, 24);
            this.lblSimulationTimeText.TabIndex = 36;
            this.lblSimulationTimeText.Text = "Simulation Time";
            this.lblSimulationTimeText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSimulationTime
            // 
            this.lblSimulationTime.AutoSize = true;
            this.lblSimulationTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSimulationTime.ForeColor = System.Drawing.Color.White;
            this.lblSimulationTime.Location = new System.Drawing.Point(340, 10);
            this.lblSimulationTime.Name = "lblSimulationTime";
            this.lblSimulationTime.Size = new System.Drawing.Size(140, 24);
            this.lblSimulationTime.TabIndex = 37;
            this.lblSimulationTime.Text = "SimulationTime";
            this.lblSimulationTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uiRouteLabel
            // 
            this.uiRouteLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiRouteLabel.AutoSize = true;
            this.uiRouteLabel.BackColor = System.Drawing.Color.Transparent;
            this.uiRouteLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiRouteLabel.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiRouteLabel.Location = new System.Drawing.Point(995, 241);
            this.uiRouteLabel.Name = "uiRouteLabel";
            this.uiRouteLabel.Size = new System.Drawing.Size(64, 16);
            this.uiRouteLabel.TabIndex = 38;
            this.uiRouteLabel.Text = "ROUTE:";
            this.uiRouteLabel.Visible = false;
            // 
            // uiShowPlatformLabels
            // 
            this.uiShowPlatformLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowPlatformLabels.AutoSize = true;
            this.uiShowPlatformLabels.BackColor = System.Drawing.Color.Transparent;
            this.uiShowPlatformLabels.Checked = true;
            this.uiShowPlatformLabels.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiShowPlatformLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowPlatformLabels.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(168)))), ((int)(((byte)(168)))), ((int)(((byte)(168)))));
            this.uiShowPlatformLabels.Location = new System.Drawing.Point(1001, 282);
            this.uiShowPlatformLabels.Name = "uiShowPlatformLabels";
            this.uiShowPlatformLabels.Size = new System.Drawing.Size(125, 22);
            this.uiShowPlatformLabels.TabIndex = 39;
            this.uiShowPlatformLabels.Text = "Platform labels";
            this.uiShowPlatformLabels.UseVisualStyleBackColor = false;
            this.uiShowPlatformLabels.Visible = false;
            // 
            // uiShowSidings
            // 
            this.uiShowSidings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowSidings.AutoSize = true;
            this.uiShowSidings.BackColor = System.Drawing.Color.Transparent;
            this.uiShowSidings.Checked = true;
            this.uiShowSidings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiShowSidings.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowSidings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(168)))), ((int)(((byte)(168)))), ((int)(((byte)(168)))));
            this.uiShowSidings.Location = new System.Drawing.Point(1002, 304);
            this.uiShowSidings.Name = "uiShowSidings";
            this.uiShowSidings.Size = new System.Drawing.Size(109, 22);
            this.uiShowSidings.TabIndex = 40;
            this.uiShowSidings.Text = "Siding labels";
            this.uiShowSidings.UseVisualStyleBackColor = false;
            this.uiShowSidings.Visible = false;
            // 
            // uiShowSignals
            // 
            this.uiShowSignals.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowSignals.AutoSize = true;
            this.uiShowSignals.BackColor = System.Drawing.Color.Transparent;
            this.uiShowSignals.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowSignals.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiShowSignals.Location = new System.Drawing.Point(998, 384);
            this.uiShowSignals.Name = "uiShowSignals";
            this.uiShowSignals.Size = new System.Drawing.Size(75, 22);
            this.uiShowSignals.TabIndex = 41;
            this.uiShowSignals.Text = "Signals";
            this.uiShowSignals.UseVisualStyleBackColor = false;
            this.uiShowSignals.Visible = false;
            // 
            // uiShowSignalState
            // 
            this.uiShowSignalState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowSignalState.AutoSize = true;
            this.uiShowSignalState.BackColor = System.Drawing.Color.Transparent;
            this.uiShowSignalState.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowSignalState.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiShowSignalState.Location = new System.Drawing.Point(1003, 408);
            this.uiShowSignalState.Name = "uiShowSignalState";
            this.uiShowSignalState.Size = new System.Drawing.Size(142, 22);
            this.uiShowSignalState.TabIndex = 42;
            this.uiShowSignalState.Text = "Signal state Label";
            this.uiShowSignalState.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.uiShowSignalState.UseVisualStyleBackColor = false;
            this.uiShowSignalState.Visible = false;
            // 
            // uiTrainLabel
            // 
            this.uiTrainLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiTrainLabel.BackColor = System.Drawing.Color.Transparent;
            this.uiTrainLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiTrainLabel.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiTrainLabel.Location = new System.Drawing.Point(995, 457);
            this.uiTrainLabel.Name = "uiTrainLabel";
            this.uiTrainLabel.Size = new System.Drawing.Size(71, 22);
            this.uiTrainLabel.TabIndex = 43;
            this.uiTrainLabel.Text = "TRAIN:";
            this.uiTrainLabel.Visible = false;
            // 
            // uibTrainKey
            // 
            this.uibTrainKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uibTrainKey.BackColor = System.Drawing.Color.Gray;
            this.uibTrainKey.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.uibTrainKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uibTrainKey.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uibTrainKey.Location = new System.Drawing.Point(1080, 501);
            this.uibTrainKey.Name = "uibTrainKey";
            this.uibTrainKey.Size = new System.Drawing.Size(51, 28);
            this.uibTrainKey.TabIndex = 57;
            this.uibTrainKey.Text = "Key";
            this.uibTrainKey.UseVisualStyleBackColor = false;
            this.uibTrainKey.Click += new System.EventHandler(this.bTrainKey_Click);
            // 
            // uiShowActiveTrainLabels
            // 
            this.uiShowActiveTrainLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowActiveTrainLabels.AutoSize = true;
            this.uiShowActiveTrainLabels.Checked = true;
            this.uiShowActiveTrainLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowActiveTrainLabels.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(168)))), ((int)(((byte)(168)))), ((int)(((byte)(168)))));
            this.uiShowActiveTrainLabels.Location = new System.Drawing.Point(1000, 549);
            this.uiShowActiveTrainLabels.Name = "uiShowActiveTrainLabels";
            this.uiShowActiveTrainLabels.Size = new System.Drawing.Size(96, 22);
            this.uiShowActiveTrainLabels.TabIndex = 1;
            this.uiShowActiveTrainLabels.TabStop = true;
            this.uiShowActiveTrainLabels.Text = "Active only";
            this.uiShowActiveTrainLabels.UseVisualStyleBackColor = false;
            this.uiShowActiveTrainLabels.Visible = false;
            // 
            // uiShowAllTrainLabels
            // 
            this.uiShowAllTrainLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowAllTrainLabels.AutoSize = true;
            this.uiShowAllTrainLabels.BackColor = System.Drawing.Color.Transparent;
            this.uiShowAllTrainLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowAllTrainLabels.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(168)))), ((int)(((byte)(168)))), ((int)(((byte)(168)))));
            this.uiShowAllTrainLabels.Location = new System.Drawing.Point(998, 526);
            this.uiShowAllTrainLabels.Name = "uiShowAllTrainLabels";
            this.uiShowAllTrainLabels.Size = new System.Drawing.Size(41, 22);
            this.uiShowAllTrainLabels.TabIndex = 0;
            this.uiShowAllTrainLabels.Text = "All";
            this.uiShowAllTrainLabels.UseVisualStyleBackColor = false;
            this.uiShowAllTrainLabels.Visible = false;
            // 
            // nudDaylightOffsetHrs
            // 
            this.nudDaylightOffsetHrs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudDaylightOffsetHrs.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.nudDaylightOffsetHrs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudDaylightOffsetHrs.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.nudDaylightOffsetHrs.Location = new System.Drawing.Point(1049, 667);
            this.nudDaylightOffsetHrs.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.nudDaylightOffsetHrs.Minimum = new decimal(new int[] {
            12,
            0,
            0,
            -2147483648});
            this.nudDaylightOffsetHrs.Name = "nudDaylightOffsetHrs";
            this.nudDaylightOffsetHrs.Size = new System.Drawing.Size(51, 21);
            this.nudDaylightOffsetHrs.TabIndex = 44;
            this.nudDaylightOffsetHrs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudDaylightOffsetHrs.Visible = false;
            this.nudDaylightOffsetHrs.ValueChanged += new System.EventHandler(this.nudDaylightOffsetHrs_ValueChanged);
            // 
            // uilblDayLightOffsetHrs
            // 
            this.uilblDayLightOffsetHrs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uilblDayLightOffsetHrs.AutoSize = true;
            this.uilblDayLightOffsetHrs.BackColor = System.Drawing.Color.Transparent;
            this.uilblDayLightOffsetHrs.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uilblDayLightOffsetHrs.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uilblDayLightOffsetHrs.Location = new System.Drawing.Point(1016, 640);
            this.uilblDayLightOffsetHrs.Name = "uilblDayLightOffsetHrs";
            this.uilblDayLightOffsetHrs.Size = new System.Drawing.Size(136, 18);
            this.uilblDayLightOffsetHrs.TabIndex = 45;
            this.uilblDayLightOffsetHrs.Text = "Daylight offset (hrs)";
            this.uilblDayLightOffsetHrs.Visible = false;
            // 
            // cdBackground
            // 
            this.cdBackground.AnyColor = true;
            this.cdBackground.ShowHelp = true;
            // 
            // uibThemeChance
            // 
            this.uibThemeChance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uibThemeChance.BackColor = System.Drawing.Color.DimGray;
            this.uibThemeChance.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.uibThemeChance.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uibThemeChance.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uibThemeChance.Location = new System.Drawing.Point(1001, 785);
            this.uibThemeChance.Name = "uibThemeChance";
            this.uibThemeChance.Size = new System.Drawing.Size(154, 28);
            this.uibThemeChance.TabIndex = 46;
            this.uibThemeChance.Text = "Toggle Color Theme";
            this.uibThemeChance.UseVisualStyleBackColor = false;
            this.uibThemeChance.Click += new System.EventHandler(this.uibThemeChance_Click);
            // 
            // uiShowSwitches
            // 
            this.uiShowSwitches.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowSwitches.AutoSize = true;
            this.uiShowSwitches.BackColor = System.Drawing.Color.Transparent;
            this.uiShowSwitches.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowSwitches.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiShowSwitches.Location = new System.Drawing.Point(997, 361);
            this.uiShowSwitches.Name = "uiShowSwitches";
            this.uiShowSwitches.Size = new System.Drawing.Size(87, 22);
            this.uiShowSwitches.TabIndex = 47;
            this.uiShowSwitches.Text = "Switches";
            this.uiShowSwitches.UseVisualStyleBackColor = false;
            this.uiShowSwitches.Visible = false;
            // 
            // lblInstruction1
            // 
            this.lblInstruction1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstruction1.ForeColor = System.Drawing.Color.Silver;
            this.lblInstruction1.Location = new System.Drawing.Point(45, 698);
            this.lblInstruction1.Name = "lblInstruction1";
            this.lblInstruction1.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction1.Size = new System.Drawing.Size(420, 25);
            this.lblInstruction1.TabIndex = 48;
            this.lblInstruction1.Text = "To pan, drag with left mouse.";
            this.lblInstruction1.Visible = false;
            // 
            // uiShowTrainLabels
            // 
            this.uiShowTrainLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowTrainLabels.AutoSize = true;
            this.uiShowTrainLabels.BackColor = System.Drawing.Color.Transparent;
            this.uiShowTrainLabels.Checked = true;
            this.uiShowTrainLabels.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiShowTrainLabels.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowTrainLabels.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiShowTrainLabels.Location = new System.Drawing.Point(997, 477);
            this.uiShowTrainLabels.Name = "uiShowTrainLabels";
            this.uiShowTrainLabels.Size = new System.Drawing.Size(67, 22);
            this.uiShowTrainLabels.TabIndex = 50;
            this.uiShowTrainLabels.Text = "Name";
            this.uiShowTrainLabels.UseVisualStyleBackColor = false;
            this.uiShowTrainLabels.Visible = false;
            // 
            // tWindow
            // 
            this.tWindow.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tWindow.Controls.Add(this.tDispatch);
            this.tWindow.Controls.Add(this.tTimetable);
            this.tWindow.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tWindow.Location = new System.Drawing.Point(1, 3);
            this.tWindow.Name = "tWindow";
            this.tWindow.SelectedIndex = 0;
            this.tWindow.Size = new System.Drawing.Size(149, 20);
            this.tWindow.TabIndex = 51;
            this.tWindow.SelectedIndexChanged += new System.EventHandler(this.tWindow_SelectedIndexChanged);
            // 
            // tDispatch
            // 
            this.tDispatch.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tDispatch.Location = new System.Drawing.Point(4, 4);
            this.tDispatch.Name = "tDispatch";
            this.tDispatch.Padding = new System.Windows.Forms.Padding(3);
            this.tDispatch.Size = new System.Drawing.Size(141, 0);
            this.tDispatch.TabIndex = 0;
            this.tDispatch.Text = "Dispatch";
            this.tDispatch.UseVisualStyleBackColor = true;
            // 
            // tTimetable
            // 
            this.tTimetable.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tTimetable.Location = new System.Drawing.Point(4, 4);
            this.tTimetable.Name = "tTimetable";
            this.tTimetable.Padding = new System.Windows.Forms.Padding(3);
            this.tTimetable.Size = new System.Drawing.Size(141, 0);
            this.tTimetable.TabIndex = 1;
            this.tTimetable.Text = "Timetable";
            this.tTimetable.UseVisualStyleBackColor = true;
            // 
            // uiShowTrainState
            // 
            this.uiShowTrainState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowTrainState.AutoSize = true;
            this.uiShowTrainState.BackColor = System.Drawing.Color.Transparent;
            this.uiShowTrainState.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowTrainState.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiShowTrainState.Location = new System.Drawing.Point(998, 501);
            this.uiShowTrainState.Name = "uiShowTrainState";
            this.uiShowTrainState.Size = new System.Drawing.Size(61, 22);
            this.uiShowTrainState.TabIndex = 52;
            this.uiShowTrainState.Text = "State";
            this.uiShowTrainState.UseVisualStyleBackColor = false;
            this.uiShowTrainState.Visible = false;
            // 
            // lblInstruction2
            // 
            this.lblInstruction2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstruction2.ForeColor = System.Drawing.Color.Silver;
            this.lblInstruction2.Location = new System.Drawing.Point(45, 724);
            this.lblInstruction2.Name = "lblInstruction2";
            this.lblInstruction2.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction2.Size = new System.Drawing.Size(420, 24);
            this.lblInstruction2.TabIndex = 53;
            this.lblInstruction2.Text = "To zoom, drag with left and right mouse or scroll mouse wheel.";
            this.lblInstruction2.Visible = false;
            // 
            // lblInstruction3
            // 
            this.lblInstruction3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstruction3.ForeColor = System.Drawing.Color.Silver;
            this.lblInstruction3.Location = new System.Drawing.Point(45, 749);
            this.lblInstruction3.Name = "lblInstruction3";
            this.lblInstruction3.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction3.Size = new System.Drawing.Size(420, 24);
            this.lblInstruction3.TabIndex = 54;
            this.lblInstruction3.Text = "To zoom in to a location, press Shift and click the left mouse.";
            this.lblInstruction3.Visible = false;
            // 
            // lblInstruction4
            // 
            this.lblInstruction4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInstruction4.ForeColor = System.Drawing.Color.Silver;
            this.lblInstruction4.Location = new System.Drawing.Point(10, 920);
            this.lblInstruction4.Name = "lblInstruction4";
            this.lblInstruction4.Padding = new System.Windows.Forms.Padding(3);
            this.lblInstruction4.Size = new System.Drawing.Size(420, 25);
            this.lblInstruction4.TabIndex = 55;
            this.lblInstruction4.Text = "To zoom out of a location, press Alt and click the left mouse.";
            this.lblInstruction4.Visible = false;
            // 
            // uiShowPlatforms
            // 
            this.uiShowPlatforms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowPlatforms.AutoSize = true;
            this.uiShowPlatforms.BackColor = System.Drawing.Color.Transparent;
            this.uiShowPlatforms.Checked = true;
            this.uiShowPlatforms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiShowPlatforms.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiShowPlatforms.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiShowPlatforms.Location = new System.Drawing.Point(998, 260);
            this.uiShowPlatforms.Name = "uiShowPlatforms";
            this.uiShowPlatforms.Size = new System.Drawing.Size(91, 22);
            this.uiShowPlatforms.TabIndex = 56;
            this.uiShowPlatforms.Text = "Platforms";
            this.uiShowPlatforms.UseVisualStyleBackColor = false;
            this.uiShowPlatforms.Visible = false;
            // 
            // uiTrackLabel
            // 
            this.uiTrackLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiTrackLabel.AutoSize = true;
            this.uiTrackLabel.BackColor = System.Drawing.Color.Transparent;
            this.uiTrackLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uiTrackLabel.ForeColor = System.Drawing.SystemColors.ActiveBorder;
            this.uiTrackLabel.Location = new System.Drawing.Point(995, 342);
            this.uiTrackLabel.Name = "uiTrackLabel";
            this.uiTrackLabel.Size = new System.Drawing.Size(61, 16);
            this.uiTrackLabel.TabIndex = 57;
            this.uiTrackLabel.Text = "TRACK:";
            // 
            // AnchorPoint
            // 
            this.AnchorPoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AnchorPoint.AutoSize = true;
            this.AnchorPoint.Location = new System.Drawing.Point(989, 17);
            this.AnchorPoint.Name = "AnchorPoint";
            this.AnchorPoint.Size = new System.Drawing.Size(13, 13);
            this.AnchorPoint.TabIndex = 58;
            this.AnchorPoint.Text = "  ";
            // 
            // DispatchViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ClientSize = new System.Drawing.Size(1187, 867);
            this.Controls.Add(this.AnchorPoint);
            this.Controls.Add(this.uibTrainKey);
            this.Controls.Add(this.uiTrackLabel);
            this.Controls.Add(this.uiShowPlatforms);
            this.Controls.Add(this.lblInstruction4);
            this.Controls.Add(this.lblInstruction3);
            this.Controls.Add(this.lblInstruction2);
            this.Controls.Add(this.uiShowTrainState);
            this.Controls.Add(this.uiShowTrainLabels);
            this.Controls.Add(this.lblInstruction1);
            this.Controls.Add(this.uiShowSwitches);
            this.Controls.Add(this.uibThemeChance);
            this.Controls.Add(this.uilblDayLightOffsetHrs);
            this.Controls.Add(this.nudDaylightOffsetHrs);
            this.Controls.Add(this.uiTrainLabel);
            this.Controls.Add(this.uiShowSignalState);
            this.Controls.Add(this.uiShowSignals);
            this.Controls.Add(this.uiShowSidings);
            this.Controls.Add(this.uiShowPlatformLabels);
            this.Controls.Add(this.uiRouteLabel);
            this.Controls.Add(this.uiShowActiveTrainLabels);
            this.Controls.Add(this.uiShowAllTrainLabels);
            this.Controls.Add(this.lblSimulationTime);
            this.Controls.Add(this.lblSimulationTimeText);
            this.Controls.Add(this.btnSeeInGame);
            this.Controls.Add(this.chkPreferGreen);
            this.Controls.Add(this.chkBoxPenalty);
            this.Controls.Add(this.btnFollow);
            this.Controls.Add(this.btnNormal);
            this.Controls.Add(this.btnAssist);
            this.Controls.Add(this.chkAllowNew);
            this.Controls.Add(this.chkPickSwitches);
            this.Controls.Add(this.chkPickSignals);
            this.Controls.Add(this.boxSetSwitch);
            this.Controls.Add(this.boxSetSignal);
            this.Controls.Add(this.chkDrawPath);
            this.Controls.Add(this.reply2Selected);
            this.Controls.Add(this.messages);
            this.Controls.Add(this.composeMSG);
            this.Controls.Add(this.msgAll);
            this.Controls.Add(this.msgSelected);
            this.Controls.Add(this.MSG);
            this.Controls.Add(this.chkShowAvatars);
            this.Controls.Add(this.chkAllowUserSwitch);
            this.Controls.Add(this.rmvButton);
            this.Controls.Add(this.resLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.windowSizeUpDown);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.tWindow);
            this.Controls.Add(this.pbCanvas);
            this.Controls.Add(this.AvatarView);
            this.Name = "DispatchViewer";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Map Window";
            this.Leave += new System.EventHandler(this.DispatcherLeave);
            ((System.ComponentModel.ISupportInitialize)(this.pbCanvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.windowSizeUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDaylightOffsetHrs)).EndInit();
            this.tWindow.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox boxSetSignal;
        private System.Windows.Forms.ListBox boxSetSwitch;
        private System.Windows.Forms.ColorDialog cdBackground;
        private System.Windows.Forms.Label lblInstruction1;
        private System.Windows.Forms.TabPage tDispatch;
        private System.Windows.Forms.TabPage tTimetable;
        public System.Windows.Forms.Button refreshButton;
        public System.Windows.Forms.NumericUpDown windowSizeUpDown;
        public System.Windows.Forms.Label resLabel;
        public System.Windows.Forms.ListView AvatarView;
        public System.Windows.Forms.Button rmvButton;
        public System.Windows.Forms.CheckBox chkAllowUserSwitch;
        public System.Windows.Forms.CheckBox chkShowAvatars;
        public System.Windows.Forms.TextBox MSG;
        public System.Windows.Forms.Button msgSelected;
        public System.Windows.Forms.Button msgAll;
        public System.Windows.Forms.Button composeMSG;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Button reply2Selected;
        public System.Windows.Forms.CheckBox chkDrawPath;
        public System.Windows.Forms.CheckBox chkPickSignals;
        public System.Windows.Forms.CheckBox chkPickSwitches;
        public System.Windows.Forms.CheckBox chkAllowNew;
        public System.Windows.Forms.ListBox messages;
        public System.Windows.Forms.Button btnAssist;
        public System.Windows.Forms.Button btnNormal;
        public System.Windows.Forms.Button btnFollow;
        public System.Windows.Forms.CheckBox chkBoxPenalty;
        public System.Windows.Forms.CheckBox chkPreferGreen;
        public System.Windows.Forms.Button btnSeeInGame;
        public System.Windows.Forms.Label lblSimulationTimeText;
        public System.Windows.Forms.Label lblSimulationTime;
        public System.Windows.Forms.Label uiRouteLabel;
        public System.Windows.Forms.CheckBox uiShowPlatforms;
        public System.Windows.Forms.CheckBox uiShowPlatformLabels;
        public System.Windows.Forms.CheckBox uiShowSidings;
        public System.Windows.Forms.CheckBox uiShowSignals;
        public System.Windows.Forms.CheckBox uiShowSignalState;
        public System.Windows.Forms.CheckBox uiShowSwitches;
        public System.Windows.Forms.CheckBox uiShowTrainLabels;
        public System.Windows.Forms.Label uiTrainLabel;
        public System.Windows.Forms.RadioButton uiShowActiveTrainLabels;
        public System.Windows.Forms.RadioButton uiShowAllTrainLabels;
        public System.Windows.Forms.NumericUpDown nudDaylightOffsetHrs;
        public System.Windows.Forms.Label uilblDayLightOffsetHrs;
        public System.Windows.Forms.Button uibThemeChance;
        public System.Windows.Forms.PictureBox pbCanvas;
        public System.Windows.Forms.TabControl tWindow;
        public System.Windows.Forms.CheckBox uiShowTrainState;
        private System.Windows.Forms.Label lblInstruction2;
        private System.Windows.Forms.Label lblInstruction3;
        private System.Windows.Forms.Label lblInstruction4;
        public System.Windows.Forms.Button uibTrainKey;
        public System.Windows.Forms.Label uiTrackLabel;
        public System.Windows.Forms.Label AnchorPoint;
    }
}


