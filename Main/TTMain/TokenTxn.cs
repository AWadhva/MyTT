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
#if WindowsCE || PocketPC
using OpenNETCF.Threading;
#endif
using System.Diagnostics;
#if false
using System.Windows.Forms;
#endif
namespace IFS2.Equipment.TicketingRules
{
    // Base treatment for Ticketing Rules
    public partial class MainTicketingRules
    {
        // This variable is now practically useless. It should never take value = true. See if it can be removed to clean.
        static bool _bAtLeastOneMediaHalted = false;

        private void MediaIssueTxn_PutMediaUnderRW()
        {
            Debug.Assert(_curMediaDistributionTxn != null);
            if (_curMediaDistributionTxn == null)
            {
                Logging.Log(LogLevel.Information, "TokenTxn_PutTokenUnderRW() _curMediaDistributionTxn == null ");
                return;
            }
            _curMediaDistributionTxn._sender.PutMediaUnderRW();
            StartDelay((int)(Timers.PutMediaOverRW), _curMediaDistributionTxn._sender.GetTimeoutInMilliSecForPutMediaUnderRWCompletion());

            _curMediaDistributionTxn._bPutMediaUnderRWSent = true;
        }

        private readonly int MAX_TOKENS_TO_TRY_BEFORE_DECLARING_AS_OOO;

        public Utility.StatusListenerDelegate listener = null;

        public int GetReaderHandleInvolvedWithTokenDispensing()
        {
            int hRw;
            if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                hRw = _hRw;
            else
                hRw = _hRwRearReaderAtTokenDispenser;

            return hRw;
        }

        public Scenario GetScenarioForTokenDispensing()
        {
            Scenario scenario;
            if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                scenario = Scenario.SCENARIO_2;
            else
                scenario = Scenario.SCENARIO_1;

            return scenario;
        }
        // Since we are calling this function from catch handlers too, this function should not raise any exception. Yes, there may be ReaderExceptions, which may get ignored, but that's all right.
        private void MediaIssueTxn_RestorePollingAtFront()
        {
            try
            {
                Logging.Log(LogLevel.Verbose, "RestorePollingAtFront");

                inst.StopField(GetReaderHandleInvolvedWithTokenDispensing());
                inst.StartPolling(Scenario.SCENARIO_1, listener);
            }
            catch (ReaderException ex)
            {
                throw ex;
            }
        }

        private void MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(bool bCouldntRead)
        {
            Logging.Log(LogLevel.Verbose, "Throwing token to bin");

            _curMediaDistributionTxn._throwTo = SendMsg.ThrowTo.Bin;
            _curMediaDistributionTxn._sender.ThrowToBin();

            StartDelay((int)Timers.ThrowMedia, _curMediaDistributionTxn._sender.GetTimeoutInMilliSecForThrowMediaToBinCompletion());
            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_DueToAnyReasonIncUnreadable++;
            if (bCouldntRead)
                _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable++;

            if (_curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_DueToAnyReasonIncUnreadable >= MAX_TOKENS_TO_TRY_BEFORE_DECLARING_AS_OOO)
            {
                Logging.Log(LogLevel.Verbose, "Putting reader OOO, because there seems some issue");
                // TODO: put meta status as alarm. Also decided that whether we should only put token functionality unavailable??

                int cnt = _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable;

                MediaIssueTxn_Abort(ReasonForAbort.ReaderError);
                // So, we don't wait for ack that the token was actually thrown to bin. It makes matter simple. Ideally TD driver would reply; it would simply get ignored because _curTxn would we null

                Debug.Assert(_evtRearReaderOOO != null);
                if (cnt >= MAX_TOKENS_TO_TRY_BEFORE_DECLARING_AS_OOO)
                {
                    if (_evtRearReaderOOO != null)
                    {
                        _evtRearReaderOOO.SetAlarm(true);
                        UpdateRearReaderMetaStatus();

                        _cscReloaderMetaStatus.UpdateMetaStatus();
                        IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                        Communication.SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                    }
                }
                return;
            }
            return;
        }

        private void Handle_StartMediaDistributionInternal()
        {
            if (_curMediaDistributionTxn == null)
            {
                Logging.Log(LogLevel.Information, "Handle_STARTMEDIADISTRIBUTIONINTERNAL _curMediaDistributionTxn == null");
                return;
            }

            _curMediaDistributionTxn._currentTokenLogicalData = null;
            _curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp = null;
            _curMediaDistributionTxn._currentCSCLogicalDataAfterIssueOp = null;

            MediaIssueTxn_PutMediaUnderRW();
        }

        private void Handle_StartTokenDistribution(EventMessage eventMessage)
        {
            try
            {
                var pars = eventMessage._par;
                if (IsMediaDistributionInProgress())
                {
                    SendMsg.TokenError();
                    return;
                }

                int cntMediaAsked = Convert.ToInt32(pars[0]);
                _curMediaDistributionTxn = new MediaDistributionTransaction(MediaDistributionTransaction.MediaType.Token, cntMediaAsked);
                _curMediaDistributionTxn._logicalData_FromMMI = pars[1];

                SmartFunctions.Instance.StopField(_hRw);
            }
            catch (ReaderException exp)
            {
                MediaIssueTxn_Abort(ReasonForAbort.ReaderError);
                throw exp;
            }

            Message("StartTokenDistribution", "", "STARTMEDIADISTRIBUTIONINTERNAL", null);
        }

