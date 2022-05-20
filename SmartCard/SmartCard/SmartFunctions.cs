/*Prithvi : TODO Key Management in the function calls */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;

namespace IFS2.Equipment.TicketingRules
{
    public sealed class SmartFunctions
    {
        private CSC_READER_TYPE _ReaderType;
        private int _hRw;

        private byte _currentKeySet = 0;

        static readonly SmartFunctions _sFunc = new SmartFunctions();
        public bool _delhiCCHSSAMUsage = false;
        public bool _cryptoflexSAMUsage = true;
        public bool _IsNFCCardDetected = false;
        private bool _isSAMConfigured=false;

        static SmartFunctions()
        {
            pStatusCSC.ucAntenna = 0x00;
            pStatusCSC.ucATR = new byte[CONSTANT.MAX_ATR_SIZE];
            pStatusCSC.ucLgATR = 0x00;
            pStatusCSC.ucNbDetectedCard = 0x00;
            pStatusCSC.ucStatCSC = 0x00;
            pStatusCSC.xCardType = (int)CSC_TYPE.CARD_NONE;
        }

        public SmartFunctions()
        {
            _delhiCCHSSAMUsage = (bool)Configuration.ReadParameter("DelhiCCHSSAMUsage", "bool", "false");
            _cryptoflexSAMUsage = (bool)Configuration.ReadParameter("CryptoflexSAMUsage", "bool", "true");
        }

        public static SmartFunctions Instance
        {
            get
            {
                return _sFunc;
            }
        }

        /// <summary>
        /// Function to INIT the reader for CSC Reading
        /// Curretly supported DESFIRE Ev0, add CSC Supported types
        /// in the Params for Init.
        /// </summary>
        /// <returns></returns>
        public CSC_API_ERROR Init(bool bStartPolling)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_DEVICE;
#if _HHD_ && _BLUEBIRD_
            if (ConfigureForPolling())
            {
                if (StartPolling(1)) Err = CSC_API_ERROR.ERR_NONE;
            }
#else
            InstallCard pCscCardParams = new InstallCard();

            pCscCardParams.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            pCscCardParams.iCardParam.xMifParam.sSize = 0;


            Err = Reader.InstallCard(_ReaderType,
                                     _hRw,
                                     DEST_TYPE.DEST_CARD,
                                     pCscCardParams);


            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (_delhiCCHSSAMUsage)
                {
                    bool IsCCHSSAMInstalled = false;

                    foreach (cSAMConf smcnf in SharedData.mSAMUsed)
                    {
                        if (smcnf.mSAMType == CONSTANT.SAMType.ISAM)
                        {
                            IsCCHSSAMInstalled = true;
                            string slot = "DMSAM=" + smcnf.SAM_Slot.ToString();


                            pCscCardParams.iCardParam.xMifParam.acOptionString = slot;
                            pCscCardParams.iCardParam.xMifParam.sSize = (short)slot.Length;
                        }
                    }
                    if (IsCCHSSAMInstalled == true)
                    {
                        /// install virtual Desfile Card for CCHS SAM
                        Err = Reader.InstallCard(_ReaderType,
                                         _hRw,
                                         DEST_TYPE.DEST_SAM_DESFIRE,
                                         pCscCardParams);
                    }

                }

                ConfigureForPolling();

                if (bStartPolling)
                    StartPolling(listenerCardProduced, Scenario.SCENARIO_1);
                return Err;
            }
#endif
            return Err;
        }

        public Utility.StatusListenerDelegate listenerCardProduced = null, listenerCardRemoved = null;

        private bool ConfigureForPolling()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;
#if _HHD_ && _BLUEBIRD_
            Err= Reader.ConfigureForPolling();
#else
            var pScenarioPolling = new ScenarioPolling[1];

            pScenarioPolling[0].xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            pScenarioPolling[0].ucAntenna = (byte)(Configuration.ReadBoolParameter("UsePrimaryAntenna", true) ? CONSTANT.SMART_ANTENNA_1 : CONSTANT.SMART_ANTENNA_2);
            pScenarioPolling[0].ucRepeatNumber = 1;

            Err = Reader.ConfigureForPolling(_ReaderType, _hRw, pScenarioPolling, Scenario.SCENARIO_1);

            if (Err != CSC_API_ERROR.ERR_NONE)
                return false;
            bool b = (bool)Configuration.ReadParameter("TokenDispenseFunctionality", "bool", "false");
            if (!b || SharedData.EquipmentType == EquipmentFamily.TOM)
                return true;
            pScenarioPolling[0].xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            pScenarioPolling[0].ucAntenna = CONSTANT.SMART_ANTENNA_2;
            pScenarioPolling[0].ucRepeatNumber = 1;


            Err = Reader.ConfigureForPolling(_ReaderType, _hRw, pScenarioPolling, Scenario.SCENARIO_2);
#endif
            return (Err == CSC_API_ERROR.ERR_NONE);
        }

        //ScenarioPolling[] pScenarioPolling = null;
        /// <summary>
        /// Function to Start Polling for a Particular Type or Types of CSC
        /// Refer to cscApi documentation for supported types and build a 
        /// Scenario accordingly.
        /// 
        /// Currently : Mifare Scenario is active.
        /// </summary>
        /// <returns></returns>
        ///
#if _HHD_ && _BLUEBIRD_
        public Boolean StartPolling(int CardType)// ISO Type A or Type B
        {
            return Reader.StartPolling();
        }
        public Boolean ReStartPolling(int CardType)// ISO Type A or Type B
        {
            return Reader.ReStartPolling();
        }
#else
        public void StartPolling(Utility.StatusListenerDelegate listener, Scenario scenario)
        {
            StartPolling(scenario, listener);
        }

        Scenario _activeScenario = Scenario.SCENARIO_1;

        public Scenario GetActiveScenario()
        {
            return _activeScenario;
        }

        public void StartPolling(Scenario scenario, Utility.StatusListenerDelegate listener)
        {
            StartPolling(scenario, listener, _hRw);
            _activeScenario = scenario;
        }

        public void StartPolling(Scenario scenario, Utility.StatusListenerDelegate listener, int hRw)
        {
            CSC_API_ERROR Err = Reader.StartPolling(CSC_READER_TYPE.V4_READER,
                                       hRw, (byte)scenario, listener);
            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, hRw);
            _activeScenario = scenario;
        }

        public void StartPollingEx(Scenario scenario, Utility.StatusListenerDelegate listener, int hRw)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            Err = Reader.StartPolling(_ReaderType,
                                       hRw, (byte)scenario, listener
            );

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                _activeScenario = scenario;
                return;
            }
            else
                throw GetExceptionForCode(Err, hRw);
        }

        public void StartPollingEx(Scenario scenario, Utility.StatusListenerDelegate listener)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            Err = Reader.StartPolling(_ReaderType,
                                       _hRw, (byte)scenario, listener
            );

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                _activeScenario = scenario;
                return;
            }
            else
                throw GetExceptionForCode(Err, _hRw);
        }
#endif

        static StatusCSC pStatusCSC;
        public enum MediaDetected { CARD, TOKEN, UNSUPPORTEDMEDIA, NONE };

        public bool GetReaderStatus(out StatusCSC status)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;
            status = new StatusCSC();
#if _HHD_ && _BLUEBIRD_
#else
            Err = Reader.StatusCheck(_ReaderType, _hRw, ref status);
