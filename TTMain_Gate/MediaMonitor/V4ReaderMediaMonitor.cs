using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.Common;
using System.Runtime.InteropServices;
using Common;

namespace IFS2.Equipment.TicketingRules
{
    public class V4ReaderMediaMonitor : ReaderMediaMonitor
    {
        #region ReaderMediaMonitor
        public override void StartPolling(object obj)
        {
            int scenario = (int)obj;

            if (Reader.StartPolling(rwTyp, rw, (byte)scenario, (Utility.StatusListenerDelegate)StatusListenerMediaProduced) == CSC_API_ERROR.ERR_NONE)
                _readerStatus = CONSTANT.ST_POLLON;
            else
                GetReaderStatus();
        }

        public override void StopPolling()
        {
            base.StopPolling();

            _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
            
            if (_readerStatus == CONSTANT.ST_INIT)
                return;
            else
                if (Reader.StopField(rwTyp, rw) == CSC_API_ERROR.ERR_NONE)
                    _readerStatus = CONSTANT.ST_INIT;
                else
                    GetReaderStatus();            
        }
        
        public override void WaitForMediaRemoval()
        {
            if (Reader.SwitchToDetectRemovalState(rwTyp, rw, StatusListenerMediaRemoved) == CSC_API_ERROR.ERR_NONE)
                _readerStatus = CONSTANT.ST_DETECT_REMOVAL;
            else
                GetReaderStatus();
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
            _readerStatus = GetReaderStatus();
            if (_readerStatus != CONSTANT.ST_INIT)
                throw new Exception("unexpected reader state found " + _readerStatus.ToString());
        }

        public void MakeCardReady()
        {
            Reader.SwitchToCardOnState(rwTyp, rw);
        }

        private byte GetReaderStatus()
        {
            StatusCSC status = new StatusCSC();
            Reader.StatusCheck(rwTyp, rw, ref status);
            _readerStatus = status.ucStatCSC;
            return _readerStatus;
        }

        byte _readerStatus;

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

        // TODO: see how relevant it is.
        DateTime _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = new DateTime(2000, 1, 1);

        #region Functions called back by V4 reader in its own thread
        void StatusListenerMediaProduced(
            IntPtr code, IntPtr status
            )
        {
            _readerStatus = CONSTANT.ST_CARDON;
            DateTime msgReceptionTimestamp = DateTime.Now;
            if (msgReceptionTimestamp < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                Logging.Log(LogLevel.Information, "StatusListenerMediaProduced: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            
                syncContext.Message(()=> {
              
                        RaiseMediaProduced(new StatusCSCEx(rw, pStatusCSC));
                });

            // TODO: I think it is necessary, else we leak memory. But executing it is causing crash. So, commenting it FTTB
            //Marshal.FreeHGlobal(status);

            code = IntPtr.Zero;
            status = IntPtr.Zero;
        }

        void StatusListenerMediaRemoved(
            IntPtr code, IntPtr status
            )
        {
            _readerStatus = CONSTANT.ST_INIT;

            DateTime msgReceptionTimestamp = DateTime.Now;
            if (msgReceptionTimestamp < _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored)
            {
                Logging.Log(LogLevel.Information, "StatusListenerMediaRemoved: Ignoring message since _tsPriorToWhenMediaProducedHasToBeIgnored = " + _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored.ToString("yyyy-MM-dd hh:MM:ss.ffffzzz"));
                return;
            }

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            // memory occupied by status gets leaked, but we are helpless, as attempt to free it causes crash
            
                syncContext.Message(() => { 
             
                        RaiseMediaRemoved(new StatusCSCEx(rw, pStatusCSC)); 
                });
            
            code = IntPtr.Zero;
            status = IntPtr.Zero;
            // It causes problem. Anyway, it was redundant, and is correctly set inside MediaRemovedInt
            //_curStatus = ReaderStatus.ST_INIT;
        }
        #endregion
    }
}