        private void Handle_StartCSCDistribution(EventMessage eventMessage)
        {
            try
            {
                var pars = eventMessage._par;
                if (IsMediaDistributionInProgress())
                {
                    SendMsg.CSCDistributionError();
                    return;
                }

                int cntMediaAsked = Convert.ToInt32(pars[0]);
                _curMediaDistributionTxn = new MediaDistributionTransaction(MediaDistributionTransaction.MediaType.CSC, cntMediaAsked);
                _curMediaDistributionTxn.CSCIssue_ProductId = Convert.ToInt16(pars[1]);
                if (pars.Length >= 3 && pars[2] != String.Empty)
                    _curMediaDistributionTxn.CSCIssue_PurseValueAsked = Convert.ToInt32(pars[2]);
                if (pars.Length >= 4 && pars[3] != String.Empty)
                {
                    try
                    {
                        _curMediaDistributionTxn._bankTopupDetails = SerializeHelper<IFS2.Equipment.Common.CCHS.BankTopupDetails>.XMLDeserialize(pars[3]);
                        _curMediaDistributionTxn.PaymentMode = PaymentMethods.BankCard;
                    }
                    catch {
                        Logging.Log(LogLevel.Error, "Couldn't deserialize _curMediaDistributionTxn._bankTopupDetails");
                    }
                }
                FareProductSpecs.OneProductSpecs fpSpecs = SharedData._fpSpecsRepository.GetSpecsFor(_curMediaDistributionTxn.CSCIssue_ProductId);
                if (fpSpecs == null || !fpSpecs._IsOpen)
                {
                    SendMsg.CSCDistributionError();
                    _curMediaDistributionTxn = null;
                    return;
                }

                SmartFunctions.Instance.StopField(_hRw);
            }
            catch (ReaderException exp)
            {
                MediaIssueTxn_Abort(ReasonForAbort.ReaderError);
                throw exp;
            }

            Message("StartTokenDistribution", "", "STARTMEDIADISTRIBUTIONINTERNAL", null);
        }

        private DateTime _lastWhenResetJamToTDWasSubmitted = new DateTime(1970, 1, 1);

        private void Handle_ThrowMediaAck(string[] p)
        {
            StopDelay((int)(Timers.ThrowMedia));
            if (p == null || p.Length != 2)
            {
                Logging.Log(LogLevel.Error, "ThrowTokenAck: Unexpected parameter");
                return;
            }

            if (_curMediaDistributionTxn == null)
            {
                Logging.Log(LogLevel.Verbose, "Seems transaction was aborted either because successive no. of bad tokens or from request from MMI");
                return;
            }

            if (_curMediaDistributionTxn._throwTo == null
                || (SendMsg.ThrowTo)_curMediaDistributionTxn._throwTo != (SendMsg.ThrowTo)(Convert.ToInt32(p[0])))
            {
                Debug.Assert(false);
                Logging.Log(LogLevel.Error, "ThrowTokenAck: Unexpected place. Transaction already is completed/aborted");
                return;
            }

            bool bThrowingSuccessful = (p[1] == "0");
            bool bTryingToThrowToOT = (p[0] == "0");

            if (!bThrowingSuccessful)
            {
                MediaIssueTxn_Abort(ReasonForAbort.TokenError);

                //if (Config._AttemptToResetTokenDispenserOnOutJam)
                //{
                //    if ((DateTime.Now - _lastWhenResetJamToTDWasSubmitted).Minutes > 60)
                //    {
                //        // Do this at most once per hour to prevent lot of tokens in bin.                        

                //        SendTDToResetIfTokenNotThere();
                //    }
                //}
                return;
            }
            else
            {
#if false
                if (bTryingToThrowToOT)
                {
                    DialogResult userack = MessageBox.Show("You just got one token", "Token", MessageBoxButtons.YesNo);
                    if (userack == DialogResult.No)
                    {
                        SendMsg.ThrowToken(SendMsg.ThrowTo.OutputTray);
                        return;
                    }
                }
#endif
                if (bTryingToThrowToOT)
                {
                    TimeSpan maxTimeSpan = new TimeSpan(0, 0, 0, 0, _curMediaDistributionTxn._sender.GetMaxTimeInMilliSecToGiveVendedMediaToLeaveFieldAfterReceivingPositiveThrowMediaToOTAck());
                    // Verify that what token dispenser is really correct. Because it is observed that if token dispenser is bad, it does not.
                    Thread.Sleep(30); // Let the token give ample time to leave the field
                    bool bMediaDetected = true;
                    DateTime tsWhenPollingForGoneTokenIsInitiated = DateTime.Now;
                    while (DateTime.Now - tsWhenPollingForGoneTokenIsInitiated < maxTimeSpan)
                    {
                        bMediaDetected = MediaIssueTxn_TryToDetectMedia(1);
                        if (!bMediaDetected)
                            break;
                        Thread.Sleep(40);
                    }
                    if (bMediaDetected)
                    {
                        Logging.Log(LogLevel.Error, "Token detected even after ack from token dispenser that token is removed");
                        Debug.Assert(_curMediaDistributionTxn._currentMediaPhysicalId != 0);
                        if (SmartFunctions.Instance.ReadSNbr() == _curMediaDistributionTxn._currentMediaPhysicalId)
                        {
                            SendMsg.TokenDispenserOutJam(true);
                            MediaIssueTxn_Abort(ReasonForAbort.TokenError);
                            return;
                        }
                        else
                        {
                            //???????????????
                            Debug.Assert(false);
                        }
                    }
                    else
                        SendMsg.TokenDispenserOutJam(false);
                }
                _curMediaDistributionTxn._hopperId = "0";
                _curMediaDistributionTxn._throwTo = null;

                if (_curMediaDistributionTxn._cntMediaLeftToBeDistributed == 0)
                {
                    _curMediaDistributionTxn = null;
                    MediaIssueTxn_RestorePollingAtFront();
                }
                else
                    Message("ThrowTokenAck", "", "STARTMEDIADISTRIBUTIONINTERNAL");
            }
        }

