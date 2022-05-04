using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.SecurityModuleInitializer;
using IFS2.Equipment.TicketingRules.MediaTreatment;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using Common;
using IFS2.Equipment.TicketingRules.MediaMonitor;

namespace IFS2.Equipment.TicketingRules
{
    /// <summary>
    /// Lifetime: from the reader found connected until it doesn't get disconnected
    /// </summary>
    class ThalesReader
    {
        public CCHSSAMManger cchsSamMgr;
        public ISAMOnThalesReader isam;
        public V4ReaderMediaMonitor mediaMonitor;
        public ConnectedThalesReaderMin connectedReader;

        // It is reader-type independent (ALMOST). TODO:Make it completly reader-type independent.
        public IMediaTreatment curMediaTreatment = null;

        // It is reader-type independent (ALMOST). TODO:Make it completly reader-type independent.
        public Poller poller;

        public ThalesReader(ConnectedThalesReaderMin rdrMin, ISyncContext syncContext, Action<StatusCSCEx> MediaProduced, Action<StatusCSCEx> MediaRemoved)
        {
            connectedReader = rdrMin;

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
                mediaMonitor = new V4ReaderMediaMonitor(syncContext, rdr);
                mediaMonitor.AddMediaProducedListener(MediaProduced);
                mediaMonitor.AddMediaRemovedListener(MediaRemoved);

                poller = new Poller(mediaMonitor, CSC_READER_TYPE.V4_READER, connectedReader.handle);

                poller.Start();
            }
        }
    }
}