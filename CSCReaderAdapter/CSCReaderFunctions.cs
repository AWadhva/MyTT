using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Threading;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.BackOffice.ThalesSecLibrary;

namespace IFS2.Equipment.TicketingRules
{
    public class CSCReaderFunctions
    {
        // private int _ReaderType;
        private int _hRw = -1;
        private ThalesReaderFunction _mThalesReaderFunction;
        private Utility.StatusListenerDelegate listenerCardProduced = null, listenerCardRemoved = null;

        CONSTANT.SAMType e_samType = CONSTANT.SAMType.NONE;
        CONSTANT.ReaderType e_readertype = CONSTANT.ReaderType.THALES_V4;
        DEST_TYPE e_DestType = DEST_TYPE.DEST_PICC_TRANSPARENT;
        public bool _IsNFCCardDetected = false;
        private bool _isSAMConfigured = false, _bReaderConnected = false;
        int samslot = 1;
        StatusCSC pStatusCSC;
        ReaderStatus mReaderStatus;
        //public enum MediaDetected { CARD, TOKEN, UNSUPPORTEDMEDIA, NONE };
        Scenario _activeScenario = Scenario.SCENARIO_1;
        private MifareSAMKeys mMifareSAMAccessKey;
        private MifareSAMKeys mMifareSAMKucKey;
        // readonly CSCReaderFunctions _sFunc = new CSCReaderFunctions();
        private MifareSAM mMifareSAM;
        private ulong _SrNbr;
        private CSC_API_ERROR _lastErrorCode;
        private byte[] key0 = { 0x5A, 0x17, 0xC4, 0xF8, 0x62, 0x5B, 0x52, 0x7E, 0x40, 0x79, 0x8A, 0x73, 0x34, 0xC7, 0xD9, 0xC6 };
        private byte[] kucKey = { 0x40, 0x30, 0x60, 0x10, 0x60, 0x90, 0x70, 0x20, 0x60, 0xF0, 0x40, 0xB0, 0x50, 0x50, 0x40, 0x00 };
        public CSCReaderFunctions()
        {
            e_samType = CONSTANT.SAMType.NXP_SAM_AV2;
            // e_samType = (CONSTANT.SAMType)(Configuration.ReadParameter("SAMType", "byte", "I"));
            e_readertype = CONSTANT.ReaderType.THALES_V4;// (CONSTANT.ReaderType)(Configuration.ReadParameter("CSCReaderType", "byte", "2"));


            mReaderStatus = new ReaderStatus();
            mReaderStatus.ReaderState = 0x00;
            mReaderStatus.xCardType = (int)CSC_TYPE.CARD_NONE;
            for (int b = 0; b < mReaderStatus.CardId.Length; b++) mReaderStatus.CardId[b] = 0;
            _mThalesReaderFunction = null;

            if ((bool)Configuration.ReadParameter("UseSAMDefaultKeys", "bool", "true")==false)
            {
                mMifareSAMKucKey = new MifareSAMKeys();
                mMifareSAMAccessKey = new MifareSAMKeys();

                mMifareSAMKucKey.keyVersion = 0x00;
                mMifareSAMKucKey.keyNum = 121;

                mMifareSAMAccessKey.keyVersion = 0x00;
                mMifareSAMAccessKey.keyNum = 0x00;

                byte[] encryptingKey = Configuration.ReadEncryptedTabByteParameter("SAMEncryptionKey", "");
                ThalesSecLibrary.Initialisation(Disk.BaseCodeDirectory + "\\Conf\\IFS2Conf_EncryptionFile.pfx", 0, 1, 32, encryptingKey);
                ThalesSecLibrary.Initialisation(Disk.BaseCodeDirectory + "\\Conf\\IFS2Conf_EncryptionFile.pfx", 1, 2, 16, encryptingKey);
                ThalesSecLibrary.Initialisation(Disk.BaseCodeDirectory + "\\Conf\\IFS2Conf_EncryptionFile.pfx", 2, 3, 32, encryptingKey);
                ThalesSecLibrary.Initialisation(Disk.BaseCodeDirectory + "\\Conf\\IFS2Conf_EncryptionFile.pfx", 3, 4, 16, encryptingKey);

                string s4 = Configuration.ReadEncryptedParameter("SAMKey", "");
                byte[] _samKey = Convert.FromBase64String(s4);
                byte[] samkey = ThalesSecLibrary.DecryptAES(_samKey, 1, 16);

                int samkeyVer = (int)Configuration.ReadParameter("SAMKeyVer", "int", "0");
                int samkeyNum = (int)Configuration.ReadParameter("SAMKeyNum", "int", "0");
                {
                    mMifareSAMAccessKey.keyVersion = (byte)samkeyVer;
                    mMifareSAMAccessKey.keyNum = (byte)samkeyNum;
                    if (samkey.Length > 0)
                    {
                       mMifareSAMAccessKey.key= samkey;
                    }
                }

                string s5 = Configuration.ReadEncryptedParameter("SAMKucKey", "");
                byte[] _samKucKey = Convert.FromBase64String(s5);
                byte[] samkuckey = ThalesSecLibrary.DecryptAES(_samKucKey, 3, 16);
                int samkuckeyVer = (int)Configuration.ReadParameter("SAMKucKeyVer", "int", "0");
                int samkuckeyNum = (int)Configuration.ReadParameter("SAMKucKeyNum", "int", "121");//121 is KUC key number for authentication
                {
                    mMifareSAMKucKey.keyVersion = (byte)samkeyVer;
                    mMifareSAMKucKey.keyNum = (byte)samkuckeyNum;
                    if (samkuckey.Length > 0)
                    {
                        mMifareSAMKucKey.key = samkuckey;
                    }
                }


            }
        }

