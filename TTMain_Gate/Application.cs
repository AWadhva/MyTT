using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.ConnectionMonitor;
using Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules.Gate
{
    public class Application
    {
        public Application(ISyncContext syncContext_, IActionTransmitter actionTransmitter_)
        {
            V4ReaderConf conf = new V4ReaderConf();
            conf.readerTyp = CSC_READER_TYPE.V4_READER;
            conf.rfPower = 2;
            syncContext = syncContext_;
            cxnMonitor = new ThalesReaderConnectionMonitor(new V4ReaderApi(), syncContext_, "COM3:", 115200, conf);
            cxnMonitor.ReaderConnected += new Action<object>(ThalesReaderConnected);
            cxnMonitor.ReaderDisconnected += new Action<object>(ReaderDisconnected);
            actionTransmitter = actionTransmitter_;
        }

        int suspendedOpTxnId;
        readonly ISyncContext syncContext;
        readonly IActionTransmitter actionTransmitter;

        public void ResumeOperationOnRW(int readerId, int messageId)
        {
            if (messageId == suspendedOpTxnId)
            {
                var monitor = rdr1.mediaMonitor as V4ReaderMediaMonitor;
                if (monitor != null)
                    monitor.MakeCardReady();

                if (rdr1.curMediaTreatment != null)
                    rdr1.curMediaTreatment.Resume();
            }
        }

        public void SetOperatingMode(int readerId, bool bCheckIn)
        {
            bSetAsEntry = bCheckIn;

            if (rdr1.poller != null)
                rdr1.poller.Stop();

            rdr1.poller.Start();
        }

        void ThalesReaderConnected(object obj)
        {
            var connectedReader = (ConnectedThalesReaderMin)obj;
            rdr1 = new ThalesReader(connectedReader, syncContext, this.MediaProduced, this.MediaRemoved);
        }

        bool bSetAsEntry = true; // TODO: read it from context/configuration

        
        // TODO: this function is going to have 
        void MediaProduced(StatusCSCEx status)
        {
            if (bSetAsEntry)
                rdr1.curMediaTreatment = new CheckInTreatement(CSC_READER_TYPE.V4_READER, rdr1.connectedReader.handle, null, actionTransmitter);
            else
                rdr1.curMediaTreatment = new CheckOutTreatement(CSC_READER_TYPE.V4_READER, rdr1.connectedReader.handle);
            rdr1.curMediaTreatment.Do(status);
        }

        void MediaRemoved(StatusCSCEx status)
        {
            rdr1.curMediaTreatment = null;
        }

        void ReaderDisconnected(object obj)
        {
            rdr1.poller.Stop();
            rdr1 = null;
        }

        ReaderConnectionMonitor cxnMonitor; // one such object per r/w
        ThalesReader rdr1; // one such object per r/w per Connection session. Gets created when the r/w gets connected. Gets destroyed when r/w gets disconnected
    }
}