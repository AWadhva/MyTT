//#if !WindowsCE  && !MonoLinux
//        static internal CSC_API_ERROR InstallCardV3(int phRw,
//                                                    DEST_TYPE pDestType,
//                                                    InstallCard pInstCardParams)
//        {

//            IntPtr piInstCardParams = Marshal.AllocHGlobal(Marshal.SizeOf(pInstCardParams));
//            Marshal.StructureToPtr(pInstCardParams, piInstCardParams, false);

//            _lastErrorCode = V3Adaptor.sSmartInstCardEx(phRw, pDestType, piInstCardParams);
//            return (CSC_API_ERROR)_lastErrorCode;
//        }
//#endif

//#if !WindowsCE  && !MonoLinux
//        static internal CSC_API_ERROR IsoCommandV3(int phRw,
//                                                   DEST_TYPE pDestType,
//                                                   byte[] pCommandApdu,
//            int maxReattemptsInCaseOfErrData,
//                                                   out byte pSw1,
//                                                   out byte pSw2,
//                                                   out byte[] pResData)
//        {
//            int ERR_CODE = CONSTANT.IS_ERROR;

//            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
//            pSw1 = 0xFF;
//            pSw2 = 0xFF;

//            short DataLen = Convert.ToInt16(CONSTANT.MAX_ISO_DATA_OUT_LENGTH);

//            try
//            {
//                for (int i = 0; i < maxReattemptsInCaseOfErrData; i++)
//                {

//                    IntPtr piDataOut = Marshal.AllocHGlobal(CONSTANT.MAX_API_DATA_OUT_LENGTH);
//                    IntPtr piDataLen = Marshal.AllocHGlobal(DataLen);

//                    Marshal.WriteInt16(piDataLen, DataLen);

//                    ERR_CODE = V3Adaptor.sSmartISOEx(phRw, pDestType, Convert.ToInt16(pCommandApdu.Length), pCommandApdu, piDataLen, piDataOut);

//                    if (ERR_CODE == CONSTANT.NO_ERROR)
//                    {

//                        CFunctions.processApduRes(piDataOut,
//                                                  piDataLen,
//                                                  out pSw1,
//                                                  out pSw2,
//                                                  out pResData);
//                    }

//                    Marshal.FreeHGlobal(piDataOut);
//                    Marshal.FreeHGlobal(piDataLen);

//                    if (ERR_CODE != (short)CSC_API_ERROR.ERR_DATA)
//                        break;
//                }
//                return (CSC_API_ERROR)ERR_CODE;
//            }
//            catch (Exception )
//            {
//                Logging.Log(LogLevel.Error, "ReaderFunctions -> IsoCommandV3: Exception ..");

//                return (CSC_API_ERROR)ERR_CODE;
//            }
//        }
//#endif

//#if !WindowsCE  && !MonoLinux
//        static internal CSC_API_ERROR InitReaderV3(ReaderComm pReaderComm,
//                                                   out int phRw,
//                                                   out FirmwareInfo pFirmware)
//        {
//            int ERR_CODE = CONSTANT.IS_ERROR;

//            phRw = 0;

//            pFirmware.Chargeur = "XXX_NO_INFO_XXX";
//            pFirmware.AppCSC = "XXX_NO_INFO_XXX";
//            pFirmware.Fpga1 = "XXX_NO_INFO_XXX";
//            pFirmware.Fpga2 = "XXX_NO_INFO_XXX";

//            try
//            {
//                unsafe
//                {
//                    /* CSC Api Version check */
//                    IntPtr apiMajorVersion = new IntPtr(CONSTANT.API_EX_MAJOR_VERSION);
//                    IntPtr apiMinorVersion = new IntPtr(CONSTANT.API_EX_MINOR_VERSION);

//                    IntPtr piMajorVersion = Marshal.AllocHGlobal(sizeof(int)),
//                           piMinorVersion = Marshal.AllocHGlobal(sizeof(int));

//                    ERR_CODE = V3Adaptor.sCSCReaderGetVersionEx(piMajorVersion, piMinorVersion);

//                    if (ERR_CODE == CONSTANT.NO_ERROR)
//                    {
//                        int* MajorVerPtr = (int*)piMajorVersion.ToPointer();
//                        int* MinorVerPtr = (int*)piMinorVersion.ToPointer();

//                        int MajorVersion = *MajorVerPtr;
//                        int MinorVersion = *MinorVerPtr;

//                        Logging.Log(LogLevel.Information, "Versions Tahales CSC API " + Convert.ToString(MajorVersion) + "." + Convert.ToString(MinorVersion));
//                        //Check for minimum version of 3.11 CSC V3 API
//                        if (MajorVersion >= 3 || MinorVersion >= 11)
//                        {
//                            ERR_CODE = V3Adaptor.sCSCReaderStartEx(pReaderComm.COM_PORT, pReaderComm.COM_SPEED, out phRw);

//                            if (ERR_CODE == CONSTANT.NO_ERROR)
//                            {
//                                StatusCSC pStatusCSC;
//                                CSC_BOOTIDENT pOutFirmareName;

//                                ERR_CODE = V3Adaptor.sSmartStatusEx(phRw, out pStatusCSC);

//                                if (pStatusCSC.ucStatCSC != CONSTANT.ST_VIRGIN)
//                                {
//                                    ERR_CODE = V3Adaptor.sCscRebootEx(phRw);
//                                }

//                                ERR_CODE = V3Adaptor.sCscConfigEx(phRw, out pOutFirmareName);

//                                pFirmware.Chargeur = Convert.ToString(pOutFirmareName.ucBootLabel);
//                                pFirmware.AppCSC = Convert.ToString(pOutFirmareName.ucPrgLabel);
//                                pFirmware.Fpga1 = Convert.ToString(pOutFirmareName.ucFPGA1Label);
//                                pFirmware.Fpga2 = Convert.ToString(pOutFirmareName.ucFPGA2Label);

//                                Marshal.FreeHGlobal(piMajorVersion);
//                                Marshal.FreeHGlobal(piMinorVersion);
//                                Marshal.FreeHGlobal(apiMajorVersion);
//                                Marshal.FreeHGlobal(apiMinorVersion);

//                                return (CSC_API_ERROR)ERR_CODE;
//                            }
//                            else
//                            {
//                                Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV3: Device not Started");

//                                return CSC_API_ERROR.ERR_DEVICE;
//                            }
//                        }
//                        else
//                        {
//                            Logging.Log(LogLevel.Error, "ReaderFunctions -> InitReaderV3: Error API");

//                            return CSC_API_ERROR.ERR_API;
//                        }
//                    }

//                    return CSC_API_ERROR.ERR_DEVICE;
//                }
//            }
//            catch (Exception exp)
//            {
//                Logging.Log(LogLevel.Error, "ReaderFunctions -> Exceptions .." + exp.Message);
//                return (CSC_API_ERROR)ERR_CODE;
//            }
//        }
//#endif

//#if !WindowsCE  && !MonoLinux
//        static internal CSC_API_ERROR StopReaderV3(int phRw)
//        {
//            _lastErrorCode = V3Adaptor.sCSCReaderStopEx(phRw);
//            return (CSC_API_ERROR)_lastErrorCode;
//        }
//#endif
