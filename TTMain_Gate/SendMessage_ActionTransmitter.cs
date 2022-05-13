using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.Gate.MediaTreatment;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules.Gate
{
    public class SendMessage_ActionTransmitter : ITransmitter
    {
        public SendMessage_ActionTransmitter()
        {
            Communication.AddEventsToExternal("ActionTaken", "MMIChannel");
        }

        #region IActionTransmitter Members

        public void MediaTreated(int rdrMnemonic, ActionTaken act, params string[] pars)
        {
            string []p = new string[pars.Length + 1];
            
            p[0] = rdrMnemonic.ToString();
            Array.Copy(pars, 0, p, 1, pars.Length);
            Communication.SendMessage("", "", "ActionTaken", p);            
        }

        public void ReaderConnected(int rdrMnemonic)
        {
            
        }

        public void ReaderDisconnected(int rdrMnemonic)
        {
            
        }

        public void MediaProduced(int rdrMnemonic)
        {
            
        }

        public void MediaRemoved(int rdrMnemonic)
        {
            
        }

        public void AgentCardTreated(int rdrMnemonic, AgentCardAction act, params string[] pars)
        {            
        }

        #endregion
    }
}
