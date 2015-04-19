namespace SMS_API_Demo
{
    partial class RetrieveDeletedDevices
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RetrieveDeletedDevices));
            this.btnGetDeletedDevices = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.dtpDateStart = new System.Windows.Forms.DateTimePicker();
            this.dtpDateEnd = new System.Windows.Forms.DateTimePicker();
            this.lblCaptionPrefix = new System.Windows.Forms.Label();
            this.txtCaptionPrefix = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.numDeviceTypeID = new System.Windows.Forms.NumericUpDown();
            this.label15 = new System.Windows.Forms.Label();
            this.numAreaID = new System.Windows.Forms.NumericUpDown();
            this.label14 = new System.Windows.Forms.Label();
            this.numDeviceID = new System.Windows.Forms.NumericUpDown();
            this.ttRetrieveDeletedDevices = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numDeviceTypeID)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAreaID)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDeviceID)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGetDeletedDevices
            // 
            this.btnGetDeletedDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGetDeletedDevices.Location = new System.Drawing.Point(12, 209);
            this.btnGetDeletedDevices.Name = "btnGetDeletedDevices";
            this.btnGetDeletedDevices.Size = new System.Drawing.Size(133, 23);
            this.btnGetDeletedDevices.TabIndex = 54;
            this.btnGetDeletedDevices.Text = "Get Deleted Devices";
            this.btnGetDeletedDevices.UseVisualStyleBackColor = true;
            this.btnGetDeletedDevices.Click += new System.EventHandler(this.btnGetDeletedDevices_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(205, 209);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 23);
            this.btnCancel.TabIndex = 55;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(39, 44);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(52, 13);
            this.label13.TabIndex = 59;
            this.label13.Text = "End Date";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(39, 18);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(55, 13);
            this.label8.TabIndex = 57;
            this.label8.Text = "Start Date";
            // 
            // dtpDateStart
            // 
            this.dtpDateStart.Checked = false;
            this.dtpDateStart.Location = new System.Drawing.Point(97, 12);
            this.dtpDateStart.Name = "dtpDateStart";
            this.dtpDateStart.ShowCheckBox = true;
            this.dtpDateStart.Size = new System.Drawing.Size(200, 20);
            this.dtpDateStart.TabIndex = 60;
            this.ttRetrieveDeletedDevices.SetToolTip(this.dtpDateStart, "Check Box and Select Date to Enable");
            // 
            // dtpDateEnd
            // 
            this.dtpDateEnd.Checked = false;
            this.dtpDateEnd.Location = new System.Drawing.Point(97, 38);
            this.dtpDateEnd.Name = "dtpDateEnd";
            this.dtpDateEnd.ShowCheckBox = true;
            this.dtpDateEnd.Size = new System.Drawing.Size(200, 20);
            this.dtpDateEnd.TabIndex = 61;
            this.ttRetrieveDeletedDevices.SetToolTip(this.dtpDateEnd, "Check Box and Select Date to Enable");
            // 
            // lblCaptionPrefix
            // 
            this.lblCaptionPrefix.AutoSize = true;
            this.lblCaptionPrefix.Location = new System.Drawing.Point(39, 79);
            this.lblCaptionPrefix.Name = "lblCaptionPrefix";
            this.lblCaptionPrefix.Size = new System.Drawing.Size(109, 13);
            this.lblCaptionPrefix.TabIndex = 63;
            this.lblCaptionPrefix.Text = "Device Caption Prefix";
            // 
            // txtCaptionPrefix
            // 
            this.txtCaptionPrefix.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCaptionPrefix.Location = new System.Drawing.Point(154, 76);
            this.txtCaptionPrefix.Name = "txtCaptionPrefix";
            this.txtCaptionPrefix.Size = new System.Drawing.Size(143, 20);
            this.txtCaptionPrefix.TabIndex = 62;
            this.ttRetrieveDeletedDevices.SetToolTip(this.txtCaptionPrefix, "Enter Beginning Characters of Device to Retrieve");
            this.txtCaptionPrefix.Validated += new System.EventHandler(this.txtCaptionPrefix_Validated);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(39, 115);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(52, 13);
            this.label16.TabIndex = 69;
            this.label16.Text = "DeviceID";
            // 
            // numDeviceTypeID
            // 
            this.numDeviceTypeID.Location = new System.Drawing.Point(97, 163);
            this.numDeviceTypeID.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.numDeviceTypeID.Minimum = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            this.numDeviceTypeID.Name = "numDeviceTypeID";
            this.numDeviceTypeID.Size = new System.Drawing.Size(73, 20);
            this.numDeviceTypeID.TabIndex = 68;
            this.ttRetrieveDeletedDevices.SetToolTip(this.numDeviceTypeID, "-99 Retrieves All");
            this.numDeviceTypeID.Value = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(51, 139);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(40, 13);
            this.label15.TabIndex = 67;
            this.label15.Text = "AreaID";
            // 
            // numAreaID
            // 
            this.numAreaID.Location = new System.Drawing.Point(97, 137);
            this.numAreaID.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.numAreaID.Minimum = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            this.numAreaID.Name = "numAreaID";
            this.numAreaID.Size = new System.Drawing.Size(73, 20);
            this.numAreaID.TabIndex = 66;
            this.ttRetrieveDeletedDevices.SetToolTip(this.numAreaID, "-99 Retrieves All");
            this.numAreaID.Value = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(18, 165);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(76, 13);
            this.label14.TabIndex = 65;
            this.label14.Text = "DeviceTypeID";
            // 
            // numDeviceID
            // 
            this.numDeviceID.Location = new System.Drawing.Point(97, 113);
            this.numDeviceID.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.numDeviceID.Minimum = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            this.numDeviceID.Name = "numDeviceID";
            this.numDeviceID.Size = new System.Drawing.Size(73, 20);
            this.numDeviceID.TabIndex = 64;
            this.ttRetrieveDeletedDevices.SetToolTip(this.numDeviceID, "-99 Retrieves All");
            this.numDeviceID.Value = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            // 
            // RetrieveDeletedDevices
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(317, 245);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.numDeviceTypeID);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.numAreaID);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.numDeviceID);
            this.Controls.Add(this.lblCaptionPrefix);
            this.Controls.Add(this.txtCaptionPrefix);
            this.Controls.Add(this.dtpDateEnd);
            this.Controls.Add(this.dtpDateStart);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnGetDeletedDevices);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RetrieveDeletedDevices";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Deleted Device Filters";
            ((System.ComponentModel.ISupportInitialize)(this.numDeviceTypeID)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAreaID)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDeviceID)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetDeletedDevices;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label8;
        public System.Windows.Forms.DateTimePicker dtpDateStart;
        public System.Windows.Forms.DateTimePicker dtpDateEnd;
        private System.Windows.Forms.Label lblCaptionPrefix;
        public System.Windows.Forms.TextBox txtCaptionPrefix;
        private System.Windows.Forms.Label label16;
        public System.Windows.Forms.NumericUpDown numDeviceTypeID;
        private System.Windows.Forms.Label label15;
        public System.Windows.Forms.NumericUpDown numAreaID;
        private System.Windows.Forms.Label label14;
        public System.Windows.Forms.NumericUpDown numDeviceID;
        private System.Windows.Forms.ToolTip ttRetrieveDeletedDevices;
    }
}