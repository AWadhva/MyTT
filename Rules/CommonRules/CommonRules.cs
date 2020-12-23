using System;
using System.Collections.Generic;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public static class CommonRules
    {


        static public TTErrorTypes CheckAgentData(LogicalMedia logMedia)
        {
            try
            {
                if (logMedia.Application != null && logMedia.Application.Agent != null)
                {
                    if (!AgentList.IsValidAgent(logMedia.Application.Agent.ReferenceRead))
                        return TTErrorTypes.AgentCard_AgentNotPresentInEOD;
                    else
                        return TTErrorTypes.NoError;
                }
                else
                    return TTErrorTypes.NoError;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules_CheckAgentData " + e.Message);
                return TTErrorTypes.BadAgentData;
            }
        }

        static public TTErrorTypes CheckMediaData(LogicalMedia logMedia)
        {
            try
            {
                if (logMedia.Media.OperationalType == MediaDescription.OperationalTypeValues.Agent) return TTErrorTypes.NoError;
                //Check if media is blocked
                if (logMedia.Media.Blocked) return TTErrorTypes.MediaBlocked;
                //Check of media status
                //if (logMedia.Media.Status == Media.StatusValues.NotInitialised) return TTErrorTypes.MediaNotInitialised;
                if (logMedia.Media.Status != Media.StatusValues.Issued) return TTErrorTypes.CardNotIssued;
                //Check if Media is in Blacklist
                Logging.Log(LogLevel.Verbose, "Check Media " + logMedia.Media.ChipSerialNumber.ToString() + " Type:" + ((int)logMedia.Media.HardwareType).ToString());
                if (MediaDenyList.VerifyMedia((int)logMedia.Media.HardwareType, logMedia.Media.ChipSerialNumber))
                {
                    logMedia.Media.Blocked = true;
                    logMedia.Media.ReasonOfBlocking = MediaDenyList.CurrentMedia.Reason;
                    logMedia.Application.TransportApplication.Blocked = true;
                    logMedia.Application.TransportApplication.ReasonOfBlocking = MediaDenyList.CurrentMedia.Reason;
                    return TTErrorTypes.MediaInDenyList;
                }
               if (!EquipmentDenyList.VerifyEquipment(logMedia.Purse.LastAddValue.EquipmentNumber, 0))
                    return TTErrorTypes.LastAddValueDeviceBlacklisted;

                if (_bCheckForMediaExpiry)
                {
                    if (DateTime.Now > logMedia.Media.ExpiryDate)
                        return TTErrorTypes.MediaEndOfValidityReached;
                }
                return TTErrorTypes.NoError;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules_CheckMediaData " + e.Message);
                return TTErrorTypes.Exception;
            }
        }

        static public TTErrorTypes CheckMediaDataForBlockingBlackListingEtc(LogicalMedia logMedia)
        {
            try
            {
                // TODO: Can't an agent card be blocked?? If yes, then we are not scanning through the deny list, which is incorrect.
                if (logMedia.Media.OperationalType == MediaDescription.OperationalTypeValues.Agent) return TTErrorTypes.NoError;
                //Check if media is blocked
                if (logMedia.Media.Blocked) return TTErrorTypes.MediaBlocked;

                //Check if Media is in Blacklist
                Logging.Log(LogLevel.Verbose, "Check Media " + logMedia.Media.ChipSerialNumber.ToString() + " Type:" + ((int)logMedia.Media.HardwareType).ToString());
                if (MediaDenyList.VerifyMedia((int)logMedia.Media.HardwareType, logMedia.Media.ChipSerialNumber))
                    return TTErrorTypes.MediaInDenyList;                
                if (!EquipmentDenyList.VerifyEquipment(logMedia.Purse.LastAddValue.EquipmentNumber, 0))
                    return TTErrorTypes.LastAddValueDeviceBlacklisted;

                return TTErrorTypes.NoError;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules_CheckMediaData " + e.Message);
                return TTErrorTypes.Exception;
            }
        }

        static bool _bCheckForMediaExpiry;
        static CommonRules()
        {
            _bCheckForMediaExpiry = (bool)Configuration.ReadParameter("CheckForMediaExpiry", "bool", "false");
            _inst = FareProductSpecs.GetInstance();
        }

        static public TTErrorTypes SetupMediaEndOfValidity(LogicalMedia logMedia, EndOfValidityMode mode, DateTime dateTime, int value)
        {
            try
            {
                switch (mode)
                {
                    case EndOfValidityMode.Fixed:
                        logMedia.Media.ExpiryDate = dateTime;
                        break;
                    case EndOfValidityMode.CurrentPlusYears:
                        logMedia.Media.ExpiryDate = DateTime.Now.AddYears(value);
                        break;
                }
                return TTErrorTypes.NoError;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules.SetupMediaEndOfValidity " + e.Message);
                return TTErrorTypes.Exception;
            }
        }

        static public TTErrorTypes SetupProductEndOfValidity(LogicalMedia logMedia, EndOfValidityMode mode, DateTime dateTime, int value)
        {
            try
            {
                switch (mode)
                {
                    case EndOfValidityMode.Fixed:
                        logMedia.Application.Products.Product(0).EndOfValidity = dateTime;
                        break;
                    case EndOfValidityMode.CurrentPlusYears:
                        logMedia.Application.Products.Product(0).EndOfValidity = DateTime.Now.AddYears(value);
                        break;
                }
                return TTErrorTypes.NoError;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules.SetupMediaEndOfValidity " + e.Message);
                return TTErrorTypes.Exception;
            }
        }

        static public TTErrorTypes SetupTransportApplicationEndOfValidity(LogicalMedia logMedia, EndOfValidityMode mode, DateTime dateTime, int value)
        {
            try
            {
                switch (mode)
                {
                    case EndOfValidityMode.Fixed:
                        logMedia.Application.TransportApplication.ExpiryDate = dateTime;
                        break;
                    case EndOfValidityMode.CurrentPlusYears:
                        logMedia.Application.TransportApplication.ExpiryDate = DateTime.Now.AddYears(value);
                        break;
                }
                return TTErrorTypes.NoError;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules.SetupTransportApplicationEndOfValidity " + e.Message);
                return TTErrorTypes.Exception;
            }
        }

        static private FareProductSpecs _inst;

        static public TTErrorTypes HasMediaTPurseProduct(LogicalMedia logMedia)
        {
            try
            {
                foreach (OneProduct p in logMedia.Application.Products.List)
                {
                    //To be corrected by putting an enum on Product Family
                    if (ProductParameters.GetProductFamily(p.Type) == 60) return TTErrorTypes.NoError;
                }
                return TTErrorTypes.NoProduct;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules.HasMediaTPurseProduct " + e.Message);
                return TTErrorTypes.Exception;
            }
        }

        static public TTErrorTypes IsFareProductOpen(LogicalMedia logMedia)
        {
            try
            {
                // TODO: Anuj: I am not sure what multiple products mean for us. Writing only to improve the function, but not finish it on this aspect
                foreach (OneProduct p in logMedia.Application.Products.List)
                {                    
                    if (_inst.IsOpen(p.Type)) return TTErrorTypes.NoError;
                }
                return TTErrorTypes.NoProduct;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CommonRules.HasMediaTPurseProduct " + e.Message);
                return TTErrorTypes.Exception;
            }
        }



    }
}
