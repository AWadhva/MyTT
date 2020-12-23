using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    class MediaOpReqDisableAutoTopup : MediaOpReqPreRegisteration
    {
        public MediaOpReqDisableAutoTopup
            (MainTicketingRules parent, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(parent, snum, mediaDataPriorToOperation)
        {            
        }

        enum Status
        {
            NotInitiated,
            DM1_WrittenButFailed,
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        public override MediaOpType GetOpType()
        {
            return MediaOpType.DisbleAutoTopup;
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            AutoReload.StatusValues activationStatusAtTimeOfRegisteration = _logicalMediaPriorToOperation.Purse.AutoReload.StatusRead;
            AutoReload.StatusValues activationStatusNow = logMediaNow.Purse.AutoReload.StatusRead;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (activationStatusAtTimeOfRegisteration != activationStatusNow)
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
                        if (activationStatusNow == AutoReload.StatusValues.Enabled)
                            return true;

                        _statusAreaDone = Status.Success;
                        return false;
                    }
                default:
                    throw new Exception("Unexpected");
            }
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp,
                        TransactionType.DisableBankTopup,                        
                        null,
                        _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr); 
            }
            return _xmlStringToSendToMMI;
        }

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
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
                    Debug.Assert(false);
                    return ResultLastAttempt.MediaNoMoreFitForOperation;
                }
            }
            _logicalMediaUpdatedForCurrentOp = new LogicalMedia(_logicalMediaPriorToOperation.ToXMLString());
            _logicalMediaPriorToOperation.DESFireDelhiLayout.Hidden = true;

            SalesRules.DisableBankTopupUpdate(_logicalMediaUpdatedForCurrentOp);
            CommonHwMedia hwCsc = _ticketingRules.hwCsc;
            if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
            {
                if (!hwCsc.WriteLocalValidationData(_logicalMediaUpdatedForCurrentOp))
                {
                    return ResultLastAttempt.MediaCouldntBeWritten;
                }

                if (hwCsc.CommitModifications())
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
            else
            {
                throw new Exception("Unexpected");
            }
        }
    }
}
