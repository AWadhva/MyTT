using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.Common;
using System.Runtime.InteropServices;
using Common;

namespace IFS2.Equipment.TicketingRules.MediaMonitor
{
    public class V4ReaderMediaMonitor : ReaderMediaMonitor
    {
        #region ReaderMediaMonitor
        public override void StartPolling(object obj)
        {
            int scenario = (int)obj;

            Reader.StartPolling(rwTyp, rw, (byte)scenario, V4Reader_MediaCallbacks.StatusListenerMediaProduced);
        }

        public override void StopPolling()
        {
            base.StopPolling();

            _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
            Reader.StopField(rwTyp, rw);
        }
        
        public override void WaitForMediaRemoval()
        {
            Reader.SwitchToDetectRemovalState(rwTyp, rw, V4Reader_MediaCallbacks.StatusListenerMediaRemoved);
        }
        #endregion
        ISyncContext syncContext;

        public class V4ReaderAssembly
        {
            public int handle;
            public CSC_READER_TYPE rwTyp;
            public List<int> isamSlots;
            public List<ScenarioPolling> scenario1;
            public List<ScenarioPolling> scenario2;
        }

        public V4ReaderMediaMonitor(ISyncContext context_, V4ReaderAssembly rdr)
        {
            syncContext = context_;

            rw = rdr.handle;
            rwTyp = rdr.rwTyp;

            V4Reader_MediaCallbacks.Register(rw, MediaProduced, MediaRemoved);

            if (!InstallMifare(rdr.isamSlots))
                throw new Exception("Couldn't install Mifare");
            if (rdr.scenario1 != null && rdr.scenario1.Count > 0)
                if (Reader.ConfigureForPolling(rdr.rwTyp, rdr.handle, rdr.scenario1.ToArray(), Scenario.SCENARIO_1) != CSC_API_ERROR.ERR_NONE)
                    throw new Exception("Couldn't configure for polling scenario 1");
            var z = Reader.ConfigureForPolling(rdr.rwTyp, rdr.handle, rdr.scenario2.ToArray(), Scenario.SCENARIO_2);
            if (rdr.scenario2 != null && rdr.scenario2.Count > 0)
                if (Reader.ConfigureForPolling(rdr.rwTyp, rdr.handle, rdr.scenario2.ToArray(), Scenario.SCENARIO_2) != CSC_API_ERROR.ERR_NONE)
                {
                    throw new Exception("Couldn't configure for polling scenario 2");
                }

            var status = GetReaderStatus();
            if (status != CONSTANT.ST_INIT)
                throw new Exception("unexpected reader state found " + status.ToString());
        }

        public void MakeCardReady()
        {
            Reader.SwitchToCardOnState(rwTyp, rw);
        }

        private byte GetReaderStatus()
        {
            StatusCSC status = new StatusCSC();
            Reader.StatusCheck(rwTyp, rw, ref status);
            return status.ucStatCSC;
        }

        private bool InstallMifare(List<int> isamSlots)
        {
            {
                InstallCard pCscCardParams = new InstallCard();

                pCscCardParams.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
                pCscCardParams.iCardParam.xMifParam.sSize = 0;

                if (Reader.InstallCard(rwTyp,
                                         rw,
                                         DEST_TYPE.DEST_CARD,
                                         pCscCardParams) != CSC_API_ERROR.ERR_NONE)
                    return false;
            }
            foreach (int samSlot in isamSlots)
            {
                InstallCard pCscCardParams = new InstallCard();

                {
                    // TODO: likely not required. so, discard
                    pCscCardParams.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
                    pCscCardParams.iCardParam.xMifParam.sSize = 0;
                }

                string slot = "DMSAM=" + samSlot.ToString();

                pCscCardParams.iCardParam.xMifParam.acOptionString = slot;
                pCscCardParams.iCardParam.xMifParam.sSize = (short)slot.Length;

                // install virtual Desfile Card for CCHS SAM
                if (Reader.InstallCard(rwTyp,
                                 rw,
                                 DEST_TYPE.DEST_SAM_DESFIRE,
                                 pCscCardParams) != CSC_API_ERROR.ERR_NONE)
                    return false;
            }
            return true;
        }
        
        int rw;
        CSC_READER_TYPE rwTyp;
        
        DateTime _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = new DateTime(2000, 1, 1);

        #region Functions called back by V4 reader in its own thread
        void MediaProduced(StatusCSCEx status, DateTime msgReceptionTimestamp)
        {            
            if (msgReceptionTimestamp < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                Logging.Log(LogLevel.Information, "MediaProduced: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }
            
            syncContext.Message(()=> {
                RaiseMediaProduced(status);
            });            
        }

        void MediaRemoved(StatusCSCEx status, DateTime msgReceptionTimestamp)
        {            
            if (msgReceptionTimestamp < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                Logging.Log(LogLevel.Information, "MediaRemoved: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }
            
            syncContext.Message(() => {
                RaiseMediaRemoved(status);                
            });
        }
        #endregion
    }
}