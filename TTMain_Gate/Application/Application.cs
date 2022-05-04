using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.ConnectionMonitor;
using Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;
using IFS2.Equipment.TicketingRules.MediaMonitor;

namespace IFS2.Equipment.TicketingRules.Gate
{
    public class Application
    {
        public Application(ISyncContext syncContext_, ITransmitter transmitter_)
        {
            V4ReaderConf conf = new V4ReaderConf();
            conf.readerTyp = CSC_READER_TYPE.V4_READER;
            conf.rfPower = 2;
            syncContext = syncContext_;
            
            cxnMonitor1 = new ThalesReaderConnectionMonitor(new V4ReaderApi(), syncContext_, "COM3:", 115200, conf);
            cxnMonitor1.ReaderConnected += new Action<object>(x => ReaderConnected(1, x));
            cxnMonitor1.ReaderDisconnected += new Action(() => ReaderDisconnected(1));

            cxnMonitor2 = new ThalesReaderConnectionMonitor(new V4ReaderApi(), syncContext_, "COM4:", 115200, conf);
            cxnMonitor2.ReaderConnected += new Action<object>(x => ReaderConnected(2, x));
            cxnMonitor2.ReaderDisconnected += new Action(() => ReaderDisconnected(2));
            transmitter = transmitter_;
        }

        int suspendedOpTxnId;
        readonly ISyncContext syncContext;
        readonly ITransmitter transmitter;

        public void ResumeOperationOnRW(int rdrMnemonic, int messageId)
        {
            //var rdr = GetReader(rdrMnemonic);

            if (messageId == suspendedOpTxnId)
            {
                var monitor = rdr1.mediaMonitor as V4ReaderMediaMonitor;
                if (monitor != null)
                    monitor.MakeCardReady();

                if (rdr1.curMediaTreatment != null)
                    rdr1.curMediaTreatment.Resume();
            }
        }

        public void SetOperatingMode(int rdrMnemonic, bool bCheckIn)
        {
            var rdr = GetReader(rdrMnemonic);
            if (rdrMnemonic == 1)
                bSet_1_AsEntry = bCheckIn;
            else
                bSet_1_AsEntry = !bCheckIn;

            rdr.poller.Stop();
            rdr.poller.Start();
        }

        void ReaderConnected(int rdrMnemonic, object obj)
        {
            var connectedReader = (ConnectedThalesReaderMin)obj;

            if (rdrMnemonic == 1)
                rdr1 = new ThalesReader(connectedReader, syncContext, 
                    x => this.MediaProduced(1, x),
                    x => this.MediaRemoved(1, x));
            else
                rdr2 = new ThalesReader(connectedReader, syncContext,
                    x => this.MediaProduced(2, x),
                    x => this.MediaRemoved(2, x));

            transmitter.ReaderConnected(rdrMnemonic);
        }

        bool bSet_1_AsEntry = true; // TODO: read it from context/configuration

        ThalesReader GetReader(int rdrMnemonic)
        {
            if (rdrMnemonic == 1)
                return rdr1;
            else if (rdrMnemonic == 2)
                return rdr2;
            else
                return null;
        }

        // It will not be this simple.
        void MediaProduced(int rdrMnemonic, StatusCSCEx status)
        {
            var rdr = GetReader(rdrMnemonic);
            transmitter.MediaProduced(rdrMnemonic);
            
            if (rdrMnemonic == 1)
                if (bSet_1_AsEntry)
                    rdr.curMediaTreatment = new CheckInTreatement(CSC_READER_TYPE.V4_READER, rdr.connectedReader.handle, null,
                        (act, pars) => { transmitter.MediaTreated(1, act, pars); });
                else
                    rdr.curMediaTreatment = new CheckOutTreatement(CSC_READER_TYPE.V4_READER, rdr.connectedReader.handle, null,
                        (act, pars) => { transmitter.MediaTreated(1, act, pars); });
            else
                if (!bSet_1_AsEntry)
                    rdr.curMediaTreatment = new CheckInTreatement(CSC_READER_TYPE.V4_READER, rdr.connectedReader.handle, null,
                        (act, pars) => { transmitter.MediaTreated(2, act, pars); });
                else
                    rdr.curMediaTreatment = new CheckOutTreatement(CSC_READER_TYPE.V4_READER, rdr.connectedReader.handle, null,
                        (act, pars) => { transmitter.MediaTreated(2, act, pars); });
            
            rdr.curMediaTreatment.Do(status);
        }

        void MediaRemoved(int rdrMnemonic, StatusCSCEx status)
        {
            var rdr = GetReader(rdrMnemonic);
            transmitter.MediaProduced(rdrMnemonic);

            rdr.curMediaTreatment = null;
        }

        void ReaderDisconnected(int rdrMnemonic)
        {
            var rdr = GetReader(rdrMnemonic);
            rdr.poller.Stop();

            if (rdrMnemonic == 1)
                rdr1 = null;
            else if (rdrMnemonic == 2)
                rdr2 = null;
            transmitter.ReaderDisconnected(rdrMnemonic);
        }

        ReaderConnectionMonitor cxnMonitor1, cxnMonitor2; // one such object per r/w
        ThalesReader rdr1, rdr2; // one such object per r/w per Connection session. Gets created when the r/w gets connected. Gets destroyed when r/w gets disconnected
    }
}