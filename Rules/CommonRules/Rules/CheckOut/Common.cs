using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckOut
{
    static class Common
    {
        public static TTErrorTypes SaleStationIsSameAsThisOne(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.Token)
                if (CommonRules.GetVirtualSiteId(logMedia.Application.LocalLastAddValue.LocationRead) != CommonRules.GetVirtualSiteId(SharedData.StationNumber))
                    return TTErrorTypes.NotSameStation;

            return TTErrorTypes.NoError;
        }

        public static TTErrorTypes CheckEntryExitBit(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (logMedia.Application.Validation.EntryExitBitRead == Validation.TypeValues.Exit)
                    return TTErrorTypes.ExitNotDone;
            return TTErrorTypes.NoError;
        }
    }
}
