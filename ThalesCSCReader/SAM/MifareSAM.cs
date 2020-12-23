using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
//using IFS2.Equipment.TicketingRules.CONSTANT;


namespace IFS2.Equipment.TicketingRules
{
    enum AVMode
    {
        AV1,
        AV2
    }
    public class MifareSAM
    {
        IReaderInterface mIReaderInterface;
        CONSTANT.SAMType eSAMType;
        AVMode opmode = 0;
        int msamslot = 1;
        const byte MIFARE_SAM_CLS = 0x80;
        const byte MIFARE_SAM_INS_AUTH_HOST = 0xA4;
        const byte MIFARE_SAM_INS_GETVER = 0x60;
        const byte MIFARE_SAM_INS_GET_KEYENTRY = 0x64;
        const byte MIFARE_SAM_INS_GET_KUCENTRY = 0x60;
        const byte MIFARE_SAM_INS_KILL_AUTH = 0xCA;
        const byte MIFARE_SAM_INS_ENCRIPT = 0xED;


        const byte MIFARE_SAM_INS_AUTH_PICC = 0x0A;
        const byte MIFARE_SAM_INS_CHG_KEY_PICC = 0xC4;
        byte[] bRandA = { 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8 };
        SecurityMgr mSecurityMgr;
        public bool _isSAMActivated;
        
        public MifareSAM(IReaderInterface pIReaderInterface,int samType, int mode, int samslot)
        {
            mIReaderInterface = pIReaderInterface;
            eSAMType =(CONSTANT.SAMType) samType;
            opmode =(AVMode) mode;
            msamslot = samslot;
            _isSAMActivated = false;
            mSecurityMgr = new SecurityMgr();
        }

