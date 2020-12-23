using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.SmartMedia;


namespace IFS2.Equipment.TicketingRules
{
    public class CSCDesfireRW
    {
        private Int64 _SrNum;
        public byte[] mDiversification = new byte[8];
        int _lastAppliSelected;
        byte _lastAuthKeyNo;
        byte[] _authKey;
        DesfireEVISO mDesfireEV;
        CSCReaderFunctions mCSCReaderFunctions;
        SecurityMgr mSecurityMgr;
        public CSCDesfireRW()
        {
            _lastAppliSelected = 0x03;
            _lastAuthKeyNo = 0xff;
            _authKey = null;
            mDesfireEV = new DesfireEVISO();
            mCSCReaderFunctions = new CSCReaderFunctions();
            mSecurityMgr = new SecurityMgr();
        }
        public bool Init(int readerType, string portno, int samtype, int samslot, out FirmwareInfo mFirmwareInfo)
        {
            bool ret = false;
            mFirmwareInfo = new FirmwareInfo();
            _lastAppliSelected = 0x03;
            _lastAuthKeyNo = 0xff;
            _authKey = new byte[16];
            ret = mCSCReaderFunctions.Init(readerType, portno, samtype, samslot, out mFirmwareInfo);           
            return ret;
        }
        #region "SAM Specific"
        public void SAM_GetVersion(out NXP_SAM_Info mVersion)
        {
            mVersion = mCSCReaderFunctions.mSAMVerInfo;
        }
        public bool SAM_Activated()
        {
            return mCSCReaderFunctions.IsSAMActivate();
        }
       
