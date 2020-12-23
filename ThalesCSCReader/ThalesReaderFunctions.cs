using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Diagnostics;
using System.Threading;

namespace IFS2.Equipment.TicketingRules
{
    public class ThalesReaderFunction : IReaderInterface
    {
        private int cschandle=-1;
        private bool _isReaderConnected = false, _SAMActivated=false;

     //   private static bool _delhiCCHSSAMUsage=false;
     //   private static bool _cryptoflexSAMUsage=true;  
        private  CONSTANT.SAMType e_samType = CONSTANT.SAMType.NONE;
        private  DelhiCCHSSAM mDelhiCCHSSAM;
        private  CSC_READER_TYPE _ReaderType = CSC_READER_TYPE.V4_READER;
        private string _portNum = "";
        FirmwareInfo _mFirmware;
        private int _samslot = 1;
        IntPtr _listener = IntPtr.Zero;
        IntPtr _listenerDetectionRemoval = IntPtr.Zero;
        private int _lastErrorCode;
        public int LastErrorCode { get { return _lastErrorCode; } }

       // private MifareSAM mMifareSAM; 

        public ThalesReaderFunction()           
        {
            cschandle = -1;
            _isReaderConnected = false;
              e_samType = (CONSTANT.SAMType)(Configuration.ReadParameter("SAMType", "byte", "I"));
       //     _cryptoflexSAMUsage = (bool)Configuration.ReadParameter("CryptoflexSAMUsage","bool","true");             
        }
        public FirmwareInfo GetReaderFirmwareData()
        {
            return _mFirmware;
        }
        public ThalesReaderFunction(CONSTANT.SAMType samtype, int samslot, CSC_READER_TYPE pReaderType, string portnum)
        {
            _portNum = portnum;
            e_samType = samtype;
            _samslot = samslot;
            _ReaderType = pReaderType;
           // _mFirmware = null;
            _isReaderConnected = false;
            //if(samtype == CONSTANT.SAMType.NXP_SAM_AV1 || samtype== CONSTANT.SAMType.NXP_SAM_AV2)
           //     mMifareSAM = new MifareSAM(this, (int)samtype, 0, samslot);
        }
        /*
        public bool ConfigureSAM(byte[] samkey)
        {
            bool ret = false;
            byte pSw1=0xff, pSw2=0xff;
            if (_isReaderConnected)
            {
                _SAMActivated = false;
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    //mMifareSAM.
                  
                    int r = InstallCardSAM((DEST_TYPE)_samslot);
                    if (r==0)
                    {
                       ret= mMifareSAM.ActivateSAM(samkey, 0x00, 0x00, 0x00, out pSw1, out pSw2);
                       _SAMActivated = ret; 
                    }
                }
            }
            return ret;
        }*/
        public int InstallCardSAM(DEST_TYPE pSam)
        {
            int ret = -3;
            CSC_API_ERROR Err;

            //Install SAM1 Card 
            InstallCard pSamCardParams = new InstallCard();

            pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)pSam;
            pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T1;
            pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);//CHECK +1 REMOVED IN AVM-TT            

            //  log(LogLevel.Verbose, "Before SAMInstallCard");
            Err = this.InstallCard(this._ReaderType,
                              this.cschandle,
                              pSam,
                              pSamCardParams);

            ret = (int)Err;
            //Logging.Log(LogLevel.Verbose, " SAM Installcard Response code: " + ret.ToString() );

