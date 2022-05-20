using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Common;

namespace TTMainCommon
{
    public static class GenerateCCHSTxn
    {
        public static string PerformAutoTopup(LogicalMedia logMedia, SmartFunctions sf)
        {
            FldsCSCPeformAddValueViaBankTopup txn = new FldsCSCPeformAddValueViaBankTopup();
            txn.addValAmt = logMedia.Purse.TPurse.Balance - logMedia.Purse.TPurse.BalanceRead;
            txn.depositInCents = logMedia.Application.TransportApplication.DepositRead;
            txn.purseRemainingVal = logMedia.Purse.TPurse.Balance;

            string cchsStr = sf.GetTDforCCHSGen(logMedia, TransactionType.TPurseBankTopupReload, txn, false, logMedia.Application.TransportApplication.Test);

            return cchsStr;                    
        }
    }
}
