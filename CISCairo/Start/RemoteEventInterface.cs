using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class RemoteEventInterface : MarshalByRefObject, iRemoteMessageInterface
    {
        public RemoteEventInterface()
        {
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public bool Message(string source, string type, string eventID, string[] pars)
        {
            try
            {
                Communication.SendMessage(source, type, eventID, pars);
                return (true);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error receiving message " + eventID + " Error:" + e.Message);
                return (false);
            }
        }
    }
}
