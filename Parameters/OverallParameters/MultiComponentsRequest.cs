using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;
using IFS2.Equipment.Parameters;


namespace IFS2.Equipment.TicketingRules
{


    public static class MultiComponentsRequest
    {
        static public string ListPricesOnLineAsString(int LineNumber, int Direction, int indexStop, Languages Language)
        {
            try
            {
                //Complete list of stations with prices
                EODFare_Message_PricesOnLine result = ListPricesOnLine(LineNumber, Direction, indexStop, Language);
                if (result != null) return SerializeHelper<EODFare_Message_PricesOnLine>.XMLSerialize(result);
            }
            catch (Exception e)
            {
                Logging.Error("MultiComponentsRequest.ListPricesOnLine " + LineNumber.ToString() + " " + Direction.ToString() + " " + indexStop.ToString());
            }
            return "";
        }

        static public EODFare_Message_PricesOnLine ListPricesOnLine(int LineNumber, int Direction, int indexStop, Languages Language)
        {
            try
            {
                //Complete list of stations with prices
                EODFare_Message_PricesOnLine result = new EODFare_Message_PricesOnLine();
                //First step is to have the list of stops remaining on this station to the terminus of the line
                EODGetListStationsOfOneLine list = ((TopologyBusParameters)BasicParameterFile.Instance("TopologyBusParameters")).ListStations(LineNumber, Direction, indexStop, Language);
                if (list != null)
                {
                    FareBusParameters fare = (FareBusParameters)BasicParameterFile.Instance("FareBusParameters");
                    int nb = list.Stations.Count;
                    if (nb > 1)
                    {
                        for (int i = 1; i < nb; i++)
                        {
                            int fareTiers;
                            int price = fare.CalculateSinglePrice(LineNumber, list.Stations[0].Code, list.Stations[i].Code, out fareTiers);
                            EODFareDetail_Message_PricesOnLine res1 = new EODFareDetail_Message_PricesOnLine();
                            result.Prices.Add(res1);
                            res1.Price = price;
                            res1.Destination = list.Stations[i].Code;
                            res1.StationName = list.Stations[i].Name;
                            res1.FareTiers = fareTiers;
                        }
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                Logging.Error("MultiComponentsRequest.ListPricesOnLine " + LineNumber.ToString() + " " + Direction.ToString() + " " + indexStop.ToString());
            }
            return null;
        }


        static public EODFare_Message_PricesOnLine ListDifferentPricesOnLine(EODFare_Message_PricesOnLine prices)
        {
            try
            {
                EODFare_Message_PricesOnLine result = new EODFare_Message_PricesOnLine();
                if (prices.Prices.Count > 0)
                {
                    foreach (EODFareDetail_Message_PricesOnLine price in prices.Prices)
                    {
                        if (price.Price > 0)
                        {
                            EODFareDetail_Message_PricesOnLine res1 = result.Prices.Find(x => x.Price == price.Price);
                            if (res1 == null)
                            {
                                res1 = new EODFareDetail_Message_PricesOnLine();
                                result.Prices.Add(res1);
                                res1.Price = price.Price;
                            }
                            res1.Destination = price.Destination;
                            res1.StationName = price.StationName;
                            res1.FareTiers = price.FareTiers;
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Logging.Error("MultiComponentsRequest.ListDifferentPricesOnLineAsString ");
            }
            return null;
        }

        static public EODFare_Message_PricesOnLine ListDifferentPricesOnLine(int LineNumber, int Direction, int indexStop, Languages Language)
        {
            try
            {
                //Complete list of stations with prices
                EODFare_Message_PricesOnLine result = new EODFare_Message_PricesOnLine();
                //First step is to have the list of stops remaining on this station to the terminus of the line
                EODGetListStationsOfOneLine list = ((TopologyBusParameters)BasicParameterFile.Instance("TopologyBusParameters")).ListStations(LineNumber, Direction, indexStop, Language);
                if (list != null)
                {
                    FareBusParameters fare = (FareBusParameters)BasicParameterFile.Instance("FareBusParameters");
                    int nb = list.Stations.Count;
                    if (nb > 1)
                    {
                        for (int i = 1; i < nb; i++)
                        {
                            int fareTiers;
                            int price = fare.CalculateSinglePrice(LineNumber, list.Stations[0].Code, list.Stations[i].Code, out fareTiers);
                            if (price >= 0)
                            {
                                EODFareDetail_Message_PricesOnLine res1 = result.Prices.Find(x => x.Price == price);
                                if (res1 == null)
                                {
                                    res1 = new EODFareDetail_Message_PricesOnLine();
                                    result.Prices.Add(res1);
                                    res1.Price = price;
                                }
                                res1.Destination = list.Stations[i].Code;
                                res1.StationName = list.Stations[i].Name;
                                res1.FareTiers = fareTiers;
                            }
                        }
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                Logging.Error("MultiComponentsRequest.ListPricesOnLine " + LineNumber.ToString() + " " + Direction.ToString() + " " + indexStop.ToString());
            }
            return null;
        }

        static public string ListPenalties(Languages language)
        {
            try
            {
                EODGetListPenalties data = new EODGetListPenalties();
                EODContent_Penalty pen = (EODContent_Penalty)BasicParameterFile.Instance("PenaltyParameters").Content;
                foreach (EODContent_Penalty_Detail pendet in pen.penalties.list)
                {
                    EODDetailOnePenalty det = new EODDetailOnePenalty();
                    det.Code = pendet.Code;
                    det.Name = pendet.Name;
                    det.Price = pendet.Price;
                    data.Products.Add(det);
                }
                string s = SerializeHelper<EODGetListPenalties>.XMLSerialize(data);
                Logging.Log(LogLevel.Verbose, "MultiComponentRequest.ListPenalties " + s);
                return s;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "MultiComponentRequest.ListPenalties " + e.Message);
            }
            return "";
        }

        //static public string ListAdditionalProducts(Languages language)
        //{
        //    try
        //    {
        //        EODGetListProductsForSale data = new EODGetListProductsForSale();
        //        EODContent_Parking pen = (EODContent_Parking)BasicParameterFile.Instance("ParkingParameters").Content;
        //        foreach (EODContent_Parking_ProductDetail pendet in pen.products.list)
        //        {
        //            EODDetailOneProductForSale det = new EODDetailOneProductForSale();
        //            det.Code = pendet.Code;
        //            det.Name = pendet.Name;
        //            det.Price = pendet.Price;
        //            data.Products.Add(det);
        //        }
        //        EODContent_AdditionalProducts prd = (EODContent_AdditionalProducts)BasicParameterFile.Instance("AdditionalProducts").Content;
        //        foreach (EODContent_AdditionalProducts_ProductDetail prod in prd.products.list)
        //        {
        //            EODDetailOneProductForSale det = new EODDetailOneProductForSale();
        //            det.Code = prod.Code;
        //            det.Name = prod.Name;
        //            det.Price = prod.Price;
        //            data.Products.Add(det);

        //        }
        //        string s = SerializeHelper<EODGetListProductsForSale>.XMLSerialize(data);
        //        return s;
        //    }
        //    catch (Exception e)
        //    {
        //        Logging.Log(LogLevel.Error, "MultiComponentRequest.ListPenalties " + e.Message);
        //    }
        //    return "";
        //}


    }
}
