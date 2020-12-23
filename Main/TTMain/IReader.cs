using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public enum ReaderOp
    {        
        SwitchToDetectRemoval_ContentWithNonRealTime,
        SwitchToDetectRemoval_RealTime, // inform me as soon as media is removed
        NoPolling_StopPolling,
        StartPolling,
        MediaMustHaveGotAwayFromField,
        SomeOperationsMayBeAskedToBePerformedOnThisMedia
    }

    interface IListener
    {
        ReaderOp MediaDetected(SmartFunctions.MediaDetected MediaDetectedState, long SerialNumber);
        void MediaRemoved();
        void FieldStopped();
        void MediaInIgnoreListAppeared();        
    }

    abstract class IReader
    {
        abstract public void StartPolling();
        abstract public void StopPolling();
        abstract public bool PollForAnyMediaAtMoment_AndPerformActionOnIt_IfNonePresentThenStopPolling(ReaderOp opToBeDoneInCaseMediaIsProduced, Action act);
        abstract public void SetState(ReaderOp readerOp);
        abstract public bool HasNativeSupportOfDetectionRemoval();
        abstract protected void MediaMustHaveGotAwayFromField();

        protected SmartFunctions.MediaDetected _MediaDetected = SmartFunctions.MediaDetected.NONE;
        protected long _MediaSrNbr = 0;

        public SmartFunctions.MediaDetected GetMediaDetected()
        {
            return _MediaDetected;
        }

        public long GetMediaSrNbr()
        {
            if (_MediaDetected == SmartFunctions.MediaDetected.NONE)
                return 0;
            else
                return _MediaSrNbr;
        }

        public void GetMediaDetectedState(out SmartFunctions.MediaDetected MediaDetected, out long MediaSrNbr)
        {
            MediaDetected = _MediaDetected;
            MediaSrNbr = GetMediaSrNbr();
        }

        virtual public void AddMediaToIgnoreList(long physicalId)
        {
            IFS2.Equipment.Common.Logging.Log(IFS2.Equipment.Common.LogLevel.Verbose, "AddMediaToIgnoreList physicalId = " + physicalId.ToString());
            _mediaIDsToIgnore.Add(physicalId);
        }

        // for our needs, we allow at most one media be halted. So, we can afford to clear both _mediaIDsToIgnore and _mediaIDsIgnored inside the same function
        virtual public void ClearIgnoreList()
        {
            _mediaIDsToIgnore.Clear();
            _mediaIDsIgnored.Clear();
        }

        // Though the client also can keep record, keeping same data at multiple places may cause maintainence problems
        public bool IsAnyMediaIgnored()
        {
            return (_mediaIDsIgnored.Count > 0);
        }

        protected List<long> _mediaIDsToIgnore = new List<long>();
        protected List<long> _mediaIDsIgnored = new List<long>();

        public void SetListener(IListener listener)
        {
            _listener = listener;
        }

        protected IListener _listener;        
    }
}
