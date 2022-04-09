// DM1::SequenceNumber needs to be updated only if the operation affects purse. Confirmed by observation on CS22 TOM/Gate softwares.
// DM2::Sale/Add value of course needs to be updated on Sale/Add value only.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public static class SalesRules
    {
        public static bool TokenSaleUpdate(LogicalMedia logMedia, int amount, int origin, int destination, short fareTier)
        {
            try
            {
                var lcav = logMedia.Application.LocalLastAddValue;
                var val = logMedia.Application.Validation;

                lcav.Amount = amount;
                DateTime now = DateTime.Now;
                lcav.DateTime = now;
                val.LastTransactionDateTime = now;
                lcav.EquipmentNumber = SharedData.EquipmentNumber;
                lcav.EquipmentType = SharedData.EquipmentType;
                lcav.Location = origin;
                val.Location = origin;
                val.EntryExitBit = Validation.TypeValues.Exit;
                lcav.ServiceProvider = SharedData.ServiceProvider;
                lcav.Destination = destination;
                lcav.FareTiers = fareTier;
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SalesRules_TokenSaleUpdate " + e.Message);
                return false;
            }
        }

        public static bool TokenSaleUpdateForPaidOrFreeExit(LogicalMedia logMedia, int amount, int siteId)
        {
            try
            {
                var lcav = logMedia.Application.LocalLastAddValue;
                var val = logMedia.Application.Validation;
                lcav.Amount = amount;
                DateTime now = DateTime.Now;
                lcav.DateTime = now;
                val.LastTransactionDateTime = now;
                lcav.EquipmentNumber = SharedData.EquipmentNumber;
                lcav.EquipmentType = SharedData.EquipmentType;
                lcav.Location = siteId;
                val.Location = siteId;
                val.EntryExitBit = Validation.TypeValues.Entry;

                lcav.ServiceProvider = SharedData.ServiceProvider;
                lcav.Destination = siteId;
                
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SalesRules_TokenSaleUpdate " + e.Message);
                return false;
            }
        }

        public static bool AddTrasactionHistoryRecord(LogicalMedia logMedia,OperationTypeValues mRideType,int amount)
        {           
            OneTransaction trans = new OneTransaction();
            try
            {
                trans.SequenceNumber = logMedia.Application.TransportApplication.SequenceNumberRead + 1;                
                trans.Amount = amount;
                trans.DateTime = DateTime.Now;
                trans.EquipmentNumber = SharedData.EquipmentNumber;
                trans.EquipmentType = SharedData.EquipmentType;
                trans.Location = SharedData.StationNumber;
                trans.NewBalance = logMedia.Purse.TPurse.Balance;
                trans.OperationType = mRideType;
                trans.ServiceProvider = SharedData.ServiceProvider;
                logMedia.Purse.History.List.Clear();//remove all records
                logMedia.Purse.History.Add(trans);
                Logging.Log(LogLevel.Verbose, "    trans.EquipmentNumber " + trans.EquipmentNumber);
                Logging.Log(LogLevel.Verbose, "    trans.EquipmentType " + trans.EquipmentType);
                Logging.Log(LogLevel.Verbose, "   trans.NewBalance  " + trans.NewBalance);
                Logging.Log(LogLevel.Verbose, "   trans.OperationType  " + trans.OperationType);

                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SalesRules_AddTrasactionHistoryRecord " + e.Message);
                return false;
            }
        }

        public static void AdjustmentUpdateForCSC(
            LogicalMedia logMedia,
            bool bSetDateTime,
            byte? entryExitBit,
            int? entryExitStationCode,
            int? purseValToDecrement,
            TTErrorCodeOnMedia rejectCode)
        {
            var val = logMedia.Application.Validation;
            if (entryExitBit != null)
            {
                if ((byte)entryExitBit == 0)
                    val.EntryExitBit = Validation.TypeValues.Exit;
                else if ((byte)entryExitBit == 1)
                    val.EntryExitBit = Validation.TypeValues.Entry;
            }
            if (entryExitStationCode != null)
                val.Location = (int)entryExitStationCode;
            if (bSetDateTime)
                val.LastTransactionDateTime = DateTime.Now;
            val.RejectCode = (short)rejectCode;
            if (purseValToDecrement != null)
            {                
                AddTrasactionHistoryRecord(logMedia,
                    OperationTypeValues.Penalty, // TODO: At the moment, it is arbitrary and not mentioned in document. It needs to be made official. In case it doesn't get approved, we can simply assign OperationTypeValues.Penalty to one of the existing values
                    (int)purseValToDecrement
                    );
                // Order is important as AddTrasactionHistoryRecord uses purse's balance, TransApp::SeqNumb
                logMedia.Purse.TPurse.Balance = logMedia.Purse.TPurse.BalanceRead - (int)purseValToDecrement;
                logMedia.Purse.TPurse.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
                logMedia.Application.TransportApplication.SequenceNumber = Math.Abs(logMedia.Purse.TPurse.SequenceNumberRead - 1);
            }
            logMedia.DESFireDelhiLayout.Reset();
        }

        public static bool PurseDeductionUpdate(LogicalMedia logMedia,int amount,PaymentMethods payment)
        {
            try
            {
                //if (increaseSequence)                
                //    logMedia.Purse.TPurse.SequenceNumber++;                
                //else               
                logMedia.Purse.TPurse.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
                
                logMedia.Application.TransportApplication.SequenceNumber = Math.Abs(logMedia.Purse.TPurse.SequenceNumberRead - 1);

                logMedia.Purse.TPurse.Balance = logMedia.Purse.TPurse.BalanceRead - amount;
                Logging.Log(LogLevel.Verbose, "logMedia.Purse.TPurse.Balance" + amount);

                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SalesRules_AddValueToUpdate " + e.Message);
                return false;
            }
        }

        public static bool AddValueUpdate(LogicalMedia logMedia,int amount,PaymentMethods payment//,bool increaseSequence
            )
        {            
            try
            {
                logMedia.Purse.TPurse.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
                logMedia.Application.TransportApplication.SequenceNumber = Math.Abs(logMedia.Purse.TPurse.SequenceNumberRead - 1);

                logMedia.Purse.TPurse.Balance = logMedia.Purse.TPurse.BalanceRead + amount;

                logMedia.Application.LocalLastAddValue.Amount = amount;
                logMedia.Application.LocalLastAddValue.DateTime = DateTime.Now;
                logMedia.Application.LocalLastAddValue.EquipmentNumber = SharedData.EquipmentNumber;
                logMedia.Application.LocalLastAddValue.EquipmentType = SharedData.EquipmentType;
                logMedia.Application.LocalLastAddValue.Location = SharedData.StationNumber;
                logMedia.Application.LocalLastAddValue.NewBalance = logMedia.Purse.TPurse.BalanceRead + amount;
                logMedia.Application.LocalLastAddValue.ServiceProvider = SharedData.ServiceProvider;
                logMedia.Application.LocalLastAddValue.SequenceNumber = Math.Abs(logMedia.Purse.TPurse.SequenceNumberRead - 1);
                if (payment==PaymentMethods.BankCard)
                    logMedia.Application.LocalLastAddValue.OperationType = LocalLastAddValue.OperationTypeValues.BankCard;
                else
                    logMedia.Application.LocalLastAddValue.OperationType = LocalLastAddValue.OperationTypeValues.Cash;

                logMedia.Purse.LastAddValue.Amount = amount;
                logMedia.Purse.LastAddValue.DateTime = DateTime.Now;
                logMedia.Purse.LastAddValue.EquipmentNumber = SharedData.EquipmentNumber;
                logMedia.Purse.LastAddValue.EquipmentType = SharedData.EquipmentType;
                switch (payment)
                {
                    case PaymentMethods.BankCard: // Done deliberatly
                    case PaymentMethods.Cash:
                        logMedia.Purse.LastAddValue.OperationType = LastAddValue.OperationTypeValues.Cash; //To be treated afterwards
                        break;
                    case PaymentMethods.BankTopup:
                        logMedia.Purse.LastAddValue.OperationType = LastAddValue.OperationTypeValues.BankTopUp;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                logMedia.Purse.LastAddValue.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
                logMedia.Purse.LastAddValue.ServiceProvider = SharedData.ServiceProvider;

                //logMedia.DESFireDelhiLayout.Reset();

                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SalesRules_AddValueToUpdate " + e.Message);
                return false;
            }
        }

        public static bool CSCIssueUpdate(
            LogicalMedia logMedia,
            short productType, 
            PaymentMethods payment, 
            bool bTestTicket,
            int txnValue,
            Customer.LanguageValues lng,
            bool bSellOnlyProduct)
        {
            try
            {
                Debug.Assert(SharedData._fpSpecsRepository.Initialized);
                Debug.Assert(SharedData._fpSpecsRepository.IsOpen(productType));
               
                #region Product
                if (!SharedData._fpSpecsRepository.Initialized)
                {
                    Logging.Log(LogLevel.Information, "fpSpecs is not initialized. Can't issue product ");
                    return false;
                }

                FareProductSpecs.OneProductSpecs fpSpecs = SharedData._fpSpecsRepository.GetSpecsFor(productType);

                if (fpSpecs != null && !fpSpecs._IsOpen)
                {
                    Logging.Log(LogLevel.Information, "Can't issue product of type " + productType);
                    return false;
                }

                OneProduct product = new OneProduct();
                product.Type = productType;
                product.StartOfValidity = DateTime.Now;
                
                var validityDeterminer = fpSpecs._Validity;
                DateTime endOfValidity = DateTime.Now;
                int validityVal = validityDeterminer.Val;

                switch (validityDeterminer.ticketValidityUnit)
                {
                    case FareProductSpecs.TicketValidityUnit.Sliding:
                        throw new NotImplementedException("Not implemented for FareProductSpecs.TicketValidityUnit.Sliding");
                    case FareProductSpecs.TicketValidityUnit.NoValidity:
                        throw new NotImplementedException("Not implemented for FareProductSpecs.TicketValidityUnit.NoValidity");
                    case FareProductSpecs.TicketValidityUnit.Week:
                        endOfValidity = endOfValidity.AddDays(7 * validityVal);
                        break;
                    case FareProductSpecs.TicketValidityUnit.Day:
                        endOfValidity = endOfValidity.AddDays(validityVal);
                        break;
                    case FareProductSpecs.TicketValidityUnit.Month:
                        endOfValidity = endOfValidity.AddMonths(validityVal);
                        break;
                    case FareProductSpecs.TicketValidityUnit.Year:
                        endOfValidity = endOfValidity.AddYears(validityVal);
                        break;
                }

                product.EndOfValidity = endOfValidity; // TODO: Take inspiration from EF_TOM_UpdateTicketValidityData, and keep this function common for all operations.

                byte duration;
                switch (validityDeterminer.ticketValidityUnit)
                {
                    case FareProductSpecs.TicketValidityUnit.Sliding:
                        throw new NotImplementedException("Not implemented for FareProductSpecs.TicketValidityUnit.Sliding");
                    default:
                        duration = 0;
                        break;
                }
                product.DurationValidity = duration;
                
                logMedia.Application.Products.Clear();
                logMedia.Application.Products.Add(product);
                #endregion

                long purseSeq;
                if (bSellOnlyProduct)
                    logMedia.Purse.TPurse.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
                else
                    logMedia.Purse.TPurse.SequenceNumber = 0;

                purseSeq = logMedia.Purse.TPurse.SequenceNumber;

                // DM1 Personalization can't be and need not be edited by TOM.

                #region TransportApplication

                var ta = logMedia.Application.TransportApplication;
                
                ta.SequenceNumber = purseSeq;

                ta.Blocked = false;
                ta.CustomerLink = TransportApplication.CustomerLinkValues.Unknown;//TODO
                if (!bSellOnlyProduct)
                    ta.Deposit = fpSpecs._Deposit;
                else
                {
                    // we don't override the value contained in ta.Deposit
                }
                ta.ExpiryDate = product.EndOfValidity; // See what values to keep for product's EOV and media's EOV
                ta.FormatVersion = 0; //TODO
                ta.InitialisationDate = DateTime.Now; // TODO: may be to remove time part
                ta.OperationalType = TransportApplication.OperationalTypeValues.Passenger;
                ta.Owner = 0; //TODO
                ta.ReasonOfBlocking = 0; //TODO
                ta.Status = TransportApplication.StatusValues.Issued;
                ta.Test = bTestTicket;

                #endregion

                #region LocalLastAddValue

                var lcav = logMedia.Application.LocalLastAddValue;

                lcav.Amount = txnValue;
                lcav.DateTime = DateTime.Now;
                lcav.EquipmentNumber = SharedData.EquipmentNumber;
                lcav.EquipmentType = SharedData.EquipmentType;
                lcav.Location = SharedData.StationNumber;
                lcav.ServiceProvider = SharedData.ServiceProvider;
                lcav.SequenceNumber = Math.Abs(purseSeq);
                if (!bSellOnlyProduct)
                    lcav.NewBalance = 0;
                else
                    lcav.NewBalance = logMedia.Purse.TPurse.BalanceRead;

                if (payment == PaymentMethods.BankCard)
                    lcav.OperationType = LocalLastAddValue.OperationTypeValues.BankCard;
                else
                    lcav.OperationType = LocalLastAddValue.OperationTypeValues.Cash;
                #endregion

                #region Validation

                var validation = logMedia.Application.Validation;
                validation.DateOfFirstTransaction = new DateTime(1980, 1, 1); // It would be set by Gate on first usage. TODO: I am keeping this value arbitrarily. May be it is incorrect, and on first entry, gate checks this field against an old value. So, put this parameter exactly as in BS21 TOM
                validation.LastTransactionDateTime = new DateTime(1980, 1, 1);
                validation.BonusValue = 0;
                validation.Location = 0; // Later found that it be let like it is; so keeping it as zero. 0xFF; // Adapted from IERS
                validation.RejectCode = 0;
                validation.EntryExitBit = Validation.TypeValues.Exit;
                validation.AgentRemainingTrips = 0;

                #endregion

                if (!bSellOnlyProduct)
                {
                    #region Media
                    var media = logMedia.Media;
                    media.Blocked = false;
                    media.Status = Media.StatusValues.Issued;
                    media.Test = bTestTicket;
                    media.Type = Media.TypeValues.CSC;
                    media.SequenceNumber = 0; //Nowhere used
                    #endregion                  

                    logMedia.Application.Customer.Language = lng;                    

                    #region Purse
                    var purse = logMedia.Purse;
                    purse.TPurse.Balance = 0;

                    purse.LastAddValue.Amount = 0;
                    purse.LastAddValue.DateTime = DateTime.Now;
                    purse.LastAddValue.EquipmentNumber = SharedData.EquipmentNumber;
                    purse.LastAddValue.EquipmentType = SharedData.EquipmentType;
                    if (payment == PaymentMethods.BankCard)
                        purse.LastAddValue.OperationType = LastAddValue.OperationTypeValues.Cash; //To be treated afterwards
                    else
                        purse.LastAddValue.OperationType = LastAddValue.OperationTypeValues.Cash;
                    purse.LastAddValue.SequenceNumber = purseSeq;
                    purse.LastAddValue.ServiceProvider = SharedData.ServiceProvider;

                    var autoReload = logMedia.Purse.AutoReload;
                    autoReload.Amount = 0;
                    autoReload.ExpiryDate = new DateTime(2000, 1, 1);
                    autoReload.AutoTopupDateAndTime = new DateTime(2000, 1, 1);
                    autoReload.Status = AutoReload.StatusValues.Disabled;
                    autoReload.Threshold = 0;
                    autoReload.UnblockingSequenceNumber = 0;

                    #endregion

                    #region Equipment
                    var eqpt = logMedia.EquipmentData;
                    eqpt.SequenceNumber = 0;
                    #endregion                    
                }

                logMedia.DESFireDelhiLayout.Reset();

                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SalesRules_AddValueToUpdate " + e.Message);
                return false;
            }
        }

        public static bool RefundUpdateToken(LogicalMedia pLogicalMedia)
        {
            try
            {
                Media m = pLogicalMedia.Media;

                Initialisation ini = pLogicalMedia.Initialisation;
                TransportApplication ta = pLogicalMedia.Application.TransportApplication;
                LocalLastAddValue lcav = pLogicalMedia.Application.LocalLastAddValue;
                Customer cu = pLogicalMedia.Application.Customer;
                Validation val = pLogicalMedia.Application.Validation;

                Products ps = pLogicalMedia.Application.Products;

                //ps.Product(0).Type = 0;

                lcav.Amount = 0;
                DateTime now = DateTime.Now;
                lcav.DateTime = now;
                val.LastTransactionDateTime = now;
                if (SharedData.EquipmentNumber == 0)
                    return false;
                lcav.EquipmentNumber = SharedData.EquipmentNumber;
                if (SharedData.EquipmentType == EquipmentFamily.Unknown || SharedData.EquipmentType == EquipmentFamily.None)
                    return false;
                lcav.EquipmentType = SharedData.EquipmentType;
                //lcav.Location = 0;
                //val.Location = 0;
                val.EntryExitBit = Validation.TypeValues.Exit;
                lcav.ServiceProvider = SharedData.ServiceProvider;
                //lcav.Destination = 0;
                //lcav.FareTiers = 0;
                ta.Status = TransportApplication.StatusValues.Refunded;

                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "RefundUpdateToken " + e.Message);
                return false;
            }            
        }

        public static void RefundUpdateCard(LogicalMedia logMedia)
        {            
            var product = logMedia.Application.Products.Product(0);

            // As in EF_CSCC_SetNoValidity
            product.DurationValidity = 255; // As in NOVALID_8BIT_DURATION	255 (from IERS)
            product.StartOfValidity = new DateTime(1980, 1, 1);
            product.EndOfValidity = new DateTime(1980, 1, 1);
            product.Type = 0;

            // TODO: Adapt what is given inside EF_TOM_BS21C_MigrationUpdate            
            
            logMedia.Purse.TPurse.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
            logMedia.Purse.TPurse.Balance = 0;
            logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo+1;
            logMedia.Media.Status = Media.StatusValues.Refunded;

            var ta = logMedia.Application.TransportApplication;
            ta.SequenceNumber = ta.SequenceNumberRead - 1;
            ta.Deposit = 0;
            ta.Status = TransportApplication.StatusValues.Refunded; // To check

            logMedia.Application.Validation.BonusValue = 0;

            logMedia.Media.Status = Media.StatusValues.Refunded;

            logMedia.DESFireDelhiLayout.Reset();
        }

        public static void EnableBankTopupUpdate(LogicalMedia logMedia, AutoTopupDetails autoTopupDetails)
        {
            // TODO: What about sequence numbers??
            var autoReload = logMedia.Purse.AutoReload;
            autoReload.Status = AutoReload.StatusValues.Enabled;
            autoReload.Amount = autoTopupDetails.ReloadAmount;
            autoReload.Threshold = autoTopupDetails.ThresholdAmount;
            autoReload.ExpiryDate = autoTopupDetails.BankTopUpEndDate;
            autoReload.UnblockingSequenceNumber = 0; // todo: may see once again

            Customer cu = logMedia.Application.Customer;
            cu.BirthDate = new DateTime(2012, 1, 1);//autoTopupDetails.PatronDOB;
            cu.IDType = autoTopupDetails.PatronIdType;
            cu.ID = autoTopupDetails.PatronId;
            
            // not setting autoReload.LastDate i.e. DM2::Sale_AddValue::Last Bank Topup Date
            logMedia.DESFireDelhiLayout.Reset();
        }

        public static void DisableBankTopupUpdate(LogicalMedia logMedia)
        {
            // TODO: What about sequence numbers??
            var autoReload = logMedia.Purse.AutoReload;
            autoReload.Status = AutoReload.StatusValues.Disabled;
            autoReload.Amount = 0;
            autoReload.Threshold = 0;
            autoReload.ExpiryDate = new DateTime(2010, 1, 1);
            autoReload.UnblockingSequenceNumber = 0; // todo: may see once again

            // not setting autoReload.LastDate i.e. DM2::Sale_AddValue::Last Bank Topup Date deliberatly
            logMedia.DESFireDelhiLayout.Reset();
        }

        public static void UpdateLastAddValueForBankTopup(LogicalMedia logMedia)
        {
            var autoReload = logMedia.Purse.AutoReload;
            autoReload.AutoTopupDateAndTime = DateTime.Now;
        }

        public static void CSCSurrenderUpdate(LogicalMedia logMedia)
        {
            // TODO: What about sequence numbers??
            var media = logMedia.Media;
            media.Status = Media.StatusValues.Surrendered;
            var ta = logMedia.Application.TransportApplication;
            ta.Status = TransportApplication.StatusValues.Surrendered;
        }

        public static void BadDebtSettlementUpdate(LogicalMedia logMedia, 
            int badDebtAmt // In paise
            )
        {
            var media = logMedia.Media;
            var autoReload = logMedia.Purse.AutoReload;
            autoReload.StatusRead = AutoReload.StatusValues.Enabled;

            if (media.Blocked)
            {
                // Anuj: I think that we always reach here.
                media.Blocked = false;
                if (autoReload.UnblockingSequenceNumber == 0)
                    autoReload.UnblockingSequenceNumber = 1;
                else
                    autoReload.UnblockingSequenceNumber = 0;
            }
            // TODO: See if the DM1#Sequence (Purse) Number i.e. logMedia.Purse.TPurse.SequenceNumber too has to be updated
            var lastAddValue = logMedia.Purse.LastAddValue;
            // Doing it only because we have been provided a valid value for  logMedia.Purse.LastAddValue.OperationType
            lastAddValue.Amount = badDebtAmt;
            lastAddValue.OperationType = LastAddValue.OperationTypeValues.BadDebtSettlement;
            lastAddValue.DateTime = DateTime.Now;
            lastAddValue.EquipmentNumber = SharedData.EquipmentNumber;
            lastAddValue.EquipmentType = SharedData.EquipmentType;
            lastAddValue.SequenceNumber = logMedia.Purse.TPurse.SequenceNumber;
            lastAddValue.ServiceProvider = SharedData.ServiceProvider;

            logMedia.DESFireDelhiLayout.Reset();
        }

        public static void AddValueCancelUpdate(LogicalMedia logMedia, int _purseValueAdded//, bool increaseSequence
            )
        {
            logMedia.Purse.TPurse.SequenceNumber = logMedia.Purse.TPurse.SequenceNumberRead - 1;
            logMedia.Purse.TPurse.Balance = logMedia.Purse.TPurse.BalanceRead - _purseValueAdded;
            logMedia.Application.TransportApplication.SequenceNumber = Math.Abs(logMedia.Purse.TPurse.SequenceNumberRead - 1);
            logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo + 1;
            // TODO: See if logMedia.Application.TransportApplication.SequenceNumber has to be affected
            // TODO: See if logMedia.Purse.LastAddValue has to be restored back            
        }

        public static void ReplaceUpdate(LogicalMedia pLogicalMedia, LogicalMedia logicalMediaOldCSC)
        {
            Media m = pLogicalMedia.Media;
            
            Initialisation ini = pLogicalMedia.Initialisation;
            TransportApplication ta = pLogicalMedia.Application.TransportApplication;
            LocalLastAddValue lcav = pLogicalMedia.Application.LocalLastAddValue;
            Customer cu = pLogicalMedia.Application.Customer;
            Validation val = pLogicalMedia.Application.Validation;
            
            Products ps = pLogicalMedia.Application.Products;

            var product = logicalMediaOldCSC.Application.Products.Product(0);
            pLogicalMedia.Application.Products.Add(product);
            lcav.Amount = logicalMediaOldCSC.Purse.TPurse.BalanceRead;
            ta.ExpiryDate = product.EndOfValidity;
            ta.OperationalType = TransportApplication.OperationalTypeValues.Passenger;
            ta.Status = TransportApplication.StatusValues.Issued;
            ta.Deposit = logicalMediaOldCSC.Application.TransportApplication.Deposit;
            
            m.Blocked = false;
            m.Status = Media.StatusValues.Issued;
            m.Test = false;
            m.Type = Media.TypeValues.CSC;

            var purse = pLogicalMedia.Purse;
            purse.TPurse.Balance = logicalMediaOldCSC.Purse.TPurse.BalanceRead;
        }
    }
}
