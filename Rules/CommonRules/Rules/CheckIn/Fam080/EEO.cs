using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam080
{
    static class EEO
    {
        public static TTErrorTypes TestFlagIsCompatibleWithEqptMode(LogicalMedia logMedia)
        {
            return Rules.Common.TestFlagIsCompatibleWithEqptMode(logMedia);
        }

    }
}
