// It would maintain MediaDetectedState and Serial Number of the media. TTMain would depend upon it

using System;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using IFS2.Equipment.CSCReader;
namespace IFS2.Equipment.TicketingRules
{
    abstract class ThalesReader : IReader
    {
        public ThalesReader()
        {
            SmartFunctions.Instance.listenerCardProduced = StatusListenerMediaProduced;
        }

        override public void ClearIgnoreList()
        {
            Logging.Log(LogLevel.Verbose, "ClearIgnoreList _curStatus = " + _curStatus.ToString());
            if (_mediaIDsIgnored.Count == 0)
            {
                base.ClearIgnoreList();
                return;
            }
            var statusBeforeStoppingField = _curStatus;
            base.ClearIgnoreList();
            
            StopField(); // So, any medias that were halted, but are still present on reader, can now answer. Done to achieve policy that at most one media (but practically at most two) be present on the r/w

            if (statusBeforeStoppingField == ReaderStatus.ST_POLLON) // See if this condition is required. It wasn't there in non-clenased version
                StartPolling();
            else
            {
                Logging.Log(LogLevel.Verbose, "statusBeforeStoppingField = " + statusBeforeStoppingField.ToString());
                SmartFunctions.Instance.StartField(); // so that when next time we ask to start polling, it doesn't take too much time.
            }
        }

