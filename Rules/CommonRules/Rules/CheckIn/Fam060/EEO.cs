using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam060
{
    static class EEO
    {
        public static TTErrorTypes TestFlagIsCompatibleWithEqptMode(LogicalMedia logMedia)
        {
            return Rules.Common.TestFlagIsCompatibleWithEqptMode(logMedia);
        }

        public static TTErrorTypes PurseValueIsAboveThreshold(LogicalMedia logMedia)
        {
            return Rules.CheckIn.Fam060.Common.PurseValueIsAboveThreshold(logMedia);
        }

        public static TTErrorTypes EF_CSCC_ControlRejectCode(LogicalMedia logMedia)
        {
            return Rules.CheckIn.Fam060.Common.EF_CSCC_ControlRejectCode(logMedia);
        }
    }
}
