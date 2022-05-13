using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public interface IMediaTreatment
    {
        bool ReadAndValidate(StatusCSCEx status, out TTErrorTypes validationResult
            , out LogicalMedia logMedia // TODO: remove it if not needed
            );
        void ResumeWrite();
        Guid Id { get; }
    }
}