        private void Handle_PutTokenUnderRWAck(string[] pars)
        {
            if (SharedData.EquipmentType != EquipmentFamily.TOM)
            {
                if (_curMediaDistributionTxn == null)
                {
                    Logging.Log(LogLevel.Information, "Handle_PutTokenUnderRWAck _curMediaDistributionTxn == null");
                    return;
                }
            }
            StopDelay((int)(Timers.PutMediaOverRW));
            bool bMediaDispensed = (pars[0] == "0");

            if (_mediaUpdate != null)
            {
                _mediaUpdate.PutTokenUnderRWAck(bMediaDispensed);
                if (_mediaUpdate != null && _mediaUpdate.IsConcluded())
                    _mediaUpdate = null;
                return;
            }
            else if (_curMediaDistributionTxn != null)
            {
                _curMediaDistributionTxn._bPutMediaUnderRWSent = false;

                try
                {
                    if (bMediaDispensed)
                    {
                        try
                        {
                            int hRw;
                            Scenario scenario;
                            if (_bMediaDispensingUsingRearReader)
                            {
                                hRw = _hRwRearReaderAtTokenDispenser;
                                scenario = Scenario.SCENARIO_1;
                            }
                            else if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                            {
                                hRw = _hRw;
                                scenario = Scenario.SCENARIO_2;
                            }
                            else
                            {
                                Debug.Assert(false);
                                throw new NotSupportedException();
                            }
                            inst.StartPolling(scenario, listener, hRw);
                            if (pars.Length >= 2)
                                _curMediaDistributionTxn._hopperId = pars[1]; // 1 or 2
                        }
                        catch (ReaderException exp)
                        {
                            Logging.Log(LogLevel.Error, "Couldn't start polling");
                            MediaIssueTxn_Abort(ReasonForAbort.ReaderError);

                            throw exp;
                        }
                    }
                    else
                    {
                        MediaIssueTxn_Abort(ReasonForAbort.TokenError);
                        return;
                    }

                    try
                    {
                        MediaIssueTxn_TryOperationOnMediaKeptOnReader();
                    }
                    catch (ReaderException err)
                    {
                        _curMediaDistributionTxn._sender.ThrowToBin();

                        MediaIssueTxn_Abort(ReasonForAbort.ReaderError);

                        throw err;
                    }
                    return;
                }
                finally
                {
                }
            }
        }

        private void MediaIssueTxn_TryOperationOnMediaKeptOnReader()
        {
            SmartFunctions inst = SmartFunctions.Instance;

            bool bMediaToBeWrittenDetected;
            // Since TokenTxn_TryToDetectToken is making several passes to detect the token, this function doesn't has to do anything more for detection
            bMediaToBeWrittenDetected = MediaIssueTxn_TryToDetectMedia(Config.nToken_MAX_TRIALS_FOR_DETECTION);

            Int64 serialNbrMediaSelectedForOperation = 0;
            if (!bMediaToBeWrittenDetected)
            {
                inst.StopField(GetReaderHandleInvolvedWithTokenDispensing());
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);

                return;
            }
            else
            {
                serialNbrMediaSelectedForOperation = SmartFunctions.Instance.ReadSNbr();
                Logging.Log(LogLevel.Verbose, "Token selected " + serialNbrMediaSelectedForOperation.ToString("X2"));
                if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                {
                    if (_evtRearReaderOOO.Activated)
                    {
                        _evtRearReaderOOO.SetAlarm(false);
                        _cscReloaderMetaStatus.UpdateMetaStatus();
                        IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                        Communication.SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                    }
                }
                else if (_bMediaDispensingUsingRearReader)
                {
                    _evtRearReaderOOO.SetAlarm(false);
                    UpdateRearReaderMetaStatus();
                }
            }

            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0; // if want to restart the count, but it is debatable, and can be removed            

