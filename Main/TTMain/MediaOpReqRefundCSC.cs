// as per appendix E, after refund, transactions generated for CCHS are "CSC Refund" and "Purse Refund". For CSC Refund, I think it is ImmediateRefund, while Purse Refund is shelved, because there is not such listing in 6.3
// Also, it seems that for non Bank topup enabled CSCs, no need to query CCHS for performing the operation.
// If the CSC is bank topup enabled, then only Q052 needs to be raised
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Diagnostics;
using IFS2.Equipment.Common.CCHS;

namespace IFS2.Equipment.TicketingRules
{
    class MediaOpReqRefundCSC : MediaOpReqPreRegisteration
    {
        public MediaOpReqRefundCSC(MainTicketingRules parent, Int64 snum, LogicalMedia mediaDataPriorToOperation, RefundParams opParsXml) :
            base(parent, snum, mediaDataPriorToOperation)
        {
            _refundParams = opParsXml;
        }
        RefundParams _refundParams;

        public override MediaOpType GetOpType()
        {
            return MediaOpType.Refund;
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;
        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCImmediateRefund txn = new FldsCSCImmediateRefund();
                    txn.issuerId = _logicalMediaPriorToOperation.Media.OwnerRead;
                    txn.psn = Math.Abs((int)_logicalMediaUpdatedForCurrentOp.Purse.TPurse.SequenceNumber);
                    txn.depositAmt = _logicalMediaPriorToOperation.Application.TransportApplication.Deposit;
                    txn.status = CSC_StatusCode_t.TicketTotalRefunded_AKA_Surrendered;
                    txn.refundMethod = RefundMethod_t.Cash;
                    txn.refundReason = SurrenderReason_t.PatronRequest_NotToBeReplaced; // TODO: see if there is more appropriate code
                    txn.refundTotal = _logicalMediaPriorToOperation.Purse.TPurse.Balance + _logicalMediaPriorToOperation.Application.TransportApplication.Deposit - _refundParams._FeesOptedByAgent;
                    txn.cscCaptured = Boolean_t.TRUE;
                    txn.remainingVal = 0; // TODO: Modify it if we find that there are multiple records 
                    txn.refundType = Refund_t.Immediate;
                    txn.fees = _refundParams._FeesOptedByAgent;                    

                    // Note that first parameter is _logicalMediaPriorToOperation instead of _logicalMediaUpdatedForCurrentOp
                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaPriorToOperation, TransactionType.CSCImmediateRefund, txn,
                        _statusAreaDone != Status.Success, _logicalMediaPriorToOperation.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            // For DM1's contents
            long seqNumDM1Now = logMediaNow.Purse.TPurse.SequenceNumberRead;
            long seqNumDM1WhileRegistering = _logicalMediaPriorToOperation.Purse.TPurse.SequenceNumberRead;

            // For DM2's contents
            long seqNumDM2Now = logMediaNow.Application.TransportApplication.SequenceNumberRead;
            long seqNumDM2AtTimeOfRegisteration = _logicalMediaPriorToOperation.Application.TransportApplication.SequenceNumberRead;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (seqNumDM1Now != seqNumDM1WhileRegistering)
                        {
                            Logging.Log(LogLevel.Error, "seqNumDM1Now = " + seqNumDM1Now.ToString()
                                + " seqNumDM1AtTimeOfRegisteration = " + seqNumDM1WhileRegistering.ToString());
                            return false;
                        }
                        else
                            return true;
                    }
                case Status.DM1_WrittenButFailed:
                    {
                        if (seqNumDM1Now != seqNumDM1WhileRegistering)
                        {
                            if (seqNumDM1WhileRegistering - 1 == seqNumDM1Now // TODO: Put this condition, after getting firm info, that we are incrementing it or decrementing it.
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
                        if (seqNumDM2AtTimeOfRegisteration == seqNumDM2Now)
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

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOpReqRefundCSC::CorrectMediaAppeared");
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;

            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
            {
                return ResultLastAttempt.MediaCouldntBeRead;
            }
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

                CommonHwMedia hwCsc = _ticketingRules.hwCsc;
                SalesRules.RefundUpdateCard(_logicalMediaUpdatedForCurrentOp);

                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
                {
                    // Attempt writing DM1
                    if (!hwCsc.UpdateTPurseData(_logicalMediaUpdatedForCurrentOp, 
                        -_logicalMediaPriorToOperation.Purse.TPurse.Balance, // balance can be either +ve or -ve. It would be taken care by the function whether to credit/debit
                        false)
                        || !hwCsc.WriteMainSaleData(_logicalMediaUpdatedForCurrentOp))
                        return ResultLastAttempt.MediaCouldntBeWritten;

                    if (!hwCsc.CommitModifications())
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
                        // Not affecting _completionStatus

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        // Though never observed that reader writer reutrns incorrect value
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.DM2_ToBeWritten;
                    }
                }

                Debug.Assert(_statusAreaDone == Status.DM2_WrittenButFailed || _statusAreaDone == Status.DM2_ToBeWritten);
                if (_statusAreaDone == Status.DM2_WrittenButFailed || _statusAreaDone == Status.DM2_ToBeWritten)
                {                    
                    if (!hwCsc.WriteLocalSaleData(_logicalMediaUpdatedForCurrentOp, false) // File #1, #3
                    || !hwCsc.WriteLocalValidationData(_logicalMediaUpdatedForCurrentOp) // File #2
                    )
                    {
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    bool bDM2Written = hwCsc.CommitModifications();                    
                    if (!bDM2Written)
                    {
                        _statusAreaDone = Status.DM2_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;

                        return ResultLastAttempt.Success;
                    }
                }
                else
                    throw new Exception("Unreachable");
            }
            else
            {
                Debug.Assert(false);
                throw new Exception("Unreachable");
            }
        }

        // Note that unlike other CSC operations, we don't take status DM1_WrittenButFailed as Done (in fact, if CSC API is correct (which we have never found to be incorrect).
        // It is done so, because this operation causes reduction in operator's liability, instead of increasing it.
        public override bool bIsOpCompletedEvenPartly()
        {
            switch (_statusAreaDone)
            {
                case Status.DM2_ToBeWritten:
                case Status.DM2_WrittenButFailed:
                case Status.Success:
                    return true;
                default:
                    return false;
            }
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
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;
    }
}
