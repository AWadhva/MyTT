using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.TicketingRules.Gate.MediaTreatment;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules.Gate
{
    public interface ITransmitter
    {
        void MediaTreated(int rdrMnemonic, ActionTaken act, params string[] pars);
        void AgentCardTreated(int rdrMnemonic, AgentCardAction act, params string[] pars);
        void ReaderConnected(int rdrMnemonic);
        void ReaderDisconnected(int rdrMnemonic);
        void MediaProduced(int rdrMnemonic);
        void MediaRemoved(int rdrMnemonic);
    }
}