#endif
            if (Err != CSC_API_ERROR.ERR_NONE)
                return false;
            return true;
        }
        private void ClearStatusCSC()
        {
            SmartFunctions.pStatusCSC.ucAntenna = 0x00;
            for (int b = 0; b < pStatusCSC.ucATR.Length; b++) SmartFunctions.pStatusCSC.ucATR[b] = 0;
            pStatusCSC.ucLgATR = 0x00;
            pStatusCSC.ucNbDetectedCard = 0;
            pStatusCSC.ucStatCSC = 0;
            pStatusCSC.xCardType = (int)CSC_TYPE.CARD_NONE;
        }

        public int SmartSyncDetectOk()
        {
            StatusCSC statusCSC = new StatusCSC();
            var Err = Reader.StatusCheck(_ReaderType, _hRw, ref statusCSC);
            if (Err != CSC_API_ERROR.ERR_NONE)
                return -1;
            else
                return statusCSC.ucStatCSC;
        }

        /// <summary>
        /// Function to Synchronize the Card information with the detected
        /// Card
        /// </summary>
        /// <returns></returns>
        public void SmartSyncDetectOk(out MediaDetected detectionState,
            out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            bool bUseAsynch,
            Scenario scenario
            )
        {

            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            detectionState = MediaDetected.NONE;
            bSameMedia = false;

            try
            {
                ClearStatusCSC();
                Err = Reader.StatusCheck(_ReaderType, _hRw, ref SmartFunctions.pStatusCSC);

                if (Err != CSC_API_ERROR.ERR_NONE)
                    return;

                //we can check the status. To see if polling is activated.
                Logging.Log(LogLevel.Verbose, "SmartSyncDetectOk " + pStatusCSC.ucStatCSC.ToString("X2") + "|" + pStatusCSC.ucNbDetectedCard.ToString() + "|" + pStatusCSC.ucLgATR.ToString() + "|" + pStatusCSC.ucAntenna.ToString());
#if _HHD_ && _BLUEBIRD_
                switch (pStatusCSC.ucStatCSC)
                {
                    case CONSTANT.ST_CARDON:
                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                        if (detectionState == MediaDetected.NONE)
                        {
                            HaltCard();
                            StartPolling(1);
                        }
                        break;
                    case CONSTANT.ST_DETECT_REMOVAL:
                        {
                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                            bSameMedia = true;
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }
#else
                switch (pStatusCSC.ucStatCSC)
                {
                    case CONSTANT.ST_CARDON:
                        {
                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                            if (detectionState == MediaDetected.NONE)
                            {
                                HaltCard();
                                StartPolling(bUseAsynch ? listenerCardProduced : null, scenario);
                            }
                            break;
                        }
                    case CONSTANT.ST_INIT:
                        {
                            StartPolling(bUseAsynch ? listenerCardProduced : null, scenario);
                            {
                                Thread.Sleep(40);// TODO: See if it can be pushed to configuration. Also, put more optimum value for it.
                                Err = Reader.StatusCheck(_ReaderType, _hRw, ref pStatusCSC);
                                switch (pStatusCSC.ucStatCSC)
                                {
                                    case CONSTANT.ST_CARDON:
                                        {
                                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                                            if (detectionState == MediaDetected.NONE)
                                            {
                                                HaltCard();
                                                StartPolling(scenario, bUseAsynch ? listenerCardProduced : null);
                                            }
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    case CONSTANT.ST_POLLON:
                        break;
                    case CONSTANT.ST_DETECT_REMOVAL:
                        {
                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                            bSameMedia = true;
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }
#endif
            }
            catch
            {
                return;
            }
        }


        // it doesn't initiates polling on its own. rest it is identical to SmartSyncDetectOk
        public void SmartSyncDetectOkPassive(//out MediaDetected detectionState,
            //out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            out StatusCSC statusCSC
            )
        {
            statusCSC = new StatusCSC();
            //statusCSC = -1;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            //detectionState = MediaDetected.NONE;
            //bSameMedia = false;

            ClearStatusCSC();
            Err = Reader.StatusCheck(_ReaderType, _hRw, ref statusCSC);

            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, _hRw);
        }

        // it doesn't initiates polling on its own. rest it is identical to SmartSyncDetectOk
        public void SmartSyncDetectOkPassive(
            Scenario scenario,
            out MediaDetected detectionState,
            out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            out StatusCSC statusCSC
            )
        {
            statusCSC = new StatusCSC();
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            detectionState = MediaDetected.NONE;
            bSameMedia = false;

            ClearStatusCSC();
            Err = Reader.StatusCheck(_ReaderType, _hRw, ref SmartFunctions.pStatusCSC);

            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, _hRw);

            //we can check the status. To see if polling is activated.
            //Logging.Log(LogLevel.Verbose, "SmartSyncDetectOk " + pStatusCSC.ucStatCSC.ToString("X2") + "|" + pStatusCSC.ucNbDetectedCard.ToString() + "|" + pStatusCSC.ucLgATR.ToString() + "|" + pStatusCSC.ucAntenna.ToString());

#if _HHD_ && _BLUEBIRD_
            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
#else
            switch (pStatusCSC.ucStatCSC)
            {
                case CONSTANT.ST_CARDON:
                    {
                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                        if (detectionState == MediaDetected.NONE)
                        {
                            Debug.Assert(false);
                            HaltCard();
                            StartPollingEx(scenario, listenerCardProduced);
                        }
                        break;
                    }
                case CONSTANT.ST_DETECT_REMOVAL:
                    {
                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                        bSameMedia = true;

                        break;
                    }
                default:
                    {
                        break; ;
                    }
            }
            statusCSC = new StatusCSC(pStatusCSC);
#endif
        }

        /// <summary>
        /// Function to Synchronize the Card information with the detected
        /// Card
        /// would let the ReaderException flow outside.
        /// Only practical exception that can be thrown is ErrorDevice
        /// </summary>
        /// <returns></returns>
        public void SmartSyncDetectOk(out MediaDetected detectionState,
            out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            out int statusCSC,
            int hRw, CSC_READER_TYPE ReaderType, Scenario scenario
            )
        {
            statusCSC = -1;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            detectionState = MediaDetected.NONE;
            bSameMedia = false;

            ClearStatusCSC();
            Err = Reader.StatusCheck(ReaderType, hRw, ref SmartFunctions.pStatusCSC); // Can interpret from doc, it (sSmartStatusEx) is not going to raise ERR_TIMEOUT

            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, hRw);

            //we can check the status. To see if polling is activated.
            Logging.Log(LogLevel.Verbose, "SmartSyncDetectOk " + pStatusCSC.ucStatCSC.ToString("X2") + "|" + pStatusCSC.ucNbDetectedCard.ToString() + "|" + pStatusCSC.ucLgATR.ToString() + "|" + pStatusCSC.ucAntenna.ToString());

            statusCSC = pStatusCSC.ucStatCSC;
#if _HHD_ && _BLUEBIRD_
            //TODO: Needed to make state machine for card detection compatible with old TT
            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
#else
            switch (pStatusCSC.ucStatCSC)
            {
                case CONSTANT.ST_CARDON:
                    {
                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                        statusCSC = CONSTANT.ST_CARDON;
                        if (detectionState == MediaDetected.NONE)
                        {
                            Debug.Assert(false);
                            HaltCard();
                            StartPollingEx(scenario, listenerCardProduced, hRw);
                        }
                        break;
                    }
                case CONSTANT.ST_INIT:
                    {
                        StartPollingEx(scenario, listenerCardProduced, hRw);
                        {
                            // TODO: May be we want to put a little sleep before going for StatusCheck
                            Err = Reader.StatusCheck(ReaderType, hRw, ref pStatusCSC);
                            if (Err != CSC_API_ERROR.ERR_NONE)
                                throw GetExceptionForCode(Err, hRw);
                            switch (pStatusCSC.ucStatCSC)
                            {
                                case CONSTANT.ST_CARDON:
                                    {
                                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                                        statusCSC = CONSTANT.ST_CARDON;
                                        if (detectionState == MediaDetected.NONE)
                                        {
                                            HaltCard();
                                            StartPollingEx(scenario, listenerCardProduced);
                                            statusCSC = CONSTANT.ST_POLLON;
                                        }
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                case CONSTANT.ST_POLLON:

                    break;
                case CONSTANT.ST_DETECT_REMOVAL:
                    {
                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                        bSameMedia = true;

                        break;
                    }
                default:
                    {
                        return;
                    }
            }
#endif
        }

        public void DetectTypeOfMediaNExtractSerialNumbers(ref StatusCSC pStatusCSC, out MediaDetected detectionState)
        {
            detectionState = MediaDetected.NONE;
            _IsNFCCardDetected = false;
#if _BLUEBIRD_
            if (pStatusCSC.xCardType == (int)CSC_TYPE.CARD_MIFARE1)
            {
                byte[] _serialNbrBytes = new byte[8];
                byte[] ba = pStatusCSC.ucATR;
                byte typ = ba[1];              
                if (typ == 0)
                {
                    // ultralight
                    detectionState = MediaDetected.TOKEN;
                    Array.Copy(ba, 2, _serialNbrBytes, 0, 7);
                }
                else if (typ == 0x03)
                {
                    // desfilre
                    detectionState = MediaDetected.CARD;
                    Array.Copy(ba, 2, _serialNbrBytes, 0, 7);
                }
                else
                {
                    // some other card of MiFare family, other than Ultralight and DESFire
                }

                _SrNbr = 0;
                for (int i = 0; i < 7; i++)
                {
                    _SrNbr *= 256;
                    _SrNbr += _serialNbrBytes[i];
                }
                return;
            }
#else

            if (pStatusCSC.xCardType == (int)CSC_TYPE.CARD_MIFARE1 && pStatusCSC.ucLgATR == 12)
            {
                ExtractFrom_StatusCSC(ref pStatusCSC, out detectionState, out _SrNbr);

                return;
            }
#endif
        }

        public void ExtractFrom_StatusCSC(ref StatusCSC pStatusCSC, out MediaDetected detectionState, out long SrNbr)
        {
            byte[] serialNbrBytes = new byte[8];
            byte[] ba = pStatusCSC.ucATR;
            _IsNFCCardDetected = false;
            detectionState = MediaDetected.NONE;
            //Logging.Log(LogLevel.Verbose, "ba.Length = " + ba.Length);
            byte SAK = ba[2];
            var typ = ((SAK >> 3) & 0x7);
            if (typ == 0)
            {
                // ultralight
                detectionState = MediaDetected.TOKEN;
                Array.Copy(ba, 3, serialNbrBytes, 0, 7);
            }
            else if (typ == 4)
            {
                // desfilre
                detectionState = MediaDetected.CARD;
                Array.Copy(ba, 3, serialNbrBytes, 0, 7);
            }
            else if (typ == 5) // NFC Desfire is detected....
            {
                if ((bool)(Configuration.ReadParameter("NFCFunctionality", "bool", "false")))
                {
                    _IsNFCCardDetected = true;
                    if (ba[3] == 0x40)// Gemalto NFC Desfire Sim card is detected...
                    {
                    }
                    detectionState = MediaDetected.CARD;
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
            }
            else
            {
                // some other card of MiFare family, other than Ultralight and DESFire
                detectionState = MediaDetected.UNSUPPORTEDMEDIA;
            }

            SrNbr = 0;
            for (int i = 0; i < 7; i++)
            {
                SrNbr *= 256;
                SrNbr += serialNbrBytes[i];
            }
        }

        public bool SwitchToDetectRemovalState(int hRw)
        {
#if !_HHD_ && !_BLUEBIRD_
            return (Reader.SwitchToDetectRemovalState(CSC_READER_TYPE.V4_READER, hRw, listenerCardRemoved) == CSC_API_ERROR.ERR_NONE);
#else
            throw new NotImplementedException();
#endif
        }

        public bool SwitchToDetectRemovalState()
        {
#if _HHD_ && _BLUEBIRD_
           return Reader.SwitchToDetectionRemoval();
            
#else
            return (Reader.SwitchToDetectRemovalState(_ReaderType, _hRw, listenerCardRemoved) == CSC_API_ERROR.ERR_NONE);
#endif
        }

        public void SwitchToDetectRemovalStateEx()
        {
#if _HHD_ && _BLUEBIRD_
            
#else
            CSC_API_ERROR error = Reader.SwitchToDetectRemovalState(_ReaderType, _hRw, listenerCardRemoved);
            if (error != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(error, _hRw);
#endif
        }

        public CSC_API_ERROR SwitchToCardOnState(int hRw)
        {
#if _HHD_ && _BLUEBIRD_
            throw new NotImplementedException();
#else
            return Reader.SwitchToCardOnState(CSC_READER_TYPE.V4_READER, hRw);
#endif
        }

        public CSC_API_ERROR SwitchToCardOnState()
        {
#if _HHD_ && _BLUEBIRD_
            return CSC_API_ERROR.ERR_NONE;
#else
            // when in ST_INIT, it returns ERR_NOEXEC
            return Reader.SwitchToCardOnState(_ReaderType, _hRw);
#endif
        }

        public void StopPolling()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;
#if _HHD_ && _BLUEBIRD_
            Err = (CSC_API_ERROR)Reader.StopPolling();
#else
            // If Reader is in ST_INIT, even then it returns ERR_NONE
            // If Reader is in ST_CARDON, even then it returns ERR_NONE
            // Even when field is OFF, it returns ERR_NONE
            Err = Reader.StopPolling(_ReaderType,
                                        _hRw);
#endif
            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, _hRw);
        }

        private long _SrNbr = 0;
        public void SetSerialNbr(long srnbr)
        {
            _SrNbr = srnbr;
        }

        public long ReadSNbr()
        {
            return _SrNbr;
        }

        public CSC_API_ERROR HaltCard(int hRw)
        {
#if !_BLUEBIRD_
            return Reader.HaltCard(CSC_READER_TYPE.V4_READER, hRw);
#else
            throw new NotImplementedException();
#endif
        }

        public CSC_API_ERROR HaltCard()
        {
#if _BLUEBIRD_
            return Reader.HaltCard();
#else
            return Reader.HaltCard(_ReaderType, _hRw);
#endif
        }
       
        public CSC_API_ERROR ReadPurseFile(out Int32 pValue, out byte pSw1, out byte pSw2)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            pValue = 0;

            byte pKeyRef, pKeyNum;
#if _HHD_ && _BLUEBIRD_
             if (_delhiCCHSSAMUsage)
                Err_Glo = Reader.ReadDM1PurseFile(out pValue, out pSw1, out pSw2);

#else
            if (_IsNFCCardDetected)
            {
                byte[] fileid = {0x02 }; 
               // bool ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadPurseFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x01, 0x02, 0x03, 0x01, out pSw1, out pSw2);
                if (ret)
                {

                    Err_Glo = Reader.IsoCommand(_ReaderType,
                                                _hRw,
                                                DEST_TYPE.DEST_PICC_TRANSPARENT,
                                                 CFunctions.getApdu(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_GETVAL_INS, 0x00, 0x00, fileid, 0x00),
                                                  3,
                                                out pSw1,
                                                out pSw2,
                                                out pResData);

                    if (Err_Glo == CSC_API_ERROR.ERR_NONE && pSw1 == 0x91 && pSw2 == 0x00)
                        pValue = (int)CFunctions.ConvertLittleEndian(pResData);
                }
                pSw1--;
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
//#if _HHD_ && _BLUEBIRD_
//                Err_Glo = Reader.ReadDM1PurseFile(out pValue, out pSw1, out pSw2);

//#else

                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x22, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                        mDesfireCardlayout.keyReferenceBytes.krbAid,
                        mDesfireCardlayout.keyReferenceBytes.krbRFU,
                        mDesfireCardlayout.CurrentKeyCardNumber,
                        mDesfireCardlayout.CurrentCommunicationByte };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_GETV_INS, 0x40, 0x22, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                        if (Err_Glo == CSC_API_ERROR.ERR_NONE)
                            pValue = (int)CFunctions.ConvertLittleEndian(pResData);
                    }

//#endif
                }
                if (_cryptoflexSAMUsage)
                {
//#if _HHD_ && _BLUEBIRD_

//#else
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x22, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                _hRw,
                                                DEST_TYPE.DEST_CARD,
                                                CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_GETV_INS, CONSTANT.NULL, 0x22, pDataIn),
                                                out pSw1,
                                                out pSw2,
                                                out pResData);

                        pValue = (int)CFunctions.ConvertLittleEndian(pResData);
                    }

//#endif
                }
            }//if (_IsNFCCardDetected)
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadSequenceNbrFile(out long pValue, out byte pSw1, out byte pSw2)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            pValue = 0;

#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                int pval = 0;
                Err_Glo = Reader.ReadDM1SequenceFile(out pval, out pSw1, out pSw2);
                pValue = pval;
            }
