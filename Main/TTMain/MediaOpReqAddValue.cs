using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Diagnostics;
using IFS2.Equipment.Common.CCHS;

namespace IFS2.Equipment.TicketingRules
{
    public class MediaOpReqAddValue : MediaOpReqPreRegisteration, IMediaCancellableOp
    {
        public MediaOpReqAddValue(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            ParseIps(opParsXml);
        }

        private void ParseIps(string opParsXml)
        {
            XDocument doc = XDocument.Parse(opParsXml);
            XElement root = doc.Root;
            _purseValueToIncrement = Convert.ToInt32(root.Element("PurseVal").Value);
        }

        enum Status 
        { 
            NotInitiated, 
            DM1_WrittenButFailed, 
            DM2_ToBeWritten, 
            DM2_WrittenButFailed, 
            Success 
        };

        Status _statusAreaDone = Status.NotInitiated;

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            // For DM1's contents
            int balanceNow = logMediaNow.Purse.TPurse.BalanceRead;
            int balanceWhileRegistering = _logicalMediaPriorToOperation.Purse.TPurse.BalanceRead;

            // For DM2's contents            
            DateTime dtLocLastTimestampNow = logMediaNow.Application.LocalLastAddValue.DateTimeRead;
            DateTime dtLocLastTimestampWhileRegistering = _logicalMediaPriorToOperation.Application.LocalLastAddValue.DateTimeRead;

