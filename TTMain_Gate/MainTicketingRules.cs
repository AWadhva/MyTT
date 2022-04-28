using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Threading;
using IFS2.Equipment.TicketingRules.ConnectionMonitor;
using IFS2.Equipment.TicketingRules.SecurityModuleInitializer;
using IFS2.Equipment.TicketingRules.CommonTT;
using Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules : TaskThread
    {
        public MainTicketingRules()
            : base("MainTicketingRules")
        {
            InitParameterRelated();
            V4ReaderConf conf = new V4ReaderConf();
            conf.readerTyp = CSC_READER_TYPE.V4_READER;
            conf.rfPower = 2;
            cxnMonitor = new ThalesReaderConnectionMonitor(new V4ReaderApi(), this, "COM3:", 115200, conf);
            cxnMonitor.ReaderConnected += new Action<object>(ThalesReaderConnected);
            cxnMonitor.ReaderDisconnected += new Action<object>(ReaderDisconnected);            
        }        
        
        void ThalesReaderConnected(object obj)
        {
            var connectedReader = (ConnectedThalesReaderMin)obj;
            rdr1 = new ThalesReader(connectedReader, this, this.MediaProduced, this.MediaRemoved);
        }

        bool bSetAsEntry = true; // TODO: read it from context/configuration

        // TODO: this function is going to have 
        void MediaProduced(StatusCSCEx status)
        {
            if (bSetAsEntry)
                rdr1.curMediaTreatment = new CheckInTreatement(CSC_READER_TYPE.V4_READER, rdr1.connectedReader.handle, null);
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
        ThalesReader rdr1;

        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            if (TreatParametersMessageReceived(eventMessage))
                return 0;
            if (TreatCommonMessage(eventMessage))
                return 0;

            switch (eventMessage.EventID)
            {
                case "SetCheckinOrCheckOut":
                    {
                        int readerId = Convert.ToInt32(eventMessage._par[0]);
                        SetOperatingMode(readerId, eventMessage._par[1] == "1");
                        break;
                    }
                case "ResumeOperationOnRW":
                    {
                        int readerId = Convert.ToInt32(eventMessage._par[0]);
                        int messageId = Convert.ToInt32(eventMessage._par[0]);

                        if (messageId == suspendedOpTxnId)
                        {
                            var monitor = rdr1.mediaMonitor as V4ReaderMediaMonitor;
                            if (monitor != null)
                                monitor.MakeCardReady();

                            if (rdr1.curMediaTreatment != null)
                                rdr1.curMediaTreatment.Resume();
                        }
                        
                        break;
                    }
            }
            return base.TreatMessageReceived(eventMessage);
        }

        int suspendedOpTxnId;

        private void SetOperatingMode(int readerId, bool bCheckIn)
        {
            bSetAsEntry = bCheckIn;

            if (rdr1.poller != null)
                rdr1.poller.Stop();
            
            rdr1.poller.Start();
        }

#if WindowsCE
        public OpenNETCF.Threading.Semaphore semStopAsked = new OpenNETCF.Threading.Semaphore(0, 10000);
#else
        public Semaphore semStopAsked = new Semaphore(0, 10000);
#endif
    }
}