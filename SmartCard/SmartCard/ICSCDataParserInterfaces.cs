using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{
    interface IDM1HistoryParser
    {
        int Location();
    }

    interface IDM2ValidationParser
    {
        DateTime DateOfFirstTransaction();
        Validation.TypeValues EntryExitBit();
        int Location();
        DateTime LastTransactionDateTime();
        short RejectCode();
        TransportApplication.StatusValues Status();
        bool Test();
        int BonusValue();
        short AgentRemainingTrips();
    }
}
