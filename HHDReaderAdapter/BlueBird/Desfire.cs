using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IFS2.Equipment.CSCReaderAdaptor
{
   public class Desfire : ISO14443
    {
        byte[] abyWriteData;
        byte[] arOldKeyEntry;
        byte[] arNewKeyEntryA;
        byte[] arNewKeyEntryB;
        byte[] arNewKeyEntryC;
        byte[] arSamMasterKey;
        byte[] arSamNewKey;
        byte byProMas;
       int m_MediaDetected=0;
       bool isRfTagDetected = false;
        public byte[] old_cardUid,mATQA;
        public enum E_ReaderState
       {
            NONE=0,
            IDLE,
            READY_POLLON,
            ACTIVE_CARDON,
            REMOVAL_DETECTION,
            HALT
       };

        E_ReaderState m_readerState = E_ReaderState.NONE;
        public Desfire(ref RFIDReader clsRFReader)
            : base(ref clsRFReader)
        {
            abyWriteData = new byte[10] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a };
            arSamMasterKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            arSamNewKey = new byte[16] { 0x2, 0x14, 0x26, 0x38, 0x42, 0x54, 0x66, 0x78, 0x82, 0x94, 0xa6, 0xb8, 0xc2, 0xd4, 0xe6, 0xf8 };
            arOldKeyEntry = new byte[57];
            arNewKeyEntryA = new byte[57];
            arNewKeyEntryB = new byte[57];
            arNewKeyEntryC = new byte[57];
            byProMas = 0;
            old_cardUid = new byte[8];
            m_readerState = E_ReaderState.NONE;
            m_MediaDetected = 0;
            mATQA = new byte[2];
            isRfTagDetected = false;
        }

        public E_ReaderState GetReaderState()
        {
            return m_readerState;
        }
        public void SetReaderState(E_ReaderState nState)
        {
            m_readerState = nState;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="abyBuf"></param>
       /// <param name="nData"></param>
       /// <param name="nIndexint"></param>
       /// <param name="nCopySize"></param>
        void CopyToBytes(byte[] abyBuf, int nData, int nIndexint, int nCopySize)
        {
            byte[] abyData = BitConverter.GetBytes(nData);
            Array.Copy(abyData, 0, abyBuf, nIndexint, nCopySize);
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="uid"></param>
       /// <returns></returns>
        //public bool DetectCard(out byte[] uid)
        //{
        //    bool bRet = false;
            
        //    if (REQA()
        //               && Multilist()
        //        /* && RATS(0x01)*/
        //               && Select(m_abyResBuf)
        //        )
        //    {
        //        uid = new byte[m_abyResBuf[0]];
        //        Array.Copy(m_abyResBuf, 1, uid, 0, m_abyResBuf[0]);
        //        bRet = true;
        //    }
        //    else
        //    {
        //        uid = new byte[1];
        //        uid[0] = 0;
        //    }
        //    return bRet;
        //}
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool ActivateReaderForDetection()
        {
            bool bRet = false;
            Thread.Sleep(400);
#if _BIP1300_
            bRet = REQA();
            if (bRet)
            {
                m_readerState = E_ReaderState.READY_POLLON;
                if (m_abyResBuf[0] == 2)
                {
                    mATQA[0] = m_abyResBuf[1];
                    mATQA[1] = m_abyResBuf[2];
                    isRfTagDetected = true;
                }
            }
            else
            {
                isRfTagDetected = false;
                if (m_abyResBuf[1] > 0 && m_abyResBuf[1] == 0xFB) bRet = true;//No RF card in the range
            }
#elif _BIP1500_
            if (!WUPA() && !REQA())
            {
                bRet = false;
                isRfTagDetected = false;
            }
            else
            {
                bRet = true;
                m_readerState = E_ReaderState.READY_POLLON;
                if (m_abyResBuf[0] == 2)
                {
                    mATQA[0] = m_abyResBuf[1];
                    mATQA[1] = m_abyResBuf[2];
                    isRfTagDetected = true;
                }
            }
#endif
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool ResetField()
        {
            bool bRet = false;

          bRet=  m_RFReader.ResetField();
          if (bRet) m_readerState = E_ReaderState.IDLE ;
          bRet=m_RFReader.SetTagType((byte)'a');
            Thread.Sleep(200);
            bRet = REQA();
            if (bRet)
            {
                m_readerState = E_ReaderState.READY_POLLON;
                isRfTagDetected = true;
            }
            else
            {
                isRfTagDetected = false;
                if (m_abyResBuf[1] > 0 && m_abyResBuf[1] == 0xFB) bRet = true;//No RF card in the range
            }
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool ResetFieldEx()
        {
            bool bRet = false;

            bRet = m_RFReader.ResetField();
            if (bRet)
            {
                m_readerState = E_ReaderState.IDLE;
                bRet = m_RFReader.SetTagType((byte)'a');
                Thread.Sleep(200);
            }
#if _BIP1300_
            bRet = REQA();
            if (bRet)
            {
                m_readerState = E_ReaderState.READY_POLLON;
                if (m_abyResBuf[0] == 2)
                {
                    mATQA[0] = m_abyResBuf[1];
                    mATQA[1] = m_abyResBuf[2];
                    isRfTagDetected = true;
                }
            }
            else
            {
                isRfTagDetected = false;
                if (m_abyResBuf[1] > 0 && m_abyResBuf[1] == 0xFB) bRet = true;//No RF card in the range
            }
#elif _BIP1500_
           // bRet = ActivateReaderForDetection();
            isRfTagDetected = false;
#endif
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="uid"></param>
       /// <returns></returns>
       public bool DetectCard(out byte[] uid)
        {            
            bool bRet = false;
            int count = 0;
           
            bRet = Multilist();
            //       && Select(m_abyResBuf)
            //)
            if (bRet)
            {
                uid = new byte[m_abyResBuf[0]];
                if (m_abyResBuf[0] < 4) bRet = false;
                Array.Copy(m_abyResBuf, 1, uid, 0, m_abyResBuf[0]);
                bRet = true;
                m_MediaDetected = 1;
            }
            else
            {
                bRet = false;
                uid = new byte[1];
                uid[0] = 0;
                m_MediaDetected = 0;
            }
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="uAtr"></param>
       /// <param name="MediaDetcted"></param>
       /// <returns></returns>
        public bool DetectCardEx(out byte[] uAtr, out int MediaDetcted)
        {

            //uid = new byte[1];
            bool bRet = false;          
            int count = 0, index = 0;
            byte byUIDLen = 0;
            byte bySAK = 0;
            byte[] byUID = new byte[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] byUID1 = new byte[10];
            Thread.Sleep(200);
            if (isRfTagDetected == false) ActivateReaderForDetection();
            Thread.Sleep(50);
#if _BIP1300_
            bRet = Multilist();
            //       && Select(m_abyResBuf)
            //)
            if (bRet)
            {
                uAtr = new byte[m_abyResBuf[0] + 2];
                if (m_abyResBuf[0] < 4)
                {
                    bRet = false;
                    m_MediaDetected = 0;
                }
                else
                {
                    bRet = true;
                    m_MediaDetected = 1;
                }
                Array.Copy(mATQA, 0, uAtr, 0, 2);
                Array.Copy(m_abyResBuf, 1, uAtr, 2, m_abyResBuf[0]);
              //  Array.Copy(m_abyResBuf, 1, mATQA, 2, m_abyResBuf[0]);
               
            }

            else
            {
                bRet = false;
                uAtr = new byte[1];
                uAtr[0] = 0;
                m_MediaDetected = 0;
                isRfTagDetected = false;
            }
#elif _BIP1500_
            uAtr = new byte[2];
             Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
             for (int i = 0; i < 3; i++)
             {
                 if (Anticollision((byte)(i + 1), ref byUIDLen, byUID) &&
                     SelectGetSAK((byte)(i + 1), byUIDLen, byUID, ref bySAK))
                 {
                     if (byUIDLen > 3)
                     {
                         if (byUID[0] == 0x88)// UID is partial..
                         {
                             Array.Copy(byUID, 1, byUID1, index, byUIDLen - 1);
                             index += byUIDLen - 1;
                         }
                         else
                         {
                             if (i > 0)
                                 Array.Copy(byUID, 0, byUID1, index, byUIDLen);
                             else Array.Copy(byUID, 0, byUID1, index, byUIDLen - 1);
                             index += byUIDLen;
                             // bRet = true;
                             //break;
                         }
                     }
                     // SAK Value Information
                     // Cascade level check bit
                     // XXXX X1XX : Serial number is not complete.
                     // XXXX X0XX : Serial number is complete.

                     // ISO14443-4 compliant check bit
                     // XX1X X0XX : Card is ISO14443-4 compliant.
                     // XX0X X0XX : Card is not ISO14443-4 compliant.
                     if ((bySAK & 0x04) == 0)
                     {
                         bRet = true;
                         break;
                     }
                 }
                 else bRet = false;

             }
             if (bRet)
             {
                 uAtr = new byte[index+2];
                 //if (m_abyResBuf[0] < 4) bRet = false;
                 Array.Copy(mATQA, 0, uAtr, 0, 2);
                 if (index >= 5)
                 {                  
                    
                     Array.Copy(byUID1, 0, uAtr, 2, index);
                     bRet = true;
                     m_MediaDetected = 1;
                 }
                 else
                 {
                     bRet = false;
                      m_MediaDetected = 0;
                 }                
             }

             else
             {
                 bRet = false;               
                 m_MediaDetected = 0;
             }           

#endif
            MediaDetcted = m_MediaDetected;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="uid"></param>
       /// <returns></returns>
        public bool SelectCard(byte[] uid)
        {
            return Select(m_abyResBuf);
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="selectcard"></param>
       /// <param name="detectionState"></param>
       /// <param name="bSameMedia"></param>
       /// <param name="uid"></param>
       /// <returns></returns>
        public bool DetectCard(bool selectcard, out int detectionState,
            out bool bSameMedia, // to be used selectivly only when detectionState != NONE
            out byte[] uid)
        {
            bool ret=false;
            int cardDetected = 0;
            detectionState = 0;
          /*  if (m_readerState == E_ReaderState.NONE || m_readerState == E_ReaderState.HALT)
            {
                
               ret= REQA(out cardDetected);
               m_readerState = E_ReaderState.ACTIVE;
            }
            //else if (m_readerState == E_ReaderState.CARD_ON) // copy same uid 
            //{
            //    uid = new byte[8];
            //    Array.Copy(old_cardUid, uid, 8);
            //    bSameMedia = true;
            //    detectionState = (int)m_readerState;
            //    m_MediaDetected = 1;
            //    return true;
            //}
            else
            {
                ret = true;
            }
            if (m_readerState == E_ReaderState.ACTIVE || m_readerState == E_ReaderState.REMOVAL_DETECTION )*/

            {
                ret = REQA(out cardDetected);
                Thread.Sleep(200);
                if (ret) ret = Multilist();
            }
                // if (ret && Multilist())
            if (ret)// && Anticollision(0x01))
            {
                ret = true;
                detectionState = (int)m_readerState;
                uid = new byte[m_abyResBuf[0]];
                if (m_abyResBuf[0] <= 2 && m_abyResBuf[1] == 0xFB) //no card
                {
                    uid = new byte[1];
                    uid[0] = 0;
                    detectionState = 0;
                    bSameMedia = false;
                    m_MediaDetected = 0;
                    Array.Clear(old_cardUid, 0, old_cardUid.Length);
                }
                else
                {

                    Array.Copy(m_abyResBuf, 1, uid, 0, m_abyResBuf[0]);

                    // bRet = true;
                    //if (old_cardUid.Equals(uid)) bSameMedia=true;
                    if (Compare(old_cardUid, uid) == true)
                    {
                        bSameMedia = true;
                    }
                    else
                    {
                        m_MediaDetected = 1;
                        Array.Clear(old_cardUid, 0, old_cardUid.Length);
                        Array.Copy(uid, old_cardUid, uid.Length);
                        bSameMedia = false;
                        if (selectcard)
                        {
                            m_readerState = E_ReaderState.ACTIVE_CARDON;
                            ret = Select(uid);
                        }

                    }
                }
                Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);
            }
            else
            {
                uid = new byte[1];
                uid[0] = 0;
                detectionState = 0;
                bSameMedia = false;
                Array.Clear(old_cardUid, 0, old_cardUid.Length);
                m_MediaDetected = 0;
            }
            
            return ret;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool HaltCardA()
        {
            bool ret = false;
            ret = base.HaltA();
            if (ret) m_readerState = E_ReaderState.HALT;
            return ret;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool SwitchToDetectionRemoval()
        {
            bool ret = false;
            m_readerState = E_ReaderState.REMOVAL_DETECTION;
            return ret;
        }
        public bool SwitchToCardOn()
        {
            bool ret = false;
            m_readerState = E_ReaderState.ACTIVE_CARDON;
            return ret;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool SwitchToPollOn()
        {
            bool ret = false;
          ret=  ResetFieldEx();
            return ret;
        }

        public int IsMediaDetected()
        {
            return m_MediaDetected;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool Authenticate()
        {
            bool ret = false;
            byte[] cmd_Authenticate = { 0x90, 0x0A, 0x00, 0x00, 0x01, 0x00, 0x00 };
            byte[] m_response;
            ret = base.ExchangeAPDU((byte)cmd_Authenticate.Length, cmd_Authenticate, out m_response);

            return ret;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="mRndB"></param>
       /// <param name="KeyNo"></param>
       /// <returns></returns>
        public bool Authenticate(out byte[] mRndB, byte KeyNo)
        {
            bool ret = false;
            byte[] cmd_Authenticate = { 0x90, 0x0A, 0x00, 0x00, 0x01, KeyNo, 0x00 };//{ 0x90, 0x0A, 0x00, 0x00, 0x01, 0x01, 0x00 }
            byte[] m_response;
            ret = base.ExchangeAPDU((byte)cmd_Authenticate.Length, cmd_Authenticate, out m_response);
            if (ret && m_response.Length >= 8)
            {
                mRndB = new byte[8];
                Array.Copy(m_response, 0, mRndB, 0, 8);
            }
            else
            {
                mRndB = new byte[1];
                ret = false;
            }

            return ret;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="mRndB"></param>
       /// <param name="KeyNo"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns>
        public bool Authenticate(out byte[] mRndB, byte KeyNo, out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte[] cmd_Authenticate = { 0x90, 0x0A, 0x00, 0x00, 0x01, KeyNo, 0x00 };//{ 0x90, 0x0A, 0x00, 0x00, 0x01, 0x01, 0x00 }
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
            int  nByteRead = 0;
            ret = base.ExchangeAPDU((byte)cmd_Authenticate.Length, cmd_Authenticate, out m_response,out nByteRead,out pSw1,out pSw2);
            if (ret && m_response.Length >= 8)
            {
                mRndB = new byte[8];
                Array.Copy(m_response, 0, mRndB, 0, 8);
            }
            else
            {
                mRndB = new byte[1];
                ret = false;
            }

            return ret;
        }
       /// <summary>
       /// SKS modifed on 20160509
       /// </summary>
       /// <param name="nAID"></param>
       /// <returns></returns>
        public bool SelectCardApplication(byte nAID)
        {
            bool ret = false;
            int nlength = 0;
            byte pSw1 = 0, pSw2 = 0;
            byte[] cmd_selectApp;
            if (nAID == 0x00)
                cmd_selectApp = new byte[] { 0x90, 0x5A, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00 };
            else cmd_selectApp = new byte[] { 0x90, 0x5A, 0x00, 0x00, 0x03, 0x44, 0x4D, (byte)nAID, 0x00 };
            byte[] m_response;
            ret = base.ExchangeAPDU((byte)cmd_selectApp.Length, cmd_selectApp, out m_response,out nlength,out pSw1,out pSw2);

            if (pSw1 == ISOCONSTANTS.DEFIRE_RESPONSE_SW1 && pSw2 == 0x00)
                return true;
            else return false;

           // return ret;
        }
       // public bool Authenticate(byte nAid, byte FileId, byte KeyNo, byte mAccessRights, byte oldKey, bool SelectApp)
       // {
       //     bool ret = false;
       //     byte[] mRndB, nRndA, mAuthCode;
       ////     byte[] cmd_Authenticate = { 0x90, 0x0A, 0x00, 0x00, 0x01, KeyNo, 0x00 };//{0x90,0x0A,0x00,0x00,0x01,0x00,0x00};
       //     byte[] m_response;
       //     if (SelectApp) ret = SelectApplication(nAid);
       //     else ret = true;
       //   //  ret = base.ExchangeAPDU((byte)cmd_Authenticate.Length, cmd_Authenticate, out m_response);
       //     ret = Authenticate(mRndB, KeyNo);
       //     if (ret && mRndB.Length== 8)
       //     {
               
       //         ret = this.CCHSSAM_GenDFAuthCode(nAid, FileId, oldKey, mAccessRights, mRndB, out mAuthCode);

       //         if (ret)
       //             ret = this.Authenticate2(mAuthCode, out nRndA);

       //     }
       //     else ret = false;
       //     return ret;
       // }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="mRndAB"></param>
       /// <param name="mRndA"></param>
       /// <returns></returns>
        public bool Authenticate2(byte[] mRndAB, out byte[] mRndA)
        {
            bool ret = false;
            byte[] m_response;

            // byte[] cmd_auth2 = getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_auth2 = new byte[6 + mRndAB.Length];
            Array.Clear(cmd_auth2, 0, cmd_auth2.Length);
            cmd_auth2[0] = 0x90;
            cmd_auth2[1] = 0xAF;
            cmd_auth2[2] = 0x00;
            cmd_auth2[3] = 0x00;
            cmd_auth2[4] = (byte)mRndAB.Length;
            Array.Copy(mRndAB, 0, cmd_auth2, 5, mRndAB.Length);

            ret = base.ExchangeAPDU((byte)cmd_auth2.Length, cmd_auth2, out m_response);
            if (ret && m_response.Length >= 8)
            {
                mRndA = new byte[8];
                Array.Copy(m_response, 0, mRndA, 0, 8);
            }
            else
            {
                mRndA = new byte[1];
                ret = false;
            }


            return ret;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="mRndAB"></param>
       /// <param name="mRndA"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns>
        public bool Authenticate2(byte[] mRndAB, out byte[] mRndA,out byte pSw1, out byte pSw2)
        {
            bool ret = false;
            byte[] m_response;
            int nByteRead = 0;
            pSw1 = 0xff;
            pSw2 = 0xff;
            // byte[] cmd_auth2 = getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_auth2 = new byte[6 + mRndAB.Length];
            Array.Clear(cmd_auth2, 0, cmd_auth2.Length);
            cmd_auth2[0] = 0x90;
            cmd_auth2[1] = 0xAF;
            cmd_auth2[2] = 0x00;
            cmd_auth2[3] = 0x00;
            cmd_auth2[4] = (byte)mRndAB.Length;
            Array.Copy(mRndAB, 0, cmd_auth2, 5, mRndAB.Length);

            ret = base.ExchangeAPDU((byte)cmd_auth2.Length, cmd_auth2, out m_response,out nByteRead,out pSw1,out pSw2);
            if (ret && m_response.Length >= 8)
            {
                mRndA = new byte[8];
                Array.Copy(m_response, 0, mRndA, 0, 8);
            }
            else
            {
                mRndA = new byte[1];
                ret = false;
            }


            return ret;
        }
       /// <summary>
        /// SKS: SelectCard_ForProcessing 
        /// ISO14443-4 Method to activate card for processing
       /// </summary>
       /// <returns></returns>
        public bool SelectCard_ForProcessing(byte bCid)
        {
            bool bRet = true;
            if (!RATS(bCid) //1 //bCid=0
                // Change Baud
                // 0x00 : 106 KBaud
                // 0x03 : 212 KBaud
                // 0x0A : 424 KBaud
                // 0x0F : 848 KBaud
                /* || !PPSR(0x01, 0xA2)*/)
                bRet = false;
            else m_readerState = E_ReaderState.ACTIVE_CARDON;//Card is selected now
            return bRet;
        }
       /// <summary>
       /// SKS: GetVersion Method
       /// Added on 20160509
       /// is used to detect an already selected card in the field ...
       /// </summary>
       /// <param name="uid"> UID of the card present in the field</param>
       /// <returns></returns>
        public bool GetVersion(out byte[] uid)
        {
            bool bRet = false;
            byte[] VersionInfo = new byte[312];            
            uid = new byte[7];
            int index = 0, nbytesRead = 0;
            byte[] m_response;
            byte pSw1 = 0x00, pSw2 = 0x00;           
            byte[] cmd_GetVersion = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_GETVER_INS, 0x00, 0x00, 0x00 }; //CLA,INA,P0,P1,LE

            bRet = base.ExchangeAPDU((byte)cmd_GetVersion.Length, cmd_GetVersion, out m_response, out nbytesRead, out pSw1, out pSw2);
            if (bRet && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
            {
                Array.Copy(m_response, 0, VersionInfo, index, m_response.Length);
                index += m_response.Length;
            }
            else
            {
                return bRet;
            }
            //part 2
            cmd_GetVersion[1] = ISOCONSTANTS.DESFIRE_MOREDATA_INS; //cmd            
            // bRet = base.ExchangeAPDU(1, abyData, out m_response);
            bRet = base.ExchangeAPDU((byte)cmd_GetVersion.Length, cmd_GetVersion, out m_response, out nbytesRead, out pSw1, out pSw2);
            if (bRet && pSw2 == ISOCONSTANTS.DESFIRE_MOREDATA_INS)
            {
                Array.Copy(m_response, 0, VersionInfo, index, m_response.Length);
                index += m_response.Length;
            }
            else
            {
                return bRet;
            }
            //part 3
            // abyData[0] = ISOCONSTANTS.DESFIRE_MOREDATA_INS; //cmd            
            // bRet = base.ExchangeAPDU(1, abyData, out m_response);
            bRet = base.ExchangeAPDU((byte)cmd_GetVersion.Length, cmd_GetVersion, out m_response, out nbytesRead, out pSw1, out pSw2);
            if (bRet && pSw2 == 0x00)
            {
                Array.Copy(m_response, 0, VersionInfo, index, m_response.Length);
                index += m_response.Length;
                Array.Copy(m_response, 0, uid, 0, 7);
            }
            else
            {
                return bRet;
            }
            // m_strMsg = "GetVersion : UID";
            //  m_strMsg += BufToString(uid,7);
            // m_RFReader.Lcdchs(m_strMsg);
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="fileNo"></param>
       /// <param name="value"></param>
       /// <returns></returns>
        public bool GetValue(byte fileNo, out int value)
        {
            bool bRet = false;
            ulong val = 0;
            byte[] cmd_GetValue = { 0x90, 0x6C, 0x00, 0x00, 0x01, fileNo, 0x00 };
            byte[] m_response;
            bRet = base.ExchangeAPDU((byte)cmd_GetValue.Length, cmd_GetValue, out m_response);
            if (bRet)
            {
                byte[] val_buff = new byte[4];
               
                Array.Copy(m_response, 0, val_buff, 0, 4);
                val = ConvertLittleEndian(val_buff);

            }
            value = (int)val;
            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="fileNo"></param>
       /// <param name="value"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns>
        public bool GetValue(byte fileNo, out int value, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            int nByteRead = 0;
            ulong val = 0;
            byte[] cmd_GetValue = { 0x90, 0x6C, 0x00, 0x00, 0x01, fileNo, 0x00 };
            byte[] m_response;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            bRet = base.ExchangeAPDU((byte)cmd_GetValue.Length, cmd_GetValue, out m_response,out nByteRead,out pSw1,out pSw2);
            if (bRet)
            {
                byte[] val_buff = new byte[4];
                if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                {
                    pSw1--;//to make CSw functions happy ... 
                    Array.Copy(m_response, 0, val_buff, 0, 4);
                    val = ConvertLittleEndian(val_buff);
                }

            }
            value = (int)val;
            return bRet;
        }


        /// <summary>
        /// Method :GetApplicationIds
        /// Gets list of Application Ids in the Card 
        /// </summary>
        /// <returns>true on success</returns>
        public bool GetApplicationIds()
        {
            bool ret = false;
            byte[] cmd_getApplications = { 0x90, 0x6A, 0x00, 0x00, 0x00 };
            byte[] m_response;
            ret = base.ExchangeAPDU((byte)cmd_getApplications.Length, cmd_getApplications, out m_response);

            return ret;
        }

        /// <summary>
        /// GetFileSettings
        /// </summary>
        /// <param name="byFID"></param>
        /// <returns></returns>
        public bool GetFileSettings(byte byFID)
        {
            bool bRet = false;
            return bRet;
        }
        
       /// <summary>
        /// GetFileIDs
       /// </summary>
       /// <returns></returns>
        public bool GetFileIDs()
        {
            bool bRet = false;
            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="nSize"></param>
       /// <returns></returns>
        public bool CreateStandardFile(int nSize)
        {
            bool bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nSize"></param>
       /// <returns></returns>
        public bool CreateBackupFile(int nSize)
        {
            bool bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nLowLimit"></param>
       /// <param name="nHighLimit"></param>
       /// <param name="nValue"></param>
       /// <param name="bLimitedCredit"></param>
       /// <returns></returns>
        public bool CreateValueFile(int nLowLimit, int nHighLimit, int nValue, bool bLimitedCredit)
        {
            bool bRet = false;
            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="nRecordSize"></param>
       /// <param name="nNumRecord"></param>
       /// <returns></returns>
        public bool CreateCyclicRecordFile(int nRecordSize, int nNumRecord)
        {
            bool bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nRecordSize"></param>
       /// <param name="nNumRecord"></param>
       /// <returns></returns>
        public bool CreateLinearRecordFile(int nRecordSize, int nNumRecord)
        {
            bool bRet = false;
            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool CommitTransaction()
        {
            bool bRet = false;
            byte[] cmd_commitTxn = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_COMMIT_TXN_INS, 0x00, 0x00, 0x00 };
            byte[] m_response;
            bRet = base.ExchangeAPDU((byte)cmd_commitTxn.Length, cmd_commitTxn, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00) bRet = true;
            else bRet = false;

            return bRet;
        }
        public bool CommitTransaction(out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] cmd_commitTxn = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_COMMIT_TXN_INS, 0x00, 0x00, 0x00 };
            byte[] m_response;
            bRet = base.ExchangeAPDU((byte)cmd_commitTxn.Length, cmd_commitTxn, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00)
            {
                pSw1 = --m_response[0];
                pSw2 = m_response[1];
                bRet = true;
            }
            else bRet = false;

            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        public bool AbortTransaction()
        {
            bool bRet = false;
            byte[] cmd_Txn = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_ABORT_TXN_INS, 0x00, 0x00, 0x00 };
            byte[] m_response;
            bRet = base.ExchangeAPDU((byte)cmd_Txn.Length, cmd_Txn, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00) bRet = true;
            else bRet = false;

            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileID"></param>
       /// <param name="nOffset"></param>
       /// <param name="nLength"></param>
       /// <param name="nDataBuff"></param>
       /// <returns></returns>
        public bool ReadData(byte nFileID,int nOffset, int nLength,out byte[] nDataBuff)
        {
            bool bRet = false;
            byte pSw1 = 0xff, pSw2 = 0xff;
            int index = 0, nbytesRead = 0; ;
            byte[] m_response;
             
            byte[] noffsetbts = base.ConvertLittleEndian(nOffset);
            byte[] nLengthbytes = base.ConvertLittleEndian(nLength);
            nDataBuff = new byte[nLength];

            byte[] cmd_ReadData = new byte[6+1 + noffsetbts.Length-1 + nLengthbytes.Length-1];//CLS+INS+P1+p2+Lc+data(fileNo[1]+offset[3]+length[3])+Le//{ ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS, 0x00, 0x00, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }

            cmd_ReadData[index++] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_ReadData[index++] = ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS;
            cmd_ReadData[index++] = 0x00;
            cmd_ReadData[index++] = 0x00;
            cmd_ReadData[index++] =(byte) (1 + noffsetbts.Length-1 + nLengthbytes.Length-1);//fileNo[1]+offset[4]+length[4]
            cmd_ReadData[index++] = nFileID;

            Array.Copy(noffsetbts, 0, cmd_ReadData, index, noffsetbts.Length-1);
            index += 3;

            Array.Copy(nLengthbytes, 0, cmd_ReadData, index, nLengthbytes.Length-1);

            bRet = base.ExchangeAPDU((byte)cmd_ReadData.Length, cmd_ReadData, out m_response,out nbytesRead,out pSw1,out pSw2);

            if (bRet)
            {
                if (pSw1== 0x91 && pSw2 == 0xAF)//Has more data
                {
                    byte[] cmd_part2 = {0x90, 0xAF, 0x00, 0x00, 0x00, 0x00};

                }
                else if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                {

                   // Array.Copy(m_response, 1, nDataBuff, 0, nbytesRead);
                    Array.Copy(m_response, 0, nDataBuff, 0, nbytesRead);
                    pSw1--;//to make CSw functions happy ... 
                }
                else // error ...
                {

                }
            }

            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileID"></param>
       /// <param name="nOffset"></param>
       /// <param name="nLength"></param>
       /// <param name="nDataBuff"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns>
        public bool ReadData(byte nFileID, int nOffset, int nLength, out byte[] nDataBuff, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            pSw1 = 0xff;
            pSw2 = 0xff;
            int index = 0, nbytesRead = 0; ;
            byte[] m_response;

            byte[] noffsetbts = base.ConvertLittleEndian(nOffset);
            byte[] nLengthbytes = base.ConvertLittleEndian(nLength);
            nDataBuff = new byte[nLength];

            byte[] cmd_ReadData = new byte[6 + 1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1];//CLS+INS+P1+p2+Lc+data(fileNo[1]+offset[3]+length[3])+Le//{ ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS, 0x00, 0x00, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }

            cmd_ReadData[index++] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_ReadData[index++] = ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS;
            cmd_ReadData[index++] = 0x00;
            cmd_ReadData[index++] = 0x00;
            cmd_ReadData[index++] = (byte)(1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1);//fileNo[1]+offset[4]+length[4]
            cmd_ReadData[index++] = nFileID;

            Array.Copy(noffsetbts, 0, cmd_ReadData, index, noffsetbts.Length - 1);
            index += 3;

            Array.Copy(nLengthbytes, 0, cmd_ReadData, index, nLengthbytes.Length - 1);

            bRet = base.ExchangeAPDU((byte)cmd_ReadData.Length, cmd_ReadData, out m_response, out nbytesRead, out pSw1, out pSw2);

            if (bRet)
            {
                if (pSw1 == 0x91 && pSw2 == 0xAF)//Has more data
                {
                    byte[] cmd_part2 = { 0x90, 0xAF, 0x00, 0x00, 0x00, 0x00 };
                    //TODO: TO be added...

                }
                else if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                {
                    pSw1--;//to make CSw functions happy ... 
                    Array.Copy(m_response, 0, nDataBuff, 0, nbytesRead);

                }
                else // error ...
                {

                }
            }

            return bRet;
        }
 
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileID"></param>
       /// <param name="nOffset"></param>
       /// <param name="nDataLen"></param>
       /// <param name="abyWriteData"></param>
       /// <returns></returns>
        public bool WriteDataFile(byte nFileID,int nOffset, int nDataLen, byte[] abyWriteData, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            byte[] m_response;
            int index = 0, nbytesRead = 0;
            int MaxDatatobeSent = 0;
            pSw1 = 0xff;
            pSw2 = 0xff;

            if (nDataLen > 52)
            {
                MaxDatatobeSent = 52;
            }
            else MaxDatatobeSent = nDataLen;

            byte[] noffsetbts = base.ConvertLittleEndian(nOffset);
            byte[] nLengthbytes = base.ConvertLittleEndian(nDataLen);

            byte[] cmd_buff = new byte[6 + 1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1 + MaxDatatobeSent];

            cmd_buff[index++] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_buff[index++] = ISOCONSTANTS.DESFIRE_WRITE_DATAFILE_INS;
            cmd_buff[index++] = 0x00;
            cmd_buff[index++] = 0x00;
            cmd_buff[index++] = (byte)(1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1 + nDataLen);//fileNo[1]+offset[3]+length[3]+datalength
            cmd_buff[index++] = nFileID;

            Array.Copy(noffsetbts, 0, cmd_buff, index, noffsetbts.Length - 1);
            index += 3;

            Array.Copy(nLengthbytes, 0, cmd_buff, index, nLengthbytes.Length - 1);

            index += 3;
          
            {
                Array.Copy(abyWriteData, 0, cmd_buff, index, MaxDatatobeSent);
            }

            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response,out nbytesRead ,out pSw1,out pSw2);
                        
             if (bRet)
             {
                 if (pSw1 == 0x91 && pSw2 == 0x00)//Data written successfully
                 {
                     pSw1--;//to make tt happy
                 }
                 else if (pSw1 == 0x91 && pSw2 == 0xAF) // more data to be written
                 {
                     byte[] cmd_buff2 = new byte[6 + (nDataLen - MaxDatatobeSent)];
                     cmd_buff2[0] = 0x90;
                     cmd_buff2[1] = 0xAF;
                     cmd_buff2[2] = 0x00;
                     cmd_buff2[3] = 0x00;
                     cmd_buff2[4] = (byte)(nDataLen - MaxDatatobeSent);
                     Array.Copy(abyWriteData, 52, cmd_buff2, 5, (nDataLen - MaxDatatobeSent));

                     bRet = base.ExchangeAPDU((byte)cmd_buff2.Length, cmd_buff2, out m_response, out nbytesRead, out pSw1, out pSw2);
                     if (bRet)
                     {
                         if (pSw1 == 0x91 && pSw2 == 0x00)//Data written successfully
                         {
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

            return bRet;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nFileID"></param>
        /// <param name="nOffset"></param>
        /// <param name="mRecordSize"></param>
        /// <param name="pNbrOfRecords"></param>
        /// <param name="mRecords"></param>
        /// <returns></returns>
        public bool ReadRecords(byte nFileID, int nOffset,int mRecordSize, int pNbrOfRecords, out byte[] mRecords)
        {
            bool bRet = false;
            int index = 0, nbytesRead=0,outbuffIndex=0;
            byte[] m_response;
            byte pSw1 = 0xff, pSw2 = 0xff;
            /*  mRecords = new byte[mRecordSize * pNbrOfRecords];

              byte[] noffsetbts = base.ConvertLittleEndian(nOffset);
              byte[] nLengthbytes = base.ConvertLittleEndian(pNbrOfRecords);
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

              bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbytesRead, out pSw1, out pSw2);

              if (bRet)
              {
                  if (pSw1== 0x91 && pSw2 == 0xAF)//Has more data
                  {
                      //byte[] cmd_part2 = { 0x90, 0xAF, 0x00, 0x00, 0x00, 0x00 };
                      Array.Copy(m_response, 0, mRecords, outbuffIndex, nbytesRead);
                      outbuffIndex += (nbytesRead);
                      bRet=   ReadRecordIntermideate(ref mRecords, outbuffIndex);

                  }
                  else if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                  {

                      Array.Copy(m_response, 0, mRecords, outbuffIndex, nbytesRead - 3);
                      outbuffIndex += (nbytesRead - 3);
                   
                  }
                  else // error ...
                  {
                      bRet = false;
                  }
              }
              */
            bRet = ReadRecords(nFileID, nOffset, mRecordSize, pNbrOfRecords, out mRecords,out pSw1,out pSw2);
            return bRet;           
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileID"></param>
       /// <param name="nOffset"></param>
       /// <param name="mRecordSize"></param>
       /// <param name="pNbrOfRecords"></param>
       /// <param name="mRecords"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns> 
       public bool ReadRecords(byte nFileID, int nOffset, int mRecordSize, int pNbrOfRecords, out byte[] mRecords,out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            int index = 0, nbytesRead = 0, outbuffIndex = 0;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;
            mRecords = new byte[mRecordSize * pNbrOfRecords];

            byte[] noffsetbts = base.ConvertLittleEndian(nOffset);
            byte[] nLengthbytes = base.ConvertLittleEndian(pNbrOfRecords);
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

            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbytesRead, out pSw1, out pSw2);

            if (bRet)
            {
                if (pSw1 == 0x91 && pSw2 == 0xAF)//Has more data
                {
                    //byte[] cmd_part2 = { 0x90, 0xAF, 0x00, 0x00, 0x00, 0x00 };
                    Array.Copy(m_response, 0, mRecords, outbuffIndex, nbytesRead);
                    outbuffIndex += (nbytesRead);
                    bRet = ReadRecordIntermideate(ref mRecords, outbuffIndex,out pSw1, out pSw2);

                }
                else if (pSw1 == 0x91 && pSw2 == 0x00)// last data packet save it and terminate ... the call
                {

                    Array.Copy(m_response, 0, mRecords, outbuffIndex, nbytesRead - 3);
                    outbuffIndex += (nbytesRead - 3);
                    pSw1--;//to make CSw functions happy ... 
                }
                else // error ...
                {
                    bRet = false;
                }
            }

            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="mRecordBuff"></param>
       /// <param name="index"></param>
       /// <returns></returns>
        private bool ReadRecordIntermideate(ref byte[] mRecordBuff, int index)
        {
            bool bRet = false;
            byte[] m_response;
            int nbRead=0;
            byte pSw1=0xFF,pSw2=0xFF;
            //getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_buff = { 0x90, 0xAF, 0x00, 0x00, 0x00 };

            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response,out nbRead,out pSw1,out pSw2);
            if (bRet)
            {
                if (pSw1 == 0x91 && pSw2 == 0x00) 
                {
                    Array.Copy(m_response, 0, mRecordBuff, index, nbRead);
                    //index += (nbRead - 3);
                    return true;
                }
                else if (pSw1 == 0x91 && pSw2 == 0xAF) //continure to read
                {
                    Array.Copy(m_response, 0, mRecordBuff, index, nbRead);
                    index += (nbRead);
                    ReadRecordIntermideate(ref mRecordBuff, index,out pSw1,out pSw2);
                }
                else
                {
                    bRet = false;
                }
            }
            return bRet;
        }
        private bool ReadRecordIntermideate(ref byte[] mRecordBuff, int index, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            byte[] m_response;
            int nbRead = 0;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            //getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_buff = { 0x90, 0xAF, 0x00, 0x00, 0x00 };

            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbRead, out pSw1, out pSw2);
            if (bRet)
            {
                if (pSw1 == 0x91 && pSw2 == 0x00)
                {
                    Array.Copy(m_response, 0, mRecordBuff, index, nbRead);
                    //index += (nbRead - 3);
                    pSw1--; // to make TT happy
                    return true;
                }
                else if (pSw1 == 0x91 && pSw2 == 0xAF) //continure to read
                {
                    Array.Copy(m_response, 0, mRecordBuff, index, nbRead);
                    index += (nbRead);
                    ReadRecordIntermideate(ref mRecordBuff, index,out pSw1, out pSw2);
                }
                else
                {
                    bRet = false;
                }
            }
            return bRet;
        }
        /// <summary>
        /// Appends one record at the end of linear/cyclic record file and erases/overwrite the oldest 
        /// record of cyclic record file if it is already full.
        /// The entire new row is cleared before data is inserted...
        /// </summary>
        /// <param name="nFileID"></param>
        /// <param name="nOffset">The offset within single record records (in bytes)</param>
        /// <param name="nDataLen"></param>
        /// <param name="abyWriteData"></param>
        /// <returns></returns>
        public bool WriteRecords(byte nFileID,int nOffset, int nDataLen, byte[] abyWriteData,out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            int index = 0;
            byte[] m_response;
            pSw1 = 0xff;
            pSw2 = 0xff;

            int MaxDatatobeSent = 0;

            if (nDataLen > 52)
            {
                MaxDatatobeSent = 52;
            }
            else MaxDatatobeSent = nDataLen;

            byte[] noffsetbts = base.ConvertLittleEndian(nOffset);
            byte[] nLengthbytes = base.ConvertLittleEndian(nDataLen);

            byte[] cmd_buff = new byte[6 + 1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1 + MaxDatatobeSent];

            cmd_buff[index++] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_buff[index++] = ISOCONSTANTS.DESFIRE_WRITE_RECFILE_INS;
            cmd_buff[index++] = 0x00;
            cmd_buff[index++] = 0x00;
            cmd_buff[index++] = (byte)(1 + noffsetbts.Length - 1 + nLengthbytes.Length - 1 + nDataLen);//fileNo[1]+offset[3]+length[3]+datalength
            cmd_buff[index++] = nFileID;

            Array.Copy(noffsetbts, 0, cmd_buff, index, noffsetbts.Length - 1);
            index += 3;

            Array.Copy(nLengthbytes, 0, cmd_buff, index, nLengthbytes.Length - 1);

            index += 3;

            Array.Copy(abyWriteData, 0, cmd_buff, index, MaxDatatobeSent);
            int nbyteRead=0;
            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbyteRead, out pSw1, out pSw2);

              if (bRet)
              {
                  if (pSw1 == 0x91 && pSw2 == 0x00) //Data written successfully
                  {
                      pSw1--;//to make CSw functions happy ... 
                      return true;
                  }
                  else if (pSw1 == 0x91 && pSw2 == 0xAF) // more data to be written
                  {
                    bRet=  WriteRecordsIntermediate(ref abyWriteData, 52, nDataLen - MaxDatatobeSent,out pSw1,out pSw2);
                  }
                  else
                  {
                      bRet = false;
                  }
              }

            return bRet;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nData"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private bool WriteRecordsIntermediate(ref byte[] nData,int startIndex, int length)
        {
            bool bRet = false;
            byte[] m_response;
            int nbRead = 0, nWritebytes=0;
            byte pSw1 = 0xFF, pSw2 = 0xFF;
            //getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_buff; //= new  byte[6+]//{ 0x90, 0xAF, 0x00, 0x00, 0x00 };

            if (length > 52)
            {
                nWritebytes = 52;
            }
            else nWritebytes = length;

            cmd_buff = new byte[6 + nWritebytes];

            cmd_buff[0] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_buff[1] = 0xAF;
            cmd_buff[2] = 0x00;
            cmd_buff[3] = 0x00;
            cmd_buff[4] = (byte) (1+nWritebytes);

            Array.Copy(nData, startIndex, cmd_buff, 5, nWritebytes);
           
            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbRead, out pSw1, out pSw2);
            if (bRet)
            {
                if (pSw1 == 0x91 && pSw2 == 0x00)
                {
                    
                    //index += (nbRead - 3);
                    return true;
                }
                else if (pSw1 == 0x91 && pSw2 == 0xAF) //continure to read
                {
                  //  Array.Copy(m_response, 0, mRecordBuff, index, nbRead);
                   // index += (nbRead);
                    startIndex += nWritebytes;
                    WriteRecordsIntermediate(ref nData, startIndex, length - startIndex);
                }
                else
                {
                    bRet = false;
                }
            }
            return bRet;
        }
        private bool WriteRecordsIntermediate(ref byte[] nData, int startIndex, int length, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            byte[] m_response;
            int nbRead = 0, nWritebytes = 0;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            //getApdu(0x90, 0xAF, 0x00, 0x00, mRndAB,0x00);
            byte[] cmd_buff; //= new  byte[6+]//{ 0x90, 0xAF, 0x00, 0x00, 0x00 };

            if (length > 52)
            {
                nWritebytes = 52;
            }
            else nWritebytes = length;

            cmd_buff = new byte[6 + nWritebytes];

            cmd_buff[0] = ISOCONSTANTS.DESFIRE_CLA;
            cmd_buff[1] = 0xAF;
            cmd_buff[2] = 0x00;
            cmd_buff[3] = 0x00;
            cmd_buff[4] = (byte)(1 + nWritebytes);

            Array.Copy(nData, startIndex, cmd_buff, 5, nWritebytes);

            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response, out nbRead, out pSw1, out pSw2);
            if (bRet)
            {
                if (pSw1 == 0x91 && pSw2 == 0x00)
                {
                    pSw1--; //SKS to make TT happy
                    //index += (nbRead - 3);
                    return true;
                }
                else if (pSw1 == 0x91 && pSw2 == 0xAF) //continure to read
                {
                    //  Array.Copy(m_response, 0, mRecordBuff, index, nbRead);
                    // index += (nbRead);
                    startIndex += nWritebytes;
                  bRet=  WriteRecordsIntermediate(ref nData, startIndex, length - startIndex,out pSw1,out pSw2);
                }
                else
                {
                    bRet = false;
                }
            }
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nValue"> value to be credited</param>
       /// <returns></returns>
        public bool Credit(int nValue)
        {
            bool bRet = false;
            byte[] m_response;
            byte[] cmd_Credit = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_CREDIT_INS, 0x00, 0x00, 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
            cmd_Credit[6] = (byte)(nValue & 0xFF);
            cmd_Credit[7] = (byte)((nValue >> 8) & 0xFF);
            cmd_Credit[8] = (byte)((nValue >> 16) & 0xFF);
            cmd_Credit[9] = (byte)((nValue >> 24) & 0xFF);
            bRet = base.ExchangeAPDU((byte)cmd_Credit.Length, cmd_Credit, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00) bRet = true;
            else bRet = false;

            return bRet;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nFileId"></param>
        /// <param name="nValue"></param>
        /// <returns></returns>
        public bool Credit(byte nFileId, int nValue)
        {
            bool bRet = false;
            byte[] m_response;
            byte[] cmd_Credit = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_CREDIT_INS, 0x00, 0x00, 0x05, nFileId, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
            cmd_Credit[6] = (byte)(nValue & 0xFF);
            cmd_Credit[7] = (byte)((nValue >> 8) & 0xFF);
            cmd_Credit[8] = (byte)((nValue >> 16) & 0xFF);
            cmd_Credit[9] = (byte)((nValue >> 24) & 0xFF);
            bRet = base.ExchangeAPDU((byte)cmd_Credit.Length, cmd_Credit, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00) bRet = true;
            else bRet = false;

            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileId"></param>
       /// <param name="nValue"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns>
        public bool Credit(byte nFileId, int nValue, out byte pSw1,out byte pSw2)
        {
            bool bRet = false;
            byte[] m_response;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            int nByteRead = 0;
            byte[] cmd_Credit = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_CREDIT_INS, 0x00, 0x00, 0x05, nFileId, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
            cmd_Credit[6] = (byte)(nValue & 0xFF);
            cmd_Credit[7] = (byte)((nValue >> 8) & 0xFF);
            cmd_Credit[8] = (byte)((nValue >> 16) & 0xFF);
            cmd_Credit[9] = (byte)((nValue >> 24) & 0xFF);
            bRet = base.ExchangeAPDU((byte)cmd_Credit.Length, cmd_Credit, out m_response,out nByteRead,out pSw1, out pSw2);

            if (bRet && pSw1 == 0x91 && pSw2 == 0x00)
            {
                pSw1--;
                bRet = true;
            }
            else bRet = false;

            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nValue">+Ve value to be debited</param>
       /// <returns></returns>
        public bool Debit(int nValue)
        {
            bool bRet = false;
            byte[] m_response;
            byte[] cmd_buff = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_DEBIT_INS, 0x00, 0x00, 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
            cmd_buff[6] = (byte)(nValue & 0xFF);
            cmd_buff[7] = (byte)((nValue >> 8) & 0xFF);
            cmd_buff[8] = (byte)((nValue >> 16) & 0xFF);
            cmd_buff[9] = (byte)((nValue >> 24) & 0xFF);
            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00) bRet = true;
            else bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileId"></param>
       /// <param name="nValue"></param>
       /// <returns></returns>
        public bool Debit(byte nFileId, int nValue)
        {
            bool bRet = false;
            byte[] m_response;
            byte[] cmd_buff = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_DEBIT_INS, 0x00, 0x00, 0x05, nFileId, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
            cmd_buff[6] = (byte)(nValue & 0xFF);
            cmd_buff[7] = (byte)((nValue >> 8) & 0xFF);
            cmd_buff[8] = (byte)((nValue >> 16) & 0xFF);
            cmd_buff[9] = (byte)((nValue >> 24) & 0xFF);
            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response);

            if (bRet && m_response[0] == 0x91 && m_response[1] == 0x00) bRet = true;
            else bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="nFileId"></param>
       /// <param name="nValue"></param>
       /// <param name="pSw1"></param>
       /// <param name="pSw2"></param>
       /// <returns></returns>
        public bool Debit(byte nFileId, int nValue, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            byte[] m_response;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            int nByteRead = 0;
            byte[] cmd_buff = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_DEBIT_INS, 0x00, 0x00, 0x05, nFileId, 0x00, 0x00, 0x00, 0x00, 0x00 };//{CLA,INS,P0,P1,Lc,data,Le}, data = {File ID, Value_LSB0,Value_LSB1,Value_MSB0,Value_MSB1 }
            cmd_buff[6] = (byte)(nValue & 0xFF);
            cmd_buff[7] = (byte)((nValue >> 8) & 0xFF);
            cmd_buff[8] = (byte)((nValue >> 16) & 0xFF);
            cmd_buff[9] = (byte)((nValue >> 24) & 0xFF);
            bRet = base.ExchangeAPDU((byte)cmd_buff.Length, cmd_buff, out m_response,out nByteRead,out pSw1,out pSw2);

            if (bRet && pSw1 == 0x91 && pSw2 == 0x00)
            {
                pSw1--;
                bRet = true;
            }
            else bRet = false;
            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="byKeyNo"></param>
       /// <param name="byKeyCompMeth"></param>
       /// <param name="byCurKeyId"></param>
       /// <param name="byCurKeyVersion"></param>
       /// <param name="byNewKeyId"></param>
       /// <param name="byNewKeyVersion"></param>
       /// <returns></returns>
        public bool ChangeKey(byte byKeyNo, byte byKeyCompMeth, byte byCurKeyId, byte byCurKeyVersion,
            byte byNewKeyId, byte byNewKeyVersion)
        {
            bool bRet = false;
            return bRet;
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="byKeyNo"></param>
       /// <returns></returns>
        public bool GetKeyEntry(byte byKeyNo)
        {
            bool bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="byOldKeyNoCEK"></param>
       /// <param name="byKeyNo"></param>
       /// <param name="byProMas"></param>
       /// <param name="byKeyEntryLen"></param>
       /// <param name="abyNewKeyEntry"></param>
       /// <returns></returns>
        public bool ChangeKeyEntry(byte byOldKeyNoCEK, byte byKeyNo, byte byProMas,
            byte byKeyEntryLen, byte[] abyNewKeyEntry)
        {
            bool bRet = false;
            return bRet;
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="byKeyNoTag"></param>
       /// <returns></returns>
        public bool GetKeyVersion(byte byKeyNoTag)
        {
            bool bRet = false;
            return bRet;
        }
    }
}
