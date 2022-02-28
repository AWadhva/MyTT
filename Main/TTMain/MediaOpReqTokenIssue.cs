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
    public class MediaOpReqTokenIssue : MediaOpReqNoPreRegisteration, IMediaCancellableOp
    {
        public MediaOpReqTokenIssue(MainTicketingRules parent, string logicalMediaReference, string data) :
            base(parent, logicalMediaReference, data)
        {
            ParseIps(data);
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.CSTIssue;
        }

        bool _bUseDispenser;
        private void ParseIps(string parsXml)
        {
            XDocument parsDoc = XDocument.Parse(parsXml);
            XElement root = parsDoc.Root;

            _bUseDispenser = (root.Element("UseDispenser").Value == "1");
        }

        public override bool DoesNeedTokenDispenser()
        {
            return _bUseDispenser;
        }

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            return Tuple.New(_logicalMediaReference.ToXMLString(), (string)null);
        }

        enum Status { NotInitiated, WrittenButFailed, Success };
        Status _statusDone = Status.NotInitiated;

        byte[] _physicalDataInTokenPriorToOperation = null;
        //LogicalMedia _logicalMediaOfTokenPriorToOperation = null;
        //long? _macOfSelectedTokenPriorToAnyOperationAttempted { get { return (_logicalMediaOfTokenPriorToOperation == null ? (long?)null : _logicalMediaOfTokenPriorToOperation.DelhiUltralightRaw.MAC); } }
        long? _macOfSelectedTokenPriorToAnyOperationAttempted;
        ulong _macAttemptedToBeWrittenInLastAttempt = 0;
        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusDone != Status.NotInitiated);
        }

        private DateTime? _dtWhenLastAttemptToTokenWriteWasMade = null;
        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::CorrectMediaAppeared _statusDone = " + _statusDone.ToString());
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
                        _statusDone = Status.NotInitiated;
                        return ResultLastAttempt.MediaNotFoundFitForOperation;
                    }
                }
                else
                {

                    _physicalDataInTokenPriorToOperation = new byte[64];
                    Array.Copy(logMediaNow._tokenPhysicalData, _physicalDataInTokenPriorToOperation, logMediaNow._tokenPhysicalData.Length);
                    _macOfSelectedTokenPriorToAnyOperationAttempted = logMediaNow.DelhiUltralightRaw.MAC;
                    //_logicalMediaOfTokenPriorToOperation = logMediaNow;
                }
            }
            else
            {
                _ticketingRules.TreatmentOnCardDetection2(false, true);

                TTErrorTypes err = _ticketingRules.ErrorForJustProducedMedia;
                if (err == TTErrorTypes.CannotReadTheCard || err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                    return ResultLastAttempt.MediaCouldntBeRead;
                else if (!EvaluateIfMediaIsYetFitForOperation())
                {
                    if (_statusDone == Status.Success)
                    {
                        Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::CorrectMediaAppeared Exit A");
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                    else
                    {
                        Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::CorrectMediaAppeared Exit B");
                        _completionStatus = MediaUpdateCompletionStatus.MediaNoMoreFitForOperation;
                        return ResultLastAttempt.MediaNoMoreFitForOperation;
                    }
                }
            }

            DelhiTokenUltralight ul = new DelhiTokenUltralight();

            _logicalMediaReference.Media.ChipSerialNumber = _mediaSrNbr;
            // TODO: To be more precise, may edit the sale date too.
            CSC_READER_TYPE ReaderType;
            int hRw;
            _ticketingRules.GetReaderHandle(out ReaderType, out hRw);
            var dataRead = _ticketingRules.GetLogicalDataOfMediaAtFront()._tokenPhysicalData;

            // It has been messed that initialization date is being stored in multiple nodes of the logical media tree. It is bad. Needs cleanup.
            if (logMediaNow.Application.TransportApplication.Status == TransportApplication.StatusValues.NotInitialised)            
                _logicalMediaReference.Media.InitialisationDate 
                    = _logicalMediaReference.Application.TransportApplication.InitialisationDate
                    = _logicalMediaReference.Initialisation.DateTime
                 = DateTime.Now;
            else
            {
                _logicalMediaReference.Media.InitialisationDate = logMediaNow.Media.InitialisationDate;
                _logicalMediaReference.Application.TransportApplication.InitialisationDate = logMediaNow.Media.InitialisationDate;
                _logicalMediaReference.Initialisation.DateTime = logMediaNow.Media.InitialisationDate;
            }
            
            byte[] cb = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocks(SharedData.TokenLayoutVersion, _logicalMediaReference, dataRead, out _macAttemptedToBeWrittenInLastAttempt));
            
            
            bool bSuccess;
            CSC_API_ERROR ErrWriting = ((DelhiTokenUltralight)_ticketingRules.hwToken).WriteToToken(cb, out bSuccess);
            _dtWhenLastAttemptToTokenWriteWasMade = DateTime.Now;
            if (ErrWriting != CSC_API_ERROR.ERR_NONE || !bSuccess)
            {
                Logging.Log(LogLevel.Verbose, "MediaOpReqTokenIssue::CorrectMediaAppeared " + ErrWriting.ToString());
                _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
            }

            if (ErrWriting == CSC_API_ERROR.ERR_NONE && bSuccess)
            {
                _statusDone = Status.Success;
                _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::CorrectMediaAppeared Exit C");
                return ResultLastAttempt.Success;
            }
            else
            {
                _statusDone = Status.WrittenButFailed;
                _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::CorrectMediaAppeared Exit D");
                return ResultLastAttempt.MediaCouldntBeWritten;
            }
        }

        enum MediaFitForCancellation { YES, NO, HADFOUND_MEDIANOMOREFITFOROPERTION_WHILEWRITING};
        private MediaFitForCancellation EvaluateIfMediaIsFitForCancellation()
        {
            LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
            var macNow = logMediaAtFront.DelhiUltralightRaw.MAC;
            switch (_completionStatus)
            {
                case MediaUpdateCompletionStatus.MediaNoMoreFitForOperation:
                    return MediaFitForCancellation.HADFOUND_MEDIANOMOREFITFOROPERTION_WHILEWRITING;
                case MediaUpdateCompletionStatus.DoneSuccessfully:
                    if ((ulong)macNow == _macAttemptedToBeWrittenInLastAttempt 
                        && _logicalMediaReference.Media.SequenceNumberRead == logMediaAtFront.Application.TransportApplication.SequenceNumberRead)
                        return MediaFitForCancellation.YES;
                    else
                        return MediaFitForCancellation.NO;
                case MediaUpdateCompletionStatus.DeclaredByMMINotToPerformPostWTE:
                    if ((ulong)macNow != _macAttemptedToBeWrittenInLastAttempt && macNow != _macOfSelectedTokenPriorToAnyOperationAttempted)
                        return MediaFitForCancellation.NO;
                    else
                        return MediaFitForCancellation.YES;
                default:
                    Debug.Assert(false);
                    Logging.Log(LogLevel.Error, "EvaluateIfMediaIsFitForCancellation Unexpected state: " + _completionStatus.ToString());
                    throw new Exception("EvaluateIfMediaIsFitForCancellation " + _completionStatus.ToString());
            }
        }

        private bool EvaluateIfMediaIsYetFitForOperation()
        {
            // if token appears within 3 seconds of last attempt, simply assueme that it is fit. It is done because on at least two occassions mac was not found to be same as 
            // either the updated mac and mac before writing.

            if (_dtWhenLastAttemptToTokenWriteWasMade != null)
                if (DateTime.Now - (DateTime)_dtWhenLastAttemptToTokenWriteWasMade < new TimeSpan(0, 0, 3))
                    return true;

            LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
            if (_statusDone == Status.WrittenButFailed)
            {
                // TODO: see if we want more checks
                //if (logMediaNow.Application.LocalLastAddValue.EquipmentNumber == SharedData.EquipmentNumber)
                Debug.Assert(_macOfSelectedTokenPriorToAnyOperationAttempted  != null);
                if (logMediaAtFront.DelhiUltralightRaw.MAC != _macOfSelectedTokenPriorToAnyOperationAttempted && logMediaAtFront.DelhiUltralightRaw.MAC != (long)_macAttemptedToBeWrittenInLastAttempt)
                {
                    _statusDone = Status.Success;
                    _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                    
                    String str = String.Format("logMediaAtFront.DelhiUltralightRaw.MAC = {0} _macOfSelectedTokenPriorToAnyOperationAttempted = {1} _macAttemptedToBeWrittenInLastAttempt = {2}", 
                        logMediaAtFront.DelhiUltralightRaw.MAC,
                        ((long)_macOfSelectedTokenPriorToAnyOperationAttempted).ToString("X2"),
                        _macAttemptedToBeWrittenInLastAttempt.ToString("X2"));

                    Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::EvaluateIfMediaIsFitForOperation Exit A. It indicates fraud; so we assume token as vended \n" + str);

//                    // DELETE ME AND DON'T LET ME IN PRODUCTION
//#if !WindowsCE && !MonoLinux
//#if DEBUG
//                    System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form() { TopMost = true }, 
//                        "Please preserve this token, and take snapshot of this messsagebox" + Environment.NewLine + str, "Fraud (incorrect)" + DateTime.Now.ToString("hh:mm:ss"));
//#endif
//#endif
                    return false;
                }
                else
                {
                    // even if logMediaNow.DelhiUltralightRaw.MAC == (long)_macAttemptedToBeWrittenInLastAttempt, we still attempt because there are 3 blocks to be written, and mac resides in first block only.
                    Logging.Log(LogLevel.Verbose, "MediaOperationRequestTokenIssue::EvaluateIfMediaIsFitForOperation Exit B");
                    return true;
                }
            }
            else
            {
                throw new Exception("");
            }
        }

        #region IMediaCancellableOp Members

        public Tuple<string, string> GetXmlStringToSendToMMIOnCancellation()
        {
            return Tuple.New(_logicalMediaReference.ToXMLString(), "");
        }

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
                    // TODO: Data that is written below may be incorrect. Check with CS22 TOM, what it should be. But for the moment, it more or less serves our purpose.
                    //byte[] cb = TokenFunctions.GetWriteCmdBuffer(_logicalMediaOfTokenPriorToOperation._tokenPhysicalData);                    
                    bool bSuccess;
                    CSC_READER_TYPE ReaderType;
                    int hRw;
                    _ticketingRules.GetReaderHandle(out ReaderType, out hRw);

                    byte[] cb = new byte[48];
                    //Array.Copy(_logicalMediaOfTokenPriorToOperation._tokenPhysicalData, 16, cb, 0, 48);
                    Array.Copy(_physicalDataInTokenPriorToOperation, 16, cb, 0, 48);
                    CSC_API_ERROR ErrWriting = ((DelhiTokenUltralight)_ticketingRules.hwToken).WriteToToken(TokenFunctions.GetWriteCmdBuffer(cb), out bSuccess);
                    if (ErrWriting != CSC_API_ERROR.ERR_NONE || !bSuccess)
                    {
                        Logging.Log(LogLevel.Verbose, "MediaOpReqTokenIssue::CorrectMediaForCancellationAppeared " + ErrWriting.ToString());
                        _lastCancelAttempt = ResultLastCancelAttempt.MediaCouldntBeWritten;
                    }
                    else
                        _lastCancelAttempt = ResultLastCancelAttempt.Success;
                    break;
            }
            return _lastCancelAttempt;
        }

        public MediaOpGen.ResultLastCancelAttempt GetLastCancelAttempt()
        {
            return _lastCancelAttempt;
        }

        MediaOpGen.ResultLastCancelAttempt _lastCancelAttempt = ResultLastCancelAttempt.None;

        #endregion
    }
}