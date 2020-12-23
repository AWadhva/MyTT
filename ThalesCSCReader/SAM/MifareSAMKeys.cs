using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public class MifareSAMKeys
    {
        public byte keyNum;
        public byte keyVersion;
        public byte[] key = new byte[16];
    }
    
}
