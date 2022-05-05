namespace GateUI
{
    partial class Form1
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rd1CheckOut = new System.Windows.Forms.RadioButton();
            this.rd1CheckIn = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rd2CheckIn = new System.Windows.Forms.RadioButton();
            this.rd2CheckOut = new System.Windows.Forms.RadioButton();
            this.txtSiteId = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSetSiteId = new System.Windows.Forms.Button();
            this.comboFareModes = new System.Windows.Forms.ComboBox();
            this.btnSetFareMode = new System.Windows.Forms.Button();
            this.userControlRW2 = new GateUI.UserControlRW();
            this.userControlRW1 = new GateUI.UserControlRW();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rd1CheckOut);
            this.groupBox1.Controls.Add(this.rd1CheckIn);
            this.groupBox1.Location = new System.Drawing.Point(179, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(146, 69);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Reader 1";
            // 
            // rd1CheckOut
            // 
            this.rd1CheckOut.AutoSize = true;
            this.rd1CheckOut.Location = new System.Drawing.Point(63, 46);
            this.rd1CheckOut.Name = "rd1CheckOut";
            this.rd1CheckOut.Size = new System.Drawing.Size(74, 17);
            this.rd1CheckOut.TabIndex = 16;
            this.rd1CheckOut.TabStop = true;
            this.rd1CheckOut.Text = "Check-out";
            this.rd1CheckOut.UseVisualStyleBackColor = true;
            this.rd1CheckOut.CheckedChanged += new System.EventHandler(this.rd1CheckOut_CheckedChanged);
            // 
            // rd1CheckIn
            // 
            this.rd1CheckIn.AutoSize = true;
            this.rd1CheckIn.Location = new System.Drawing.Point(63, 23);
            this.rd1CheckIn.Name = "rd1CheckIn";
            this.rd1CheckIn.Size = new System.Drawing.Size(67, 17);
            this.rd1CheckIn.TabIndex = 15;
            this.rd1CheckIn.TabStop = true;
            this.rd1CheckIn.Text = "Check-in";
            this.rd1CheckIn.UseVisualStyleBackColor = true;
            this.rd1CheckIn.CheckedChanged += new System.EventHandler(this.rd1CheckIn_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rd2CheckIn);
            this.groupBox2.Controls.Add(this.rd2CheckOut);
            this.groupBox2.Location = new System.Drawing.Point(721, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(146, 69);
            this.groupBox2.TabIndex = 18;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Reader 2";
            // 
            // rd2CheckIn
            // 
            this.rd2CheckIn.AutoSize = true;
            this.rd2CheckIn.Location = new System.Drawing.Point(34, 19);
            this.rd2CheckIn.Name = "rd2CheckIn";
            this.rd2CheckIn.Size = new System.Drawing.Size(67, 17);
            this.rd2CheckIn.TabIndex = 16;
            this.rd2CheckIn.TabStop = true;
            this.rd2CheckIn.Text = "Check-in";
            this.rd2CheckIn.UseVisualStyleBackColor = true;
            this.rd2CheckIn.CheckedChanged += new System.EventHandler(this.rd2CheckIn_CheckedChanged);
            // 
            // rd2CheckOut
            // 
            this.rd2CheckOut.AutoSize = true;
            this.rd2CheckOut.Location = new System.Drawing.Point(34, 46);
            this.rd2CheckOut.Name = "rd2CheckOut";
            this.rd2CheckOut.Size = new System.Drawing.Size(74, 17);
            this.rd2CheckOut.TabIndex = 15;
            this.rd2CheckOut.TabStop = true;
            this.rd2CheckOut.Text = "Check-out";
            this.rd2CheckOut.UseVisualStyleBackColor = true;
            this.rd2CheckOut.CheckedChanged += new System.EventHandler(this.rd2CheckOut_CheckedChanged);
            // 
            // txtSiteId
            // 
            this.txtSiteId.Location = new System.Drawing.Point(1206, 166);
            this.txtSiteId.Name = "txtSiteId";
            this.txtSiteId.Size = new System.Drawing.Size(73, 20);
            this.txtSiteId.TabIndex = 19;
            this.txtSiteId.TextChanged += new System.EventHandler(this.txtSiteId_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1166, 169);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "SiteId";
            // 
            // btnSetSiteId
            // 
            this.btnSetSiteId.Enabled = false;
            this.btnSetSiteId.Location = new System.Drawing.Point(1184, 194);
            this.btnSetSiteId.Name = "btnSetSiteId";
            this.btnSetSiteId.Size = new System.Drawing.Size(94, 27);
            this.btnSetSiteId.TabIndex = 21;
            this.btnSetSiteId.Text = "Set Site Id";
            this.btnSetSiteId.UseVisualStyleBackColor = true;
            this.btnSetSiteId.Click += new System.EventHandler(this.btnSetSiteId_Click);
            // 
            // comboFareModes
            // 
            this.comboFareModes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboFareModes.FormattingEnabled = true;
            this.comboFareModes.Location = new System.Drawing.Point(1184, 298);
            this.comboFareModes.Name = "comboFareModes";
            this.comboFareModes.Size = new System.Drawing.Size(121, 21);
            this.comboFareModes.TabIndex = 22;
            this.comboFareModes.SelectedIndexChanged += new System.EventHandler(this.comboFareModes_SelectedIndexChanged);
            // 
            // btnSetFareMode
            // 
            this.btnSetFareMode.Enabled = false;
            this.btnSetFareMode.Location = new System.Drawing.Point(1206, 325);
            this.btnSetFareMode.Name = "btnSetFareMode";
            this.btnSetFareMode.Size = new System.Drawing.Size(94, 27);
            this.btnSetFareMode.TabIndex = 23;
            this.btnSetFareMode.Text = "Set Fare Mode";
            this.btnSetFareMode.UseVisualStyleBackColor = true;
            this.btnSetFareMode.Click += new System.EventHandler(this.btnSetFareMode_Click);
            // 
            // userControlRW2
            // 
            this.userControlRW2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.userControlRW2.Location = new System.Drawing.Point(597, 88);
            this.userControlRW2.Name = "userControlRW2";
            this.userControlRW2.Size = new System.Drawing.Size(511, 484);
            this.userControlRW2.TabIndex = 12;
            // 
            // userControlRW1
            // 
            this.userControlRW1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.userControlRW1.Location = new System.Drawing.Point(22, 88);
            this.userControlRW1.Name = "userControlRW1";
            this.userControlRW1.Size = new System.Drawing.Size(511, 484);
            this.userControlRW1.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1361, 584);
            this.Controls.Add(this.btnSetFareMode);
            this.Controls.Add(this.comboFareModes);
            this.Controls.Add(this.btnSetSiteId);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSiteId);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.userControlRW2);
            this.Controls.Add(this.userControlRW1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private UserControlRW userControlRW1;
        private UserControlRW userControlRW2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rd1CheckOut;
        private System.Windows.Forms.RadioButton rd1CheckIn;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rd2CheckIn;
        private System.Windows.Forms.RadioButton rd2CheckOut;
        private System.Windows.Forms.TextBox txtSiteId;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSetSiteId;
        private System.Windows.Forms.ComboBox comboFareModes;
        private System.Windows.Forms.Button btnSetFareMode;

    }
}

