/*Prithvi : Common functions for Command and Response treatments */

using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;

namespace IFS2.Equipment.TicketingRules.CommonFunctions
{
    public static class CFunctions
    {
        /// <summary>
        /// This Function process the APDU Response from sSmartIsoEx of cscApi
        /// Splits the response to
        /// : SW Codes
        /// : Response Data
        /// </summary>
        /// <param name="pResDataOut"></param>
        /// <param name="pRespDataLen"></param>
        /// <param name="pSw1"></param>
        /// <param name="pSw2"></param>
        /// <param name="pResData"></param>
        static public void processApduRes(IntPtr pResDataOut,
                                          IntPtr pRespDataLen,
                                          out byte pSw1,
                                          out byte pSw2,
                                          out byte[] pResData)
        {
            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            try
            {
                unsafe
                {
                    ushort* ResLenPtr = (ushort*)pRespDataLen.ToPointer();

                    int ResLen = *ResLenPtr;

                    byte* opArray = (byte*)pResDataOut.ToPointer();
                    byte[] opArray_ = new byte[ResLen];
                    for (int i = 0; i < ResLen; i++)
                    {
                        opArray_[i] = *opArray;
                        opArray++;
                    }

                    // Checks for any response data, other than SWx codes
                    if (ResLen > CONSTANT.MIN_ISO_DATA_OUT_LENGTH)
                    {
                        pResData = new byte[ResLen - CONSTANT.MIN_ISO_DATA_OUT_LENGTH];

                        Array.Copy(opArray_, 0, pResData, 0, pResData.Length);

                        pSw1 = opArray_[pResData.Length];
                        pSw2 = opArray_[pResData.Length + 1];
                    }
                    else
                    {
                        pSw1 = opArray_[0];
                        pSw2 = opArray_[1];
                    }
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex);
            }
        }

        /// <summary>
        /// Concatenate to two bytes
        /// </summary>
        /// <param name="pB1"></param>
        /// <param name="pB2"></param>
        /// <returns></returns>
        static public byte[] concBytes(byte pB1,
                                       byte pB2)
        {
            byte[] retArr = new byte[] { pB1, pB2 };
            return retArr;
        }

        
        /// <summary>
        /// /* CLS INS P1 P2 - Minimum */ 
        /// </summary>
        /// <param name="pCLA"></param>
        /// <param name="pINS"></param>
        /// <param name="pP1"></param>
        /// <param name="pP2"></param>
        /// <returns></returns>
        static public byte[] getApdu(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2)
        {
            byte[] retApdu = new byte[CONSTANT.MIN_ISO_DATA_IN_LENGTH];

            /* CLA INS P1 P2 - formatting [Mandatory]*/
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            return retApdu;
        }

        /* CLS INS P1 P2 + LC + DATA IN */
        static public byte[] getApdu(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2,
                                        byte[] pDataIn)
        {
            byte[] retApdu = new byte[CONSTANT.MIN_ISO_DATA_IN_LENGTH + CONSTANT.LX_ISO_IN_LENGTH + pDataIn.Length];

            /* CLA INS P1 P2 - formatting [Mandatory] */
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            /* LC */
            retApdu[4] = (byte)pDataIn.Length;

            /* DATA IN */
            Array.Copy(pDataIn, 0, retApdu, 5, pDataIn.Length);

            return retApdu;
        }

        /* CLS INS P1 P2 + LC (Null) + LE */
        static public byte[] getApdu(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2,
                                        byte pLE)
        {
            byte[] retApdu = new byte[CONSTANT.MIN_ISO_DATA_IN_LENGTH + CONSTANT.LX_ISO_IN_LENGTH];

            /* CLA INS P1 P2 - formatting [Mandatory]*/
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            /* LC */
            //retApdu[5] = CONSTANT.NULL;

            /* LE */
            retApdu[4] = pLE;

            return retApdu;
        }

