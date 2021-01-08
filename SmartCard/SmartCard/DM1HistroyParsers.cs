using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{
    class DM1HistoryParser_Ver0 : IDM1HistoryParser
    {
        readonly int OFFSET;
        readonly byte[] pResData;
        public DM1HistoryParser_Ver0(byte[] pResData_, int idx)
        {
            pResData = pResData_;
            OFFSET = idx * 32 * 8;
        }        

        public int Location()
        {
            return (int)CFunctions.GetBitData(OFFSET + 142, 8, pResData);
        }
    }

    class DM1HistoryParser_Ver1 : IDM1HistoryParser
    {
        readonly int OFFSET;
        readonly byte[] pResData;
        public DM1HistoryParser_Ver1(byte[] pResData_, int idx)
        {
            pResData = pResData_;
            OFFSET = idx * 32 * 8;
        }

        public int Location()
        {
            int lsb = (int)CFunctions.GetBitData(OFFSET + 142, 8, pResData);
            int msb = (int)CFunctions.GetBitData(OFFSET + 161, 2, pResData);

            return 256*msb + lsb;
        }
    }
}
