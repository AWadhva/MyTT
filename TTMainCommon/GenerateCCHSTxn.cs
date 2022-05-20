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
        public static string PerformAutoTopup(SmartFunctions sf, LogicalMedia logMedia)
        {
            FldsCSCPeformAddValueViaBankTopup txn = new FldsCSCPeformAddValueViaBankTopup();
            txn.addValAmt = logMedia.Purse.TPurse.Balance - logMedia.Purse.TPurse.BalanceRead;
            txn.depositInCents = logMedia.Application.TransportApplication.DepositRead;
            txn.purseRemainingVal = logMedia.Purse.TPurse.Balance;

            string cchsStr = sf.GetTDforCCHSGen(logMedia, TransactionType.TPurseBankTopupReload, txn, false, logMedia.Application.TransportApplication.Test);

            return cchsStr;                    
        }

        public static string CheckIn_PurseCard(SmartFunctions sf, LogicalMedia logMedia, FareMode mode)
        {
            FldsCSCTrainEntry txn = new FldsCSCTrainEntry();
            txn.fareIndicator = ConvertFromFareMode(mode);
            txn.purseRemainingValue = logMedia.Purse.TPurse.Balance;
            txn.TransferNumber = 0;
            txn.transferProvider = ParticipantID_t.DMRC;
            txn.transit1RemainingVal = 0;
            txn.transit2RemainingVal = 0;
            txn.TravelScheme = 2;

            string cchsStr = sf.GetTDforCCHSGen(logMedia, TransactionType.MetroCheckInWithTPurse, txn, false, logMedia.Application.TransportApplication.Test);

            return cchsStr;            
        }

        public static string CheckIn_NonPurseCard(SmartFunctions sf, LogicalMedia logMedia, FareMode mode)
        {
            FldsCSCTrainPassEntry txn = new FldsCSCTrainPassEntry();
            txn.fareIndicator = ConvertFromFareMode(mode);
            txn.TransferNumber = 0;
            txn.transferProvider = ParticipantID_t.DMRC;
            txn.TravelScheme = 0;
            txn.tripCount = 0;

            string cchsStr = sf.GetTDforCCHSGen(logMedia, TransactionType.MetroCheckInWithPass, txn, false, logMedia.Application.TransportApplication.Test);

            return cchsStr;
        }

        public static string CheckOut_PurseCard(SmartFunctions sf, LogicalMedia logMedia, FareMode mode)
        {
            FldsCSCTrainFareDeduction txn = new FldsCSCTrainFareDeduction();
            txn.discountAmount = 0;
            txn.discountReason = DiscountReason_t.NoDiscount;
            txn.entryStation = 0;
            txn.entryTime = new DateTime();
            txn.fareCode = 0;
            txn.fareIndicator = ConvertFromFareMode(mode);
            txn.freeTravelValue = 0;
            txn.purseRemainingValue = 0;
            txn.rebateAmount = 0;
            txn.TransferNumber = 0;
            txn.transferProvider = ParticipantID_t.DMRC;
            txn.transit1RemainingVal = 0;
            txn.transit2RemainingVal = 0;
            txn.TravelScheme = 0;
            txn.txnValue = 0;
            
            string cchsStr = sf.GetTDforCCHSGen(logMedia, TransactionType.MetroCheckOutWithTPurse, txn, false, logMedia.Application.TransportApplication.Test);

            return cchsStr;
        }

        public static string CheckOut_NonPurseCard(SmartFunctions sf, LogicalMedia logMedia, FareMode mode)
        {
            FldsCSCTrainRideDeduction txn = new FldsCSCTrainRideDeduction();

            txn.entryStation = 0;
            txn.entryTime = new DateTime();
            txn.fareCode = 0;
            txn.fareIndicator = ConvertFromFareMode(mode);
            txn.rebateAmount = 0;
            txn.TransferNumber = 0;
            txn.transferProvider = ParticipantID_t.DMRC;
            txn.TravelScheme = 0;
            txn.tripCount = 0;
            txn.txnValue = 0;

            string cchsStr = sf.GetTDforCCHSGen(logMedia, TransactionType.MetroCheckOutWithPass, txn, false, logMedia.Application.TransportApplication.Test);
            return cchsStr;
        }

        private static FareIndicator_t ConvertFromFareMode(FareMode mode)
        {
            switch (mode)
            {
                case FareMode.EEO:
                    return FareIndicator_t.EEO;
                case FareMode.Incident:                    
                    return FareIndicator_t.Incident;
                case FareMode.TMO:
                    return FareIndicator_t.TMO;
                default:
                    return FareIndicator_t.Normal;
            }
        }        
    }
}