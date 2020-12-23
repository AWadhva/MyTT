using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    public class IReaderInterface
    {
        //private bool _isConnected;
        virtual public bool InitReader(int readertype, string readerport, int samtype, int samslot)
        {
            return false;
        }
        virtual public bool IsoCommandExe(DEST_TYPE pDestType,
                                                byte[] pCommandApdu,
                                                out byte pSw1,
                                                out byte pSw2,
                                                out byte[] pResData)
        {
            pSw1 = 0xff;
            pSw2 = 0xff;
            pResData = new byte[1];
            return false;
        }
        virtual public bool IsReaderConnected()
        {
            return false;
        }
    }
}