        /*
        public static CSCReaderFunctions Instance
        {
            get
            {
                return _sFunc;
            }
        }
        */
        public bool Init(int readertype, string readerport, int samtype, int psamslot, out FirmwareInfo mFirmwareInfo)
        {
            int ret = -1;
            _isSAMConfigured=false;
            mFirmwareInfo = new FirmwareInfo();
            samslot = psamslot;
            _mThalesReaderFunction = new ThalesReaderFunction((CONSTANT.SAMType)samtype, samslot, (CSC_READER_TYPE)readertype, readerport);
            _bReaderConnected = _mThalesReaderFunction.ReloadReader();
            if (_bReaderConnected)
            {
                mFirmwareInfo = _mThalesReaderFunction.GetReaderFirmwareData();
                ret =SM_Init();// SM_Init(key0);  
                if(ret==0)
                {
                    _isSAMConfigured=true;                   
                }
            }          
            return _bReaderConnected ;
        }
        public bool RestartReader(out FirmwareInfo mFirmwareInfo)
        {
            int ret = -1;
            _isSAMConfigured = false;
            mFirmwareInfo = new FirmwareInfo();
            _bReaderConnected = _mThalesReaderFunction.ReloadReader();
            if (_bReaderConnected)
            {
                mFirmwareInfo = _mThalesReaderFunction.GetReaderFirmwareData();
                ret = SM_Init();// SM_Init(key0);
                if (ret == 0)
                {
                    _isSAMConfigured = true;
                }
            }
            else
            {
                //throw GetExceptionForCode((CSC_API_ERROR)_mThalesReaderFunction.LastErrorCode, _hRw);
            }
            return _bReaderConnected;
        }

        public bool RestartReader(byte[] sam_key)
        {
            int ret = -1;
            _isSAMConfigured = false;
           
            _bReaderConnected = _mThalesReaderFunction.ReloadReader();
            if (_bReaderConnected)
            {
                ret= SM_Init(sam_key);
                if (ret == 0)
                {
                    _isSAMConfigured = true;
                }
            }
            else
            {
                // throw GetExceptionForCode((CSC_API_ERROR)_mThalesReaderFunction.LastErrorCode, _hRw);
            }
            return _bReaderConnected;
        }        
        private void ClearStatus()
        {
            //CSCReaderFunctions.pStatusCSC.ucAntenna = 0x00;
            for (int b = 0; b < mReaderStatus.CardId.Length; b++) mReaderStatus.CardId[b] = 0;
            mReaderStatus.ReaderState = 0x00;
            mReaderStatus.xCardType = (int)CSC_TYPE.CARD_NONE;
        }

