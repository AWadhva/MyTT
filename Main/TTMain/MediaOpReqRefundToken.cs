using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Diagnostics;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    class MediaOpReqRefundToken : MediaOpReqPreRegisteration
    {
        public MediaOpReqRefundToken(MainTicketingRules parent, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(parent, snum, mediaDataPriorToOperation)
        {            
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.Refund;
        }

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            _logicalMediaUpdatedForCurrentOp.DelhiUltralightRaw.Hidden = true;
            return Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), (string)null);
        }

        private DateTime? _dtOfLastBadWriteAttempt;
        LogicalMedia _logicalMediaUpdatedForCurrentOp;

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
            switch (_statusDone)
            {
                case Status.NotInitiated:
                    {
                        return (_logicalMediaPriorToOperation.DelhiUltralightRaw.MAC == logMediaAtFront.DelhiUltralightRaw.MAC);
                    }
                case Status.WrittenButFailed:
                    {
                        if (logMediaAtFront.DelhiUltralightRaw.MAC != _logicalMediaPriorToOperation.DelhiUltralightRaw.MAC && logMediaAtFront.DelhiUltralightRaw.MAC != (long)_macAttemptedToBeWrittenInLastAttempt)
                        {
                            _statusDone = Status.Success;
                            _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                            Logging.Log(LogLevel.Verbose, "MediaOpReqRefundToken::EvaluateIfMediaIsFitForOperation Exit A. It indicates fraud; so we assume token as vended");
                            return false;
                        }
                        else
                        {
                            // even if logMediaNow.DelhiUltralightRaw.MAC == (long)_macAttemptedToBeWrittenInLastAttempt, we still attempt because there are 3 blocks to be written, and mac resides in first block only.
                            Logging.Log(LogLevel.Verbose, "MediaOpReqRefundToken::EvaluateIfMediaIsFitForOperation Exit B");
                            return true;
                        }
                    }
                default:
                    throw new Exception();
            }
        }
        ulong _macAttemptedToBeWrittenInLastAttempt = 0;
        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOpReqRefund::CorrectMediaAppeared _statusDone = " + _statusDone.ToString());
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;
            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
            {
                return ResultLastAttempt.MediaCouldntBeRead;
            }
            LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
            if (!EvaluateIfMediaIsFitForOperation())
            {
                if (_statusDone == Status.Success)
                {
                    return ResultLastAttempt.Success;
                }
                else
                {
                    return ResultLastAttempt.MediaNoMoreFitForOperation;
                }
            }

            _logicalMediaUpdatedForCurrentOp = new LogicalMedia(_logicalMediaPriorToOperation.ToXMLString());
            _logicalMediaUpdatedForCurrentOp.DelhiUltralightRaw.Hidden = true;

            CommonHwMedia hwToken = _ticketingRules.hwToken;
            if (!SalesRules.RefundUpdateToken(_logicalMediaUpdatedForCurrentOp))
                return ResultLastAttempt.MediaCouldntBeRead;

            byte[] cb = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocks(SharedData.TokenLayoutVersion, _logicalMediaUpdatedForCurrentOp, logMediaAtFront._tokenPhysicalData, out _macAttemptedToBeWrittenInLastAttempt));
            CSC_READER_TYPE readerType;

            int hRW;
            _ticketingRules.GetReaderHandle(out readerType, out hRW);
            bool bSuccess;
            CSC_API_ERROR ErrWriting = ((DelhiTokenUltralight)hwToken).WriteToToken(cb, out bSuccess);
            if (ErrWriting == CSC_API_ERROR.ERR_NONE && bSuccess)
            {
                _statusDone = Status.Success;
                _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                return ResultLastAttempt.Success;
            }
            else
            {
                _dtOfLastBadWriteAttempt = _logicalMediaUpdatedForCurrentOp.Application.Validation.LastTransactionDateTime;
                _statusDone = Status.WrittenButFailed;
                _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                return ResultLastAttempt.MediaCouldntBeWritten;
            }
        }

        enum Status { NotInitiated, WrittenButFailed, Success };
        Status _statusDone = Status.NotInitiated;

        public override bool bIsOpCompletedEvenPartly()
        {
            // only fully completed opearation would be accounted because it is outflow of money from system.
            return (_statusDone == Status.Success);
        }
    }
}
