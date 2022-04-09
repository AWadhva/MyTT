using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckOut.Fam060
{
    static class TMO
    {
        public static TTErrorTypes CheckEntryExitBit(LogicalMedia logMedia)
        {
            return Rules.CheckOut.Common.CheckEntryExitBit(logMedia);
        }

        public static TTErrorTypes TestFlagIsCompatibleWithEqptMode(LogicalMedia logMedia)
        {
            return Rules.Common.TestFlagIsCompatibleWithEqptMode(logMedia);
        }        
    }
}
