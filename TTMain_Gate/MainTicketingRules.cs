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

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules : TaskThread
    {
        public MainTicketingRules()
            : base("MainTicketingRules")
        {
            V4ReaderConf conf = new V4ReaderConf();
            conf.readerTyp = CSC_READER_TYPE.V4_READER;
            conf.rfPower = 2;
            mgr = new ThalesReaderConnectionMonitor(new V4ReaderApi(), this, "COM3:", 115200, conf);
            mgr.ReaderConnected += new Action<object>(ReaderConnected);
            mgr.ReaderDisconnected += new Action<object>(ReaderDisconnected);
        }

        void ReaderConnected(object obj)
        {
            ThalesReader reader = (ThalesReader)obj;

            bool bProductionSam = false; // ideally, it should have been in ISAMOnThalesReader
            cchsSamMgr = new CCHSSAMManger(CSC_READER_TYPE.V4_READER, reader.handle, bProductionSam);

            ISAMConf conf = new ISAMConf();
            conf.IsProductionSAM = false;
            conf._readDeviceIDInCCHSSAM = true;
            conf._signatureAtEachTransaction = true;
            conf.hRw = reader.handle;
            conf.mCCHSSAMMgr = cchsSamMgr;
            conf.slotId = 1;
            conf.readerType = CSC_READER_TYPE.V4_READER;

            ISAMOnThalesReader isam = new ISAMOnThalesReader(conf);
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

                mediaMonitor = new V4ReaderMediaMonitor(this, reader.handle, CSC_READER_TYPE.V4_READER, new List<int> { conf.slotId }, new List<ScenarioPolling> { sc1 }, new List<ScenarioPolling> { sc2 });
                Poller poller = new Poller(mediaMonitor, CSC_READER_TYPE.V4_READER, reader.handle);
                poller.Start();
            }
        }

        class Poller
        {
            ReaderMediaMonitor mediaMonitor;
            CSC_READER_TYPE rwTyp;
            int hRw;

            public Poller(ReaderMediaMonitor mediaMonitor_, CSC_READER_TYPE rwTyp_, int hRw_)
            {
                mediaMonitor = mediaMonitor_;
                rwTyp = rwTyp_;
                hRw = hRw_;
            }

            public void MediaProduced(StatusCSC status, DateTime dt)
            {
                SmartFunctions sf = new SmartFunctions();
                StatusCSCEx x = new StatusCSCEx(status);
                sf._delhiCCHSSAMUsage = true;
                sf._cryptoflexSAMUsage = false;

                sf.SetReaderType(rwTyp, hRw);
                sf.SetSerialNbr(x.SerialNumber);

                if (x.IsNFC)
                    if (Configuration.ReadBoolParameter("NFCFunctionality", false))
                        sf._IsNFCCardDetected = true;


                
                LogicalMedia logMedia = new LogicalMedia();
                if (x.IsDesFire)
                {
                    DelhiDesfireEV0 csc = new DelhiDesfireEV0(sf);
                    csc.ReadTPurseData(logMedia, MediaDetectionTreatment.Gate_CheckIn);
                }
                else if (x.IsUltraLight)
                {
                    DelhiTokenUltralight token = new DelhiTokenUltralight(sf, 0);
                    token.SetRWHandle(hRw);
                    token.ReadMediaData(logMedia, MediaDetectionTreatment.Gate_CheckIn);
                }
                mediaMonitor.DoneReadWriteWithThisMedia(MediaRemoved);
            }
            public void MediaRemoved(StatusCSC status, DateTime dt)
            {
                mediaMonitor.StartPolling(1, MediaProduced);
            }
            public void Start()
            {
                mediaMonitor.StartPolling(1, MediaProduced);
            }
        }

        //private void DummyPollCycle()
        //{
        //    mediaMonitor.StartPolling(1, (x, y) => { Console.WriteLine(x.ToString() + " " + y.ToString()); });
        //}

        void ReaderDisconnected(object obj)
        {
            cchsSamMgr = null;
            isam = null;
        }

        ReaderConnectionMonitor mgr; // one such object per r/w
        CCHSSAMManger cchsSamMgr;    // one such object per r/w per its connected session. gets destroyed when r/w gets disconnected.
        ISAMOnThalesReader isam; // one such object for every ISAM.
        V4ReaderMediaMonitor mediaMonitor;  // one such object per V4 r/w per its connected session. gets destroyed when r/w gets disconnected.

        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            if (TreatParametersMessageReceived(eventMessage))
                return 0;
            if (TreatCommonMessage(eventMessage))
                return 0;

            switch (eventMessage.EventID)
            {
            }
            return base.TreatMessageReceived(eventMessage);
        }
#if WindowsCE
        public OpenNETCF.Threading.Semaphore semStopAsked = new OpenNETCF.Threading.Semaphore(0, 10000);
#else
        public Semaphore semStopAsked = new Semaphore(0, 10000);
#endif
    }
}