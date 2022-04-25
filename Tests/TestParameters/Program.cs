using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Parameters;

namespace TestParameters
{
    class Program
    {
        static void Main(string[] args)
        {
            string content;

            BasicParameterFile.Register(new ParkingParameters());
            BasicParameterFile.Instance("ParkingParameters").Load();
            string s = ((ParkingParameters)BasicParameterFile.Instance("ParkingParameters")).ListLines(Languages.English);

            BasicParameterFile.Register(new AdditionalProductsParameters());
            BasicParameterFile.Instance("AdditionalProducts").Load();
            s = ((AdditionalProductsParameters)BasicParameterFile.Instance("AdditionalProducts")).ListProducts(Languages.English);

            BasicParameterFile.Register(new TopologyBusParameters());
            BasicParameterFile.Instance("TopologyBusParameters").Load();
            s = ((TopologyBusParameters)BasicParameterFile.Instance("TopologyBusParameters")).ListStations(103, Languages.English);

            BasicParameterFile.Register(new FareBusParameters());
            BasicParameterFile.Instance("FareBusParameters").Load();
            int j = ((FareBusParameters)BasicParameterFile.Instance("FareBusParameters")).CalculateSinglePrice(103, 46, 47);

            s= MultiComponentsRequest.ListPricesOnLineAsString(103, 0, 40, Languages.English);
            Console.WriteLine(s);
            s = MultiComponentsRequest.ListPricesOnLineAsString(103, 1, 45, Languages.English);
            Console.WriteLine(s);

            Crypto.VerifyXmlFile("C:\\IFS2\\Data\\CurrentXmlParameters\\Topology.Xml", BasicParameterFile.CCPublicKey());

            CheckPenaltyComponent();

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\Topology.Xml");
            TopologyParameters.LoadVersion(content);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\AgentList.Xml");
            AgentList.LoadVersion(content);

            EOD_OneAgent agent;
            int i=(int)AgentList.VerifyAgent(1, "23", out agent);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\MediaDenyList.Xml");            
            MediaDenyList.LoadVersion(content);
            bool t = MediaDenyList.VerifyMedia(3, 25);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\RangeDenyList.Xml");
            RangeDenyList.LoadVersion(content);
            bool p = RangeDenyList.VerifyRange(6, 876);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\EquipmentDenyList.Xml");
            EquipmentDenyList.LoadVersion(content);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\OverallParameters.Xml");
            OverallParameters.LoadVersion(content);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\TVMEquipmentParameters.Xml");
            TVMEquipmentParameters.LoadVersion(content);

            content = File.ReadAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\FareParameters.Xml");
            FareParameters.LoadVersion(content);



            //int FareTier;
            int faretiers;
            int tokenPrice = (int)SalePriceCalculation.CalculatePriceSiteBased(1, 47, 46, DateTime.Now, out faretiers);
            tokenPrice = (int)SalePriceCalculation.CalculateTokenPriceZoneBased(3);
            /*

            int FareType = ProductParameters.GetFareType(1, 2, 0);
            int DayType = TimeParameters.GetDayType(1, DateTime.Now);
            int Time = TimeParameters.GetIntervalType(1, DateTime.Now);
            int FareTiers = FareParameters.GetFareTier(1, 2);
            int FareGroup = FareParameters.GetFareGroup(FareType, DayType, Time);
            int FareValue = FareParameters.GetFareValue(FareGroup, FareTiers, 1);
             * */

            Console.ReadKey();
        }

        static void CheckPenaltyComponent()
        {
            BasicParameterFile.Register(new PenaltyParameters());
            BasicParameterFile.Instance("PenaltyParameters").Load();
            string s = MultiComponentsRequest.ListPenalties(Languages.English);
            Console.WriteLine(s);
            Console.ReadKey();
        }

    }
}