        public ulong GetSmartMediaUID()
        {
            return _SrNbr;
        }
        /// <summary>
        /// Function to Synchronize the Card information with the detected
        /// Card
        /// would let the ReaderException flow outside.
        /// Only practical exception that can be thrown is ErrorDevice
        /// </summary>
        /// <returns></returns>
        public void SmartSyncDetectOk(out MediaTypeDetected detectionState,
            out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            out int statusCSC, CSC_READER_TYPE ReaderType, Scenario scenario
            )
        {
            detectionState = MediaTypeDetected.NONE;
            statusCSC = -1;
            bSameMedia = false;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;
        }
        /// <summary>
        /// Function to Synchronize the Card information with the detected
        /// Card
        /// </summary>
        /// <returns></returns>
        public void SmartSyncDetectOk(out MediaTypeDetected detectionState,
            out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            bool bUseAsynch)
        {

            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            detectionState = MediaTypeDetected.NONE;
            bSameMedia = false;
            try
            {
                StatusCSC pStatusCSC = new StatusCSC();
                ClearStatus();
                Err = _mThalesReaderFunction.StatusCheck((CSC_READER_TYPE)e_readertype, ref pStatusCSC);
                if (Err != CSC_API_ERROR.ERR_NONE)
                    return;
                Logging.Log(LogLevel.Verbose, "SmartSyncDetectOk " + pStatusCSC.ucStatCSC.ToString("X2") + "|" + pStatusCSC.ucNbDetectedCard.ToString() + "|" + pStatusCSC.ucLgATR.ToString() + "|" + pStatusCSC.ucAntenna.ToString());

                switch (pStatusCSC.ucStatCSC)
                {
                    case CONSTANT.ST_CARDON:
                        {
                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                            if (detectionState == MediaTypeDetected.NONE)
                            {
                                HaltCard();
                                StartPollingEx(Scenario.SCENARIO_1, bUseAsynch ? listenerCardProduced : null);
                            }
                            break;
                        }
                        break;
                    case CONSTANT.ST_INIT:
                        {
                            StartPollingEx();
                            Thread.Sleep(40);
                            Err = _mThalesReaderFunction.StatusCheck((CSC_READER_TYPE)e_readertype, ref pStatusCSC);
                            switch (pStatusCSC.ucStatCSC)
                            {
                                case CONSTANT.ST_CARDON:
                                    {
                                        DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                                        if (detectionState == MediaTypeDetected.NONE)
                                        {
                                            HaltCard();
                                            StartPollingEx(Scenario.SCENARIO_1, bUseAsynch ? listenerCardProduced : null);
                                        }
                                        break;
                                    }
                            }
                        }
                        break;
                    case CONSTANT.ST_POLLON:
                        break;
                    case CONSTANT.ST_DETECT_REMOVAL:
                        {
                            DetectTypeOfMediaNExtractSerialNumbers(ref pStatusCSC, out detectionState);
                            bSameMedia = true;
                        }
                        break;
                    default:
                        {
                            return;
                        }

                }//switch (pStatusCSC.ucStatCSC)

            }
            catch (Exception ex)
            {
            }

        }

