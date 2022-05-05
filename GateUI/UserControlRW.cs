using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.MediaTreatment;
using IFS2.Equipment.Common;

namespace GateUI
{
    public partial class UserControlRW : UserControl
    {
        public UserControlRW()
        {
            InitializeComponent();
            this.BackColor = colorRdrDisconnected;
        }

        public void Transmit(ActionTaken act, params string[] pars)
        {
            ClearEveryThing();
            switch (act)
            {
                case ActionTaken.AutoToppedUp:
                    {
                        lblToppedUp.Show();
                        txtToppedUp.Show();

                        int amt = Convert.ToInt32(pars[0]);
                        txtToppedUp.Text = (amt / 100).ToString();

                        UpdateFields(GetLogicalMedia(pars[1]));

                        break;
                    }
                case ActionTaken.AlreadyBlocked_ie_BlackListed:
                    ShowPassageNotAllowedIcon();
                    break;
                case ActionTaken.CheckInPermitted:
                    {
                        ShowPassageAllowedIcon();

                        UpdateFields(GetLogicalMedia(pars[0]));

                        break;
                    }                
                case ActionTaken.CheckInNotPermitted:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));
                    SetErrorField(pars[0]);
                    break;
                case ActionTaken.CheckInNotPermitted_RejectCodePutByMe:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));
                    break;
                case ActionTaken.CheckOutPermitted:
                    ShowPassageAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[0]));
                    break;
                case ActionTaken.CheckOutNotPermitted_RejectCodePutByMe:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));
                    break;
                case ActionTaken.CheckOutNotPermitted:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));
                    SetErrorField(pars[0]);
                    break;
            }
        }

        private void SetErrorField(string p)
        {
            txtError.Show();

            int code = Convert.ToInt32(p);
            txtError.Text = String.Format("({0}){1}", code, ((TTErrorTypes)code).ToString());
        }

        private LogicalMedia GetLogicalMedia(string p)
        {
            LogicalMedia logMedia = new LogicalMedia();
            logMedia.Initialisation(p);
            return logMedia;
        }

        private void ClearEveryThing()
        {
            HidePassageIcon();
            txtRejectCode.Text = "";
            txtBalance.Text = "";
            txtToppedUp.Text = "";
            txtError.Text = "";

            lblToppedUp.Hide();
            txtToppedUp.Hide();            

            panelRejectCode.Hide();
        }

        private void ShowPassageAllowedIcon()
        {
            imgIcon.Show();
            imgIcon.Image = Image.FromFile(@"d:\junk\right.png");
        }

        private void ShowPassageNotAllowedIcon()
        {
            imgIcon.Show();
            imgIcon.Image = Image.FromFile(@"d:\junk\wrong.png");
        }

        private void HidePassageIcon()
        {
            imgIcon.Hide();
        }

        private void UpdateFields(LogicalMedia logMedia)
        {
            SetPurseBalance(logMedia);
            SetRejectCode(logMedia);
        }

        private void SetRejectCode(LogicalMedia logMedia)
        {
            int rejectCode = logMedia.Application.Validation.RejectCode;
            if (rejectCode == 0)
                panelRejectCode.Hide();
            else
            {
                panelRejectCode.Show();
                txtRejectCode.Text = rejectCode.ToString();
            }
        }

        private void SetPurseBalance(LogicalMedia logMedia)
        {
            txtBalance.Text = ((logMedia.Purse.TPurse.Balance) / 100).ToString();
        }

        Color colorRdrConnectedWithNoMedia = Color.FromArgb(192, 255, 192);
        Color colorRdrDisconnected = Color.FromArgb(255, 192, 192);
        internal void ReaderConnected()
        {
            this.BackColor = colorRdrConnectedWithNoMedia;
            ClearEveryThing();
        }

        internal void ReaderDisconnected()
        {
            this.BackColor = colorRdrDisconnected;
            ClearEveryThing();
        }

        internal void MediaProduced()
        {
            this.BackColor = Color.FromArgb(255, 255, 192);
        }

        internal void MediaRemoved()
        {
            this.BackColor = colorRdrConnectedWithNoMedia;
            ClearEveryThing();
        }
    }
}
