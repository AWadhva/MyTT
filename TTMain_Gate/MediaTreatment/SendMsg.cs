using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    static class Transmit
    {
        public static Action<ActionTaken, string[]> transmitter;

        internal static void AutoTopup(int amt, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.AutoToppedUp, new string[]{amt.ToString(), logMedia.ToXMLString()});
        }

        internal static void SuccessfulCheckin(LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInPermitted, new string[]{logMedia.ToXMLString()});
        }

        internal static void SuccessfulCheckout(LogicalMedia logMedia, 
            int purseAmtDeducted // should be set to zero if the product has no purse e.g. for family 80
            )
        {
            transmitter(ActionTaken.CheckOutPermitted, new string[]{logMedia.ToXMLString()});
        }

        internal static void FailedWrite()
        {
            transmitter(ActionTaken.ProblemWhileRW, new string[]{});
        }

        internal static void Blacklisted(LogicalMedia logMedia)
        {
            transmitter(ActionTaken.Blocked_ie_BlackListedByMe, new string[]{logMedia.ToXMLString()});
        }

        internal static void CheckInNotPermitted_NoRejectCodeWrittenByMe(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInNotPermitted_RejectCodeAlreadyPresent, new string[]{((int)validationResult).ToString(), logMedia.ToXMLString()});
        }

        internal static void CheckInBlocked_And_RejectCodeWrittenByMe(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInNotPermitted_RejectCodePutByMe, new string[]{((int)validationResult).ToString(), logMedia.ToXMLString()});
        }

        internal static void CheckInBlocked_ForSomethingElse(TTErrorTypes validationResult, LogicalMedia logMedia)
        {
            transmitter(ActionTaken.CheckInNotPermitted_LetsFinalizeTheseCodesLater, 
                new string[]{
                ((int)validationResult).ToString(), logMedia.ToXMLString()});
        }

        internal static void FailedRead()
        {
            transmitter(ActionTaken.ProblemWhileRW, new string[]{});
        }
    }
}