        public bool ActivateSAM(byte[] key, byte authmode, byte keyno, byte keyver, out byte pSw1, out byte pSw2)
        {
            //bool ret = false;
            byte[] encripted_Rndb, encripted_RndA_dash;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            _isSAMActivated = false;
            this.SAM_AuthenticateHostPart1(authmode, keyno, keyver, null, out encripted_Rndb, out pSw1, out pSw2);
            if (pSw1 == 0x90 && pSw2 == 0xAF)
            {
                pSw1 = 0xFF;
                pSw2 = 0xFF;
                byte[] rndab = mSecurityMgr.CalculateRndAB_AV1(bRandA, encripted_Rndb, key);
                this.SAM_AuthenticateHostStep2(rndab, out encripted_RndA_dash, out pSw1, out pSw2);
                if (pSw1 == 0x90 && pSw2 == 0x00)
                {
                    _isSAMActivated = true;
                }
            }
            return _isSAMActivated;
        }
        public bool AuthenicateSAMwithKUC(byte[] kuckey, byte authmode, byte keyno, byte keyver, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
             pSw1 = 0xFF;
            pSw2 = 0xFF;
            if (false)
            {
               
                byte[] encripted_Rndb, encripted_RndA_dash;

                this.SAM_AuthenticateHostPart1(authmode, keyno, keyver, null, out encripted_Rndb, out pSw1, out pSw2);
                if (pSw1 == 0x90 && pSw2 == 0xAF)
                {
                    pSw1 = 0xFF;
                    pSw2 = 0xFF;
                    byte[] rndab = mSecurityMgr.CalculateRndAB_AV1(bRandA, encripted_Rndb, kuckey);
                    this.SAM_AuthenticateHostStep2(rndab, out encripted_RndA_dash, out pSw1, out pSw2);
                    if (pSw1 == 0x90 && pSw2 == 0x00)
                    {
                        ret = true;
                    }
                }
            }
            else ret = ActivateSAM(kuckey, authmode, keyno, keyver, out pSw1, out pSw2);
            return ret;
        }
        public bool SAM_GetVersion(out NXP_SAM_Info verinfo, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
           // byte[] response;
            byte [] bverinfo = new byte[1];
            verinfo = null;
            if (mIReaderInterface.IsReaderConnected())
            {
                byte[] apdu_getver = {MIFARE_SAM_CLS,MIFARE_SAM_INS_GETVER, 0x00,0x00,0x00 }; //CLA, INS, P1,P2,LE

                ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu_getver, out pSw1, out pSw2, out bverinfo);
                if (ret && pSw1 == 0x90 && pSw2 == 0x00)
                {
                    if (bverinfo.Length > 29)
                    {
                        try
                        {
                            verinfo = new NXP_SAM_Info();
                            verinfo.VendorId = bverinfo[0];
                            verinfo.MajorNum = bverinfo[3];
                            verinfo.MinorNum = bverinfo[4];
                            Array.Copy(bverinfo, 14, verinfo.SerialNum, 0, 7);
                            verinfo.CryptoSettings = bverinfo[29];
                            verinfo.Mode = bverinfo[30];
                        }
                        catch(Exception ex)
                        {
                            
                        }
                    }
                }
            }
            return ret;
        }
        public bool SAM_GetKeyEntry(byte KeyEntryNum, out byte[] keyDetails, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xFF; pSw2 = 0xFF;
            // byte[] response;
            byte[] apdu = CFunctions.getApdu(0x80, 0x64, KeyEntryNum, 0x00, 0x00);            
            keyDetails = new byte[1];
            if (mIReaderInterface.IsReaderConnected())
            {
                ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out keyDetails);
            }
            return ret;
        }
        //page 52-53... SAMAV2.pdf
        public bool SAM_GetKUCEntry(byte KeyEntryNum, out byte[] kucDetails, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xFF; pSw2 = 0xFF;
            // byte[] response;
            kucDetails = new byte[1];
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] apdu = CFunctions.getApdu(0x80, 0x6C, KeyEntryNum, 0x00, 0x00);

            if (mIReaderInterface.IsReaderConnected())
            {
                ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out kucDetails);
            }

            return ret;
        }
        public bool SAM_ChangeKUCEntry(byte KeyNumKUCEntry,bool bupdateLimit,bool bUpdateKeyNoKUC,bool bupdatKeyVersion, byte[] dataIn, out byte pSw1, out byte pSw2 )
        {
            bool ret = false;
            pSw1 = 0xFF; pSw2 = 0xFF;
            byte P2 = 0x00;
            byte[] response;
            if (bupdateLimit) P2 = 0x80;
            if (bUpdateKeyNoKUC) P2 |= 0x40;
            if (bupdatKeyVersion) P2 |= 0x20;

          //  byte[] apdu = CFunctions.getApdu(0x80, 0xCC, KeyNumKUCEntry, P2, 0x00);
            byte[] apdu = CFunctions.getApdu(MIFARE_SAM_CLS, 0xCC, KeyNumKUCEntry, P2, dataIn, 0x00);
            if (mIReaderInterface.IsReaderConnected())
            {
                ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out response);
            }
            return ret;
        }
        private bool SAM_AuthenticateHostPart1(byte authmode, byte keyno, byte keyver, byte[] bdivInp, out byte[] encrypt_bRndB, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xFF; pSw2 = 0xFF;
            // byte[] response;
            encrypt_bRndB = new byte[1];

            if (opmode == AVMode.AV1)
            {
                byte[] data;
                if ((authmode & 0x01) > 0 && bdivInp.Length>0)
                {
                    data = new byte[2 + bdivInp.Length];
                    data[0] = keyno;
                    data[1] = keyver;
                    Array.Copy(bdivInp, 0, data, 2, bdivInp.Length);
                }
                else
                {
                    data = new byte[2];
                    data[0] = keyno;
                    data[1] = keyver;
                }
                // = { MIFARE_SAM_CLS, MIFARE_SAM_INS_AUTH_HOST, authmode, 0x00, };
                byte[] apdu_auth1 = CFunctions.getApdu(MIFARE_SAM_CLS, MIFARE_SAM_INS_AUTH_HOST, authmode, 0x00, data, 0x00);

                ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu_auth1, out pSw1, out pSw2, out encrypt_bRndB);
            }

            return ret;
        }
        private bool SAM_AuthenticateHostStep2(byte[] in_ciphered_RndAB, out byte[] outRndA, out byte pSw1, out byte pSw2)
        {
           bool Err;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            outRndA = new byte[1];
            byte[] apdu = new byte[in_ciphered_RndAB.Length + 6];
            apdu[0] = 0x80;
            apdu[1] = 0xA4;
            apdu[2] = 0x00;
            apdu[3] = 0x00;
            apdu[4] = (byte)in_ciphered_RndAB.Length;
            Array.Copy(in_ciphered_RndAB, 0, apdu, 5, in_ciphered_RndAB.Length);
            apdu[apdu.Length - 1] = 0x00;
            // byte[] apdu = CFunctions.getApdu(0x80, 0xA4, 0x00, 0x00, in_ciphered_RndAB, 0x00);

            Err = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out outRndA);
            if (pSw1 == 0x90 && pSw2 == 0x00)
            {
                return Err;
            }
            else return false;
        }
        public bool SAM_KillAuthentication(out byte pSw1, out byte pSw2)
        {
            bool Err = false ;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] apdu = { 0x80, 0xCA, 0x00, 0x00 };
            byte[] response = new byte[1];

            Err = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out response);

            return Err;
        }
        public bool SAM_AuthenticatePICC_Step1(byte authmode, byte[] bRndB_crpt, byte keyNum, byte keyver, byte[] bdivInp, out byte[] en_RanA_RndB, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xFF; pSw2 = 0xFF;
            
            //if authmode b:0 , is '0' i.e. no key diversification will be used , so param bdivInp will be ignored ...
            //authmode b:1 , is '0' then keyno is key entry number of SAM else keyno is Desfire Key number
            //b:4, is '0' , use AV1 compatibility mode key diversification else AV2
            en_RanA_RndB = new byte[1];

            /*
            byte[] apdu = new byte[bRndB_crpt.Length + 6 + 2];

            apdu[0] = 0x80;
            apdu[1] = 0x0A;
            apdu[2] = authmode;// authentication mode
            apdu[3] = 0x00;//P2
            apdu[4] = (byte)(bRndB_crpt.Length + 2);//keyno+keyver
            apdu[5] = keyNum;
            apdu[6] = keyver;
            Array.Copy(bRndB_crpt, 0, apdu, 7, bRndB_crpt.Length);
            apdu[apdu.Length - 1] = 0x00;
            */
            byte[] data;
            if ((authmode & 0x01) > 0 && bdivInp!=null)
            {
                data = new byte[2 + bdivInp.Length + bRndB_crpt.Length];
                data[0] = keyNum;
                data[1] = keyver;
                Array.Copy(bRndB_crpt, 0, data, 2, bRndB_crpt.Length);
                Array.Copy(bdivInp, 0, data, 2 + bRndB_crpt.Length, bdivInp.Length);
            }
            else
            {
                data = new byte[2 + bRndB_crpt.Length];
                data[0] = keyNum;
                data[1] = keyver;
                Array.Copy(bRndB_crpt, 0, data, 2, bRndB_crpt.Length);
            }
            
            byte[] apdu_auth1 = CFunctions.getApdu(MIFARE_SAM_CLS, MIFARE_SAM_INS_AUTH_PICC, authmode, 0x00, data, 0x00);
            ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu_auth1, out pSw1, out pSw2, out en_RanA_RndB);
            if (pSw1 == 0x90 && pSw2 == 0xAF)
            {
                ret=true;
            }

            return ret;
        }
        public bool SAM_AuthenticatePICC_Step2(byte[] ciphered_RndA_dash, out byte pSw1, out byte pSw2)
        {
            bool Err=false;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] response = new byte[1];
            byte[] apdu = new byte[ciphered_RndA_dash.Length + 5];
            apdu[0] = 0x80;
            apdu[1] = 0x0A;
            apdu[2] = 0x00;
            apdu[3] = 0x00;
            apdu[4] = (byte)ciphered_RndA_dash.Length;
            Array.Copy(ciphered_RndA_dash, 0, apdu, 5, ciphered_RndA_dash.Length);
            //apdu[apdu.Length - 1] = 0x00;
            // byte[] apdu = CFunctions.getApdu(0x80, 0xA4, 0x00, 0x00, in_ciphered_RndAB, 0x00);

            Err = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out response);
            if (pSw1 == 0x90 && pSw2 == 0x00)
            {
                Err = true;
            }
            else Err = false;
            return Err;
        }

        public bool SAM_EncryptData(byte[] datain, out byte[] outEnData, out byte pSw1, out byte pSw2)
        {
            // logic for big data encryption is required to be added.... 
            bool Err= false;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            outEnData = new byte[1];

            byte[] apdu = new byte[datain.Length + 6];
            apdu[0] = 0x80;
            apdu[1] = 0xED;
            apdu[2] = 0x00;// full frame
            apdu[3] = 0x00;//offset of the data 
            apdu[4] = (byte)datain.Length;
            Array.Copy(datain, 0, apdu, 5, datain.Length);
            apdu[apdu.Length - 1] = 0x00;
            Err = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu, out pSw1, out pSw2, out outEnData);
            if (pSw1 == 0x90 && pSw2 == 0x00)
            {
                 Err= true;
            }
            else Err=false;
            return Err;
        }

        public bool SAM_ChangeKey(byte keyConfMethod, byte oldkeyEntry, byte oldKeyver, byte newKeyEntry, byte newkeyver, byte cardKeyNum, byte[] DivIn,out byte[] cryptogram, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            pSw1 = 0xff;
            pSw2 = 0xff;
            byte cnf = cardKeyNum;
            byte[] data;
            cryptogram = new byte[1];
            if (cardKeyNum == 0x00)// 0x00 is master key index of DF card..
                cnf |= 0x10;
            try
            {
                if ((keyConfMethod & 0x06) > 0 && DivIn != null)
                {
                    data = new byte[4 + DivIn.Length];
                    Array.Copy(DivIn, 0, data, 4, DivIn.Length);
                }
                else
                {
                    data = new byte[4];
                }
                data[0] = oldkeyEntry;
                data[1] = oldKeyver;
                data[2] = newKeyEntry;
                data[3] = newkeyver;

                byte[] apdu_changekey = CFunctions.getApdu(MIFARE_SAM_CLS, MIFARE_SAM_INS_CHG_KEY_PICC, keyConfMethod, cnf, data, 0x00);

                ret = mIReaderInterface.IsoCommandExe((DEST_TYPE)msamslot, apdu_changekey, out pSw1, out pSw2, out cryptogram);
                if (pSw1 == 0x90 && pSw2 == 0x00)
                {
                    ret = true;
                }
                else ret = false;
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }

        public string SAM_CheckAuthFailureResponse(byte pSw1, byte pSw2, out bool IsQuotaKUCReached)
        {
            string err_description = "";
            IsQuotaKUCReached = false;
            if (pSw1 == 0x69)
            {
                if (pSw2 == 0x84) err_description = "Key Entry or KUC not valid";
                else if (pSw2 == 0x85)
                {
                    IsQuotaKUCReached = true;
                    err_description = "KUC Quota limit reached";
                }
            }
            else if (pSw1 == 0x65 && pSw2== 0x81) err_description = "Memory Failuer, KUC could not be updated";
            else if (pSw1 == 0x6A)
            {
                if (pSw2 == 0x80) err_description = "Incorrect parameters in command data field";
                else if (pSw2 == 0x82) err_description = "Key version not found";
            }
            return err_description;
        }      
    }
}