#else
            byte pKeyRef, pKeyNum;
            if (_IsNFCCardDetected)
            {
                Logging.Log(LogLevel.Verbose, "ReadSeqFile");
                byte[] fileid = { 0x01 }; 
                //bool ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                bool ret;
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x01, 0x01, 0x03, 0x01, out pSw1, out pSw2);
                if (ret)
                {

                    Err_Glo = Reader.IsoCommand(_ReaderType,
                                                _hRw,
                                                DEST_TYPE.DEST_PICC_TRANSPARENT,
                                                 CFunctions.getApdu(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_GETVAL_INS, 0x00, 0x00, fileid, 0x00),
                                                  3,
                                                out pSw1,
                                                out pSw2,
                                                out pResData);

                    if (Err_Glo == CSC_API_ERROR.ERR_NONE && pSw1 == 0x91 && pSw2 == 0x00)
                        pValue = (int)CFunctions.ConvertLittleEndian(pResData);
                }
                pSw1--;
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x21, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_GETV_INS, 0x40, 0x21, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);
                        if (Err_Glo == CSC_API_ERROR.ERR_NONE)
                            pValue = (long)CFunctions.ConvertLittleEndian(pResData) + 1; //SKS: 14-11-2014 As TOM is showing sequence No -1//

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x21, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                _hRw,
                                                DEST_TYPE.DEST_CARD,
                                                CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_GETV_INS, CONSTANT.NULL, 0x21, pDataIn),
                                                out pSw1,
                                                out pSw2,
                                                out pResData);

                        pValue = (long)CFunctions.ConvertLittleEndian(pResData) + 1;//SKS: 14-11-2014 As TOM is showing sequence No -1//CFunctions.ConvertLittleEndian(pResData);
                    }
                }
            }//if (_IsNFCCardDetected)
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadHistoryFile(int pNbrOfRecords, out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;
            bool bRet = false;
            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            if (pNbrOfRecords < CONSTANT.MIFARE_HISTORY_RECORDS)
            {
#if _BLUEBIRD_
                if (_delhiCCHSSAMUsage)
                {
                    Err_Glo = Reader.ReadDM1HistoryFile(pNbrOfRecords, 0, 32, out pSw1, out pSw2,out pResData);
                }
#else
                if (_IsNFCCardDetected)
                {
                   // bRet = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                   
                    if (_IsSoftSAM)
                        bRet = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                    else
                        bRet = Authenticate(0x01, 0x03, 0x03, 0x01, out pSw1, out pSw2);
                    if (bRet)
                    {
                        bRet = ReadRecords(0x03, 0, 32, pNbrOfRecords, out pResData, out pSw1, out pSw2);
                    }
                  if (bRet) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
                else
                {
                    if (_delhiCCHSSAMUsage)
                    {

                        sDesfireCardLayout mDesfireCardlayout;
                        if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x43, EAccessPermission.E_AccessPermission_ReadWrite, true))
                        {
                            byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,(byte)pNbrOfRecords};

                            Err_Glo = Reader.IsoCommand(_ReaderType,
                                                     _hRw,
                                                     DEST_TYPE.DEST_CARD,
                                                     CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x43, pDataIn),
                                                     3,
                                                     out pSw1,
                                                     out pSw2,
                                                     out pResData);

                        }

                    }
                    if (_cryptoflexSAMUsage)
                    {
                        byte pKeyRef, pKeyNum;

                        if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x43, _currentKeySet, out pKeyRef, out pKeyNum))
                        {
                            byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, (byte)pNbrOfRecords };

                            Err_Glo = Reader.IsoCommand(_ReaderType,
                                            _hRw,
                                            DEST_TYPE.DEST_CARD,
                                            CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x43, pDataIn),
                                            out pSw1,
                                            out pSw2,
                                            out pResData);
                        }
                    }
                }//if (_IsNFCCardDetected)
               // return Err_Glo;
#endif
            }

            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }
        private bool _IsSoftSAM = false;
        public CSC_API_ERROR ReadValidationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM1ValidationFile(out pSw1, out pSw2, out pResData);
            }
#else
            byte pKeyRef, pKeyNum;

            if (_IsNFCCardDetected )
            {                
                bool ret ;
                Logging.Log(LogLevel.Verbose, "ReadValidationFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else               
                   ret = Authenticate(0x01, 0x05, 0x03, 0x01, out pSw1, out pSw2);               
                if (ret)
                {
                    ret = ReadDataFile(0x05, 0, 32,out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x15, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20}; // TODO : in TOM nbyte is 0x1B 

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x15, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x15, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                            _hRw,
                                            DEST_TYPE.DEST_CARD,
                                            CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x15, pDataIn),
                                            3,
                                            out pSw1,
                                            out pSw2,
                                            out pResData);
                    }
                }
            }//
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadSaleFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;
#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM1SaleFile(out pSw1, out pSw2, out pResData);
            }
#else
            byte pKeyRef, pKeyNum;
            if (_IsNFCCardDetected )
            {
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadSaleFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else              
                    ret = Authenticate(0x01, 0x06, 0x03, 0x01, out pSw1, out pSw2);
                
                if (ret)
                {
                    ret = ReadDataFile(0x06, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {
                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x16, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20};
                        // byte[] idata = new byte[] { 0x82, 0xB2, 0x40, 0x16, 0x08, 0x81, 0x01, 0x00, 0x06, 0x00, 0x00, 0x00, 0x06 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x16, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);
                        //                    System.Windows.Forms.MessageBox.Show("Err = " + Err.ToString(), "Sales file reading");

                    }

                }
                if (_cryptoflexSAMUsage)
                {

                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x16, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                        _hRw,
                                        DEST_TYPE.DEST_CARD,
                                        CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x16, pDataIn),
                                        out pSw1,
                                        out pSw2,
                                        out pResData);
                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadPurseLinkageFileDelhiDesfire(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;
#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM1PurseLinkage(0, 32, out pSw1, out pSw2, out pResData);
            }
#else
            byte pKeyRef, pKeyNum;

            if (_IsNFCCardDetected )
            {
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadPurseLinkageFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else                
                    ret = Authenticate(0x01, 0x00, 0x03, 0x01, out pSw1, out pSw2);
                
                if (ret)
                {
                    ret = ReadDataFile(0x00, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {
                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x10, EAccessPermission.E_AccessPermission_Read, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,12};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                 _hRw,
                                 DEST_TYPE.DEST_CARD,
                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x10, pDataIn),
                                 3,
                                 out pSw1,
                                 out pSw2,
                                 out pResData);
                    }
                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x10, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                            _hRw,
                                            DEST_TYPE.DEST_CARD,
                                            CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x10, pDataIn),
                                            out pSw1,
                                            out pSw2,
                                            out pResData);
                    }
                }
            }//if (_IsNFCCardDetected)
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadPendingFareDeductionFileDM2_DelhiDesfire(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM2PendingFareDeductionFile(out pSw1, out pSw2, out pResData);
            }
