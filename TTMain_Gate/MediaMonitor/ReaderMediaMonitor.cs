using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using Common;

namespace IFS2.Equipment.TicketingRules
{
    public abstract class ReaderMediaMonitor
    {
        abstract public void StartPolling(object obj, Action<StatusCSCEx, DateTime> MediaProduced_);
        abstract public void StopPolling();
        abstract public void DoneReadWriteWithThisMedia(Action<StatusCSCEx, DateTime> MediaRemoved_);

        // for now, don't delete this function from comments
        //abstract public void SetIgnoreMediaList(List<int> media);

        public Action<StatusCSCEx, DateTime> MediaProduced;
        public Action<StatusCSCEx, DateTime> MediaRemoved;
    }
}
