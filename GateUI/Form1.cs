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
using IFS2.Equipment.Parameters;

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

            BasicParameterFile.Register(new MaxiTravelTime());
        }

        public class Combobox_FareMode_Item
        {
            public string Text { get; set; }
            public FareMode Value { get; set; }

            public Combobox_FareMode_Item(FareMode value, string text)
            {
                Text = text;
                Value = value;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            app = new IFS2.Equipment.TicketingRules.Gate.Application(this, this);

            comboFareModes.Items.Add(new Combobox_FareMode_Item(FareMode.Normal, FareMode.Normal.ToString()));
            comboFareModes.Items.Add(new Combobox_FareMode_Item(FareMode.EEO, FareMode.EEO.ToString()));
            comboFareModes.Items.Add(new Combobox_FareMode_Item(FareMode.TMO, FareMode.TMO.ToString()));
            comboFareModes.Items.Add(new Combobox_FareMode_Item(FareMode.Incident, FareMode.Incident.ToString()));

            comboFareModes.SelectedIndex = 0;
            txtSiteId.Text = "15";
        }

        IFS2.Equipment.TicketingRules.Gate.Application app;

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

        public void MediaTreated(int rdrMnemonic, ActionTaken act, params string[] pars)
        {
            if (rdrMnemonic == 1)
                userControlRW1.Transmit(act, pars);
            else
                userControlRW2.Transmit(act, pars);
        }

        UserControlRW GetUcForRdr(int rdrMnemonic)
        {
            if (rdrMnemonic == 1)
                return userControlRW1;
            else
                return userControlRW2;
        }

        #endregion

        #region ITransmitter Members


        public void ReaderConnected(int rdrMnemonic)
        {
            GetUcForRdr(rdrMnemonic).ReaderConnected();
        }

        public void ReaderDisconnected(int rdrMnemonic)
        {
            GetUcForRdr(rdrMnemonic).ReaderDisconnected();
        }

        public void MediaProduced(int rdrMnemonic)
        {
            GetUcForRdr(rdrMnemonic).MediaProduced();
        }

        public void MediaRemoved(int rdrMnemonic)
        {
            GetUcForRdr(rdrMnemonic).MediaRemoved();
        }

        #endregion        

        private void rd1CheckIn_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;
            if (!btn.Focused)
                return;

            rd2CheckOut.Checked = true;
            app.SetOperatingMode(1, true);
        }

        private void rd1CheckOut_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;
            if (!btn.Focused)
                return;

            rd2CheckIn.Checked = true;
            app.SetOperatingMode(1, false);
        }

        private void rd2CheckIn_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;
            if (!btn.Focused)
                return;

            rd1CheckOut.Checked = true;
            app.SetOperatingMode(2, true);
        }

        private void rd2CheckOut_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton btn = (RadioButton)sender;
            if (!btn.Focused)
                return;

            rd1CheckIn.Checked = true;
            app.SetOperatingMode(2, false);
        }

        private void btnSetSiteId_Click(object sender, EventArgs e)
        {
            
        }

        private void txtSiteId_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int siteId = Convert.ToInt32(txtSiteId.Text);
                app.SetStationNumber(siteId);                
            }
            catch
            { }
        }

        private void comboFareModes_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = comboFareModes.SelectedIndex;

            FareMode mode;
            if (idx == 0)
                mode = FareMode.Normal;
            else if (idx == 1)
                mode = FareMode.EEO;
            else if (idx == 2)
                mode = FareMode.TMO;
            else if (idx == 3)
                mode = FareMode.Incident;
            else
                return;

            app.SetFareMode(mode);            
        }        
    }
}