        // Returns true, if media was found. Else, false
        public override bool PollForAnyMediaAtMoment_AndPerformActionOnIt_IfNonePresentThenStopPolling(ReaderOp opToBeDoneInCaseMediaIsProduced, Action actToBeDoneOnMedia)
        {
            Logging.Log(LogLevel.Verbose, "PollForAnyMediaAtMoment_ThenStopPollingIfNoMediaIsThere");
            //Debug.Assert(_curStatus == ReaderStatus.ST_INIT);
            bool bSameMedia;
            StatusCSC pStatusCSC;
            Scenario scenario = Scenario.SCENARIO_1;

            
            if (_curStatus == ReaderStatus.ST_INIT)
            {
                _MediaSrNbr = 0; SmartFunctions.Instance.SetSerialNbr(_MediaSrNbr);
            
                // Clean me
                if (this is V3Reader)
                {
                    StartPollingSync();
                    Thread.Sleep(50); // this delay is necessary, else it seems v3 reader is not able to shift to CardOn, even when media is there
                }
                else
                    StartPolling();
                _curStatus = ReaderStatus.ST_POLLON;
            }
            
            SmartFunctions.Instance.SmartSyncDetectOkPassive(scenario, out _MediaDetected, out bSameMedia, out pStatusCSC);
            _curStatus = (ReaderStatus)(pStatusCSC.ucStatCSC);
            Logging.Log(LogLevel.Verbose, "_curStatus = " + _curStatus.ToString());
            switch ((ReaderStatus)(pStatusCSC.ucStatCSC))
            {
                case ReaderStatus.ST_INIT:
                    {
                        Debug.Assert(false);
                        return false;
                    }
                case ReaderStatus.ST_CARDON:                    
                    DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out _MediaDetected, out _MediaSrNbr); SmartFunctions.Instance.SetSerialNbr(_MediaSrNbr);                    
                    if (actToBeDoneOnMedia != null)
                        actToBeDoneOnMedia();

                    ActToAchieveReaderOp(opToBeDoneInCaseMediaIsProduced);
                    _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
                    return true;
                case ReaderStatus.ST_POLLON:
                    {
                        StopPolling();
                        return false;
                        //SmartFunctions.Instance.StopPolling();
                        //Thread.Sleep(50); // Putting it only because we need to wait for some amount after polling to get the correct result
                        //SmartFunctions.Instance.SmartSyncDetectOkPassive(scenario, out _MediaDetected, out bSameMedia, out pStatusCSC);
                        //_curStatus = pStatusCSC.ucStatCSC;

                        //if (pStatusCSC.ucStatCSC == ReaderStatus.ST_CARDON)
                        //{
                        //    DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out _MediaDetected, out _MediaSrNbr); SmartFunctions.Instance.SetSerialNbr(_MediaSrNbr);                            
                        //    if (actToBeDoneOnMedia != null)
                        //        actToBeDoneOnMedia();

                        //    ActToAchieveReaderOp(opToBeDoneInCaseMediaIsProduced);
                        //    return true;
                        //}
                        //else
                        //    return false;
                    }
                case ReaderStatus.ST_DETECT_REMOVAL:
                    {
                        SmartFunctions.Instance.SwitchToCardOnState();
                        Thread.Sleep(50); // Putting it only because we need to wait for some amount after polling to get the correct result
                        SmartFunctions.Instance.SmartSyncDetectOkPassive(scenario, out _MediaDetected, out bSameMedia, out pStatusCSC);
                        _curStatus = (ReaderStatus)(pStatusCSC.ucStatCSC);

                        if ((ReaderStatus)(pStatusCSC.ucStatCSC) == ReaderStatus.ST_CARDON)
                        {
                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out _MediaDetected, out _MediaSrNbr); SmartFunctions.Instance.SetSerialNbr(_MediaSrNbr);
                            if (actToBeDoneOnMedia != null)
                                actToBeDoneOnMedia();

                            ActToAchieveReaderOp(opToBeDoneInCaseMediaIsProduced);
                            return true;
                        }
                        else
                            return false;
                    }
                default:
                    return false;
            }
        }
        
        protected void StatusListenerMediaProduced(
            IntPtr code, IntPtr status
            )
        {
            DateTime msgReceptionTimestamp = DateTime.Now;
            if (msgReceptionTimestamp < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                Logging.Log(LogLevel.Information, "StatusListenerMediaProduced: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            Communication.SendMessage("", "", "MediaProduced", SerializeHelper<StatusCSC>.XMLSerialize(pStatusCSC), msgReceptionTimestamp.Ticks.ToString());

            // TODO: I think it is necessary, else we leak memory. But executing it is causing crash. So, commenting it FTTB
            //Marshal.FreeHGlobal(status);

            code = IntPtr.Zero;
            status = IntPtr.Zero;
        }
        
        protected DateTime _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = new DateTime(2000, 1, 1);
        protected DateTime _tsWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast = new DateTime(2000, 1, 1);

        public enum ReaderStatus
        {
            ST_VIRGIN = CONSTANT.ST_VIRGIN,
            ST_INIT = CONSTANT.ST_INIT,
            ST_POLLON = CONSTANT.ST_POLLON,
            ST_CARDON  = CONSTANT.ST_CARDON,
            ST_DETECT_REMOVAL = CONSTANT.ST_DETECT_REMOVAL,
            ST_DISCR = CONSTANT.ST_DETECT_REMOVAL
        }

        protected ReaderStatus _curStatus;

        protected abstract void SwitchToDetectRemoval_ContentWithNonRealTime();
        protected abstract void SwitchToDetectRemoval_RealTime();
        protected abstract void SomeOperationsMayBeAskedToBePerformedOnThisMedia();        

        protected void StartPollingSync()
        {
            SmartFunctions.Instance.StartPollingEx(Scenario.SCENARIO_1, null);            
            _curStatus = ReaderStatus.ST_POLLON;
        }

        protected ReaderStatus GetCurrentStatusId()
        {
            if (_curStatus == 0)
            {
                // TODO: needs fix. because _MediaDetected and _MediaSrNbr wouldn't get updated properly, if the media is present, when reader gets connected
                StatusCSC status;
                SmartFunctions.Instance.SmartSyncDetectOkPassive(out status);
                _curStatus = (ReaderStatus)(status.ucStatCSC);
            }
            return _curStatus;
        }

        protected void RestartField()
        {
            Logging.Log(LogLevel.Verbose, "ThalesReader::RestartField");
            StopField();
            SmartFunctions.Instance.StartField();
        }

        internal void StopField()
        {
            Logging.Log(LogLevel.Verbose, "ThalesReader.StopField");
            SmartFunctions.Instance.StopField();
            _listener.FieldStopped();
            Thread.Sleep(50); // Giving sufficient time for any media appearance/disappearance to be raised, and subsequently ignored because of _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored
            _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
            _mediaIDsIgnored.Clear();            
            _curStatus = ReaderStatus.ST_INIT;
            _MediaDetected = SmartFunctions.MediaDetected.NONE;
            _MediaSrNbr = 0;
            SmartFunctions.Instance.SetSerialNbr(_MediaSrNbr);
        }

        protected void HaltCurrentCardNRestartPolling()
        {
            Debug.Assert(_curStatus == ReaderStatus.ST_CARDON);
            Logging.Log(LogLevel.Verbose, "ThalesReader.HaltCurrentCardNRestartPolling. Trace is " 
                //+ Environment.StackTrace
                );
            SmartFunctions.Instance.HaltCard();
            _mediaIDsIgnored.Add(SmartFunctions.Instance.ReadSNbr());
            SmartFunctions.Instance.StartPolling(SmartFunctions.Instance.GetActiveScenario(), (Utility.StatusListenerDelegate)StatusListenerMediaProduced);
            _curStatus = ReaderStatus.ST_POLLON;
        }

        virtual protected bool IsMediaProducedTreated()
        {
            return false;
        }

        public void MediaProduced(StatusCSC pStatusCSC, DateTime tsWhenMediaWasProduced)
        {
            ExtendTimerToPing();
            Logging.Log(LogLevel.Verbose, "MediaProduced");

            if (tsWhenMediaWasProduced < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored
                || tsWhenMediaWasProduced < _tsWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast)
            {
                Logging.Log(LogLevel.Information, "MEDIAPRODUCED: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }

            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out _MediaDetected, out _MediaSrNbr); SmartFunctions.Instance.SetSerialNbr(_MediaSrNbr);
            _curStatus = (ReaderStatus)(pStatusCSC.ucStatCSC);

            if (IsMediaProducedTreated())
            {
                Logging.Log(LogLevel.Verbose, "Returning because IsMediaProducedTreated is true");
                return;
            }
            if (_mediaIDsToIgnore.Count > 0)
            {
                if (_mediaIDsToIgnore.Contains(_MediaSrNbr))
                {
                    HaltCurrentCardNRestartPolling();
                    _listener.MediaInIgnoreListAppeared();
                    return;
                }
            }

            ReaderOp readerOp = _listener.MediaDetected(_MediaDetected, _MediaSrNbr);
            ActToAchieveReaderOp(readerOp);
        }

        private void ExtendTimerToPing()
        {
           
        }

        protected void ActToAchieveReaderOp(ReaderOp readerOp)
        {
            Logging.Log(LogLevel.Verbose, "ThalesReader.ActToAchieveReaderOp readerOp = " + readerOp.ToString());
            switch (readerOp)
            {
                case ReaderOp.StartPolling:
                    Debug.Assert(false);
                    break;
                case ReaderOp.NoPolling_StopPolling:
                    StopField();
                    break;
                case ReaderOp.SwitchToDetectRemoval_ContentWithNonRealTime:
                    SwitchToDetectRemoval_ContentWithNonRealTime();
                    break;
                case ReaderOp.SwitchToDetectRemoval_RealTime:
                    SwitchToDetectRemoval_RealTime();
                    break;
                case ReaderOp.SomeOperationsMayBeAskedToBePerformedOnThisMedia:
                    SomeOperationsMayBeAskedToBePerformedOnThisMedia();
                    break;
                case ReaderOp.MediaMustHaveGotAwayFromField:
                    MediaMustHaveGotAwayFromField();
                    break;
            }
        }

        protected long ReadSNbr(byte[] _serialNbrBytes)
        {
            long snbr = 0;
            for (int i = 0; i < 7; i++)
            {
                snbr *= 256;
                snbr += _serialNbrBytes[i];
            }
            return snbr;
        }

        protected void DetectTypeOfMediaNExtractSerialNumbers(ref StatusCSC pStatusCSC, out SmartFunctions.MediaDetected detectionState, out long SerialNbr)
        {
            detectionState = SmartFunctions.MediaDetected.NONE;
            byte[] serialNbrBytes = new byte[8];
            SmartFunctions.Instance._IsNFCCardDetected = false;
            if (pStatusCSC.xCardType == (int)CSC_TYPE.CARD_MIFARE1 && pStatusCSC.ucLgATR == 12)
            {
                byte[] ba = pStatusCSC.ucATR;
                //Logging.Log(LogLevel.Verbose, "ba.Length = " + ba.Length);
                byte SAK = ba[2];
                var typ = ((SAK >> 3) & 0x7);
                if (typ == 0)
                {
                    // ultralight
                    detectionState = SmartFunctions.MediaDetected.TOKEN;
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
                else if (typ == 4)
                {
                    // desfire
                    detectionState = SmartFunctions.MediaDetected.CARD;
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
                else if (typ == 5) // NFC Desfire is detected....
                {
                    if ((bool)(Configuration.ReadParameter("NFCFunctionality", "bool", "false")))
                    {
                        SmartFunctions.Instance._IsNFCCardDetected = true;
                        if (ba[3] == 0x40)// Gemalto NFC Desfire Sim card is detected...
                        {
                        }
                        detectionState = SmartFunctions.MediaDetected.CARD;
                        Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                    }
                }
                else
                {
                    // some other card of MiFare family, other than Ultralight and DESFire
                    detectionState = SmartFunctions.MediaDetected.UNSUPPORTEDMEDIA;
                }
            }
            SerialNbr = ReadSNbr(serialNbrBytes);
        } 
    }
}