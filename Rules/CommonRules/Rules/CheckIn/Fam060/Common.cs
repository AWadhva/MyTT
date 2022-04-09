using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam060
{
    static class Common
    {
        public static TTErrorTypes PurseValueIsAboveThreshold(LogicalMedia logMedia)
        {
            int purseVal = logMedia.Purse.TPurse.BalanceRead;
            if (purseVal < FareParameters.MinimumExitValue)
                return TTErrorTypes.AmountTooLow;
            
            return TTErrorTypes.NoError;
        }
    }
}