        public void SmartSyncDetectOk(out MediaTypeDetected detectionState,
           out bool bSameMedia, // to be used selectivly only when detectionState != NONE
           bool bUseAsynch,
           Scenario scenario
           )
        {

            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            detectionState = MediaTypeDetected.NONE;
            bSameMedia = false;
        }
        public CSC_API_ERROR HaltCard()
        {
            return _mThalesReaderFunction.HaltCard();
        }
        public void DetectTypeOfMediaNExtractSerialNumbers(ref StatusCSC pStatusCSC, out MediaTypeDetected detectionState)
        {
            detectionState = MediaTypeDetected.NONE;
            if (pStatusCSC.xCardType == (int)CSC_TYPE.CARD_MIFARE1 && pStatusCSC.ucLgATR == 12)
            {
                ExtractFrom_StatusCSC(ref pStatusCSC, out detectionState, out _SrNbr);

                return;
            }
        }
        public void ExtractFrom_StatusCSC(ref StatusCSC pStatusCSC, out MediaTypeDetected detectionState, out ulong SrNbr)
        {
            byte[] serialNbrBytes = new byte[8];
            byte[] ba = pStatusCSC.ucATR;
            detectionState = MediaTypeDetected.NONE;
            _IsNFCCardDetected = false;
            byte SAK = ba[2];
            var typ = ((SAK >> 3) & 0x7);
            if (typ == 0)
            {
                // ultralight
                detectionState = MediaTypeDetected.TOKEN;
                Array.Copy(ba, 3, serialNbrBytes, 0, 7);
            }
            else if (typ == 4)
            {
                // desfilre
                detectionState = MediaTypeDetected.CARD;
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
                    detectionState = MediaTypeDetected.CARD;
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
            }
            else
            {
                // some other card of MiFare family, other than Ultralight and DESFire
                detectionState = MediaTypeDetected.UNSUPPORTEDMEDIA;
            }

            SrNbr = 0;
            for (int i = 0; i < 7; i++)
            {
                SrNbr *= 256;
                SrNbr += serialNbrBytes[i];
            }
        }
        public void StartField()
        {
            switch (e_readertype)
            {
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    {
                        CSC_API_ERROR err = _mThalesReaderFunction.StartField((CSC_READER_TYPE)e_readertype);
                        if (err != CSC_API_ERROR.ERR_NONE && err != CSC_API_ERROR.ERR_NOEXEC)
                            throw GetExceptionForCode(err, -1);
                    }
                    break;
                default:
                    break;
            }
        }
        public CSC_API_ERROR PingReader()
        {
#if !WindowsCE  && !MonoLinux

            switch ((CSC_READER_TYPE)e_readertype)
            {
                case CSC_READER_TYPE.V4_READER:
                    {
                        _lastErrorCode = _mThalesReaderFunction.PingReader((CSC_READER_TYPE)e_readertype);
                        return (CSC_API_ERROR)_lastErrorCode;
                    }
                default:
                    throw new NotSupportedException(); // v3 doesn't offer pinging.
            }
#else
            throw new NotImplementedException();
#endif
        }
        public void StopField()
        {
            switch (e_readertype)
            {
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    {
                        // This command always stops field, and shifts R/W to ST_INIT, irrespective of which state the R/W is currently in
                        CSC_API_ERROR err = _mThalesReaderFunction.StopField((CSC_READER_TYPE)e_readertype);
                        if (err != CSC_API_ERROR.ERR_NONE)
                            throw GetExceptionForCode(err, _hRw);
                    }
                    break;
                default: //ERR_DEVICE
                    throw GetExceptionForCode(CSC_API_ERROR.ERR_DEVICE, _hRw);
                    break;
            }
        }
        public void StartPollingEx()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            switch (e_readertype)
            {
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    {
                        Err = _mThalesReaderFunction.StartPolling((CSC_READER_TYPE)e_readertype,
                                                   (byte)_activeScenario, listenerCardProduced
                        );
                    }
                    break;
                default:
                    break;
            }

