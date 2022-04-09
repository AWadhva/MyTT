using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam040
{
    static class AllModes
    {
        public static TTErrorTypes VerifyMac(LogicalMedia logMedia)
        {
            return Rules.Common.EF_Token_VerifyMac(logMedia);
        }
    }
}