using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules
{
    /// <summary>
    /// Enables continuous polling of underlying ReaderMediaMonitor. i.e. 
    /// - as soon as the media is removed from R/W, it StartPolling, 
    /// - as soon as the media is produced, it WaitForMediaRemoval
    /// It remains in this cycle until Stop() is called on it.
    /// 
    /// Lifetime: 
    /// - should be created when R/W has got connected.
    /// - should be destroyed when R/W has got disconnected
    /// </summary>
    class Poller
    {
        ReaderMediaMonitor mediaMonitor;
        CSC_READER_TYPE rwTyp;
        int hRw;

        // TODO: make this constructor parameters {CSC_READER_TYPE, int} of type 'object', so that this class is available for use with all readers.
        public Poller(ReaderMediaMonitor mediaMonitor_, CSC_READER_TYPE rwTyp_, int hRw_)
        {
            mediaMonitor = mediaMonitor_;
            mediaMonitor.AddMediaProducedListener(this.MediaProduced);
            mediaMonitor.AddMediaRemovedListener(this.MediaRemoved);

            rwTyp = rwTyp_;
            hRw = hRw_;
        }
        
        public void Start()
        {
            mediaMonitor.StartPolling(1);
        }

        DateTime dtWhenPollingWasStopped = new DateTime();
        public void Stop()
        {
            dtWhenPollingWasStopped = DateTime.Now;
            mediaMonitor.StopPolling();
        }

        void MediaProduced(StatusCSCEx status)
        {
            mediaMonitor.WaitForMediaRemoval();
        }

        void MediaRemoved(StatusCSCEx status)
        {
            mediaMonitor.StartPolling(1);
        }
    }
}