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

        public void Transmit(int rdrMnemonic, ActionTaken act, params string[] pars)
        {
            userControlRW1.Transmit(act, pars);
        }

        #endregion
    }
}
