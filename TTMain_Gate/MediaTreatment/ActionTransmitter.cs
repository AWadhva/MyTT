using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Gate.MediaTreatment
{
    public class ActionTransmitter
    {
        Action<ActionTaken, string[]> transmitter;

        public ActionTransmitter(Action<ActionTaken, string[]> transmitter_)
        {
            transmitter = transmitter_;
        }

        internal void AutoTopup(int amt, LogicalMedia logMedia, string cchsStr)
        {
            transmitter(ActionTaken.AutoToppedUp, new string[]{amt.ToString(), logMedia.ToXMLString()});
        }

        internal void CheckInPermitted(LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInPermitted, new string[]{logMedia.ToXMLString()});
        }

        internal void CheckOutPermitted(LogicalMedia logMedia 
            //, int purseAmtDeducted // should be set to zero if the product has no purse e.g. for family 80
            )
        {
            transmitter(ActionTaken.CheckOutPermitted, new string[] { logMedia.ToXMLString(), (logMedia.Purse.TPurse.BalanceRead - logMedia.Purse.TPurse.Balance).ToString() });
        }

        internal void FailedWrite()
        {
            transmitter(ActionTaken.ProblemWhileRW, new string[]{});
        }

        internal void Blacklisted(LogicalMedia logMedia)
        {
            transmitter(ActionTaken.Blocked_ie_BlackListedByMe, new string[]{logMedia.ToXMLString()});
        }

        internal void CheckInNotPermitted_And_RejectCodeWrittenByMe(short rejectCode, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInNotPermitted_RejectCodePutByMe, new string[] { ((int)rejectCode).ToString(), logMedia.ToXMLString() });
        }

        internal void CheckInNotPermitted(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInNotPermitted, 
                new string[]{
                ((int)validationResult).ToString(), logMedia.ToXMLString()});
        }

        internal void FailedRead()
        {
            transmitter(ActionTaken.ProblemWhileRW, new string[]{});
        }

        internal void CheckOutNotPermitted_And_RejectCodeWrittenByMe(short rejectCode, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckOutNotPermitted_RejectCodePutByMe, new string[] { rejectCode.ToString(), logMedia.ToXMLString() });
        }

        internal void CheckOutNotPermitted(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckOutNotPermitted, new string[] { ((int)validationResult).ToString(), logMedia.ToXMLString()});
        }
    }
}