            return ret;
        }
        public CSC_API_ERROR InstallCardsForPolling()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_DEVICE;
            InstallCard pCscCardParams = new InstallCard();

            pCscCardParams.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            //pCscCardParams.iCardParam.xMifParam.sSize = 0;
            //  pCscCardParams.iCardParam.xMifParam.sSize = 51;
            //  pCscCardParams.iCardParam.xMifParam.acOptionString = "PCD_TO_PICC_COM_SPEED=424;PICC_TO_PCD_COM_SPEED=424";


            Err = this.InstallCard(CSC_READER_TYPE.V4_READER,
                                     cschandle,
                                     DEST_TYPE.DEST_CARD,
                                     pCscCardParams);

            //Err = Reader.InstallCard(CSC_READER_TYPE.V4_READER,
            //                         hRw,
            //                        (DEST_TYPE) DEST_PICC_TRANSPARENT,
            //                         pCscCardParams);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                var pScenarioPolling = new ScenarioPolling[1];

                pScenarioPolling[0].xCardType = (int)(CSC_TYPE.CARD_14443_A);//CARD_MIFARE1);
                pScenarioPolling[0].ucAntenna = CONSTANT.SMART_ANTENNA_1;
                pScenarioPolling[0].ucRepeatNumber = 1;
                Err = this.ConfigureForPolling(CSC_READER_TYPE.V4_READER, cschandle, pScenarioPolling, Scenario.SCENARIO_1);

            }
            return Err;
        }
        
        public  void Start(CONSTANT.SAMType samtype, CSC_READER_TYPE pReaderType)
        {
            e_samType = samtype;
            _ReaderType = pReaderType;
            //if(e_samType == CONSTANT.SAMType.ISAM)
             //   
        }

        public int getCscHandle()
        {
            return cschandle;
        }

        public void setCscHandle(int phRw)
        {
            cschandle = phRw;
        }

         public CSC_API_ERROR StatusCheck(CSC_READER_TYPE pReaderType,                                                
                                                ref StatusCSC pStatusCSC)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    return StatusV4(cschandle, ref pStatusCSC);
                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }

         public CSC_API_ERROR StopReader(CSC_READER_TYPE pReaderType,
                                               int phRw)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:

                    return StopReaderV4(phRw);

                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }
         public CSC_API_ERROR StopReader()
         {
             switch (_ReaderType)
             {
                 case CSC_READER_TYPE.V3_READER:
                 case CSC_READER_TYPE.V4_READER:
                     return StopReaderV4(cschandle);
                 default:
                     return CSC_API_ERROR.ERR_DEVICE;
             }
         }

         public CSC_API_ERROR InstallCard(CSC_READER_TYPE pReaderType,
                                                int phRw,
                                                DEST_TYPE pDestType,
                                                InstallCard pInstCardParams)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    return InstallCardV4(phRw,
                                         pDestType,
                                         pInstCardParams);
                default:

                    return CSC_API_ERROR.ERR_DEVICE;
           }
        }

         public CSC_API_ERROR ConfigureForPolling(CSC_READER_TYPE pReaderType,
                                                 int phRw,
                                                 ScenarioPolling[] pPollingParams
                                                 , Scenario scenarioNum
            )
        {
            _lastErrorCode = ThalesReaderAdapter.sSmartConfigEx(phRw, (byte)scenarioNum, (byte)pPollingParams.Length, pPollingParams);
            return (CSC_API_ERROR)_lastErrorCode;
        }
        
         public CSC_API_ERROR StartPolling(CSC_READER_TYPE pReaderType,
                                                 int phRw,
            byte scenario,
            Utility.StatusListenerDelegate listener
            )
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    return StartPollingV4(phRw, scenario, listener);
                default:
                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }
         public CSC_API_ERROR StartPolling(CSC_READER_TYPE pReaderType,
                                                      byte scenario,
            Utility.StatusListenerDelegate listener
            )
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    return StartPollingV4(cschandle, scenario, listener);
                default:
                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }

         public CSC_API_ERROR StartPolling(CSC_READER_TYPE pReaderType,
                                                  Utility.StatusListenerDelegate listener
            )
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    return StartPollingV4(cschandle, listener);

                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }
        
         public CSC_API_ERROR StopPolling(CSC_READER_TYPE pReaderType)
        {
            short ERR_CODE = CONSTANT.IS_ERROR;

            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sSmartStopPollingEx(cschandle);

                    return (CSC_API_ERROR)ERR_CODE;

                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }


         public CSC_API_ERROR IsoCommand(                                                
                                                DEST_TYPE pDestType,
                                                byte[] pCommandApdu,
                                                out byte pSw1,
                                                out byte pSw2,
                                                out byte[] pResData)
         {
             return IsoCommand(_ReaderType,
                 cschandle,
                 pDestType,
                 pCommandApdu,
                 1,
                 out pSw1,
                 out pSw2,
                 out pResData);
         }
        /*
         public bool IsoCommand(
                                    DEST_TYPE pDestType,
                                    byte[] pCommandApdu,
                                    out byte pSw1,
                                    out byte pSw2,
                                    out byte[] pResData)
         {
            
         }*/
         public CSC_API_ERROR IsoCommand(CSC_READER_TYPE pReaderType,
                                               int phRw,
                                               DEST_TYPE pDestType,
                                               byte[] pCommandApdu,
                                               out byte pSw1,
                                               out byte pSw2,
                                               out byte[] pResData)
        {
            return IsoCommand(pReaderType,
                phRw,
                pDestType,
                pCommandApdu,
                1,
                out pSw1,
                out pSw2,
                out pResData);
        }
        /* @PARAMs : 
         * pReaderType  -> Type of Reader (V3, V4, Note : V0-V2 not supported
         * phRw         -> CSC Handle (Refer CSC API Docs
         * pCommandApdu -> Formatted APDU to the Reader
         * pRespDataLen -> length of the response data
         * pSw1, pSw2   -> SWx codes
         * pResData     -> Response data
         */
         public CSC_API_ERROR IsoCommand(CSC_READER_TYPE pReaderType,
                                               int phRw,
                                               DEST_TYPE pDestType,
                                               byte[] pCommandApdu,
            int maxAttempt,
                                               out byte pSw1,
                                               out byte pSw2,
                                               out byte[] pResData)
        {
            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:

                    return IsoCommandV4(phRw,
                                         pDestType,
                                         pCommandApdu,
                                         maxAttempt,
                                         out pSw1,
                                         out pSw2,
                                         out pResData);                    
                default:
                    pResData[0] = 0xFF;
                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }
         public CSC_API_ERROR HaltCard()
         {
             switch (_ReaderType)
             {
                 case CSC_READER_TYPE.V3_READER:
                 case CSC_READER_TYPE.V4_READER:
                     {
                         _lastErrorCode = ThalesReaderAdapter.sSmartHaltCardEx(cschandle);
                         return (CSC_API_ERROR)_lastErrorCode;
                     }
                 default:
                     Debug.Assert(false);
                     return CSC_API_ERROR.ERR_NONE;
             }
         }
         public CSC_API_ERROR HaltCard(CSC_READER_TYPE pReaderType, 
            int phRw)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = ThalesReaderAdapter.sSmartHaltCardEx(phRw);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }                
                default:
                    Debug.Assert(false);
                    return CSC_API_ERROR.ERR_NONE;
            }
        }

         public CSC_API_ERROR SwitchToCardOnState(CSC_READER_TYPE pReaderType,
            int phRw)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = ThalesReaderAdapter.sSmartStopDetectRemovalEx(phRw);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
                default:
                    Debug.Assert(false);
                    return CSC_API_ERROR.ERR_NONE;
            }
        }
         public CSC_API_ERROR SwitchToCardOnState(CSC_READER_TYPE pReaderType)
         {
             switch (pReaderType)
             {
                 case CSC_READER_TYPE.V3_READER:
                 case CSC_READER_TYPE.V4_READER:
                     {
                         _lastErrorCode = ThalesReaderAdapter.sSmartStopDetectRemovalEx(cschandle);
                         return (CSC_API_ERROR)_lastErrorCode;
                     }
                 default:
                     Debug.Assert(false);
                     return CSC_API_ERROR.ERR_NONE;
             }
         }

         public CSC_API_ERROR SwitchToDetectRemovalState(CSC_READER_TYPE pReaderType,       
             Utility.StatusListenerDelegate listener)
         {
             switch (pReaderType)
             {
                 case CSC_READER_TYPE.V4_READER:
                     {
                         if (listener == null)
                             _lastErrorCode = ThalesReaderAdapter.sSmartStartDetectRemovalEx(cschandle, CONSTANT.DETECTION_WITHOUT_EVENT, IntPtr.Zero);
#if !WindowsCE && !MonoLinux && !NoAdditionalTests
                         else
                         {
                             if (_Del_listenerDetectionRemoval == null)
                                 _Del_listenerDetectionRemoval = new Utility.StatusListenerDelegate(listener);

                             _lastErrorCode = ThalesReaderAdapter.sSmartStartDetectRemovalEx_V4(cschandle, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerDetectionRemoval);
                         }
#endif
                         return (CSC_API_ERROR)_lastErrorCode;
                     }
                 default:
                     // for V3, detection removal is not supported 
                     throw new NotSupportedException();
             }
         }

         public CSC_API_ERROR SwitchToDetectRemovalState(CSC_READER_TYPE pReaderType,
            int phRw,
            Utility.StatusListenerDelegate listener)
        {
            switch (pReaderType)
            {                
                case CSC_READER_TYPE.V4_READER:
                    {
                        if (listener == null)
                            _lastErrorCode = ThalesReaderAdapter.sSmartStartDetectRemovalEx(phRw, CONSTANT.DETECTION_WITHOUT_EVENT, IntPtr.Zero);
#if !WindowsCE && !MonoLinux && !NoAdditionalTests
                        else
                        {
                            if (_Del_listenerDetectionRemoval == null)
                                _Del_listenerDetectionRemoval = new Utility.StatusListenerDelegate(listener);

                            _lastErrorCode = ThalesReaderAdapter.sSmartStartDetectRemovalEx_V4(phRw, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerDetectionRemoval);
                        }
#endif
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
                default:
                    // for V3, detection removal is not supported 
                    throw new NotSupportedException();                    
            }
        }

         Utility.StatusListenerDelegate _Del_listenerDetectionRemoval = null, _Del_listenerStartPolling = null;
         internal CSC_API_ERROR InstallCardV4(int phRw,
                                                    DEST_TYPE pDestType,
                                                    InstallCard pInstCardParams)
        {
#if WindowsCE
            
                    Logging.Log(LogLevel.Verbose, "inside InstallCardV4");
                  //  InstallCard* ptrinstCardParams = &pInstCardParams;
                    IntPtr piInstCardParams = IntPtr.Zero;// Marshal.AllocHGlobal(28);

                    //if (pInstCardParams.xCardType == (int)DEST_TYPE.DEST_CARD || pInstCardParams.xCardType ==(int) DEST_TYPE.DEST_SAM_DESFIRE)
                    if(pDestType == DEST_TYPE.DEST_SAM_DESFIRE || pDestType == DEST_TYPE.DEST_CARD)
                    {
                        Logging.Log(LogLevel.Verbose, "inside InstallCardV4 pInstCardParams.xCardType =" + pInstCardParams.xCardType.ToString());
                        byte[] pbyte = new byte[28];
                        pbyte[0] = (byte)(pInstCardParams.xCardType) ;
                        pbyte[1] = (byte)(pInstCardParams.xCardType>>8);
                        pbyte[2] = (byte)(pInstCardParams.xCardType>>16);
                        pbyte[3] = (byte)(pInstCardParams.xCardType>>24);
                        pbyte[4] = (byte)(pInstCardParams.iCardParam.xMifParam.sSize);
                        pbyte[5] = (byte)(pInstCardParams.iCardParam.xMifParam.sSize>>8);
                        int i = 6;
                       // if ((pInstCardParams.iCardParam.xMifParam.acOptionString.Length > 0) && pInstCardParams.iCardParam.xMifParam.acOptionString[0]!=0)
                       if(pInstCardParams.iCardParam.xMifParam.sSize>0)
                        {
                            byte[] optionsbyte = new byte[(int)pInstCardParams.iCardParam.xMifParam.sSize];
                           Encoding.ASCII.GetBytes(pInstCardParams.iCardParam.xMifParam.acOptionString, 0, (int)pInstCardParams.iCardParam.xMifParam.sSize,optionsbyte,0);
                            foreach (byte element in optionsbyte)
                            {
                                pbyte[i++] = element;
                            }
                            pbyte[i] = 0;
                        }
                       // 
                        piInstCardParams = Marshal.AllocHGlobal(pbyte.Length + 2);
                        Marshal.Copy(pbyte, 0, piInstCardParams, pbyte.Length);
                        Marshal.WriteInt16(piInstCardParams, pbyte.Length, 0);

                    }
                    else
                    {

                        Logging.Log(LogLevel.Verbose, "inside else  InstallCardV4 pInstCardParams.xCardType =" + pInstCardParams.xCardType.ToString());
                        byte[] pbyte = new byte[28];
                        pbyte[0] = (byte)(pInstCardParams.xCardType);
                        pbyte[1] = (byte)(pInstCardParams.xCardType >> 8);
                        pbyte[2] = (byte)(pInstCardParams.xCardType >> 16);
                        pbyte[3] = (byte)(pInstCardParams.xCardType >> 24);
                        pbyte[4] = pInstCardParams.iCardParam.xSamParam.ucSamSelected;
                        pbyte[5] = (byte)(pInstCardParams.iCardParam.xSamParam.ucProtocolType);
                        pbyte[6] = (byte)(pInstCardParams.iCardParam.xSamParam.ulTimeOut); //ulTimeOut
                        pbyte[7] = (byte)(pInstCardParams.iCardParam.xSamParam.ulTimeOut>>8);
                        pbyte[8] = (byte)(pInstCardParams.iCardParam.xSamParam.ulTimeOut >> 16);
                        pbyte[9] = (byte)(pInstCardParams.iCardParam.xSamParam.ulTimeOut >> 24); 
                        int i = 10;
                        if ((pInstCardParams.iCardParam.xSamParam.acOptionString.Length > 0) && pInstCardParams.iCardParam.xSamParam.acOptionString[0] != 0)
                        {
                            foreach (byte element in Encoding.ASCII.GetBytes(pInstCardParams.iCardParam.xSamParam.acOptionString))
                            {
                                pbyte[i++] = element;
                            }
                            pbyte[i] = 0;
                        }
                        // 
                        piInstCardParams = Marshal.AllocHGlobal(pbyte.Length + 2);
                        Marshal.Copy(pbyte, 0, piInstCardParams, pbyte.Length);
                        Marshal.WriteInt16(piInstCardParams, pbyte.Length, 0);
                    }

                    _lastErrorCode = V4Adaptor.sSmartInstCardEx(phRw, pDestType, piInstCardParams);

                    return (CSC_API_ERROR)_lastErrorCode;
            
#else
            IntPtr piInstCardParams = Marshal.AllocHGlobal(Marshal.SizeOf(pInstCardParams));
            Marshal.StructureToPtr(pInstCardParams, piInstCardParams, false);
           // IntPtr piInstCardParams = MarshalAnsi.StructureToPtr(pInstCardParams);
            _lastErrorCode = ThalesReaderAdapter.sSmartInstCardEx(phRw, pDestType, piInstCardParams);
            return (CSC_API_ERROR)_lastErrorCode;
#endif
        }
        
         internal CSC_API_ERROR StartPollingV4(int phRw, byte scenarioNum, Utility.StatusListenerDelegate lisenter)
        {
            if (lisenter == null)
                _lastErrorCode = ThalesReaderAdapter.sSmartStartPollingEx(phRw, scenarioNum, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITHOUT_EVENT, IntPtr.Zero);
#if !WindowsCE && !MonoLinux && !NoAdditionalTests
            else
            {
                if (_Del_listenerStartPolling == null)                
                    _Del_listenerStartPolling = new Utility.StatusListenerDelegate(lisenter);                    
                
                _lastErrorCode = ThalesReaderAdapter.sSmartStartPollingEx_V4(phRw, scenarioNum, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerStartPolling);
            }
#endif
            return (CSC_API_ERROR)_lastErrorCode;
        }

         internal CSC_API_ERROR StartPollingV4(int phRw, Utility.StatusListenerDelegate lisenter)
        {
            return StartPollingV4(phRw, 1, lisenter);            
        }

         internal CSC_API_ERROR IsoCommandV4(int phRw,
                                                   DEST_TYPE pDestType,
                                                   byte[] pCommandApdu,
            int maxReattemptsInCaseOfErrData,
                                                   out byte pSw1,
                                                   out byte pSw2,
                                                   out byte[] pResData)
        {
            short ERR_CODE = CONSTANT.IS_ERROR;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            short DataLen = Convert.ToInt16(CONSTANT.MAX_ISO_DATA_OUT_LENGTH);

            try
            {
                for (int i = 0; i < maxReattemptsInCaseOfErrData; i++)
                {
                    IntPtr piDataOut = Marshal.AllocHGlobal(CONSTANT.MAX_API_DATA_OUT_LENGTH);
                    //jl: This I think has to be replaces DataLen by sizeof(Int16) to check
                    IntPtr piDataLen = Marshal.AllocHGlobal(DataLen);

                    Marshal.WriteInt16(piDataLen, DataLen);

                    _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sSmartISOEx(phRw, pDestType, Convert.ToInt16(pCommandApdu.Length), pCommandApdu, piDataLen, piDataOut);

                    if (ERR_CODE == CONSTANT.NO_ERROR)
                    {

                        CFunctions.processApduRes(piDataOut,
                                                  piDataLen,
                                                  out pSw1,
                                                  out pSw2,
                                                  out pResData);
                    }
                    unsafe
                    {
                        ushort* ResLenPtr = (ushort*)piDataLen.ToPointer();

                        DataLen = (short)*ResLenPtr;
                    }

                    if (DataLen <= 2)
                    {
                        pResData = null;
                    }

                    Marshal.FreeHGlobal(piDataOut);
                    Marshal.FreeHGlobal(piDataLen);

                    if (ERR_CODE != (short)CSC_API_ERROR.ERR_DATA)
                        break;
                }
                return (CSC_API_ERROR)ERR_CODE;
            }
            catch (Exception)
            {
                Logging.Log(LogLevel.Error, "ReaderFunctions -> IsoCommandV4: Exception ..");

                return (CSC_API_ERROR)ERR_CODE;
            }
        }

        /// <summary>
        /// Reloads the reader plain.
        /// </summary>
        /// <param name="pReaderType">ReaderType v4 only supported</param>
        /// <param name="pReaderComm">Reader Communication Settings</param>
        /// <param name="phRw">out handle</param>
        /// <param name="pFirmware">out Firmware Information</param>
        /// <param name="rfPower">The rf power.</param>
        /// <returns>CSC_API_ERROR</returns>
         public CSC_API_ERROR ReloadReaderPlain(CSC_READER_TYPE pReaderType,
                                         ReaderComm pReaderComm,
                                         out int phRw,
                                         out FirmwareInfo pFirmware,
            byte? rfPower)
        {
            CSC_API_ERROR Err;
            phRw = -1;

            try
            {
                unsafe
                {
                    switch (pReaderType)
                    {
                        case CSC_READER_TYPE.V3_READER:
                        case CSC_READER_TYPE.V4_READER:
                            Logging.Log(LogLevel.Verbose, "ReaderFunctions.ReloadReader before InitReaderv4");
                            Err = InitReader(pReaderComm, out phRw, out pFirmware, rfPower);
                            Logging.Log(LogLevel.Verbose, "ReaderFunctions.ReloadReader after InitReaderv4");
                            break;
                        default:
                            throw new NotSupportedException();                            
                    }

                    return Err;
                }
            }
            catch (Exception)
            {
                Logging.Log(LogLevel.Error, "ReaderFunctions -> ReloadReader: Exception");
                pFirmware = new FirmwareInfo() ;
                return CSC_API_ERROR.ERR_DEVICE;
            }
        }

         public bool SAMInstallCard(InstallCard mInstallcard, CSC_TYPE cardType)
         {
             CSC_API_ERROR err = CSC_API_ERROR.ERR_NOT_AVAIL;
             if (_isReaderConnected)
             {
                 err = InstallCard(_ReaderType, cschandle, (DEST_TYPE)mInstallcard.iCardParam.xSamParam.ucSamSelected, mInstallcard);
             }//_isReaderConnected
             return err == CSC_API_ERROR.ERR_NONE ? true : false;
         }
         public bool ReloadReader()
         {
             Logging.Trace("ThalesReaderFunctions.ReloadReader.Start");
             CSC_API_ERROR err = CSC_API_ERROR.ERR_NOT_AVAIL;
             ReaderComm rcomm = new ReaderComm();
             _mFirmware = new FirmwareInfo();
             rcomm.COM_PORT = _portNum;
             rcomm.COM_SPEED = 115200;
             //cschandle=-1;
             int phandle = -1;
             _isReaderConnected = false;
             err = ReloadReader(_ReaderType, rcomm, out phandle, out _mFirmware);
             Logging.Trace("ThalesReaderFunctions.ReloadReader.AfterReloadReaderCall "+((int)err).ToString());
             if (err == CSC_API_ERROR.ERR_NONE) _isReaderConnected = true;

             return _isReaderConnected;
         }
         private CSC_API_ERROR ReloadReader(CSC_READER_TYPE pReaderType,
                                                 ReaderComm pReaderComm,
                                                 out int phRw,
                                                 out FirmwareInfo pFirmware)
        {
            CSC_API_ERROR Err;

            phRw = -1;
            _ReaderType = pReaderType;
            pFirmware.Chargeur = "XXX_NO_INFO_XXX";
            pFirmware.AppCSC = "XXX_NO_INFO_XXX";
            pFirmware.Fpga1 = "XXX_NO_INFO_XXX";
            pFirmware.Fpga2 = "XXX_NO_INFO_XXX";

            //Install SAM1 Card 
           /* InstallCard pSamCardParams = new InstallCard();

            pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)(DEST_TYPE.DEST_SAM1);
            pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T0;
            pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);//CHECK +1 REMOVED IN AVM-TT    */        

            try
            {
                unsafe
                {
                    switch (pReaderType)
                    {
                        case CSC_READER_TYPE.V4_READER:
                        case CSC_READER_TYPE.V3_READER:
                         Logging.Log(LogLevel.Verbose, "ReaderFunctions.ReloadReader before InitReaderv4");
                         byte readerRFPower = (byte)Configuration.ReadParameter("PowerOfCSCReader", "byte", "1");
                         Err = InitReader(pReaderComm, out phRw, out pFirmware, readerRFPower);
                         Logging.Log(LogLevel.Verbose, "ReaderFunctions.ReloadReader after InitReaderv4");
                           break;

                        default:

                            return CSC_API_ERROR.ERR_DEVICE;
                    }

                    if (Err == CONSTANT.NO_ERROR)
                    {
                        Logging.Log(LogLevel.Verbose, "Before Reader.InstallCard");

                        //if (_cryptoflexSAMUsage)
                  /*      if(e_samType == CONSTANT.SAMType.THALES_SAM)
                        {
                        Err = InstallCard(pReaderType,
                                          phRw,
                                          DEST_TYPE.DEST_SAM1,
                                          pSamCardParams);
                        }
                        else if (e_samType == CONSTANT.SAMType.ISAM || e_samType == CONSTANT.SAMType.PSAM)
                        {
                            mDelhiCCHSSAM = new DelhiCCHSSAM(pReaderType, phRw);
                            mDelhiCCHSSAM.SAMInstallCard(DEST_TYPE.DEST_SAM5);
                        }
                   */
                        cschandle = phRw;
                        Err = this.InstallCardsForPolling();
                        Logging.Log(LogLevel.Verbose, "after Reader.InstallCard return code = " + Err.ToString());
                        if (Err == CONSTANT.NO_ERROR)
                        {
                            
                        }
                        return Err;
                    }

                    return Err;
                }
            }
            catch (Exception)
            {
                Logging.Log(LogLevel.Error, "ReaderFunctions -> ReloadReader: Exception");
                return CSC_API_ERROR.ERR_DEVICE;
            }
        }

         internal CSC_API_ERROR InitReader(ReaderComm pReaderComm,
                                                   out int phRw,
                                                   out FirmwareInfo pFirmware, byte? rfPower)
        {
            short ERR_CODE = CONSTANT.IS_ERROR;

            phRw = -1;

            pFirmware.Chargeur = "XXX_NO_INFO_XXX";
            pFirmware.AppCSC = "XXX_NO_INFO_XXX";
            pFirmware.Fpga1 = "XXX_NO_INFO_XXX";
            pFirmware.Fpga2 = "XXX_NO_INFO_XXX";

            try
            {
                unsafe
                {
                    IntPtr piMajorVersion = Marshal.AllocHGlobal(sizeof(int)),
                           piMinorVersion = Marshal.AllocHGlobal(sizeof(int));

                    
                    //Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4, before  ThalesReader.sCSCReaderGetApiVersionEx");
                    _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sCSCReaderGetApiVersionEx(piMajorVersion, piMinorVersion);
                   // ERR_CODE = ThalesReader.sCSCReaderGetApiVersionEx(&pMajorVersion, &pMinorVersion);
                    //Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4, after  ThalesReader.sCSCReaderGetApiVersionEx");

                    if (ERR_CODE == CONSTANT.NO_ERROR)
                    {
                        int* MajorVerPtr = (int*)piMajorVersion.ToPointer();
                        int* MinorVerPtr = (int*)piMinorVersion.ToPointer();

                        int MajorVersion = *MajorVerPtr;
                        int MinorVersion = *MinorVerPtr;
                       // Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV4, before  if (MajorVersion >= 1 || MinorVersion >= 2)");
                        Logging.Log(LogLevel.Information, "InitReaderV4 Versions CSC API " + Convert.ToString(MajorVersion) + "." + Convert.ToString(MinorVersion));

                        //Check for minimum version of 1.2 CSC V4 API
                        if (MajorVersion >= 1 || MinorVersion >= 2)
                        {
#if !WITHOUT_SHARED_DATA
                          //  SharedData.cscApiVersion = Convert.ToString(MajorVersion) + "." + Convert.ToString(MinorVersion);//SKS to be checked 
#endif
                            Logging.Log(LogLevel.Verbose, "InitReaderV4 Start Communication " + Convert.ToString(pReaderComm.COM_PORT)+";"+Convert.ToString(pReaderComm.COM_SPEED));
                            //TO CHECK FOLLOWING PART. IS Not in Comment IN AVM-TT
#if WindowsCE
                            IntPtr strPtr;
                            if (pReaderComm.COM_PORT == null)
                                strPtr = IntPtr.Zero;
                            else
                            {
                                byte[] bytes = Encoding.ASCII.GetBytes(pReaderComm.COM_PORT);
                                strPtr = Marshal.AllocHGlobal(bytes.Length + 2);
                                Marshal.Copy(bytes, 0, strPtr, bytes.Length);
                                Marshal.WriteInt16(strPtr, bytes.Length, 0);                                
                               // Logging.Log(LogLevel.Verbose,"strPtr = "+ strPtr.ToString());  
                            }
                            _lastErrorCode = ERR_CODE = V4Adaptor.sCSCReaderStartEx(strPtr, pReaderComm.COM_SPEED, out phRw);
                            if (strPtr != IntPtr.Zero) Marshal.FreeHGlobal(strPtr);
#else
                            if (cschandle >= 0)
                            {
                                ERR_CODE = ThalesReaderAdapter.sCSCReaderStopEx(cschandle);
                                cschandle = -1;
                            }
                            _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sCSCReaderStartEx(pReaderComm.COM_PORT, pReaderComm.COM_SPEED, out phRw);
#endif
                            Logging.Log(LogLevel.Verbose, "InitReaderV4 Start Communication " + Convert.ToString(ERR_CODE)+";"+Convert.ToString(phRw));

                            if (ERR_CODE == CONSTANT.NO_ERROR && phRw>=0)
                            {
                                cschandle = phRw;
                                StatusCSC pStatusCSC;
                                _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sSmartStatusEx(phRw, out pStatusCSC);

                                Logging.Log(LogLevel.Verbose, "InitReaderV4 Reading Status " + Convert.ToString(ERR_CODE));
                                if (ERR_CODE != 0)
                                {
                                    // It is observed (not have time to see which all scenarios) that even when R/W is disconnected, sCSCReaderStartEx says ERR_NONE. 
                                    // But we get ERR_TIMEOUT in sSmartStatusEx for this scenario.
                                    Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4,  pStatusCSC.ucStatCSC: " + pStatusCSC.ucStatCSC);
                                    return (CSC_API_ERROR)ERR_CODE;
                                }
                                if (pStatusCSC.ucStatCSC != CONSTANT.ST_VIRGIN)
                                {
                                    Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4 Restarting Reader....");
                                    _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sCscRebootEx(phRw);                                  
                                }
                                if (rfPower != null)
                                {
                                    Logging.Log(LogLevel.Verbose, "InitReaderV4 SetReaderRFPower to: " + ((byte)rfPower).ToString("X2"));
                                    ERR_CODE = (short)SetReaderRFPower((byte)rfPower, phRw);
                                    Logging.Log(LogLevel.Verbose, "InitReaderV4 SetReaderRFPower Return Code:  " + Convert.ToString(ERR_CODE) + ";" + Convert.ToString(phRw));
                                }
#if!WindowsCE
                                CSC_BOOTIDENT pOutFirmareName;
                                    Logging.Log(LogLevel.Verbose, "InitReaderV4 Has rebooted " + Convert.ToString(ERR_CODE));

                                _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sCscConfigEx(phRw, out pOutFirmareName);

                                pFirmware.Chargeur = Convert.ToString(pOutFirmareName.ucBootLabel);
                                pFirmware.AppCSC = Convert.ToString(pOutFirmareName.ucPrgLabel);
                                if (_ReaderType == CSC_READER_TYPE.V4_READER) 
                                {
                                    //uding Fpga1 field for storing API version...
                                    pFirmware.Fpga1 = "API Ver:" + MajorVersion.ToString() + "." + MinorVersion.ToString();
                                }
                                else
                                {
                                    pFirmware.Fpga1 = Convert.ToString(pOutFirmareName.ucFPGA1Label);
                                    pFirmware.Fpga2 = Convert.ToString(pOutFirmareName.ucFPGA2Label);
                                }
                                //_mFirmware = new FirmwareInfo();
                                _mFirmware = pFirmware;
#else
                                    CSC_FW_INFO cscinfo = new CSC_FW_INFO();                               
                                   
                                    Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4, before V4Adaptor.sCscGetFwInfoEx ");
                                    _lastErrorCode = ERR_CODE = V4Adaptor.sCscGetFwInfoEx(phRw, &cscinfo);
                                    if (ERR_CODE == 0)
                                    {
                                        byte[] arr = new byte[48];
                                        int i;
                                        for ( i = 0; i < 48 && cscinfo.blVersionName[i]!=0; i++)
                                        {
                                           // Logging.Log(LogLevel.Verbose, "Byte" + cscinfo.blVersionName[i]);
                                            arr[i] = cscinfo.blVersionName[i];
                                        }
                                        arr[i]= 0;
                                    
                                        pFirmware.Chargeur = System.Text.Encoding.ASCII.GetString(arr, 0, i);
                                        Logging.Log(LogLevel.Verbose, "Loader = " + pFirmware.Chargeur);
                                        for (i = 0; i < 48 && cscinfo.appVersionName[i] != 0; i++)
                                        {
                                         //   Logging.Log(LogLevel.Verbose, "Byte" + cscinfo.blVersionName[i]);
                                            arr[i] = cscinfo.appVersionName[i];
                                        }
                                        arr[i] = 0;
                                        pFirmware.AppCSC = System.Text.Encoding.ASCII.GetString(arr, 0, i);
                                        Logging.Log(LogLevel.Verbose, "Loader = " + pFirmware.AppCSC);
                                      //  pFirmware.Fpga1 = "";
                                       // pFirmware.Fpga2 = "";
                                    }
  
#endif
                                Logging.Log(LogLevel.Information, "ReaderFunctions -> Device Started");

                                Marshal.FreeHGlobal(piMajorVersion);
                                Marshal.FreeHGlobal(piMinorVersion);

                                return (CSC_API_ERROR)ERR_CODE;
                            }
                            else
                            {
                                Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV4: Device not Started");

                                return CSC_API_ERROR.ERR_DEVICE;
                            }
                        }
                        else
                        {
                            Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV4: Error API");

                            return CSC_API_ERROR.ERR_API;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV4: Exception .." +Ex.Message);
            }

            return (CSC_API_ERROR)ERR_CODE;

        }
         public CSC_API_ERROR SetReaderRFPower(byte mRFPower, int hRW)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = IsoCommand((CSC_READER_TYPE.V4_READER),
                                  hRW,
                                  DEST_TYPE.DEST_GEN,
                                  CFunctions.getApdu(CONSTANT.CSC_CLA, CONSTANT.CSC_INS, mRFPower, CONSTANT.NULL),
                                  out pSw1,
                                  out pSw2,
                                  out pResData);
            _lastErrorCode = (int)Err;
            return Err;

        }        

         private CSC_API_ERROR StatusV4(int phRw, ref StatusCSC pStatusCSC)
        {
            short ERR_CODE = CONSTANT.IS_ERROR;
            try
            {
                _lastErrorCode = ERR_CODE = ThalesReaderAdapter.sSmartStatusEx(phRw, out pStatusCSC);
                return (CSC_API_ERROR)ERR_CODE;
            }
            catch (Exception )
            {
                Logging.Log(LogLevel.Error, "ReaderFunctions -> StatusV4: Exception ..");
                return (CSC_API_ERROR)ERR_CODE;
            }
        }

         internal CSC_API_ERROR StopReaderV4(int phRw)
        {
            _lastErrorCode = ThalesReaderAdapter.sCSCReaderStopEx(phRw);
            return (CSC_API_ERROR)_lastErrorCode;
        }
         
        
        // make it available for WindowsCE  or MonoLinux, if needed
        
        public  CSC_API_ERROR PingReader(CSC_READER_TYPE cSC_READER_TYPE)
        {
#if !WindowsCE  && !MonoLinux
            short MINPINGSIZE = 7, MINPONGSIZE = 7;
            switch (cSC_READER_TYPE)
            {                
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = ThalesReaderAdapter.sCscPingEx(cschandle, MINPINGSIZE, MINPONGSIZE);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
                default:
                    throw new NotSupportedException(); // v3 doesn't offer pinging.
            }
#else
            throw new NotImplementedException();
#endif
        }

        public  CSC_API_ERROR StopField(CSC_READER_TYPE cSC_READER_TYPE, int _hRw)
        {
            switch (cSC_READER_TYPE)
            {
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.V3_READER:                    
                        _lastErrorCode = ThalesReaderAdapter.sSmartFieldEx(_hRw, (byte)0);
                        return (CSC_API_ERROR)_lastErrorCode;                    
                default:
                    return CSC_API_ERROR.ERR_NOT_AVAIL;
            }
        }
        public  CSC_API_ERROR StopField(CSC_READER_TYPE cSC_READER_TYPE)
        {
            switch (cSC_READER_TYPE)
            {
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.V3_READER:
                    _lastErrorCode = ThalesReaderAdapter.sSmartFieldEx(cschandle, (byte)0);
                    return (CSC_API_ERROR)_lastErrorCode;
                default:
                    return CSC_API_ERROR.ERR_NOT_AVAIL;
            }
        }
        public  CSC_API_ERROR StartField(CSC_READER_TYPE cSC_READER_TYPE)
        {
            _lastErrorCode =(int) CSC_API_ERROR.ERR_API;

            if (cSC_READER_TYPE == CSC_READER_TYPE.V4_READER || cSC_READER_TYPE == CSC_READER_TYPE.V3_READER)
                _lastErrorCode = ThalesReaderAdapter.sSmartFieldEx(cschandle, CONSTANT.FIELD_ON);

            return (CSC_API_ERROR)_lastErrorCode;
        }
        public CSC_API_ERROR InitSecurityModule()
        {
            _lastErrorCode = (int)CSC_API_ERROR.ERR_API;

            return (CSC_API_ERROR)_lastErrorCode;
        }

        public override bool InitReader(int readertype, string readerport, int samtype, int samslot)
        {
            throw new NotImplementedException();
        }

        public override bool IsReaderConnected()
        {
           // throw new NotImplementedException();            
            return _isReaderConnected;
        }

        public override bool IsoCommandExe(DEST_TYPE pDestType, byte[] pCommandApdu, out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            return IsoCommand(_ReaderType,
                cschandle,
                pDestType,
                pCommandApdu,
                1,
                out pSw1,
                out pSw2,
                out pResData) == CSC_API_ERROR.ERR_NONE ? true : false;
        }

        public bool CheckandUpdateReaderFirmware(string sfilepath)
        {
            bool ret = false;
            //step 1: opne the file and read header, check its version
            if (_isReaderConnected)
            {
                try
                {

                    FileStream stream = new FileStream(sfilepath, FileMode.Open, FileAccess.Read);
                    byte[] block = new byte[48];
                    long pos = stream.Seek(-176, SeekOrigin.End);
                    stream.Read(block, 0, 48);
                    stream.Close();
                    string strappver = System.Text.Encoding.ASCII.GetString(block).Trim();
                    char[] ch = { '\0' };
                    string appver = strappver.TrimEnd(ch);
                    char[] dili = {'v' };
                    Logging.Log(LogLevel.Information, "Firmware File appli version: " + appver + " CSC Appli version: " + _mFirmware.AppCSC);
                    string[] arr_fileappver = appver.Split(dili);
                    string[] arr_cscappver = _mFirmware.AppCSC.Split(dili);

                    if (arr_fileappver.Length == 2 && arr_cscappver.Length == 2)
                    {
                        if (arr_cscappver[1] != arr_fileappver[1])// both versions are different
                        {
                            Logging.Log(LogLevel.Verbose, "Firmware not same try to update firmware... ");
                            int retry = 3;
                            int err = -1;
                            do
                            {
                                err = ThalesReaderAdapter.sCscInstallFileEx(cschandle, sfilepath);
                                if (err != 0)
                                {
                                    Thread.Sleep(5 * 1000);
                                    Logging.Log(LogLevel.Error, "Failed to update firmware... retrying... ");
                                }
                                else
                                {
                                    Logging.Log(LogLevel.Verbose, "Firmware Update Successfull... ");
                                    break;
                                }
                                retry--;
                            } while (retry > 0 );
                            if (err == 0) ret = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(LogLevel.Error, "CheckandUpdateReaderFirmware exception filepath: " + sfilepath);
                }
                if (ret)
                {
                    Logging.Log(LogLevel.Information, "Rebooting the reader.... ");
                    ThalesReaderAdapter.sCscRebootEx(cschandle);
                }
            }
            return ret;
        }
    }
}