#else
            byte pKeyRef, pKeyNum;
            if (_IsNFCCardDetected )
            {
                //bool ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadDM2_PendingFareFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x02, 0x00, 0x01, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x00, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {
                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM2_AREA_CODE, 0x10, EAccessPermission.E_AccessPermission_Read, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,15};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                 _hRw,
                                 DEST_TYPE.DEST_CARD,
                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x10, pDataIn),
                                 3,
                                 out pSw1,
                                 out pSw2,
                                 out pResData);
                    }
                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM2_AREA_CODE, 0x10, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                            _hRw,
                                            DEST_TYPE.DEST_CARD,
                                            CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x10, pDataIn),
                                            out pSw1,
                                            out pSw2,
                                            out pResData);
                    }
                }
            }

#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadPersonalizationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM1PersonalizationFile(out pSw1, out pSw2, out pResData);
            }
#else
            byte pKeyRef, pKeyNum;
            if (_IsNFCCardDetected )
            {
                //bool ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadDM1_PersonalizationFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x01, 0x08, 0x03, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x08, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x08, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20};// in TOm it is 0x14

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x08, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }
                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x08, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                            _hRw,
                                            DEST_TYPE.DEST_CARD,
                                            CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x08, pDataIn),
                                            out pSw1,
                                            out pSw2,
                                            out pResData);
                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadCardHolderFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;
#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM1CardHolderFile(out pSw1, out pSw2, out pResData);
            }
#else
            byte pKeyRef, pKeyNum;
            if (_IsNFCCardDetected )
            {
                //bool ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadCardHolderFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x01, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x01, 0x09, 0x03, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x09, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x09, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20}; //in TOM it nbyte is 0x1F

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x09, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x09, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                            _hRw,
                                            DEST_TYPE.DEST_CARD,
                                            CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x09, pDataIn),
                                            out pSw1,
                                            out pSw2,
                                            out pResData);
                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadMetroSaleFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM2SaleFile(out pSw1, out pSw2, out pResData);
            }
#else
            if (_IsNFCCardDetected )
            {
                //bool ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadDM2_SaleFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x02, 0x01, 0x01, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x01, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                byte pKeyRef, pKeyNum;
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM2_AREA_CODE, 0x11, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x11, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM2_AREA_CODE, 0x11, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                               _hRw,
                                               DEST_TYPE.DEST_CARD,
                                               CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x11, pDataIn),
                                               out pSw1,
                                               out pSw2,
                                               out pResData);
                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;

        }

        public CSC_API_ERROR ReadMetroValidationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;


#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM2ValidationFile(out pSw1, out pSw2, out pResData);
            }
#else
            if (_IsNFCCardDetected )
            {
               // bool ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadDM2_ValidationFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x02, 0x02, 0x01, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x02, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                byte pKeyRef, pKeyNum;
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM2_AREA_CODE, 0x12, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x12, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM2_AREA_CODE, 0x12, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                               _hRw,
                                               DEST_TYPE.DEST_CARD,
                                               CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x12, pDataIn),
                                               out pSw1,
                                               out pSw2,
                                               out pResData);

                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;

        }

        public CSC_API_ERROR ReadMetroAddValueFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;


#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM2AddValueFile(out pSw1, out pSw2, out pResData);
            }
#else
            if (_IsNFCCardDetected )
            {
               // bool ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadDM2_AddValFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x02, 0x03, 0x01, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x03, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                byte pKeyRef, pKeyNum;
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM2_AREA_CODE, 0x13, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x13, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM2_AREA_CODE, 0x13, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                               _hRw,
                                               DEST_TYPE.DEST_CARD,
                                               CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x13, pDataIn),
                                               out pSw1,
                                               out pSw2,
                                               out pResData);
                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR ReadMetroPersonalization(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

#if _BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                Err_Glo = Reader.ReadDM2AgentPersonalizationFile(out pSw1, out pSw2, out pResData);
            }
#else
            if (_IsNFCCardDetected )
            {
                //bool ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                bool ret;
                Logging.Log(LogLevel.Verbose, "ReadDM2_PersonalizationFile");
                if (_IsSoftSAM)
                    ret = Authenticate(0x02, 0x01, out pSw1, out pSw2);
                else
                    ret = Authenticate(0x02, 0x08, 0x01, 0x01, out pSw1, out pSw2);
                if (ret)
                {
                    ret = ReadDataFile(0x08, 0, 32, out pResData, out pSw1, out pSw2);
                    if (ret) Err_Glo = CSC_API_ERROR.ERR_NONE;
                }
            }
            else
            {
                byte pKeyRef, pKeyNum;
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM2_AREA_CODE, 0x08, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                0x00,0x00,0x20};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, 0x40, 0x08, pDataIn),
                                                 3,
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    if (SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM2_AREA_CODE, 0x08, _currentKeySet, out pKeyRef, out pKeyNum))
                    {
                        byte[] pDataIn = new byte[] { pKeyRef, pKeyNum, 0x00, 0x00, 0x00, 0x20 };

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                              _hRw,
                                              DEST_TYPE.DEST_CARD,
                                              CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, 0x08, pDataIn),
                                              3,
                                              out pSw1,
                                              out pSw2,
                                              out pResData);
                    }
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        CSC_API_ERROR Err_Glo = CSC_API_ERROR.ERR_NOEXEC;
        byte _pSw1 = 0xFF;
        byte _pSw2 = 0xFF;

        /// <summary>
        /// Method to Credit/Debit Tpurse file
        /// </summary>
        /// <param name="pSw1"></param>
        /// <param name="pSw2"></param>
        /// <param name="pValue"></param>
        /// <param name="IsForCredit"></param>
        /// <returns></returns>
        public CSC_API_ERROR WritePurseFile(out byte pSw1, out byte pSw2, Int32 pValue)
        {
            bool IsForCredit = (pValue > 0);
            pValue = Math.Abs(pValue);
            pSw1 = pSw2 = Byte.MaxValue;

            if (pValue == 0)
                return CSC_API_ERROR.ERR_NONE;

            Logging.Log(LogLevel.Information, "WritePurseFile with amount = " + pValue.ToString());
            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

            byte[] blockData = CFunctions.ConvertToBytesLE(pValue);
#if _BLUEBIRD_
            if (IsForCredit == true)
            {
                Err_Glo = Reader.CreditDM1PurseFile(pValue, out pSw1, out pSw2);
            }
            else //for debit
            {
                Err_Glo = Reader.DebitDM1PurseFile(pValue, out pSw1, out pSw2);
            }
#else
            if (_IsNFCCardDetected )
            {
              //  Logging.Log(LogLevel.Error, "WritePurseFile Function Not implemented for NFC cards " );
                //Err_Glo = CSC_API_ERROR.ERR_NOT_AVAIL;
                WriteValueFileEx(0x02, 0x03, CONSTANT.DM1_AREA_CODE, out pSw1, out pSw2, pValue, IsForCredit);
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x22, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                        mDesfireCardlayout.keyReferenceBytes.krbAid,
                        mDesfireCardlayout.keyReferenceBytes.krbRFU,
                        mDesfireCardlayout.CurrentKeyCardNumber,
                        mDesfireCardlayout.CurrentCommunicationByte,
                        blockData[0],blockData[1],blockData[2],blockData[3]};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, IsForCredit ? CONSTANT.MIFARE_ADDV_INS : CONSTANT.MIFARE_DEBV_INS, 0x40, 0x22, pDataIn),
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {

                    byte[] pDataIn = new byte[7];

                    byte pKeyRef, pKeyNum;

                    SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x22, _currentKeySet, out pKeyRef, out pKeyNum);

                    pDataIn[0] = pKeyRef;
                    pDataIn[1] = pKeyNum;
                    pDataIn[2] = 0x00;

                    Array.Copy(blockData, 0, pDataIn, 3, blockData.Length);

                    Err_Glo = Reader.IsoCommand(_ReaderType,
                                          _hRw,
                                          DEST_TYPE.DEST_CARD,
                                          CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA,
                                          IsForCredit ? CONSTANT.MIFARE_ADDV_INS : CONSTANT.MIFARE_DEBV_INS,
                                          CONSTANT.NULL, 0x22, pDataIn),
                                          out pSw1,
                                          out pSw2,
                                          out pResData);
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;

        }

        public CSC_API_ERROR WriteSequenceNbrFile(out byte pSw1, out byte pSw2, Int32 pValue)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;
            pSw1 = 0xff;
            pSw2 = 0xff;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

            byte[] blockData = CFunctions.ConvertToBytesLE(pValue);
#if _BLUEBIRD_
            Err_Glo = Reader.DebitDM1SequenceFile(pValue, out pSw1, out pSw2);
#else
            if (_IsNFCCardDetected )
            {
                Logging.Log(LogLevel.Verbose, "WriteSequenceNbrFile ");
               // Err_Glo = CSC_API_ERROR.ERR_NOT_AVAIL;
                WriteValueFileEx(0x01, 0x02, CONSTANT.DM1_AREA_CODE, out pSw1, out pSw2, pValue, false);
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, CONSTANT.DM1_AREA_CODE, 0x21, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        byte[] pDataIn = new byte[] { mDesfireCardlayout.keyReferenceBytes.krbPCD,
                mDesfireCardlayout.keyReferenceBytes.krbAid,
                mDesfireCardlayout.keyReferenceBytes.krbRFU,
                mDesfireCardlayout.CurrentKeyCardNumber,
                mDesfireCardlayout.CurrentCommunicationByte,
                blockData[0],blockData[1],blockData[2],blockData[3]};

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_DEBV_INS, 0x40, 0x21, pDataIn),
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    byte[] pDataIn = new byte[7];

                    byte pKeyRef, pKeyNum;

                    SecurityMgr.Instance.GetPCDKeyRef(CONSTANT.DM1_AREA_CODE, 0x21, _currentKeySet, out pKeyRef, out pKeyNum);

                    pDataIn[0] = pKeyRef;
                    pDataIn[1] = pKeyNum;
                    pDataIn[2] = 0x00;

                    Array.Copy(blockData, 0, pDataIn, 3, blockData.Length);

                    Err_Glo = Reader.IsoCommand(_ReaderType,
                                          _hRw,
                                          DEST_TYPE.DEST_CARD,
                                          CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_DEBV_INS, CONSTANT.NULL, 0x21, pDataIn),
                                          out pSw1,
                                          out pSw2,
                                          out pResData);
                }
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public bool WriteDataFile(int pAid, int pFileNbr, byte[] pDataBuffer)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

            int numBytes = pDataBuffer.Length;
