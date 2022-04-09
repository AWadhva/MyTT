using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    class MacCalculator : IMacCalcultor
    {
        #region IMacCalcultor Members

        public byte[] Calculate(LogicalMedia logMedia)
        {
            byte[] MacDataBuf = new byte[2 * CONSTANT.MIFARE_ULTRALT_BLOC_SIZE - 8];
            Array.Copy(logMedia._tokenPhysicalData, MacDataBuf, 2 * CONSTANT.MIFARE_ULTRALT_BLOC_SIZE - 8);
            return SecurityMgr.Instance.GenerateMAC(MacDataBuf);

        }

        public bool VerfiyMac(LogicalMedia logMedia)
        {
            var idealMac = Calculate(logMedia);
            byte[] macbytesPresent = new byte[8];
            Array.Copy(logMedia._tokenPhysicalData, 24, macbytesPresent, 0, 8);
            return idealMac.SequenceEqual(macbytesPresent);
        }

        #endregion
    }
}
