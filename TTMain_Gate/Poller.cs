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
    class Poller
    {
        ReaderMediaMonitor mediaMonitor;
        CSC_READER_TYPE rwTyp;
        int hRw;
        IMediaTreatment mediaTreatement;

        public Poller(ReaderMediaMonitor mediaMonitor_, CSC_READER_TYPE rwTyp_, int hRw_, IMediaTreatment mediaTreatement_)
        {
            mediaMonitor = mediaMonitor_;
            rwTyp = rwTyp_;
            hRw = hRw_;
            mediaTreatement = mediaTreatement_;
        }

        void MediaProduced(StatusCSCEx status, DateTime dt)
        {
            mediaTreatement.Do(status, dt);

            mediaMonitor.DoneReadWriteWithThisMedia(MediaRemoved);
        }
        void MediaRemoved(StatusCSCEx status, DateTime dt)
        {
            mediaMonitor.StartPolling(1, MediaProduced);
        }
        public void Start()
        {
            mediaMonitor.StartPolling(1, MediaProduced);
        }
    }
}