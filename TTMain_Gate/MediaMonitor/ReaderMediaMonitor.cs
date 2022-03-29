using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    public abstract class ReaderMediaMonitor
    {
        abstract public void StartPolling(object obj, Action<
            StatusCSC, // TODO: StatusCSC is Thales Reader specific. Change it to more generic
            DateTime> MediaProduced_
            );
        abstract public void StopPolling();        
        abstract public void DoneReadWriteWithThisMedia(Action<StatusCSC, // TODO: StatusCSC is Thales Reader specific. Change it to more generic
            DateTime> MediaRemoved_);
        abstract public void SetIgnoreMediaList(List<int> media);
        
        //public void SetCallbacks()
        //{
        //    MediaProduced = MediaProduced_;
        //    MediaRemoved = MediaRemoved_;
        //}

        public Action<StatusCSC, DateTime> MediaProduced;
        public Action<StatusCSC, DateTime> MediaRemoved;
    }
}
