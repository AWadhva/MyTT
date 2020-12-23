using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.SmartMedia;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    public enum E_TP_Errors
    {
        OK= 0x00,
        ERR_NOT_A_VERGINCARD= 0x01,
        ERR_AUTH_FAILURE = 0x02,
        ERR_READ = 0x03,
        ERR_WRITE = 0x04,
        ERR_NOT_SAME_CARD = 0x05,
        ERR_KUC_LIMIT_OVER =0x06
    };
    public class DFCairo
    {
        private CSCDesfireRW mCSCDesfireRW;
        private bool _IsReaderConnected;
               
        public DFCairo()
        {
            _IsReaderConnected = false;
            mCSCDesfireRW = new CSCDesfireRW();
        }

        public bool Init(int readerType, string portno, int samtype, int samslot,out FirmwareInfo mFirmwareInfo)
        {
            mFirmwareInfo = new FirmwareInfo();
            _IsReaderConnected = mCSCDesfireRW.Init(readerType, portno, samtype, samslot, out mFirmwareInfo);
            return _IsReaderConnected;
        }
        public void Registerlistiner(int listertype, Utility.StatusListenerDelegate mlistenerCard)
        {
            //depending upon Reader type
            mCSCDesfireRW.Registerlistiner(listertype, mlistenerCard);
        }        
        public CSCDesfireRW GetReaderInstance()
        {
            return mCSCDesfireRW;
        }
        public bool InitializeCard(UInt32 cardEngNo, UInt32 artwork,UInt16 cardvalidity, out int errocode)
        {
            bool kuclimit = false;
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            errocode = (int)E_TP_Errors.ERR_READ;//3;
            if (_IsReaderConnected)
            {
                //1. Load DF phy. layout
                //2. based on the phy layout Initialize the card....
                //or manually apply layout to the card....

                //manual layout 
                //0x0f-> bit4-b7 =0 application master key authentication needed to chnage any key  


                //1. Select the card AID 0x00
                //2. Perform the Card Master Key authentication (0x8X 0x0A...APDU, Key Entry 104.
                //3. Change the Card Master Key 104 => 100
                //4. Authenticate with the Card Master Key 100
                //5. Create Application 0x818000
                //6. Select Application 0x818000
                //7. Authenticate with the FACTORY AID 0x818000’s Master Key (Key Entry 103).
                //8. Change the AID 0x818000’s Master Key 103=> 101 (source Key Entry 103 and Target Key Entry 101.)
                //9. Authenticate with the REAL AID 0x818000 Master Key (Key Entry 101).
                //10. Change all the AID 0x818000 keys 
                /*     Key Changing from 21 => 1
                       Key Changing from 22 => 2
                       Key Changing from 23 => 3
                 */
                /*
                   11. Same process is performed for AID 0XFFFFFF, considering the following relationships.
                        Key Changing from 60 => 40 (in this case, the Application MK)
                        Key Changing from 61 => 41
                        Key Changing from 62 => 42
                 */
                byte keyIndex = 104;
                byte[] b_RndAB;
                ret = mCSCDesfireRW.SelectApplication(0x00);
                if (ret)
                {
                    ret = false;
                    //a. Authenicate with 104 (card master,aid 0x00 ), then 

                    // DfPhyLayout.Instance.GetKeyEntry(0x00,0x00,EAccessPermission.E_AccessPermission_ChangeKey,out keyIndex);
                    ret = mCSCDesfireRW.Authenticate(0x00, 0x00, keyIndex, 0x00, 0x00, null, out b_RndAB, out pSw1, out pSw2);
                    if (ret)//(ret)
                    {
                        ret = false;
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = mCSCDesfireRW.CreateApplication(0x818000, 0x0F, 0x04, out pSw1, out pSw2);
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = false;
                        ret = mCSCDesfireRW.CreateApplication(0xFFFFFF, 0x0F, 0x03, out pSw1, out pSw2);//
                    }
                    else
                    {
                        //bool kuclimit=false;
                        mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out kuclimit);
                        if (kuclimit) errocode =(int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                        errocode = (int)E_TP_Errors.ERR_AUTH_FAILURE;//2;
                    }

                }
                else errocode = 3;
                if (ret)
                {
                    pSw1 = 0xff; pSw2 = 0xff;
                    ret = mCSCDesfireRW.SelectApplication(0x818000);
                    {
                        keyIndex = 103;//0x65//T.P. (Personalization & Application Master key)
                        ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, keyIndex, 0x00, 0x00, null, out b_RndAB, out pSw1, out pSw2);
                        Logging.Log(LogLevel.Information, "InitializeCard () Auth appid:0x818000,Key num:103 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                        if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        {
                            // if (false)//commenting as of now...
                            {
                                ret = false;
                                //create files ... according to 4020_19806 Ba 20120704 Fare Media Electrical Data Layout Specification , 4.2.2
                                pSw1 = 0xff; pSw2 = 0xff;
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x00, DF_FILE_TYPE.BACKUP_DATA_FILE, 0x03, 0x20, 0x3F, 0x20);//32bytes T_FAT/GAT FAT page 41 , 4.2.4.2
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x00  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x08, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x00, 0x3F, 0xC0);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x08  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x09, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x10, 0x3F, 0x0350);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x09  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x0A, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x20, 0x3F, 0x0006F0);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x0A  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x01, DF_FILE_TYPE.VALUE_FILE, 0x03, 0x10, 0x32, 0x00);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x01  result: " + ret.ToString());
                            }
                            //File contect
                            if (ret)
                            {
                                ret = CreateDirectoryContent(0x818000, 0, 0, cardvalidity, out kuclimit);
                                if (!ret)
                                {
                                    if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                                    else errocode = (int)E_TP_Errors.ERR_WRITE;//4 
                                }
                            }
                            else errocode = (int)E_TP_Errors.ERR_WRITE;//4 

                        }
                        else 
                        {
                           
                            mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out kuclimit);
                            if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                            errocode = (int)E_TP_Errors.ERR_AUTH_FAILURE;//2;
                        }

                    }
                    ret = mCSCDesfireRW.SelectApplication(0xFFFFFF);
                    if (ret)
                    {
                        pSw1 = 0xff; pSw2 = 0xff;
                        keyIndex = 60;//0x28//C.I. (Card Issuer & Application Master key)
                        ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x00, keyIndex, 0x00, 0x00, null, out b_RndAB, out pSw1, out pSw2);
                        Logging.Log(LogLevel.Information, "InitializeCard () Auth appid:0xFFFFFF,Key num:60 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                        if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        {
                            //create files ...
                            pSw1 = 0xff; pSw2 = 0xff;
                            ret = mCSCDesfireRW.CreateFile(0xFFFFFF, 0x08, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x00, 0x00, 0xEF, 0x40);//64bytes
                            Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0xFFFFFF, fileId 0x08  result: " + ret.ToString());
                            if (ret)
                            {
                                ret = mCSCDesfireRW.CreateFile(0xFFFFFF, 0x09, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x12, 0x2F, 0x80);//64bytes
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0xFFFFFF, fileId 0x09  result: " + ret.ToString());
                            }
                            else errocode = (int)E_TP_Errors.ERR_WRITE;//4

                            //File contect
                            if (ret)
                            {
                                ret = CreateDirectoryContent(0xFFFFFF, cardEngNo, artwork, cardvalidity, out kuclimit);
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_EFCardHeader result: " + ret.ToString());
                                if (!ret)
                                {
                                    if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                                    else errocode = (int)E_TP_Errors.ERR_WRITE;//4 
                                }
                            }
                            else errocode = (int)E_TP_Errors.ERR_WRITE;//4

                        }
                        else //errocode = 2;
                        {
                            mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out kuclimit);
                            if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                            errocode = (int)E_TP_Errors.ERR_AUTH_FAILURE;//2;
                        }
                    }
                    else
                    {
                        Logging.Log(LogLevel.Error, "InitializeCard, Select appli 0xFFFFFF Failed ....");
                       // errocode = 3;
                        errocode = (int)E_TP_Errors.ERR_READ;//3
                    }
                    //Now change the keys....
                    if (ret)
                    {
                        Logging.Log(LogLevel.Verbose, "File content created ... Now Changing Keys");
                        ret = ChangeCardKeys(out kuclimit);
                        if (ret) Logging.Log(LogLevel.Verbose, "Change Keys Sucess!!!");
                        else
                        {
                            Logging.Log(LogLevel.Error, "Change Keys Failed!!!");
                            if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                            else errocode = (int)E_TP_Errors.ERR_WRITE; //4;
                        }
                    }
                }//
                ///
                Logging.Log(LogLevel.Verbose, "File content created ... End");
            }
            return ret;
        }
        public bool InitializeCardEx(UInt32 cardEngNo, UInt32 artwork, UInt16 cardvalidity, out int errocode)
        {
            bool kuclimit = false;
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            errocode = (int)E_TP_Errors.ERR_READ;//3;
            if (_IsReaderConnected)
            {
                //just after above if condition
                mCSCDesfireRW.ResetProperties();
                //1. Load DF phy. layout
                //2. based on the phy layout Initialize the card....
                //or manually apply layout to the card....

                //manual layout 
                //0x0f-> bit4-b7 =0 application master key authentication needed to chnage any key  


                //1. Select the card AID 0x00
                //2. Perform the Card Master Key authentication (0x8X 0x0A...APDU, Key Entry 104.
                //3. Change the Card Master Key 104 => 100
                //4. Authenticate with the Card Master Key 100
                //5. Create Application 0x818000
                //6. Select Application 0x818000
                //7. Authenticate with the FACTORY AID 0x818000’s Master Key (Key Entry 103).
                //8. Change the AID 0x818000’s Master Key 103=> 101 (source Key Entry 103 and Target Key Entry 101.)
                //9. Authenticate with the REAL AID 0x818000 Master Key (Key Entry 101).
                //10. Change all the AID 0x818000 keys 
                /*     Key Changing from 21 => 1
                       Key Changing from 22 => 2
                       Key Changing from 23 => 3
                 */
                /*
                   11. Same process is performed for AID 0XFFFFFF, considering the following relationships.
                        Key Changing from 60 => 40 (in this case, the Application MK)
                        Key Changing from 61 => 41
                        Key Changing from 62 => 42
                 */
                byte keyIndex = 104;
                byte[] b_RndAB;
                ret = mCSCDesfireRW.SelectApplication(0x00);
                if (ret)
                {
                    ret = false;
                    //a. Authenicate with 104 (card master,aid 0x00 ), then 

                    // DfPhyLayout.Instance.GetKeyEntry(0x00,0x00,EAccessPermission.E_AccessPermission_ChangeKey,out keyIndex);
                    ret = mCSCDesfireRW.Authenticate(0x00, 0x00, keyIndex, 0x00, 0x00, null, out b_RndAB, out pSw1, out pSw2);
                    if (ret)//(ret)
                    {
                        ret = false;
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = mCSCDesfireRW.CreateApplication(0x818000, 0x0F, 0x04, out pSw1, out pSw2);
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = false;
                        ret = mCSCDesfireRW.CreateApplication(0xFFFFFF, 0x0F, 0x03, out pSw1, out pSw2);//
                    }
                    else
                    {
                        //bool kuclimit=false;
                        mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out kuclimit);
                        if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                        errocode = (int)E_TP_Errors.ERR_AUTH_FAILURE;//2;
                    }

                }
                else errocode = 3;
                //Now change the keys....
                if (ret)
                {
                    Logging.Log(LogLevel.Verbose, "Changing Keys...");
                    ret = ChangeCardKeys(out kuclimit);
                    if (ret) Logging.Log(LogLevel.Verbose, "Change Keys Sucess!!!");
                    else
                    {
                        Logging.Log(LogLevel.Error, "Change Keys Failed!!!");
                        if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                        else errocode = (int)E_TP_Errors.ERR_WRITE; //4;
                    }
                }

                if (ret)
                {
                    pSw1 = 0xff; pSw2 = 0xff;
                    ret = mCSCDesfireRW.SelectApplication(0x818000);
                    {
                        keyIndex = 0x65;//T.P. (Personalization & Application Master key)
                        ret = mCSCDesfireRW.Authenticate(0x818000, 0x11, keyIndex, 0x00, 0x00, mCSCDesfireRW.mDiversification, out b_RndAB, out pSw1, out pSw2);
                        Logging.Log(LogLevel.Information, "InitializeCard () Auth appid:0x818000,Key num:103 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                        if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        {
                            // if (false)//commenting as of now...
                            {
                                ret = false;
                                //create files ... according to 4020_19806 Ba 20120704 Fare Media Electrical Data Layout Specification , 4.2.2
                                pSw1 = 0xff; pSw2 = 0xff;
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x00, DF_FILE_TYPE.BACKUP_DATA_FILE, 0x03, 0x20, 0x3F, 0x20);//32bytes T_FAT/GAT FAT page 41 , 4.2.4.2
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x00  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x08, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x00, 0x3F, 0xC0);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x08  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x09, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x10, 0x3F, 0x0350);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x09  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x0A, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x20, 0x3F, 0x0006F0);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x0A  result: " + ret.ToString());
                                ret = mCSCDesfireRW.CreateFile(0x818000, 0x01, DF_FILE_TYPE.VALUE_FILE, 0x03, 0x10, 0x32, 0x00);
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0x818000, fileId 0x01  result: " + ret.ToString());
                            }
                            //File contect
                            if (ret)
                            {
                                ret = CreateDirectoryContentEx(0x818000, 0, 0, cardvalidity, out kuclimit);
                                if (!ret)
                                {
                                    if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                                    else errocode = (int)E_TP_Errors.ERR_WRITE;//4 
                                }
                            }
                            else errocode = (int)E_TP_Errors.ERR_WRITE;//4 

                        }
                        else
                        {

                            mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out kuclimit);
                            if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                            errocode = (int)E_TP_Errors.ERR_AUTH_FAILURE;//2;
                        }

                    }
                    ret = mCSCDesfireRW.SelectApplication(0xFFFFFF);
                    if (ret)
                    {
                        pSw1 = 0xff; pSw2 = 0xff;
                        keyIndex = 40;//0x28//C.I. (Card Issuer & Application Master key)
                        ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x11, keyIndex, 0x00, 0x00, mCSCDesfireRW.mDiversification, out b_RndAB, out pSw1, out pSw2);
                        Logging.Log(LogLevel.Information, "InitializeCard () Auth appid:0xFFFFFF,Key num:60 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                        if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        {
                            //create files ...
                            pSw1 = 0xff; pSw2 = 0xff;
                            ret = mCSCDesfireRW.CreateFile(0xFFFFFF, 0x08, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x00, 0x00, 0xEF, 0x40);//64bytes
                            Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0xFFFFFF, fileId 0x08  result: " + ret.ToString());
                            if (ret)
                            {
                                ret = mCSCDesfireRW.CreateFile(0xFFFFFF, 0x09, DF_FILE_TYPE.STANDARD_DATA_FILE, 0x03, 0x12, 0x2F, 0x80);//64bytes
                                Logging.Log(LogLevel.Verbose, "InitializeCard () CreateFile, App 0xFFFFFF, fileId 0x09  result: " + ret.ToString());
                            }
                            else errocode = (int)E_TP_Errors.ERR_WRITE;//4

                            //File contect
                            if (ret)
                            {
                                ret = CreateDirectoryContentEx(0xFFFFFF, cardEngNo, artwork, cardvalidity, out kuclimit);
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_EFCardHeader result: " + ret.ToString());
                                if (!ret)
                                {
                                    if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                                    else errocode = (int)E_TP_Errors.ERR_WRITE;//4 
                                }
                            }
                            else errocode = (int)E_TP_Errors.ERR_WRITE;//4

                        }
                        else //errocode = 2;
                        {
                            mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out kuclimit);
                            if (kuclimit) errocode = (int)E_TP_Errors.ERR_KUC_LIMIT_OVER;
                            errocode = (int)E_TP_Errors.ERR_AUTH_FAILURE;//2;
                        }
                    }
                    else
                    {
                        Logging.Log(LogLevel.Error, "InitializeCard, Select appli 0xFFFFFF Failed ....");
                        // errocode = 3;
                        errocode = (int)E_TP_Errors.ERR_READ;//3
                    }
                   
                }//
                ///
                Logging.Log(LogLevel.Verbose, "File content created ... End");
            }
            return ret;
        }
        public bool FormateCard(bool usedefaultkey)
        {
            bool ret = false;
            byte pSw1 = 0xFF, pSw2 = 0xFF;
            byte keyIndex = 104;
            if (!usedefaultkey) keyIndex = 100;
            byte[] b_RndAB;
            //just before .SelectApplication(0x00);
            mCSCDesfireRW.ResetProperties();
            ret = mCSCDesfireRW.SelectApplication(0x00);
            if (ret)
            {
                //a. Authenicate with 104 (card master,aid 0x00 ), then                                   
                ret = mCSCDesfireRW.Authenticate(0x00, 0x00, keyIndex, 0x00, 0x00, null, out b_RndAB, out pSw1, out pSw2);
                if (!ret)
                {
                    //try with other keys
                    if (usedefaultkey)// try with real key 100
                        keyIndex = 100;
                    else //may be card has not changed its keys so try with default keys ...
                        keyIndex = 104;
                    mCSCDesfireRW.ResetProperties();
                    ret = mCSCDesfireRW.Authenticate(0x00, 0x00, keyIndex, 0x00, 0x00, null, out b_RndAB, out pSw1, out pSw2);
                }
               
                if (ret)
                {
                    ret = mCSCDesfireRW.FormateCard();
                }
            }
            return ret;
        }//
        private bool CreateDirectoryContent(int appid, UInt32 cardEngNo, UInt32 artwork, UInt16 cardEndofvalidity, out bool isKUCLimitOver)
        {
            isKUCLimitOver = false;
            bool ret = false;
            byte pSw1 = 0xFF, pSw2 = 0xFF;
            byte[] response;
            Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () In!!! , _IsReaderConnected:" + _IsReaderConnected.ToString() + " appid: " + appid.ToString());
            if (_IsReaderConnected)
            {
                if (appid == 0x00)
                {
                }
                else if (appid == 0xFFFFFF)
                {
                    ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x00, 60, 0, 0x00, null, out response, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0xFFFFFF,Key num:60 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                    if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    {
                        ret = Write_EFCardHeader(cardEngNo, artwork, cardEndofvalidity);
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_EFCardHeader result: " + ret.ToString());
                        if (ret)
                        {
                            //commit txn..
                            ret = mCSCDesfireRW.commitTxn();
                        }
                    }
                    else
                    {
                        ret = false;
                        // bool kuclimit = false;
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Autheticate Failed appid:0xFFFFFF,Key num:60 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));

                    }

                }
                else if (appid == 0x818000)
                {
                    // mCSCDesfireRW.SelectApplication(appid);

                    //As keys are not changed ---Authenticate with default factory key ..

                    ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, /*103*/ 22, 0, 0x02, null, out response, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0x818000,Key num:22 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                    if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    {
                        ret = Write_GATFAT();
                        ret = Write_T_ServiceProvider();
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_T_ServiceProvider result: " + ret.ToString());
                        if (ret)
                        {
                            ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, 103, 0, 0x00, null, out response, out pSw1, out pSw2);
                            if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                            {
                                ret = Write_T_EnvironmentData(cardEndofvalidity);
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_T_EnvironmentData result: " + ret.ToString());
                                // ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, 21, 0,0x01, null, out response, out pSw1, out pSw2);
                                if (ret)
                                {
                                    //commit txn..
                                    ret = mCSCDesfireRW.commitTxn();
                                }
                            }
                            else
                            {
                                ret = false;                               
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Autheticate Failed appid:0x818000,Key num:103 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));

                            }
                        }
                    }//
                    else
                    {
                        ret = false;                      
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Autheticate Failed appid:0x818000,Key num:22 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));

                    }

                }
            }//if (_IsReaderConnected)
            return ret;
        }
        private bool CreateDirectoryContent(int appid, UInt32 cardEngNo, UInt32 artwork)
        {
            bool ret = false;
            byte pSw1 = 0xFF, pSw2 = 0xFF;
            byte[] response;
            Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () In!!! , _IsReaderConnected:" + _IsReaderConnected.ToString() + " appid: " + appid.ToString());
            if (_IsReaderConnected)
            {
                if (appid == 0x00)
                {
                }
                else if (appid == 0xFFFFFF)
                {
                    ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x00, 60, 0, 0x00, null, out response, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0xFFFFFF,Key num:60 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                    DateTime dt = new DateTime();
                    //dt.AddYears(10);
                    UInt16 dosdate = CFunctions.ToDosDate(dt);
                    ret = Write_EFCardHeader(cardEngNo, artwork, dosdate);
                    Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_EFCardHeader result: " + ret.ToString());
                    if (ret)
                    {
                        //commit txn..
                        ret = mCSCDesfireRW.commitTxn();
                    }

                }
                else if (appid == 0x818000)
                {
                    // mCSCDesfireRW.SelectApplication(appid);

                    //As keys are not changed ---Authenticate with default factory key ..

                    ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, /*103*/ 22, 0, 0x02, null, out response, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0x818000,Key num:22 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                    if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    {
                        ret = Write_GATFAT();
                        ret = Write_T_ServiceProvider();
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_T_ServiceProvider result: " + ret.ToString());
                        if (ret)
                        {
                            ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, 103, 0, 0x00, null, out response, out pSw1, out pSw2);
                            if (ret)
                            {
                                DateTime dt = new DateTime();
                                //dt.AddYears(10);
                                UInt16 dosdate = CFunctions.ToDosDate(dt);
                                ret = Write_T_EnvironmentData(dosdate);
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_T_EnvironmentData result: " + ret.ToString());
                                // ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, 21, 0,0x01, null, out response, out pSw1, out pSw2);
                                if (ret)
                                {
                                    //commit txn..
                                    ret = mCSCDesfireRW.commitTxn();
                                }
                            }
                        }
                    }//

                }
            }//if (_IsReaderConnected)
            return ret;
        }
        private bool CreateDirectoryContentEx(int appid, UInt32 cardEngNo, UInt32 artwork, UInt16 cardEndofvalidity, out bool isKUCLimitOver)
        {
            isKUCLimitOver = false;
            bool ret = false;
            byte pSw1 = 0xFF, pSw2 = 0xFF;
            byte[] response;
            Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () In!!! , _IsReaderConnected:" + _IsReaderConnected.ToString() + " appid: " + appid.ToString());
            if (_IsReaderConnected)
            {
                if (appid == 0x00)
                {
                }
                else if (appid == 0xFFFFFF)
                {
                    ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x11, 40, 0, 0x00, mCSCDesfireRW.mDiversification, out response, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0xFFFFFF,Key num:40 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                    if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    {
                        ret = Write_EFCardHeader(cardEngNo, artwork, cardEndofvalidity);
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_EFCardHeader result: " + ret.ToString());
                        if (ret)
                        {
                            //pSw1 = 0xFF;
                            //pSw2 = 0xFF;
                            //ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x11, 41, 0, 0x01, mCSCDesfireRW.mDiversification, out response, out pSw1, out pSw2);
                            //Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0xFFFFFF,Key num:41 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                            //if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                            //{
                            //    ret = W_CM_CardHolderInformation();
                            //    //commit txn..
                            //    if (ret)
                            //    {
                                    ret = false;
                                    ret = mCSCDesfireRW.commitTxn();
                            //    }
                            //}
                        }
                        if(!ret)
                        {                           
                            Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Failed appid:0xFFFFFF , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));

                        }
                    }
                    else
                    {
                        ret = false;
                        // bool kuclimit = false;
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Autheticate Failed appid:0xFFFFFF : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));
                    }

                }
                else if (appid == 0x818000)
                {
                    // mCSCDesfireRW.SelectApplication(appid);

                    //As keys are not changed ---Authenticate with default factory key ..

                    ret = mCSCDesfireRW.Authenticate(0x818000, 0x11, /*103*/ 0x02, 0, 0x02, mCSCDesfireRW.mDiversification, out response, out pSw1, out pSw2);
                    Logging.Log(LogLevel.Information, "Write_EFCardHeader () Auth appid:0x818000,Key num:22 result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
                    if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    {
                        ret = Write_GATFAT();
                        ret = Write_T_ServiceProvider();
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_T_ServiceProvider result: " + ret.ToString());
                        if (ret)
                        {
                            ret = mCSCDesfireRW.Authenticate(0x818000, 0x11, 101, 0, 0x00, mCSCDesfireRW.mDiversification, out response, out pSw1, out pSw2);
                            if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                            {
                                ret = Write_T_EnvironmentData(cardEndofvalidity);
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Write_T_EnvironmentData result: " + ret.ToString());
                                // ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, 21, 0,0x01, null, out response, out pSw1, out pSw2);
                                if (ret)
                                {
                                    //commit txn..
                                    ret = mCSCDesfireRW.commitTxn();
                                }
                            }
                            else
                            {
                                ret = false;
                                Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Autheticate Failed appid:0x818000,Key num:103 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));

                            }
                        }
                    }//
                    else
                    {
                        ret = false;
                        Logging.Log(LogLevel.Verbose, "CreateDirectoryContent () Autheticate Failed appid:0x818000,Key num:22 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out isKUCLimitOver));

                    }

                }
            }//if (_IsReaderConnected)
            return ret;
        }
        private bool Write_EFCardHeader(UInt32 CardengraveNo, UInt32 artwork,UInt16 cardendofvalidity)
        {
            //appip 0xfffff, fileid =0x08

            bool ret = false;
            //  byte[] data = { 0xCC, 0x80, 0x00, 0x00, 0xE3, 0xDA, 0x80, 0x03, 0xC3, 0x37, 0x46, 0xE0, 0x00, 0x00, 0x02, 0x01,0xF8,0x00,0x00,0x80,0x00,0x08 };
            var bitBuffer = new bool[256];
            int index = 0;
            byte pSw1 = 0xff, pSw2 = 0xff;

            Logging.Log(LogLevel.Verbose, "Write_EFCardHeader () In!!! , engNum:" + CardengraveNo.ToString() + "  Artwork:" + artwork.ToString());

            //country 10 bits
            index = CFunctions.ConvertToBitsASN1_UPER(818, index, 10, bitBuffer);

            //Format 20 bits
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 20, bitBuffer);

            //Directory  len 3 bits
            index = CFunctions.ConvertToBitsASN1_UPER(1, index, 3, bitBuffer);

            //App id 20 bits
            index = CFunctions.ConvertToBitsASN1_UPER(818000, index, 20, bitBuffer);

            //App layout 4 bits
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 4, bitBuffer);

            //App Key version 4 bits
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 4, bitBuffer);

            //CardAlternate ID 32 bits 
            index = CFunctions.ConvertToBitsASN1_UPER((int)CardengraveNo, index, 32, bitBuffer);

            //Card ID iteration 8 bits (93)
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 8, bitBuffer);// ??? to be checked 

            index = CFunctions.ConvertToBitsASN1_UPER((int)artwork, index, 16, bitBuffer);// 

            index = CFunctions.ConvertToBitsASN1_UPER(cardendofvalidity, index, 16, bitBuffer);// Endof validity

            index = CFunctions.ConvertToBitsASN1_UPER(1, index, 20, bitBuffer);//Card issuer on 20190305
            index = CFunctions.ConvertToBitsASN1_UPER(1, index, 20, bitBuffer);//Card Retailer 20190305

            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 5, bitBuffer);
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 6, bitBuffer); //To complete to 23 bytes

            //
            byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256); //23*8

            Logging.Log(LogLevel.Verbose, "Write_EFCardHeader () Dat to write: " + BitConverter.ToString(databuff).Replace("-", string.Empty));

            ret = mCSCDesfireRW.WriteDataFile(0x08, (byte)DF_FILE_TYPE.STANDARD_DATA_FILE, false, 0x00, databuff, out pSw1, out pSw2);

            Logging.Log(LogLevel.Information, "Write_EFCardHeader () out, result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");

            return ret;
        }
        //private bool W_EF_CardHolderInformation()//file id 0x09
        //{
        //    //
        //    bool ret = false;
        //    var bitBuffer = new bool[256];

        //    int index = 0;

        //    //Presence bitmap=0
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 5, bitBuffer);

        //    //Holder name 16 bits. No need as presence bit is 0
        //    //index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 39 * 8, bitBuffer);
        //    index = CFunctions.ConvertToBitsASN1_UPER(0, index, 3, bitBuffer); //To complete to 1 byte

        //    byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256); //1*8

        //    return ret;
        //}

        //private bool W_CM_CardHolderInformation()//file id 0x09
        //{
        //    //appip 0xfffff,
        //    bool ret = false;
        //    var bitBuffer = new bool[64];
        //    byte pSw1 = 0xff, pSw2 = 0xff;
        //    int index = 0;
        //    Logging.Log(LogLevel.Verbose, "W_CM_CardHolderInformation () In!!!");
        //    //Presence bitmap=0
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 4, bitBuffer);

        //    //Holder birth year 16 bits
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 16, bitBuffer);

        //    //Holder birth month 8 bits
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 8, bitBuffer);

        //    //Holder birth day 8 bits
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 8, bitBuffer);

        //    //Holder Language
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)'a', index, 7, bitBuffer);
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)'r', index, 7, bitBuffer);
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)'a', index, 7, bitBuffer);

        //    //Holder Passenger Class
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 4, bitBuffer);

        //    //Holder Profile. Lenth is initialised to 0
        //    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 3, bitBuffer);

        //    byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);//8*8
        //    Logging.Log(LogLevel.Verbose, "W_CM_CardHolderInformation () Dat to write: " + BitConverter.ToString(databuff).Replace("-", string.Empty));

        //    ret = mCSCDesfireRW.WriteDataFile(0x09, (byte)DF_FILE_TYPE.STANDARD_DATA_FILE, true, 0x00, databuff, out pSw1, out pSw2);

        //    Logging.Log(LogLevel.Information, "W_CM_CardHolderInformation () out, result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
        //    return ret;
        //}
        private bool Write_T_EnvironmentData(UInt16 cardendofvalidity)
        {
            //appid 0x818000 fileid 0x08
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            var bitBuffer = new bool[256];
            try
            {
                int index = 0;
                Logging.Log(LogLevel.Verbose, "Write_T_EnvironmentData () In!!!");
                //Presence bitmap=0
                index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 4, bitBuffer);
                //envVersion num =0
                index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 6, bitBuffer);
                //envNetworkId
                //index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 8, bitBuffer);
                //index = CFunctions.ConvertToBitsASN1_UPER(818000, index, 20, bitBuffer);
                //index = CFunctions.ConvertToBitsASN1_UPER(0x010101, index, 24, bitBuffer);
                index = CFunctions.ConvertToBitsASN1_UPER(0x818000, index, 24, bitBuffer);

                //currency 16 bits
                index = CFunctions.ConvertToBitsASN1_UPER(Configuration.ReadIntParameter("CardCurrencyByDefault",10264), index, 16, bitBuffer); //0x2818=10264,  0x1818=6168

                //deposite amt 16 bits
                index = CFunctions.ConvertToBitsASN1_UPER(0, index, 16, bitBuffer);

                //application issuer id 16 bits
                index = CFunctions.ConvertToBitsASN1_UPER(1, index, 16, bitBuffer);

                //Dos Date 16 bits
                index = CFunctions.ConvertToBitsASN1_UPER(cardendofvalidity, index, 16, bitBuffer);

                //Appli type 4 bits
                index = CFunctions.ConvertToBitsASN1_UPER(0, index, 4, bitBuffer);

                Logging.Trace("W_CM_CardHolderInformation () In!!!");
                //Presence bitmap=0
                index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 4, bitBuffer);

                //Holder birth year 16 bits
                index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 16, bitBuffer);

                //Holder birth month 8 bits
                index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 8, bitBuffer);

                //Holder birth day 8 bits
                index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 8, bitBuffer);

                //Holder Language
                index = CFunctions.ConvertToBitsASN1_UPER((byte)'a', index, 7, bitBuffer);
                index = CFunctions.ConvertToBitsASN1_UPER((byte)'r', index, 7, bitBuffer);
                index = CFunctions.ConvertToBitsASN1_UPER((byte)'a', index, 7, bitBuffer);

                //Holder Passenger Class
                index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 4, bitBuffer);

                //Holder Profile. Lenth is initialised to 0
                index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 3, bitBuffer);

                Logging.Trace("W_CM_AutoReloadData () In!!!");
                index = CFunctions.ConvertToBitsASN1_UPER(0, index, 1, bitBuffer); //To complete to 13 bytes


                index = CFunctions.ConvertToBitsASN1_UPER(0, index, 1, bitBuffer); //To complete to 21 bytes

                byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);//(21*8)

                Logging.Log(LogLevel.Verbose, "Write_T_EnvironmentData () Dat to write: " + BitConverter.ToString(databuff).Replace("-", string.Empty));
                ret = mCSCDesfireRW.WriteDataFile(0x08, 0x00, true, 0x00, databuff, out pSw1, out pSw2);
                Logging.Log(LogLevel.Information, "Write_T_EnvironmentData () out, result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "Write_T_EnvironmentData () Exception, " + ex.Message);
            }
            return ret;
        }
        private bool Write_GATFAT()
        {
            /// appid 0x818000 file id 0x00 T_FAT
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            var bitBuffer = new bool[256];
            Logging.Log(LogLevel.Verbose, "Write_GATFAT () In!!!");
            int index = 0;

            index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 1, bitBuffer);
            for (int i = 0; i < 8; i++)
            {
                index = CFunctions.ConvertToBitsASN1_UPER((byte)i, index, 4, bitBuffer);
            }
            //TPurseLog
            for (int i = 0; i < 2; i++)
            {
                index = CFunctions.ConvertToBitsASN1_UPER((byte)i, index, 2, bitBuffer);
            }
            //fileApplicationContext
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 1, bitBuffer);
            //fileContractList
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 1, bitBuffer);

            //fileCounter len 8
            for (int i = 0; i < 8; i++)
            {
                index = CFunctions.ConvertToBitsASN1_UPER((byte)i, index, 4, bitBuffer);
            }

            //fileEventLog len 10
            for (int i = 0; i < 10; i++)
            {
                index = CFunctions.ConvertToBitsASN1_UPER((byte)i, index, 4, bitBuffer);
            }

            //fileSpecialEventList
            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 1, bitBuffer);

            //fileEventLog len 6
            for (int i = 0; i < 6; i++)
            {
                index = CFunctions.ConvertToBitsASN1_UPER((byte)i, index, 3, bitBuffer);
            }

            index = CFunctions.ConvertToBitsASN1_UPER(0, index, 6, bitBuffer); //To complete to 17 bytes

            byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);//17*8
            Logging.Log(LogLevel.Verbose, "Write_GATFAT () Dat to write: " + BitConverter.ToString(databuff).Replace("-", string.Empty));

            ret = mCSCDesfireRW.WriteDataFile(0x00, (byte)DF_FILE_TYPE.BACKUP_DATA_FILE, true, 0x00, databuff, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "Write_GATFAT () out, result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
            return ret;
        }
        private bool Write_T_ServiceProvider()
        {

            bool ret = false;
            var bitBuffer = new bool[256];
            byte pSw1 = 0xff, pSw2 = 0xff;
            Logging.Log(LogLevel.Verbose, "Write_T_ServiceProvider () In!!!");
            int index = 0;
            try
            {
                //Application Context 
                {
                    index = CFunctions.ConvertToBitsASN1_UPER((byte)1, index, 4, bitBuffer);

                    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 8, bitBuffer);
                    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 18, bitBuffer);
                    index = CFunctions.ConvertToBitsASN1_UPER((byte)0, index, 18, bitBuffer);
                }
                //TV .. default key is 22
                byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256); //6*8

                Logging.Log(LogLevel.Verbose, "Write_T_ServiceProvider () Dat to write: " + BitConverter.ToString(databuff).Replace("-", string.Empty));

                ret = mCSCDesfireRW.WriteDataFile(0x0A, 0x00, true, 0x00, databuff, out pSw1, out pSw2);
                Logging.Log(LogLevel.Verbose, "Write_T_ServiceProvider () out, result:  pSw1[" + pSw1.ToString("X2") + "] pSw2[" + pSw2.ToString("X2") + "]");
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "Write_T_ServiceProvider () exception " + ex.Message);
            }
            return ret;
        }

        public bool ChangeCardKey(byte oldKeyNum, byte oldkeyver, byte newkeyNum, byte newKeyVer, out bool IsSAMKUCLimitOver)
        {
            IsSAMKUCLimitOver = false;
            bool ret = false;

            return ret;
        }
        public bool ChangeCardKeys(out bool IsSAMKUCLimitOver)
        {
            IsSAMKUCLimitOver = false;
            bool ret = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            byte[] response;
            Logging.Log(LogLevel.Verbose, "ChangeCardKeys() In");
            if (_IsReaderConnected)
            {

                //1.  change all keys of  0x818000
                //2. change all keys of  0xFFFFFF
                //3. change Master key of the card....

                pSw1 = 0xff; pSw2 = 0xff;
                ret = mCSCDesfireRW.SelectApplication(0x818000);
                if (ret)
                {
                    Logging.Log(LogLevel.Verbose, "Application 0x818000 Selected ");
                    ret = mCSCDesfireRW.Authenticate(0x818000, 0x00, 103, 0x00, null, out response, out pSw1, out pSw2);
                    if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    {
                        pSw1 = 0xff; pSw2 = 0xff;
                        ret = mCSCDesfireRW.ChangeKey(0x22, 23, 0x00, 0x03, 0, 0x03, mCSCDesfireRW.mDiversification);
                        Logging.Log(LogLevel.Verbose, "ChangeKey Key Num:23  reult:" + ret.ToString());
                        if (ret)
                        {
                            ret = mCSCDesfireRW.ChangeKey(0x22, 22, 0x00, 0x02, 0, 0x02, mCSCDesfireRW.mDiversification);
                            Logging.Log(LogLevel.Verbose, "ChangeKey Key Num:22  reult:" + ret.ToString());
                        }

                        if (ret)
                        {
                            ret = mCSCDesfireRW.ChangeKey(0x22, 21, 0x00, 0x01, 0, 0x01, mCSCDesfireRW.mDiversification);
                            Logging.Log(LogLevel.Verbose, "ChangeKey Key Num:21  reult:" + ret.ToString());
                        }


                        if (ret)
                        {
                            Logging.Log(LogLevel.Information, "ChangeKey Master key 103 t 101....");
                            ret = mCSCDesfireRW.ChangeKey(0x23, 103, 0x00, 0x65, 0x00, 0x00, mCSCDesfireRW.mDiversification);//application master key
                            Logging.Log(LogLevel.Information, "ChangeKey Master key 103 to 101..result " + ret.ToString());

                        }


                    }
                    if (!ret)
                    {
                        Logging.Log(LogLevel.Error, "ChangeKey Failed  for app 0x818000");                    
                        ret = false;
                        // bool kuclimit = false;
                        Logging.Log(LogLevel.Verbose, "ChangeKey () Autheticate Failed appid:0x818000,Key num:103 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out IsSAMKUCLimitOver));
                   
                    }
                }
                else Logging.Log(LogLevel.Error, "ChangeCardKeys () Application 0x818000 selection failed !!!");

                if (ret)
                {
                    pSw1 = 0xff; pSw2 = 0xff;
                    ret = mCSCDesfireRW.SelectApplication(0xFFFFFF);
                    if (ret)
                    {
                        ret = mCSCDesfireRW.Authenticate(0xFFFFFF, 0x00, 60, 0x00, null, out response, out pSw1, out pSw2);
                        if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        {
                            pSw1 = 0xff; pSw2 = 0xff;

                            {
                                ret = mCSCDesfireRW.ChangeKey(0x22, 62, 0x00, 42, 0x00, 0x02, mCSCDesfireRW.mDiversification);
                                Logging.Log(LogLevel.Verbose, "ChangeKey Key Num:62 ->42  result:" + ret.ToString());
                            }
                            if (ret)
                            {
                                ret = mCSCDesfireRW.ChangeKey(0x22, 61, 0x00, 41, 0x00, 0x01, mCSCDesfireRW.mDiversification);
                                Logging.Log(LogLevel.Verbose, "ChangeKey Key Num:61->41  result:" + ret.ToString());
                            }
                            if (ret)
                            {
                                ret = mCSCDesfireRW.ChangeKey(0x23, 60, 0x00, 40, 0x00, 0x00, mCSCDesfireRW.mDiversification);//application master key
                                Logging.Log(LogLevel.Information, "ChangeKey Key Num:60->40  result:" + ret.ToString());
                            }

                        }//if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        else
                        {
                            Logging.Log(LogLevel.Error, "ChangeKey Failed  for app 0xFFFFFF");
                            ret = false;
                            // bool kuclimit = false;
                            Logging.Log(LogLevel.Verbose, "ChangeKey () Autheticate Failed appid:0xFFFFFF,Key num:60 , : " + mCSCDesfireRW.CheckAuthFailure(pSw1, pSw2, out IsSAMKUCLimitOver));

                        }
                    }//ret
                    else Logging.Log(LogLevel.Error, "ChangeCardKeys () Application 0xFFFFFF selection failed !!!");
                }

                if (ret) //change Card master key
                {
                    Logging.Log(LogLevel.Warning, "ChangeCardKeys () for Card Master key Skipped !!!");
                    pSw1 = 0xff; pSw2 = 0xff;
                    /*
                    ret = mCSCDesfireRW.SelectApplication(0x00);
                    if (ret)
                    {
                        ret = mCSCDesfireRW.Authenticate(0x00, 0x00, 104, 0x00, null, out response, out pSw1, out pSw2);
                        if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                        {
                            pSw1 = 0xff; pSw2 = 0xff;
                            ret = mCSCDesfireRW.ChangeKey(0x23, 104, 0x00, 100, 0x00, 0x00, mCSCDesfireRW.mDiversification);//application master key
                        }//if (ret && (pSw1 == 0x90 || pSw1 == 0x91) && pSw2 == 0x00)
                    }
                     */
                }

            }//if (_IsReaderConnected)
            else Logging.Log(LogLevel.Error, "ChangeCardKeys () Reader Not connected!!!");
            return ret;
        }
        private bool ChangeKeySettings(byte KeyEntry, byte keySettings)
        {
            bool ret = false;
            return ret;
        }
        public bool CheckKUCQuota(byte bbKUCKeyNum, long KUCthreshold, out long currCount, out long currQuota, out bool isReloadNeeded)
        {
            bool ret = false;
            byte pSw1 = 0xFF, pSw2 = 0xff;
            isReloadNeeded = false;
            currCount = 0;
            currQuota = 0;
           ret= mCSCDesfireRW.SAM_GetKUCQuota(bbKUCKeyNum, out currQuota, out currCount);
           if (ret && (currCount>= KUCthreshold) && currQuota>0)
           {
               isReloadNeeded = true;
           }
            return ret;
        }
    }
}