            //SmartFunctions.Instance.SwitchToDetectRemovalStateEx();
            Thread.Sleep(Config.nTimeInMilliSecToLetEjectedTokenFromDispenserSettleProperlyInRFField);
            if (_curMediaDistributionTxn._mediaType == MediaDistributionTransaction.MediaType.Token)
                TokenTxn_OperationPostSuccessfulDetection(serialNbrMediaSelectedForOperation);
            else
                CSCIssueTxn_OperationPostSuccessfulDetection(serialNbrMediaSelectedForOperation);
        }

        public void HaltCurrentCardNRestartPollingRearReader()
        {
            SmartFunctions.Instance.HaltCard(_hRwRearReaderAtTokenDispenser);

            SmartFunctions.Instance.StartPolling(Scenario.SCENARIO_1, null, _hRwRearReaderAtTokenDispenser);
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;

            Thread.Sleep(Config._nTimeToSleepAfterRemovalOfMediaToPollAgain); // because if we ask immediatly for status, very likely we would still get state POLL_ON
        }

        public void HaltCurrentCardNRestartPollingSynch()
        {
            //Logging.Log(LogLevel.Verbose, "HaltCurrentCardNRestartPolling. Trace is " + Environment.StackTrace);
            SmartFunctions.Instance.HaltCard();
            SmartFunctions.Instance.StartPolling(SmartFunctions.Instance.GetActiveScenario(), (Utility.StatusListenerDelegate)null);

            ClearDataStructuresOnMediaRemoval();

            Thread.Sleep(Config._nTimeToSleepAfterRemovalOfMediaToPollAgain); // because if we ask immediatly for status, very likely we would still get state POLL_ON
        }

        void UpdateRearReaderMetaStatus()
        {
            try
            {
                if (_evtRearReaderMetaStatus == null)
                    return;

                _evtRearReaderMetaStatus.UpdateMetaStatus();
                if (_evtRearReaderMetaStatus.HasChangedSinceLastSave)
                {
                    SendMetaStatusRearReader();
                    IFSEventsList.SaveContext(moduleRearReader);
                }
            }
            catch (Exception)
            { }
        }

        private void SendMetaStatusRearReader()
        {
            if (_evtRearReaderMetaStatus == null)
                return;
            Communication.SendMessage(ThreadName, "", "RearReaderMetaStatus", _evtRearReaderMetaStatus.Value.ToString(), GetStatusRearReader());
            _bRearReaderStatusBroadcastOnce = true;
        }

        string GetStatusRearReader()
        {
            if (_evtRearReaderMetaStatus == null)
                return "";
            try
            {
                //Construct the XML string
                var sb = new StringBuilder();
                XmlWriter xmlWriter = XmlWriter.Create(sb, contextFile_XMLWriterSettings);
                xmlWriter.WriteStartElement("RearReader");
                string alarms = "IsRearOffLine;RearOOO";
                IFSEventsList.ComposeXMLForStatus(xmlWriter, moduleRearReader, "RearReaderMetaStatus", alarms, null);
                xmlWriter.WriteEndElement(); //UPS
                xmlWriter.Close();
                return sb.ToString();
            }
            catch { return ""; }
        }

        private DateTime _timeStampWhenRearReaderWasPingedLast = new DateTime(2000, 1, 1);

        private void CSCIssueTxn_OperationPostSuccessfulDetection(
            Int64 serialNbrTokenSelectedForOperation
            )
        {
            SmartFunctions inst = SmartFunctions.Instance;
            int hRw = GetReaderHandleInvolvedWithTokenDispensing();

            bool bMediaRead = false;
            LogicalMedia logMedia = new LogicalMedia();
            CommonHwMedia hwCsc = new DelhiDesfireEV0();

            bMediaRead = hwCsc.ReadMediaData(logMedia, MediaDetectionTreatment.TOM_AnalysisForCSCIssue);
            if (!bMediaRead)
            {
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);
                return;
            }

            bool bMediaFitForOperation = (
                (logMedia.Media.Status == Media.StatusValues.Initialised
                || (logMedia.Media.Status == Media.StatusValues.Refunded && Config.ALLOW_REFUNDED_CSC_BEISSUED))
                && logMedia.Purse.TPurse.Balance == 0);

            if (bMediaFitForOperation)
                bMediaFitForOperation = !MediaDenyList.VerifyMedia((int)logMedia.Media.HardwareType, logMedia.Media.ChipSerialNumber);

            if (!bMediaFitForOperation)
            {
                inst.StopField(hRw);
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(false);

                return;
            }

            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0;
            var fpSpecs = SharedData._fpSpecsRepository.GetSpecsFor(_curMediaDistributionTxn.CSCIssue_ProductId);

