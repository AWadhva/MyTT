using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public interface IV4ReaderApi
    {
        short sCSCReaderGetApiVersionEx(out int piMajorVersion, out int piMinorVersion);
        short sCSCReaderStartEx(string pszComName, int ulSpeed, out int phRw);
        short sCSCReaderStopEx(int phRw);
    }
}
