using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace Common
{
    /// <summary>
    /// a R/W type agnostic class, telling the brief about the media produced in front of a R/W
    /// </summary>
    public class StatusCSCEx
    {
        public StatusCSCEx(StatusCSC pStatusCSC)
        {
            cardTyp = (CSC_TYPE)pStatusCSC.xCardType;

            byte[] serialNbrBytes = new byte[8];

            if (pStatusCSC.xCardType == (int)CSC_TYPE.CARD_MIFARE1 && pStatusCSC.ucLgATR == 12)
            {
                byte[] ba = pStatusCSC.ucATR;
                //Logging.Log(LogLevel.Verbose, "ba.Length = " + ba.Length);
                byte SAK = ba[2];
                var typ = ((SAK >> 3) & 0x7);
                if (typ == 0)
                {
                    IsUltraLight = true;
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
                else if (typ == 4)
                {
                    IsDesFire = true;                    
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
                else if (typ == 5) // NFC Desfire is detected....
                {
                    IsNFC = true;
                    Array.Copy(ba, 3, serialNbrBytes, 0, 7);
                }
                else
                {
                    IsUnsupported = true;
                }
            }
            SerialNumber = ConvertSNbr(serialNbrBytes);
        }
        
        public CSC_TYPE cardTyp { private set; get; }
        public bool IsUltraLight { private set; get; }
        public bool IsDesFire { private set; get; }
        public bool IsNFC { private set; get; }
        public bool IsUnsupported { private set; get; }

        public long SerialNumber { private set; get; }

        private long ConvertSNbr(byte[] serialNbrBytes)
        {
            long SrNbr = 0;
            for (int i = 0; i < 7; i++)
            {
                SrNbr *= 256;
                SrNbr += serialNbrBytes[i];
            }
            return SrNbr;
        }
    }
}