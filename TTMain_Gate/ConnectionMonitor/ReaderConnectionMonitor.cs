using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public abstract class ReaderConnectionMonitor
    {
        public ReaderConnectionMonitor(
            ISyncContext syncContext_
            )
        {
            syncContext = syncContext_;
 
            timerCheckForInitialization = new MyTimer(syncContext, CheckConnection);
            timerCheckForInitialization.StartOneShot(0);

            timerRoutineCheck = new MyTimer(syncContext, RoutineCheck);
        }

        MyTimer timerCheckForInitialization;
        MyTimer timerRoutineCheck;
        bool bConnected = false;

        protected ISyncContext syncContext;
        public event Action<object> ReaderConnected; // the parameter would contain the details of the reader which is connected.
        public event Action<object> ReaderDisconnected;

        void CheckConnection()
        {
            object readerDetails;
            if (!bConnected)
            {
                if (Init(out readerDetails))
                {                    
                    if (ReaderConnected != null)
                        ReaderConnected(readerDetails);
                    bConnected = true;
                }
                else
                    timerCheckForInitialization.StartOneShot(1000);
            }
            if (bConnected)
                timerRoutineCheck.StartOneShot(1000);
        }

        void RoutineCheck()
        {
            bConnected = Connected();
            if (bConnected)
                timerRoutineCheck.StartOneShot(1000);
            else
            {
                timerCheckForInitialization.StartOneShot(1000);
                if (ReaderDisconnected != null)
                    ReaderDisconnected(null);
            }
        }

        abstract public bool Init(out object readerDetails); // let's constrain the 'readerDetails' to its handle only. rest of the things like firmware etc. would be taken care of separatly
        abstract public bool Connected();
    }
}