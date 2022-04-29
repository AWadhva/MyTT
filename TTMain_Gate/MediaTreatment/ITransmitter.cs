using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public interface ITransmitter
    {
        void Transmit(int rdrMnemonic, ActionTaken act, params string[] pars);
        // other functions like readerConnected, readerDisconnected, mediaProduced, mediaRemoved
    }
}