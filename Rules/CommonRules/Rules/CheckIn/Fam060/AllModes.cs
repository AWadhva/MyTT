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
    }
}
