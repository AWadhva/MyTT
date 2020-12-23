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
    public abstract class MediaOpGen : IUpdateMediaOp
    {
        static public readonly List<MediaOpType> _OpTypesWithPreRegisterationNotRequired
            = new List<MediaOpType>() { MediaOpType.CSTIssue, MediaOpType.CSCIssue, MediaOpType.TTagIssue, MediaOpType.TTagUpdate };
        protected MediaUpdateCompletionStatus _completionStatus = MediaUpdateCompletionStatus.NotDone;
        public long _mediaSrNbr = 0;
        protected readonly MainTicketingRules _ticketingRules;
        protected MainTicketingRules.MediaOperationsRequested _txn;
        
        public MediaOpGen(
            MainTicketingRules parent
            )
        {
            _ticketingRules = parent;
        }

        public void SetTxn(MainTicketingRules.MediaOperationsRequested txn)
        {
            _txn = txn;
        }

        private bool _bIsAudited = false;

        public bool IsAudited()
        {
            return _bIsAudited;
        }
        public void SetAudited()
        {
            //Debug.Assert(!_bIsAudited);
            _bIsAudited = true;
        }

        static internal IUpdateMediaOp GetMediaOperationRequest_ReqdPreReg(
            MainTicketingRules _this,
            MediaOpType opTypeRequested,
            long mediaSerialNumber,
            string mediaLogicalDataPriorToWriting,
            string opParsXml
            )
        {
            MediaOpGen req;
            switch (opTypeRequested)
            {
                case MediaOpType.SettleBadDebt:
                    req = new MediaOpReqSettleBadDebt(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                    break;
                case MediaOpType.CSCSurrender:
                    req = new MediaOpReqCSCSurrender(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                    break;
                case MediaOpType.AddValue:
                    req = new MediaOpReqAddValue(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                    break;
                case MediaOpType.BankTopupPerform:
                    req = new MediaOpReqPeformAutoTopup(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                    break;
                case MediaOpType.EnableAutoTopup:
                    req = new MediaOpReqEnableAutoTopup(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                    break;
                case MediaOpType.DisbleAutoTopup:
                    req = new MediaOpReqDisableAutoTopup(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                    break;
                case MediaOpType.CSCReplacemnt:
                    req = new MediaOpReqCSCReplacement(_this, opParsXml);
                    break;
                case MediaOpType.Refund:
                    {
                        LogicalMedia logicalMedia = new LogicalMedia(mediaLogicalDataPriorToWriting);
                        
                        switch (logicalMedia.Media.Type)
                        {
                            case Media.TypeValues.Token:
                                {
                                    req = new MediaOpReqRefundToken(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                                    break;
                                }
                            case Media.TypeValues.CSC:
                                {                                    
                                    req = new MediaOpReqRefundCSC(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), SerializeHelper<RefundParams>.XMLDeserialize(XDocument.Parse(opParsXml).Root.Value));
                                    break;
                                }
                            default:
                                {
                                    throw new Exception("Unexpected media type");
                                }
                        }
                        break;
                    }
                case MediaOpType.Adjustment:
                    {
                        LogicalMedia logicalMedia = new LogicalMedia(mediaLogicalDataPriorToWriting);
                        AdjustmentParams adjPars = AdjustmentParams.FromXmlString(opParsXml);
                        switch (logicalMedia.Media.Type)
                        {
                            case Media.TypeValues.Token:
                                {
                                    req = new MediaOpReqTokenAdjustment(_this, mediaSerialNumber, logicalMedia, adjPars);
                                    break;
                                }
                            case Media.TypeValues.CSC:
                                {
                                    if (!adjPars._bUsePurse)
                                        req = new MediaOpReqAdjustmentCSCNonPurse(_this, mediaSerialNumber, logicalMedia, adjPars);
                                    else
                                        req = new MediaOpReqAdjustmentCSCUsingPurse(_this, mediaSerialNumber, logicalMedia, adjPars);
                                    break;
                                }
                            default:
                                {
                                    throw new Exception("Unexpected media type");
                                }
                        }
                        break;
                    }
                case MediaOpType.NewProduct:
                    {
                        req = new MediaOpReqNewProduct(_this, mediaSerialNumber, new LogicalMedia(mediaLogicalDataPriorToWriting), opParsXml);
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
            return req;
        }

        abstract public MediaOpType GetOpType();
        private bool _bAtLeastSomethingWasWrittenInLastAttempt = true;
        virtual public bool bAtLeastSomethingMayHaveGotWrittenInLastAttempt() { return _bAtLeastSomethingWasWrittenInLastAttempt; }
        public void SetAtLeastSomethingWasWrittenInLastAttempt() { _bAtLeastSomethingWasWrittenInLastAttempt = true; }
        public void ResetAtLeastSomethingWasWrittenInLastAttempt() { _bAtLeastSomethingWasWrittenInLastAttempt = false; }

        abstract public Tuple<string, string> GetXmlStringToSendToMMI();
        abstract public bool bIsOpCompletedEvenPartly();

        // ResultLastAttempt would not be maintained by anywhere. It would and should simply be lost after call to CorrectMediaAppeared.
        // Client of this CorrectMediaAppeared would to apprise of the status, which MMI can subsequently use to display appropriate messages on screen
        public enum ResultLastAttempt { 
            MediaCouldntBeRead, 
            MediaCouldntBeWritten, // MMI must not make decision whether to generate transaction or not based on this code. It would be made by each individual operation.            
            MediaNoMoreFitForOperation, // for pre-registered ops, it basically checks that the data is same as when it was registered. For non-pre registered, it is contender to be there (after WTE), but for simplicity, we leave it
            // It should come only for fraud cases or where we are expecting that reader has not performed the operation (as per its return code) while it has actually.
            MediaNotFoundFitForOperation, // applicable only for non-pre registered operation.
            Success };
        abstract public ResultLastAttempt CorrectMediaAppeared();

        public enum ResultLastCancelAttempt
        {
            None,
            MediaCouldntBeRead,
            MediaCouldntBeWritten,
            MediaNotFoundFitForCancellation,
            Success
        };

        internal static IUpdateMediaNonPreRegisteredOp GetMediaOperationRequest_NoPreRegReqd(
            MainTicketingRules _this,
            MediaOpType opTypeRequested,
            string logicalMediaReference,
            string parsXml)
        {
            switch (opTypeRequested)
            {
                case MediaOpType.CSCIssue:                    
                        return (new MediaOpReqCSCIssue(_this, parsXml));                    
                case MediaOpType.CSTIssue:                    
                        return (new MediaOpReqTokenIssue(_this, logicalMediaReference, parsXml));                    
                case MediaOpType.TTagIssue:
                        return (new MediaOpReqTTagIssue(_this, logicalMediaReference));
                case MediaOpType.TTagUpdate:
                        return (new MediaOpReqTTagUpdate(_this, logicalMediaReference));
                default:
                    throw new Exception();
            }
        }

        #region IUpdateMediaOp Members        

        public long? GetMediaSerialNumber()
        {
            return _mediaSrNbr;
        }

        public MediaUpdateCompletionStatus GetStatus()
        {
            return _completionStatus;
        }

        public void DeclarePartCompletedAsDone()
        {
            // media updated info (i.e the logical data, and xdr for cchs is communicated on first wte, if it occurred.
            // if it is RTE, we need not worry anyways.
            Debug.Assert(_completionStatus == MediaUpdateCompletionStatus.DoneWithWTE                 
                || _completionStatus == MediaUpdateCompletionStatus.NotDone);

            // Treatment of RTE for non-preregistered operations is different. There we have to junk the old object and create the new object, and put it in list at right place.
            // There are very little chance where agent actually wants the pre-registered operation be aborted, and proceed with next operation
            if (_completionStatus == MediaUpdateCompletionStatus.NotDone)
            {
                if (this is MediaOpReqPreRegisteration)
                    _completionStatus = MediaUpdateCompletionStatus.DeclaredByMMINotToPerformPostRTE;
                else
                    _completionStatus = MediaUpdateCompletionStatus.NotDone;
            }
            else
                _completionStatus = MediaUpdateCompletionStatus.DeclaredByMMINotToPerformPostWTE;
        }

        #endregion    
    }
}
