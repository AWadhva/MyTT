using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules
{
    static class Common
    {
        static public TTErrorTypes EF_Token_VerifyMac(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.Token)
                if (!ValidationRules.macCalculator.VerfiyMac(logMedia))
                    return TTErrorTypes.MACError;
            
            return TTErrorTypes.NoError;
        }

        public static TTErrorTypes CheckTokenIsIssued(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.Token)
                if (logMedia.Application.TransportApplication.StatusRead != TransportApplication.StatusValues.Issued)
                    return TTErrorTypes.TokenNotIssued; // TODO: in old gate, it was StatusError
            
            return TTErrorTypes.NoError;
        }

        public static TTErrorTypes CheckCSCIsIssued(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (logMedia.Media.Status != Media.StatusValues.Issued)
                    return TTErrorTypes.StatusError;

            return TTErrorTypes.NoError;
        }

        public static TTErrorTypes TestFlagIsCompatibleWithEqptMode(LogicalMedia logMedia)
        {
            bool bTestTicket = logMedia.Media.Test;

            if ((bTestTicket && SharedData._agentShift == null)
                || (!bTestTicket && SharedData._agentShift != null))
                return TTErrorTypes.IncompatibleValueOfTestFlag;
            
            return TTErrorTypes.NoError;
        }

        internal static TTErrorTypes CheckTravelTimeIsNotExceeded(LogicalMedia logMedia)
        {
            var exitAttemptTime = DateTime.Now;
            var entryTime = logMedia.Application.Validation.LastTransactionDateTimeRead;

            if (exitAttemptTime - entryTime > CommonRules.EF_EOD_GetMaxPaidTime(logMedia.Application.Validation.LocationRead))
            {
                CommonRules.SetRejectCode(logMedia, TTErrorCodeOnMedia.ExcessTripTime);
                return TTErrorTypes.MaxTravelTimeExceeded;
            }
            return TTErrorTypes.NoError;
        }
    }
}
