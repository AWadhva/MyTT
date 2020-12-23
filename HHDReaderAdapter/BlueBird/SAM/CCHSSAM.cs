using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace IFS2.Equipment.CSCReaderAdaptor
{
    public class cCCHSSAMInfo1
    {
        public byte ServiceProvider;
        public byte SAMType;
        public string SAMAppVersion = "";
    }
     public class CCHSSAM:ISO14443
    {
      //  byte[] arSamMasterKey;
      //  byte[] arSamNewKey;
         private bool IsWrongPinAttempted = false;
        byte SAMType = 0x00;
        bool m_SAMopened = false;
        cCCHSSAMInfo1 mccchsamdetails;
        int mSAMSequence = 0, mSAMID;
        int m_samSlot = 1;
         public CCHSSAM(ref RFIDReader clsRFReader)
             : base(ref clsRFReader)
         {
             m_SAMopened = false;
             mSAMSequence = 0;
             mSAMID = 0;
         }
         public CCHSSAM(ref RFIDReader clsRFReader, int SamSlot)
             : base(ref clsRFReader)
         {
             m_SAMopened = false;
             m_samSlot = SamSlot;
         }
         public int GetSAMSequence()
         {
             return mSAMSequence;
         }
         public int GetSAMId()
         {
             return mSAMID;
         }
         public cCCHSSAMInfo1 GetSAMInfo()
         {
             return mccchsamdetails;
         }

         public bool SAM_Open(int slotNo)
         {
             bool bIsSAM = true;     // true = SAM slot, false = IC slot
             byte byCardType = 0x02; // 0x02 = General speed ISO7816
             byte bySAMSlot = 0;     // Slot number = 0 ~ 3
             byte byMode = 0x47;     // 0x47 = General ISO7816 mode
             byte[] abyInputParam = new byte[2] { 0x01, 0x11 };  // Default baudrate : 0x11= 9600 { 0x01, 0x11 }; 
             byte[] abyChangePPS = new byte[4] { 0x03, 0xF1, 0x11, 0x94 };  // change PPS : 0x13 = 38400( refer to datasheet of card manufacturer )
             uint dwBaudRate = 38400;
             if (slotNo < 3) bySAMSlot = (byte)slotNo;
           
             if (m_RFReader.OpenICComm() // Open IC port
                 && m_RFReader.SAMDefType(byCardType, bySAMSlot, m_abyResBuf)    // Config IC device

                 && m_RFReader.SAMSlotIOMode(bIsSAM, byMode, m_abyResBuf)
#if _BIP1300_
                 && m_RFReader.ICPowerOn(bIsSAM, abyInputParam, m_abyResBuf)     // Power on to the card                 
#elif _BIP1500_
                && m_RFReader.ICPowerOn(bySAMSlot, abyInputParam, m_abyResBuf)     // Power on to the card
#endif
                && m_RFReader.ICChangePPS(bIsSAM, abyChangePPS, m_abyResBuf)    // Change PPS to the card
                 && m_RFReader.ICChangebaudrate(dwBaudRate, m_abyResBuf))        // Change baudrate to IC device
             {
                
                 return true;
             }
            
             return false;
         }

         public bool SAM_Close()
         {
             if (m_RFReader.ICPowerDown(true, m_abyResBuf)   // Power off to the card
                 && m_RFReader.CloseICComm())                // Close IC port
             {
                // m_strMsg += "....success\r\n";
                 return true;
             }
            // m_strMsg += "....fail\r\n";
             return false;
         }
         public void resetbuffer(byte[] m_inputbuff, int size)
         {
             for (int i = 0; i < size; i++) //PinCode(8bytes
             {
                 m_inputbuff[i] = (byte)'\0';
             }
         }
         /// <summary>
         /// 
         /// </summary>
         /// <param name="mAPDUCmd"></param>
         /// <param name="m_response"></param>
         /// <param name="pSw1"></param>
         /// <param name="pSw2"></param>
         /// <returns></returns>
         public bool SAM_IsoCommand(byte[] mAPDUCmd, out byte[] m_response, out byte pSw1, out byte pSw2)
         {
             bool bRet = false;
             m_response = new byte[2];
             pSw1 = 0xFF;
             pSw2 = 0xFF;
             Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);

             bRet = m_RFReader.SendSAMCommand(true, mAPDUCmd, (byte)mAPDUCmd.Length);
             if (bRet)
             {
                 bRet = m_RFReader.GetSAMData(m_abyResBuf, (uint)5 * 1000);
                 byte length = m_abyResBuf[0];
                 if (bRet && length > 0)
                 {
                     if (length > ISOCONSTANTS.MIN_ISO_DATA_OUT_LENGTH)
                     {
                         m_response = new byte[length - ISOCONSTANTS.MIN_ISO_DATA_OUT_LENGTH];

                         Array.Copy(m_abyResBuf, 1, m_response, 0, m_response.Length);

                         pSw1 = m_abyResBuf[length - 1];
                         pSw2 = m_abyResBuf[length ];
                         if (pSw2 != 0x00) bRet = false;
                     }
                     else
                     {
                         pSw1 = m_abyResBuf[1];
                         pSw2 = m_abyResBuf[2];
                         if (pSw2 != 0x00) bRet = false;
                     }
                 }
             }
             return bRet;
         }
         public bool SAM_IsoCommand(int bSamSlot, byte[] mAPDUCmd, out byte[] m_response, out byte pSw1, out byte pSw2)
         {
             bool bRet = false;
             m_response = new byte[2];
             pSw1 = 0xFF;
             pSw2 = 0xFF;
             Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);

             bRet = m_RFReader.SendSAMCommand(bSamSlot, mAPDUCmd, (byte)mAPDUCmd.Length);
             if (bRet)
             {
                 bRet = m_RFReader.GetSAMData(m_abyResBuf, (uint)5 * 1000);
                 byte length;//= m_abyResBuf[1];//SKS:0
                 //  if (length > 0xFF)
                 {
                     length = (byte)(m_abyResBuf[1] | (m_abyResBuf[0] << 8));
                 }
                 if (bRet && length > 0)
                 {
                     if (length > ISOCONSTANTS.MIN_ISO_DATA_OUT_LENGTH)
                     {
                         m_response = new byte[length - ISOCONSTANTS.MIN_ISO_DATA_OUT_LENGTH];

                         // Array.Copy(m_abyResBuf, 1, m_response, 0, m_response.Length);
                         Array.Copy(m_abyResBuf, 2, m_response, 0, m_response.Length);

                         pSw1 = m_abyResBuf[length];//length - 1
                         pSw2 = m_abyResBuf[length + 1];//length
                         if (pSw2 != 0x00) bRet = false;
                     }
                     else
                     {
                         pSw1 = m_abyResBuf[2];//m_abyResBuf[1];
                         pSw2 = m_abyResBuf[3];//m_abyResBuf[2];
                         if (pSw2 != 0x00) bRet = false;
                     }
                 }
             }
             return bRet;
         }


         public bool CCHSSAM_GetSAMId(out int mDSMID)
         {
             bool ret = false;
             mDSMID = 0;
             byte[] dsmid = new byte[4];
             byte pSw1 = 0xff;
             byte pSw2 = 0xff;

             byte[] isoReqBuffer;
             byte[] response;

             resetbuffer(m_abyResBuf, 312);

             isoReqBuffer = APDU.getApduISO(0xD0, 0x31, 0x00, 0x00, 0x04);
#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response,out pSw1,out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
             if (ret)
             {
                // ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)1000);

                 if (ret && pSw1 == 0x90)
                 {
                     Array.Copy(response, 0, dsmid, 0, 4);
                     mDSMID = (int)ConvertLittleEndian(dsmid);
                     ret = true;

                   //  m_strMsg += ("SAM ID: " + mDSMID.ToString() + "\n");
                  //   m_RFReader.Lcdchs(m_strMsg);
                 }
                 else ret = false;

             }

             return ret;
         }
         public bool CCHSSAM_SelectApplication()
         {
             bool ret = false;
             byte[] iso_cmd_SAMSelectApp = { 0xD4, 0x10, 0x00, 0x00, 0x00, 0x00, 0x09 };//step 1 : select sam application
             byte[] isoReqBuffer, response;
               byte pSw1 = 0xff;
             byte pSw2 = 0xff;
             resetbuffer(m_abyResBuf, 312);

             isoReqBuffer = APDU.getApduISO(0x00, 0xA4, 0x04, 0x0C, iso_cmd_SAMSelectApp);
#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
             if (ret)
             {
                // ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)1000);

                 if (ret && pSw1 == 0x90)
                 {
                     return true;
                 }

             }

             return ret;
         }
         public bool CCHSSAM_SAMActivation(bool IsProductionSAM, byte SamType)
         {
             bool ret = false;
             byte pSw1 = 0xff;
             byte pSw2 = 0xff;
             byte[] SAM_Activation ;
             if (!IsProductionSAM) SAM_Activation = new byte[] { SamType, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x00 };////TYPE(1byte) + PinCode(8bytes)+OptionalData(1byte),  step 2: authenticate sam
             else SAM_Activation = new byte[] { SamType, 0x46, 0x46, 0x34, 0x30, 0x37, 0x45, 0x30, 0x32, 0x00 };////TYPE(1byte) + PinCode(8bytes)+OptionalData(1byte),  step 2: authenticate sam

             byte[] isoReqBuffer, response;

             resetbuffer(m_abyResBuf, 312);
             isoReqBuffer = APDU.getApduISO(0xD0, 0x03, 0x00, 0x00, SAM_Activation);
#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
             if (ret)
             {
                 //ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)1000);

                 if (ret && pSw1 == 0x90 && pSw2==0x00)
                 {
                     ret = true;
                 }
                 else
                 {
                     if (pSw2 == 0x07)//SM_AUTHENTICATION_FAILURE=0x07
                         IsWrongPinAttempted = true;
                     ret = false;
                 }

             }
             return ret;
         }
         //GetSAMSequence
         public bool CCHSSAM_GetSAMSequence(out int SAMSequenceNo)
         {
             bool ret = false;
             byte pSw1 = 0xff;
             byte pSw2 = 0xff;
             SAMSequenceNo = 0;
             byte[] isoReqBuffer,response;
             byte[] seq = new byte[4];

             resetbuffer(m_abyResBuf, 312);
             isoReqBuffer = APDU.getApduISO(0xD0, 0x07, 0x00, 0x00, 0x04);
#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
             if (ret)
             {
                // ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)1000);

                 if (ret &&pSw1==0x90)// m_abyResBuf[0] == 6 && m_abyResBuf[5] == 0x90)
                 {
                     Array.Copy(response, 0, seq, 0, 4);
                     SAMSequenceNo = (int)ConvertLittleEndian(seq);
                     ret = true;
                     m_strMsg += ("SAM Seq: " + SAMSequenceNo.ToString() + "\n");
                     m_RFReader.Lcdchs(m_strMsg);
                 }
                 else ret = false;

             }

             return ret;
         }
         //GetSAMStatus //out cCCHSSAMInfo )

         public bool GenerateTAC(byte[] iData, int length, out byte[] CalculatedTAC)
         {
             byte[] HashData;
             bool ret = false;
             byte pSw1 = 0xff;
             byte pSw2 = 0xff;
             byte[] isoReqBuffer,response;
             resetbuffer(m_abyResBuf, m_abyResBuf.Length);
             
             CalculatedTAC = new byte[4];
             //string Hashstr = "";
             if (length > 244)
             {
                 //TODO: Calculate SHA1 HASH bytes 
                 HashData = CalculateSHA1Hash(iData, length);
             }
             else
             {
                 HashData = new byte[length];
                 //iData.CopyTo(HashData, 0);
                 Array.Copy(iData, HashData, length);
             }
             isoReqBuffer = APDU.getApduISO(0xD0, 0x10, 0x00, 0x00,HashData);

#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
              if (ret)
              {
                  if (pSw1 == 0x90 && pSw2 == 0x00)
                  {
                      Array.Copy(response, 0, CalculatedTAC, 0, 4);
                  }
                  else ret = false;
              }
              return ret;
         }
         public bool CCHSSAM_GetSAMStatus(out cCCHSSAMInfo1 mSAMStatusInfo)
         {
             bool ret = false;
             byte pSw1 = 0xff;
             byte pSw2 = 0xff;
             mSAMStatusInfo = new cCCHSSAMInfo1();
             mSAMStatusInfo.SAMAppVersion = "XXX";
             mSAMStatusInfo.ServiceProvider = 0x02;
             mSAMStatusInfo.SAMType = 0x00;
             byte[] isoReqBuffer,response;

             resetbuffer(m_abyResBuf, 312);
             isoReqBuffer = APDU.getApduISO(0xD0, 0x05, 0x00, 0x00, 0x06);

#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
             if (ret)
             {
               //  ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)1000);

                 if ( pSw1==0x90 && pSw2==0x00)
                 {
                     mSAMStatusInfo.ServiceProvider = response[0];
                     mSAMStatusInfo.SAMType = response[1];
                     mSAMStatusInfo.SAMAppVersion = Encoding.ASCII.GetString(response, 2, 4);
                     ret = true;
                     m_strMsg += ("SAM ServiceProvider: " + mSAMStatusInfo.ServiceProvider.ToString());
                     m_strMsg += ("SAM SAMType: " + mSAMStatusInfo.SAMType.ToString());
                     m_strMsg += ("SAM ServiceProvider: " + mSAMStatusInfo.SAMAppVersion);
                    // m_RFReader.Lcdchs(m_strMsg);
                 }
                 else ret = false;

             }


             return ret;
         }

         public bool CCHSSAM_GenDFAuthCode(byte AppId, byte FileId, byte newkey, byte mAccessRights, byte[] RnDB_buff, out byte[] mAuthCode)
         {
             bool ret = false;
             byte pSw1 = 0xff;
             byte pSw2 = 0xff;
             byte[] cmd_buff = new byte[10];
             cmd_buff[0] = newkey;
             cmd_buff[1] = mAccessRights;

             byte[] response;

             Array.Copy(RnDB_buff, 0, cmd_buff, 2, 8);

             byte[] isoReqBuffer = APDU.getApduISO(0xD0, 0x25, AppId, FileId, cmd_buff, 0x10);
             Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);

