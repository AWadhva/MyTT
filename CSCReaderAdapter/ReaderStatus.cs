using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public class ReaderStatus
    {
        public byte ReaderState;
        public int xCardType;
        public byte[] CardId;

        public ReaderStatus()
        {
            ReaderState = 0;
            xCardType = 0;            
            CardId = new byte[CONSTANT.MAX_ATR_SIZE];
        }
    }
}
