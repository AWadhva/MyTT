﻿using System;
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
        public Application(ISyncContext syncContext_, ITransmitter actionTransmitter_)
        {
            V4ReaderConf conf = new V4ReaderConf();
            conf.readerTyp = CSC_READER_TYPE.V4_READER;
            conf.rfPower = 2;
            syncContext = syncContext_;
            
            cxnMonitor1 = new ThalesReaderConnectionMonitor(new V4ReaderApi(), syncContext_, "COM3:", 115200, conf);
            cxnMonitor1.ReaderConnected += new Action<object>(x => ThalesReaderConnected(1, x));
            cxnMonitor1.ReaderDisconnected += new Action(() => ReaderDisconnected(1));

            //cxnMonitor2 = new ThalesReaderConnectionMonitor(2, new V4ReaderApi(), syncContext_, "COM2:", 115200, conf);
            //cxnMonitor2.ReaderConnected += new Action<object>(ThalesReaderConnected);
            //cxnMonitor2.ReaderDisconnected += new Action<int>(ReaderDisconnected);

            actionTransmitter = actionTransmitter_;
        }

        int suspendedOpTxnId;
        readonly ISyncContext syncContext;
        readonly ITransmitter actionTransmitter;

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

            bSetAsEntry = bCheckIn; // TODO: correct it

            rdr.poller.Stop();
            rdr.poller.Start();
        }

        void ThalesReaderConnected(int rdrMnemonic, object obj)
        {
            var connectedReader = (ConnectedThalesReaderMin)obj;
            rdr1 = new ThalesReader(connectedReader, syncContext, 
                x => this.MediaProduced(1, x),
                x => this.MediaRemoved(1, x));
        }

        bool bSetAsEntry = true; // TODO: read it from context/configuration

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

            if (bSetAsEntry)
                rdr1.curMediaTreatment = new CheckInTreatement(CSC_READER_TYPE.V4_READER, rdr1.connectedReader.handle, null,
                    (act, pars) => { actionTransmitter.Transmit(1, act, pars); });
            else
                rdr1.curMediaTreatment = new CheckOutTreatement(CSC_READER_TYPE.V4_READER, rdr1.connectedReader.handle);
            
            rdr1.curMediaTreatment.Do(status);
        }

        void MediaRemoved(int rdrMnemonic, StatusCSCEx status)
        {
            var rdr = GetReader(rdrMnemonic);

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
        }

        ReaderConnectionMonitor cxnMonitor1, cnxMonitor2; // one such object per r/w
        ThalesReader rdr1, rdr2; // one such object per r/w per Connection session. Gets created when the r/w gets connected. Gets destroyed when r/w gets disconnected
    }
}