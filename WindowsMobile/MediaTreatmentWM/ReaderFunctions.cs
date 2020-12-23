#define _RW_TIME_CHECK 

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using IFS2.Equipment.CSCReaderAdaptor;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Threading;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;


namespace IFS2.Equipment.CSCReader
{
    public class Reader
    {
#if _RW_TIME_CHECK
        private static DateTime _start;
        private static int _startTick;
#endif
        private static bool _delhiCCHSSAMUsage = false;
        private static bool _cryptoflexSAMUsage = true;
        static bool m_bIsOpen, SAMConfigured;
        static RFIDReader m_pRFReader;
        static Desfire mDesfire;
        static CCHSSAM mCCHSSAMManager;
        private static byte _lastFileAccesed, _lastSelectedApplication,_lastKeyUsed;
        static bool _IsSessionKeyCreatedPrevously;
        static byte[] _oldcardId = new byte[8];
        static Desfire.E_ReaderState mReaderState = Desfire.E_ReaderState.NONE;
        static private int _lastErrorCode;
        static public int LastErrorCode { get { return _lastErrorCode; } }
        static private bool _isProductionSAM = false;
        static Reader()
        {
            #if _RW_TIME_CHECK
            _start = DateTime.Now;
            _startTick = Environment.TickCount;
            #endif
            _delhiCCHSSAMUsage = (bool)Configuration.ReadParameter("DelhiCCHSSAMUsage", "bool", "false");
            _cryptoflexSAMUsage = (bool)Configuration.ReadParameter("CryptoflexSAMUsage", "bool", "true");
           _isProductionSAM = (bool)Configuration.ReadParameter("IsProductionSAM", "bool", "false");
            m_bIsOpen = false;
            m_pRFReader =  new RFIDReader();
            mDesfire = new Desfire(ref m_pRFReader);
            mCCHSSAMManager = new CCHSSAM(ref m_pRFReader);
            _lastFileAccesed = 0x00;
            SAMConfigured = false;
            _lastSelectedApplication = 0x00;
            _lastKeyUsed = 0x00;
            _IsSessionKeyCreatedPrevously = false;
            Array.Clear(_oldcardId, 0, _oldcardId.Length);
            mReaderState = Desfire.E_ReaderState.NONE;
          
        }
#if _RW_TIME_CHECK
        static public DateTime GetTimeStamp()
        {
            return _start.AddMilliseconds(Environment.TickCount - _startTick);
        }
#endif
        static public CSC_API_ERROR ReloadReader(CSC_READER_TYPE pReaderType,
                                                ReaderComm pReaderComm,                                                
                                                out FirmwareInfo pFirmware)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            pFirmware.Chargeur = "NA";
            pFirmware.AppCSC = "NA";
            pFirmware.Fpga1 = "NA";
            pFirmware.Fpga2 = "NA";
            _lastFileAccesed = 0x00;
            _lastSelectedApplication = 0x00;
            _lastKeyUsed = 0x00;
            _IsSessionKeyCreatedPrevously = false;
            Array.Clear(_oldcardId, 0, _oldcardId.Length);
#if _BLUEBIRD_
            if (m_bIsOpen)
            {
                m_pRFReader.CloseReader();
                m_pRFReader.CloseComm();
                m_bIsOpen = false;
            }
            if (!m_bIsOpen)
            {
                if (m_pRFReader.OpenComm(pReaderComm.COM_PORT, 0, /*9600*/(uint) pReaderComm.COM_SPEED, 1)
                   && m_pRFReader.OpenReader(1, 0))
                {
                    Thread.Sleep(200);
                    m_bIsOpen = true;
                    if (mDesfire.ResetFieldEx())//Err= CSC_API_ERROR.ERR_NONE;//// Reset Reader and configure for IS)14443A
                    {
                 //      SAMConfigured= mCCHSSAMManager.ConfigureSAM(0, true,(byte) CONSTANT.CCHSSAMType.PSAM);
                  //     if (SAMConfigured) Err = CSC_API_ERROR.ERR_NONE;
                       mReaderState = mDesfire.GetReaderState();
                       Err = CSC_API_ERROR.ERR_NONE;
                    }
                    //else Err = CSC_API_ERROR.ERR_API;
                }
            }
#endif
            return Err;
        }
        static public CSC_API_ERROR ConfigureSAM(byte mSamType, int SlotNo)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            SAMConfigured = false;
            SAMConfigured = mCCHSSAMManager.ConfigureSAM(SlotNo, true, mSamType, _isProductionSAM);
            if (SAMConfigured) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public int GetSAMSequence()
        {
            return mCCHSSAMManager.GetSAMSequence();
        }
        static public int GetSAMId()
        {
            return mCCHSSAMManager.GetSAMId();
        }
        static public cCCHSSAMInfo GetSAMInfo()
        {
            cCCHSSAMInfo mcCCHSSAMInfo = new cCCHSSAMInfo();
            mcCCHSSAMInfo.SAMAppVersion = mCCHSSAMManager.GetSAMInfo().SAMAppVersion;
            mcCCHSSAMInfo.SAMType = (CONSTANT.SAMType) mCCHSSAMManager.GetSAMInfo().SAMType;
            mcCCHSSAMInfo.ServiceProvider = mCCHSSAMManager.GetSAMInfo().ServiceProvider;

            return mcCCHSSAMInfo;
        }
        static internal bool GetReaderVersion(out string version)
        {
            bool ret = false;
            version = "NA";

            return ret;
        }
        static public CSC_API_ERROR InstallCard(CSC_READER_TYPE pReaderType,
                                               int phRw,
                                               DEST_TYPE pDestType,
                                               InstallCard pInstCardParams)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NONE;

