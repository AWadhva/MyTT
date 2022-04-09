using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public interface IMacCalcultor
    {
        byte[] Calculate(LogicalMedia logMedia);
        bool VerfiyMac(LogicalMedia logMedia);
    }
}
