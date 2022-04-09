using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam060
{
    static class AllModes
    {
        public static TTErrorTypes CheckCSCIsIssued(LogicalMedia logMedia)
        {
            return Rules.Common.CheckCSCIsIssued(logMedia);
        }

        public static TTErrorTypes EF_CSCC_ControlRejectCode(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;
            switch (logMedia.Application.Validation.RejectCodeRead)
            {
                case (short)TTErrorCodeOnMedia.AmountTooLow:
                case (short)TTErrorCodeOnMedia.BlackListTicket:
                case (short)TTErrorCodeOnMedia.TicketNotStillValid:
                case (short)TTErrorCodeOnMedia.OutOfDate:
                case (short)TTErrorCodeOnMedia.NoEntryFound:
                case (short)TTErrorCodeOnMedia.ExitNotDone:
                case (short)TTErrorCodeOnMedia.NoAuthorizedEntry:
                case (short)TTErrorCodeOnMedia.NotSaleStation:
                case (short)TTErrorCodeOnMedia.DelayAfterSaleExceeded:
                case (short)TTErrorCodeOnMedia.ExcessTripTime:
                    validation.RejectCode = (short)TTErrorCodeOnMedia.NoError;
                    return TTErrorTypes.NoError;
                case (short)TTErrorCodeOnMedia.ExitMismatch:
                case (short)TTErrorCodeOnMedia.RequiredExit:
                    if (SharedData.StationNumber == validation.LocationRead)
                    {
                        validation.RejectCode = (short)TTErrorCodeOnMedia.NoError;
                        return TTErrorTypes.NoError;
                    }
                    else
                        return TTErrorTypes.ExitMismatch;
            }
            return TTErrorTypes.NoError;
        }
    }
}
