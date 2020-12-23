using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules
    {
        private void Handle_UpdateMedia(EventMessage eventMessage)
        {
            try
            {
                Debug.Assert(_mediaUpdate == null);
                Debug.Assert(!IsMediaDistributionInProgress());
                if (SharedData.EquipmentType == EquipmentFamily.TOM)
                    Debug.Assert(SharedData._agentShift != null);

                if (_mediaUpdate != null)
                {
                    Logging.Log(LogLevel.Error, "_mediaUpdate is not null");
                    return;
                }

                string requestsXml = eventMessage._par[0];
                
                _mediaUpdate = new MediaOperationsRequested(requestsXml, this);
                if (eventMessage._par.Length >= 3 && eventMessage._par[1] != "")
                {
                    PaymentMethods paymentMethod = Utility.ParseEnum<PaymentMethods>(eventMessage._par[1]);
                    if (paymentMethod == PaymentMethods.BankCard)
                    {
                        IFS2.Equipment.Common.CCHS.BankTopupDetails bankCardPaymentDetails = SerializeHelper<IFS2.Equipment.Common.CCHS.BankTopupDetails>.XMLDeserialize(eventMessage._par[2]);
                        _mediaUpdate.SetBankCardPaymentDetails(bankCardPaymentDetails);
                    }
                }
                
                if (_mediaUpdate.HasAnyTokenOperation() && !SecurityMgr.Instance.IsTokenKeyLoaded)
                {
                    _mediaUpdate = null;
                    SendMsg.UpdateMediaTerminated();
                    return;
                }
                _mediaUpdate.SeeForInitiatingNewOperation();
            }
            catch
            { }
        }

        private void Handle_UpdateMediaAbortTxn()
        {
            if (_mediaUpdate == null)
            {
                // Generally control doesn't reach here. It happens often on dev machine, where TT is reinvoked, w/o need to restart MMI
                SendMsg.UpdateMediaTerminated();
                return;
            }
            
            _mediaUpdate.AbortTransaction();
            if (_mediaUpdate.IsConcluded())
            {
                SendMsg.UpdateMediaTerminated();
                SetMediaUpdateConcluded();                
                return;
            }
        }

        public class OriginalRequestId
        {
            public int _opIdx;
            public int _subIdx;            

            public OriginalRequestId(int opIdx, int subIdx)
            {
                _opIdx = opIdx;
                _subIdx = subIdx;                
            }
        }
        
        public class MediaOperationsRequested
        {
            private List<Tuple<IUpdateMediaOp, OriginalRequestId>> _ops = new List<Tuple<IUpdateMediaOp, OriginalRequestId>>();
            Dictionary<IUpdateMediaOp, int> _indicesInSubmittedRequest = new Dictionary<IUpdateMediaOp, int>();            

            public enum PutTokenUnderRWStatus { NotSent, SentButWaitingForAck, AckReceived };

            bool _bLastMediaNotFoundFitForOperationAckedAtLeastOnceForGivenMediaInThisCycleOfMediaPlacementAndRemoval = false;
            public int? _idxCurOpSelected = null;
            // In beginning its value would be -1, if there is at least 1 (or even only 1) pre-registered required operation.
            // As the media would be produced, it would be scanned through the list of medias, which are pre-registered.
            // In case one is found, _idxCurOpSelected is set accordingly. After completion of a pre-registered required operation,
            // if at least one (or even only one) is left, its value is set to -1.
            // In case all pre-registered required operation are complete or declared by agent that proceed anyways or found that media's current data is different than pre-registered data,
            // or, there were none
            // then _idxCurOpSelected would be set to the first "Not-pre register Non-token dispenser", the media number would be set immediatly after a card is produced and read, such that 
            // it fulfills the criterion e.g. for issueing a card, if a token is produced, or a card which is already issued is produced, that would NOT qualify and hence MediaNumber would
            // not be set. But once, it is set, all writing would be targetted only towards that.
            
            private long? _lastMediaAskedByTTToMMIForRemoval = null;            
            
            // Only medias, which TT want them to be removed. It includes, a. successful writing b. Media Not found fit for operation c. Media no more fit for operation
            // It MUST NOT include RTE and WTE on media treatment.
            // These medias when appear again within a time limit (typically 3-5 seconds), would be halted, and we will ask reader to see if there is any other media in
            // its vicinity on which update operation can be performed.            

            
            public bool _bAskedMMIToRemoveNonDetectableMedia = false;            

            public bool _bAbortRequested = false;

            public bool HasAnyTokenOperation()
            {
                foreach (var op in _ops)
                {
                    IUpdateMediaOp typ = op.First;
                    if (typ is MediaOpReqRefundToken || typ is MediaOpReqTokenAdjustment || typ is MediaOpReqTokenIssue)
                        return true;                    
                }
                return false;
            }

            private void SetCurIndex(int idx)
            {
                Logging.Log(LogLevel.Verbose, " SetCurIndex = " + idx);
                _idxCurOpSelected = idx;
                if (idx == -1)
                    return;

                SetPutTokenUnderRWStatus(PutTokenUnderRWStatus.NotSent);
                var op = _ops[idx];
                SendMsg.UpdateMedia_InitiatingNewOp(op.Second._opIdx, op.Second._subIdx);
                MediaDetectionTreatment readPurpose;
                switch (op.First.GetOpType())
                {
                    case MediaOpType.NewProduct:
                        readPurpose = MediaDetectionTreatment.TOM_PutNewProductInExistingMedia; // TODO: better to use TOM_PutNewProductInExistingMedia_Check
                        break;
                    case MediaOpType.CSCSurrender:
                        readPurpose = MediaDetectionTreatment.TOM_CSCSurrender;
                        break;
                    case MediaOpType.SettleBadDebt:
                        readPurpose = MediaDetectionTreatment.TOM_SettleBadDebt;
                        break;
                    case MediaOpType.AddValue:
                        readPurpose = MediaDetectionTreatment.TOM_AnalysisForAddVal_Check;
                        break;
                    case MediaOpType.Adjustment:
                        if (op.First is MediaOpReqAdjustmentCSCNonPurse || op.First is MediaOpReqTokenAdjustment)
                            readPurpose = MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check;
                        else if (op.First is MediaOpReqAdjustmentCSCUsingPurse)
                            readPurpose = MediaDetectionTreatment.TOM_AnalysisForAdjustment_Purse_Check;
                        else
                            throw new NotImplementedException();
                        break;
                    case MediaOpType.EnableAutoTopup:
                        readPurpose = MediaDetectionTreatment.TOM_AutoTopupActivation;
                        break;
                    case MediaOpType.DisbleAutoTopup:
                        readPurpose = MediaDetectionTreatment.TOM_AutoTopupDeActivation;
                        break;
                    case MediaOpType.BankTopupPerform:
                        readPurpose = MediaDetectionTreatment.TOM_AutoTopupPerform;
                        break;
                    case MediaOpType.CSCIssue:
                    case MediaOpType.CSCReplacemnt:
                        readPurpose = MediaDetectionTreatment.TOM_AnalysisForCSCIssue;
                        break;
                    case MediaOpType.CSTIssue:                        
                        readPurpose = MediaDetectionTreatment.TOM_AnalysisForTokenIssue;
                        break;
                    case MediaOpType.TTagIssue:
                        readPurpose = MediaDetectionTreatment.AnalyisForTTagIssue;
                        break;
                    case MediaOpType.TTagUpdate:
                        readPurpose = MediaDetectionTreatment.AnalyisForTTagUpdate;
                        break;
                    case MediaOpType.Refund:
                        readPurpose = MediaDetectionTreatment.TOM_AnalysisForRefund_Check;
                        break;
                    default:
                        throw new Exception("Unexpected optype");                        
                }
                _ticketingRules._readDataFor = readPurpose;
            }

            PutTokenUnderRWStatus _putTokenUnderRWStatus;

            int? FindNextIndex()
            {
                Debug.Assert(_idxCurOpSelected == null);                

                if (_idxCurOpSelected != null)
                {
                    Logging.Log(LogLevel.Error, "Aborting transaction on own because of inconsitent state");
                    AbortTransaction();
                    throw new Exception("Just aborted");
                }
                if (_bAbortRequested)
                {
                    return null;
                }

                // First preference to token dispenser operations
                int IdxOpRequiringTokenDispenser = _ops.FindIndex(x => x.First is IUpdateMediaNonPreRegisteredOp
                    && ((IUpdateMediaNonPreRegisteredOp)(x.First)).DoesNeedTokenDispenser()
                    && (x.First.GetStatus() == MediaUpdateCompletionStatus.NotDone                     
                    )
                    && !_bCompleteAFMOpUsingLooseTokens);
                
                if (IdxOpRequiringTokenDispenser != -1)
                {
                    if (!SharedData.IsDispenserAvailable)
                    {
                        _blockedOnAFMOpBecauseAFMIsNotAvailable = true;
                        return -2;
                    }
                    else
                        return IdxOpRequiringTokenDispenser;
                }

                // Second preference to Pre-registered operations
                if (_ops.Exists(x => !(x.First is IUpdateMediaNonPreRegisteredOp)
                                         && (x.First.GetStatus() == MediaUpdateCompletionStatus.NotDone
                                         //|| x.First.GetStatus() == MediaUpdateCompletionStatus.DeclaredByMMINotToPerformPostRTE
                                         )))
                {
                    return -1;
                }

                // Finally to non-pre-registered, non-token-dispenser-involving ops.
                int idx = _ops.FindIndex(x => x.First is IUpdateMediaNonPreRegisteredOp
                    && (x.First.GetStatus() == MediaUpdateCompletionStatus.NotDone                    
                    )
                    );

                if (idx != -1)
                    return idx;
                
                return null;
            }

            void AskTokenDispenserToDispenseToken_TOM()
            {
                Logging.Log(LogLevel.Verbose, "AskTokenDispenserToDispenseToken_TOM");
                Debug.Assert(_putTokenUnderRWStatus == PutTokenUnderRWStatus.NotSent);                

                SendMsg.PutTokenUnderRW();
                SetPutTokenUnderRWStatus(PutTokenUnderRWStatus.SentButWaitingForAck);
                _ticketingRules.StartDelay((int)Timers.PutMediaOverRW, Config.nTimeOutInMilliSecForPutTokenUnderRWCompletion);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns>true, if it wants that polling should be started; else false. 
            /// At the beginning of function, reader is in ST_INIT.
            /// At the end of this function, we remain at ST_INIT, though within the execution, it may shift to polling, but eventually when
            /// it leaves, it is ST_INIT. Its client, would initiate polling on the basis of the return value of the function</returns>
            public bool MediaRemoved()
            {
                Logging.Log(LogLevel.Verbose, "MediaRemoved");
                
                if (_bWaitingForAgentAckAboutMediaNotFoundFitForOperation)
                {
                    Logging.Log(LogLevel.Verbose, "_bWaitingForAgentAckAboutMediaNotFoundFitForOperation = true");
                    if (!_bLastMediaNotFoundFitForOperationAckedAtLeastOnceForGivenMediaInThisCycleOfMediaPlacementAndRemoval)
                    {
                        Logging.Log(LogLevel.Verbose, "_bLastMediaNotFoundFitForOperationAckedAtLeastOnceForGivenMediaInThisCycleOfMediaPlacementAndRemoval = false");
                        return false; // we will not get anything by polling at this stage.
                    }
                }

                if (_lastMediaAskedByTTToMMIForRemoval != null)
                {
                    SendMsg.AskedMediaRemoved();

                    // it implies that this list would now contain only single element at the most. IT HAD TO BE DONE IN RESPONSE TO BUG; IT IS NO ACCIDENTAL CODE.
                    // it also would affect _mediasHalted, which would now contain at most one element.

                    _ticketingRules._reader.ClearIgnoreList(); // though it is not required, because we have already done ClearIgnoreList in TTMain.MediaRemoved
                    _ticketingRules._reader.AddMediaToIgnoreList((long)_lastMediaAskedByTTToMMIForRemoval);
                    _ticketingRules.StartTimer(Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList);

                    SetlastMediaAskedByTTToMMIForRemoval(null);
                }

                if (_bWaitingForSomeMediaToArrivePostRTEOrWTE)
                {
                    // More appropriate assertion is that it is unreachable when _reader.HasNativeSupportOfDetectionRemoval() == false
                    if (_ticketingRules._reader is V3Reader)
                        Debug.Assert(false);
                    
                    _ticketingRules.StartTimer(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE);
                    return true;
                }
                
                //Thread.Sleep(Config._nTimeToSleepAfterRemovalOfMediaToPollAgain);

                if (_idxCurOpSelected == null)
                {
                    SeeForInitiatingNewOperation();
                    if (_idxCurOpSelected != null)
                    {
                        if (DoesCurrentOpInolvesTokenDispenser() && _putTokenUnderRWStatus == PutTokenUnderRWStatus.SentButWaitingForAck)
                        {
                            Logging.Log(LogLevel.Information, "MediaRemoved returning since _putTokenUnderRWStatus == PutTokenUnderRWStatus.SentButWaitingForAck");                            
                            return false;
                        }
                    }
                }
                else
                {
                    if (DoesCurrentOpInolvesTokenDispenser() 
                        && GetCurrentOp().GetStatus() == MediaUpdateCompletionStatus.NotDone                        
                        && (GetCurrentOp().GetMediaSerialNumber() == null || GetCurrentOp().GetMediaSerialNumber() == 0) // TODO: Remove this confusion and take either null or 0 for denoting that no media is selected
                        )
                    {
                        if (_putTokenUnderRWStatus == PutTokenUnderRWStatus.AckReceived)
                            return true;
                        else
                        {
                            DoForInitiatingTokenDispenserOp();
                            return false;
                        }
                    }
                }
                
                if (_lastMediaAskedByTTToMMIForRemoval != null)
                    return false;
                else
                    return true;
            }

            /// <summary>
            /// To be called on
            /// 1. Any new UpdateMedia request
            /// 2. Removal of a media, on 
            /// a. Media conclusion
            /// b. when it is not yet decided which operation is to be performed
            /// </summary>
            public void SeeForInitiatingNewOperation()
            {
                Logging.Log(LogLevel.Verbose, "SeeForInitiatingNewOperation");
                if (!_blockedOnAFMOpBecauseAFMIsNotAvailable)
                {
                    Debug.Assert(_idxCurOpSelected == null);
                }
                int? nextIdx = FindNextIndex();
                #region CodeSanityCheck
                Logging.Log(LogLevel.Verbose, "SeeForInitiatingNewOperation nextIdx = " + (nextIdx==null ? " null" : ((int)nextIdx).ToString()));
                Debug.Assert(nextIdx != null); // Because had it been null, 
                if (nextIdx == null)
                    MarkAsConcludedAndStopInvolvedTimers();
                if (nextIdx == -2)
                {
                    if (_bCompleteAFMOpUsingLooseTokens)
                    {
                        // In the current workflow, it should never reach here.
                        Debug.Assert(false);
                        CompleteAFMOpUsingLooseTokens();
                    }
                    else
                    {                        
                        SendMsg.AskAgentToChooseIfHeWantsToCompleteOperationUsingLooseTokens();
                        _ticketingRules._reader.StopPolling();
                        return;
                    }

                    //Debug.Assert(false);
                    // token dispenser has ceased working. It happened in between the operation got completed, and application is waiting for media removal
                    // We should not reach here, if handler of TokenDispenserMetaStatus(Bad) is handled properly. On receiving that message, we would
                    // abort active UpdateMedia.
                    // TODO: Abort the transaction.
                    return;
                }
                #endregion
                SetCurIndex((int)nextIdx);
                if (DoesCurrentOpInolvesTokenDispenser())                
                    DoForInitiatingTokenDispenserOp();                
                else
                    _ticketingRules._reader.StartPolling();
            }            

            private void DoForInitiatingTokenDispenserOp()
            {
                Logging.Log(LogLevel.Verbose, "DoForInitiatingTokenDispenserOp");
                if (!SharedData.IsDispenserAvailable)
                {
                    _ticketingRules._reader.StopPolling();
                    SendMsg.AskAgentToChooseIfHeWantsToCompleteOperationUsingLooseTokens();

                    Logging.Log(LogLevel.Verbose, "!SharedData.IsDispenserAvailable");

                    return;
                }
                Debug.Assert(!_ticketingRules.IsTimerActive(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE));
                
                SetPutTokenUnderRWStatus(PutTokenUnderRWStatus.NotSent);

                bool bMediaPresent = _ticketingRules._reader.PollForAnyMediaAtMoment_AndPerformActionOnIt_IfNonePresentThenStopPolling(ReaderOp.SwitchToDetectRemoval_RealTime, null);
                Logging.Log(LogLevel.Verbose, "bMediaPresent = " + bMediaPresent.ToString());
                if (!bMediaPresent)
                {
                    AskTokenDispenserToDispenseToken_TOM();
                    return;
                }
                else
                {
                    if ((_mediaSerialNumberConcludedLast_DontGoStrictlyByMyName == null) || 
                        (long)_mediaSerialNumberConcludedLast_DontGoStrictlyByMyName != _ticketingRules._MediaSerialNbr)
                    {
                        Logging.Log(LogLevel.Verbose, "_mediaSerialNumberConcludedLast_DontGoStrictlyByMyName = " + 
                            (_mediaSerialNumberConcludedLast_DontGoStrictlyByMyName == null? "null" :_ticketingRules.GetIdOfToken((long)_mediaSerialNumberConcludedLast_DontGoStrictlyByMyName).ToString()) +
                            " srNbr = " + _ticketingRules._MediaSerialNbr.ToString());
                        UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.MediaAlreadyPresent);
                        _mediaSerialNumberConcludedLast_DontGoStrictlyByMyName = null;
                    }
                    else
                    {
                        // we need not ask agent to remove it. all we have to do is wait.                        
                        SetlastMediaAskedByTTToMMIForRemoval(_ticketingRules._MediaSerialNbr);
                    }
                    return;
                }
            }

            private long? _mediaSerialNumberConcludedLast_DontGoStrictlyByMyName = null;

            private void SetPutTokenUnderRWStatus(PutTokenUnderRWStatus putTokenUnderRWStatus)
            {
                _putTokenUnderRWStatus = putTokenUnderRWStatus;
            }
            
            // This variable is used to avoid a rare, low priority, bug. If we rely solely on Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE,
            // it may happen that when MediaAppears, we stop the timer (but message is already in queue), and in the same cycle
            // we again start the timer then the timeout message in queue would get processed, while it should not have.
            private bool _bWaitingForSomeMediaToArrivePostRTEOrWTE = false;
            public bool IsWaitingForSomeMediaToArrivePostRTEOrWTE()
            {
                return _bWaitingForSomeMediaToArrivePostRTEOrWTE;
            }

            public void MediaAppeared(
                        SmartFunctions.MediaDetected mediaDetected,
                        long srNbr
                        )
            {
                if (mediaDetected == SmartFunctions.MediaDetected.CARD)
                    _ticketingRules.hwCsc.ResetSelectedAppId();

                if (_bWaitingForSomeMediaToArrivePostRTEOrWTE)
                {
                    SendMsg.SomeMediaAppearedPostRTEOrWTEInLastCycle();
                    _bWaitingForSomeMediaToArrivePostRTEOrWTE = false;
                }

                _bLastMediaNotFoundFitForOperationAckedAtLeastOnceForGivenMediaInThisCycleOfMediaPlacementAndRemoval = false;
                Logging.Log(LogLevel.Verbose, "MediaAppeared 1 " +  Convert.ToString(_ticketingRules.GetIdOfToken(srNbr)));
                if (_ticketingRules.IsTimerActive(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE))
                {
                    _ticketingRules.StopTimer(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE);                    
                    Logging.Log(LogLevel.Verbose, "MediaAppeared 2");
                }

                Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared mediaDetected = " + mediaDetected.ToString()
                    + "srNbr = " + Convert.ToString(_ticketingRules.GetIdOfToken(srNbr)));
                

                // i.e. whether we're doing after RTE/WTE on the media or not.
                #region Determine_bCorrectMediaDetected
                if (DoesCurrentOpRequiresPreRegisteration())
                {
                    // Add value, refund. In fact all CSC operations, except Card Isssue; and CST refund, adjustment operation
                    if (GetCurrentOp() != null)
                    {
                        if (GetCurrentOp().GetMediaSerialNumber() != srNbr)
                        {
                            UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.IncorrectMedia);
                            return;
                        }
                    }
                    else
                    {
                        // i.e. we are waiting for a media from one among the list of pre-registered medias to appear
                        int idx = _ops.FindIndex(x => x.First is IUpdateMediaPreRegisteredOp
                            && x.First.GetStatus() == MediaUpdateCompletionStatus.NotDone
                            && x.First.GetMediaSerialNumber() == srNbr
                            );
                        if (idx != -1)
                        {
                            // Got the correct media, from the list which was registered.
                            SetCurIndex(idx);
                        }
                        else
                        {
                            UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.IncorrectMedia);
                            return;
                        }
                    }
                }                
                else
                {
                    bool bCurrentMediaRegisteredForCurOp = DoesCurrentOpEverAttemptedWithCurrentMedia();
                    // Card Issue or Token Issue
                    Debug.Assert(_idxCurOpSelected >= 0);
                    Debug.Assert(_ops[(int)_idxCurOpSelected].First is IUpdateMediaNonPreRegisteredOp);

                    if (!bCurrentMediaRegisteredForCurOp)
                    {
                        Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B1");
                        if (DoesCurrentOpInolvesTokenDispenser())
                        {
                            Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B11");
                            switch (_putTokenUnderRWStatus)
                            {
                                case PutTokenUnderRWStatus.NotSent:
                                    {
                                        var curOp = GetCurrentOp();
                                        if (curOp.GetMediaSerialNumber() == 0 || curOp.GetMediaSerialNumber() == null)
                                        {
                                            Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B010");
                                            UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.MediaAlreadyPresent);
                                        }
                                        else
                                        {
                                            Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B011");
                                            UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.IncorrectMedia);
                                        }

                                        return;
                                    }
                                case PutTokenUnderRWStatus.AckReceived:
                                    {
                                        Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B111");

                                        if (!Config.bTokenFallsDirectlyOnReader)
                                        {
                                            if (_ticketingRules.WasMediaTreatedRecently(srNbr))
                                            {
                                                UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.MediaAlreadyTreatedRecently);
                                                // Letting _putTokenUnderRWStatus as it is, so that when agent puts the media dispensed by dispenser, we treat it smoothly
                                                return;
                                            }
                                        }

                                        if (_ticketingRules._reader.GetMediaDetected() != SmartFunctions.MediaDetected.TOKEN)
                                        {
                                            UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.NotAToken);
                                            // Letting _putTokenUnderRWStatus as it is, so that when agent puts the media dispensed by dispenser, we treat it smoothly
                                            return;
                                        }
                                        ((IUpdateMediaNonPreRegisteredOp)_ops[(int)_idxCurOpSelected].First).SetMediaSerialNumber(srNbr);                                        
                                        _putTokenUnderRWStatus = PutTokenUnderRWStatus.NotSent;
                                        break;
                                    }
                                case PutTokenUnderRWStatus.SentButWaitingForAck:
                                    {
                                        Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B112");
                                        Debug.Assert(false); // just to check
                                        // scenario when this arises is very rare, because we have stopped the polling, before sending puttokenunderrw 
                                        _ticketingRules._reader.StopPolling();
                                        return;
                                    }
                            }
                        }
                        else
                        {
                            var curOp = _ops[(int)_idxCurOpSelected].First;
                            if (this._ops.Exists(x=> !Object.ReferenceEquals(x, curOp)
                                && x.First.GetStatus() != MediaUpdateCompletionStatus.NotDone
                                && x.First.GetMediaSerialNumber() != null
                                && (long)(x.First.GetMediaSerialNumber()) == srNbr))
                            {
                                UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.MediaAlreadyTreatedRecently);
                                return;
                            }
                            
                            if (curOp.GetMediaSerialNumber() == 0 || curOp.GetMediaSerialNumber() == null)
                            {
                                Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B12");
                                ((IUpdateMediaNonPreRegisteredOp)_ops[(int)_idxCurOpSelected].First).SetMediaSerialNumber(srNbr);                                
                            }
                            else
                            {
                                Logging.Log(LogLevel.Verbose, "UpdateMedia::MediaAppeared B13");
                                UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.IncorrectMedia);
                                
                                return;
                            }
                            // that's another thing that it that the operation may realize (in CorrecteMediaAppeared) that media is not appropriate for the operation
                            // Operation itself would then set it to null
                        }
                    }
                }
                #endregion                

                AttemptOperation(srNbr);
            }            

            private void AttemptOperation(long srNbr)
            {
                var opTot = GetCurrentOpEx();
                IUpdateMediaOp op = opTot.First;
                OriginalRequestId opIndex = opTot.Second;
                MediaOpGen.ResultLastAttempt result = GetCurrentOp().CorrectMediaAppeared();
                Logging.Log(LogLevel.Verbose, "UpdateMedia result = " + result.ToString());

                switch (result)
                {
                    case MediaOpGen.ResultLastAttempt.MediaCouldntBeWritten:
                    case MediaOpGen.ResultLastAttempt.Success:
                        _ticketingRules.SetMediaKeptOnReaderWasLastUpdatedInThisVeryCycle();
                        ReportMMIAboutOperationCompletionOrPartCompletionWhatever();
                        break;
                }

                if (result == MediaOpGen.ResultLastAttempt.MediaNoMoreFitForOperation)
                {
                    // This is certainly not unreachable. Have put assert just to see when this sitution arises
                    Debug.Assert(false);
                }

                switch (result)
                {
                    case MediaOpGen.ResultLastAttempt.MediaCouldntBeRead:
                        {
                            // Taking the advantage that we have only desfire and MiFare. Had there been more, we would create a class for it
                            //SmartFunctions.Instance.GetLastResult(out err, out pSw1, out pSw2);
                            SmartFunctions.MediaDetected mediaDetected = _ticketingRules._MediaDetectedState;
                            bool bOperationFailedBecauseOfMediaDisappearance;
                            if (mediaDetected == SmartFunctions.MediaDetected.TOKEN)
                                bOperationFailedBecauseOfMediaDisappearance = (_ticketingRules.hwToken.GetLastStatus() == CommonHwMedia.Status.Failed_MediaWasNotInField);
                            else if (mediaDetected == SmartFunctions.MediaDetected.CARD)
                                bOperationFailedBecauseOfMediaDisappearance = (_ticketingRules.hwCsc.GetLastStatus() == CommonHwMedia.Status.Failed_MediaWasNotInField);
                            else
                            {
                                Debug.Assert(false);
                                throw new NotSupportedException();
                            }

                            if (bOperationFailedBecauseOfMediaDisappearance)                            
                                _bWaitingForSomeMediaToArrivePostRTEOrWTE = true;                            

                            SendMsg.RTE_Or_WTE(RTE_Or_WTE.RTE, ((long)GetCurrentOp().GetMediaSerialNumber()));

                            break;
                        }
                    case MediaOpGen.ResultLastAttempt.Success:
                        {
                            OperationConcluded();
                            break;
                        }
                    case MediaOpGen.ResultLastAttempt.MediaCouldntBeWritten:
                        {
                            SmartFunctions.MediaDetected mediaDetected = _ticketingRules._MediaDetectedState;
                            bool bOperationFailedBecauseOfMediaDisappearance;
                            if (mediaDetected == SmartFunctions.MediaDetected.TOKEN)
                                bOperationFailedBecauseOfMediaDisappearance = (_ticketingRules.hwToken.GetLastStatus() == CommonHwMedia.Status.Failed_MediaWasNotInField);
                            else if (mediaDetected == SmartFunctions.MediaDetected.CARD)
                                bOperationFailedBecauseOfMediaDisappearance = (_ticketingRules.hwCsc.GetLastStatus() == CommonHwMedia.Status.Failed_MediaWasNotInField);
                            else
                            {
                                Debug.Assert(false);
                                throw new NotSupportedException();
                            }

                            if (bOperationFailedBecauseOfMediaDisappearance)                            
                                _bWaitingForSomeMediaToArrivePostRTEOrWTE = true;
                            
                            SendMsg.RTE_Or_WTE(RTE_Or_WTE.WTE, ((long)GetCurrentOp().GetMediaSerialNumber()));                            

                            break;
                        }
                    case MediaOpGen.ResultLastAttempt.MediaNoMoreFitForOperation:
                        {
                            SendMsg.UpdateMediaOpCantBePerformed(opIndex._opIdx, opIndex._subIdx);
                            OperationConcluded();
                            break;
                        }
                    case MediaOpGen.ResultLastAttempt.MediaNotFoundFitForOperation:
                        {
                            LogicalMedia logMedia = _ticketingRules.GetLogicalDataOfMediaAtFront();
                            SendMsg.MediaNotFoundFitForOperation(_ticketingRules.ErrorForJustProducedMedia, logMedia);
                            SetlastMediaAskedByTTToMMIForRemoval(srNbr);
                            
                            _bWaitingForAgentAckAboutMediaNotFoundFitForOperation = true;
                            //_idxCurOpSelected = null;
                            break;
                        }
                }
            }
            private bool _bWaitingForAgentAckAboutMediaNotFoundFitForOperation = false;
            
            private List<IUpdateMediaOp> UpdateMediaRequestBuilder(XElement requestElem)
            {
                MediaOpType opTypeRequested = (MediaOpType)(Convert.ToInt32(requestElem.Element("MediaOpType").Value));
                bool bMediaWithPriorRegisterationRequired = !MediaOpGen._OpTypesWithPreRegisterationNotRequired.Exists(x => x == opTypeRequested);
                if (bMediaWithPriorRegisterationRequired)
                {
                    long mediaSerialNumber = Convert.ToInt64(requestElem.Element("MediaNum").Value);
                    string logicalDataPriorToOperation = requestElem.Element("LogicalDataInitial").Value;
                    string operationPars = "";
                    if (requestElem.Element("Pars") != null)
                        operationPars = requestElem.Element("Pars").ToString();
                    var request = MediaOpGen.GetMediaOperationRequest_ReqdPreReg(_ticketingRules, opTypeRequested, mediaSerialNumber, logicalDataPriorToOperation, operationPars);
                    ((MediaOpGen)request).SetTxn(this);
                    //TODO: Perform check that a media appears only once.
                    return new List<IUpdateMediaOp> { request };
                }
                else
                {
                    List<IUpdateMediaOp> result = new List<IUpdateMediaOp>();
                    int count = Convert.ToInt32(requestElem.Element("Count").Value);
                    string logicalMediaRef = null;
                    if (opTypeRequested == MediaOpType.CSTIssue || opTypeRequested == MediaOpType.TTagUpdate || opTypeRequested == MediaOpType.TTagIssue)
                        logicalMediaRef = requestElem.Element("LogDataRef").Value;

                    for (int i = 0; i < count; i++)
                    {
                        IUpdateMediaNonPreRegisteredOp request = MediaOpGen.GetMediaOperationRequest_NoPreRegReqd(
                            _ticketingRules,
                            opTypeRequested,
                            logicalMediaRef,
                            requestElem.Element("Pars").ToString());

                        result.Add(request);
                    }
                    return result;
                }
            }

            internal void PutTokenUnderRWAck(bool result) 
            {
                Debug.Assert(_putTokenUnderRWStatus == PutTokenUnderRWStatus.SentButWaitingForAck);
                if (_putTokenUnderRWStatus != PutTokenUnderRWStatus.SentButWaitingForAck)
                {
                    Logging.Log(LogLevel.Information, "PutTokenUnderRWAck returning since _putTokenUnderRWStatus == " + _putTokenUnderRWStatus.ToString());
                    return;
                }
                SetPutTokenUnderRWStatus(PutTokenUnderRWStatus.AckReceived);
                if (!result)
                {
                    if (!_bAbortRequested)
                    {
                        _ticketingRules._reader.StopPolling();
                        SendMsg.AskAgentToChooseIfHeWantsToCompleteOperationUsingLooseTokens();
                    }
                    else
                    {
                        MarkAsConcludedAndStopInvolvedTimers();
                        SendMsg.UpdateMediaTerminated();
                        _ticketingRules.SetMediaUpdateConcluded();                        
                    }
                }
                else
                {
                    Thread.Sleep(Config.nTimeInMilliSecToLetTokenSettleInFieldAfterGettingDispensedFromContainer);

                    _ticketingRules._reader.StartPolling();
                    _ticketingRules.StartTimer(Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck);//, Config._nTimeInMilliSecToLetTokenGettingDetectedAfter_PutTokenUnderRWPositiveAck);
                }
            }

            public bool IsAbortRequested() { return _bAbortRequested; }

            // true: if by the end of the execution, transaction is aborted. 
            // false: otherwise. Only case is when we are waiting for Ack from Token Dispenser.
            public void AbortTransaction()
            {
                Debug.Assert(_bAbortRequested == false);

                bool bTokenTxnWaitingForDispenserResponseInProgress = false;
                if (DoesCurrentOpInolvesTokenDispenser() && _putTokenUnderRWStatus == PutTokenUnderRWStatus.SentButWaitingForAck)
                {
                    bTokenTxnWaitingForDispenserResponseInProgress = true;
                }

                if (bTokenTxnWaitingForDispenserResponseInProgress)
                {
                    _bAbortRequested = true;
                    SendMsg.CancelPutTokenUnderRW();
                    //_ticketingRules.SetWaitingForCancelPutTokenUnderRWAck();
                    _ticketingRules.StartDelay((int)Timers.TimeoutCancelPutMediaOverAntennaAckNotRecvd, 75 * 1000);

                    //return false;
                }
                else
                {
                    var op = GetCurrentOp();
                    if (op != null)
                    {
                        OperationConcluded();
                    }
                    MarkAsConcludedAndStopInvolvedTimers();                    
                }
            }

            internal Tuple<IUpdateMediaOp, OriginalRequestId> GetCurrentOpEx()
            {
                if (_idxCurOpSelected == null || _idxCurOpSelected == -1)
                    return null;
                return _ops[(int)_idxCurOpSelected];
            }

            internal IUpdateMediaOp GetCurrentOp()
            {
                if (_idxCurOpSelected == null || _idxCurOpSelected == -1)
                    return null;
                return _ops[(int)_idxCurOpSelected].First;
            }

            private bool DoesCurrentOpEverAttemptedWithCurrentMedia()
            {
                if (_idxCurOpSelected == -1)
                {
                    Logging.Log(LogLevel.Verbose, "DoesCurrentOpEverAttemptedWithCurrentMedia Exit A");
                    // current op is not selected yet
                    return false;
                }

                var op = GetCurrentOp();
                Debug.Assert(op != null);

                long? srNbr = op.GetMediaSerialNumber();
                Logging.Log(LogLevel.Verbose, "DoesCurrentOpEverAttemptedWithCurrentMedia srNbr = " + srNbr == null ? "null" : (_ticketingRules.GetIdOfToken((long)srNbr)).ToString() + " SmartFunctions.Instance.ReadSNbr = " + SmartFunctions.Instance.ReadSNbr());
                if (srNbr == null || srNbr == 0) // TODO: it is becoming terrible for managing both null and 0. getting error prone.
                {
                    Logging.Log(LogLevel.Verbose, "DoesCurrentOpEverAttemptedWithCurrentMedia Exit B");
                    return false;
                }
                else
                {
                    if ((long)srNbr == _ticketingRules._reader.GetMediaSrNbr())
                    {
                        Logging.Log(LogLevel.Verbose, "DoesCurrentOpEverAttemptedWithCurrentMedia Exit C");
                        return true;
                    }
                    else
                    {
                        Logging.Log(LogLevel.Verbose, "DoesCurrentOpEverAttemptedWithCurrentMedia Exit D");
                        return false;
                    }
                }
            }

            private bool DoesCurrentOpRequiresPreRegisteration()
            {
                if (_idxCurOpSelected == -1)
                    return true;
                if (GetCurrentOp() is IUpdateMediaPreRegisteredOp)
                    return true;
                return false;
            }

            public bool DoesCurrentOpInolvesTokenDispenser()
            {
                //        Debug.Assert(_idxCurOpSelected != null);
                if (_idxCurOpSelected == -1 || _idxCurOpSelected == null)
                    return false;
                var op = _ops[(int)_idxCurOpSelected].First;
                if (!(op is IUpdateMediaNonPreRegisteredOp))
                    return false;

                if (((IUpdateMediaNonPreRegisteredOp)op).DoesNeedTokenDispenser())
                    if (!_bCompleteAFMOpUsingLooseTokens)
                        return true;
                return false;
            }

            private void UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia reasonCodeForAskingAgentToRemoveToken)
            {
                Logging.Log(LogLevel.Verbose, "UpdateMedia_AskAgentToRemoveMedia " + reasonCodeForAskingAgentToRemoveToken.ToString());                
                SendMsg.RemoveMedia(reasonCodeForAskingAgentToRemoveToken);
                SetlastMediaAskedByTTToMMIForRemoval(_ticketingRules._reader.GetMediaSrNbr());
            }

            private void SetlastMediaAskedByTTToMMIForRemoval(long? lastMediaAskedByTTToMMIForRemoval)
            {
                //Logging.Log(LogLevel.Verbose, "SetlastMediaAskedByTTToMMIForRemoval. Stack = \n" + Environment.StackTrace);
                _lastMediaAskedByTTToMMIForRemoval = lastMediaAskedByTTToMMIForRemoval;
                Logging.Log(LogLevel.Verbose, "SetlastMediaAskedByTTToMMIForRemoval " + (_lastMediaAskedByTTToMMIForRemoval != null ? Convert.ToString(_ticketingRules.GetIdOfToken((long)_lastMediaAskedByTTToMMIForRemoval)) : "") /*+ Environment.StackTrace*/);
            }

            private void OperationConcluded()
            {
                Logging.Log(LogLevel.Verbose, "OperationConcluded()");

                SendMsg.UpdateMediaOpConcluded(GetCurrentOpEx().Second._opIdx, GetCurrentOpEx().Second._subIdx);
                SetlastMediaAskedByTTToMMIForRemoval(_ticketingRules._reader.GetMediaSrNbr());

                long? curOpMediaSerialNbr = GetCurrentOp().GetMediaSerialNumber();
                if (curOpMediaSerialNbr != null && curOpMediaSerialNbr != 0)
                {
                    _ticketingRules.AddMediaTreatedRecently((long)(GetCurrentOp().GetMediaSerialNumber()));
                    _mediaSerialNumberConcludedLast_DontGoStrictlyByMyName = GetCurrentOp().GetMediaSerialNumber();
                }
                else
                    _mediaSerialNumberConcludedLast_DontGoStrictlyByMyName = null;

                _idxCurOpSelected = null; // It would be assigned any value only after updated media is removed.

                int? nextIdx = FindNextIndex();
                if (nextIdx == null)
                {
                    SendMsg.UpdateMediaTerminated(); // TODO: Maybe we want to communicate reason code too.
                    MarkAsConcludedAndStopInvolvedTimers();                    
                    return;
                }
                else if (nextIdx == -2)
                {
                    // It can happen in a multi-operation txn, where while doing card operations, token dispensr's status goes bad.
                    SendMsg.UpdateMediaTerminated();
                    return;
                }

                //_mediasToHalt would be affected on media removal; not now.
            }

            private void ReportMMIAboutOperationCompletionOrPartCompletionWhatever()
            {
                IUpdateMediaOp op = GetCurrentOp();

                var opIdx = _ops.Find(x => Object.ReferenceEquals(x.First, op)).Second;

                if (!(op is MediaOpReqCSCIssue))
                {
                    if (op.bIsOpCompletedEvenPartly()
                        //! && op.IsAudited()
                        )
                    {
                        if (op is MediaOpReqEnableAutoTopup)
                        {
                            Tuple<string, string> x = ((MediaOpReqEnableAutoTopup)op).GetXmlStringForInitBankTopupToSendToMMI();
                            if (x != null)
                                SendMsg.UpdateMediaOpInitialiseBankTopup(x.First, x.Second);
                        }
                        Tuple<string, string> StringToSendToMMI = op.GetXmlStringToSendToMMI();
                        SendMsg.UpdateMediaOpAudited(
                            opIdx._opIdx,
                            opIdx._subIdx,
                            op.GetStatus() == MediaUpdateCompletionStatus.DoneSuccessfully,
                            StringToSendToMMI.First,
                            StringToSendToMMI.Second
                        );
                        op.SetAudited();
                    }
                }
                else
                {
                    MediaOpReqCSCIssue op_ = (MediaOpReqCSCIssue)GetCurrentOp();
                    Tuple<string, string, bool> issueTxn = op_.GetIssueTxnToUploadToMMI();
                    if (issueTxn != null)
                    {
                        SendMsg.UpdateMediaOpAuditedCSCIssue(
                            0,                            
                            opIdx._opIdx,
                            opIdx._subIdx,
                            issueTxn.Third,
                            issueTxn.First,
                            issueTxn.Second
                        );
                    }

                    Tuple<string, string, bool> addvalTxn = op_.GetAddValTxnToUploadToMMI();
                    if (addvalTxn != null)
                    {
                        SendMsg.UpdateMediaOpAuditedCSCIssue(
                            1,                            
                            opIdx._opIdx,
                            opIdx._subIdx,
                            addvalTxn.Third,
                            addvalTxn.First,
                            addvalTxn.Second
                        );
                    }
                }
            }

            private bool _bIsConcluded = false;            
            private void MarkAsConcludedAndStopInvolvedTimers()
            {
                _idxCurOpSelected = null;
                _bIsConcluded = true;
                //_ticketingRules._reader.ClearIgnoreList();
                _ticketingRules.StopTimer(Timers.PutMediaOverRW);
                _ticketingRules.StopTimer(Timers.TimeoutCancelPutMediaOverAntennaAckNotRecvd);
                _ticketingRules.StopTimer(Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck);
                _ticketingRules.StopTimer(Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList); // TODO: See how the presence/absence of this line impacts.
                _ticketingRules.StopTimer(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE);
            }

            public bool IsConcluded() { return _bIsConcluded; }            

            MainTicketingRules _ticketingRules;
            public MediaOperationsRequested(string requestsXml, MainTicketingRules ticketingRules)
            {
                _ticketingRules = ticketingRules;
                XDocument doc = XDocument.Parse(requestsXml);
                XElement root = doc.Root;
                if (root == null || root.Name != "UpdateRequest")
                {
                    Logging.Log(LogLevel.Error, "Handle_UpdateMedia: Incorrect format");
                    return;
                }

                int idx = -1;
                foreach (XElement requestElem in root.Elements("Request"))
                {
                    ++idx;
                    List<IUpdateMediaOp> requests = UpdateMediaRequestBuilder(requestElem);
                    if (requests == null)
                    {
                        SendMsg.UpdateMediaTerminated();
                        // todo: somehow communicate to caller in tt, either by throwing exception or ...
                        return;
                    }
                    for (int i = 0; i < requests.Count; i++)
                        _ops.Add(Tuple.New(requests[i], new OriginalRequestId(idx, i)));
                }

                _ops.Sort((x, y) =>
                {
                    if (x.Second._opIdx == y.Second._opIdx)
                        return x.Second._subIdx - y.Second._subIdx;
                    else if (x.First is IUpdateMediaPreRegisteredOp && y.First is IUpdateMediaPreRegisteredOp)
                    {
                        // both are pre-registered.
                        //return x.Second._opIdx - y.Second._opIdx;
                        return 0; // It doesn't matter where they appear.
                    }
                    else if (x.First is IUpdateMediaNonPreRegisteredOp && y.First is IUpdateMediaNonPreRegisteredOp)
                    {
                        // both are non-preregistered
                        var firstOp = (IUpdateMediaNonPreRegisteredOp)x.First;
                        var secondOp = (IUpdateMediaNonPreRegisteredOp)y.First;

                        if (firstOp.DoesNeedTokenDispenser() && !secondOp.DoesNeedTokenDispenser())
                            return -1;
                        else if (!firstOp.DoesNeedTokenDispenser() && secondOp.DoesNeedTokenDispenser())
                            return 1;
                        else
                            return x.Second._opIdx - y.Second._opIdx;
                    }
                    else if (x.First is IUpdateMediaNonPreRegisteredOp && y.First is IUpdateMediaPreRegisteredOp)
                    {
                        var firstOp = (IUpdateMediaNonPreRegisteredOp)x.First;
                        var secondOp = (IUpdateMediaPreRegisteredOp)y.First;

                        if (firstOp.DoesNeedTokenDispenser())
                            return -1;
                        else
                            return 1;
                    }
                    else if (x.First is IUpdateMediaPreRegisteredOp && y.First is IUpdateMediaNonPreRegisteredOp)
                    {
                        var firstOp = (IUpdateMediaPreRegisteredOp)x.First;
                        var secondOp = (IUpdateMediaNonPreRegisteredOp)y.First;

                        if (secondOp.DoesNeedTokenDispenser())
                            return 1;
                        else
                            return -1;
                    }
                    else
                    {
                        Debug.Assert(false); // We've exhaustivly covered all items above
                        return 0;
                    }
                });
            }
            
            protected PaymentMethods _paymentType = PaymentMethods.Cash;
            protected IFS2.Equipment.Common.CCHS.BankTopupDetails _bankTopupDetails = null;

            public void SetBankCardPaymentDetails(IFS2.Equipment.Common.CCHS.BankTopupDetails bankTopupDetails)
            {
                _paymentType = PaymentMethods.BankCard;
                _bankTopupDetails = bankTopupDetails;
            }

            public PaymentMethods PaymentType { get { return _paymentType; } }

            public IFS2.Equipment.Common.CCHS.BankTopupDetails BankTopupDetails { get { return _bankTopupDetails; } }                

            internal void NonDetectableMediaRemoved()
            {
                Debug.Assert(_bAskedMMIToRemoveNonDetectableMedia == true && DoesCurrentOpInolvesTokenDispenser());
                if (!DoesCurrentOpInolvesTokenDispenser())
                    return;
                _bAskedMMIToRemoveNonDetectableMedia = false;
                DoForInitiatingTokenDispenserOp();
            }

            internal void NoMediaDetectedWithinAskedTimeFrame()
            {
                Logging.Log(LogLevel.Verbose, "NoMediaDetectedWithinAskedTimeFrame");
                _ticketingRules._reader.StopPolling();
                _bAskedMMIToRemoveNonDetectableMedia = true;
                SendMsg.RemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.UnDetectableToken);            
            }

            internal void DeclarePartCompletedAsDone(int opIdx, int opSubIdx, long ticketPhysicalId)
            {                
                _ticketingRules.StopTimer(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE);
                _bWaitingForSomeMediaToArrivePostRTEOrWTE = false;
                var op = GetCurrentOpEx();
                if (op == null)
                {
                    Logging.Log(LogLevel.Information, "DeclarePartCompletedAsDone returning null");
                    return;
                }
                
                if (op.Second._opIdx == opIdx
                    && op.Second._subIdx == opSubIdx
                    && op.First.GetMediaSerialNumber() == ticketPhysicalId)
                {
                    _ticketingRules.AddMediaTreatedRecently(ticketPhysicalId);

                    IUpdateMediaOp op_ = op.First;
                    int? nextIdx = null;
                    if (op_ is MediaOpReqPreRegisteration
                        || op_.bIsOpCompletedEvenPartly())
                    {
                        op_.DeclarePartCompletedAsDone();
                        _idxCurOpSelected = null;
                    }
                    else
                    {
                        IUpdateMediaOp opRenewed;
                        MediaOpReqNoPreRegisteration op__ = (MediaOpReqNoPreRegisteration)op_;
                        switch(op.First.GetOpType())
                        {
                            case MediaOpType.CSCIssue:
                                opRenewed = new MediaOpReqCSCIssue(_ticketingRules, op__._parsXml);
                                break;
                            case MediaOpType.CSTIssue:
                                opRenewed = new MediaOpReqTokenIssue(_ticketingRules, op__._logicalMediaReferenceString, op__._parsXml);
                                break;
                            case MediaOpType.TTagIssue:
                                opRenewed = new MediaOpReqTTagIssue(_ticketingRules, op__._logicalMediaReferenceString);
                                break;
                            case MediaOpType.TTagUpdate:
                                opRenewed = new MediaOpReqTTagUpdate(_ticketingRules, op__._logicalMediaReferenceString);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        int idx = _ops.FindIndex(x=>Object.ReferenceEquals(x.First, op_));
                        _ops[idx] = Tuple.New(opRenewed, op.Second);
                        nextIdx = idx;
                    }

                    if (nextIdx == null)
                        nextIdx = FindNextIndex();  
                    
                    if (nextIdx == null)
                    {
                        SendMsg.UpdateMediaTerminated();
                        
                        MarkAsConcludedAndStopInvolvedTimers();
                        _ticketingRules.SetMediaUpdateConcluded();                        

                        return;
                    }
                    else
                    {                        
                        SetCurIndex((int)nextIdx);                        
                        
                        if (_ticketingRules._MediaDetectedState != SmartFunctions.MediaDetected.NONE)
                        {
                            // Implies the media on which RTE or WTE was witnessed is still placed on the reader
                            UpdateMedia_AskAgentToRemoveMedia(ReasonCodeForAskingAgentToRemoveMedia.MediaDeclaredPartCompletedAsDone);
                            return;
                        }
                        else
                        {
                            if (DoesCurrentOpInolvesTokenDispenser())
                                DoForInitiatingTokenDispenserOp();
                            else
                                _ticketingRules._reader.StartPolling();
                        }
                    }
                }
                else
                {
                    Logging.Log(LogLevel.Information, "DeclarePartCompletedAsDone ignored");
                }
            }

            bool _bCompleteAFMOpUsingLooseTokens = false;
            bool _blockedOnAFMOpBecauseAFMIsNotAvailable = false;

            internal void CompleteAFMOpUsingLooseTokens()
            {
                _bCompleteAFMOpUsingLooseTokens = true;                
                Logging.Log(LogLevel.Verbose, "CompleteAFMOpUsingLooseTokens " + _blockedOnAFMOpBecauseAFMIsNotAvailable.ToString());

                _idxCurOpSelected = null;
                SeeForInitiatingNewOperation();                
            }

            internal void MediaNotFoundFitForOperationAcked()
            {
                _bLastMediaNotFoundFitForOperationAckedAtLeastOnceForGivenMediaInThisCycleOfMediaPlacementAndRemoval = true;
                _bWaitingForAgentAckAboutMediaNotFoundFitForOperation = false;

                if (_ticketingRules._MediaDetectedState != SmartFunctions.MediaDetected.NONE)
                {
                    if (DoesCurrentOpInolvesTokenDispenser())
                        DoForInitiatingTokenDispenserOp();
                    else
                        AttemptOperation(_ticketingRules._MediaSerialNbr);
                }
                else
                {
                    if (!DoesCurrentOpInolvesTokenDispenser())
                        _ticketingRules._reader.StartPolling();
                    else
                        DoForInitiatingTokenDispenserOp();
                }
            }

            internal void UpdateMediaRollbackOp()
            {
                if (!(_ops.Exists(x => x.First is IMediaCancellableOp
                    && ((IMediaCancellableOp)x.First).GetLastCancelAttempt() == MediaOpGen.ResultLastCancelAttempt.None
                    || ((IMediaCancellableOp)x.First).GetLastCancelAttempt() == MediaOpGen.ResultLastCancelAttempt.MediaCouldntBeRead)))
                {
                    SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.AllOpsRolledBack_SoNothingToDo);
                    return;
                }

                var sf = SmartFunctions.Instance;
                _ticketingRules._reader.StopPolling();
                if (!_ticketingRules._reader.PollForAnyMediaAtMoment_AndPerformActionOnIt_IfNonePresentThenStopPolling(ReaderOp.SomeOperationsMayBeAskedToBePerformedOnThisMedia,
                    delegate()
                    {
                        var opDetail = _ops.Find(x => x.First.GetMediaSerialNumber() == _ticketingRules._MediaSerialNbr);
                        if (opDetail == null)
                        {
                            SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.MediaWasNotPartOfAnyOperationOfLastTxn);
                            return;
                        }
                        IUpdateMediaOp op = opDetail.First;
                        if (!(op is IMediaCancellableOp))
                        {
                            SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.NotPossible);
                            return;
                        }
                        if ((op is MediaOpReqAddValue && !Config.bAllowRollbackingAddValue)
                            || (op is MediaOpReqTokenIssue && !Config.bAllowRollbackingTokenIssue))
                        {
                            SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.CancelOfThisOpTypeNotAllowed);
                            return;
                        }

                        IMediaCancellableOp op_ = (IMediaCancellableOp)op;
                        MediaOpGen.ResultLastCancelAttempt lastCancelAttempt = op_.GetLastCancelAttempt();
                        if (lastCancelAttempt == MediaOpGen.ResultLastCancelAttempt.MediaCouldntBeWritten
                            || lastCancelAttempt == MediaOpGen.ResultLastCancelAttempt.MediaNotFoundFitForCancellation
                            || lastCancelAttempt == MediaOpGen.ResultLastCancelAttempt.Success)
                        {
                            SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.NotPossible);
                            return;
                        }
                        lastCancelAttempt = op_.CorrectMediaForCancellationAppeared();
                        Logging.Log(LogLevel.Verbose, " lastCancelAttempt = " + lastCancelAttempt.ToString());

                        switch (lastCancelAttempt)
                        {
                            case MediaOpGen.ResultLastCancelAttempt.Success:
                                var xml = op_.GetXmlStringToSendToMMIOnCancellation();
                                SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.Success, opDetail.Second._opIdx, opDetail.Second._subIdx, xml.First, xml.Second);
                                _ticketingRules.SetMediaKeptOnReaderWasLastUpdatedInThisVeryCycle();
                                break;
                            case MediaOpGen.ResultLastCancelAttempt.MediaNotFoundFitForCancellation:
                                SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.NotPossible);
                                break;
                            case MediaOpGen.ResultLastCancelAttempt.MediaCouldntBeWritten:
                                SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.WriteTicketError_WouldNotBeAttemptedMore, opDetail.Second._opIdx, opDetail.Second._subIdx);
                                _ticketingRules.SetMediaKeptOnReaderWasLastUpdatedInThisVeryCycle();
                                break;
                            case MediaOpGen.ResultLastCancelAttempt.MediaCouldntBeRead:
                                SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.ReadTicketError);
                                break;
                            default:
                                {
                                    Logging.Log(LogLevel.Error, "UpdateMediaRollbackOp: received unexpected result :" + lastCancelAttempt.ToString());
                                    Debug.Assert(false);
                                    return;
                                }
                        }
                    }))
                {
                    Logging.Log(LogLevel.Verbose, "PollForAnyMediaAtMoment_ThenStopPollingIfNoMediaIsThere returned false");
                    if (_ticketingRules._MediaDetectedState == SmartFunctions.MediaDetected.NONE)
                    {
                        SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.ReadTicketError);
                        return;
                    }
                }                
                
                if (!(_ops.Exists(x => x.First is IMediaCancellableOp
                    && x.First.IsAudited()
                    && ((IMediaCancellableOp)x.First).GetLastCancelAttempt() == MediaOpGen.ResultLastCancelAttempt.None
                    || ((IMediaCancellableOp)x.First).GetLastCancelAttempt() == MediaOpGen.ResultLastCancelAttempt.MediaCouldntBeRead)))
                {
                    DoOnRollbackingGettingCompletedOrAbandoned();
                    return;
                }
            }

            public void DoOnRollbackingGettingCompletedOrAbandoned()
            {
                _ticketingRules._reader.StopPolling();// RestartField(); // we can afford to restart field, because cancellations are rare.                
                SendMsg.UpdateMediaRollbackCompletedOrAbandoned();
                _ticketingRules.DoOnRollbackingGettingCompletedOrAbandoned();
            }
        }

        internal void DoOnRollbackingGettingCompletedOrAbandoned()
        {
            SetReadPurpose(_readDataFor);
        }
    }
}