// For auto-topup, wrting to DM2::SaleAddValue is very important, because that would be decided in making the decision for disabling the auto topup. So, we write DM2 first
// and then DM1.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public class MediaOpReqPeformAutoTopup : MediaOpReqPreRegisteration
    {
        public MediaOpReqPeformAutoTopup(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            _purseValueToIncrement = mediaDataPriorToOperation.Purse.AutoReload.AmountRead;
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.BankTopupPerform;
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;
        int _purseValueToIncrement;
        Status _statusAreaDone = Status.NotInitiated;

        enum Status
        {
            NotInitiated,
            DM2_WrittenButFailed,
            DM1_ToBeWritten,
            DM1_WrittenButFailed,
            Success
        };


        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCPeformAddValueViaBankTopup txn = new FldsCSCPeformAddValueViaBankTopup();
                    txn.addValAmt = _purseValueToIncrement;
                    txn.depositInCents = _logicalMediaPriorToOperation.Application.TransportApplication.Deposit;
                    txn.purseRemainingVal = _logicalMediaUpdatedForCurrentOp.Purse.TPurse.Balance;

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp, TransactionType.TPurseBankTopupReload, txn,
                    _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }

                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }        

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            DateTime lastBankTopupDateNow = logMediaNow.Purse.AutoReload.AutoTopupDateAndTimeRead;

            int balanceNow_DM1 = logMediaNow.Purse.TPurse.BalanceRead;
            DateTime expiryDateNow = logMediaNow.Application.Products.Product(0).EndOfValidity;

            long seqNumNow = logMediaNow.Purse.TPurse.SequenceNumberRead;
            int balanceWhileRegistering_DM1 = _logicalMediaPriorToOperation.Purse.TPurse.BalanceRead;
            DateTime expiryDateWhileRegistering = _logicalMediaPriorToOperation.Application.Products.Product(0).EndOfValidity;
       
            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (balanceWhileRegistering_DM1 != balanceNow_DM1)
                        {
                            Logging.Log(LogLevel.Error, "balanceWhileRegistering = " + balanceWhileRegistering_DM1.ToString()
                                + " balanceNow = " + balanceNow_DM1.ToString());

                            return false;
                        }
                        else
                            return true;
                    }
                case Status.DM2_WrittenButFailed:
                    {
                        if (expiryDateNow == expiryDateWhileRegistering) // it makes more 
                        {
                            _statusAreaDone = Status.DM1_ToBeWritten;
                            return true;
                        }                        
                        else
                        {
                            return false;
                        }
                    }
                case Status.DM1_WrittenButFailed:
                    {
                        if (balanceWhileRegistering_DM1 == balanceNow_DM1
                            //&& seqNumWhileRegistering + 1 = seqNumNow // TODO: Put this condition, after getting firm info, that we are incrementing it or decrementing it. In fact this should be the only condition
                            )
                            return true;
                        else
                        {
                            _statusAreaDone = Status.Success;
                            return false;
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOperationRequestAddValue::CorrectMediaAppeared");
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;

            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
                return ResultLastAttempt.MediaCouldntBeRead;
            else if (err == TTErrorTypes.NoError)
            {
                LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();

                if (!EvaluateIfMediaIsFitForOperation())
                {
                    if (_statusAreaDone == Status.Success)
                    {
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                    else
                    {
                        _completionStatus = MediaUpdateCompletionStatus.MediaNoMoreFitForOperation;
                        return ResultLastAttempt.MediaNoMoreFitForOperation;
                    }
                }

                _logicalMediaUpdatedForCurrentOp = new LogicalMedia(_logicalMediaPriorToOperation.ToXMLString());

                SalesRules.AddValueUpdate(_logicalMediaUpdatedForCurrentOp, _purseValueToIncrement, PaymentMethods.BankTopup//, false
                    );

                SharedData.CSC_oldEndOfValidityDate = _logicalMediaUpdatedForCurrentOp.Application.Products.Product(0).EndOfValidity; // SKS: to put count into CCHS Txn TPURSE Header
                if (
                CommonRules.SetupProductEndOfValidity(_logicalMediaUpdatedForCurrentOp,
                    EndOfValidityMode.CurrentPlusYears,
                    DateTime.Now,
                    MainTicketingRules._nProductValidityYears //ANUJ: To be removed later when we adopt TOM's Sale Definition list.
                    ) != TTErrorTypes.NoError)
                    return ResultLastAttempt.MediaCouldntBeRead;
                SalesRules.UpdateLastAddValueForBankTopup(_logicalMediaUpdatedForCurrentOp);
                
                CommonHwMedia hwCsc = _ticketingRules.hwCsc;

                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM2_WrittenButFailed)
                {
                    // Attempt writing DM2
                    if (hwCsc.WriteLocalSaleData(_logicalMediaUpdatedForCurrentOp, true))
                    {
                        _statusAreaDone = Status.DM1_ToBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.DM2_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    } 
                }
                Debug.Assert(_statusAreaDone == Status.DM1_ToBeWritten || _statusAreaDone == Status.DM1_WrittenButFailed);

                if (_statusAreaDone == Status.DM1_ToBeWritten || _statusAreaDone == Status.DM1_WrittenButFailed)
                {                    
                    if (hwCsc.UpdateTPurseData(_logicalMediaUpdatedForCurrentOp, _purseValueToIncrement, true))
                    {
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                    else
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                }
                throw new Exception("Unreachable at the moment");
            }
            else
            {
                // TODO: Still make sure that polling does get initiated, so that TT doesn't remain in useless state.
                Debug.Assert(false);
                throw new Exception("Unexpected error code");
            }
        }

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }
    }
}
