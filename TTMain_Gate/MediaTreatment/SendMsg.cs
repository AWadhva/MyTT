using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    static class Transmit
    {
        public static IActionTransmitter transmitter;

        internal static void AutoTopup(int amt, LogicalMedia logMedia)
        {
            transmitter.Transmit(ActionTaken.AutoToppedUp, amt.ToString(), logMedia.ToXMLString());
        }

        internal static void SuccessfulCheckin(LogicalMedia logMedia)
        {
            transmitter.Transmit(ActionTaken.CheckInPermitted, logMedia.ToXMLString());
        }

        internal static void SuccessfulCheckout(LogicalMedia logMedia, 
            int purseAmtDeducted // should be set to zero if the product has no purse e.g. for family 80
            )
        {
            transmitter.Transmit(ActionTaken.CheckOutPermitted, logMedia.ToXMLString());
        }

        internal static void FailedWrite()
        {
            transmitter.Transmit(ActionTaken.ProblemWhileRW);
        }

        internal static void Blacklisted(LogicalMedia logMedia)
        {
            transmitter.Transmit(ActionTaken.Blocked_ie_BlackListedByMe, logMedia.ToXMLString());
        }

        internal static void CheckInNotPermitted_NoRejectCodeWrittenByMe(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter.Transmit(ActionTaken.CheckInNotPermitted_RejectCodePutByMe, ((int)validationResult).ToString(), logMedia.ToXMLString());
        }

        internal static void CheckInBlocked_And_RejectCodeWrittenByMe(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter.Transmit(ActionTaken.CheckInNotPermitted_RejectCodePutByMe, ((int)validationResult).ToString(), logMedia.ToXMLString());
        }

        internal static void FailedRead()
        {
            transmitter.Transmit(ActionTaken.ProblemWhileRW);
        }
    }
}