        /* CLS INS P1 P2 + LC + DATA IN + LE  */
        static public byte[] getApdu(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2,
                                        byte[] pDataIn,
                                        byte pLE)
        {

            byte[] retApdu;
            int apduln = 6; //CONSTANT.MIN_ISO_DATA_IN_LENGTH + 2 * CONSTANT.LX_ISO_IN_LENGTH; 
             if (pDataIn != null)  apduln += pDataIn.Length;

             retApdu = new byte[apduln];
            /* CLA INS P1 P2 - formatting [Mandatory]*/
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            /* LC */
            if (pDataIn != null)
            {
                retApdu[4] = (byte)pDataIn.Length;

                /* DATA IN */
                Array.Copy(pDataIn, 0, retApdu, 5, pDataIn.Length);// CONSTANT.MAX_ISO_DATA_OUT_LENGTH - 1);
                /* Length Expected - Optional */
                retApdu[retApdu.Length-1] = pLE;//CONSTANT.MAX_ISO_DATA_OUT_LENGTH
            }
            else
            {
                retApdu[4] = 0x00;
                retApdu[5] = pLE;
            }      

            return retApdu;
        }

        /* Convert Data LSB First - General Use */
        static public ulong ConvertLittleEndian(byte[] pDataIn)
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

        /* Get Data from Bits (LSB), Offset : n Bits */
        static public ulong GetBitData(int pBitOffset, int pSize, byte[] pDataIn)
        {
            //cannot read more than 64 bits.
            if (pSize > 64) pSize = 64;
            //Preparation of bits buffer.
            bool[] b = new bool[64];
            //Initialisation
            for (int i = 0; i < 64; i++) b[i] = false;

            int offset = pBitOffset / 8; //Start offset on byte
            int rest = pBitOffset % 8; //To find the right bit in the byte
            int flag = 0x80; //Is used to test the right bit
            while (rest > 0)
            {
                flag >>= 1;
                rest--;
            }
            //Reading the bits
            for (int i = 0; i < pSize; i++)
            {
                if ((pDataIn[offset] & flag) == 0) b[i] = false;
                else b[i] = true;
                flag >>= 1;
                if (flag == 0)
                {
                    flag = 0x80;
                    offset++;
                }
            }
            //We have now a table of equivalent of bits in the right order.
            //Low value is the first 8 bits
            ulong result = 0;
            int k = 0; // Count the number of multiple of 8
            while (pSize > 0)
            {
                byte val8 = 0;
                int max = 8;
                if (pSize < 8) max = pSize;
                for (int i = max; i > 0; i--)
                {
                    byte j;
                    if (i == max) j = 1;
                    else j = ((byte)(0x1 << (max - i)));
                    if (b[i - 1 + k * 8]) val8 |= j;
                }
                if (k != 0) result += ((ulong)val8 << (8 * k));
                else result += (val8);
                pSize -= 8;
                k++;
            }
            return result;
        }

        /* Get Data in (Bytes) from Response, Offset : n Bits */
        static public byte[] GetBytesFromResp(int pBitOffset, int pSize, byte[] pDataIn)
        {
            BitArray pBitArray = new BitArray(pDataIn);

            BitArray trimBits = new BitArray(pSize);

            // Trim the required bits from the buffer
            for (int i = pBitOffset; i < pBitOffset + pSize; i++)
            {
                trimBits[i - pBitOffset] = pBitArray[i];
            }

            byte[] byteArray = new byte[(int)Math.Ceiling((double)trimBits.Length / 8)];
            trimBits.CopyTo(byteArray, 0);

            return byteArray;
        }

        /// <summary>
        /// Get the Key Reference Byte for Mifare Virtual SAM
        /// as per cscApi documentation
        /// </summary>
        /// <param name="pAut"></param>
        /// <param name="pKeySet"></param>
        /// <param name="pDiv"></param>
        /// <param name="pKeyNum"></param>
        /// <returns></returns>
        static public byte GetKeyReference(int  pAut,
                                           int  pKeySet,
                                           int  pDiv,
                                           int  pKeyNum)
        {
            string RefBin = Convert.ToString(pAut, 2).PadLeft(1, '0');
            RefBin += Convert.ToString(pKeySet, 2).PadLeft(2, '0');
            RefBin += Convert.ToString(pDiv, 2).PadLeft(1, '0');
            RefBin += Convert.ToString(pKeyNum, 2).PadLeft(4, '0');

            Int16 KeyReference = Convert.ToInt16(RefBin, 2);
            return (byte)KeyReference;
        }

