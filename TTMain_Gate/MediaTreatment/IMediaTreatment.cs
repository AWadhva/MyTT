using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    interface IMediaTreatment
    {
        void Do(StatusCSCEx status);
        void Resume();
    }
}
