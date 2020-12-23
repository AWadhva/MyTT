using System;
using System.Collections.Generic;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public enum CSC_CCHS_RejectReason
    {
        InsufficientRemainingValue = 0,
        IllegalTicketType,
        IllegalCSCType,
        EntryBitMismatch,
        ExitBitMismatch,
        TestTicketUsedInRevenue,
        RevenueTicketUsedInMaintenance,
        TicketTypeNotPermitted,
        ExcessTime,
        TickedExpired,
        PassExpired,
        ExcesiveRemainingValue,
        AlreadyBlocked,
        AddValueExpiry,
        InvalidCSCConfiguration,
        LoginFailure,
        IllegalIssuer,
        UsageRistriction,
        BankTopUpAppliRejected,
        CSCExpired,
        Blacklisted,
        ExcessiveReadFailure,
        ExcessiveWriteFailure,
        CSCTicketNotActive,
        InvalidEntryStation,
        IssureNotSupported = 26,

        LastAddValueDeviceBlacklisted = 30,
        NotRejected = 255

    }

    public enum EndOfValidityMode { Fixed = 1, CurrentPlusYears = 2 }
}