            if (Err == CSC_API_ERROR.ERR_NONE)
            {

                return;
            }
            else
                throw GetExceptionForCode(Err, _hRw);
        }
        public void StartPollingEx(Scenario scenario)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            switch (e_readertype)
            {
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    {
                        Err = _mThalesReaderFunction.StartPolling((CSC_READER_TYPE)e_readertype,
                                                   (byte)scenario, listenerCardProduced
                        );
                    }
                    break;
                default:
                    break;
            }

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                _activeScenario = scenario;
                return;
            }
            else
                throw GetExceptionForCode(Err, _hRw);
        }
        public void StartPollingEx(Scenario scenario, Utility.StatusListenerDelegate listener)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            switch (e_readertype)
            {
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    {
                        Err = _mThalesReaderFunction.StartPolling((CSC_READER_TYPE)e_readertype,
                                                   (byte)scenario, listener
                        );
                    }
                    break;
                default:
                    break;
            }

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                _activeScenario = scenario;
                return;
            }
            else
                throw GetExceptionForCode(Err, _hRw);
        }
        public void StopPolling()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            switch (e_readertype)
            {
                case CONSTANT.ReaderType.BIP_1500:
#if _HHD_ && _BLUEBIRD_
                    Err = (CSC_API_ERROR)Reader.StopPolling();
#endif
                    break;
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    // If Reader is in ST_INIT, even then it returns ERR_NONE
                    // If Reader is in ST_CARDON, even then it returns ERR_NONE
                    // Even when field is OFF, it returns ERR_NONE
                    Err = _mThalesReaderFunction.StopPolling((CSC_READER_TYPE)e_readertype);
                    break;
                default:
                    break;
            }

            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, _hRw);
        }

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
        public void Registerlistiner(int listertype, Utility.StatusListenerDelegate mlistenerCard)
        {
            if (listertype == 0)//carddetection
            {
                listenerCardProduced = mlistenerCard;
            }
            else if (listertype > 0)
            {
                listenerCardRemoved = mlistenerCard;
            }
        }
        public bool SwitchToDetectRemovalState()
        {
            return (_mThalesReaderFunction.SwitchToDetectRemovalState((CSC_READER_TYPE)e_readertype, listenerCardRemoved) == CSC_API_ERROR.ERR_NONE);
        }
        public CSC_API_ERROR SwitchToCardOnState()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NONE;
#if _HHD_ && _BLUEBIRD_
            return CSC_API_ERROR.ERR_NONE;
#else
            // when in ST_INIT, it returns ERR_NOEXEC
            Err= _mThalesReaderFunction.SwitchToCardOnState((CSC_READER_TYPE)e_readertype);

            if (Err != CSC_API_ERROR.ERR_NONE)
                throw GetExceptionForCode(Err, _hRw);

            return Err;