#if _BLUEBIRD_
              sDesfireCardLayout mDesfireCardlayout;
              if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, (byte)pAid, (byte)pFileNbr, EAccessPermission.E_AccessPermission_ReadWrite, true))
              {
                  Err_Glo = Reader.WriteDataFile((byte)pAid, (byte)mDesfireCardlayout.fileId_MifareDf, mDesfireCardlayout.arSetting.kcnReadWrite, 0x00, 0, pDataBuffer);
              }
              if (Err_Glo == CSC_API_ERROR.ERR_NONE) return true;
#else
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            if (_IsNFCCardDetected )
            {
                byte fileId =(byte)( pFileNbr & 0x0F);
                //Logging.Log(LogLevel.Error, "Write data Function Not implemented for NFC cards ");
                //Err_Glo = CSC_API_ERROR.ERR_NOT_AVAIL;
                WriteDataFileEx((byte)pAid, fileId,false, 0, pDataBuffer.Length, pDataBuffer, out pSw1, out pSw2);
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    byte[] pDataIn = new byte[numBytes + 8];
                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, (byte)pAid, (byte)pFileNbr, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        pDataIn[0] = mDesfireCardlayout.keyReferenceBytes.krbPCD;
                        pDataIn[1] = mDesfireCardlayout.keyReferenceBytes.krbAid;
                        pDataIn[2] = mDesfireCardlayout.keyReferenceBytes.krbRFU;
                        pDataIn[3] = mDesfireCardlayout.CurrentKeyCardNumber;
                        pDataIn[4] = mDesfireCardlayout.CurrentCommunicationByte;
                        //blockData[0],blockData[1],blockData[2],blockData[3]};
                        pDataIn[5] = 0x00;
                        pDataIn[6] = 0x00;
                        pDataIn[7] = (byte)numBytes;
                        Array.Copy(pDataBuffer, 0, pDataIn, 8, pDataBuffer.Length);

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_WRIT_INS, 0x40, (byte)pFileNbr, pDataIn),
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                    }

                }
                if (_cryptoflexSAMUsage)
                {
                    byte[] pDataIn = new byte[numBytes + 6];

                    byte pKeyRef, pKeyNum;

                    SecurityMgr.Instance.GetPCDKeyRef((byte)pAid, (byte)pFileNbr, _currentKeySet, out pKeyRef, out pKeyNum);

                    pDataIn[0] = pKeyRef;
                    pDataIn[1] = pKeyNum;
                    pDataIn[2] = 0x00;
                    pDataIn[3] = 0x00;
                    pDataIn[4] = 0x00;
                    pDataIn[5] = (byte)numBytes;

                    Array.Copy(pDataBuffer, 0, pDataIn, 6, pDataBuffer.Length);

                    Err_Glo = Reader.IsoCommand(_ReaderType,
                                           _hRw,
                                           DEST_TYPE.DEST_CARD,
                                           CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_WRIT_INS, CONSTANT.NULL, (byte)pFileNbr, pDataIn),
                                           out pSw1,
                                           out pSw2,
                                           out pResData);
                }
            }
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            if (Err_Glo == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS && pSw2 == 0)
                return true;
#endif
            return false;

        }

        public bool WriteRecordFile(int pAid, int pFileNbr, Int16 pOffset, byte[] pDataBuffer)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

            int numBytes = pDataBuffer.Length;
            byte[] Offset = CFunctions.ConvertToBytesLE(pOffset);
#if _BLUEBIRD_
              sDesfireCardLayout mDesfireCardlayout;
              if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, (byte)pAid, (byte)pFileNbr, EAccessPermission.E_AccessPermission_ReadWrite, true))
              {
                 // mDesfireCardlayout.arSetting.kcnReadWrite
                  Err_Glo = Reader.WriteRecordsFile((byte)pAid, (byte)mDesfireCardlayout.fileId_MifareDf, (byte)mDesfireCardlayout.arSetting.kcnReadWrite, 0x00, pOffset, pDataBuffer);
              }
              if (Err_Glo == CSC_API_ERROR.ERR_NONE) return true;
              
#else
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            if (_IsNFCCardDetected )
            {
               // Logging.Log(LogLevel.Error, "Write record Function Not implemented for NFC cards ");
              
                byte fileId = (byte)(pFileNbr & 0x0F);
                WriteDataFileEx((byte)pAid, fileId, true, pOffset, pDataBuffer.Length, pDataBuffer, out pSw1, out pSw2);
            }
            else
            {
                if (_delhiCCHSSAMUsage)
                {

                    byte[] pDataIn = new byte[numBytes + 8];                    
                   
                    sDesfireCardLayout mDesfireCardlayout;
                    if (SecurityMgr.Instance.getLayoutDefinition(out mDesfireCardlayout, (byte)pAid, (byte)pFileNbr, EAccessPermission.E_AccessPermission_ReadWrite, true))
                    {
                        pDataIn[0] = mDesfireCardlayout.keyReferenceBytes.krbPCD;
                        pDataIn[1] = mDesfireCardlayout.keyReferenceBytes.krbAid;
                        pDataIn[2] = mDesfireCardlayout.keyReferenceBytes.krbRFU;
                        pDataIn[3] = mDesfireCardlayout.CurrentKeyCardNumber;
                        pDataIn[4] = mDesfireCardlayout.CurrentCommunicationByte;
                        //blockData[0],blockData[1],blockData[2],blockData[3]};
                        pDataIn[5] = Offset[0];
                        pDataIn[6] = Offset[1];
                        pDataIn[7] = (byte)numBytes;
                        Array.Copy(pDataBuffer, 0, pDataIn, 8, pDataBuffer.Length);

                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                                 _hRw,
                                                 DEST_TYPE.DEST_CARD,
                                                 CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_WRIT_INS, 0x40, (byte)pFileNbr, pDataIn),
                                                 out _pSw1,
                                                 out _pSw2,
                                                 out pResData);

                    }
                    

                }
                if (_cryptoflexSAMUsage)
                {
                    byte[] pDataIn = new byte[numBytes + 6];


                    byte pKeyRef, pKeyNum;

                    SecurityMgr.Instance.GetPCDKeyRef((byte)pAid, (byte)pFileNbr, _currentKeySet, out pKeyRef, out pKeyNum);

                    pDataIn[0] = pKeyRef;
                    pDataIn[1] = pKeyNum;
                    pDataIn[2] = 0x00;
                    pDataIn[3] = Offset[0];
                    pDataIn[4] = Offset[1];
                    pDataIn[5] = (byte)numBytes;

                    Array.Copy(pDataBuffer, 0, pDataIn, 6, pDataBuffer.Length);

                    Err_Glo = Reader.IsoCommand(_ReaderType,
                                           _hRw,
                                           DEST_TYPE.DEST_CARD,
                                           CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_WRIT_INS, CONSTANT.NULL, (byte)pFileNbr, pDataIn),
                                           out _pSw1,
                                           out _pSw2,
                                           out pResData);
                }
            }
            if (Err_Glo == CONSTANT.NO_ERROR && _pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                return true;
            }
#endif
            return false;

        }

        public CSC_API_ERROR CommitTransaction(out byte pSw1, out byte pSw2)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
#if _BLUEBIRD_
            Err_Glo = Reader.CommitTransaction(out pSw1,out pSw2);
#else
            if (_IsNFCCardDetected )
            {              
                CommitTransactionEx(out pSw1, out pSw2);                
            }
            else
            {
                Err_Glo = Reader.IsoCommand(_ReaderType,
                                      _hRw,
                                      DEST_TYPE.DEST_CARD,
                                      CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_COMT_INS, CONSTANT.NULL, CONSTANT.NULL, CONSTANT.NULL),
                                      out pSw1,
                                      out pSw2,
                                      out pResData);
            }
#endif
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public CSC_API_ERROR AbortTransaction(out byte pSw1, out byte pSw2)
        {
            Err_Glo = CSC_API_ERROR.ERR_NOEXEC;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
#if _BLUEBIRD_
            Err_Glo = Reader.AbortTransaction();
#else
            if (_IsNFCCardDetected)
            {
                Logging.Log(LogLevel.Error, "Abort Txn Function Not implemented for NFC cards ");
                Err_Glo = CSC_API_ERROR.ERR_NOT_AVAIL;
            }
            else
            {
                Err_Glo = Reader.IsoCommand(_ReaderType,
                                      _hRw,
                                      DEST_TYPE.DEST_CARD,
                                      CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_COMT_INS, 0x01, CONSTANT.NULL, CONSTANT.NULL),
                                      out pSw1,
                                      out pSw2,
                                      out pResData);
            }
