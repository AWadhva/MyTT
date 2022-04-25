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
        abstract public void StartPolling(object obj);
        virtual public void StopPolling()
        {
            if (statusCur != null)
            {
                RaiseMediaRemoved(statusCur);
                statusCur = null;
            }
        }

        abstract public void WaitForMediaRemoval();       

        StatusCSCEx statusCur;

        // for now, don't delete this function from comments
        //abstract public void SetIgnoreMediaList(List<int> media);

        // Normally, we should have used .net "event" for it. But "event" doesn't guarentee order of execution of subscribers. And it is a requirement for us.
        // Using below approach is not an issue because a. lifetime of its clients exceed its lifetime b. we don't need the facility to unsubscribe
        List<Action<StatusCSCEx>> MediaProduced = new List<Action<StatusCSCEx>>();
        List<Action<StatusCSCEx>> MediaRemoved = new List<Action<StatusCSCEx>>();

        public void AddMediaProducedListener(Action<StatusCSCEx> act)
        {
            MediaProduced.Add(act);
        }

        public void AddMediaRemovedListener(Action<StatusCSCEx> act)
        {
            MediaRemoved.Add(act);
        }

        protected void RaiseMediaProduced(StatusCSCEx status)
        {
            foreach (var listener in MediaProduced)
                listener(status);
        }

        protected void RaiseMediaRemoved(StatusCSCEx status)
        {
            foreach (var listener in MediaRemoved)
                listener(status);
        }
    }
}