            //Err = mDesfire.ResetField() == true? 0 : -1;
            _lastErrorCode= (int) Err;
            return Err;

        }

        static public CSC_API_ERROR ConfigureForPolling( )
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API; 
            if (mDesfire.ResetFieldEx())
                Err = CSC_API_ERROR.ERR_NONE;

            _IsSessionKeyCreatedPrevously = false;
            _lastFileAccesed = 0x00;
            _lastKeyUsed = 0x00;
            _lastSelectedApplication = 0x00;
            _lastErrorCode =(int) Err;
            return Err;

        }

        static public CSC_API_ERROR HaltCard()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API; 
#if _BIP1300_
            if (mDesfire.HaltCardA()) Err = CSC_API_ERROR.ERR_NONE;
#elif _BIP1500_
            if (mDesfire.ResetFieldEx()) Err = CSC_API_ERROR.ERR_NONE;
#endif
            _IsSessionKeyCreatedPrevously = false;
            _lastFileAccesed = 0x00;
            _lastKeyUsed = 0x00;
            _lastSelectedApplication = 0x00;
            _lastErrorCode =(int) Err;
            return Err;

        }
        static public bool  StartPolling()
        {
#if _BIP1300_
            return mDesfire.ActivateReaderForDetection();// reconfigure for Poll casd A
#elif _BIP1500_
            return true;
#else
            return false;
#endif
        }
        static public bool ReStartPolling()
        {
            bool ret = false;
            _IsSessionKeyCreatedPrevously = false;
            _lastFileAccesed = 0x00;
            _lastKeyUsed = 0x00;
            _lastSelectedApplication = 0x00;
            ret = mDesfire.ResetFieldEx();
            return ret;
        }
        static public bool SwitchToDetectionRemoval()
        {
            return mDesfire.SwitchToDetectionRemoval();
        }
        static public bool SwitchToCardOn()
        {
            return mDesfire.SwitchToCardOn();
        }
        static public bool SwitchToPollOn()
        {
            return mDesfire.SwitchToPollOn();
        }
        static public CSC_API_ERROR StatusCheck(CSC_READER_TYPE pReaderType,
                                               int phRw,
                                               ref StatusCSC pStatusCSC)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte[] uid, uAtr;
            int MediaDetected = 0;
           
            //TODO : code for detecting the card..
      #if _BIP1300_
             uid = new byte[7];
            ret = mDesfire.DetectCardEx(out uAtr, out MediaDetected);
            if (ret)
            {
                Array.Copy(uAtr, 2, uid, 0, uid.Length);
               
                switch (mDesfire.GetReaderState())
                {
                   
                    case Desfire.E_ReaderState.READY_POLLON:

                        ret = mDesfire.SelectCard(uid);

                        if (ret)
                        {
                            mDesfire.SetReaderState(Desfire.E_ReaderState.ACTIVE_CARDON);
                            pStatusCSC.ucStatCSC = 0x10;// CardOn status
                        }
                        else
                            pStatusCSC.ucStatCSC = 0x04;// PollON status
                        break;

                    case Desfire.E_ReaderState.REMOVAL_DETECTION:
                        ret = mDesfire.SelectCard(uid);
                        pStatusCSC.ucStatCSC = 0x20;// REMOVAL_DETECTION status
                        break;
                    case Desfire.E_ReaderState.ACTIVE_CARDON:
                        pStatusCSC.ucStatCSC = 0x10;// CardOn status
                        //do not select the card... send old data...
                        break;

                    case Desfire.E_ReaderState.HALT:
                    default://TODO : to be checked at run time
                        ret = mDesfire.SelectCard(uid);
                        break;
                }

                pStatusCSC.ucATR = new byte[128];
                Array.Copy(uAtr, pStatusCSC.ucATR, uAtr.Length);
                pStatusCSC.ucNbDetectedCard = 1;
                pStatusCSC.xCardType = 3;//Mifare Card
               
            }
            else
            {
                //No media ... Reset the RF field to detect new card...
                mDesfire.ResetFieldEx();
            }
