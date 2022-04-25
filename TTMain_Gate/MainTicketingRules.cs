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
            connectedReader = (ConnectedThalesReaderMin)obj;

            bool bProductionSam = false; // ideally, it should have been in ISAMOnThalesReader
            cchsSamMgr = new CCHSSAMManger(CSC_READER_TYPE.V4_READER, connectedReader.handle, bProductionSam);

            ISAMConf conf = new ISAMConf();
            conf.IsProductionSAM = false;
            conf._readDeviceIDInCCHSSAM = true;
            conf._signatureAtEachTransaction = true;
            conf.hRw = connectedReader.handle;
            conf.mCCHSSAMMgr = cchsSamMgr;
            conf.slotId = 1;
            conf.readerType = CSC_READER_TYPE.V4_READER;

            isam = new ISAMOnThalesReader(conf);
            bool bPresent;
            bool bWorking;
            isam.Initialize(out bPresent, out bWorking);
            if (bPresent && bWorking)
            {
                ScenarioPolling sc1 = new ScenarioPolling();
                sc1.ucAntenna = 1;
                sc1.ucRepeatNumber = 1;
                sc1.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);

                ScenarioPolling sc2 = new ScenarioPolling();
                sc2.ucAntenna = 2;
                sc2.ucRepeatNumber = 1;
                sc2.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);

                V4ReaderMediaMonitor.V4ReaderAssembly rdr = new V4ReaderMediaMonitor.V4ReaderAssembly();
                rdr.handle = connectedReader.handle;
                rdr.isamSlots = new List<int> { conf.slotId };
                rdr.rwTyp = CSC_READER_TYPE.V4_READER;
                rdr.scenario1 = new List<ScenarioPolling> { sc1 };
                rdr.scenario2 = new List<ScenarioPolling> { sc2 };
                mediaMonitor = new V4ReaderMediaMonitor(this, rdr);
                mediaMonitor.AddMediaProducedListener(this.MediaProduced);
                mediaMonitor.AddMediaRemovedListener(this.MediaRemoved);

                poller = new Poller(mediaMonitor, CSC_READER_TYPE.V4_READER, connectedReader.handle);
                
                poller.Start();
            }
        }

        bool bSetAsEntry = true; // TODO: read it from context/configuration

        // TODO: this function is going to have 
        void MediaProduced(StatusCSCEx status)
        {
            if (bSetAsEntry)
                curMediaTreatment = new CheckInTreatement(CSC_READER_TYPE.V4_READER, connectedReader.handle, null);
            else
                curMediaTreatment = new CheckOutTreatement(CSC_READER_TYPE.V4_READER, connectedReader.handle);
            curMediaTreatment.Do(status);
        }

        void MediaRemoved(StatusCSCEx status)
        {
            curMediaTreatment = null;
        }

        void ReaderDisconnected(object obj)
        {
            cchsSamMgr = null;
            isam = null;
            connectedReader = null;
            mediaMonitor = null;
            curMediaTreatment = null;
            poller = null;
        }

        ReaderConnectionMonitor cxnMonitor; // one such object per r/w
        CCHSSAMManger cchsSamMgr;    // one such object per r/w per its connected session. gets destroyed when r/w gets disconnected.
        ISAMOnThalesReader isam; // one such object for every ISAM.
        V4ReaderMediaMonitor mediaMonitor;  // one such object per V4 r/w per its connected session. gets destroyed when r/w gets disconnected.        
        ConnectedThalesReaderMin connectedReader; // 1-per-reader per session

        // It is reader-type independent (ALMOST). TODO:Make it completly reader-type independent.
        IMediaTreatment curMediaTreatment = null; // 1-per-media appearance. Gets destroyed when media is removed

        // It is reader-type independent (ALMOST). TODO:Make it completly reader-type independent.
        Poller poller; // 1-per-reader per session

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
                            var monitor = mediaMonitor as V4ReaderMediaMonitor;
                            if (monitor != null)
                                monitor.MakeCardReady();

                            if (curMediaTreatment != null)
                                curMediaTreatment.Resume();
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

            if (poller != null)
                poller.Stop();
            
            poller.Start();
        }

#if WindowsCE
        public OpenNETCF.Threading.Semaphore semStopAsked = new OpenNETCF.Threading.Semaphore(0, 10000);
#else
        public Semaphore semStopAsked = new Semaphore(0, 10000);
#endif
    }
}