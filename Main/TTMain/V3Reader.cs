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
    class V3Reader : ThalesReader
    {
        // much better would have been an interface like ITimerProvider
        MainTicketingRules _timerServiceProvider;

        public V3Reader(MainTicketingRules timerServiceProvider)
        {
            _timerServiceProvider = timerServiceProvider;
            _curStatus = ReaderStatus.ST_INIT; // this may be incorrect, if r/w is not connected. but for now, it does no harm
        }

        protected override void SomeOperationsMayBeAskedToBePerformedOnThisMedia()
        {
            _timerServiceProvider.StopTimer(MainTicketingRules.Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved);
            RestartField();
        }

        protected override void SwitchToDetectRemoval_ContentWithNonRealTime()
        {
            throw new NotImplementedException();
        }

        protected override void SwitchToDetectRemoval_RealTime()
        {
            _timerServiceProvider.StopTimer(MainTicketingRules.Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved);
            Logging.Log(LogLevel.Verbose, "V3Reader::SwitchToDetectRemoval_RealTime");            
            _bCheckingForMediaRemovalAggressively = true;
			_MediaSrNbrBeingWaitedForRemoval = _MediaSrNbr;
            _timerServiceProvider.StartTimer(MainTicketingRules.Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly);
            //CheckForMediaRemoved();
        }

        public override void StopPolling()
        {
            _timerServiceProvider.StopTimer(MainTicketingRules.Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved);
            Logging.Log(LogLevel.Verbose, "V3Reader::StopPolling");
            switch (_curStatus)
            {
                case ReaderStatus.ST_POLLON:
                    {
                        SmartFunctions.Instance.StopPolling();
                        // But may be we the media was already produced, so the reader would have been in CARD_ON state
                        StatusCSC statusCSC;
                        SmartFunctions.Instance.SmartSyncDetectOkPassive(out statusCSC);
                        _curStatus = (ReaderStatus)(statusCSC.ucStatCSC);
                        if ((ReaderStatus)statusCSC.ucStatCSC == ReaderStatus.ST_CARDON)
                            RestartField();
                        break;
                    }
                case ReaderStatus.ST_CARDON:
                    RestartField();
                    break;
                default:
                    {
                        StatusCSC statusCSC;
                        SmartFunctions.Instance.SmartSyncDetectOkPassive(out statusCSC);
                        _curStatus = (ReaderStatus)(statusCSC.ucStatCSC);
                        break;
                    }
            }
            _curStatus = ReaderStatus.ST_INIT;            
			_bCheckingForMediaRemovalAggressively = false;
            _MediaSrNbrBeingWaitedForRemoval = 0;
        }

        public void MediaRemovedInt(long ticketPhysicalId, DateTime tsWhenMediaWasRemoved)
        {
            _curStatus = ReaderStatus.ST_INIT;
            _listener.MediaRemoved();
        }
        
        public override void StartPolling()
        {
            if (_curStatus != ReaderStatus.ST_INIT)
                return;
            SmartFunctions.Instance.StartPollingEx(Scenario.SCENARIO_1, StatusListenerMediaProduced);
            _curStatus = ReaderStatus.ST_POLLON;
        }

        bool _bCheckingForMediaRemovalAggressively;
        bool _bCheckingForMediaRemovalNonAggressively = false;
        long _MediaSrNbrBeingWaitedForRemoval;

        internal void CheckForMediaRemoved()
        {
            if (!_bCheckingForMediaRemovalAggressively)
                return;

            if (_curStatus != ReaderStatus.ST_INIT)
                RestartField();

            bool bDifferentMedia = false;
            bool bAnyMediaPresent = PollForAnyMediaAtMoment_AndPerformActionOnIt_IfNonePresentThenStopPolling(ReaderOp.NoPolling_StopPolling, delegate() 
            {
                bDifferentMedia = (_MediaSrNbr != _MediaSrNbrBeingWaitedForRemoval);
            }
            );

            bool bMediaRemoved = !bAnyMediaPresent || (bAnyMediaPresent && bDifferentMedia);
            if (!bMediaRemoved)
            {
                Thread.Sleep(1000); // so that we don't burn the reader by restarting field too aggressively.
                _timerServiceProvider.StartTimer(IFS2.Equipment.TicketingRules.MainTicketingRules.Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly);
            }
            else
            {
                if (bDifferentMedia)
                    RestartField();
                _bCheckingForMediaRemovalAggressively = false;
                _listener.MediaRemoved();
            }
        }

        protected override void MediaMustHaveGotAwayFromField()
        {
            Logging.Log(LogLevel.Verbose, "V3Reader.MediaMustHaveGotAwayFromField");

            RestartField();
            StartPolling();
        }

        public override void SetState(ReaderOp readerOp)
        {
            Logging.Log(LogLevel.Verbose, "V3Reader.SetState " + readerOp.ToString());

            if (_curStatus != ReaderStatus.ST_INIT)
                RestartField();

            StartPolling();
            _timerServiceProvider.StartTimer(MainTicketingRules.Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved);
        }

        protected override bool IsMediaProducedTreated()
        {
            if (_MediaSrNbrBeingWaitedForRemoval != 0 && _bCheckingForMediaRemovalNonAggressively)
            {
                if (_MediaSrNbr == _MediaSrNbrBeingWaitedForRemoval)
                {
                    HaltCurrentCardNRestartPolling();
                    _timerServiceProvider.StartTimer(MainTicketingRules.Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved);
                }
                else
                {
                    _timerServiceProvider.StopTimer(MainTicketingRules.Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved);
                    RestartField();
                    _listener.MediaRemoved();
                }
                return true;
            }
            else
                return false;
        }

        internal void Timeout_TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved()
        {
            _listener.MediaRemoved();
        }

        public override bool HasNativeSupportOfDetectionRemoval()
        {
            return false;
        }
    }
}