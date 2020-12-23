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
    class MediaOpReqAdjustmentCSCNonPurse : MediaOpReqPreRegisteration
    {
        public MediaOpReqAdjustmentCSCNonPurse(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, AdjustmentParams opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            _adjParams = opParsXml;            
        }
        AdjustmentParams _adjParams;
        DateTime? _dtOfLastBadWriteAttempt;

        enum Status 
        { 
            NotInitiated, 
            DM2_WrittenButFailed,             
            Success 
        };

        Status _statusAreaDone = Status.NotInitiated;
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.Adjustment;
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;
        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCashSurchargePayment txn = new FldsCashSurchargePayment();

                    txn.surchargeAmt = (int)_adjParams._AmountOptedByAgent; // TODO: How is this different than surchargeTotal??
                    txn.surchargeTotal = (int)_adjParams._AmountOptedByAgent;
                    txn.surchargeDetails = _logicalMediaPriorToOperation.Application.Validation.RejectCode;

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp, TransactionType.CashSurchargePayment, txn,
            _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }

        private bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            DateTime tsAtMediaRegisteration = _logicalMediaPriorToOperation.Application.Validation.LastTransactionDateTimeRead;
            DateTime tsNow = logMediaNow.Application.Validation.LastTransactionDateTimeRead;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (tsAtMediaRegisteration != tsNow)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                case Status.DM2_WrittenButFailed:
                    {
                        if (tsAtMediaRegisteration == tsNow)
                            return true;
                        else if (tsAtMediaRegisteration == (DateTime)_dtOfLastBadWriteAttempt)
                        {
                            _statusAreaDone = Status.Success;
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                default:
                    throw new Exception("Unexpected");
            }
        }

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;
            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
                return ResultLastAttempt.MediaCouldntBeRead;

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
                null,
                _adjParams._RejectCodeOnMediaPostAdjustment);

            CommonHwMedia hwCsc = _ticketingRules.hwCsc;
            if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM2_WrittenButFailed)
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
                    _dtOfLastBadWriteAttempt = _logicalMediaUpdatedForCurrentOp.Application.Validation.LastTransactionDateTime;
                    // Though never observed that reader writer reutrns incorrect value

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
                throw new Exception("Unexpected");
        }
    }
}