#endif            
            _pSw1 = pSw1;
            _pSw2 = pSw2;

            return Err_Glo;
        }

        public bool SelectApplication(byte pAppNbr)
        {
#if _BLUEBIRD_
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;
            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

          Err=  Reader.SelectApplication(pAppNbr);
          if (Err == CSC_API_ERROR.ERR_NONE) return true; else return false;
#else

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            byte[] pDataIn = new byte[] { 0x44, 0x4D, pAppNbr };
            if (_IsNFCCardDetected)
            {
                 Err_Glo = Reader.IsoCommand(_ReaderType,
                                    _hRw,
                                    DEST_TYPE.DEST_PICC_TRANSPARENT,
                                    CFunctions.getApdu(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_SELA_INS, CONSTANT.NULL, CONSTANT.NULL, pDataIn,CONSTANT.NULL),
                                    3,
                                    out pSw1,
                                    out pSw2,
                                    out pResData);
                 pSw1 -= 1;
            }
            else
            {
            Err_Glo = Reader.IsoCommand(_ReaderType,
                                    _hRw,
                                    DEST_TYPE.DEST_CARD,
                                    CFunctions.getApdu(CONSTANT.MIFARE_DESFIRE_CLA, CONSTANT.MIFARE_SELA_INS, CONSTANT.NULL, CONSTANT.NULL, pDataIn),
                                    3,
                                    out pSw1,
                                    out pSw2,
                                    out pResData);
            }        

            _pSw1 = pSw1;
            _pSw2 = pSw2;

            if (pSw1 == CONSTANT.COMMAND_SUCCESS && Err_Glo == CSC_API_ERROR.ERR_NONE)
            {
                if (_IsNFCCardDetected)
                {
                    _lastSelectedApplication = pAppNbr;
                    _IsSessionKeyCreatedPrevously = false;
                }
                return true;
            }
            else
                return false;
#endif
        }

        public void SetReaderType(CSC_READER_TYPE pReaderType, int phRw)
        {
            _ReaderType = pReaderType;
            _hRw = phRw;
        }

        public bool GetTDforCCHS(LogicalMedia logMedia, TransactionType Txntype, int TxnSequenceNo, int Amount,
            out string xdrStr, // return value is the result of SerializeHelper<byte[]>.Serialize on a byte array (_xdr.Result)
            bool bWTE, bool bTest, bool bAsPartOfIssueOp)
        {
            return ComposeCCHSTxn.TreatXDRCompatibility(logMedia, out xdrStr,
                Txntype,
                SharedData.TransactionSeqNo, Amount, _hRw, _ReaderType, bWTE, bTest, bAsPartOfIssueOp, null);
        }
        public bool GetTDforCCHS(LogicalMedia logMedia, TransactionType Txntype, int TxnSequenceNo, int Amount, out string xdrStr)
        {
            return GetTDforCCHS(logMedia, Txntype, TxnSequenceNo, Amount, out xdrStr, false, false, false);
        }

        public bool GetTDforCCHSForEFTAddVal(LogicalMedia logMedia, IFS2.Equipment.Common.CCHS.BankTopupDetails bankTopupDetails, int TxnSequenceNo, int Amount, out string xdrStr)
        {
            return ComposeCCHSTxn.TreatXDRCompatibility(logMedia, out xdrStr,
                TransactionType.TXN_CSC_ADD_VALUE_EFT,
                SharedData.TransactionSeqNo, Amount, _hRw, _ReaderType, false, false, false, bankTopupDetails);
        }

        public bool GetTDforCCHSUnreadable(TransactionType Txntype, int TxnSequenceNo, long physicalId, int owner, int deposit, int fareProduct, object pars, out string xdrStr)
        {
            return ComposeCCHSTxn.UnreadableCSC(Txntype, TxnSequenceNo, _hRw, CSC_READER_TYPE.V4_READER,
                false, physicalId, owner, deposit, fareProduct, pars,
                out xdrStr
                );
        }

        public string GetTDforCCHSGen(LogicalMedia logMedia, TransactionType Txntype, object pars, bool bWTE, bool bTest)
        {
            int TransactionSeqNo = ++SharedData.TransactionSeqNo;
            string result;
            switch (Txntype)
            {
                case TransactionType.CSCIssue:
                case TransactionType.AddValueCancel:
                case TransactionType.CSC_BAD_DEBT_CASH_PAYMENT:
                case TransactionType.CSCPenaltySurchargePaidByCash:
                case TransactionType.CSC_SURCHARGE_PAYMENT:
                case TransactionType.CSCImmediateRefund:
                case TransactionType.EnableBankTopup:
                case TransactionType.DisableBankTopup:
                case TransactionType.TPurseBankTopupReload:
                case TransactionType.TXN_CSC_ADD_VALUE_EFT:
                case TransactionType.CSC_SURRENDERED:
                case TransactionType.InitialiseBankTopup:
                case TransactionType.TPurseDeduction:
                case TransactionType.BusCheckOutWithTPurse:
                case TransactionType.MetroCheckOutWithTPurse:
                case TransactionType.MetroCheckOutWithPass:
                case TransactionType.MetroCheckInWithPass:
                case TransactionType.MetroCheckInWithTPurse:
                    ComposeCCHSTxn.TreatXDRCompatibility2(logMedia, out result, Txntype, SharedData.TransactionSeqNo, _hRw, _ReaderType, bWTE, bTest, pars);
                    break;
                default:
                    throw new NotImplementedException();
            }
            logMedia.EquipmentData.SequenceNumber = TransactionSeqNo;
            return Utility.MakeTag("XdrData", result);
        }

        DateTime _dtTillWhenPollingWasActive = DateTime.Now;
