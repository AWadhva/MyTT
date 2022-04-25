using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace IFS2.Equipment.TicketingRules
{
    interface ISupervisor
    {
        bool MediaDetected(StatusCSCEx serialNumber);
        //bool MediaRead(LogicalMedia logMedia);
        //void MediaProcessed(LogicalMedia logMedia);
    }
}
