using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckOut
{
    static class AllFamilies
    {
        static public TTErrorTypes CheckForOpenFareProduct(LogicalMedia logMedia)
        {
            return CommonRules.IsFareProductOpen(logMedia);
        }

        public static TTErrorTypes CheckProductEndOfValidity(LogicalMedia logMedia)
        {
            return ValidationRules.CheckProductEndOfValidity(logMedia);
        }
    }
}