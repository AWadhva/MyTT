using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckOut.Fam010
{
    static class AllModes
    {
        static public TTErrorTypes VerifyMac(LogicalMedia logMedia)
        {
            return Rules.Common.EF_Token_VerifyMac(logMedia);
        }        
    }
}