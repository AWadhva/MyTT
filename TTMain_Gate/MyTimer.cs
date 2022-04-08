using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Threading;

namespace IFS2.Equipment.TicketingRules
{
    public class MyTimer
    {
        public MyTimer(ISyncContext context_, Action act_)
        {
            context = context_;
            act = act_;

            timer = new Timer(new TimerCallback(Timeout), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        private void Timeout(Object stateInfo)
        {            
            context.Message(act);
        }

        public void Start(int firstTickInMs, int nextTickInMs)
        {
            timer.Change(firstTickInMs, nextTickInMs);
        }

        public void StartOneShot(int firstTickInMs)
        {
            timer.Change(firstTickInMs, System.Threading.Timeout.Infinite);
        }

        public void Stop()
        {
            timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        ISyncContext context; // synchronization context in which 'act' would be called
        Action act; 
        Timer timer;
    }
}