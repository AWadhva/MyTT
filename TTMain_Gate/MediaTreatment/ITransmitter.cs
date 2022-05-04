using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public interface ITransmitter
    {
        void MediaTreated(int rdrMnemonic, ActionTaken act, params string[] pars);
        void ReaderConnected(int rdrMnemonic);
        void ReaderDisconnected(int rdrMnemonic);
        void MediaProduced(int rdrMnemonic);
        void MediaRemoved(int rdrMnemonic);
    }
}