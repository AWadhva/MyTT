using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public interface IActionTransmitter
    {
        void Transmit(ActionTaken act, params string[] pars);
    }
}