#endif
        }
       
        public int ISOCommand(DEST_TYPE dest, byte[] isoapdu, out byte[] response, out byte pSw1, out byte pSw2)
        {
            //int ret = -1;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOT_AVAIL;
            pSw1 = 0xff;
            pSw2 = 0xff;
            response = null;
            switch (e_readertype)
            {
                case CONSTANT.ReaderType.THALES_V3:
                case CONSTANT.ReaderType.THALES_V4:
                    {
                        Err = _mThalesReaderFunction.IsoCommand(e_DestType, isoapdu, out pSw1, out pSw2, out response);
                        //if (Err != CSC_API_ERROR.ERR_NONE)
                        //    throw GetExceptionForCode(Err, _hRw);
                    }
                    break;
                default:
                    break;
            }//Switch

            return (int)Err;

        }

        public bool UpdateReaderFirmware(string sfilePath)
        {
            return _mThalesReaderFunction.CheckandUpdateReaderFirmware(sfilePath);
        }

        #region "Mifare SAM AV1,AV2"
        private bool _SAMActivated = false;
        public NXP_SAM_Info mSAMVerInfo;
        public bool IsSAMActivate()
        {
            return _SAMActivated;
        }
        private int SM_Init(byte[] samkey)
        {
            int r = -1;
            bool ret = false;
            _SAMActivated = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
            {
                mMifareSAM = new MifareSAM(_mThalesReaderFunction, (int)e_samType, 0, samslot);

                if (mMifareSAM != null)
                {
                    r = _mThalesReaderFunction.InstallCardSAM((DEST_TYPE)samslot);
                    if (r == 0)
                    {
                        ret = mMifareSAM.SAM_GetVersion(out mSAMVerInfo, out pSw1, out pSw2);
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = mMifareSAM.ActivateSAM(samkey, 0x00, 0x00, 0x00, out pSw1, out pSw2);
                        _SAMActivated = ret;
                        if (ret) r = 0;
                        else r = -1;
                    }
                }//if (mMifareSAM != null)
            }
            return r;
        }
        private int SM_Init()
        {
            int r = -1;
            bool ret = false;
            _SAMActivated = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
            {
                mMifareSAM = new MifareSAM(_mThalesReaderFunction, (int)e_samType, 0, samslot);

                if (mMifareSAM != null)
                {
                    r = _mThalesReaderFunction.InstallCardSAM((DEST_TYPE)samslot);
                    if (r == 0)
                    {
                        ret = mMifareSAM.SAM_GetVersion(out mSAMVerInfo, out pSw1, out pSw2);
                        pSw1 = 0xff; pSw2 = 0xff;
                        if ((mMifareSAMAccessKey != null) && !(bool)Configuration.ReadParameter("UseSAMDefaultKeys", "bool", "true"))
                        {
                            ret = mMifareSAM.ActivateSAM(mMifareSAMAccessKey.key, 0x00, mMifareSAMAccessKey.keyNum, mMifareSAMAccessKey.keyVersion, out pSw1, out pSw2);
                        }
                        else
                            ret = mMifareSAM.ActivateSAM(key0, 0x00, 0x00, 0x00, out pSw1, out pSw2);
                        _SAMActivated = ret;
                        if (ret) r = 0;
                        else r = -1;
                    }
                }//if (mMifareSAM != null)
            }
            return r;
        }
        public bool SM_AuthenticateHost(byte[] key, bool isKucKey,byte bKeynum, byte bKeyVer, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte[] samkey = new byte[16];
            pSw1 = 0xff; pSw2 = 0xff;
            if (isKucKey)
            {                
                if (key != null && key.Length==16 )
                {
                    Array.Copy(key, 0, samkey, 0, samkey.Length);

                }
                else
                {
                    Array.Copy(kucKey, 0, samkey, 0, samkey.Length);
                }
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    if (_SAMActivated)
                    {
                        ret = mMifareSAM.AuthenicateSAMwithKUC(samkey, 0x00, bKeynum, bKeyVer, out pSw1, out pSw2);
                    }
                }
            }
            return ret;
        }
        public bool SM_ChangeKUCEntry(byte KeyNumKUCEntry, bool bupdateLimit, bool bUpdateKeyNoKUC, bool bupdatKeyVersion, byte[] dataIn, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xff; pSw2 = 0xff;
            if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
            {
                if (_SAMActivated)
                {
                    if ((mMifareSAMKucKey != null)&& !(bool)Configuration.ReadParameter("UseSAMDefaultKeys", "bool", "true"))
                    {
                        ret = SM_AuthenticateHost(mMifareSAMKucKey.key, true, mMifareSAMKucKey.keyNum, mMifareSAMKucKey.keyVersion, out pSw1, out pSw2);
                    }
                    else
                        ret = SM_AuthenticateHost(kucKey, true, 121, 0x00, out pSw1, out pSw2);
                    if (ret)
                    {
                        ret = false;
                        mMifareSAM.SAM_ChangeKUCEntry(KeyNumKUCEntry, bupdateLimit, bUpdateKeyNoKUC, bupdatKeyVersion, dataIn, out pSw1, out pSw2);
                        if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }
        public string SM_CheckAuthFailure(byte pSw1, byte pSw2, out bool IsKUCLimitReached)
        {
            IsKUCLimitReached = false;
            return mMifareSAM.SAM_CheckAuthFailureResponse(pSw1,pSw2,out IsKUCLimitReached);
        }
        public bool SM_GetKUCEntry(byte bKeyNum, out long msamquota, out long samcurrVal, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte []response;
            msamquota = 0;
            samcurrVal = 0;
            pSw1 = 0xff; pSw2 = 0xff;
            if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
            {
                if (_SAMActivated)
                {
                    mMifareSAM.SAM_GetKUCEntry(bKeyNum, out response, out pSw1, out pSw2);
                    if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                    {
                        msamquota = response[0];
                        msamquota |= (response[1] << 8);
                        msamquota |= (response[2] << 16);
                        msamquota |= (response[3] << 24);

                        samcurrVal = response[6];
                        samcurrVal |= (response[7] << 8);
                        samcurrVal |= (response[8] << 16);
                        samcurrVal |= (response[9] << 24);

                        ret = true;
                    }
                }
            }
            return ret;
        }
        public bool SM_AuthenticatePICC_Step1(byte authmode, byte[] bRndB_crpt, byte keyEntry, byte keyver, byte[] bdivInp, out byte[] en_RanA_RndB, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            // byte keyNum = keyEntry;// call get key entry to get the key number from SAM 
            en_RanA_RndB = null;
            pSw1 = 0xff; pSw2 = 0xff;
            if (_SAMActivated)
            {
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    if (mMifareSAM != null)
                    {
                        // byte[] keydetails;
                        //  
                        //  

                        if (mMifareSAM._isSAMActivated)
                        {
                            mMifareSAM.SAM_KillAuthentication(out pSw1, out pSw2);
                            pSw1 = 0xff; pSw2 = 0xff;
                            ret = mMifareSAM.SAM_AuthenticatePICC_Step1(authmode, bRndB_crpt, keyEntry, keyver, bdivInp, out en_RanA_RndB, out pSw1, out pSw2);
                        }

                    }
                }//if nXP SAM
            }//if (_SAMActivated)
            return ret;
        }
        public bool SM_AuthenticatePICC_Step2(byte[] ciphered_RndA_dash, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xff;
            pSw2 = 0xff;
            if (_SAMActivated)
            {
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    if (mMifareSAM != null && mMifareSAM._isSAMActivated)
                    {
                        ret = mMifareSAM.SAM_AuthenticatePICC_Step2(ciphered_RndA_dash, out pSw1, out pSw2);
                        if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }
        public bool SM_GetKeyEntry(byte KeyEntryNum, out int appId_Df, out byte keyNum)
        {
            bool ret = false;
            byte[] keydetails;
            byte pSw1 = 0xff; byte pSw2 = 0xff;
            keyNum = 0xff;
            appId_Df = 0;
            if (_SAMActivated)
            {
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    ret = mMifareSAM.SAM_GetKeyEntry(KeyEntryNum, out keydetails, out pSw1, out pSw2);
                    if (pSw1 == 0x90 && pSw2 == 0x00)
                    {
                        keyNum = keydetails[6];
                        appId_Df = keydetails[3];
                        appId_Df <<= 8;
                        appId_Df |= keydetails[4];
                        appId_Df <<= 8;
                        appId_Df |= keydetails[5];
                    }
                }// if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
            }//if (_SAMActivated)
            return ret;
        }
        public bool SM_EncryptData(byte[] datain, out byte[] outEnData, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            outEnData = new byte[1];
            pSw1 = 0xff; pSw2 = 0xff;

            if (_SAMActivated)
            {
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    if (mMifareSAM._isSAMActivated)
                    {
                        ret = mMifareSAM.SAM_EncryptData(datain, out outEnData, out pSw1, out pSw2);
                    }
                }
            }
            return ret;
        }
        public bool SM_ChangeKey(byte keyConfMethod, byte oldkeyEntry, byte oldKeyver, byte newKeyEntry, byte newkeyver, byte cardKeyNum, byte[] DivIn, out byte[] cryptogram, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            cryptogram = new byte[1];
            pSw1 = 0xff; pSw2 = 0xff;

            if (_SAMActivated)
            {
                if (e_samType == CONSTANT.SAMType.NXP_SAM_AV1 || e_samType == CONSTANT.SAMType.NXP_SAM_AV2)
                {
                    if (mMifareSAM._isSAMActivated)
                    {
                        ret = mMifareSAM.SAM_ChangeKey(keyConfMethod, oldkeyEntry, oldKeyver, newKeyEntry, newkeyver, cardKeyNum, DivIn, out cryptogram, out pSw1, out pSw2);
                    }
                }
            }
            return ret;
        }
        #endregion

    }
}
