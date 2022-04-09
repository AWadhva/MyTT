using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckOut
{
    static class AllFamilies
    {
        // TODO: It depends upon the interpretation of open product. Below implementation is according to: Allow closed products only to be refunded, then below implementation is good
        // But if it implies: "allow only Open products be issued. If already issued, let it work", then this implementation is wrong.
        static public TTErrorTypes CheckForOpenFareProduct(LogicalMedia logMedia)
        {
            return CommonRules.IsFareProductOpen(logMedia);
        }
    }
}