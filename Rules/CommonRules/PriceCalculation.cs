using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public static class SalePriceCalculation
    {
        public static int CalculateTokenPriceSiteBased(int pOrigin, int pDestination, out int pFareTier)
        {
            return CalculatePriceSiteBased(1, pOrigin, pDestination, DateTime.Now, out pFareTier);
        }

        public static int CalculatePriceSiteBased(int TicketType, int pOrigin, int pDestination, DateTime startTime, out int pFareTier)
        {            
            int serviveProvider = 2; //DMRC
            int DayType;
            int FareGroup;
            int FareTier;
            int FareValue = 0;

            pFareTier = -1;

            //First step search for the fare type to apply according to the Single ticket.
            //Single journey is product reference 1, Service provider is DMRC 2, railcard is not used as 0
            int FareType = ProductParameters.GetFareType(TicketType, serviveProvider, 0);
            if (FareType <= 0) return -1;
            //Determination of DayType
            //Current day type. There is only one calendar at this moment

            DayType = TimeParameters.GetDayType(1, startTime);
            if (DayType < 0) return -1;
            //Determination of Interval type
            int Interval = TimeParameters.GetIntervalType(1, startTime);
            if (Interval < 0) return -1;
            //Determination of FareGroup
            FareGroup = FareParameters.GetFareGroup(FareType, DayType, Interval);
            if (FareGroup <= 0) return -1;
            //Determination of Fare Tiers in the station to station matrix
            FareTier = FareParameters.GetFareTier(pOrigin, pDestination);
            if (FareTier < 0) return -1;
            //Determination of price
            FareValue = FareParameters.GetFareValue(FareGroup, FareTier, 1);
            pFareTier = FareTier;
            if (FareValue <= 0) return -1;
            return FareValue;
        }

        public static int CalculateTokenPriceZoneBased(int FareTier, DateTime dt)
        {
            int TicketType = 1; //Single Journey Product
            int serviveProvider = 2; //DMRC
            int DayType;
            int FareGroup;
            int FareValue = 0;

            if (FareTier <= 0) return -1;

            //First step search for the fare type to apply according to the Single ticket.
            //Single journey is product reference 1, Service provider is DMRC 2, railcard is not used as 0
            int FareType = ProductParameters.GetFareType(TicketType, serviveProvider, 0);
            if (FareType <= 0) return -1;
            //Determination of DayType
            //Current day type. There is only one calendar at this moment
            DayType = TimeParameters.GetDayType(1, dt);
            if (DayType < 0) return -1;
            //Determination of Interval type
            int Interval = TimeParameters.GetIntervalType(1, dt);
            if (Interval < 0) return -1;
            //Determination of FareGroup
            FareGroup = FareParameters.GetFareGroup(FareType, DayType, Interval);
            if (FareGroup <= 0) return -1;

            //Determination of price
            FareValue = FareParameters.GetFareValue(FareGroup, FareTier, 1);

            if (FareValue <= 0) return -1;
            return FareValue;
        }

        public static int CalculateTokenPriceZoneBased(int FareTier)
        {
            return CalculateTokenPriceZoneBased(FareTier, DateTime.Now);
        }
    }
}
