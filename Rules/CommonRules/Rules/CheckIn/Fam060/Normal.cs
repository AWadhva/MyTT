using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules.CheckIn.Fam060
{
    static class Normal
    {
        public static TTErrorTypes CheckEntryExitBit(LogicalMedia logMedia)
        {
            return Rules.CheckIn.Common.CheckEntryExitBit(logMedia);
        }

        public static TTErrorTypes TestFlagIsCompatibleWithEqptMode(LogicalMedia logMedia)
        {
            return Rules.Common.TestFlagIsCompatibleWithEqptMode(logMedia);
        }

        public static TTErrorTypes EF_CSCC_ControlRejectCode(LogicalMedia logMedia)
        {
            return Rules.CheckIn.Fam060.Common.EF_CSCC_ControlRejectCode(logMedia);
        }

        public static TTErrorTypes MinimumPurseValueForCheckin(LogicalMedia logMedia)
        {
            var prod = logMedia.Application.Products.Product(0);            
            var purse = logMedia.Purse;
            if (purse.AutoReload.StatusRead == AutoReload.StatusValues.Enabled
                && purse.TPurse.BalanceRead < purse.AutoReload.ThresholdRead
                && FareProductSpecs.GetInstance().IsAddValueSupported(prod.TypeRead))
                return TTErrorTypes.NeedToPerformAutoTopup;
            else
                return Rules.CheckIn.Fam060.Common.PurseValueIsAboveThreshold(logMedia);
        }
    }
}