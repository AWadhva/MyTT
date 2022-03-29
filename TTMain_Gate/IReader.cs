using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    abstract public class IReader
    {
        abstract public void StartPolling();
        abstract public void StopPolling();
    }
}
