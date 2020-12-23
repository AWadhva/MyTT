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
    class MediaOpReqTokenAdjustment : MediaOpReqPreRegisteration
    {
        public MediaOpReqTokenAdjustment(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, AdjustmentParams opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            _adjParams = opParsXml;            
        }

        AdjustmentParams _adjParams;
        // ignoring fields Journey Management and status from older code, because they make sense for RJT, but strangly they are included in code for family #10 too.
        // See, if the understanding is incorrect, then include that.

        enum Status { NotInitiated, WrittenButFailed, Success };
        Status _statusDone = Status.NotInitiated;

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusDone != Status.NotInitiated);
        }

        DateTime? _dtOfLastBadWriteAttempt;
        LogicalMedia _logicalMediaUpdatedForCurrentOp;
        
        public override MediaOpType GetOpType()
        {
            return MediaOpType.Adjustment;
        }

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            _logicalMediaUpdatedForCurrentOp.DelhiUltralightRaw.Hidden = true;
            return Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), (string)null);
        }
        
        ulong _macAttemptedToBeWrittenInLastAttempt = 0; 

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOpReqTokenAdjustment::CorrectMediaAppeared _statusDone = " + _statusDone.ToString());
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;

            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
                return ResultLastAttempt.MediaCouldntBeRead;

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

            _logicalMediaUpdatedForCurrentOp.Application.Validation.RejectCode = (short)_adjParams._RejectCodeOnMediaPostAdjustment;


            if (_adjParams._entryExitBitPostAdjustment != null)
            {
                if ((int)_adjParams._entryExitBitPostAdjustment == CONSTANT.MBC_GateExit)
                    _logicalMediaUpdatedForCurrentOp.Application.Validation.EntryExitBit = Validation.TypeValues.Exit;
                else if ((int)_adjParams._entryExitBitPostAdjustment == CONSTANT.MBC_GateEntry)
                    _logicalMediaUpdatedForCurrentOp.Application.Validation.EntryExitBit = Validation.TypeValues.Entry;
                else
                    Debug.Assert(false);
            }

            if (_adjParams._fareTierPostAdjustment != null)
                _logicalMediaUpdatedForCurrentOp.Application.LocalLastAddValue.FareTiers = (short)_adjParams._fareTierPostAdjustment;
            if (_adjParams._entryExitStationCodePostAdjustment != null)
                _logicalMediaUpdatedForCurrentOp.Application.Validation.Location = (int)_adjParams._entryExitStationCodePostAdjustment;

            _logicalMediaUpdatedForCurrentOp.Application.Validation.LastTransactionDateTime
                = _logicalMediaUpdatedForCurrentOp.Application.LocalLastAddValue.DateTime
                = DateTime.Now;
            
            byte[] cb = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocks(_logicalMediaUpdatedForCurrentOp, logMediaAtFront._tokenPhysicalData, out _macAttemptedToBeWrittenInLastAttempt));
            CSC_READER_TYPE readerType;
            int hRW;
            _ticketingRules.GetReaderHandle(out readerType, out hRW);
            
            bool bSuccess;
            CSC_API_ERROR ErrWriting = ((DelhiTokenUltralight)_ticketingRules.hwToken).WriteToToken(cb, out bSuccess);
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

        private bool EvaluateIfMediaIsFitForOperation()
        {            
            LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
            switch (_statusDone)// || _statusDone == Status.WrittenButFailed)
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
                            Logging.Log(LogLevel.Verbose, "MediaOpReqTokenAdjustment::EvaluateIfMediaIsFitForOperation Exit A. It indicates fraud; so we assume token as vended");
                            return false;
                        }
                        else
                        {
                            // even if logMediaNow.DelhiUltralightRaw.MAC == (long)_macAttemptedToBeWrittenInLastAttempt, we still attempt because there are 3 blocks to be written, and mac resides in first block only.
                            Logging.Log(LogLevel.Verbose, "MediaOpReqTokenAdjustment::EvaluateIfMediaIsFitForOperation Exit B");
                            return true;
                        }
                    }
                default:
                    throw new Exception();
            }
        }
    }
}