        public bool SAM_Authenticate(byte bKeyNum, byte[] key, byte keyver, bool isKucKey)
        {
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            ret= mCSCReaderFunctions.SM_AuthenticateHost(key,isKucKey,bKeyNum,keyver,out pSw1, out pSw2);            
            return ret;
        }
        public bool SAM_ChangeKUCQuota( byte KeyNumKUCEntry,bool bupdateLimit,bool bUpdateKeyNoKUC,bool bupdatKeyVersion,long currQuota,byte[] newKUCEntry )
        {
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            ret = mCSCReaderFunctions.SM_ChangeKUCEntry(KeyNumKUCEntry, bupdateLimit, bUpdateKeyNoKUC, bupdatKeyVersion, newKUCEntry,out pSw1,out pSw2);
            return ret;
        }
        public bool SAM_GetKUCQuota(byte bKUCKey, out long currentSamQuota, out long currentSamValue)
        {
            bool ret = false;
            byte pSw1 = 0xFF, pSw2 = 0xFF;
            currentSamQuota = 0;
            currentSamValue = 0;
            Logging.Log(LogLevel.Verbose, "in side SAM_GetKUCQuota");
            ret = mCSCReaderFunctions.SM_GetKUCEntry(bKUCKey, out currentSamQuota, out currentSamValue, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "SAM_GetKUCQuota ret=" + ret.ToString() + " pSw1:" + pSw1.ToString() + " pSw2:" + pSw2.ToString() + " currentSamQuota: " + currentSamQuota.ToString() + " currentSamValue: " + currentSamValue.ToString());
            return ret;
        }
        #endregion
        #region "RW specific functions"
        public void Registerlistiner(int listertype, Utility.StatusListenerDelegate mlistenerCard)
        {
            //depending upon Reader type
            mCSCReaderFunctions.Registerlistiner(listertype, mlistenerCard);
        }
        public bool RW_RestartReader(out FirmwareInfo mFirmwareInfo)
        {
            return mCSCReaderFunctions.RestartReader(out mFirmwareInfo);
        }
        public bool RW_RestartReader(byte[] samKey)
        {
            return mCSCReaderFunctions.RestartReader(samKey);
        }
        public bool RW_SwitchToDetectRemovalState()
        {
            return mCSCReaderFunctions.SwitchToDetectRemovalState();
        }
        public CSC_API_ERROR RW_SwitchToCardOnState()
        {
            return mCSCReaderFunctions.SwitchToCardOnState();
        }
        public void RW_StopPolling()
        {
            mCSCReaderFunctions.StopPolling();
        }
        public void RW_StartPolling()
        {
            mCSCReaderFunctions.StartPollingEx();
        }
        public CSC_API_ERROR RW_PingReader()
        {
            return mCSCReaderFunctions.PingReader();
        }
        public void RW_StartField()
        {
            mCSCReaderFunctions.StartField();
        }
        public void RW_StopField()
        {
            mCSCReaderFunctions.StopField();
        }
        /// <summary>
        /// updates CSC Reader Firmware
        /// </summary>
        /// <param name="sfilePath"></param>
        /// <returns></returns>
        public bool RW_UpdateFirmware(string sfilePath)
        {
            return mCSCReaderFunctions.UpdateReaderFirmware(sfilePath);
        }
        public void RW_SmartSyncDetectOk(out MediaTypeDetected detectionState,
           out bool bSameMedia, // to be used selectivly only when detectionState != NONE
           bool bUseAsynch)
        {
            mCSCReaderFunctions.SmartSyncDetectOk(out detectionState, out bSameMedia, bUseAsynch);
        }
        public void RW_Extract_StatusCSC(ref StatusCSC pStatusCSC, out ulong SrNbr, out MediaTypeDetected detectionState)
        {
             detectionState = MediaTypeDetected.NONE;
            SrNbr = 0;
            mCSCReaderFunctions.ExtractFrom_StatusCSC(ref pStatusCSC, out detectionState, out SrNbr);
            Logging.Log(LogLevel.Verbose, "RW_Extract_StatusCSC  Media Type:" + detectionState.ToString());
            if (SrNbr > 0)
            {
                //var resultArray = Encoding.encoding.GetBytes(SrNbr.ToString());
                // var t = (ulong)SrNbr;
                byte[] resultArray = BitConverter.GetBytes(SrNbr);
                if (resultArray.Length == 8)
                {

                    Array.Copy(resultArray, 0, mDiversification, 0, 7);
                    mDiversification[7] = 0x88;
                    Array.Reverse(mDiversification);
                }
            }
        }
        #endregion
        #region "CSC NXP DesFire Functions"
        public string CheckAuthFailure(byte pSw1, byte pSw2, out bool IsKUCLimitReached)
        {
            IsKUCLimitReached = false;
            return mCSCReaderFunctions.SM_CheckAuthFailure(pSw1,pSw2,out IsKUCLimitReached);
        }
        public bool Authenticate(int AppId, byte authmode, byte KeyIndex, byte keyver, byte[] inDiv, out byte[] mSessionKey, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            int err = -1;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
            mSessionKey = new byte[1];
            if (AppId == _lastAppliSelected)
            {
                if (_lastAuthKeyNo == KeyIndex)//already authenticated with same key no need to authenticate again
                {
                    pSw1 = 0x91;
                    pSw2 = 0x00;
                    return true;
                }
            }
            else
            {
                //select the application 1st
                ret = SelectApplication(AppId);
                if (!ret) return ret;
            }
            byte keyNum = 0x00;
            int appid_df;
            byte[] en_RndAB;
            byte[] bRndA;
            ret = mCSCReaderFunctions.SM_GetKeyEntry(KeyIndex, out appid_df, out keyNum);
            Logging.Log(LogLevel.Verbose, "Authenticate, Entry Key[" + KeyIndex.ToString() + "] CardKeyNum[" + keyNum.ToString() + "]");
            if (ret)// && (appid_df == AppId))// no need to re-verify Appi --on 20190218
            {
                //Step 1.
                byte[] apdu = mDesfireEV._AuthenticateAPDU(keyNum);

                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out m_response, out pSw1, out pSw2);
                Logging.Log(LogLevel.Verbose, "Authenticate1 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                {
                    pSw1 = 0xff; pSw2 = 0xff;
                    //Step 2.
                    ret = mCSCReaderFunctions.SM_AuthenticatePICC_Step1(0x00, m_response, KeyIndex, keyver, inDiv, out en_RndAB, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Verbose, "SM_AuthenticatePICC_Step1 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                    if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                    {
                        ret = false;
                        pSw1 = 0xff; pSw2 = 0xff;
                        //Step 3.
                        ret = Authenticate2(en_RndAB, out bRndA, out pSw1, out pSw2);
                        Logging.Log(LogLevel.Verbose, "Authenticate2 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                        if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                        {
                            pSw1 = 0xff;
                            pSw2 = 0xff;
                            //Step 4.
                            mCSCReaderFunctions.SM_AuthenticatePICC_Step2(bRndA, out pSw1, out pSw2);
                            Logging.Log(LogLevel.Verbose, "SM_AuthenticatePICC_Step2 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                            if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                            {
                                Logging.Log(LogLevel.Verbose, "AuthenticatePICC done with Success ");
                                ret = true;
                                _lastAuthKeyNo = KeyIndex;// keyNum;
                            }
                            else
                            {
                                Logging.Log(LogLevel.Verbose, "AuthenticatePICC Failed!! ");
                            }
                        }// ret = Authenticate2
                    }
                }//if (err == 0 && pSw1 == 0x91 && pSw2
                else ret = false;
            }//if (ret && (appid_df == AppId))
            return ret;
        }
        public bool Authenticate(int AppId, byte authmode, byte KeyIndex, byte keyver, byte cardKeyNum, byte[] inDiv, out byte[] mSessionKey, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            int err = -1;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
            mSessionKey = new byte[1];
            if (AppId == _lastAppliSelected)
            {
                if (_lastAuthKeyNo == KeyIndex)//already authenticated with same key no need to authenticate again
                {
                    pSw1 = 0x91;
                    pSw2 = 0x00;
                    return true;
                }
            }
            else
            {
                //select the application 1st
                ret = SelectApplication(AppId);
                if (!ret) return ret;
            }
            byte keyNum = cardKeyNum;
            int appid_df;
            byte[] en_RndAB;
            byte[] bRndA;
            // ret = mCSCReaderFunctions.SM_GetKeyEntry(KeyIndex, out appid_df, out keyNum);
            // if (ret)// && (appid_df == AppId))// no need to re-verify Appi --on 20190218
            {
                //Step 1.
                byte[] apdu = mDesfireEV._AuthenticateAPDU(keyNum);

                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out m_response, out pSw1, out pSw2);
                Logging.Log(LogLevel.Verbose, "Authenticate1 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                {
                    pSw1 = 0xff; pSw2 = 0xff;
                    //Step 2.
                    ret = mCSCReaderFunctions.SM_AuthenticatePICC_Step1(authmode, m_response, KeyIndex, keyver, inDiv, out en_RndAB, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Verbose, "SM_AuthenticatePICC_Step1 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                    if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                    {
                        ret = false;
                        pSw1 = 0xff; pSw2 = 0xff;
                        //Step 3.
                        ret = Authenticate2(en_RndAB, out bRndA, out pSw1, out pSw2);
                        Logging.Log(LogLevel.Verbose, "Authenticate2 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                        if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                        {
                            pSw1 = 0xff;
                            pSw2 = 0xff;
                            //Step 4.
                            mCSCReaderFunctions.SM_AuthenticatePICC_Step2(bRndA, out pSw1, out pSw2);
                            Logging.Log(LogLevel.Verbose, "SM_AuthenticatePICC_Step2 pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                            if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                            {
                                ret = true;
                                _lastAuthKeyNo = KeyIndex;// keyNum;
                            }
                            else
                            {
                                Logging.Log(LogLevel.Verbose, "AuthenticatePICC Failed!! ");
                            }
                        }// ret = Authenticate2
                    }
                }//if (err == 0 && pSw1 == 0x91 && pSw2
            }//if (ret && (appid_df == AppId))
            return ret;
        }
        public bool Authenticate(int AppId, byte authmode, byte KeyIndex, byte keyver, bool bUseSAMKeys, out byte[] mRndAB, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            int err = -1;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
            mRndAB = new byte[1];
            if (AppId == _lastAppliSelected)
            {
                if (_lastAuthKeyNo == KeyIndex)//already authenticated with same key no need to authenticate again
                {
                    pSw1 = 0x91;
                    pSw2 = 0x00;
                    return true;
                }
            }
            else
            {
                //select the application 1st
                ret = SelectApplication(AppId);
                if (!ret) return ret;
            }
            if (bUseSAMKeys)
            {
                byte[] apdu = mDesfireEV._AuthenticateAPDU(KeyIndex);

                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out m_response, out pSw1, out pSw2);
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                {
                    if (bUseSAMKeys)
                    {
                        //load static default keys
                        byte[] key = new byte[16];
                        for (int j = 0; j < 16; j++) key[j] = TicketingKeys.Keys[KeyIndex, j];
                        byte[] challange = mSecurityMgr.CalculateRndAB(m_response, key);
                        pSw1 = 0xff;
                        pSw1 = 0xff;
                        ret = Authenticate2(challange, out m_response, out pSw1, out pSw2);//
                        if (pSw1 == CONSTANT.COMMAND_SUCCESS + 1 && pSw2 == 0)
                        {

                            //Note: before copy, rnda can be decripted and verified .... then session key can be generated...
                            //As of now assuming all is well
                            mRndAB = new byte[mSecurityMgr.sessionkey.Length];
                            _lastAuthKeyNo = KeyIndex;
                            Array.Copy(mSecurityMgr.sessionkey, mRndAB, mRndAB.Length);
                            //

                        }
                        else ret = false;
                    }
                    else //Real SAM
                    {

                    }
                }
            }
            else
            {
                // real SAM
                byte keyNum = 0x00;
                int appid_df;
                byte[] en_RndAB;
                byte[] bRndA;
                ret = mCSCReaderFunctions.SM_GetKeyEntry(KeyIndex, out appid_df, out keyNum);
                if (ret && (appid_df == AppId))
                {
                    byte[] apdu = mDesfireEV._AuthenticateAPDU(keyNum);
                    err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out m_response, out pSw1, out pSw2);
                    if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                    {
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = mCSCReaderFunctions.SM_AuthenticatePICC_Step1(0x00, m_response, KeyIndex, keyver, null, out en_RndAB, out pSw1, out pSw2);
                        if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
                        {
                            ret = false;
                            pSw1 = 0xff; pSw2 = 0xff;
                            ret = Authenticate2(en_RndAB, out bRndA, out pSw1, out pSw2);
                            if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                            {
                                pSw1 = 0xff;
                                pSw2 = 0xff;
                                mCSCReaderFunctions.SM_AuthenticatePICC_Step2(bRndA, out pSw1, out pSw2);
                                if ((pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                                {
                                    ret = true;
                                    _lastAuthKeyNo = KeyIndex;
                                }
                            }// ret = Authenticate2
                        }
                    }//if (err == 0 && pSw1 == 0x91 && pSw2
                }//if (ret && (appid_df == AppId))
            }
            return ret;
        }
        /*
        public bool Authenticate(int AppId, byte KeyEntry,byte keyver, byte accesRights, out byte[] mRndAB, out byte pSw1, out byte pSw2)
        {
        }*/
        private bool Authenticate2(byte[] mRndAB, out byte[] mRndA, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            int err = -1;
            byte[] m_response;
            int nByteRead = 0;
            pSw1 = 0xff;
            pSw2 = 0xff;
            mRndA = new byte[1];

            err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT,
                mDesfireEV._AuthenticateAPDU_Step2(mRndAB), out m_response, out pSw1, out pSw2);
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                mRndA = new byte[8];
                Array.Copy(m_response, 0, mRndA, 0, 8);
                ret = true;
            }
            else
            {
                mRndA = new byte[1];
                ret = false;

            }
            return ret;
        }

        public bool CreateApplication(int appId, byte keysetting, byte nbKeys, out byte pSw1, out byte pSw2)
        {
            bool ret = true;
            int err = -1;
            pSw1 = 0xff;
            pSw2 = 0xff;
            byte[] apdu;//= mDesfireEV._CreateApplicationAPDU(appId, keysetting, nbKeys);
            byte[] mresponse;

            //for cairo project it applications are created with chipher comm not plain
            apdu = mDesfireEV._CreateApplicationAPDU(appId, keysetting, nbKeys);
            err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out mresponse, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "CreateApplication pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                ret = true;
            }
            else ret = false;

            return ret;
        }
        public bool SelectApplication(int appId)
        {
            bool ret = false;
            int err = -1;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] resp;
            /*  if (appId == _lastAppliSelected)
              {
                  ret = true;
              }
              else*/
            {
                byte[] apdu = mDesfireEV._SelectAppAPDU(appId);
                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out resp, out pSw1, out pSw2);
                Logging.Log(LogLevel.Verbose, "SelectApplication aid[ " + appId.ToString() + "]  pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                {
                    _lastAppliSelected = appId;
                    ret = true;
                }
                else
                    _lastAppliSelected = 0x03;
            }
            return ret;
        }
        public bool DeleteApplication(int appid)
        {
            bool ret = false;
            int err = -1;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] respose;

            /*if (_lastAppliSelected != appid)
            {
                //select application
                ret = SelectApplication(appid);
                if (ret)
                {
                   if (_lastAuthKeyNo != 0x00)
                    {
                        ret = Authenticate(appid,0x00, 100, 0x00, false, out respose, out pSw1, out pSw2);
                        if (ret &&( pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                        {
                        }
                        else ret = false;
                    }
                }
            }*/

            {
                byte[] apdu = mDesfireEV._deleteAppAPDU(appid);
                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out respose, out pSw1, out pSw2);
                Logging.Log(LogLevel.Verbose, "SelectApplication pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                {
                    ret = true;
                }
            }//ret

            return ret;
        }
        public bool ChangeKey(int appid, byte authmode, byte keyConfMethod, byte oldkeyEntry, byte oldKeyver, byte newKeyEntry, byte newkeyver, byte cardKeyNum, byte[] DivIn)
        {
            return false;
            bool ret = true;
            int err = -1;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] respose;
            byte[] cryptogram;
            //1. Select Application
            if (_lastAppliSelected != appid)
            {
                //select application
                ret = SelectApplication(appid);
                _lastAuthKeyNo = 0xff;
            }
            //2. Authenticate with the change key of App  normally is master key of the aid
            if (ret)
            {
                if (_lastAuthKeyNo != oldkeyEntry)
                {
                    ret = Authenticate(appid, authmode, oldkeyEntry, oldKeyver, cardKeyNum, DivIn, out respose, out pSw1, out pSw2);
                    if (ret && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                    {
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = mCSCReaderFunctions.SM_ChangeKey(keyConfMethod, oldkeyEntry, oldKeyver, newKeyEntry, newkeyver, cardKeyNum, DivIn, out cryptogram, out pSw1, out pSw2);
                        if (ret && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                        {
                            pSw1 = 0xff;
                            pSw2 = 0xff;
                            byte[] apdu_changekey = mDesfireEV.changeKeys(cardKeyNum, cryptogram);
                            err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu_changekey, out respose, out pSw1, out pSw2);
                            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                            {
                                //file created with success
                                ret = true;
                            }
                            else ret = false;
                        }//mCSCReaderFunctions.SM_ChangeKey
                    }
                    else ret = false;
                }//if (_lastAuthKeyNo != keyNum)
            }//~2. Authenticate with the change key
            //3. Encrypt new key 
            if (ret)
            {

            }//~Step 3
            //4. update the key
            return ret;
        }
        public bool ChangeKey(byte keyConfMethod, byte oldkeyEntry, byte oldKeyver, byte newKeyEntry, byte newkeyver, byte cardKeyNum, byte[] DivIn)
        {
            //return false;
            bool ret = false;
            int err = -1;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] respose;
            byte[] cryptogram;
            Logging.Log(LogLevel.Verbose, "CSCDesfireRW::ChanegKey  KeyConf[" + keyConfMethod.ToString("X2") + "] oldKeyEntry[" + oldkeyEntry.ToString() + "] OldKeyVer[" + oldKeyver.ToString() + "] Newkey[" + newKeyEntry.ToString() + "] newkeyVer[" + newkeyver.ToString() + "] CardKeyNum[" + cardKeyNum.ToString("X2") + "]");
            pSw1 = 0xff; pSw2 = 0xff;
            ret = mCSCReaderFunctions.SM_ChangeKey(keyConfMethod, oldkeyEntry, oldKeyver, newKeyEntry, newkeyver, cardKeyNum, DivIn, out cryptogram, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, " mCSCReaderFunctions.SM_ChangeKey pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
            if (ret && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                pSw1 = 0xff;
                pSw2 = 0xff;
                byte[] apdu_changekey = mDesfireEV.changeKeys(cardKeyNum, cryptogram);

                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu_changekey, out respose, out pSw1, out pSw2);
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                {
                    // success
                    ret = true;
                    Logging.Log(LogLevel.Verbose, "Key changed with Sucess!!");
                }
                else
                {
                    ret = false;
                    Logging.Log(LogLevel.Error, "Failed to change Keys pSw1:[" + pSw1.ToString("X2") + "]  pSw2[" + pSw2.ToString("X2") + "]");
                }
            }//mCSCReaderFunctions.SM_ChangeKey 

            return ret;
        }
        public bool CreateFile(int appid, byte fileId, DF_FILE_TYPE fileType, byte bcommsettings, byte accessLSB, byte accessMSB, int size)
        {
            bool ret = true;
            int err = -1;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] respose;
            //1. select application
            //2. Authenticate with application master key
            //3. create file
            Logging.Log(LogLevel.Verbose, "Inside CSCDesfireRW.CreateFile , fileId[" + fileId.ToString("X2") + "]");
            /*  if (_lastAppliSelected != appid)
              {
                  //select application
                  ret = SelectApplication(appid);               
              }
             */
            if (ret)
            {
                /* if (_lastAuthKeyNo != 0x00)
                 {
                     //2. Authenticate with master key of the application
                     ret = Authenticate(appid, 0x00, 0x00, 0x00, false, out respose, out pSw1, out pSw2);
                     if (ret && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                     {
                     }
                     else ret = false;
                 }*/
                if (ret)//create file 
                {
                    pSw1 = 0xff;
                    pSw2 = 0xff;
                    byte[] apdu = mDesfireEV._CreateFileAPDU(fileId, (byte)fileType, bcommsettings, accessLSB, accessMSB, size);
                    err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out respose, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Verbose, "CreateFile pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                    if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                    {
                        //file created with success
                        Logging.Log(LogLevel.Verbose, "Inside CSCDesfireRW.CreateFile , Sucess");
                    }
                    else
                    {
                        ret = false;
                        Logging.Log(LogLevel.Error, "Inside CSCDesfireRW.CreateFile , fileId[" + fileId.ToString("X2") + "] Failed....");
                    }
                }//create file now

            }
            else
            {
                Logging.Log(LogLevel.Error, "CSCDesfireRW.CreateFile ,Select Application failed appid: " + appid.ToString());
            }
            return ret;
        }
        public bool DeleteFile(byte fileId)
        {
            bool ret = false;
            int err = -1;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] respose;

            byte[] apdu = mDesfireEV._DeleteFileAPDU(fileId);

            err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out respose, out pSw1, out pSw2);
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                //file created with success
                ret = true;
            }

            return ret;
        }
        public bool commitTxn()
        {
            bool ret = false;
            int err = -1;
            byte[] response;
            byte pSw1 = 0xff, pSw2 = 0xff;

            byte[] apdu = mDesfireEV._CommitAPDU();
            err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out response, out pSw1, out pSw2);
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && (pSw2 == 0x00 || pSw2 == 0x0C))
            {
                ret = true;
            }
            else ret = false;

            return ret;
        }
        public bool ChangeMasterKeySettings(byte keySettings)
        {
            bool ret = false;
            int err = -1;
            byte[] response;
            byte pSw1 = 0xff, pSw2 = 0xff;

            byte[] crypted_keyset;
            byte[] keyset = { keySettings };
            ret = mCSCReaderFunctions.SM_EncryptData(keyset, out crypted_keyset, out pSw1, out pSw2);
            if (ret == true && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                ret = false;
                pSw1 = 0xff; pSw2 = 0xff;

                byte[] apdu = mDesfireEV._ChangeKeySettingsAPDU(crypted_keyset);
                err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out response, out pSw1, out pSw2);

                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
                {
                    ret = true;
                }
                else ret = false;

            }
            return ret;
        }
        public bool WriteDataFile(byte fileid, byte fileType, bool IsCrypted, int offset, byte[] bdata, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte[] pResData = null;
            int nbWrite = 0, index = 32;//52
            byte[] crpt_data;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            if (IsCrypted)
                ret = mCSCReaderFunctions.SM_EncryptData(bdata, out crpt_data, out pSw1, out pSw2);
            else
            {
                pSw1 = 0x90;
                pSw2 = 0x00;
                ret = true;
                crpt_data = new byte[bdata.Length];
                Array.Copy(bdata, crpt_data, bdata.Length);
            }
            // if (crpt_data.Length > 52) index = 52;--needed to be verified ..why if the length is >32 is PICC asking for AF
            Logging.Log(LogLevel.Warning, "WriteDataFile Data frames size needed to be...verified...");
            if (ret && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                pSw1 = 0xFF;
                pSw2 = 0xFF;
                byte[] apdu = mDesfireEV._WriteDataAPUD(fileid, fileType, offset, crpt_data, bdata.Length);

                int err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out pResData, out pSw1, out pSw2);
                Logging.Log(LogLevel.Verbose, "WriteDataFile pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
                if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90))
                {
                    if (pSw2 == 0xAF)
                    {
                        pSw1 = 0xFF;
                        pSw2 = 0xFF;
                        ret = WriteIntermediate(crpt_data, index, out pSw1, out pSw2);
                    }
                    else if (pSw2 == 0x00) ret = true;
                }
                return ret;
            }
            else return false;
        }
        private bool WriteIntermediate(byte[] pdata, int startindex, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte[] pResData = null;
            byte[] Databuff = null;
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            int len = pdata.Length - startindex;

            Logging.Log(LogLevel.Verbose, "WriteIntermediate data len: " + len.ToString() + "startIndex: " + startindex.ToString());
            /*
            if (len <= 59)
            {
                Databuff = new byte[len];
                Array.Copy(pdata, startindex, Databuff, 0, len);
            }
            else
            {
                Databuff = new byte[59];
                startindex += 59;
                Array.Copy(pdata, startindex, Databuff, 0, 59);
            }*/
            if (len <= 32)
            {
                Databuff = new byte[len];
                Array.Copy(pdata, startindex, Databuff, 0, len);
            }
            else
            {
                Databuff = new byte[32];
                startindex += 32;
                Array.Copy(pdata, startindex, Databuff, 0, 32);
            }
            byte[] apdu = mDesfireEV._WriteDataIntermediateAPDU(Databuff);

            int err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, apdu, out pResData, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "WriteIntermediate pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90))
            {
                if (pSw2 == 0xAF)
                {
                    pSw1 = 0xFF;
                    pSw2 = 0xFF;
                    ret = WriteIntermediate(pdata, startindex, out pSw1, out pSw2);
                }
                else if (pSw2 == 0x00) ret = true;
            }
            return ret;
        }
        public bool GetApplicationId(out List<int> appids, out byte pSw1, out byte pSw2)
        {
            byte[] response;
            bool ret = false;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            appids = new List<int>();
            appids.Clear();
            int err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, mDesfireEV._GetApplicationIds(), out response, out pSw1, out pSw2);
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90))
            {
                if (pSw2 == 0x00 || pSw2 == 0xAF)
                {
                    ret = true;
                    if (response.Length >= 3)
                    {
                        int nbapp = response.Length / 3;
                        int appid = 0;
                        byte b = 0;
                        for (int i = 0; i < nbapp * 3; )
                        {
                            appid = 0;
                            appid |= response[i + 2];
                            appid <<= 8;
                            appid |= response[i + 1];
                            appid <<= 8;
                            appid |= response[i];
                            i = i + 3;
                            appids.Add(appid);
                        }
                    }
                }
            }
            return ret;
        }
        public int CheckIfCardProducedisVirgin()
        {
            byte[] response;
            int ret = -1;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            int err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, mDesfireEV._GetApplicationIds(), out response, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "CheckIfCardProducedisVirgin pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90))
            {
                if (pSw2 == 0x00 || pSw2 == 0xAF)
                {
                    if (response != null)
                        ret = 0;
                    else ret = 1;
                }

            }
            else Logging.Log(LogLevel.Error, "CheckIfCardProducedisVirgin error pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
            return ret;
        }
        public bool FormateCard()
        {
            byte[] response;
            bool ret = false;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            int err = mCSCReaderFunctions.ISOCommand(DEST_TYPE.DEST_PICC_TRANSPARENT, mDesfireEV._FormateCard(), out response, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "FormateCard pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
            if (err == 0 && (pSw1 == 0x91 || pSw1 == 0x90) && pSw2 == 0x00)
            {
                ret = true;
            }
            else Logging.Log(LogLevel.Error, "FormateCard Error pSw1[" + pSw1.ToString("X2") + "] , pSw2[" + pSw2.ToString("X2") + "]");
            return ret;
        }

        public void ResetProperties()
        {
            _lastAppliSelected = 0x03;
            _lastAuthKeyNo = 0xff;
        }        
        #endregion
    }
}
