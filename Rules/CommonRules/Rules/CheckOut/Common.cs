using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckOut
{
    static class Common
    {
        internal static TTErrorTypes SaleStationIsSameAsThisOne(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.Token)
                if (CommonRules.GetVirtualSiteId(logMedia.Application.LocalLastAddValue.LocationRead) != CommonRules.GetVirtualSiteId(SharedData.StationNumber))
                    return TTErrorTypes.NotSameStation;

            return TTErrorTypes.NoError;
        }

        internal static TTErrorTypes CheckEntryExitBit(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (logMedia.Application.Validation.EntryExitBitRead == Validation.TypeValues.Exit)
                    return TTErrorTypes.ExitMismatch;
            return TTErrorTypes.NoError;
        }

        internal static TTErrorTypes ControlRejectCode_NormalMode(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;
            if (validation.RejectCodeRead == 0)
                return TTErrorTypes.NoError;

            switch (validation.RejectCodeRead)
            {
                case (short)TTErrorCodeOnMedia.RequiredExit:
                    if (SharedData.StationNumber == validation.LocationRead)
                    {
                        validation.RejectCode = (short)TTErrorCodeOnMedia.NoError;
                        return TTErrorTypes.NoError;
                    }
                    else
                    {
                        validation.RejectCode = (short)TTErrorCodeOnMedia.ExitMismatch;
                        return TTErrorTypes.SomeRejectCode;
                    }
                default:
                    return TTErrorTypes.SomeRejectCode;
            }
        }

        internal static TTErrorTypes ControlRejectCode_EEO(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;
            if (validation.RejectCodeRead == 0)
                return TTErrorTypes.NoError;

            switch (validation.RejectCodeRead)
            {
                case (short)TTErrorCodeOnMedia.RequiredExit:
                    if (SharedData.StationNumber == validation.LocationRead)
                    {
                        validation.RejectCode = (short)TTErrorCodeOnMedia.NoError;
                        return TTErrorTypes.NoError;
                    }
                    else
                    {
                        validation.RejectCode = (short)TTErrorCodeOnMedia.ExitMismatch;
                        return TTErrorTypes.SomeRejectCode;
                    }
                case (short)TTErrorCodeOnMedia.NoEntryFound:
                case (short)TTErrorCodeOnMedia.ExitNotDone:
                    validation.RejectCode = (short)TTErrorCodeOnMedia.NoError;
                    return TTErrorTypes.NoError;
                default:
                    return TTErrorTypes.SomeRejectCode;
            }
        }

        readonly static List<OperationTypeValues> exitCodes = new List<OperationTypeValues>{
            OperationTypeValues.ValueDeductedInExit,
            OperationTypeValues.PointsOrRidesDeductedInExit,
            OperationTypeValues.PeriodicTicketExit,
            OperationTypeValues.NoValueDeductedInExit,
            OperationTypeValues.LoyaltyPointsUsedInExit
        };

        internal static TTErrorTypes CheckForRecovery(LogicalMedia logMedia)
        {
            var now = DateTime.Now;
            var historyRec = logMedia.Purse.History.Transaction(0);
            if (historyRec.LocationRead == SharedData.StationNumber
                && exitCodes.Contains(historyRec.OperationTypeRead)
                && (historyRec.DateTimeRead < now
                        ? now - historyRec.DateTimeRead < new TimeSpan(0, 3, 0) 
                        : historyRec.DateTimeRead - now < new TimeSpan(0, 0, 1))
                && logMedia.Application.Validation.EntryExitBitRead == Validation.TypeValues.Entry
                && logMedia.Purse.TPurse.BalanceRead == historyRec.NewBalanceRead)
                return TTErrorTypes.RecoveryNeeded;

            return TTErrorTypes.NoError;
        }
    }
}