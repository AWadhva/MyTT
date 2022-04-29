using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;
using IFS2.Equipment.TicketingRules;

namespace GateUI
{
    public partial class Form1 : Form, ISyncContext, ITransmitter
    {
        public Form1()
        {
            InitializeComponent();
            ParametersRelated();
        }

        private void ParametersRelated()
        {
            FareParameters.Start();
            TopologyParameters.Start();
            MediaDenyList.Start();
            EquipmentDenyList.Start();
            RangeDenyList.Start();
            AgentList.Start();
            OverallParameters.Start();
            TVMEquipmentParameters.Start();
            TicketsSaleParameters.Start();

            AgentList.Initialise();
            MediaDenyList.Initialise();
            RangeDenyList.Initialise();
            EquipmentDenyList.Initialise();
            TopologyParameters.Initialise();
            OverallParameters.Initialise();

            FareProductSpecs.Load(false);
            SharedData._fpSpecsRepository = FareProductSpecs.GetInstance();
            FareParameters.Initialise();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            IFS2.Equipment.TicketingRules.Gate.Application app = new IFS2.Equipment.TicketingRules.Gate.Application(this, this);
        }

        #region ISyncContext Members

        public void Message(Action act)
        {
            this.BeginInvoke(act);
        }

        public void Message(Action<string[]> act, params string[] pars)
        {
            this.BeginInvoke(act, pars);
        }

        #endregion

        #region IActionTransmitter Members

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
                case ActionTaken.CheckInNotPermitted_LetsFinalizeTheseCodesLater:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));

                    break;
                case ActionTaken.CheckInNotPermitted_RejectCodeAlreadyPresent:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));
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
                case ActionTaken.CheckOutNotPermitted_RejectCodeAlreadyPresent:
                    ShowPassageNotAllowedIcon();
                    UpdateFields(GetLogicalMedia(pars[1]));
                    break;
            }
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

        #endregion        
    
        #region IActionTransmitter Members

        public void Transmit(int rdrMnemonic, ActionTaken act, params string[] pars)
        {
            Transmit(act, pars); // TODO: correct it.
        }

        #endregion
    }
}