        /* Convert Dos date representation from the Response */
        static public DateTime ConvertDosDate(int pBitOffset, byte[] pDataIn)
        {
            DateTime noDate = new DateTime(1980, 01, 01);

            try
            {
                ulong ulongdate = (ulong)GetBitData(pBitOffset, 16, pDataIn);
                int day = (int)(ulongdate & 0x1F);

                int month = (int)((ulongdate >> 5) & 0xF);

                int year = (int)((ulongdate >> 9) & 0x7F) + 1980;

                if (day != 0 && month != 0)
                {
                    DateTime Date = new DateTime(year, month, day);

                    return Date;
                }
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, Ex.Message + Ex.StackTrace);
            }

            return noDate;
        }

        static public DateTime ConvertDosTime(int pBitOffset, byte[] pDataIn)
        {
            DateTime noDate = new DateTime(1980, 01, 01);

            try
            {
                ushort tim = (ushort)GetBitData(pBitOffset, 16, pDataIn);

                int seconds = (int)(tim & 0x1F) * 2;
                if (seconds > 59)
                {
                    // only to cope with the case that in earlier application, 5-bit restriction had got ignored; and it would be too punishing to ignore the date altogether.
                    seconds = (int)(tim & 0x1F);
                }
                int minute = (int)((tim >> 5) & 0x3F);
                int hour = (int)(tim >> 11);

                return new DateTime(2000, 1, 1, hour, minute, seconds);
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, Ex.Message + Ex.StackTrace);
            }

