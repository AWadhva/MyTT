using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Bluebird.RFID;

namespace IFS2.Equipment.CSCReaderAdaptor
{
   public class ISO14443
    {
        protected RFIDReader m_RFReader;
        protected byte[] m_abyCmdBuf;
        protected byte[] m_abyResBuf;
        protected byte[] m_abyUID;
        protected byte[] m_abyProtocol;

        protected String m_strMsg;

        public ISO14443(ref RFIDReader RFReader)
        {
            m_RFReader = RFReader;
            m_abyCmdBuf = new byte[312];
            m_abyResBuf = new byte[312];
            m_abyUID = new byte[16];
            m_abyProtocol = new byte[8];
            m_strMsg = null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pDataIn"></param>
        /// <returns></returns>
        public ulong ConvertLittleEndian(byte[] pDataIn)
        {
            int pos = 0;
            ulong result = 0;
            foreach (byte by in pDataIn)
            {
                result |= (ulong)(by << pos);
                pos += 8;
            }
            return result;
        }
        public byte[] ConvertLittleEndian(int nValue)
        {
            byte [] nbytes = new byte[4];
            nbytes[0] = (byte)(nValue & 0xFF);
            nbytes[1] = (byte)((nValue >> 8) & 0xFF);
            nbytes[2] = (byte)((nValue >> 16) & 0xFF);
            nbytes[3] = (byte)((nValue >> 24) & 0xFF);
            return nbytes;
        }
        public bool CopyBuf(byte[] abySource, int nSourceIndex, byte[] abyDest, int nDestIndex, int nLen)
        {
            Array.Copy(abySource, nSourceIndex, abyDest, nDestIndex, nLen);
            return true;
        }

        public String BufToString(byte[] abyBuf, int nLength)
        {
            String str = "";
            for (int i = 0; i < nLength; i++)
            {
                str += String.Format("0x{0:x} ", abyBuf[i]);
            }
            str += "\r\n";
            return str;
        }
        public bool Compare(byte[] b1, byte[] b2)
        {
            return Encoding.ASCII.GetString(b1,0,b2.Length) == Encoding.ASCII.GetString(b2,0,b2.Length);
        }

        public bool CommandFlow(uint dwCmdFlow)
        {
            bool bRet = false;

            switch (dwCmdFlow)
            {
                case 0: // REQA
                    if (REQA())
                        bRet = true;
                    break;
                case 1: // Multilist
                    if (REQA()
                        && Multilist())
                        bRet = true;
                    break;
                case 2: // Halt A
                    if (REQA()
                        && Multilist()
                        && HaltA())
                        bRet = true;
                    break;
                case 3: // Select
                    if (REQA()
                        && Multilist()
                        && Select(m_abyResBuf))
                        bRet = true;
                    break;
                case 4: // RATS
                    if (REQA()
                        && Multilist()
                        && RATS(0x01))
                        bRet = true;
                    break;
                case 5:	// HighSpeedSelect
                    if (REQA()
                        && HighSpeedSelect(0x14))
                        bRet = true;
                    break;
                case 6:	// PPSR
                    if (REQA()
                        && Multilist()
                        && RATS(0x01)
                        && PPSR(0x01, 0x14))
                        bRet = true;
                    break;
                case 7:	// REQB
                    if (REQB(0x00, 0x01))
                        bRet = true;
                    break;
                case 8:	// Halt B
                    if (REQB(0x00, 0x01)
                        && HaltB(m_abyUID))
                        bRet = true;
                    break;
                case 9:	// Attrib
                    if (REQB(0x00, 0x01)
                        && Attrib(m_abyUID, m_abyProtocol, 0x01, 0x00))
                        bRet = true;
                    break;
                case 10: // DeSelect (B)
                    if (REQB(0x00, 0x01)
                        && Attrib(m_abyUID, m_abyProtocol, 0x01, 0x00)
                        && DeSelect(0x01))
                        bRet = true;
                    break;
            }
            return bRet;
        }

        //public String BuftoString(byte[] abyBuf, int nLength)
        //{
        //    String strResult = null;
        //    String strTemp = null;

        //    for (int i = 0; i < nLength; i++)
        //    {
        //        strTemp = "0x" + abyBuf[i];
        //        strResult.Insert(i * 5, strTemp);
        //    }

        //    return strResult;
        //}

        // ISO 14443 A
        public bool REQA()
        {
            bool bRet = false;
            m_strMsg = "iso reqa : ";
            m_abyCmdBuf[0] = 0;
            Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);
            Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
            if (m_RFReader.SendCommand("iso reqa", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
         
            return bRet;
        }
        public bool REQA(out int cardpresent)
        {
            bool bRet = false;
            m_strMsg = "iso reqa : ";
            m_abyCmdBuf[0] = 0;
            cardpresent = 0;
            Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);
            if (m_RFReader.SendCommand("iso reqa", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
            {
                bRet = true;
                if (m_abyResBuf[1] == 0xFB) cardpresent = 0;// No RF card
                else if (m_abyResBuf[1] == 0x00) cardpresent = 1; //Card is there
                else if (m_abyResBuf[1] == 0xFA) cardpresent = 2; // unknown card

            }
          
            return bRet;
        }

        public bool Multilist()
        {
            int i;
            bool bRet = false;
            byte[] abyTemp = new byte[256];
            Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);
            m_strMsg = "multilist : ";
            m_abyCmdBuf[0] = 0;
            if (!m_RFReader.SendCommand("multilist", m_abyCmdBuf))
                return false;

            for (i = 0; i < 2; i++)
            {
                m_RFReader.GetData(abyTemp);
                if (abyTemp[0] < 4)
                    break;

                Array.Copy(abyTemp, m_abyResBuf, abyTemp[0] + 1);
            }
            if (i > 0)
                bRet = true;

            return bRet;
        }

        public bool HaltA()
        {
            bool bRet = false;
            m_strMsg = "iso hlta : ";
            m_abyCmdBuf[0] = 0;
            if (m_RFReader.SendCommand("iso hlta", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
         
            return bRet;
        }

        public bool Select(byte[] abyUID)
        {
            bool bRet = false;
            m_strMsg = "select : ";
            m_abyCmdBuf[0] = abyUID[0];
            Array.Copy(abyUID, 1, m_abyCmdBuf, 1, abyUID[0]);
            if (m_RFReader.SendCommand("select", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }
        public bool Select(byte[] abyUID, byte length)
        {
            bool bRet = false;
            m_strMsg = "select : ";
            m_abyCmdBuf[0] = length;
            Array.Copy(abyUID, 1, m_abyCmdBuf, 1, abyUID[0]);
            if (m_RFReader.SendCommand("select", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }


        public bool RATS(byte byCID)
        {
            bool bRet = false;
            m_strMsg = "iso rats : ";
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byCID;
            if (m_RFReader.SendCommand("iso rats", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
          
            return bRet;
        }

        public bool PPSR(byte byCID, byte byDsiDsr)
        {
            bool bRet = false;
            m_strMsg = "PPSR : ";
            m_abyCmdBuf[0] = 0x02;
            m_abyCmdBuf[1] = byCID;
            m_abyCmdBuf[2] = byDsiDsr;
            if (m_RFReader.SendCommand("PPSR", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }

        public bool HighSpeedSelect(byte byDsiDsr)
        {
            bool bRet = false;
            m_strMsg = "h : ";
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byDsiDsr;
            if (m_RFReader.SendCommand("h", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
          
            return bRet;
        }
        public bool HighSpeedSelect(byte byDsiDsr, out byte[] uid)
        {
            bool bRet = false;
            m_strMsg = "h : ";
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byDsiDsr;
            if (m_RFReader.SendCommand("h", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
            {
                uid = new byte[m_abyResBuf[0]];
                Array.Copy(m_abyResBuf, 1, uid, 0, m_abyResBuf[0]);
                bRet = true;
            }
            else
            {
                uid = new byte[1];
                uid[0] = 0;
            }
          
            return bRet;
        }

        public bool REQB(byte byAFI, byte byNBSLOT)
        {
            bool bRet = false;
            m_strMsg = "iso reqb : ";
            m_abyCmdBuf[0] = 0x02;
            m_abyCmdBuf[1] = byAFI;
            m_abyCmdBuf[2] = byNBSLOT;
            if (m_RFReader.SendCommand("iso reqb", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
            {
                Array.Copy(m_abyResBuf, 2, m_abyUID, 0, 4);
                Array.Copy(m_abyResBuf, 10, m_abyProtocol, 0, 3);
                bRet = true;
            }
         
            return bRet;
        }

        public bool SlotMarker(byte bySLOTNB)
        {
            bool bRet = false;
            m_strMsg = "iso slotmarker : ";
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = bySLOTNB;
            if (m_RFReader.SendCommand("iso slotmarker", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }

        public bool HaltB(byte[] abyUID)
        {
            bool bRet = false;
            m_strMsg = "iso hltb : ";
            m_abyCmdBuf[0] = 0x04;
            Array.Copy(abyUID, 0, m_abyCmdBuf, 1, 4);
            if (m_RFReader.SendCommand("iso hltb", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }

        public bool Attrib(byte[] abyUID, byte[] abyProtocol, byte byCID, byte byBitRate)
        {
            bool bRet = false;
            m_strMsg = "iso attrib : ";
            m_abyCmdBuf[0] = 0x09;
            Array.Copy(abyUID, 0, m_abyCmdBuf, 1, 4);
            Array.Copy(abyProtocol, 0, m_abyCmdBuf, 5, 3);
            m_abyCmdBuf[8] = byCID;
            m_abyCmdBuf[9] = byBitRate;
            if (m_RFReader.SendCommand("iso attrib", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
            {

                bRet = true;
            }
           
            return bRet;
        }

        public bool DeSelect(byte byCID)
        {
            bool bRet = false;
            m_strMsg = "deselect : ";
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byCID;
            if (m_RFReader.SendCommand("deselect", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
         
            return bRet;
        }
        public bool WUPA()
        {
            bool bRet = false;
            m_strMsg = "iso wupa : ";
            m_abyCmdBuf[0] = 0;
            m_abyCmdBuf[1] = 0;
            m_abyCmdBuf[2] = 0;
            m_abyCmdBuf[3] = 0;
            m_abyCmdBuf[4] = 0;
            m_abyCmdBuf[5] = 0;
            if (m_RFReader.SendCommand("iso wupa", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }
        public bool Anticollision(byte level)
        {
            bool bRet = false;
            m_strMsg = "iso anticoll:";
            Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
            m_abyCmdBuf[0] = 1;
            m_abyCmdBuf[1] = level;
            if (m_RFReader.SendCommand("iso anticoll", m_abyCmdBuf)
               && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
          
            return bRet;

        }
        public bool Anticollision(byte byLevel, ref byte pLen, byte[] pUID)
        {
            bool bRet = false;
            m_strMsg = "iso anticoll : ";
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byLevel;
            pLen = 0;

            if (m_RFReader.SendCommand("iso anticoll", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
            {
                pLen = m_abyResBuf[0];
                CopyBuf(m_abyResBuf, 1, pUID, 0, pLen);
                bRet = true;
            }
           
            return bRet;
        }
        public bool SelectGetSAK(byte byLevel, byte byLen, byte[] pUID, ref byte pSAK)
        {
            bool bRet = false;
            m_strMsg = "iso select : ";
            m_abyCmdBuf[0] = (byte)(byLen + 1);
            m_abyCmdBuf[1] = byLevel;
            CopyBuf(pUID, 0, m_abyCmdBuf, 2, byLen);

            if (m_RFReader.SendCommand("iso select", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
            {
                pSAK = m_abyResBuf[1];
                bRet = true;
            }
          
            return bRet;
        }
       
        //public bool SelectCardA(byte[] uid)
        //{
        //    bool bRet = false;
        //    m_strMsg = "iso select";

        //    Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
        //    Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);

        //    m_abyCmdBuf[0] = (byte)uid.Length;
        //    Array.Copy(uid, 0, m_abyCmdBuf, 1, m_abyCmdBuf[0]);
        //    if (m_RFReader.SendCommand("iso select", m_abyCmdBuf)
        //       && m_RFReader.GetData(m_abyResBuf))
        //        bRet = true;
        //    m_strMsg += BufToString(m_abyResBuf, m_abyResBuf[0] + 1);
        //    m_RFReader.Lcdchs(m_strMsg);
        //    return bRet;
        //}
        public bool ExchangeAPDU(byte byLen, byte[] abyCardCmd)
        {
            bool bRet = false;
            m_strMsg = "iso apdu:";//"iso hltb : ";
            Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
            m_abyCmdBuf[0] = byLen;

            Array.Copy(abyCardCmd, 0, m_abyCmdBuf, 1, byLen);
            if (m_RFReader.SendCommand(/*"iso hltb"*/
                                                     "iso apdu", m_abyCmdBuf)
                && m_RFReader.GetData(m_abyResBuf))
                bRet = true;
           
            return bRet;
        }
        public bool ExchangeAPDU(byte byLen, byte[] abyCardCmd, out byte[] m_result)
        {
            bool bRet = false;
         //   m_strMsg = "iso apdu:";//"iso hltb : ";
            Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
            Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);
            m_abyCmdBuf[0] = byLen;
            int nbyte = 0;
            Array.Copy(abyCardCmd, 0, m_abyCmdBuf, 1, byLen);
         //   byte[] cmd = Encoding.ASCII.GetBytes("iso apdu\0");//{ (byte)'i', (byte)'s', (byte)'o', (byte)' ', (byte)'a', (byte)'p', (byte)'d', (byte)'u' };
            if (m_RFReader.SendCommand("iso apdu", m_abyCmdBuf))
            {

                if (m_RFReader.GetDataTimeout(m_abyResBuf, ref nbyte, 5 * 1000))
                {
                    m_result = new byte[m_abyResBuf[0]];
                    Array.Copy(m_abyResBuf, 1, m_result, 0, m_abyResBuf[0]);
                    bRet = true;
                }
                else
                {
                    if (nbyte > 0)
                    {
                        m_result = new byte[nbyte];
                        Array.Copy(m_abyResBuf, 1, m_result, 0, m_abyResBuf[0]);
                    }
                    else
                    {
                        bRet = false;
                        m_result = new byte[1];
                        m_result[0] = 0;
                    }
                }
            }
            else
            {
                m_result = new byte[1];
                m_result[0] = 0;
            }



            //if(m_RFReader.SendCommandGetDataTimeout(cmd,m_abyCmdBuf,m_abyResBuf,2500,ref nbyte))
            //{
            //    m_result = new byte[m_abyResBuf[0]];
            //    Array.Copy(m_abyResBuf, 1, m_result, 0, m_abyResBuf[0]);
            //    bRet = true;
            //}

            //m_strMsg += BufToString(m_abyResBuf, m_abyResBuf[0] + 1);
            //m_RFReader.Lcdchs(m_strMsg);
            return bRet;
        }

        public bool ExchangeAPDU(byte byLen, byte[] abyCardCmd, out byte[] m_result,out int nbytesRead, out byte pSw1, out byte pSw2)
        {
            bool bRet = false;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            //   m_strMsg = "iso apdu:";//"iso hltb : ";
            Array.Clear(m_abyCmdBuf, 0, m_abyCmdBuf.Length);
            Array.Clear(m_abyResBuf, 0, m_abyResBuf.Length);
            m_abyCmdBuf[0] = byLen;
            int nbyte = 0;
            nbytesRead = 0;
            Array.Copy(abyCardCmd, 0, m_abyCmdBuf, 1, byLen);
            //   byte[] cmd = Encoding.ASCII.GetBytes("iso apdu\0");//{ (byte)'i', (byte)'s', (byte)'o', (byte)' ', (byte)'a', (byte)'p', (byte)'d', (byte)'u' };
            if (m_RFReader.SendCommand("iso apdu", m_abyCmdBuf))
            {

                if (m_RFReader.GetDataTimeout(m_abyResBuf, ref nbyte, 5 * 1000))
                {
                  
                    bRet = true;
                    m_result = new byte[nbyte];//m_abyResBuf[0]];
                    if (nbyte > 2)
                    {
                        nbytesRead = nbyte - 2;
                        m_result = new byte[nbytesRead];//m_abyResBuf[0]];
                        Array.Copy(m_abyResBuf, 1, m_result, 0, nbytesRead/*nbyte - 3*/);// remove psw1 , psw2 , data length bytes                   
                       
                        pSw1 = m_abyResBuf[m_abyResBuf[0] - 1];
                        pSw2 = m_abyResBuf[m_abyResBuf[0]];
                    }
                    else
                    {
                        m_result = new byte[nbyte];
                        nbytesRead = 0;
                        Array.Copy(m_abyResBuf, 1, m_result, 0, m_abyResBuf[0]);
#if _BIP1300_
                        pSw1 = m_abyResBuf[m_abyResBuf[1]];
                        pSw2 = m_abyResBuf[m_abyResBuf[2]];
#elif _BIP1500_
                        pSw1 = m_abyResBuf[m_abyResBuf[0]-1];
                        pSw2 = m_abyResBuf[m_abyResBuf[0]];
#endif
                    }
                }
                else
                {
                    if (m_abyResBuf[0] <=2)
                    {
                        m_result = new byte[m_abyResBuf[0]];
                        nbytesRead =0;
                        Array.Copy(m_abyResBuf, 1, m_result, 0, m_abyResBuf[0]);
                        pSw1 = m_abyResBuf[m_abyResBuf[0] - 1];
                        pSw2 = m_abyResBuf[m_abyResBuf[0]];
                    }
                    else
                    {
                        bRet = false;
                        m_result = new byte[1];
                        m_result[0] = 0;
                    }
                }
            }
            else
            {
                m_result = new byte[1];
                m_result[0] = 0;
                bRet = false;
            }        
            return bRet;
        }
  
    }

}
