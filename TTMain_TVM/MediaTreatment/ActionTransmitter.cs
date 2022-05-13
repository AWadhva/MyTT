using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment.TVM
{
    public class ActionTransmitter
    {
        Action<ActionTaken, string[]> transmitter;

        public ActionTransmitter(Action<ActionTaken, string[]> transmitter_)
        {
            transmitter = transmitter_;
        }

        internal void FailedWrite()
        {
            transmitter(ActionTaken.ProblemWhileRW, new string[]{});
        }

        internal void Blacklisted(LogicalMedia logMedia)
        {
            transmitter(ActionTaken.Blocked_ie_BlackListedByMe, new string[]{logMedia.ToXMLString()});
        }

        internal void FailedRead()
        {
            transmitter(ActionTaken.ProblemWhileRW, new string[]{});
        }

        internal void GoodAgentCard(LogicalMedia logMedia)
        {
            throw new NotImplementedException();
        }
    }
}