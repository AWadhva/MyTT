using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public abstract class MediaOpReqPreRegisteration : MediaOpGen, IUpdateMediaPreRegisteredOp
    {
        public MediaOpReqPreRegisteration(
            MainTicketingRules parent,
            Int64 mediaSNum,
            LogicalMedia mediaDataPriorToOperation
            )
            : base(parent)
        {
            _mediaSrNbr = mediaSNum;
            _logicalMediaPriorToOperation = mediaDataPriorToOperation;
            if (_logicalMediaPriorToOperation.DelhiUltralightRaw != null)
                _logicalMediaPriorToOperation.DelhiUltralightRaw.Hidden = true;

            if (_logicalMediaPriorToOperation.DESFireDelhiLayout != null)
                _logicalMediaPriorToOperation.DESFireDelhiLayout.Hidden = true;
        }

        protected readonly LogicalMedia _logicalMediaPriorToOperation = null;
    }
}