            return noDate;
        }

        static public DateTime MergeDateTime(DateTime dt, DateTime tim)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, tim.Hour, tim.Minute, tim.Second);
        }

        private readonly static DateTime MinDosDate = new DateTime(1980, 1, 1);
        /// <summary>
        /// From DateTime to DosDate format Convertor
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>DosDate (16bit int)</returns>
        static public ushort ToDosDate(this DateTime dateTime)
        {
            if (dateTime < MinDosDate)
                dateTime = MinDosDate;

            uint day = (uint)dateTime.Day;              // Between 1 and 31
            uint month = (uint)dateTime.Month;          // Between 1 and 12
            uint years = (uint)(dateTime.Year - 1980);  // From 1980

            if (years > 127)
                throw new ArgumentOutOfRangeException("Cannot represent the year.");

            uint dosDateTime = 0;
            dosDateTime |= day << (16 - 16);
            dosDateTime |= month << (21 - 16);
            dosDateTime |= years << (25 - 16);

            return unchecked((ushort)dosDateTime);
        }
        static public ushort ToDosDate2(this DateTime dateTime)
        {
            return ToDosDate(dateTime, new DateTime(1990, 1, 1));
        }

        static public ushort ToDosDate(this DateTime dateTime,DateTime minimum)
        {
            if (dateTime < minimum)
                dateTime = minimum;

            uint day = (uint)dateTime.Day;              // Between 1 and 31
            uint month = (uint)dateTime.Month;          // Between 1 and 12
            uint years = (uint)(dateTime.Year - minimum.Year);  // From year of minimum

            if (years > 127)
                throw new ArgumentOutOfRangeException("Cannot represent the year.");

            uint dosDateTime = 0;
            dosDateTime |= day << (16 - 16);
            dosDateTime |= month << (21 - 16);
            dosDateTime |= years << (25 - 16);

            return unchecked((ushort)dosDateTime);
        }

        static public void GetDosDateTime(DateTime dt, bool bConvertToUTC, out ushort dosDate, out ushort dosTime)
        {
//#if WindowsCE
            if (bConvertToUTC)
                dt = dt.ToUniversalTime();

            dosDate = dt.ToDosDate();
            dosTime = dt.ToDosTime();
//#else
//            if (bConvertToUTC)
//                dt = TimeZoneInfo.ConvertTimeToUtc(dt);
               
//            dosDate = CFunctions.ToDosDate(dt);
//            dosTime = CFunctions.ToDosTime(dt);
//#endif
        }

        /// <summary>
        /// From DateTime to DosTime format Convertor
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>DosDate (16bit int)</returns>
        static public ushort ToDosTime(this DateTime dateTime)
        {
            uint hours = (uint)dateTime.Hour;
            uint minutes = (uint)dateTime.Minute;
            uint seconds = (uint)dateTime.Second;

            uint dosTime = 0;
            dosTime |= (uint)(seconds/2) << 0;
            dosTime |= minutes << 5;
            dosTime |= hours << 11;

            return unchecked((ushort)dosTime);
        }

        /// <summary>
        /// Convert From UnixTimeStamp to Date and Time
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns>DateTime</returns>
        static public DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Convert From Date time to Unix Time Stamp
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        static public int ConvertToUnixTimestamp(DateTime dt)
        {
            //create Timespan by subtracting the value provided from
            //the Unix Epoch
            TimeSpan span = (dt - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

            //return the total seconds (which is a UNIX timestamp)
            return (int)span.TotalSeconds;
        }


        /// <summary>
        /// Get the Number of Days between Two dates
        /// </summary>
        /// <param name="pStartDate"></param>
        /// <param name="pCurrentDate"></param>
        /// <returns>Days</returns>
        static public int GetNumberOfDays(DateTime pStartDate, DateTime pCurrentDate)
        {
            TimeSpan span = pCurrentDate - pStartDate;
            return (int)span.TotalDays;
        }

        static public byte[] ConvertToBytesLE(BitArray pBits)
        {
            byte[] byteArray = new byte[(int)Math.Ceiling((double)pBits.Length / 8)];
            pBits.CopyTo(byteArray, 0);

            return byteArray;
        }

        //pSize represents the size of boolbuffer. Shall be a multiple of 8.
        static public byte[] ConvertBoolTableToBytes(bool[] boolbuffer,int pSize)
        {
            int j = pSize/8; //The size of bytes that 
            if ((pSize%8)!=0) j++; //Normally not the case
            byte[] tab = new byte[j];
            for (int i=0; i<j && pSize>0;i++)
            {
                byte val = 0;
                byte flag = 0x80;
                for (int k = 0; k < 8 && pSize > 0; k++,pSize--)
                {
                    if (boolbuffer[i * 8 + k]) val |= flag;
                    flag >>= 1;
                }
                tab[i]=val;
            }
            return tab;
        }

        /// <summary>
        /// Convert to Little Endian Bytes Array from a Int64 Value
        /// </summary>
        /// <param name="pValue"></param>
        /// <returns>byte[]</returns>
        static public byte[] ConvertToBytesLE(Int64 pValue)
        {
            return DataConverter.GetBytesLE(pValue);
        }

        /// <summary>
        /// Convert to Little Endian Bytes Array from a Int32 Value
        /// </summary>
        /// <param name="pValue"></param>
        /// <returns>byte[]</returns>
        static public byte[] ConvertToBytesLE(Int32 pValue)
        {
            return DataConverter.GetBytesLE(pValue);
        }

        /// <summary>
        /// Convert to Little Endian Bytes Array from a Int16 Value
        /// </summary>
        /// <param name="pValue"></param>
        /// <returns>byte[]</returns>
        static public byte[] ConvertToBytesLE(Int16 pValue)
        {
            return DataConverter.GetBytesLE(pValue);
        }

        /// <summary>
        /// Convert to Little Endian Bytes Array from a ushort Value
        /// </summary>
        /// <param name="pValue"></param>
        /// <returns>byte[]</returns>
        static public byte[] ConvertToBytesLE(ushort pValue)
        {
            return DataConverter.GetBytesLE(pValue);
        }
        
        static public UInt64 ConvertFromByteLE(byte[] pBytes)
        {
            return DataConverter.UInt64FromLE(pBytes, 0);
        }

         static public UInt32 ConvertU32FromByteLE(byte[] pBytes)
        {
            return DataConverter.UInt32FromLE(pBytes, 0);
        }

        static public UInt64 ConvertFromByteBE(byte[] pBytes)
        {
            return DataConverter.UInt64FromBE(pBytes, 0);
        }

        static public UInt64 ConvertFromByteNative(byte[] pBytes)
        {
            return DataConverter.UInt64FromNative(pBytes, 0);
        }

        static public byte[] ConvertToBytesBE(ushort pValue)
        {
            return DataConverter.GetBytesBE(pValue);
        }
                 
        static public byte[] GetBytesFromStr(string pBin)
        {
            int numOfBytes = pBin.Length / 8;
            byte[] bytes = new byte[numOfBytes];
            for (int i = 0; i < numOfBytes; ++i)
            {
                bytes[i] = Convert.ToByte(pBin.Substring(8 * i, 8), 2);
            }

            return bytes;
        }       


        /// <summary>
        /// Covertor for Byte Array to Simple String format 
        /// Ex : [0x78], [0xAB], .. -> 78AB...
        /// </summary>
        /// <param name="pInput"></param>
        /// <returns></returns>
        static public string ByteArrayToString(byte[] pInput)
        {
            ASCIIEncoding enc = new ASCIIEncoding();
#if WindowsCE
            string str = enc.GetString(pInput,0,pInput.Length);
#else
            string str = enc.GetString(pInput);
#endif
            return str;
        }

        static public string ByteArrayToStringAssumingNullTerminated(byte[] pInput)
        {
            List<byte> lst = new List<byte>();
            for (int i = 0; i < pInput.Length; i++)
            {
                if (pInput[i] == 0)
                    break;
                lst.Add(pInput[i]);
            }
            ASCIIEncoding enc = new ASCIIEncoding();
#if WindowsCE
            string str = enc.GetString(pInput,0,pInput.Length);
#else
            string str = enc.GetString(lst.ToArray());
#endif
            return str;
        }

        /// <summary>
        /// Covertor for Byte Array to Hex String format
        /// Ex : [0x78], [0xAB], .. -> 0x780xAB...
        /// </summary>
        /// <param name="pInput"></param>
        /// <returns></returns>
        static public string ByteArrayToHexString(byte[] pInput)
        {
            StringBuilder hex = new StringBuilder(pInput.Length * 2);
            foreach (byte b in pInput)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        /// <summary>
        /// Convertor for Hex coded in String to byte[]
        /// ex :- 78AB -> [0x78], [0xAB]
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        static public int ConvertToBits(string pValue, int startInResultBuffer, int pSizeInBitsToAccomodatein, bool[] buffer)
        {
            int pSizeInBytesToAccomodatein = (int)(pSizeInBitsToAccomodatein / 8);
            var bits = new BitArray(pValue.ToCharArray().Select(x=>(byte)x).ToArray());
            int curBit = startInResultBuffer;
            int cntBitsInserted = 0;
            for (; cntBitsInserted <= pSizeInBytesToAccomodatein * 8 && cntBitsInserted < bits.Count; )
                buffer[curBit++] = bits[cntBitsInserted++];

            for (; cntBitsInserted <= pSizeInBytesToAccomodatein * 8; cntBitsInserted++)
                buffer[curBit++] = false;
            return startInResultBuffer + pSizeInBytesToAccomodatein * 8;
        }

        /// <summary>
        /// Converts to bits array following ASN1.0 UPER standard
        /// </summary>
        /// <param name="pValue"></param>
        /// <param name="start"></param>
        /// <param name="pSize"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        static public int ConvertToBitsASN1_UPER(int pValue, int start, int pSize, bool[] buffer)
        {
            int i;
            bool[] bf = new bool[pSize];
            byte[] databytes = BitConverter.GetBytes(pValue);
            for (i = 0; i < pSize; i++)
            {
                buffer[start + i] = false;
                bf[i] = false;
            }

            BitArray b = new BitArray(new int[] { pValue });
            bool[] bits = new bool[b.Count];
            b.CopyTo(bits, 0);
            Array.Copy(bits, 0, bf, 0, pSize);

            Array.Reverse(bf);

            Array.Copy(bf, 0, buffer, start, pSize);

            start += pSize;
            return start;
        }
        /// <summary>
        /// Convert to Bits, with Size to be the number of Bits
        /// </summary>
        /// <param name="pValue"></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        static public int ConvertToBits(ulong pValue,int start, int pSize, bool[] buffer)
        {
            //Taille limitée à 64 bits.
            if (pSize > 64) pSize = 64;
            //First initialisation of all bits to false
            int i;
            for (i=0; i < pSize; i++) buffer[start+i]=false;

            i = start;
            while(pSize>0)
            {
                int j = 8;
                if ( pSize < 8) j=pSize;
                //Preparation of tag to read byte
                byte flag = 0x1;
                if (j > 1)
                {
                    for (int k = 0; k < (j-1); k++) flag <<= 1;
                }
                byte val = (byte)(pValue & 0xFF);
                while (flag>0)
                {
                    if ((val & flag) == 0) buffer[i] = false;
                    else buffer[i] = true;
                    i++;
                    flag>>=1;
                }
                pValue >>= 8;
                pSize -= 8;
            }
            return i;

            ////int k = (pSize / 8); // 
            ////pSize 
            //string binaryString = Convert.ToString(pValue, 2); //Convert to binary in a string

            //BitArray retBits = new BitArray(pSize);

            //int[] bits = binaryString.PadLeft(pSize, '0') // Add 0's from left
            //             .Select(c => int.Parse(c.ToString())) // convert each char to int
            //             .ToArray(); // Convert IEnumerable from select to Array

            ////Check Bits formed -- Testing Snippet --
            //for (int i = 0; i < bits.Count(); i++)
            //{
            //    retBits[i] = Convert.ToBoolean(bits[i]);
            //}

            //return retBits;
        }

        static public byte ConvertToByte(BitArray bits)
        {
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        static public int getIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];

        }
        /// <summary>
        /// Function to convert Binary String recived from Cryptoflex certificate
        /// binary data to Date format YYYY-MM-DD / HH:MM:SS
        /// </summary>
        /// <param name="pBin"></param>
        /// <returns></returns>
        static public string FormartDateStrFromBin(byte[] pBin)
        {
            string retDateStr = "";

            char[] bca = CFunctions.ByteArrayToString(pBin).ToCharArray();

            for (int i = 0; i < bca.Length; i++)
            {
                if (i == 4 || i == 6)
                {
                    retDateStr += "-";
                }
                else if(i == 10 || i == 12)
                {
                    retDateStr += ":";
                }
                else if (i == 8)
                {
                    retDateStr += " ";
                }

                retDateStr += bca[i];
            }

            return retDateStr;
        }


        /// <summary>
        /// Function extracts the subject data from Cryptoflex certificate
        /// Binary data to Subject ex DMRC.Line1.Ca...
        /// </summary>
        /// <param name="pBin"></param>
        /// <returns></returns>
        static public string FormartSubjectFromBin(byte[] pBin)
        {
            string retDateStr = "";

            char[] sca = CFunctions.ByteArrayToString(pBin).ToCharArray();
            int i = 0;

            do
            {
                retDateStr += sca[i];
                i++;
            } while (sca[i]!='\0');
            return retDateStr;
        }

        /// <summary>
        /// Only for Internal Tests, never to be used in Production.
        /// Better Comment and Complile for Production
        /// </summary>
        /// <param name="pBitArray"></param>
        static public void PrintBitsConsole(BitArray pBitArray)
        {
            //Check Bits formed -- Testing Snippet --
            for (int i = 0; i < pBitArray.Count; i++)
            {
                bool bit = pBitArray.Get(i);
                Console.Write(bit ? 1 : 0);
            }

            Console.WriteLine();
#if WindowsCE
            Console.WriteLine("Enter Q to continue");
            Console.ReadLine();
#else
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();
#endif
        }
        
    }
}