            switch(_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (balanceWhileRegistering != balanceNow
                            || dtLocLastTimestampNow != dtLocLastTimestampWhileRegistering)
                        {
                            Logging.Log(LogLevel.Error, "balanceWhileRegistering = " + balanceWhileRegistering.ToString()
                                + " balanceNow = " + balanceNow.ToString());
                            
                            return false;
                        }
                        else
                            return true;
                    }
                case Status.DM1_WrittenButFailed:
                    {
                        if (balanceWhileRegistering != balanceNow)
                        {
                            if (balanceWhileRegistering + _purseValueToIncrement == balanceNow
                                && dtLocLastTimestampNow == dtLocLastTimestampWhileRegistering
                                )
                            {
                                Logging.Log(LogLevel.Information, "Status.DM1_WrittenButFailed: had got successful");
                                _statusAreaDone = Status.DM2_ToBeWritten;

                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                case Status.DM2_WrittenButFailed:
                    {
                        if (dtLocLastTimestampNow == dtLocLastTimestampWhileRegistering)
                        {
                            return true;
                        }
                        else
                        {
                            _statusAreaDone = Status.Success;
                            return false;
                        }
                    }
                case Status.Success:
                default:
                    {
                        // we would never reach here
                        Debug.Assert(false);
                        return true;
                    }                    
            }
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;
        Tuple<string, string> _xmlStringForCancellationToSendToMMI = null;

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    switch (_txn.PaymentType)
                    {
                        case PaymentMethods.Cash:
                            {
                                //Modification JL. Tags were not there previously.
                                //cchsStr = new XElement("CCHSStr",
                                //    new XElement("Typ", TransactionType.TPurseDirectReload.ToString()),
                                //    new XElement("TD", SmartFunctions.Instance.GetTDforCCHS(_logicalMediaUpdatedForCurrentOp, TransactionType.TPurseDirectReload, ++SharedData.TransactionSeqNo, _purseValueToIncrement, out cchsStr))).ToString();
                                SmartFunctions.Instance.GetTDforCCHS(_logicalMediaUpdatedForCurrentOp, TransactionType.TPurseDirectReload, ++SharedData.TransactionSeqNo, _purseValueToIncrement, out cchsStr, _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test, false);
                                _logicalMediaUpdatedForCurrentOp.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;
                                cchsStr = Utility.MakeTag("XdrData", cchsStr);
                                break;
                            }
                        case PaymentMethods.BankCard:
                            {
                                FldsCSCPeformAddValueViaEFT txn = new FldsCSCPeformAddValueViaEFT();
                                txn.addValAmt = _purseValueToIncrement;
                                txn.depositInCents = _logicalMediaPriorToOperation.Application.TransportApplication.Deposit;
                                txn.purseRemainingVal = _logicalMediaUpdatedForCurrentOp.Purse.TPurse.Balance;

                                var bankTopupDetails = _txn.BankTopupDetails;

                                txn.systemTraceAuditNumber = bankTopupDetails.SystemTraceAuditNumber;
                                txn.dtTimeBankTerminalTransaction = bankTopupDetails.dt;
                                txn.authorizationCode = bankTopupDetails.authorizationCode;
                                txn.bankTerminalId = bankTopupDetails.TerminalID;
                                txn.resoponseCode = bankTopupDetails.responseCodeFromTerminal;

                                txn.operationType = OperationType_t.Other; // TODO: It is neither sale nor replacement. But Other doesn't seem to a good value. Let's see what fits
                                cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp, TransactionType.TXN_CSC_ADD_VALUE_EFT, txn,
                                    _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                                break;
                            }
                        default:
                            throw new NotImplementedException(); // Bank topup is implemented separatly inside MediaOpReqPeformAutoTopup
                    } 
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.AddValue;
        }

        int _purseValueToIncrement;

        public int PurseValueToIncrement { get { return _purseValueToIncrement; } }        
        
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;
        LogicalMedia _logMediaUpdatedForCancelOp = null;

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

                if (!
                SalesRules.AddValueUpdate(_logicalMediaUpdatedForCurrentOp, _purseValueToIncrement, _txn.PaymentType))
                    return ResultLastAttempt.MediaCouldntBeRead;

                SharedData.CSC_oldEndOfValidityDate = _logicalMediaUpdatedForCurrentOp.Application.Products.Product(0).EndOfValidity; // SKS: to put count into CCHS Txn TPURSE Header
                if (
                CommonRules.SetupProductEndOfValidity(_logicalMediaUpdatedForCurrentOp,
                    EndOfValidityMode.CurrentPlusYears,
                    DateTime.Now,
                    MainTicketingRules._nProductValidityYears //ANUJ: To be removed later when we adopt TOM's Sale Definition list.
                    ) != TTErrorTypes.NoError)
                    return ResultLastAttempt.MediaCouldntBeRead;

                //bool bGenerateTxn = false;
                
                CommonHwMedia hwCsc = _ticketingRules.hwCsc;

                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
                {
                    // Attempt writing DM1
                    if (hwCsc.UpdateTPurseData(_logicalMediaUpdatedForCurrentOp, _purseValueToIncrement, true))
                    {
                        _statusAreaDone = Status.DM2_ToBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                        if(_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)                        
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                }
                Debug.Assert(_statusAreaDone == Status.DM2_ToBeWritten || _statusAreaDone == Status.DM2_WrittenButFailed);

                // Attempt writing DM2
                if (_statusAreaDone == Status.DM2_ToBeWritten || _statusAreaDone == Status.DM2_WrittenButFailed)
                {
                    if (hwCsc.WriteLocalSaleData(_logicalMediaUpdatedForCurrentOp, true))
                    {
                        Logging.Log(LogLevel.Verbose, "WriteLocalSaleData Done Successfully");
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                    else
                    {
                        Logging.Log(LogLevel.Error, "WriteLocalSaleData Done Failed");
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                        _statusAreaDone = Status.DM2_WrittenButFailed;
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

        MediaOpGen.ResultLastCancelAttempt _lastCancelAttempt = ResultLastCancelAttempt.None;

        #region IMediaCancellableOp Members

        public Tuple<string, string> GetXmlStringToSendToMMIOnCancellation()
        {
            if (_xmlStringForCancellationToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCAddValueCancel txn = new FldsCSCAddValueCancel();
                    txn._addValueAmount = _purseValueToIncrement; // TODO: may be it is -1*_purseValueToIncrement
                    txn._cscDepositInRupees = (byte)(_logicalMediaPriorToOperation.Application.TransportApplication.Deposit/100);
                    txn._purseRemianingValue_PostAddValue = _logicalMediaPriorToOperation.Purse.TPurse.BalanceRead + _purseValueToIncrement;

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logMediaUpdatedForCancelOp, TransactionType.AddValueCancel, txn,
                        GetLastCancelAttempt() != ResultLastCancelAttempt.Success,
                        _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);                    
                }
                _xmlStringForCancellationToSendToMMI = Tuple.New(_logMediaUpdatedForCancelOp.ToXMLString(), cchsStr);
            }
            return _xmlStringForCancellationToSendToMMI;
        }

        enum MediaFitForCancellation { YES, 
            NO, // TODO: what is meaning of NO. For now, both these cases are stuffed: a. Fraud b. Not required. we will have to see, whether to bifurcate them in two separate codes.
            HADFOUND_MEDIANOMOREFITFOROPERTION_WHILEWRITING };

        public MediaOpGen.ResultLastCancelAttempt CorrectMediaForCancellationAppeared()
        {
            Debug.Assert(_lastCancelAttempt == ResultLastCancelAttempt.None || _lastCancelAttempt == ResultLastCancelAttempt.MediaCouldntBeRead);

            _ticketingRules.TreatmentOnCardDetection2(false, false);
            var error = _ticketingRules.ErrorForJustProducedMedia;
            if (error != TTErrorTypes.NoError)
            {
                if (error == TTErrorTypes.CannotReadTheCard || error == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                {
                    _lastCancelAttempt = ResultLastCancelAttempt.MediaCouldntBeRead;
                    return _lastCancelAttempt;
                }
            }
            MediaFitForCancellation canBeCancelled = EvaluateIfMediaIsFitForCancellation();
            switch (canBeCancelled)
            {
                case MediaFitForCancellation.NO:
                case MediaFitForCancellation.HADFOUND_MEDIANOMOREFITFOROPERTION_WHILEWRITING:
                    _lastCancelAttempt = ResultLastCancelAttempt.MediaNotFoundFitForCancellation;
                    break;
                case MediaFitForCancellation.YES:
                    _logMediaUpdatedForCancelOp = new LogicalMedia(_logicalMediaUpdatedForCurrentOp.ToXMLString());
       
                    CommonHwMedia hwCsc = _ticketingRules.hwCsc;

                    LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
                    int purseValueToDecrement = logMediaAtFront.Purse.TPurse.BalanceRead - _logicalMediaPriorToOperation.Purse.TPurse.BalanceRead;
                    SalesRules.AddValueCancelUpdate(_logMediaUpdatedForCancelOp, -purseValueToDecrement);

                    if (hwCsc.UpdateTPurseData(_logMediaUpdatedForCancelOp, -purseValueToDecrement, true))
                    {
                        _lastCancelAttempt = ResultLastCancelAttempt.Success;

                        // may be writing local sale data after detemininig that it is success is wiser for Cancel
                        hwCsc.WriteLocalSaleData(_logMediaUpdatedForCancelOp, true);
                    }
                    else
                        _lastCancelAttempt = ResultLastCancelAttempt.MediaCouldntBeWritten;
                    break;
            }
            return _lastCancelAttempt;
        }

        private MediaFitForCancellation EvaluateIfMediaIsFitForCancellation()
        {
            LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
            Logging.Log(LogLevel.Verbose, "EvaluateIfMediaIsFitForCancellation _completionStatus = " + _completionStatus.ToString());
            switch (_completionStatus)
            {
                case MediaUpdateCompletionStatus.MediaNoMoreFitForOperation:
                    {
                        Logging.Log(LogLevel.Information, "EvaluateIfMediaIsFitForCancellation Exit A1");
                        return MediaFitForCancellation.HADFOUND_MEDIANOMOREFITFOROPERTION_WHILEWRITING;
                    }
                case MediaUpdateCompletionStatus.DoneSuccessfully:
                    if (logMediaAtFront.Purse.TPurse.BalanceRead == _logicalMediaUpdatedForCurrentOp.Purse.TPurse.Balance
                        && logMediaAtFront.Application.Validation.EntryExitBitRead == _logicalMediaUpdatedForCurrentOp.Application.Validation.EntryExitBit // this is necessary to check, because on entry at gate, sequence number is not modified
                        && logMediaAtFront.Purse.TPurse.SequenceNumberRead == _logicalMediaUpdatedForCurrentOp.Purse.TPurse.SequenceNumber
                        //&& logMediaAtFront.Application.LocalLastAddValue.DateTimeRead == _logicalMediaUpdatedForCurrentOp.Application.LocalLastAddValue.DateTime
                        )
                    {
                        Logging.Log(LogLevel.Information, "EvaluateIfMediaIsFitForCancellation Exit C1");
                        return MediaFitForCancellation.YES;
                    }
                    else
                    {
                        Logging.Log(LogLevel.Information, "EvaluateIfMediaIsFitForCancellation Exit C2");
                        return MediaFitForCancellation.NO;
                    }
                case MediaUpdateCompletionStatus.NotDone:
                    return MediaFitForCancellation.NO;
                case MediaUpdateCompletionStatus.DeclaredByMMINotToPerformPostWTE:
                    {
                        switch(_statusAreaDone)
                        {
                            case Status.DM1_WrittenButFailed:
                                {
                                    if (logMediaAtFront.Purse.TPurse.BalanceRead == _logicalMediaPriorToOperation.Purse.TPurse.Balance
                                     && logMediaAtFront.Application.Validation.EntryExitBitRead == _logicalMediaPriorToOperation.Application.Validation.EntryExitBit
                                     //&& logMediaAtFront.Application.LocalLastAddValue.DateTimeRead == _logicalMediaPriorToOperation.Application.LocalLastAddValue.DateTimeRead
                                        )
                                    {
                                        Logging.Log(LogLevel.Information, "EvaluateIfMediaIsFitForCancellation Exit B1");
                                        return MediaFitForCancellation.YES;
                                    }
                                    else
                                        return MediaFitForCancellation.NO;
                                }
                            case Status.DM2_WrittenButFailed:
                                {
                                    if (logMediaAtFront.Purse.TPurse.BalanceRead == _logicalMediaUpdatedForCurrentOp.Purse.TPurse.Balance
                                    && logMediaAtFront.Application.Validation.EntryExitBitRead == _logicalMediaPriorToOperation.Application.Validation.EntryExitBit // this is necessary to check, because on entry at gate, sequence number is not modified
                                    //&& logMediaAtFront.Application.LocalLastAddValue.DateTimeRead == _logicalMediaPriorToOperation.Application.LocalLastAddValue.DateTimeRead
                                        )
                                    {
                                        Logging.Log(LogLevel.Information, "EvaluateIfMediaIsFitForCancellation Exit B2");
                                        return MediaFitForCancellation.YES;
                                    }
                                    else
                                        return MediaFitForCancellation.NO;                                    
                                }
                            default:
                                {
                                    Debug.Assert(false);
                                    Logging.Log(LogLevel.Error, "EvaluateIfMediaIsFitForCancellation DeclaredByMMINotToPerformPostWTE Unexpected state: " + _completionStatus.ToString());
                                    throw new Exception("EvaluateIfMediaIsFitForCancellation " + _statusAreaDone.ToString()); 
                                }
                        }
                    }
                default:
                    Debug.Assert(false);
                    Logging.Log(LogLevel.Error, "EvaluateIfMediaIsFitForCancellation Unexpected state: " + _completionStatus.ToString());
                    throw new Exception("EvaluateIfMediaIsFitForCancellation " + _completionStatus.ToString());
            }
        }

        public MediaOpGen.ResultLastCancelAttempt GetLastCancelAttempt()
        {
            return _lastCancelAttempt;
        }

        #endregion
    }
}