            SalesRules.CSCIssueUpdate(logMedia, _curMediaDistributionTxn.CSCIssue_ProductId, _curMediaDistributionTxn.PaymentMode, false, fpSpecs._Deposit + fpSpecs._SalePrice.Val, _curMediaDistributionTxn._language, false);
            LogicalMedia logMediaAfterCSCIssueUpdate = new LogicalMedia(logMedia.ToXMLString());
            if (_curMediaDistributionTxn.CSCIssue_PurseValueAsked != 0)
                SalesRules.AddValueUpdate(logMedia, _curMediaDistributionTxn.CSCIssue_PurseValueAsked, _curMediaDistributionTxn.PaymentMode);

            // Order is important. Also CommitModifications is called twice. First after writing DM2, and then after writing DM1
            bool bMediaWritten = hwCsc.WriteLocalSaleData(logMedia, false) // File #1, #3
                    && hwCsc.WriteLocalValidationData(logMedia) // File #2
                    && hwCsc.CommitModifications()
                    && hwCsc.UpdateTPurseData(logMedia, _curMediaDistributionTxn.CSCIssue_PurseValueAsked, false) // File #1, #2, #5
                    && hwCsc.WriteMainSaleData(logMedia) // File #6                    
                    && hwCsc.WriteCardHolderData(logMedia)
                    && hwCsc.CommitModifications();

            var ret = SmartFunctions.Instance.SwitchToDetectRemovalState(hRw);

