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
    class MediaOpReqAdjustmentCSCUsingPurse : MediaOpReqPreRegisteration
    {
        public MediaOpReqAdjustmentCSCUsingPurse(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, AdjustmentParams opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            _adjParams = opParsXml;
        }
        AdjustmentParams _adjParams;
        public override MediaOpType GetOpType()
        {
            return MediaOpType.Adjustment;
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

        Tuple<string, string> _xmlStringToSendToMMI = null;
        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCSurchargePayment txn = new FldsCSCSurchargePayment();
                    txn.purseRemainingVal = _logicalMediaUpdatedForCurrentOp.Purse.TPurse.Balance;
                    txn.surchargeAmt = (int)_adjParams._AmountOptedByAgent; // TODO: How is this different than surchargeTotal??
                    txn.surchargeTotal = (int)_adjParams._AmountOptedByAgent;
                    txn.surchargeDetails = _logicalMediaPriorToOperation.Application.Validation.RejectCode;
                    // TODO: Leaving etnry location field and exit location field.

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp, TransactionType.CSC_SURCHARGE_PAYMENT, txn,
                        _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
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
                _logicalMediaPriorToOperation.DESFireDelhiLayout.Hidden = true;

                SalesRules.AdjustmentUpdateForCSC(_logicalMediaUpdatedForCurrentOp,
                    _adjParams._bUpdateDateTimeOnAdjustment,
                    (byte?)_adjParams._entryExitBitPostAdjustment,
                    _adjParams._entryExitStationCodePostAdjustment,
                    _adjParams._AmountOptedByAgent,
                    _adjParams._RejectCodeOnMediaPostAdjustment);
                
                CommonHwMedia hwCsc = _ticketingRules.hwCsc;                

                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
                {
                    // Attempt writing DM1
                    if (!hwCsc.UpdateTPurseData(_logicalMediaUpdatedForCurrentOp, -1*(int)(_adjParams._AmountOptedByAgent), false)
                        || !hwCsc.AppendCommonAreaPurseHistoryRecord(_logicalMediaUpdatedForCurrentOp))
                    {
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }

                    if (hwCsc._CommitModifications())
                    {
                        _statusAreaDone = Status.DM2_ToBeWritten;
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

                Debug.Assert(_statusAreaDone == Status.DM2_ToBeWritten || _statusAreaDone == Status.DM2_WrittenButFailed);

                // Attempt writing DM2
                if (_statusAreaDone == Status.DM2_ToBeWritten || _statusAreaDone == Status.DM2_WrittenButFailed)
                {
                    if (!hwCsc.WriteLocalValidationData(_logicalMediaUpdatedForCurrentOp)) // File #2
                    {
                        // we let the status remain NotDone
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    bool bDM2Written = hwCsc.CommitModifications();
                    // TODO: Generate txn record here irrespective of bDM2Written
                    if (!bDM2Written)
                    {
                        _statusAreaDone = Status.DM2_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        
                        // Though never observed that reader writer reutrns incorrect value
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
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

        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        private bool EvaluateIfMediaIsFitForOperation()
        {            
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            // For DM1's contents
            int balanceNow = logMediaNow.Purse.TPurse.BalanceRead;

            // For DM2's contents
            short rejectCodeNow = logMediaNow.Application.Validation.RejectCodeRead;

            int balanceWhileRegistering = _logicalMediaPriorToOperation.Purse.TPurse.BalanceRead;
            long rejectCodeWhileRegistering = _logicalMediaPriorToOperation.Application.Validation.RejectCodeRead;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (balanceWhileRegistering != balanceNow
                            || rejectCodeWhileRegistering != rejectCodeNow)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                case Status.DM1_WrittenButFailed:
                    {
                        if (balanceWhileRegistering != balanceNow)
                        {
                            if (balanceWhileRegistering - _adjParams._AmountOptedByAgent == balanceNow
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
                        if (rejectCodeNow == rejectCodeWhileRegistering)
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
        
        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }
    }
}