#elif _BIP1500_
        /*    if (_isCardSelected == false)
            {
                ret = mDesfire.DetectCardEx(out uAtr, out MediaDetected);
                if (ret)
                {
                   ret= mDesfire.SelectCard_ForProcessing(0x00);
                   if (ret)
                   {
                       _isCardSelected = true;
                       pStatusCSC.ucATR = new byte[128];
                       Array.Copy(uAtr, pStatusCSC.ucATR, uAtr.Length);
                       pStatusCSC.ucNbDetectedCard = 1;
                       pStatusCSC.xCardType = 3;//Mifare Card
                   }
                }

                if (ret == false)
                {
                    mDesfire.ResetFieldEx();
                    _isCardSelected = false;
                }
            }
            if(_isCardSelected)
            {
                //TODO: 
                _isCardSelected = mDesfire.GetVersion(out uid);
                if(_isCardSelected)
                {
                    pStatusCSC.ucATR = new byte[128];
                    Array.Copy(mDesfire.mATQA, 0, pStatusCSC.ucATR, 0, 2);
                    Array.Copy(uid, 0, pStatusCSC.ucATR, 2, uid.Length);
                    pStatusCSC.ucNbDetectedCard = 1;
                    pStatusCSC.xCardType = 3;//Mifare Card
                }

            }*/
            switch (mDesfire.GetReaderState())
            {
                case Desfire.E_ReaderState.IDLE:
                   // ret = mDesfire.ActivateReaderForDetection();
                   // break;
                case Desfire.E_ReaderState.READY_POLLON:// Fresh Poll for a card 
                    ret = mDesfire.DetectCardEx(out uAtr, out MediaDetected);
                    if (ret)
                    {
                        ret = mDesfire.SelectCard_ForProcessing(0x00);
                        if (ret)
                        {
                            pStatusCSC.ucStatCSC = 0x10;// CardOn status
                           // _isCardSelected = true;
                            pStatusCSC.ucATR = new byte[128];
                            Array.Copy(uAtr, pStatusCSC.ucATR, uAtr.Length);
                            pStatusCSC.ucNbDetectedCard = 1;
                            pStatusCSC.xCardType = 3;//Mifare Card
                        }
                    }
                    break;
                case Desfire.E_ReaderState.ACTIVE_CARDON://Card is already detected and selected for R/W
                    //break;
                case Desfire.E_ReaderState.REMOVAL_DETECTION:// Detect for removal of a selected card 
                    pStatusCSC.ucStatCSC = 0x20;// Removal status
                    ret = mDesfire.GetVersion(out uid);
                    if (ret)
                    {
                        pStatusCSC.ucATR = new byte[128];
                        Array.Copy(mDesfire.mATQA, 0, pStatusCSC.ucATR, 0, 2);
                        Array.Copy(uid, 0, pStatusCSC.ucATR, 2, uid.Length);
                        pStatusCSC.ucNbDetectedCard = 1;
                        pStatusCSC.xCardType = 3;//Mifare Card
                    }
                    else
                    {//Card is removed
                        ret = false;
                    }
                    break;
                case Desfire.E_ReaderState.HALT:
                default:
                    break;
            }