            if (!bMediaWritten)
            {
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);
                return;
            }

            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0;

            string cchsStrIssue = "", cchsStrAddVal = "";

            if (_delhiCCHSSAMUsage)
            {
                {
                    FldsCSCIssueTxn txn = new FldsCSCIssueTxn();

                    txn.psn = (int)logMediaAfterCSCIssueUpdate.Application.LocalLastAddValue.SequenceNumber;
                    txn.ticket1StartDate = DateTime.Now;
                    txn.cscType = CSC_Type_t.CardADesfire;
                    txn.ticket1Type = _curMediaDistributionTxn.CSCIssue_ProductId;
                    int family = ProductParameters.GetProductFamily(_curMediaDistributionTxn.CSCIssue_ProductId);
                    int fees;
                    if (family == 60)
                    {
                        txn.issueType = IssueType.CardIssue_Deposit_CommonPurse_SVProduct;
                        fees = 0;
                    }
                    else
                    {
                        txn.issueType = IssueType.CardIssue_Deposit_CommonPurse_PeriodPass;
                        fees = fpSpecs._SalePrice.Val;
                    }
                    txn.ticket2StartDate = DateTime.MaxValue;
                    txn.ticket2EndDate = DateTime.MaxValue;
                    txn.cntRides = 0;
                    txn.blockingStatus = 1; // Unblocked
                    txn.lng = 0; // English (only value that can be assigned for this field)
                    txn.cscRemainingValue = 0;
                    txn.transactionValue = fpSpecs._Deposit + fees;
                    txn.cscDepositAmount = fpSpecs._Deposit;

                    cchsStrIssue = SmartFunctions.Instance.GetTDforCCHSGen(logMediaAfterCSCIssueUpdate, TransactionType.CSCIssue, txn, false, false);
                }

                if (_curMediaDistributionTxn.CSCIssue_PurseValueAsked > 0)
                {
                    switch (_curMediaDistributionTxn.PaymentMode)
                    {
                        case PaymentMethods.Cash:
                            {
                                SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.TPurseDirectReload, ++SharedData.TransactionSeqNo, _curMediaDistributionTxn.CSCIssue_PurseValueAsked, out cchsStrAddVal, false, false, true);
                                logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;
                                cchsStrAddVal = Utility.MakeTag("XdrData", cchsStrAddVal);
                                break;
                            }
                        case PaymentMethods.BankCard:
                            {
                                FldsCSCPeformAddValueViaEFT txn = new FldsCSCPeformAddValueViaEFT();
                                txn.addValAmt = _curMediaDistributionTxn.CSCIssue_PurseValueAsked;
                                txn.depositInCents = fpSpecs._Deposit;
                                txn.purseRemainingVal = _curMediaDistributionTxn.CSCIssue_PurseValueAsked;

                                var bankTopupDetails = _curMediaDistributionTxn._bankTopupDetails;
                                Debug.Assert(bankTopupDetails != null);

                                if (bankTopupDetails != null)
                                {
                                    txn.systemTraceAuditNumber = bankTopupDetails.SystemTraceAuditNumber;
                                    txn.dtTimeBankTerminalTransaction = bankTopupDetails.dt;
                                    txn.authorizationCode = bankTopupDetails.authorizationCode;
                                    txn.bankTerminalId = bankTopupDetails.TerminalID;
                                    txn.resoponseCode = bankTopupDetails.responseCodeFromTerminal;

                                    txn.operationType = OperationType_t.Other; // TODO: It is neither sale nor replacement. But Other doesn't seem to a good value. Let's see what fits
                                }
                                cchsStrAddVal = SmartFunctions.Instance.GetTDforCCHSGen(logMedia, TransactionType.TXN_CSC_ADD_VALUE_EFT, txn, false, false);
                                break;
                            }
                    }
                }
            }

            _curMediaDistributionTxn._currentCSCLogicalDataAfterIssueOp = Tuple.New(logMediaAfterCSCIssueUpdate.ToXMLString(), cchsStrIssue);
            if (_curMediaDistributionTxn.CSCIssue_PurseValueAsked > 0)
                _curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp = Tuple.New(logMedia.ToXMLString(), cchsStrAddVal);

            MediaIssueTxn_MediaDistributed();

            _curMediaDistributionTxn._throwTo = SendMsg.ThrowTo.OutputTray;
            SendMsg.ThrowCSC(SendMsg.ThrowTo.OutputTray);

            StartDelay((int)Timers.ThrowMedia, Config.nTimeOutInMilliSecForThrowToOTCompletion);

            return;
        }

        private void CSCRefundTxn_OperationPostSuccessfulDetection(
    Int64 serialNbrTokenSelectedForOperation
    )
        {
            SmartFunctions inst = SmartFunctions.Instance;
            int hRw = GetReaderHandleInvolvedWithTokenDispensing();

            bool bMediaRead = false;
            LogicalMedia logMedia = new LogicalMedia();
            CommonHwMedia hwCsc = new DelhiDesfireEV0();

            bMediaRead = hwCsc.ReadMediaData(logMedia, MediaDetectionTreatment.TOM_AnalysisForRefund);
            if (!bMediaRead)
            {
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);
                return;
            }

            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0;

            int balance = logMedia.Purse.TPurse.Balance;
            SalesRules.RefundUpdateCard(logMedia);

            bool bMediaWritten = hwCsc.UpdateTPurseData(logMedia, -balance, false)
                    && hwCsc.WriteMainSaleData(logMedia)
                    && hwCsc.CommitModifications()
                    && hwCsc.WriteLocalSaleData(logMedia, false) // File #1, #3
                    && hwCsc.WriteLocalValidationData(logMedia)
                    && hwCsc.CommitModifications();

            var ret = SmartFunctions.Instance.SwitchToDetectRemovalState(hRw);

            if (!bMediaWritten)
            {
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);
                return;
            }

            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0;

            //MediaIssueTxn_MediaDistributed();

            _curMediaDistributionTxn._throwTo = SendMsg.ThrowTo.OutputTray;
            SendMsg.ThrowCSC(SendMsg.ThrowTo.OutputTray);

            StartDelay((int)Timers.ThrowMedia, Config.nTimeOutInMilliSecForThrowToOTCompletion);

            return;
        }

        private void TokenTxn_OperationPostSuccessfulDetection(
            Int64 serialNbrTokenSelectedForOperation
            )
        {
            SmartFunctions inst = SmartFunctions.Instance;
            int hRw = GetReaderHandleInvolvedWithTokenDispensing();
            DelhiTokenUltralight ul = new DelhiTokenUltralight(hRw);
            SmartFunctions.MediaDetected mediaDetected;
            byte[] _pTokenData = null;
            LogicalMedia tmpMedia = null;
            bool bTokenRead = false;

            DateTime timeTillAttemptsHaveToBeMade = DateTime.Now.AddMilliseconds(Config.nTotalTimeInMilliSecToLetTTAttemptWriteToTokenFromTD);

            int attemptCount = 0;
            CSC_API_ERROR ErrReading = CSC_API_ERROR.ERR_NONE;

            while (true)
            {
                attemptCount++;
                bool bSameMedia;
                int statusCSC;

                Logging.Log(LogLevel.Verbose, ("attemptCount = " + attemptCount));

                SmartFunctions.Instance.SmartSyncDetectOk(out mediaDetected, out bSameMedia, out statusCSC, hRw, CSC_READER_TYPE.V4_READER, GetScenarioForTokenDispensing());

                if (mediaDetected == SmartFunctions.MediaDetected.NONE)
                {
                    #region MediaDisappearedButHopefullyMayReappear

                    if (DateTime.Now > timeTillAttemptsHaveToBeMade)
                    {
                        Logging.Log(LogLevel.Verbose, "Max attempts made (MediaDisappearedButHopefullyMayReappear). Total Attempts made = " + attemptCount.ToString());
                        break;
                    }
                    else
                    {
                        Thread.Sleep(Config.nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD);
                        continue;
                    }
                    #endregion
                }

                if (SmartFunctions.Instance.ReadSNbr() != serialNbrTokenSelectedForOperation)
                {
                    #region DifferentMediaMostProbablyTheOneBeingDraggedOutFromLastOperation
                    _bAtLeastOneMediaHalted = true;
                    Logging.Log(LogLevel.Verbose, "Halted " + SmartFunctions.Instance.ReadSNbr() + " while reading");
                    if (_bMediaDispensingUsingRearReader)
                        HaltCurrentCardNRestartPollingRearReader();
                    else if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                        HaltCurrentCardNRestartPollingSynch();
                    if (DateTime.Now > timeTillAttemptsHaveToBeMade)
                    {
                        break;
                    }
                    else
                    {
                        // Already slept in HaltCurrentCardNRestartPolling. So, no need to sleep again.                        
                        continue;
                    }
                    #endregion
                }

                #region TryToReadToken
                tmpMedia = new LogicalMedia();

                ul.SetRWHandle(GetReaderHandleInvolvedWithTokenDispensing());
                bTokenRead = ul.ReadMediaData2(tmpMedia, out _pTokenData, out ErrReading, MediaDetectionTreatment.BasicAnalysis_TOM);

                if (bTokenRead)
                {
                    byte[] dataTrimmed = new byte[64];
                    Array.Copy(_pTokenData, 1, dataTrimmed, 0, 64);
                    _pTokenData = dataTrimmed;

                    Logging.Log(LogLevel.Verbose, "Token read successfully");
                    break;
                }

                if (DateTime.Now > timeTillAttemptsHaveToBeMade)
                {
                    Logging.Log(LogLevel.Verbose, "Time Elapsed while Reading. Total attempts made = " + attemptCount.ToString());
                    break;
                }
                #endregion
                if (ErrReading == CSC_API_ERROR.ERR_TIMEOUT)
                {
                    Logging.Log(LogLevel.Verbose, "Couldn't read from token ERR_TIMEOUT. Sleeping..");
                    SmartFunctions.Instance.SwitchToDetectRemovalState(hRw);
                    Thread.Sleep(Config.nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD);
                    continue;
                }
                else
                {
                    Debug.Assert(ErrReading != CSC_API_ERROR.ERR_NONE);
                    break;
                }
            }

            if (!bTokenRead)
            {
                inst.StopField(hRw);
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);

                return;
            }

            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0;

            LogicalMedia logMedia = new LogicalMedia(_curMediaDistributionTxn._logicalData_FromMMI);
            // In "GETTOKENBINDATA" also, we are using the cached logical data generated during GetTokenPrice and ignoring what is comming with the command. Here too, we assume that GetTokenPrice would have been called prior to the request.
            logMedia.Media.ChipSerialNumber = serialNbrTokenSelectedForOperation;

            ulong mac_NotUsedByMe;
            byte[] cb = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocks(SharedData.TokenLayoutVersion, logMedia, _pTokenData, out mac_NotUsedByMe));

            Logging.Log(LogLevel.Verbose, "Trying to write to token " + Utility.ByteListToString(cb));
            bool bTokenWritten = false;

            CSC_API_ERROR? ErrWriting = null;
            #region WriteMedia
            while (true)
            {
                bool bSameMedia;
                int statusCSC;

                SmartFunctions.Instance.SmartSyncDetectOk(out mediaDetected, out bSameMedia, out statusCSC, hRw, CSC_READER_TYPE.V4_READER, GetScenarioForTokenDispensing());

                if (mediaDetected == SmartFunctions.MediaDetected.NONE)
                {
                    if (DateTime.Now > timeTillAttemptsHaveToBeMade)
                    {
                        Logging.Log(LogLevel.Verbose, "Time Elapsed while Writing. AttemptCount = " + attemptCount.ToString());
                        break;
                    }
                    else
                    {
                        attemptCount++;
                        Thread.Sleep(Config.nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD);
                        continue;
                    }
                }
                else
                {
                    if (SmartFunctions.Instance.ReadSNbr() != serialNbrTokenSelectedForOperation)
                    {
                        _bAtLeastOneMediaHalted = true;
                        Logging.Log(LogLevel.Verbose, "Halted " + SmartFunctions.Instance.ReadSNbr() + " while writing");
                        HaltCurrentCardNRestartPollingSynch();
                        if (DateTime.Now > timeTillAttemptsHaveToBeMade)
                        {
                            break;
                        }
                        else
                        {
                            // Already slept in HaltCurrentCardNRestartPolling                         
                            continue;
                        }
                    }
                    else
                    {
                        if (statusCSC == CONSTANT.ST_DETECT_REMOVAL)
                        {
                            // This is unreachable. Putting it for extra defence.
                            inst.SwitchToCardOnState(hRw);
                        }
                        byte p1, p2;

                        bool bSuccess;
                        ErrWriting = TokenFunctions.WriteBlocks(CSC_READER_TYPE.V4_READER, GetReaderHandleInvolvedWithTokenDispensing(), cb, out p1, out p2, out bSuccess);
                        var ret = SmartFunctions.Instance.SwitchToDetectRemovalState(hRw);
                        if (ErrWriting == CSC_API_ERROR.ERR_NONE && bSuccess)
                        {
                            bTokenWritten = true;
                            break;
                        }
                        else if (ErrWriting == CSC_API_ERROR.ERR_TIMEOUT || ErrWriting == CSC_API_ERROR.ERR_NOEXEC)
                        {
                            inst.SwitchToDetectRemovalState(hRw);
                            Thread.Sleep(Config.nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD);
                            if (DateTime.Now > timeTillAttemptsHaveToBeMade)
                            {
                                break;
                            }
                            else
                            {
                                Thread.Sleep(Config.nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD);
                                continue;
                            }
                        }
                    }
                }
            }
            #endregion
            inst.StopField(hRw);

            if (!bTokenWritten)
            {
                MediaIssueTxn_SubmitRequestToThrowToBinAndSeeIfWeCanProceedWithTransactionFurther(true);

                return;
            }
            _curMediaDistributionTxn._cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0;
            _curMediaDistributionTxn._currentTokenLogicalData = logMedia.ToXMLString();

            MediaIssueTxn_MediaDistributed();

            _curMediaDistributionTxn._throwTo = SendMsg.ThrowTo.OutputTray;
            SendMsg.ThrowToken(SendMsg.ThrowTo.OutputTray);
            StartDelay((int)Timers.ThrowMedia, Config.nTimeOutInMilliSecForThrowToOTCompletion);

            return;
        }

        private enum ReasonForAbort { TokenError, ReaderError, StopRequestedFromMMI };
        private void MediaIssueTxn_Abort(ReasonForAbort reason)
        {
            StopDelay((int)Timers.ThrowMedia);
            StopDelay((int)Timers.PutMediaOverRW);
            StopDelay((int)Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck);
            StopDelay((int)Timers.TimeoutCancelPutMediaOverAntennaAckNotRecvd);

            switch (reason)
            {
                case ReasonForAbort.TokenError:
                    if (_curMediaDistributionTxn != null && _curMediaDistributionTxn._cntMediaLeftToBeDistributed > 0)
                        _curMediaDistributionTxn._sender.MediaDistributionHaltedDueToSomeProblem();
                    break;
                case ReasonForAbort.ReaderError:
                    _curMediaDistributionTxn._sender.MediaDistributionHaltedDueToSomeProblem();
                    break;
                case ReasonForAbort.StopRequestedFromMMI:
                    _curMediaDistributionTxn._sender.StopMediaDistributionAck();
                    break;
            }

            _curMediaDistributionTxn = null;
            MediaIssueTxn_RestorePollingAtFront();
        }


        private bool MediaIssueTxn_TryToDetectMedia(
            int maxTrialsForDetection)
        {
            SmartFunctions.MediaDetected mediaDetected = SmartFunctions.MediaDetected.NONE;
            int statusCSC;

            int hRw = GetReaderHandleInvolvedWithTokenDispensing();

            for (int i = 0; i < maxTrialsForDetection; i++)
            {
                Thread.Sleep(Config.nSleepIntervalInMSecs_TokenNonDetection);
                bool bSameMedia;

                SmartFunctions.Instance.SmartSyncDetectOk(out mediaDetected, out bSameMedia, out statusCSC
                    , hRw, CSC_READER_TYPE.V4_READER, GetScenarioForTokenDispensing());
                if (mediaDetected != SmartFunctions.MediaDetected.NONE)
                {
                    long currentlyDetectedToken = SmartFunctions.Instance.ReadSNbr();
                    Logging.Log(LogLevel.Verbose, "TokenTxn_TryToDetectToken detected " + currentlyDetectedToken.ToString("X2"));
                    return true;
                }
            }
            if (mediaDetected == SmartFunctions.MediaDetected.NONE)
                Logging.Log(LogLevel.Verbose, "No media detected");
            return false;
        }

        private void MediaIssueTxn_MediaDistributed()
        {
            _curMediaDistributionTxn._cntMediaLeftToBeDistributed--;

            if (_curMediaDistributionTxn._mediaType == MediaDistributionTransaction.MediaType.Token)
                SendMsg.TokenDistributed(_curMediaDistributionTxn._cntMediaLeftToBeDistributed != 0,
                   _curMediaDistributionTxn._cntMediaRequested - _curMediaDistributionTxn._cntMediaLeftToBeDistributed,
                   _curMediaDistributionTxn._hopperId,
                   _curMediaDistributionTxn._currentTokenLogicalData);
            else
                SendMsg.CSCDistributed(_curMediaDistributionTxn._cntMediaLeftToBeDistributed != 0,
                   _curMediaDistributionTxn._cntMediaRequested - _curMediaDistributionTxn._cntMediaLeftToBeDistributed,
                   _curMediaDistributionTxn._currentCSCLogicalDataAfterIssueOp.First ?? "",
                   _curMediaDistributionTxn._currentCSCLogicalDataAfterIssueOp.Second ?? "",
                   (_curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp == null || _curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp.First == null) ? "" : _curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp.First,
                   (_curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp == null || _curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp.Second == null) ? "" : _curMediaDistributionTxn._currentCSCLogicalDataAfterAddValueOp.Second
                   );
            return;
        }

        // In current flow, this function should be practically unreachable. Still not removing it, to avoid any risk
        public void RestartField()
        {
            //Debug.Assert(_reader == null);
            StopField();
            SmartFunctions.Instance.StartField();
        }

        // In current flow, this function should be practically unreachable. Still not removing it, to avoid any risk
        public void StopField()
        {
            Debug.Assert(_reader == null);
            Logging.Log(LogLevel.Verbose, "StopField");
            SmartFunctions.Instance.StopField();
            ClearDataStructuresOnMediaRemoval();
            StopTimer(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE);
            Thread.Sleep(50); // Giving sufficient time for any media appearance/disappearance to be raised, and subsequently ignored because of _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored
        }
    }
}