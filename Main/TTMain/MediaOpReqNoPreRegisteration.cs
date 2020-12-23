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
    public abstract class MediaOpReqNoPreRegisteration : MediaOpGen, IUpdateMediaNonPreRegisteredOp
    {
        public MediaOpReqNoPreRegisteration(
            MainTicketingRules ticketingRules,
            string logicalMediaReference,
            string parsXml
            )
            : base(ticketingRules)
        {            
            _logicalMediaReferenceString = logicalMediaReference;
            if (logicalMediaReference != null)
                _logicalMediaReference = new LogicalMedia(logicalMediaReference);
            _parsXml = parsXml;
        }

        protected LogicalMedia _logicalMediaReference;
        public readonly string _logicalMediaReferenceString;
        public readonly string _parsXml;

        #region IUpdateMediaNonPreRegisteredOp Members

        public void SetMediaSerialNumber(long sNum)
        {
            Logging.Log(LogLevel.Verbose, "SetMediaSerialNumber = " + sNum.ToString());
            _mediaSrNbr = sNum; // but bad thing is that the object may set it back to null, if it realizes that the operation is not possible on this media (perhaps just because of incorrect media type)
        }

        public abstract bool DoesNeedTokenDispenser();

        #endregion

         public override abstract MediaOpType GetOpType();
    }
}