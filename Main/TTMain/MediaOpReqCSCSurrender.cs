using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using IFS2.Equipment.Common.CCHS;
using System.Diagnostics;
using System.Xml.Linq;

namespace IFS2.Equipment.TicketingRules
{
    public class MediaOpReqCSCSurrender : MediaOpReqPreRegisteration
    {
        public MediaOpReqCSCSurrender(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            _pars = SerializeHelper<CSCSurrenderedParams>.XMLDeserialize(XDocument.Parse(opParsXml).Root.Value);
            _logicalMediaUpdatedForCurrentOp = mediaDataPriorToOperation;
        }

        CSCSurrenderedParams _pars;
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        public override IFS2.Equipment.Common.MediaOpType GetOpType()
        {
            return IFS2.Equipment.Common.MediaOpType.CSCSurrender;
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCSurrendered txn = new FldsCSCSurrendered();
                    txn._patronName = new PatronName_t(_pars.PatronName);
                    txn._surrenderReason = _pars.SurrenderReason;
                    txn._refundLevel = 1;
                    txn._cscStatus = CSC_StatusCode_t.NotBlocked;
                    txn._refundMethod = _pars.RefundMethod;                    
                    
                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp,
                        TransactionType.CSC_SURRENDERED,                        
                        txn,
                        _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }

        enum Status
        {
            NotInitiated,
            DM1_WrittenButFailed,          
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOpReqCSCSurrender::CorrectMediaAppeared");
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

                SalesRules.CSCSurrenderUpdate(_logicalMediaUpdatedForCurrentOp);
                
                CommonHwMedia hwCsc = _ticketingRules.hwCsc;

                // Surrender is not commonly used, and i am in hurry. so, no segregation for write to dm1 successs, fail etc.
                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
                {
                    // Attempt writing DM1
                    if (!hwCsc.WriteMainSaleData(_logicalMediaUpdatedForCurrentOp)) // File #6                    
                        return ResultLastAttempt.MediaCouldntBeWritten;
                
                    if (hwCsc.CommitModifications())
                    {
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        //return ResultLastAttempt.Success;
                    }
                    else
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }

                    if (!hwCsc.WriteLocalValidationData(_logicalMediaUpdatedForCurrentOp) || !hwCsc.CommitModifications())
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
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

                throw new Exception("Unreachable at the moment");
            }
            else
            {
                // TODO: Still make sure that polling does get initiated, so that TT doesn't remain in useless state.
                Debug.Assert(false);
                throw new Exception("Unexpected error code");
            }
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {            
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            var mediaStatusAtTimeOfRegisteration = _logicalMediaPriorToOperation.Media.Status;
            var mediaStatusAtTimeOfRegisterationNow = logMediaNow.Media.Status;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (mediaStatusAtTimeOfRegisteration != mediaStatusAtTimeOfRegisterationNow)
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
                        if (mediaStatusAtTimeOfRegisterationNow == Media.StatusValues.Surrendered)
                            return true;

                        _statusAreaDone = Status.Success;
                        return false;
                    }
                default:
                    throw new Exception("Unexpected");
            }
        }
    }
}
