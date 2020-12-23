// Would use DM1::Sale, DM2::Validation. Both have status fields

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
    public class MediaOpReqCSCReplacement : MediaOpReqNoPreRegisteration
    {
        public MediaOpReqCSCReplacement(MainTicketingRules parent, 
            string logMedia
            ) :
            base(parent, null, null)
        {
            _logicalMediaOldCSC = new LogicalMedia(logMedia);
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.CSCReplacemnt;
        }
        
        Tuple<string, string> _xmlStringToSendToMMI = null;

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCReplacement txn = new FldsCSCReplacement();
                    //txn.purseIssuerId = _purseIssuerId;
                    txn.purseSequenceNumber = (int)_logicalMediaReference.Application.LocalLastAddValue.SequenceNumber;
                    txn.replacedCSC = _logicalMediaOldCSC;                    

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaReference, TransactionType.MediaReplacement, txn,
                        _statusAreaDone != Status.Success, false);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);                
            }
            return _xmlStringToSendToMMI;
        }

        LogicalMedia _logicalMediaOldCSC;
        
        enum Status
        {
            NotInitiated,
            DM2_WrittenButFailed,
            DM1_ToBeWritten,
            DM1_WrittenButFailed,
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;

        public override bool DoesNeedTokenDispenser()
        {
            return false;
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            TransportApplication.StatusValues taStatusNow = logMediaNow.Application.TransportApplication.StatusRead;
            Media.StatusValues mediaStatusNow = logMediaNow.Media.StatusRead;
            int purseValueNow = logMediaNow.Purse.TPurse.BalanceRead;

            switch (_statusAreaDone)
            {
                case Status.DM2_WrittenButFailed:
                    {
                        if (taStatusNow == TransportApplication.StatusValues.Issued)
                        {
                            _statusAreaDone = Status.DM1_ToBeWritten;
                            return true;
                        }
                        else if (taStatusNow == _statusAtBeginningTA)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case Status.DM1_WrittenButFailed:
                    {
                        if (mediaStatusNow == Media.StatusValues.Issued)
                        {
                            _statusAreaDone = Status.Success;
                            return false;
                        }
                        else
                            return (mediaStatusNow == _statusBeginningMedia && purseValueNow == 0);
                        
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        // using these new variable, because in lab we are allowing CSCs with status other than Initialised be issued
        TransportApplication.StatusValues _statusAtBeginningTA = TransportApplication.StatusValues.Unknown;
        Media.StatusValues _statusBeginningMedia = Media.StatusValues.Unknown;        

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            try
            {
                if (_statusAreaDone == Status.NotInitiated)
                {
                    _ticketingRules.TreatmentOnCardDetection2(false, false);
                    var error = _ticketingRules.ErrorForJustProducedMedia;
                    if (error != TTErrorTypes.NoError)
                    {
                        if (error == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || error == TTErrorTypes.CannotReadTheCard)
                            return ResultLastAttempt.MediaCouldntBeRead;
                        else
                        {
                            SetMediaSerialNumber(0);
                            _statusAreaDone = Status.NotInitiated;
                            return ResultLastAttempt.MediaNotFoundFitForOperation;
                        }
                    }
                    _statusAtBeginningTA = _ticketingRules.GetLogicalDataOfMediaAtFront().Application.TransportApplication.Status;
                    _statusBeginningMedia = _ticketingRules.GetLogicalDataOfMediaAtFront().Media.Status;
                }

                _ticketingRules.TreatmentOnCardDetection2(false, true);

                TTErrorTypes err = _ticketingRules.ErrorForJustProducedMedia;
                if (err == TTErrorTypes.CannotReadTheCard || err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                    return ResultLastAttempt.MediaCouldntBeRead;

                if (_statusAreaDone != Status.NotInitiated && !EvaluateIfMediaIsFitForOperation())
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

                if (_statusAreaDone == Status.NotInitiated)
                {
                    _logicalMediaReference = logMediaNow;
                    //SalesRules.ReplaceUpdate(_logicalMediaReference, _logicalMediaOldCSC);
                        //return ResultLastAttempt.MediaCouldntBeWritten;
                }

                CommonHwMedia hwCsc = _ticketingRules.hwCsc;
                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM2_WrittenButFailed)
                {
                    // Attempt writing DM2 (File #8 need not be and cannot be written by TOM).
                    if (!hwCsc.WriteLocalSaleData(_logicalMediaReference, false) // File #1, #3
                    || !hwCsc.WriteLocalValidationData(_logicalMediaReference) // File #2
                    )
                    {
                        // we let the status remain NotDone
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    bool bDM2Written = hwCsc.CommitModifications();

                    if (!bDM2Written)
                    {
                        _statusAreaDone = Status.DM2_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                        _statusAreaDone = Status.DM1_ToBeWritten;
                }

                if (_statusAreaDone == Status.DM1_WrittenButFailed || _statusAreaDone == Status.DM1_ToBeWritten)
                {
                    // Attempt writing DM1
                    if (!hwCsc.UpdateTPurseData(_logicalMediaReference, _logicalMediaReference.Purse.LastAddValue.Amount, false) // File #1, #2, #5
                    || !hwCsc.WriteMainSaleData(_logicalMediaReference) // File #6                    
                    || !hwCsc.WriteCardHolderData(_logicalMediaReference)) // File #9. So, we write whole auto-topup data also in this file. This may be bad, but don't have time to be elegant
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    if (!hwCsc.CommitModifications())
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();

                        // Though never observed that reader writer reutrns incorrect value
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                }
            }
            finally
            {
            }
            throw new NotImplementedException();
        }        

        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }
    }
}
