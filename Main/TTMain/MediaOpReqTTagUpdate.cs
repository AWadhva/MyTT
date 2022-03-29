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
    public class MediaOpReqTTagUpdate : MediaOpReqNoPreRegisteration
    {
        public MediaOpReqTTagUpdate(MainTicketingRules parent, string logicalMediaReference) :
            base(parent, logicalMediaReference, null)
        {}

        public override MediaOpType GetOpType()
        {
            return MediaOpType.TTagUpdate;
        }        

        public override bool DoesNeedTokenDispenser()
        {
            return false;
        }

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            return Tuple.New(_logicalMediaReference.ToXMLString(), (string)null);
        }

        enum Status { NotInitiated, WrittenButFailed, Success };
        Status _statusDone = Status.NotInitiated;

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusDone != Status.NotInitiated);
        }
        
        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOpReqTTagIssue::CorrectMediaAppeared _statusDone = " + _statusDone.ToString());
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            if (_statusDone == Status.NotInitiated)
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
                        return ResultLastAttempt.MediaNotFoundFitForOperation;
                    }
                }
            }
            else
            {
                _ticketingRules.TreatmentOnCardDetection2(false, true);

                TTErrorTypes err = _ticketingRules.ErrorForJustProducedMedia;
                if (err == TTErrorTypes.CannotReadTheCard || err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                    return ResultLastAttempt.MediaCouldntBeRead;
                else if (!EvaluateIfMediaIsFitForOperation())
                {
                    if (_statusDone == Status.Success)
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
            }

            DelhiTokenUltralight ul = new DelhiTokenUltralight(null, 0);

            _logicalMediaReference.Media.ChipSerialNumber = _mediaSrNbr;
            _logicalMediaReference.TTag.SerialNumber = logMediaNow.TTag.SerialNumber;
            _logicalMediaReference.TTag.IssueDate = logMediaNow.TTag.IssueDate;
            _logicalMediaReference.TTag.CountTokens = logMediaNow.TTag.CountTokens;

            // TODO: To be more precise, may edit the sale date too.
            CSC_READER_TYPE ReaderType;
            int hRw;
            _ticketingRules.GetReaderHandle(out ReaderType, out hRw);
            var dataRead = _ticketingRules.GetLogicalDataOfMediaAtFront();

            _timeLastWriteAttempted = _logicalMediaReference.TTag.TimeLastWritten = DateTime.Now;

            byte[] cb = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocksForTTag(_logicalMediaReference.TTag));            
            bool bSuccess;

            CSC_API_ERROR ErrWriting = ((DelhiTokenUltralight)_ticketingRules.hwToken).WriteToToken(cb, out bSuccess);
            if (ErrWriting != CSC_API_ERROR.ERR_NONE)
                Logging.Log(LogLevel.Verbose, "MediaOpReqTTagUpdate::CorrectMediaAppeared " + ErrWriting.ToString());

            if (ErrWriting == CSC_API_ERROR.ERR_NONE && bSuccess)
            {
                _statusDone = Status.Success;
                _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                Logging.Log(LogLevel.Verbose, "MediaOpReqTTagUpdate::CorrectMediaAppeared Exit C");
                return ResultLastAttempt.Success;
            }
            else
            {
                _statusDone = Status.WrittenButFailed;
                Logging.Log(LogLevel.Verbose, "MediaOpReqTTagUpdate::CorrectMediaAppeared Exit D");
                return ResultLastAttempt.MediaCouldntBeWritten;
            }
        }

        private DateTime _timeLastWriteAttempted = new DateTime(2000, 1, 1);
        private bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            if (_statusDone == Status.WrittenButFailed)
            {                
                if (logMediaNow.TTag.TimeLastWritten == _timeLastWriteAttempted)
                {
                    _statusDone = Status.Success;
                    _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                    Logging.Log(LogLevel.Verbose, "MediaOpReqTTagUpdate::EvaluateIfMediaIsFitForOperation Exit A");
                    return false;
                }
                else
                {
                    Logging.Log(LogLevel.Verbose, "MediaOpReqTTagUpdate::EvaluateIfMediaIsFitForOperation Exit B");
                    return true;
                }
            }
            else
            {
                throw new Exception("");
            }
        }
    }
}