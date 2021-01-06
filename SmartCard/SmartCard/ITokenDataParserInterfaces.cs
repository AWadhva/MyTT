using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{
    interface ISTDParser
    {
        DateTime Initialisationdate();
        DateTime SaleDate();
        short DesignType();
        Customer.LanguageValues Language();
        short Owner();
        short FareTier();
        int Location();
    }

    interface IVTDParser
    {
        int SeqNum();
        TransportApplication.StatusValues Status();
        short LogicalTokenType();
        int SaleEquipmentNumber();
        int EntryExitStationCode();
        DateTime LastTransactionDateTime();
        int Destination();
        short RejectCode();
        Validation.TypeValues EntryExitBit();
        bool Test();
        int Amount();
        byte JourneyManagement();
    }
}