#if !_HHD_
        public void StopField()
        {
            // This command always stops field, and shifts R/W to ST_INIT, irrespective of which state the R/W is currently in
            CSC_API_ERROR err = Reader.StopField(_ReaderType, _hRw);
            if (err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(err, _hRw);
        }

        public void StopField(int hRw)
        {
            // This command always stops field, and shifts R/W to ST_INIT, irrespective of which state the R/W is currently in
            CSC_API_ERROR err = Reader.StopField(CSC_READER_TYPE.V4_READER, hRw);
            if (err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(err, hRw);
        }

        public void StartField()
        {
            CSC_API_ERROR err = Reader.StartField(_ReaderType, _hRw);
            if (err != CSC_API_ERROR.ERR_NONE && err != CSC_API_ERROR.ERR_NOEXEC)
                throw GetExceptionForCode(err, _hRw);
        }
#else
        public void StopField(){}
        public short StartField(){return 0;}
        public Scenario GetActiveScenario()
        {
            return Scenario.SCENARIO_1;
        }
#endif
        public ReaderException GetExceptionForCode(CSC_API_ERROR err, int hRw)
        {
            switch (err)
            {
                case CSC_API_ERROR.ERR_NONE:
                    return null;
                default:
                    return new ReaderException(err, hRw);
            }
        }

        public void GetLastResult(out CSC_API_ERROR err, out byte pSw1, out byte pSw2)
        {
            err = Err_Glo;
            pSw1 = _pSw1;
            pSw2 = _pSw2;
        }

        #region "NFC Desfire functions " 
        private byte _lastSelectedApplication = 0xff;
        private byte _lastAuthKeyNum = 0xff;
        private bool _IsSessionKeyCreatedPrevously = false;
        //public void NFCCardDetected(bool bdetected )
        //{
        //    _IsNFCCardDetected = bdetected;
        //}
        private bool Authenticate(byte AppId,byte fileId,byte mAccRight, byte KeyIndex, out byte pSw1, out byte pSw2)
        {
            bool ret = true;
            byte[] pResData;
            CSC_API_ERROR Err;

           
            pSw1 = 0xff;
            pSw2 = 0xff;
#if !_BLUEBIRD_
            Logging.Log(LogLevel.Verbose, "Authenticate  :" + AppId.ToString("X2") + ":" + fileId.ToString("X2") + ":" + KeyIndex.ToString("X2") + ":" + mAccRight.ToString("X2"));
            if (_lastSelectedApplication == AppId && _lastAuthKeyNum == KeyIndex && _IsSessionKeyCreatedPrevously)
            {
                return true;
            }
            else if (_lastSelectedApplication != AppId)
            {
                //select application and process for authentication
                ret = SelectApplication(AppId);
                if (ret)
                {
                    _lastSelectedApplication = AppId;
                }
                else
                {
                    _lastSelectedApplication = 0xff;
                    _IsSessionKeyCreatedPrevously = false;
                    _lastAuthKeyNum = 0xff;
                    return false;
                }
            }
            byte[] bKeyIndex = { KeyIndex };
            _IsSessionKeyCreatedPrevously = false;
            _lastAuthKeyNum = 0xff;
            byte[] apdu = CFunctions.getApdu(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_AUTH_INS, CONSTANT.NULL, CONSTANT.NULL, bKeyIndex, 0x00);
            // log("Authenticate for Application : " + AppId.ToString());
            // string hex = BitConverter.ToString(apdu).Replace("-", string.Empty);

            //log("APDU Command : " + hex);

            Err_Glo = Reader.IsoCommand(_ReaderType,
                                    _hRw,
                                    DEST_TYPE.DEST_PICC_TRANSPARENT,
                                    apdu,
                                    out pSw1,
                                    out pSw2,
                                    out pResData);

            _pSw1 =pSw1;
            _pSw1--;
            _pSw2 = pSw2;
             //Logging.Log(LogLevel.Verbose, "Authenticate Response : " + Err_Glo.ToString());
             Logging.Log(LogLevel.Verbose, "Authenticate SW1: " + pSw1.ToString("X2") + " SW2: " + _pSw2.ToString("X2"));

            if (pSw1 == CONSTANT.COMMAND_SUCCESS + 1 && Err_Glo == CSC_API_ERROR.ERR_NONE)
            {
                if (pResData != null)
                {
                    // log("Response Data: ");
                    //  foreach (byte b in pResData) log(b.ToString("X2") + " ", false);
                    // log(BitConverter.ToString(pResData).Replace("-", string.Empty));
                    //log("");
                    if (pSw2 == 0xAF)
                    {
                        byte[] rnda = null;
                        byte[] mAuthCode = null;
                       ret = GetAuthCode(AppId, fileId, 0x00, mAccRight, pResData, out mAuthCode, out pSw1, out pSw2);
                       if (ret && pSw2 == 0x00)
                       {

                           pSw1 = 0xff;
                           pSw2 = 0xff;
                           ret = Authenticate2(mAuthCode, out rnda, out pSw1, out pSw2);
                           if (pSw1 == CONSTANT.COMMAND_SUCCESS && pSw2 == 0)
                           {
                               ret = true;
                               _IsSessionKeyCreatedPrevously = true;
                               _lastAuthKeyNum = KeyIndex;
                           }
                           else ret = false;

                       }
                       else ret = false;
                        // log("SW1: " + pSw1.ToString("X2") + " SW2: " + pSw2.ToString("X2"));
                    }

                }
            }
# endif
            // Logging.Log(LogLevel.Verbose, " Authenticate result SW1: " + pSw1.ToString("X2") + " SW2: " + pSw2.ToString("X2"));
            return ret;
        }
        private  bool Authenticate(byte AppId, byte KeyIndex, out byte pSw1, out byte pSw2)
        {
            bool ret = true;
            byte[] pResData;
            CSC_API_ERROR Err ;
           
            pSw1 = 0xff;
            pSw2 = 0xff;
#if !_BLUEBIRD_
            Logging.Log(LogLevel.Verbose, "Authenticate SoftDSM :" + AppId.ToString("X2") + ":" +  KeyIndex.ToString("X2"));
            if (_lastSelectedApplication == AppId && _lastAuthKeyNum == KeyIndex && _IsSessionKeyCreatedPrevously)
            {
                return true;
            }
            else if (_lastSelectedApplication != AppId) 
            {                
                    //select application and process for authentication
                ret = SelectApplication(AppId);
                if (ret)
                {
                    _lastSelectedApplication = AppId;
                }
                else
                {
                    _lastSelectedApplication = 0xff;
                    _IsSessionKeyCreatedPrevously = false;
                    _lastAuthKeyNum = 0xff;
                    return false;
                }
            }
            byte[] bKeyIndex = {KeyIndex };
            _IsSessionKeyCreatedPrevously = false;
            _lastAuthKeyNum = 0xff;
            byte[] apdu = CFunctions.getApdu(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_AUTH_INS, CONSTANT.NULL, CONSTANT.NULL, bKeyIndex, 0x00);
           // log("Authenticate for Application : " + AppId.ToString());
           // string hex = BitConverter.ToString(apdu).Replace("-", string.Empty);

            //log("APDU Command : " + hex);

            Err_Glo = Reader.IsoCommand(_ReaderType,
                                    _hRw,
                                    DEST_TYPE.DEST_PICC_TRANSPARENT,
                                    apdu,
                                    out pSw1,
                                    out pSw2,
                                    out pResData);

            _pSw1 -=1;
            _pSw2 = pSw2;
           // log("Authenticate Response : " + Err_Glo.ToString());
            //log("SW1: " + pSw1.ToString("X2") + " SW2: " + _pSw2.ToString("X2"));

            if (pSw1 == CONSTANT.COMMAND_SUCCESS + 1 &&  Err_Glo == CSC_API_ERROR.ERR_NONE)
            {
                if (pResData != null)
                {
                   // log("Response Data: ");
                  //  foreach (byte b in pResData) log(b.ToString("X2") + " ", false);
                   // log(BitConverter.ToString(pResData).Replace("-", string.Empty));
                    //log("");
                    if (pSw2 == 0xAF)
                    {
                        byte[] rnda = null;                      

                        byte[] key = new byte[16];
                        for (int j = 0; j < 16; j++) key[j] =0;// 0 to use default key as of now...
                        byte[] data ;
                        
                        data = SecurityMgr.Instance.CalculateRndAB(pResData, key);
                                               
                        pSw1 = 0xff;
                        pSw1 = 0xff;
                        ret = Authenticate2(data, out rnda, out pSw1, out pSw2);                       
                        if (pSw1 == CONSTANT.COMMAND_SUCCESS && pSw2 == 0)
                        {                            
                            ret = true;
                            _IsSessionKeyCreatedPrevously = true;
                            _lastAuthKeyNum = KeyIndex;
                        }
                        else ret = false;
                        
                       // log("SW1: " + pSw1.ToString("X2") + " SW2: " + pSw2.ToString("X2"));
                    }
                                 
                }
            }
# endif
            return ret;
        }
        private bool Authenticate2(byte[] mRndAB, out byte[] mRndA, out byte pSw1, out byte pSw2)
        {
            bool ret = true;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
            mRndA = new byte[1];
            DEST_TYPE destType = DEST_TYPE.DEST_PICC_TRANSPARENT;
           
            // byte[] cmd_auth2 = getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_auth2 = new byte[6 + mRndAB.Length];
            Array.Clear(cmd_auth2, 0, cmd_auth2.Length);
#if !_BLUEBIRD_
            cmd_auth2[0] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_auth2[1] = ISOCONSTANTS.DESFIRE_MOREDATA_INS; // 0xAF;
            cmd_auth2[2] = 0x00;
            cmd_auth2[3] = 0x00;
            cmd_auth2[4] = (byte)mRndAB.Length;
            Array.Copy(mRndAB, 0, cmd_auth2, 5, mRndAB.Length);

           // string hex = BitConverter.ToString(cmd_auth2).Replace("-", string.Empty);
            
           // log("APDU Command : " + hex);
            Err_Glo = Reader.IsoCommand(_ReaderType,
                                    _hRw,
                                    destType,
                                   cmd_auth2,
                                    out pSw1,
                                    out pSw2,
                                    out m_response);

            if (Err_Glo == CSC_API_ERROR.ERR_NONE && pSw2 == 0)
            {
                if (pSw1 == 0x91 && pSw2 == 0x00)
                {
                  //  log("Response: ");
                  //  foreach (byte b in m_response) log(b.ToString("X2") + " ", false);
                  //  log("");
                    mRndA = new byte[8];
                    Array.Copy(m_response, 0, mRndA, 0, 8);
                }
                else
                {
                    mRndA = new byte[1];
                    ret = false;

                }
            }
            else
            {
                mRndA = new byte[1];
                ret = false;

            }
            pSw1 -= 1;
            Logging.Log(LogLevel.Verbose,"Authenticate2 pSW1: 0x" + pSw1.ToString("X2") + "  pSw2: 0x" + pSw2.ToString("X2"));
#endif
            return ret;
        }
        #region "CCHS SAM direct NFC session key management"
        //private byte _lastFileAccesed, _lastSelectedApplication, _lastKeyUsed;
        private bool GetAuthCode(byte mAppId, byte mFileid, byte oldkey, byte AccessRight, byte[] rndb, out byte[] arAuthCode, out byte pSw1, out byte pSw2)
        {
            
            
            arAuthCode = null;
            bool ret = true;
            pSw1 = 0xff;
            pSw2 = 0xFF;
            #if !_BLUEBIRD_
            if (_delhiCCHSSAMUsage)
            {
                CCHSSAMManger mCCHSSamManger = new CCHSSAMManger(_ReaderType, _hRw);
                foreach (cSAMConf samcnf in SharedData.mSAMUsed)
                {
                    if (samcnf.mSAMType == CONSTANT.SAMType.ISAM)
                    {
                        ret = mCCHSSamManger.GetAuthCode((DEST_TYPE)samcnf.SAM_Slot, mAppId, mFileid, oldkey, AccessRight, rndb, out arAuthCode, out pSw1, out pSw2);
                    }
                }
            }
            else ret = false;
            #endif
            return ret;
        }
        #endregion
        private bool ReadRecords(byte nFileID, int nOffset, int mRecordSize, int pNbrOfRecords, out byte[] mRecords, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            int index = 0, nbytesRead = 0, outbuffIndex = 0;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
           
            mRecords = new byte[mRecordSize * pNbrOfRecords];
#if !_BLUEBIRD_
            byte[] noffsetbts = CFunctions.ConvertToBytesLE(nOffset);
            byte[] nLengthbytes = CFunctions.ConvertToBytesLE(pNbrOfRecords);
            byte[] cmd_buff = new byte[6 + 1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1];

            cmd_buff[index++] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_buff[index++] = ISOCONSTANTS.DESFIRE_READ_RECFILE_INS;
            cmd_buff[index++] = 0x00;
            cmd_buff[index++] = 0x00;
            cmd_buff[index++] = (byte)(1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1);//fileNo[1]+offset[3]+length[3]+datalength
            cmd_buff[index++] = nFileID;

            Array.Copy(noffsetbts, 0, cmd_buff, index, noffsetbts.Length - 1);
            index += 3;

            Array.Copy(nLengthbytes, 0, cmd_buff, index, nLengthbytes.Length - 1);

            Err_Glo = Reader.IsoCommand(_ReaderType,
                                   _hRw,
                                   DEST_TYPE.DEST_PICC_TRANSPARENT,
                                  cmd_buff,
                                   out pSw1,
                                   out pSw2,
                                   out m_response);

            if (Err_Glo == CSC_API_ERROR.ERR_NONE)
            {
                
                if (pSw1 == 0x91 && pSw2 == 0xAF)//Has more data
                {
                    //byte[] cmd_part2 = { 0x90, 0xAF, 0x00, 0x00, 0x00, 0x00 };
                    Array.Copy(m_response, 0, mRecords, outbuffIndex, m_response.Length);
                    outbuffIndex += (m_response.Length);
                    bRet = ReadRecordIntermideate(ref mRecords, outbuffIndex, out pSw1, out pSw2);

                }
                else if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                {

                    Array.Copy(m_response, 0, mRecords, outbuffIndex, m_response.Length);
                    outbuffIndex += (m_response.Length);
                    bRet = true;
                    pSw1--;//to make CSw functions happy ... 
                }
                else // error ...
                {
                    bRet = false;
                }
            }
#endif
            return bRet;
        }
        private bool ReadRecordIntermideate(ref byte[] mRecordBuff, int index, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            byte[] m_response;
            int nbRead = 0;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            #if !_BLUEBIRD_
            DEST_TYPE destType = DEST_TYPE.DEST_PICC_TRANSPARENT;           
            byte[] cmd_buff = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_MOREDATA_INS, 0x00, 0x00, 0x00 };

           // bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbRead, out pSw1, out pSw2);
            Err_Glo = Reader.IsoCommand(_ReaderType,
                                  _hRw,
                                  destType,
                                 cmd_buff,
                                  out pSw1,
                                  out pSw2,
                                  out m_response);
            if (Err_Glo == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 == 0x91 && pSw2 == 0x00)
                {
                    Array.Copy(m_response, 0, mRecordBuff, index, m_response.Length);                   
                    pSw1--; // to make TT happy
                    //return true;
                    bRet = true;
                }
                else if (pSw1 == 0x91 && pSw2 == 0xAF) //continure to read
                {
                    Array.Copy(m_response, 0, mRecordBuff, index, m_response.Length);
                    index += (m_response.Length);
                    ReadRecordIntermideate(ref mRecordBuff, index, out pSw1, out pSw2);
                }
                else
                {
                    bRet = false;
                }
            }
# endif
            return bRet;
        }

        private bool ReadDataFile(byte nFileID, int nOffset, int nLength, out byte[] nDataBuff, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            pSw1 = 0xff;
            pSw2 = 0xff;
            
            int index = 0, nbytesRead = 0; ;
            byte[] m_response;

            byte[] noffsetbts = CFunctions.ConvertToBytesLE(nOffset);
            byte[] nLengthbytes = CFunctions.ConvertToBytesLE(nLength);
            nDataBuff = new byte[nLength];

            byte[] cmd_ReadData = new byte[6 + 1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1];//CLS+INS+P1+p2+Lc+data(fileNo[1]+offset[3]+length[3])+Le//{ ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS, 0x00, 0x00, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
#if !_BLUEBIRD_
            cmd_ReadData[index++] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_ReadData[index++] = ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS;
            cmd_ReadData[index++] = 0x00;
            cmd_ReadData[index++] = 0x00;
            cmd_ReadData[index++] = (byte)(1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1);//fileNo[1]+offset[4]+length[4]
            cmd_ReadData[index++] = nFileID;

            Array.Copy(noffsetbts, 0, cmd_ReadData, index, noffsetbts.Length - 1);
            index += 3;

            Array.Copy(nLengthbytes, 0, cmd_ReadData, index, nLengthbytes.Length - 1);

            Err_Glo = Reader.IsoCommand(_ReaderType,
                                   _hRw,
                                   DEST_TYPE.DEST_PICC_TRANSPARENT,
                                  cmd_ReadData,
                                   out pSw1,
                                   out pSw2,
                                   out m_response);
            if (Err_Glo == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 == 0x91 && pSw2 == 0xAF)//Has more data
                {
                    byte[] cmd_part2 = { 0x90, 0xAF, 0x00, 0x00, 0x00, 0x00 };
                    //TODO: TO be added...

                }
                else if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                {
                    pSw1--;//to make CSw functions happy ... 
                    Array.Copy(m_response, 0, nDataBuff, 0, m_response.Length);
                    bRet = true;
                }
                else // error ...
                {

                }
            }
