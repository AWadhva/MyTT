using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules.SecurityModuleInitializer
{
    interface SAM
    {
        object Initialize(out bool bPresent, out bool bWorking);
        object GetStatus();
        string GetSerialNumber();
        bool IsPresent();
    }
}
