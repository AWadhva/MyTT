using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.Common;
using System.Runtime.InteropServices;

namespace IFS2.Equipment.TicketingRules
{
    public class V4ReaderMediaMonitor : ReaderMediaMonitor
    {
        #region ReaderMediaMonitor
        public override void StartPolling(object obj, 
            Action<StatusCSC, DateTime> MediaProduced_
            )
        {
            MediaProduced = MediaProduced_;

            int scenario = (int)obj;

            if (Reader.StartPolling(rwTyp, rw, (byte)scenario, (Utility.StatusListenerDelegate)StatusListenerMediaProduced) == CSC_API_ERROR.ERR_NONE)
                _readerStatus = CONSTANT.ST_POLLON;
            else
                GetReaderStatus();
        }

        public override void StopPolling()
        {                
            _tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
            MediaRemoved = null;
            MediaProduced = null;            

            if (_readerStatus == CONSTANT.ST_INIT)
                return;
            else
                if (Reader.StopField(rwTyp, rw) == CSC_API_ERROR.ERR_NONE)
                    _readerStatus = CONSTANT.ST_INIT;
                else
                    GetReaderStatus();
        }
        
        public override void DoneReadWriteWithThisMedia(Action<StatusCSC, DateTime> MediaRemoved_)
        {
            MediaRemoved = MediaRemoved_;
            //var x = Reader.SwitchToDetectRemovalState(rwTyp, rw, StatusListenerMediaRemoved);
            if (Reader.SwitchToDetectRemovalState(rwTyp, rw, StatusListenerMediaRemoved) == CSC_API_ERROR.ERR_NONE)
                _readerStatus = CONSTANT.ST_DETECT_REMOVAL;
            else
                GetReaderStatus();
        }

        public override void SetIgnoreMediaList(List<int> media)
        {
            throw new NotImplementedException();
        }
        #endregion
        ISyncContext syncContext;

        public V4ReaderMediaMonitor(ISyncContext context_, int handle_, CSC_READER_TYPE rwTyp_, List<int> isamSlots, List<ScenarioPolling> scenario1, List<ScenarioPolling> scenario2)
        {
            syncContext = context_;

            rw = handle_;
            rwTyp = rwTyp_;

            if (!InstallMifare(isamSlots))
                throw new Exception("Couldn't install Mifare");
            if (scenario1 != null && scenario1.Count > 0)
                if (Reader.ConfigureForPolling(rwTyp, rw, scenario1.ToArray(), Scenario.SCENARIO_1) != CSC_API_ERROR.ERR_NONE)
                    throw new Exception("Couldn't configure for polling scenario 1");
            var z = Reader.ConfigureForPolling(rwTyp, rw, scenario2.ToArray(), Scenario.SCENARIO_2);
            if (scenario2 != null && scenario2.Count > 0)
                if (Reader.ConfigureForPolling(rwTyp, rw, scenario2.ToArray(), Scenario.SCENARIO_2) != CSC_API_ERROR.ERR_NONE)
                {
                    throw new Exception("Couldn't configure for polling scenario 2");
                }
            _readerStatus = GetReaderStatus();
            if (_readerStatus != CONSTANT.ST_INIT)
                throw new Exception("unexpected reader state found " + _readerStatus.ToString());
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
            if (MediaProduced != null)
                syncContext.Message(()=> {
                    if (MediaProduced != null)
                        MediaProduced(pStatusCSC, msgReceptionTimestamp);
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
            if (MediaRemoved != null)
                syncContext.Message(() => { 
                    if (MediaRemoved != null)
                        MediaRemoved(pStatusCSC, msgReceptionTimestamp); 
                });
            
            code = IntPtr.Zero;
            status = IntPtr.Zero;
            // It causes problem. Anyway, it was redundant, and is correctly set inside MediaRemovedInt
            //_curStatus = ReaderStatus.ST_INIT;
        }
        #endregion       
    }
}