# endif
            return bRet;
        }
        private bool WriteDataFileEx(byte appId, byte fileId,bool IsRecordFile,  int nOffset, int nDataLen, byte[] abyWriteData, out byte pSw1, out byte pSw2)
        {
           
            bool bRet = false;
            byte keyIndex = 0x00;
            byte[] m_response;
            int index = 0, nbytesRead = 0;
            int MaxDatatobeSent;
            pSw1 = 0xff;
            pSw2 = 0xff;
            #if !_BLUEBIRD_
            if (appId == 0x01)
            {
                if (fileId < 5) keyIndex = (byte)(fileId + 1);
                else keyIndex = fileId;
            }
            else if (appId == 0x02) keyIndex = 0x01;
            else
            {
                Logging.Log(LogLevel.Error, "WriteDataFileEx Wrong AppID:" + appId.ToString("X2"));
                return false;
            }
            Logging.Log(LogLevel.Verbose, "WriteDataFileEx In data:" + appId.ToString("X2") + ":" + fileId.ToString("X2") + ":" + keyIndex.ToString("X2") + ":" + IsRecordFile.ToString());  
            if (_IsSoftSAM)
                bRet = Authenticate(appId, keyIndex, out pSw1, out pSw2);
            else
                bRet = Authenticate(appId, fileId, 0x01, keyIndex, out pSw1, out pSw2);
            if (bRet)
            {
                byte File_INS = ISOCONSTANTS.DESFIRE_WRITE_DATAFILE_INS;
                //if (appId == 0x01 && fileId == 0x03 ) File_INS = ISOCONSTANTS.DESFIRE_WRITE_RECFILE_INS;
                if (IsRecordFile) File_INS = ISOCONSTANTS.DESFIRE_WRITE_RECFILE_INS;

                if (nDataLen > 32)
                {
                    MaxDatatobeSent = 32;
                }
                else MaxDatatobeSent = nDataLen;

                byte[] noffsetbts = CFunctions.ConvertToBytesLE(nOffset);
                byte[] nLengthbytes = CFunctions.ConvertToBytesLE(nDataLen);

                byte[] cmd_buff = new byte[6 + 1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1 + MaxDatatobeSent];

                cmd_buff[index++] = ISOCONSTANTS.DESFIRE_CLA;
                cmd_buff[index++] = File_INS;// ISOCONSTANTS.DESFIRE_WRITE_DATAFILE_INS;
                cmd_buff[index++] = 0x00;
                cmd_buff[index++] = 0x00;
                cmd_buff[index++] = (byte)(1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1 + nDataLen);//fileNo[1]+offset[3]+length[3]+datalength
                cmd_buff[index++] = fileId;

                Array.Copy(noffsetbts, 0, cmd_buff, index, noffsetbts.Length - 1);
                index += 3;

                Array.Copy(nLengthbytes, 0, cmd_buff, index, nLengthbytes.Length - 1);

                index += 3;

                {
                    Array.Copy(abyWriteData, 0, cmd_buff, index, MaxDatatobeSent);
                }

                Err_Glo = Reader.IsoCommand(_ReaderType,
                                       _hRw,
                                       DEST_TYPE.DEST_PICC_TRANSPARENT,
                                      cmd_buff,
                                       out pSw1,
                                       out pSw2,
                                       out m_response);

                if (Err_Glo == CSC_API_ERROR.ERR_NONE)
                {
                    if (pSw1 == 0x91 && pSw2 == 0x00)//Data written successfully
                    {
                        bRet = true;
                        pSw1--;//to make tt happy
                    }
                    else if (pSw1 == 0x91 && pSw2 == 0xAF) // more data to be written
                    {
                        pSw1 = 0xff;
                        pSw2 = 0xff;
                        byte[] cmd_buff2 = new byte[6 + (nDataLen - MaxDatatobeSent)];
                        cmd_buff2[0] = 0x90;
                        cmd_buff2[1] = ISOCONSTANTS.DESFIRE_MOREDATA_INS;
                        cmd_buff2[2] = 0x00;
                        cmd_buff2[3] = 0x00;
                        cmd_buff2[4] = (byte)(nDataLen - MaxDatatobeSent);
                        Array.Copy(abyWriteData, 32, cmd_buff2, 5, (nDataLen - MaxDatatobeSent));

                        //bRet = base.ExchangeAPDU((byte)cmd_buff2.Length, cmd_buff2, out m_response, out nbytesRead, out pSw1, out pSw2);
                        Err_Glo = Reader.IsoCommand(_ReaderType,
                                       _hRw,
                                       DEST_TYPE.DEST_PICC_TRANSPARENT,
                                      cmd_buff,
                                       out pSw1,
                                       out pSw2,
                                       out m_response);
                        if (Err_Glo == CSC_API_ERROR.ERR_NONE)
                        {
                            if (pSw1 == 0x91 && pSw2 == 0x00)//Data written successfully
                            {
                                bRet = true;
                                pSw1--;//to make tt happy
                            }
                            else
                            {// error
                                bRet = false;
                            }
                        }
                    }
                    else //error condition
                    {
                        bRet = false;
                    }
                }
            }
            Logging.Log(LogLevel.Verbose, "WriteDataFileEx Ret: " + bRet.ToString() + "pSw1:" + pSw1.ToString("X2") + "pSw2:" + pSw2.ToString("X2"));
#endif
            return bRet;
        }
        private bool WriteValueFileEx(byte fileId, byte keyIndex, byte appId, out byte pSw1, out byte pSw2, Int32 pValue, bool IsForCredit)
        {
            bool bRet=false;
            byte[] m_response;
            
            Logging.Log(LogLevel.Verbose, "WriteValueFileEx  IN IsForCredit " + IsForCredit.ToString() + " with amount = " + pValue.ToString());
            //bool IsForCredit = (pValue > 0);
            pSw1 = pSw2 = Byte.MaxValue;
#if !_BLUEBIRD_
            byte INS = ISOCONSTANTS.DESFIRE_CREDIT_INS;
            if (!IsForCredit)
            {
                INS = ISOCONSTANTS.DESFIRE_DEBIT_INS;
                pValue = Math.Abs(pValue);
            }
//            pSw1 = pSw2 = Byte.MaxValue;
//#if !_BLUEBIRD_
            if (pValue == 0)
            {
                Err_Glo= CSC_API_ERROR.ERR_NONE;
                return true;
            }

           
            //byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

           
            if (_IsSoftSAM)
                bRet = Authenticate(appId, keyIndex, out pSw1, out pSw2);
            else
                bRet = Authenticate(appId, fileId, 0x01, keyIndex, out pSw1, out pSw2);
            if (bRet)
            {
                int index = 0;                
                byte[] blockData = CFunctions.ConvertToBytesLE(pValue);
                byte[] cmd_buff = new byte[6 + 1 + blockData.Length];//{ ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.CLA, 0x00, 0x00, 0x00 };
                cmd_buff[index++] = ISOCONSTANTS.DESFIRE_CLA;
                cmd_buff[index++] = INS;
                cmd_buff[index++] = 0x00;//p1
                cmd_buff[index++] = 0x00;//p2
                cmd_buff[index++] = (byte)(1 + blockData.Length);//LC
                cmd_buff[index++] = fileId;
                Array.Copy(blockData, 0, cmd_buff, index, blockData.Length);
                cmd_buff[index + blockData.Length] = 0x00;

                Err_Glo = Reader.IsoCommand(_ReaderType,
                                  _hRw,
                                  DEST_TYPE.DEST_PICC_TRANSPARENT,
                                 cmd_buff,
                                  out pSw1,
                                  out pSw2,
                                  out m_response);
                if (Err_Glo == CSC_API_ERROR.ERR_NONE)
                {
                    if (pSw1 == 0x91 && pSw2 == 0x00)//Data written successfully
                    {
                        bRet = true;
                        pSw1--;//to make tt happy
                    }
                    else
                    {// error
                        bRet = false;
                    }
                }
            }
            Logging.Log(LogLevel.Verbose, "WriteValueFileEx Ret: " + bRet.ToString() + "pSw1:" + pSw1.ToString("X2") + "pSw2:" + pSw2.ToString("X2"));
# endif
            return bRet;
        }
        public bool CommitTransactionEx(out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xFF;
            #if !_BLUEBIRD_
            byte[] cmd_buff = {0x90,0xC7,0x00,0x00,0x00};
            Err_Glo = Reader.IsoCommand(_ReaderType,
                                  _hRw,
                                  DEST_TYPE.DEST_PICC_TRANSPARENT,
                                 cmd_buff,
                                  out pSw1,
                                  out pSw2,
                                  out m_response);
                if (Err_Glo == CSC_API_ERROR.ERR_NONE)
                {
                    if (pSw1 == 0x91 && pSw2 == 0x00)//Data written successfully
                    {
                        ret = true;
                        pSw1--;//to make tt happy
                    }
                    else
                    {// error
                        ret = false;
                    }
                }
# endif
            return ret;

        }

        #endregion
    }
}