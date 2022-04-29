﻿namespace GateUI
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
            this.panelReader1 = new System.Windows.Forms.Panel();
            this.panelRejectCode = new System.Windows.Forms.Panel();
            this.txtRejectCode = new System.Windows.Forms.TextBox();
            this.lblRejectCode = new System.Windows.Forms.Label();
            this.txtToppedUp = new System.Windows.Forms.TextBox();
            this.lblToppedUp = new System.Windows.Forms.Label();
            this.txtBalance = new System.Windows.Forms.TextBox();
            this.imgIcon = new System.Windows.Forms.PictureBox();
            this.lblPurse = new System.Windows.Forms.Label();
            this.userControlRW1 = new GateUI.UserControlRW();
            this.panelReader1.SuspendLayout();
            this.panelRejectCode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // panelReader1
            // 
            this.panelReader1.Controls.Add(this.panelRejectCode);
            this.panelReader1.Controls.Add(this.txtToppedUp);
            this.panelReader1.Controls.Add(this.lblToppedUp);
            this.panelReader1.Controls.Add(this.txtBalance);
            this.panelReader1.Controls.Add(this.imgIcon);
            this.panelReader1.Controls.Add(this.lblPurse);
            this.panelReader1.Location = new System.Drawing.Point(71, 27);
            this.panelReader1.Name = "panelReader1";
            this.panelReader1.Size = new System.Drawing.Size(431, 464);
            this.panelReader1.TabIndex = 10;
            // 
            // panelRejectCode
            // 
            this.panelRejectCode.Controls.Add(this.txtRejectCode);
            this.panelRejectCode.Controls.Add(this.lblRejectCode);
            this.panelRejectCode.Location = new System.Drawing.Point(46, 165);
            this.panelRejectCode.Name = "panelRejectCode";
            this.panelRejectCode.Size = new System.Drawing.Size(373, 79);
            this.panelRejectCode.TabIndex = 15;
            // 
            // txtRejectCode
            // 
            this.txtRejectCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRejectCode.Location = new System.Drawing.Point(205, 27);
            this.txtRejectCode.Name = "txtRejectCode";
            this.txtRejectCode.ReadOnly = true;
            this.txtRejectCode.Size = new System.Drawing.Size(155, 38);
            this.txtRejectCode.TabIndex = 10;
            // 
            // lblRejectCode
            // 
            this.lblRejectCode.AutoSize = true;
            this.lblRejectCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRejectCode.Location = new System.Drawing.Point(13, 27);
            this.lblRejectCode.Name = "lblRejectCode";
            this.lblRejectCode.Size = new System.Drawing.Size(175, 33);
            this.lblRejectCode.TabIndex = 9;
            this.lblRejectCode.Text = "Reject Code";
            // 
            // txtToppedUp
            // 
            this.txtToppedUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtToppedUp.Location = new System.Drawing.Point(206, 104);
            this.txtToppedUp.Name = "txtToppedUp";
            this.txtToppedUp.ReadOnly = true;
            this.txtToppedUp.Size = new System.Drawing.Size(155, 38);
            this.txtToppedUp.TabIndex = 14;
            // 
            // lblToppedUp
            // 
            this.lblToppedUp.AutoSize = true;
            this.lblToppedUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblToppedUp.Location = new System.Drawing.Point(61, 106);
            this.lblToppedUp.Name = "lblToppedUp";
            this.lblToppedUp.Size = new System.Drawing.Size(107, 33);
            this.lblToppedUp.TabIndex = 13;
            this.lblToppedUp.Text = "Top-up";
            // 
            // txtBalance
            // 
            this.txtBalance.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBalance.Location = new System.Drawing.Point(206, 48);
            this.txtBalance.Name = "txtBalance";
            this.txtBalance.ReadOnly = true;
            this.txtBalance.Size = new System.Drawing.Size(155, 38);
            this.txtBalance.TabIndex = 12;
            // 
            // imgIcon
            // 
            this.imgIcon.Location = new System.Drawing.Point(12, 273);
            this.imgIcon.Name = "imgIcon";
            this.imgIcon.Size = new System.Drawing.Size(256, 144);
            this.imgIcon.TabIndex = 11;
            this.imgIcon.TabStop = false;
            // 
            // lblPurse
            // 
            this.lblPurse.AutoSize = true;
            this.lblPurse.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPurse.Location = new System.Drawing.Point(61, 50);
            this.lblPurse.Name = "lblPurse";
            this.lblPurse.Size = new System.Drawing.Size(120, 33);
            this.lblPurse.TabIndex = 10;
            this.lblPurse.Text = "Balance";
            // 
            // userControlRW1
            // 
            this.userControlRW1.Location = new System.Drawing.Point(577, 27);
            this.userControlRW1.Name = "userControlRW1";
            this.userControlRW1.Size = new System.Drawing.Size(511, 484);
            this.userControlRW1.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1214, 584);
            this.Controls.Add(this.userControlRW1);
            this.Controls.Add(this.panelReader1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.panelReader1.ResumeLayout(false);
            this.panelReader1.PerformLayout();
            this.panelRejectCode.ResumeLayout(false);
            this.panelRejectCode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelReader1;
        private System.Windows.Forms.Panel panelRejectCode;
        private System.Windows.Forms.TextBox txtRejectCode;
        private System.Windows.Forms.Label lblRejectCode;
        private System.Windows.Forms.TextBox txtToppedUp;
        private System.Windows.Forms.Label lblToppedUp;
        private System.Windows.Forms.TextBox txtBalance;
        private System.Windows.Forms.PictureBox imgIcon;
        private System.Windows.Forms.Label lblPurse;
        private UserControlRW userControlRW1;

    }
}