#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
             if (ret)
             {
               //  ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)5 * 1000);
                 if (pSw1==0x90&& pSw2==0x00)//m_abyResBuf[0] >= 16)
                 {
                     mAuthCode = new byte[16];
                     Array.Copy(response, 0, mAuthCode, 0, 16);
                 }
                 else
                 {
                     mAuthCode = new byte[] { 0x00 };
                     ret = false;
                 }


             }
             else mAuthCode = new byte[] { 0x00 };
             return ret;
         }

         public bool ConfigureSAM(int SlotNo, bool reset, byte SAMType)
         {
           /*  bool ret = false;
             if (reset)
             {
                 if (m_SAMopened == true) SAM_Close();
                 ret = SAM_Open(SlotNo);
                 m_SAMopened = ret;
             }
             if (ret)
                 ret = CCHSSAM_SelectApplication();
             if (ret && !IsWrongPinAttempted) ret = CCHSSAM_SAMActivation(false, 0x50); //P-SAM= 0x50 , I-SAM= 0x49
             else ret = false;

             if (ret) ret = CCHSSAM_GetSAMSequence(out mSAMSequence);

             if (ret) ret = CCHSSAM_GetSAMId(out mSAMID);

             if (ret)//Now switch to mode 3 
                 ret = CCHSSAM_GetSAMStatus(out mccchsamdetails);

             return ret;*/
             return ConfigureSAM(SlotNo, reset, SAMType, false);
         }
         public bool ConfigureSAM(int SlotNo, bool reset, byte SAMType,bool IsProductionSAM)
         {
             bool ret = false;
             if (reset)
             {
                 if (m_SAMopened == true) SAM_Close();
                 ret = SAM_Open(SlotNo);
                 m_SAMopened = ret;
             }
             if (ret)
                 ret = CCHSSAM_SelectApplication();
             if (ret && !IsWrongPinAttempted) ret = CCHSSAM_SAMActivation(IsProductionSAM, SAMType); //P-SAM= 0x50 , I-SAM= 0x49
             else ret = false;

             if (ret) ret = CCHSSAM_GetSAMSequence(out mSAMSequence);

             if (ret) ret = CCHSSAM_GetSAMId(out mSAMID);

             if (ret)//Now switch to mode 3 
                 ret = CCHSSAM_GetSAMStatus(out mccchsamdetails);

             return ret;
         }

         public bool WriteSAMSequence(int SequenceNo)
         {
             
             bool ret = false;
             byte pSw1 = 0xFF;
             byte pSw2 = 0xFF;
             byte[] isoResponseBuffer, response;

             byte[] SequenceNobytes = base.ConvertLittleEndian(SequenceNo);// TODO: As we are reading SAM Seq in Little indian

             byte[] isoReqBuffer = APDU.getApduISO(0xD0, 0x08, 0x00, 0x00, SequenceNobytes);

#if _BIP1300_
             ret = SAM_IsoCommand(isoReqBuffer, out response, out pSw1, out pSw2);
#elif _BIP1500_
             ret = SAM_IsoCommand(m_samSlot, isoReqBuffer, out response, out pSw1, out pSw2);
#endif
              if (ret)
              {
                  //  ret = m_RFReader.GetSAMData(m_abyResBuf, (uint)1000);

                  if (pSw1 == 0x90 && pSw2 == 0x00)
                  {
                      ret = true;
                  }
                  else ret = false;
              }
              return ret;
         }

         private byte[] CalculateSHA1Hash(byte[] iData, int iDatalength)
         {
             byte[] result;
             // byte[] data = Encoding.ASCII.GetBytes(xmlstr);
             SHA1 sha = new SHA1CryptoServiceProvider();
             // This is one implementation of the abstract class SHA1.
             result = sha.ComputeHash(iData, 0, iDatalength);

             return result;
         }
    }
}
