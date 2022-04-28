using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public class SendMessage_ActionTransmitter : IActionTransmitter
    {
        public SendMessage_ActionTransmitter()
        {
            Communication.AddEventsToExternal("ActionTaken", "MMIChannel");
        }

        #region IActionTransmitter Members

        public void Transmit(ActionTaken act, params string[] pars)
        {
            Communication.SendMessage("", "", "ActionTaken", pars);
        }

        #endregion
    }
}
