using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam010
{
    public static class EEO
    {
        static public TTErrorTypes CheckTokenIsIssued(LogicalMedia logMedia)
        {
            return Rules.Common.CheckTokenIsIssued(logMedia);
        }
    }
}