#endif
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            else
            {
                mDesfire.ResetFieldEx();
               _IsSessionKeyCreatedPrevously = false;
                _lastFileAccesed = 0x00;
                _lastKeyUsed = 0x00;
                _lastSelectedApplication = 0x00;
            }
            _lastErrorCode =(int) Err;

            return Err;
        }
        static public CSC_API_ERROR StopPolling()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;

            if(mDesfire.ResetFieldEx() ) Err =CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;

            return Err;
        }

        static public CSC_API_ERROR IsoCommand(/*CSC_READER_TYPE pReaderType,
                                              int phRw,
                                              DEST_TYPE pDestType,*/
                                              byte[] pCommandApdu,
                                              out byte pSw1,
                                              out byte pSw2,
                                              out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            int nbytesRead=0;

            if (mDesfire.ExchangeAPDU((byte)pCommandApdu.Length, pCommandApdu, out pResData, out nbytesRead, out pSw1, out pSw2))
                Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR ReadDM1PurseLinkage(int offset,int nLength, out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x00;
            byte AppId = 0x01;
            byte mKeyNo = 0x01;// rw/r/w key
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[nLength];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x03, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, offset, nLength, out pResData,out pSw1,out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR ReadDM1ValidationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            // Application is Already Selected ...... 
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;            
            
            byte mFileid = 0x05;
            byte AppId = 0x01;            
            bool ret = false;
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
  /*              if ((_lastFileAccesed == mValidationFileid)&& _lastSelectedApplication== AppId) //Aready authenticated ... with WR Key .... no need to re-Authenticate with PCCI..
                {
                    //DO nothing ...
                    ret = true;
                }
                else
                {
                    _lastSelectedApplication = AppId;
                    if (SAMConfigured)
                    {
                        ret = mDesfire.Authenticate(out nRndB, 0x05);// Key no of File
                        if (ret)
                        {
                            ret = mCCHSSAMManager.CCHSSAM_GenDFAuthCode(0x01, mValidationFileid, 0x00, 0x02, nRndB, out mAuthCode);
                            if (ret)
                            {
                                ret = mDesfire.Authenticate2(mAuthCode, out nRndA);
                                if (ret) _lastFileAccesed =  mValidationFileid;
                            }
                        }
                    }
                }*/
                ret = GenerateSessionKey(AppId, mFileid, 0x01, 0x00, 0x03, out pSw1, out pSw2);// GenerateSessionKey(AppId, mFileid, 0x05, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {                   
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1,out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR ReadDM1PurseFile(out Int32 pValue, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x02;
            //key 0x03 =rw
            byte AppId = 0x01;
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pValue = 0;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM1 Purse:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, 0x01, 0x00, 0x03, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.GetValue(mFileid, out pValue, out pSw1,out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of Read DM1 Purse:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }
        static public CSC_API_ERROR CreditDM1PurseFile(int pValue, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x02;
            byte AppId = 0x01;
            pSw1 = 0xff;
            pSw2 = 0xFF;
           
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, 0x03, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.Credit(mFileid, pValue, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR DebitDM1PurseFile(int pValue, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x02;
            byte AppId = 0x01;
            pSw1 = 0xff;
            pSw2 = 0xFF;

            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, 0x03, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.Debit(mFileid, pValue, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR ReadDM1SequenceFile(out Int32 pValue, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x01;
            byte AppId = 0x01;
            byte mKeyNo = 0x01;// 0x02= rw key , 0x01= read only key
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pValue = 0;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM1 Sequence:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x03, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.GetValue(mFileid, out pValue, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of Read DM1 Sequence:-  Ticks:" +GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }
        static public CSC_API_ERROR CreditDM1SequenceFile(Int32 pValue, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x01;
            byte AppId = 0x01;
            byte mKeyNo = 0x02;// rw key , 0x01= read only key
            pSw1 = 0xff;
            pSw2 = 0xFF;
           // pValue = 0;
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.Credit(mFileid,  pValue, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR DebitDM1SequenceFile(int pValue, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x01;
            byte AppId = 0x01;
            byte mKeyNo = 0x02;// rw key , 0x01= read only key
            pSw1 = 0xff;
            pSw2 = 0xFF;
            // pValue = 0;
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.Debit(mFileid, pValue, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR ReadDM1HistoryFile(int nRecords,int offset, int nRecordSize,out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x03;
            byte AppId = 0x01;
            byte mKeyNo = 0x01;//0x04 rw key , 0x01= read only key
            pSw1 = 0xff;
            pSw2 = 0xFF;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "ReadDM1HistoryFile:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            pResData = new byte[nRecordSize * nRecordSize];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x03, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadRecords(mFileid, offset, nRecordSize, nRecords, out pResData,out pSw1,out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)  
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "ReadDM1HistoryFile:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR ReadDM1SaleFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x06;
            byte AppId = 0x01;
            
            pResData = null; 
            pSw1 = 0xff;
            pSw2 = 0xFF;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM1 Sale:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
            /*    if (_lastFileAccesed == mFileid && _lastSelectedApplication == AppId) //Aready authenticated ... with WR Key .... no need to re-Authenticate with PCCI..
                {
                    //DO nothing ...
                    ret = true;
                }
                else
                {
                    _lastSelectedApplication = AppId;
                    if (SAMConfigured)
                    {
                        ret = mDesfire.Authenticate(out nRndB, 0x06);// Key no of File
                        if (ret)
                        {
                            ret = mCCHSSAMManager.CCHSSAM_GenDFAuthCode(AppId, mFileid, 0x00, 0x02, nRndB, out mAuthCode);
                            if (ret)
                            {
                                ret = mDesfire.Authenticate2(mAuthCode, out nRndA);
                                if (ret) _lastFileAccesed = mFileid;
                            }
                        }
                    }//if (SAMConfigured)
                }//else if (_lastFileAccesed == mFileid) 
             */
                ret = GenerateSessionKey(AppId, mFileid, 0x01, 0x00, 0x03, out pSw1, out pSw2);//GenerateSessionKey(AppId, mFileid, 0x06, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
           
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of Read DM1 Sale:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }
        static public CSC_API_ERROR ReadDM1PersonalizationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x08;
            byte AppId = 0x01;
            byte mKeyNo = 0x01;//0x08; rw key , 0x01= read only key
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM1 Personalize:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x03, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of Read DM1 Personalize:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }
        static public CSC_API_ERROR ReadDM1CardHolderFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x09;
            byte AppId = 0x01;
            byte mKeyNo = 0x01;//0x09 rw key , 0x01= read only key
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM1 Card Holder:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
               // ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x03, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of Read DM1 Card holder:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }
        static public CSC_API_ERROR ReadDM2PendingFareDeductionFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x00;
            byte AppId = 0x02;
            byte mKeyNo = 0x01;// rw key 
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }

        static public CSC_API_ERROR ReadDM2SaleFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x01;
            byte AppId = 0x02;
            byte mKeyNo = 0x01;//
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM2 Sale:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of Read DM2 Sale:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }

        static public CSC_API_ERROR ReadDM2ValidationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x02;
            byte AppId = 0x02;
            byte mKeyNo = 0x01;//
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM2 Validation:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Read DM2 Validation:-  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return Err;
        }

        static public CSC_API_ERROR ReadDM2AddValueFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x03;
            byte AppId = 0x02;
            byte mKeyNo = 0x01;//
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }

        static public CSC_API_ERROR ReadDM2AgentPersonalizationFile(out byte pSw1, out byte pSw2, out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte mFileid = 0x08;
            byte AppId = 0x02;
            byte mKeyNo = 0x01;//
            pSw1 = 0xff;
            pSw2 = 0xFF;
            pResData = new byte[1];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(AppId, mFileid, mKeyNo, 0x00, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.ReadData(mFileid, 0, 32, out pResData, out pSw1, out pSw2);
                }
            }//if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }

        static public CSC_API_ERROR GenerateTAC(byte[] inData, int inLength, out byte[]Tac)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            Tac = new byte[4];
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                if (SAMConfigured)
                {
                    ret = mCCHSSAMManager.GenerateTAC(inData, inLength, out Tac);
                }
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        static public CSC_API_ERROR SelectApplication(byte mAppId)
        {
              CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            _IsSessionKeyCreatedPrevously = false;
            _lastFileAccesed = 0x00;
            _lastKeyUsed = 0x00;
            _lastSelectedApplication = 0x00;
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = mDesfire.SelectCardApplication(mAppId);
                _lastSelectedApplication = mAppId;
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }

        static public CSC_API_ERROR CommitTransaction(out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            pSw1 = 0xff;
            pSw2 = 0xff;
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = mDesfire.CommitTransaction(out pSw1, out pSw2);
                if (ret)
                {
                    _lastFileAccesed = 0x00;
                    _lastKeyUsed = 0x00;
                    _lastSelectedApplication = 0x00;
                    _IsSessionKeyCreatedPrevously = false;
                }
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode = (int)Err;
            return Err;
        }

        static public CSC_API_ERROR CommitTransaction()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;

            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = mDesfire.CommitTransaction();
                if (ret)
                {
                    _lastFileAccesed = 0x00;
                    _lastKeyUsed = 0x00;
                    _lastSelectedApplication = 0x00;
                    _IsSessionKeyCreatedPrevously = false;
                }
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }

        static public CSC_API_ERROR AbortTransaction()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            ret = mDesfire.AbortTransaction();
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            return Err;
        }

        static private bool GenerateSessionKey(byte mAppId,byte mFileid, byte mKeyId,byte oldkey,byte AccessRight, out byte pSw1, out byte pSw2)
        {
            Logging.Log(LogLevel.Verbose, "Gen Session AppID- [" + mAppId + "] Lasst App ID[" + _lastSelectedApplication + "] FileID = [" + mFileid + "] KeyId[" + mKeyId + "] AccessRights[" + AccessRight + "] SessionKeyCreatedPrevously " + _IsSessionKeyCreatedPrevously.ToString() + "lastFileAccesed " + _lastFileAccesed.ToString());
            byte[] nRndB;
            byte[] nRndA;
            byte[] mAuthCode;
            byte[] record;
            bool ret = false;
            pSw1 = 0xff;
            pSw2 = 0xFF;
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "Start of Generate Session Key Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif            
            if (_lastSelectedApplication == mAppId) //Aready authenticated ... with WR Key .... no need to re-Authenticate with PCCI..
            {
                if (_lastSelectedApplication == 0x02)// all files has same access keys... so no need to re-authenticate again..
                {
                    if (_IsSessionKeyCreatedPrevously)
                    {
                       
                        return true;
                    }
                }
                else
                {
                    if (_lastFileAccesed == mFileid && _IsSessionKeyCreatedPrevously) return true;
                    else if (AccessRight == 0x03 && _IsSessionKeyCreatedPrevously)//read
                    {                        
                        Logging.Log(LogLevel.Verbose, "Generate Session Key is Already created for Reading"); 
                        return true;
                    }
                    else { }
                }
                ret = true;
            }
            else// current application is not selected ... select the application
            {
                Logging.Log(LogLevel.Verbose, "Generate Session Key Selecting Appid [" + mAppId + "]");
                CSC_API_ERROR Err = SelectApplication(mAppId);
                if (Err == CSC_API_ERROR.ERR_NONE) ret = true;
                else ret = false;
            }
            if(ret)
            {
                //_lastSelectedApplication = mAppId;
                _IsSessionKeyCreatedPrevously = false;
                Logging.Log(LogLevel.Verbose, "Generate Session Key.. Generating new Session Key");
                if (SAMConfigured)
                {
#if _RW_TIME_CHECK
                    Logging.Log(LogLevel.Information, "Start of Authentication with CSC Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                    ret = mDesfire.Authenticate(out nRndB, mKeyId);// Key no of File
#if _RW_TIME_CHECK
                    Logging.Log(LogLevel.Information, "End of  Authentication with CSC Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                    if (ret)
                    {
#if _RW_TIME_CHECK
                        Logging.Log(LogLevel.Information, "Start of Gen Auth Code with SAM Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                        ret = mCCHSSAMManager.CCHSSAM_GenDFAuthCode(mAppId, mFileid, oldkey, AccessRight, nRndB, out mAuthCode);
#if _RW_TIME_CHECK
                        Logging.Log(LogLevel.Information, "End of Gen Auth Code with SAM Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                        if (ret)
                        {
#if _RW_TIME_CHECK
                            Logging.Log(LogLevel.Information, "Start of Authentication2 with CSC Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                            ret = mDesfire.Authenticate2(mAuthCode, out nRndA);
#if _RW_TIME_CHECK
                            Logging.Log(LogLevel.Information, "End of Authentication2 with CSC Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                            if (ret)
                            {
                                _IsSessionKeyCreatedPrevously = true;
                                _lastFileAccesed = mFileid;
                               // _lastSelectedApplication = mAppId; //SKS added on 20160511
                            }                            
                        }
                    }
                }//if (SAMConfigured)
                if (!ret)
                {
                    _IsSessionKeyCreatedPrevously = false;
                    _lastFileAccesed = 0x00;
                }
            } //if (!ret)
#if _RW_TIME_CHECK
            Logging.Log(LogLevel.Information, "End of  Gen Session Key ret[" + ret + "]  Ticks:" + GetTimeStamp().Ticks.ToString() + " Time:" + GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
            return ret;
        }//func end

        internal static CSC_API_ERROR WriteSAMSequence(int TxnSeq)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            //throw new NotImplementedException();
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                if (SAMConfigured)
                {
                    ret = mCCHSSAMManager.WriteSAMSequence(TxnSeq);
                }
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }

        internal static CSC_API_ERROR WriteRecordsFile(byte pAid, byte pFileNbr, byte pKeyNo, byte oldKey, int pOffset, byte[] pDataBuffer)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
           byte pSw1 = 0xff;
           byte pSw2 = 0xFF;
            
            //throw new NotImplementedException();
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(pAid, pFileNbr, pKeyNo, oldKey, 0x02, out pSw1, out pSw2);
                if (ret)
                {
                    ret = mDesfire.WriteRecords(pFileNbr, pOffset,pDataBuffer.Length, pDataBuffer,out pSw1,out pSw2);
                }
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        internal static CSC_API_ERROR WriteDataFile(byte pAid, byte pFileNbr, byte pKeyNo, byte oldKey, int pOffset, byte[] pDataBuffer)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            bool ret = false;
            byte pSw1 = 0xff;
            byte pSw2 = 0xFF;

            //throw new NotImplementedException();
            if (m_bIsOpen && mDesfire.IsMediaDetected() > 0)
            {
                ret = GenerateSessionKey(pAid, pFileNbr, pKeyNo, oldKey, 0x02, out pSw1, out pSw2);
                if (ret)//(pSw1 == 0x91 && pSw2 == 0x00)
                {
                    ret = mDesfire.WriteDataFile(pFileNbr, pOffset, pDataBuffer.Length, pDataBuffer, out pSw1,out pSw2);
                }
            }
            if (ret) Err = CSC_API_ERROR.ERR_NONE;
            _lastErrorCode =(int) Err;
            return Err;
        }
        public static CSC_API_ERROR PingReader(CSC_READER_TYPE cSC_READER_TYPE, int _hRw)
        {
            throw new NotImplementedException();
        }
    }
}
