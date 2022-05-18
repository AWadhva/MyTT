using System;
using System.Collections.Generic;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.Rules;
using IFS2.Equipment.Parameters;

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

        static public TimeSpan EF_EOD_GetMaxPaidTime(int entrySite)
        {
            int durationInMinutes = 0;

            Logging.Log(LogLevel.Verbose, "EF_EOD_GetMaxPaidTime called with " + entrySite);
            if (GetVirtualSiteId(entrySite) == GetVirtualSiteId(SharedData.StationNumber))
                return new TimeSpan(0, 0, FareParameters.ShortestReturnTripDuration);

            int FareTier = FareParameters.GetFareTier(entrySite, SharedData.StationNumber);
            durationInMinutes = ((MaxiTravelTime)BasicParameterFile.Instance("MaxiTravelTime")).Duration(FareTier);

            return new TimeSpan(0, durationInMinutes, 0);
        }

        static internal int GetVirtualSiteId(int siteId)
        {
            int result = siteId;
            bool bFound = Config._VirtualSiteId.TryGetValue(siteId, out result);
            return result;
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

        static public int CalculateAdjustmentChargesForExcessTripTime(TTErrorCodeOnMedia rejectCode, int fp, int entrySite, TimeSpan durationSinceEntry)
        {
            TimeSpan maxPaidAreaTimeAllowed = CommonRules.EF_EOD_GetMaxPaidTime(entrySite);
            if (durationSinceEntry < maxPaidAreaTimeAllowed)
                return 0;

            int amtPerHour = EF_EOD_GetAdjustmentCharge(fp, rejectCode);
            int ExcessHours = (int)(Math.Ceiling((durationSinceEntry - maxPaidAreaTimeAllowed).TotalHours));

            return ExcessHours * amtPerHour;
        }

        static public int EF_EOD_GetAdjustmentCharge(int fp, TTErrorCodeOnMedia rejectCode)
        {
            return FareParameters._surcharges[SharedData._fpSpecsRepository.GetSurchargeIdx(fp, _diErrorCodeVsSurcharge[rejectCode])].Price;
        }

        static readonly Dictionary<TTErrorCodeOnMedia, Common.FareProductSpecs.SurchargeTyp> _diErrorCodeVsSurcharge = new Dictionary<TTErrorCodeOnMedia, Common.FareProductSpecs.SurchargeTyp>()
        {
            {TTErrorCodeOnMedia.ExitMismatch, Common.FareProductSpecs.SurchargeTyp.EntryExitMismatch},
            {TTErrorCodeOnMedia.ExitNotDone, Common.FareProductSpecs.SurchargeTyp.EntryExitMismatch},
            {TTErrorCodeOnMedia.NoEntryFound, Common.FareProductSpecs.SurchargeTyp.EntryExitMismatch},
            {TTErrorCodeOnMedia.ExcessTripTime, Common.FareProductSpecs.SurchargeTyp.OverStay},
            {TTErrorCodeOnMedia.AmountTooLow, Common.FareProductSpecs.SurchargeTyp.UnderFare},
        };


        static public TTErrorTypes IsFareProductOpen(LogicalMedia logMedia)
        {
            try
            {                
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

        public static void SetRejectCode(LogicalMedia logMedia, TTErrorCodeOnMedia code)
        {
            logMedia.Application.Validation.RejectCode = (short)code;
        }

        public static void UpdateForCheckIn(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;

            validation.EntryExitBit = Validation.TypeValues.Entry;
            validation.LastTransactionDateTime = DateTime.Now;
            validation.Location = SharedData.StationNumber;
            validation.RejectCode = 0;

            // TODO: see if we need to put PeriodicTicketEntry for family 80
            SalesRules.AddTrasactionHistoryRecord(logMedia, OperationTypeValues.NoValueDeductedInEntry, 0);
        }

        public static void UpdateForCheckOut_NormalScenario(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;
            var purse = logMedia.Purse;

            int productType = logMedia.Application.Products.Product(0).Type;

            int fare = 0;
            if (ProductParameters.GetProductFamily(productType) == 60)
            {
                int notUsed;
                fare = SalePriceCalculation.CalculatePriceSiteBased(productType, validation.LocationRead, SharedData.StationNumber, validation.LastTransactionDateTimeRead, out notUsed);
                purse.TPurse.Balance = purse.TPurse.BalanceRead - fare;
            }

            validation.EntryExitBit = Validation.TypeValues.Exit;
            validation.LastTransactionDateTime = DateTime.Now;
            validation.Location = SharedData.StationNumber;
            validation.RejectCode = 0;

            purse.TPurse.SequenceNumber = purse.TPurse.SequenceNumberRead - 1;

            // TODO: see if we need to put PeriodicTicketExit for family 80
            SalesRules.AddTrasactionHistoryRecord(logMedia, OperationTypeValues.ValueDeductedInExit, fare);
        }

        public static void UpdateForCheckOut_Recovery(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;

            validation.EntryExitBit = Validation.TypeValues.Exit;
            validation.LastTransactionDateTime = DateTime.Now;
            validation.Location = SharedData.StationNumber;
            validation.RejectCode = 0;
        }
    }
}
