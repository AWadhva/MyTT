using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReaderAdaptor;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Diagnostics;

namespace IFS2.Equipment.CSCReader
{
    public class Reader
    {
        private static int hRw=-1;

        private static bool _delhiCCHSSAMUsage=false;
        private static bool _cryptoflexSAMUsage=true;        

        static Reader()
        {
            _delhiCCHSSAMUsage= (bool)Configuration.ReadParameter("DelhiCCHSSAMUsage","bool","false");
            _cryptoflexSAMUsage = (bool)Configuration.ReadParameter("CryptoflexSAMUsage","bool","true");
            hRw = -1;
        }
        
        public static void Start(bool cryptoflex, bool delhiCCHS)
        {
            _delhiCCHSSAMUsage = delhiCCHS;
            _cryptoflexSAMUsage = cryptoflex;
        }

        public int getCscHandle()
        {
            return hRw;
        }

        public void setCscHandle(int phRw)
        {
            hRw = phRw;
        }

        static public CSC_API_ERROR StatusCheck(CSC_READER_TYPE pReaderType,
                                                int phRw,
                                                ref StatusCSC pStatusCSC)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.VIRTUAL_READER:
                    return StatusV4(pReaderType,phRw, ref pStatusCSC);               
                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }

        static public CSC_API_ERROR StopReader(CSC_READER_TYPE pReaderType,
                                               int phRw)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.VIRTUAL_READER:
                    return StopReaderV4(pReaderType,phRw);
                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }

        static public CSC_API_ERROR InstallCard(CSC_READER_TYPE pReaderType,
                                                int phRw,
                                                DEST_TYPE pDestType,
                                                InstallCard pInstCardParams)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.VIRTUAL_READER:
                    return InstallCardV4(pReaderType ,phRw,
                                         pDestType,
                                         pInstCardParams);
                default:

                    return CSC_API_ERROR.ERR_DEVICE;
           }
        }

        static public CSC_API_ERROR ConfigureForPolling(CSC_READER_TYPE pReaderType,
                                                 int phRw,
                                                 ScenarioPolling[] pPollingParams
                                                 , Scenario scenarioNum
            )
        {
#if !WindowsCE && !MonoLinux && !NoVirtualReader
            if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sSmartConfigEx(phRw, (byte)scenarioNum, (byte)pPollingParams.Length, pPollingParams);
            else 
#endif
                _lastErrorCode = V4Adaptor.sSmartConfigEx(phRw, (byte)scenarioNum, (byte)pPollingParams.Length, pPollingParams);
            return (CSC_API_ERROR)_lastErrorCode;
        }
        
        static public CSC_API_ERROR StartPolling(CSC_READER_TYPE pReaderType,
                                                 int phRw,
            byte scenario,
            Utility.StatusListenerDelegate listener
            )
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.VIRTUAL_READER:
                    return StartPollingV4(pReaderType,phRw, scenario, listener);

                default:
                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }

        static public CSC_API_ERROR StartPolling(CSC_READER_TYPE pReaderType,
                                                 int phRw, Utility.StatusListenerDelegate listener
            )
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.VIRTUAL_READER:
                    return StartPollingV4(pReaderType, phRw, listener);

                default:

                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }
        
        static public CSC_API_ERROR StopPolling(CSC_READER_TYPE pReaderType,
                                                int phRw)
        {
            short ERR_CODE = CONSTANT.IS_ERROR;

            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    _lastErrorCode = ERR_CODE = V4Adaptor.sSmartStopPollingEx(phRw);
                    return (CSC_API_ERROR)ERR_CODE;
#if !WindowsCE
#if !MonoLinux
#if !NoVirtualReader
                case CSC_READER_TYPE.VIRTUAL_READER:
                    _lastErrorCode = ERR_CODE = CSCThalesVirtualReader.sSmartStopPollingEx(phRw);
                    return (CSC_API_ERROR)ERR_CODE;
#endif
#endif
#endif

                default:
                    return CSC_API_ERROR.ERR_DEVICE;
            }
        }


        static public CSC_API_ERROR IsoCommand(CSC_READER_TYPE pReaderType,
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
        static public CSC_API_ERROR IsoCommand(CSC_READER_TYPE pReaderType,
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
                case CSC_READER_TYPE.VIRTUAL_READER:

                    return IsoCommandV4(pReaderType,phRw,
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

        static public CSC_API_ERROR HaltCard(CSC_READER_TYPE pReaderType, 
            int phRw)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = V4Adaptor.sSmartHaltCardEx(phRw);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
#if !WindowsCE
#if !MonoLinux
#if !NoVirtualReader
                case CSC_READER_TYPE.VIRTUAL_READER:
                    return (CSC_API_ERROR)CSCThalesVirtualReader.sSmartHaltCardEx(phRw);
#endif
#endif
#endif
                default:
                    Debug.Assert(false);
                    return CSC_API_ERROR.ERR_NONE;
            }
        }

        static public CSC_API_ERROR SwitchToCardOnState(CSC_READER_TYPE pReaderType,
            int phRw)
        {
            switch (pReaderType)
            {
                case CSC_READER_TYPE.V3_READER:
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = V4Adaptor.sSmartStopDetectRemovalEx(phRw);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
#if !WindowsCE
#if !MonoLinux
#if !NoVirtualReader
                case CSC_READER_TYPE.VIRTUAL_READER:
                    _lastErrorCode = CSCThalesVirtualReader.sSmartStopDetectRemovalEx(phRw);
                    return (CSC_API_ERROR)_lastErrorCode;
#endif
#endif
#endif
                default:
                    Debug.Assert(false);
                    return CSC_API_ERROR.ERR_NONE;
            }
        }

        static public CSC_API_ERROR SwitchToDetectRemovalState(CSC_READER_TYPE pReaderType,
            int phRw,
            Utility.StatusListenerDelegate listener)
        {
            switch (pReaderType)
            {                
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.VIRTUAL_READER:
                    {
                        if (listener == null)
                        {
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                            if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sSmartStartDetectRemovalEx(phRw, CONSTANT.DETECTION_WITHOUT_EVENT);
                            else 
#endif
                                _lastErrorCode = V4Adaptor.sSmartStartDetectRemovalEx(phRw, CONSTANT.DETECTION_WITHOUT_EVENT, IntPtr.Zero);
                        }
#if !WindowsCE && !MonoLinux && !NoAdditionalTests
                        else
                        {
                            if (_Del_listenerDetectionRemoval == null)
                                _Del_listenerDetectionRemoval = new Utility.StatusListenerDelegate(listener);
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                            if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sSmartStartDetectRemovalEx(phRw, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerDetectionRemoval);
                            else 
#endif
                                _lastErrorCode = V4Adaptor.sSmartStartDetectRemovalEx_V4(phRw, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerDetectionRemoval);
                        }
#endif
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
                default:
                    // for V3, detection removal is not supported 
                    throw new NotSupportedException();                    
            }
        }

        static Utility.StatusListenerDelegate _Del_listenerDetectionRemoval = null, _Del_listenerStartPolling = null;
        static internal CSC_API_ERROR InstallCardV4(CSC_READER_TYPE pReaderType,int phRw,
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
#if !WindowsCE && !MonoLinux && !NoVirtualReader
            if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sSmartInstCardEx(phRw, pDestType, pInstCardParams);
            else 
#endif
                _lastErrorCode = V4Adaptor.sSmartInstCardEx(phRw, pDestType, piInstCardParams);
            return (CSC_API_ERROR)_lastErrorCode;
#endif
        }
        static IntPtr _listener = IntPtr.Zero;
        static IntPtr _listenerDetectionRemoval = IntPtr.Zero;
        static internal CSC_API_ERROR StartPollingV4(CSC_READER_TYPE pReaderType,int phRw, byte scenarioNum, Utility.StatusListenerDelegate lisenter)
        {
            if (lisenter == null)
            {
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sSmartStartPollingEx(phRw, scenarioNum, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITHOUT_EVENT, null);
                else 
#endif
                    _lastErrorCode = V4Adaptor.sSmartStartPollingEx(phRw, scenarioNum, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITHOUT_EVENT, IntPtr.Zero);
            }
#if !WindowsCE && !MonoLinux && !NoAdditionalTests
            else
            {
                if (_Del_listenerStartPolling == null)
                    _Del_listenerStartPolling = new Utility.StatusListenerDelegate(lisenter);
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sSmartStartPollingEx(phRw, scenarioNum, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerStartPolling);
                else
#endif 
                    _lastErrorCode = V4Adaptor.sSmartStartPollingEx_V4(phRw, scenarioNum, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITH_EVENT, _Del_listenerStartPolling);
            }
#endif
            return (CSC_API_ERROR)_lastErrorCode;
        }

        static internal CSC_API_ERROR StartPollingV4(CSC_READER_TYPE pReaderType, int phRw, Utility.StatusListenerDelegate lisenter)
        {
            return StartPollingV4(pReaderType,phRw, 1, lisenter);            
        }

        static internal CSC_API_ERROR IsoCommandV4(CSC_READER_TYPE pReaderType, int phRw,
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
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                    if (pReaderType == CSC_READER_TYPE.VIRTUAL_READER)
                    {
                        byte[] DataOut;
                        _lastErrorCode = ERR_CODE = CSCThalesVirtualReader.sSmartISOEx(phRw, pDestType, Convert.ToInt16(pCommandApdu.Length), pCommandApdu, out DataLen, out DataOut);
                        if (DataOut == null)
                        {
                            DataLen = 0;
                        }
                        else
                        {
                            Marshal.WriteInt16(piDataLen, DataLen);
                            unsafe
                            {
                                byte* opArray = (byte*)piDataOut.ToPointer();
                                if (DataLen > 0) for (i = 0; i < DataOut.Length; i++) *(opArray + i) = DataOut[i];
                            }
                        }
                    }
                    else
#endif
                        _lastErrorCode = ERR_CODE = V4Adaptor.sSmartISOEx(phRw, pDestType, Convert.ToInt16(pCommandApdu.Length), pCommandApdu, piDataLen, piDataOut);

                    if (ERR_CODE == CONSTANT.NO_ERROR)
                    {

                        CFunctions.processApduRes(piDataOut,
                                                  piDataLen,
                                                  out pSw1,
                                                  out pSw2,
                                                  out pResData);
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
        static public CSC_API_ERROR ReloadReaderPlain(CSC_READER_TYPE pReaderType,
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
                        case CSC_READER_TYPE.VIRTUAL_READER:
                            Logging.Log(LogLevel.Verbose, "ReaderFunctions.ReloadReader before InitReaderv4");
                            Err = InitReaderV4(pReaderType,pReaderComm, out phRw, out pFirmware, rfPower);
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

        static public CSC_API_ERROR ReloadReader(CSC_READER_TYPE pReaderType,
                                                 ReaderComm pReaderComm,
                                                 out int phRw,
                                                 out FirmwareInfo pFirmware)
        {
            CSC_API_ERROR Err;

            phRw = -1;

            pFirmware.Chargeur = "XXX_NO_INFO_XXX";
            pFirmware.AppCSC = "XXX_NO_INFO_XXX";
            pFirmware.Fpga1 = "XXX_NO_INFO_XXX";
            pFirmware.Fpga2 = "XXX_NO_INFO_XXX";

            //Install SAM1 Card 
            InstallCard pSamCardParams = new InstallCard();

            pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)(DEST_TYPE.DEST_SAM1);
            pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T0;
            pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);//CHECK +1 REMOVED IN AVM-TT            

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
                         Err = InitReaderV4(pReaderType,pReaderComm, out phRw, out pFirmware, readerRFPower);
                         Logging.Log(LogLevel.Verbose, "ReaderFunctions.ReloadReader after InitReaderv4");
                           break;

                        default:

                            return CSC_API_ERROR.ERR_DEVICE;
                    }

                    if (Err == CONSTANT.NO_ERROR)
                    {
                        Logging.Log(LogLevel.Verbose, "Before Reader.InstallCard");

                        if (_cryptoflexSAMUsage)
                        {
                        Err = Reader.InstallCard(pReaderType,
                                          phRw,
                                          DEST_TYPE.DEST_SAM1,
                                          pSamCardParams);
                        }
                        Logging.Log(LogLevel.Verbose, "after Reader.InstallCard return code = " + Err.ToString());
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

        static internal CSC_API_ERROR InitReaderV4(CSC_READER_TYPE pReaderType,ReaderComm pReaderComm,
                                                   out int phRw,
                                                   out FirmwareInfo pFirmware, byte? rfPower )
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

                    int MajorVersion=0;
                    int MinorVersion=0;
                    //Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4, before  V4Adaptor.sCSCReaderGetApiVersionEx");
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                    if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode=ERR_CODE=CSCThalesVirtualReader.sCSCReaderGetApiVersionEx(out MajorVersion, out MinorVersion);
                    else
#endif 
                        _lastErrorCode = ERR_CODE = V4Adaptor.sCSCReaderGetApiVersionEx(piMajorVersion, piMinorVersion);
                   // ERR_CODE = V4Adaptor.sCSCReaderGetApiVersionEx(&pMajorVersion, &pMinorVersion);
                    //Logging.Log(LogLevel.Verbose, "ReaderFunctions -> InitReaderV4, after  V4Adaptor.sCSCReaderGetApiVersionEx");

                    if (ERR_CODE == CONSTANT.NO_ERROR)
                    {
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                        if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER)
                        {
                        }
                        else
                        {
#endif
                            int* MajorVerPtr = (int*)piMajorVersion.ToPointer();
                            int* MinorVerPtr = (int*)piMinorVersion.ToPointer();

                            MajorVersion = *MajorVerPtr;
                            MinorVersion = *MinorVerPtr;
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                        }
#endif
                       // Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV4, before  if (MajorVersion >= 1 || MinorVersion >= 2)");
                        Logging.Log(LogLevel.Information, "InitReaderV4 Versions CSC API " + Convert.ToString(MajorVersion) + "." + Convert.ToString(MinorVersion));

                        //Check for minimum version of 1.2 CSC V4 API
                        if (MajorVersion >= 1 || MinorVersion >= 2)
                        {
#if !WITHOUT_SHARED_DATA
                            SharedData.cscApiVersion = Convert.ToString(MajorVersion) + "." + Convert.ToString(MinorVersion);
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
                            //SKS added on 20190501
                            //if (hRw >= 0)
                            //{
                            //    Logging.Log(LogLevel.Verbose, "InitReaderV4 a previous session was already open , closing now");
                            //    ERR_CODE = V4Adaptor.sCSCReaderStopEx(hRw);
                            //    hRw = -1;
                            //}
                            //~SKS
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                            if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode=ERR_CODE=CSCThalesVirtualReader.sCSCReaderStartEx(pReaderComm.COM_PORT, pReaderComm.COM_SPEED, out phRw);
                            else
#endif 
                                _lastErrorCode = ERR_CODE = V4Adaptor.sCSCReaderStartEx(pReaderComm.COM_PORT, pReaderComm.COM_SPEED, out phRw);
#endif
                            Logging.Log(LogLevel.Verbose, "InitReaderV4 Start Communication " + Convert.ToString(ERR_CODE)+";"+Convert.ToString(phRw));                           

                            if (ERR_CODE == CONSTANT.NO_ERROR)
                            {
                                hRw = phRw;//SKS added on 20190501
                                StatusCSC pStatusCSC;
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                                if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = ERR_CODE = CSCThalesVirtualReader.sSmartStatusEx(phRw, out pStatusCSC);
                                else 
#endif
                                    _lastErrorCode = ERR_CODE = V4Adaptor.sSmartStatusEx(phRw, out pStatusCSC);

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
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                                    if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = ERR_CODE = CSCThalesVirtualReader.sCscRebootEx(phRw); 
                                    else
#endif 
                                        _lastErrorCode = ERR_CODE = V4Adaptor.sCscRebootEx(phRw);                                  
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
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                                if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = ERR_CODE = CSCThalesVirtualReader.sCscConfigEx(phRw, out pOutFirmareName);
                                else 
#endif
                                    _lastErrorCode = ERR_CODE = V4Adaptor.sCscConfigEx(phRw, out pOutFirmareName);

                                pFirmware.Chargeur = Convert.ToString(pOutFirmareName.ucBootLabel);
                                pFirmware.AppCSC = Convert.ToString(pOutFirmareName.ucPrgLabel);
                                pFirmware.Fpga1 = Convert.ToString(pOutFirmareName.ucFPGA1Label);
                                pFirmware.Fpga2 = Convert.ToString(pOutFirmareName.ucFPGA2Label);
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
        static public CSC_API_ERROR SetReaderRFPower(byte mRFPower, int hRW)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand((CSC_READER_TYPE.V4_READER),
                                  hRW,
                                  DEST_TYPE.DEST_GEN,
                                  CFunctions.getApdu(CONSTANT.CSC_CLA, CONSTANT.CSC_INS, mRFPower, CONSTANT.NULL),
                                  out pSw1,
                                  out pSw2,
                                  out pResData);
            _lastErrorCode = (int)Err;
            return Err;

        }        


        static private CSC_API_ERROR StatusV4(CSC_READER_TYPE pReader_Type, int phRw, ref StatusCSC pStatusCSC)
        {
            short ERR_CODE = CONSTANT.IS_ERROR;
            try
            {
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                if (pReader_Type==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = ERR_CODE = CSCThalesVirtualReader.sSmartStatusEx(phRw, out pStatusCSC);
                else
#endif 
                    _lastErrorCode = ERR_CODE = V4Adaptor.sSmartStatusEx(phRw, out pStatusCSC);
                return (CSC_API_ERROR)ERR_CODE;
            }
            catch (Exception )
            {
                Logging.Log(LogLevel.Error, "ReaderFunctions -> StatusV4: Exception ..");
                return (CSC_API_ERROR)ERR_CODE;
            }
        }


        static internal CSC_API_ERROR StopReaderV4(CSC_READER_TYPE pReaderType,int phRw)
        {
#if !WindowsCE && !MonoLinux && !NoVirtualReader
            if (pReaderType==CSC_READER_TYPE.VIRTUAL_READER) _lastErrorCode = CSCThalesVirtualReader.sCSCReaderStopEx(phRw);
            else
#endif 
                _lastErrorCode = V4Adaptor.sCSCReaderStopEx(phRw);
            return (CSC_API_ERROR)_lastErrorCode;
        }

        static private int _lastErrorCode;
        static public int LastErrorCode { get { return _lastErrorCode; } }
        
        // make it available for WindowsCE  or MonoLinux, if needed
        
        public static CSC_API_ERROR PingReader(CSC_READER_TYPE cSC_READER_TYPE, int _hRw)
        {
#if !WindowsCE  && !MonoLinux
            short MINPINGSIZE = 7, MINPONGSIZE = 7;
            switch (cSC_READER_TYPE)
            {                
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = V4Adaptor.sCscPingEx(_hRw, MINPINGSIZE, MINPONGSIZE);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                case CSC_READER_TYPE.VIRTUAL_READER:
                    {
                        _lastErrorCode = CSCThalesVirtualReader.sCscPingEx(_hRw, MINPINGSIZE, MINPONGSIZE);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
#endif
                default:
                    throw new NotSupportedException(); // v3 doesn't offer pinging.
            }
#else
            throw new NotImplementedException();
#endif
        }

        public static CSC_API_ERROR StopField(CSC_READER_TYPE cSC_READER_TYPE, int _hRw)
        {
            switch (cSC_READER_TYPE)
            {
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.V3_READER:                    
                        _lastErrorCode = V4Adaptor.sSmartFieldEx(_hRw, (byte)0);
                        return (CSC_API_ERROR)_lastErrorCode; 
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                case CSC_READER_TYPE.VIRTUAL_READER:
                        _lastErrorCode = CSCThalesVirtualReader.sSmartFieldEx(_hRw, (byte)0);
                        return (CSC_API_ERROR)_lastErrorCode; 
#endif
                default:
                    return CSC_API_ERROR.ERR_NOT_AVAIL;
            }
        }
        public static CSC_API_ERROR StartField(CSC_READER_TYPE cSC_READER_TYPE, int _hRw)
        {
            switch (cSC_READER_TYPE)
            {
                case CSC_READER_TYPE.V4_READER:
                case CSC_READER_TYPE.V3_READER:
                    _lastErrorCode = V4Adaptor.sSmartFieldEx(_hRw, CONSTANT.FIELD_ON);
                    return (CSC_API_ERROR)_lastErrorCode;
#if !WindowsCE && !MonoLinux && !NoVirtualReader
                case CSC_READER_TYPE.VIRTUAL_READER:
                    _lastErrorCode = CSCThalesVirtualReader.sSmartFieldEx(_hRw, CONSTANT.FIELD_ON);
                    return (CSC_API_ERROR)_lastErrorCode;
#endif
                default:
                    return CSC_API_ERROR.ERR_API;
            }
        }
    }
}
