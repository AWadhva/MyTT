using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    class V4Reader : ThalesReader
    {
        public V4Reader(): base()
        {
            SmartFunctions.Instance.listenerCardRemoved = StatusListenerMediaRemoved;
        }

        protected override void SomeOperationsMayBeAskedToBePerformedOnThisMedia()
        {
            try
            {
                SmartFunctions.Instance.SwitchToDetectRemovalStateEx();
                _curStatus = ReaderStatus.ST_DETECT_REMOVAL;
                _tsWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast = DateTime.Now;                
            }
            catch(ReaderException exp)
            {
                if (exp.Code == CSC_API_ERROR.ERR_NOEXEC)
                    _curStatus = ReaderStatus.ST_INIT;
                else
                    throw exp;
            }
        }

        protected override void SwitchToDetectRemoval_ContentWithNonRealTime()
        {
            SmartFunctions.Instance.SwitchToDetectRemovalStateEx();
            // NO, it may be counter-productive. because we may deliberatly ignore mediaremoved event, and will not start polling
            //_tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now; // TODO: Not sure how useful it is to put this statement in detection removal
            _curStatus = ReaderStatus.ST_DETECT_REMOVAL;
            SetWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast();
        }

        protected override void SwitchToDetectRemoval_RealTime()
        {
            SmartFunctions.Instance.SwitchToDetectRemovalStateEx();
            _curStatus = ReaderStatus.ST_DETECT_REMOVAL;
            SetWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast();
        }

        private void SetWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast()
        {
            Thread.Sleep(50);
            _tsWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast = DateTime.Now;
        }

        public override void StopPolling()
        {    
            Logging.Log(LogLevel.Verbose, "V4Reader::StopPolling");
            switch (GetCurrentStatusId())
            {
                case ReaderStatus.ST_POLLON:
                    {
                        SmartFunctions.Instance.StopPolling();
                        // But may be we the media was already produced, so the reader would have been in CARD_ON state
                        StatusCSC statusCSC;
                        SmartFunctions.Instance.SmartSyncDetectOkPassive(out statusCSC);
                        _curStatus = (ReaderStatus)(statusCSC.ucStatCSC);
                        if (_curStatus == ReaderStatus.ST_CARDON)
                            StopField();
                        break;
                    }
                case ReaderStatus.ST_CARDON:
                case ReaderStatus.ST_DETECT_REMOVAL:
                    StopField();
                    break;
            }
            _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
            _curStatus = ReaderStatus.ST_INIT;
        }

        public void MediaRemovedInt(long ticketPhysicalId, DateTime tsWhenMediaWasRemoved)
        {
            if (tsWhenMediaWasRemoved < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                return;
            }
            _curStatus = ReaderStatus.ST_INIT;
            _MediaDetected = SmartFunctions.MediaDetected.NONE;
            _listener.MediaRemoved();
        }

        public void StatusListenerMediaRemoved(
            IntPtr code, IntPtr status
            )
        {
            DateTime msgReceptionTimestamp = DateTime.Now;
            if (msgReceptionTimestamp < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                Logging.Log(LogLevel.Information, "StatusListenerMediaRemoved: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            // memory occupied by status gets leaked, but we are helpless, as attempt to free it causes crash

            Communication.SendMessage("", "", "MediaRemovedInt", //SerializeHelper<StatusCSC>.XMLSerialize(pStatusCSC),
                msgReceptionTimestamp.Ticks.ToString());
            code = IntPtr.Zero;
            status = IntPtr.Zero;
            // It causes problem. Anyway, it was redundant, and is correctly set inside MediaRemovedInt
            //_curStatus = ReaderStatus.ST_INIT;
        }
        
        public override void StartPolling()
        {
            switch (GetCurrentStatusId())
            {
                case ReaderStatus.ST_DETECT_REMOVAL:
                    {
                        CSC_API_ERROR err = SmartFunctions.Instance.SwitchToCardOnState();
                        if (err == CSC_API_ERROR.ERR_NONE)
                        {
                            _curStatus = ReaderStatus.ST_CARDON;
                            ReaderOp op = _listener.MediaDetected(_MediaDetected, _MediaSrNbr);
                            ActToAchieveReaderOp(op);
                        }
                        break;
                    }
                case ReaderStatus.ST_INIT:
                    {
                        SmartFunctions.Instance.StartPollingEx(Scenario.SCENARIO_1, StatusListenerMediaProduced);
                        _curStatus = ReaderStatus.ST_POLLON;
                        break;
                    }
            }
        }

        protected override void MediaMustHaveGotAwayFromField()
        {
            SmartFunctions.Instance.SwitchToDetectRemovalStateEx();
            _curStatus = ReaderStatus.ST_DETECT_REMOVAL;
            SetWhenSwitchToDetectRemovalWasExecutedSuccessfullyLast();
        }

        public override void SetState(ReaderOp readerOp)
        {
            // at least for now, there is no reason to do anything in it for V4 reader.
        }

        public override bool HasNativeSupportOfDetectionRemoval()
        {
            return true;
        }
    }
}
