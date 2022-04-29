using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public class SendMessage_ActionTransmitter : ITransmitter
    {
        public SendMessage_ActionTransmitter()
        {
            Communication.AddEventsToExternal("ActionTaken", "MMIChannel");
        }

        #region IActionTransmitter Members

        public void Transmit(int rdrMnemonic, ActionTaken act, params string[] pars)
        {
            string []p = new string[pars.Length + 1];
            
            p[0] = rdrMnemonic.ToString();
            Array.Copy(pars, 0, p, 1, pars.Length);
            
            Communication.SendMessage("", "", "ActionTaken", p);
        }

        #endregion
    }
}
