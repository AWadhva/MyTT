using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.Rules
{
    static public class AllPurpose
    {
        static public TTErrorTypes CheckIfMediaIsBlocked(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (logMedia.Media.Blocked)
                    return TTErrorTypes.MediaBlocked;
            return TTErrorTypes.NoError;
        }

        static public TTErrorTypes CheckMediaExpiry(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (DateTime.Now > logMedia.Media.ExpiryDate)
                    return TTErrorTypes.MediaEndOfValidityReached;
            
            return TTErrorTypes.NoError;
        }

        static public TTErrorTypes CheckForLastOperationEquipmentBlacklisted(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (!EquipmentDenyList.VerifyEquipment(logMedia.Purse.LastAddValue.EquipmentNumber, 0))
                    return TTErrorTypes.LastAddValueDeviceBlacklisted;

            return TTErrorTypes.NoError;
        }

        static public TTErrorTypes EF_CSCC_ControlTicketNotSurrendered(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (logMedia.Media.Status == Media.StatusValues.Surrendered)
                    return TTErrorTypes.SurrenderedMedia;
            return TTErrorTypes.NoError;
        }

        static public TTErrorTypes EF_TOM_ControlCSCCIssuanceData(LogicalMedia logMedia)
        {
            if (logMedia.Media.Type == Media.TypeValues.CSC)
                if (logMedia.Initialisation.DateTime > DateTime.Now)
                    return TTErrorTypes.TPERR_IssuanceReject;
            return TTErrorTypes.NoError;
        }

        static public TTErrorTypes CheckForMediaBlackList(LogicalMedia logMedia)
        {
            if (MediaDenyList.VerifyMedia((int)logMedia.Media.HardwareType, logMedia.Media.ChipSerialNumber))
            {
                logMedia.Media.Blocked = true;
                logMedia.Media.ReasonOfBlocking = MediaDenyList.CurrentMedia.Reason;
                logMedia.Application.TransportApplication.Blocked = true;
                logMedia.Application.TransportApplication.ReasonOfBlocking = MediaDenyList.CurrentMedia.Reason;

                return TTErrorTypes.MediaInDenyList;
            }
            return TTErrorTypes.NoError;
        }
    }
}