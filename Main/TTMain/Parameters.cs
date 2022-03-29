using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Parameters;
using System.IO;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules
    {
        public static OneEvent _fpStatus = null;
        public static OneEvent _fpError = null;
        public static OneEvent _fpMetaStatus = null;
        public static OneEvent _prgStatus = null;
        public static OneEvent _prgError = null;
        public static OneEvent _prgMetaStatus = null;

        public static OneEvent _parametersMissing = null;
        public static OneEvent _parametersError = null;
        public static OneEvent _parametersActivationError = null;
        public static OneEvent _parametersMetaStatus = null;
        public static OneEvent _globalMetaStatus = null;


        public bool TreatParametersMessageReceived(EventMessage eventMessage)
        {
            if (eventMessage.EventID == null || eventMessage.EventID == string.Empty)
                return false;
            switch (eventMessage.EventID.ToUpper())
            {
                case "GETSINGLETICKETPRICE":
                    try
                    {
                        EODAskForPriceCalculation eod = SerializeHelper<EODAskForPriceCalculation>.XMLDeserialize(eventMessage.Attribute);
                        int price=-1;
                        switch (eod.ShiftMode)
                        {
                            case ShiftOperationalType.Metro:
                                break;
                            case ShiftOperationalType.Bus:
                                price = ((FareBusParameters)BasicParameterFile.Instance("FareBusParameters")).CalculateSinglePrice(eod.Line, eod.Origin, eod.Destination);
                                break;
                        }
                        if (price < 0) Communication.SendMessage(ThreadName, "Answer", "GetSingleTicketPriceAnswer", "1", "");
                        else
                        {
                            EODAnswerForPriceCalculation ans = new EODAnswerForPriceCalculation(price);
                            Communication.SendMessage(ThreadName, "Answer", "GetSingleTicketPriceAnswer", "0", SerializeHelper<EODAnswerForPriceCalculation>.XMLSerialize(ans));
                        }
                        break;
                    }
                    catch (Exception e21)
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatMessageReceived.GetSingleTicketPrice " + e21.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetSingleTicketPriceAnswer", "1", "");
                    break;
                case "GETLISTPRODUCTS":
                    try
                    {
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[0]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = TicketsSaleParameters.GetListProductsForSale(l);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListProductsAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListProducts " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListProductsAnswer", "1", "");
                    break;
                case "GETLISTPENALTIES":
                    try
                    {
                        Logging.Log(LogLevel.Verbose, "Parameters : Has received message for GetListPenalties ");
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[0]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = MultiComponentsRequest.ListPenalties(l);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListPenaltiesAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListPenalties " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListPenaltiesAnswer", "1", "");
                    break;
                case "GETPARKINGDURATIONPRICE":
                    //parameter 1 : Vehicle type
                    //parameter 2 : Duration
                    try
                    {
                        int price = ((ParkingParameters)BasicParameterFile.Instance("ParkingParameters")).PricePerDuration(0, 0, Convert.ToInt32(eventMessage._par[0]), Convert.ToInt32(eventMessage._par[1]));
                        if (price < 0) Communication.SendMessage(ThreadName, "Answer", "GetParkingDurationPriceAnswer", "1", "");
                        else Communication.SendMessage(ThreadName, "Answer", "GetParkingDurationPriceAnswer", "0", Convert.ToString(price));
                        break;
                    }
                    catch (Exception e20)
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatMessageReceived.GetParkingDurationPrice " + e20.Message);
                    }
                    Communication.SendMessage(ThreadName,"Answer","GetParkingDurationPriceAnswer","1","");
                    break;
                case "GETLISTADDITIONALPRODUCTS":
                    try
                    {
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[0]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = ((AdditionalProductsParameters)BasicParameterFile.Instance("AdditionalProducts")).ListProducts(l);
                        Logging.Debug("Parameters.GetListAdditionalProductsAnswer " + s);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListAdditionalProductsAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListAdditionalProductsAnswer " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListAdditionalProductsAnswer", "1", "");
                    break;
                case "GETLISTPARKINGPRODUCTS":
                    try
                    {
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[0]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = ((ParkingParameters)BasicParameterFile.Instance("ParkingParameters")).ListProducts(l);
                        Logging.Debug("Parameters.GetListParkingProducts " + s);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListParkingProductsAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListParkingProductsAnswer " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListParkingProductsAnswer", "1", "");
                    break;
                case "GETLISTPARKINGLINES":
                    try
                    {
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[0]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = ((ParkingParameters)BasicParameterFile.Instance("ParkingParameters")).ListLines(l);
                        Logging.Debug("Parameters.GetListParkingLines " + s);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListParkingLinesAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListParkingLinesAnswer " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListParkingLinesAnswer", "1", "");
                    break;
                case "GETLISTPARKINGSTATIONS":
                    try
                    {
                        int lineNumber = Convert.ToInt32(eventMessage.Attribute);
                        int direction = Convert.ToInt32(eventMessage.Message);
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[2]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = ((ParkingParameters)BasicParameterFile.Instance("ParkingParameters")).ListStations(lineNumber,l);
                        Logging.Debug("Parameters.GetListParkingStations " + s);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListParkingStationsAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListParkingStations " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListParkingStationsAnswer", "1", "");
                    break;

                case "GETLISTSTATIONS":
                    try
                    {
                        int lineNumber = Convert.ToInt32(eventMessage.Attribute);
                        int direction = Convert.ToInt32(eventMessage.Message);
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[2]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = TopologyParameters.GetListStations(lineNumber,direction,l);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListStationsAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListStations " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListStationsAnswer", "1", "");
                    break;
                case "GETLISTLINES":
                    try
                    {
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage.Attribute));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = TopologyParameters.GetListLines(l);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListLinesAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListLines " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListLinesAnswer", "1","");
                    break;
                case "GETLISTBUSLINES":
                    try
                    {
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[0]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = ((TopologyBusParameters)BasicParameterFile.Instance("TopologyBusParameters")).ListLines(l);
                        Logging.Debug("Parameters.GetListBusLines " + s);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListBusLinesAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListBusLinesAnswer " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListBusLinesAnswer", "1", "");
                    break;
                case "GETLISTBUSSTATIONS":
                    try
                    {
                        int lineNumber = Convert.ToInt32(eventMessage.Attribute);
                        int direction = Convert.ToInt32(eventMessage.Message);
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[2]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        string s = ((TopologyBusParameters)BasicParameterFile.Instance("TopologyBusParameters")).ListStations(lineNumber, l);
                        if (s != "")
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetListBusStationsAnswer", "0", s);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListBuStations " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListBusStationsAnswer", "1", "");
                    break;
                case "GETLISTPRICESONRESTOFLINE":
                    try
                    {
                        int lineNumber = Convert.ToInt32(eventMessage.Attribute);
                        int direction = Convert.ToInt32(eventMessage.Message);
                        int indexStation = Convert.ToInt32(eventMessage._par[2]);
                        Languages l;
                        try
                        {
                            l = (Languages)(Convert.ToInt32(eventMessage._par[3]));
                        }
                        catch
                        {
                            l = Languages.English;
                        }
                        EODFare_Message_PricesOnLine result = MultiComponentsRequest.ListPricesOnLine(lineNumber, direction, indexStation, l);
                        EODFare_Message_PricesOnLine result1 = MultiComponentsRequest.ListDifferentPricesOnLine(result);
                        if ((result != null) && (result1 != null))
                        {
                            string s = SerializeHelper<EODFare_Message_PricesOnLine>.XMLSerialize(result);
                            string s1 = SerializeHelper<EODFare_Message_PricesOnLine>.XMLSerialize(result1);
                            Communication.SendMessage(ThreadName, "Answer", "GetListPricesOnRestOfLineAnswer", "0", s,s1);
                            break;
                        }
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatParametersMessageReceived.GetListPricesOnRestOfLine " + eventMessage.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetListPricesOnRestOfLineAnswer", "1", "");
                    break;
                case "GETTRAVELDURATIONFROMFARETIERS":
                    //parameter 1 : Fare tiers
                    //return parameter 1 : 0 is OK, 1 is error, parameter 2 is value of duration in case of OK
                    try
                    {
                        int duration = ((MaxiTravelTime)BasicParameterFile.Instance("TravelTimeParameters")).Duration( Convert.ToInt32(eventMessage._par[0]));
                        Communication.SendMessage(ThreadName, "Answer", "GetTravelDurationFromFareTiersAnswer", "0", Convert.ToString(duration));
                        break;
                    }
                    catch (Exception e21)
                    {
                        Logging.Log(LogLevel.Error, "Parameters.TreatMessageReceived.GetTravelDurationFromFareTiers " + e21.Message);
                    }
                    Communication.SendMessage(ThreadName, "Answer", "GetParkingDurationPriceAnswer", "1", "0");
                    break;
                case "GETEODMETASTATUS":
                    Communication.SendMessage(ThreadName, "Status", "EODMetaStatus", Convert.ToString(_parametersMetaStatus.Value), EODFileStatusList.EODMetaStatus());
                    break;
                case "ACTIVATENEWPARAMETERFILE":
                    //Message received when there is a new set of parameters to take in count.
                    string content;                    

                    EODFileStatus edfs = new EODFileStatus();
    
                    try
                    {
                        int res = 1;
                        switch (eventMessage.Attribute.ToUpper())
                        {
                            case "LOCALPENALTYPARAMETERS":
                                if (!BasicParameterFile.Instance("PenaltyParameters").ActivateComponent(eventMessage.Message)) res=0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALPARKINGPARAMETERS":
                                if (!BasicParameterFile.Instance("ParkingParameters").ActivateComponent(eventMessage.Message)) res=0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALADDITIONALPRODUCTS":
                                if (!BasicParameterFile.Instance("AdditionalProducts").ActivateComponent(eventMessage.Message)) res=0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALBUSTOPOLOGY":
                                if (!BasicParameterFile.Instance("TopologyBusParameters").ActivateComponent(eventMessage.Message)) res = 0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALBUSFARE":
                                if (!BasicParameterFile.Instance("FareBusParameters").ActivateComponent(eventMessage.Message)) res = 0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALHIGHSECURITYLIST":
                                if (!BasicParameterFile.Instance("HighSecurityList").ActivateComponent(eventMessage.Message)) res = 0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALMAXITRAVELTIME":
                                if (!BasicParameterFile.Instance("MaxiTravelTime").ActivateComponent(eventMessage.Message)) res = 0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "LOCALAGENTLIST":
                                if (!BasicParameterFile.Instance("LocalAgentList").ActivateComponent(eventMessage.Message)) res = 0; //Saving new component in xmlCurrentParameters directory
                                break;
                            case "OVERALLPARAMETERS":
                                try
                                {
                                    if (Config._bEODReceivedInXml)
                                    {
                                        content = Disk.ReadAllTextFile(eventMessage.Message);
                                    }
                                    else
                                    {
                                        content = DelhiSpecific.TreatSystemParameters(eventMessage.Message);
                                    }
                                    OverallParameters.Save(content);
                                    OverallParameters.LoadVersion(content);
                                    OverallParameters._overallActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("OverallParameters", 1);
                                    Communication.SendMessage(ThreadName, "Answer", "EquipmentParams", eventMessage.Attribute, content);
                                }
                                catch
                                {
                                    OverallParameters._overallActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("OverallParameters", 0);
                                    res = 0;
                                }
                                if (!Config._bEODReceivedInXml)
                                {
                                    try
                                    {
                                        content = DelhiSpecific.TreatTopologyParameters(eventMessage.Message);
                                        TopologyParameters.Save(content);
                                        TopologyParameters.LoadVersion(content);
                                        if (SharedData.EquipmentType == EquipmentFamily.TOM)
                                            Communication.SendMessage(ThreadName, "", "ParametersUpdated", "Topology");                                        
                                        else
                                            Communication.SendMessage(ThreadName, "", "TopologyParametersUpdated");

                                        TopologyParameters._topologyActivation.SetAlarm(false);
                                        EODFileStatusList.UpdateStatus("TopologyParameters", 1);
                                    }
                                    catch
                                    {
                                        TopologyParameters._topologyActivation.SetAlarm(true);
                                        EODFileStatusList.UpdateStatus("TopologyParameters", 0);
                                        res = 0;
                                    }
                                }
                                break;
                                //Topology can be received only separated in XML
                            case "TOPOLOGY":
                                try
                                {
                                    content = Disk.ReadAllTextFile(eventMessage.Message);
                                    TopologyParameters.Save(content);
                                    TopologyParameters.LoadVersion(content);

                                    if (SharedData.EquipmentType == EquipmentFamily.TOM)
                                        Communication.SendMessage(ThreadName, "", "ParametersUpdated", "Topology");
                                    else
                                        Communication.SendMessage(ThreadName, "", "TopologyParametersUpdated");

                                    TopologyParameters._topologyActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("TopologyParameters", 1);
                                }
                                catch
                                {
                                    TopologyParameters._topologyActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("TopologyParameters", 0);
                                    res = 0;
                                }
                                break;
                            case "TVMEQUIPMENTPARAMETERS":
                                    try
                                    {
                                        if (Config._bEODReceivedInXml)
                                        {
                                            content = Disk.ReadAllTextFile(eventMessage.Message);
                                        }
                                        else
                                        {
                                            content = DelhiSpecific.TreatTVMEquipmentParameters(eventMessage.Message);
                                        }
                                        TVMEquipmentParameters.Save(content);
                                        TVMEquipmentParameters.LoadVersion(content);
                                        TVMEquipmentParameters._equipmentParametersActivation.SetAlarm(false);
                                        EODFileStatusList.UpdateStatus("EquipmentParameters", 1);
                                        Communication.SendMessage(ThreadName, "Answer", "EquipmentParams", eventMessage.Attribute, content);
                                    }
                                    catch
                                    {
                                        TVMEquipmentParameters._equipmentParametersActivation.SetAlarm(true);
                                        EODFileStatusList.UpdateStatus("EquipmentParameters", 0);
                                        res = 0;
                                    }
                                break;
                            case "MEDIADENYLIST":
                                try
                                {
                                    if (Config._bEODReceivedInXml)
                                    {
                                        content = Disk.ReadAllTextFile(eventMessage.Message);
                                    }
                                    else
                                    {
                                        content = DelhiSpecific.TreatMediaDenyList(eventMessage.Message);
                                    }
                                    MediaDenyList.Save(content);
                                    MediaDenyList.LoadVersion(content);
                                    MediaDenyList._mediaDenyListActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("MediaDenyList", 1);
                                }
                                catch
                                {
                                    MediaDenyList._mediaDenyListActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("MediaDenyList", 0);
                                    res = 0;
                                }
                                if (!Config._bEODReceivedInXml)
                                {
                                    try
                                    {
                                        content = DelhiSpecific.TreatRangeDenyList(eventMessage.Message);
                                        RangeDenyList.Save(content);
                                        RangeDenyList.LoadVersion(content);
                                        RangeDenyList._rangeDenyListActivation.SetAlarm(false);
                                        EODFileStatusList.UpdateStatus("RangeDenyList", 1);
                                    }
                                    catch
                                    {
                                        RangeDenyList._rangeDenyListActivation.SetAlarm(true);
                                        EODFileStatusList.UpdateStatus("RangeDenyList", 0);
                                        res = 0;
                                    }
                                }
                                break;
                            //This separate is only in XML
                            case "RANGEDENYLIST":

                                try
                                {
                                    content = Disk.ReadAllTextFile(eventMessage.Message);
                                    RangeDenyList.Save(content);
                                    RangeDenyList.LoadVersion(content);
                                    RangeDenyList._rangeDenyListActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("RangeDenyList", 1);
                                }
                                catch
                                {
                                    RangeDenyList._rangeDenyListActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("RangeDenyList", 0);
                                    res = 0;
                                }
                                break;
                            case "EQUIPMENTDENYLIST":        
                                try
                                {
                                    if (Config._bEODReceivedInXml)
                                    {
                                        content = Disk.ReadAllTextFile(eventMessage.Message);
                                    }
                                    else
                                    {
                                        content = DelhiSpecific.TreatEquipmentDenyList(eventMessage.Message);
                                    }

                                    EquipmentDenyList.Save(content);
                                    EquipmentDenyList.LoadVersion(content);
                                    EquipmentDenyList._equipmentDenyListActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("EquipmentDenyList", 1);
                                }
                                catch
                                {
                                    EquipmentDenyList._equipmentDenyListActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("EquipmentDenyList", 0);
                                    res = 0;
                                }
                                break;
                            case "AGENTLIST":
                                try
                                {
                                    if (Config._bEODReceivedInXml)
                                    {
                                        content = Disk.ReadAllTextFile(eventMessage.Message);
                                    }
                                    else
                                    {
                                        content = DelhiSpecific.TreatAgentList(eventMessage.Message);
                                    }
                                    AgentList.Save(content);
                                    AgentList.LoadVersion(content);
                                    AgentList._agentListActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("AgentList",1);
                                }
                                catch
                                {
                                    AgentList._agentListActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("AgentList",0);
                                    res = 0;
                                }
                                break;
                            case "FAREPARAMETERS":
                                try
                                {
                                    if (Config._bEODReceivedInXml)
                                    {
                                        content = Disk.ReadAllTextFile(eventMessage.Message);
                                    }
                                    else
                                    {
                                        content = DelhiSpecific.TreatFareParameters(eventMessage.Message);
                                    }
                                    Communication.SendMessage(ThreadName, "", "ParametersUpdated", "FAREPARAMETERS");

                                    FareParameters.Save(content);                                    
                                    FareParameters.LoadVersion(content);
                                    FareParameters._faresActivation.SetAlarm(false);
                                    EODFileStatusList.UpdateStatus("FareParameters", 1);
                                }
                                catch
                                {
                                    FareParameters._faresActivation.SetAlarm(true);
                                    EODFileStatusList.UpdateStatus("FareParameters", 0);
                                    res = 0;
                                }
                                break;
                            case "TICKETSALEPARAMETERS":
                                    try
                                    {
                                        content = Disk.ReadAllTextFile(eventMessage.Message);
                                        TicketsSaleParameters.Save(content);                                        
                                        TicketsSaleParameters.LoadVersion(content);
                                        TicketsSaleParameters._ticketSaleParametersActivation.SetAlarm(false);
                                        EODFileStatusList.UpdateStatus("TicketSaleParameters", 1);
                                        Communication.SendMessage(ThreadName, "", "ParametersUpdated", "TICKETSALEPARAMETERS");
                                    }
                                    catch
                                    {
                                        TicketsSaleParameters._ticketSaleParametersActivation.SetAlarm(true);
                                        EODFileStatusList.UpdateStatus("TicketSaleParameters", 0);
                                        res = 0;
                                    }
                                break;
                        }
                        //Update different levels
                        _parametersMissing.UpdateMetaAlarm();
                        _parametersError.UpdateMetaAlarm();
                        _parametersActivationError.UpdateMetaAlarm();
                        _parametersMetaStatus.UpdateMetaStatus();
                        if (_parametersMetaStatus.HasChangedSinceLastSave)
                            Communication.SendMessage(ThreadName, "Status", "EODMetaStatus", Convert.ToString(_parametersMetaStatus.Value), EODFileStatusList.EODMetaStatus());
                        _globalMetaStatus.UpdateMetaStatus();
                        IFSEventsList.SaveIfHasChangedSinceLastSave("TTComponent");

                        Communication.SendMessage(ThreadName, "Answer", "ActivateNewParameterFileAnswer", eventMessage.Attribute, Convert.ToString(res));
                    }
                    catch (Exception ex1)
                    {
                        Logging.Log(LogLevel.Error, "TTMain_TreatMessageReceived " + ex1.Message);
                        Communication.SendMessage(ThreadName, "Answer", "ActivateNewParameterFileAnswer", eventMessage.Attribute, "0");
                    }
                    return (true);
            }
            return false;
        }

        private void InitParameterRelated()
        {
            RegisterMessages_ForParameters();
            try
            {
                Directory.CreateDirectory(Disk.BaseDataDirectory + @"\CurrentXmlParameters\");
            }
            catch { }

            //We need to create the static from the parameters
            FareParameters.Start();
            TopologyParameters.Start();
            MediaDenyList.Start();
            EquipmentDenyList.Start();
            RangeDenyList.Start();
            AgentList.Start();
            OverallParameters.Start();
            TVMEquipmentParameters.Start();
            TicketsSaleParameters.Start();

            BasicParameterFile.Register(new PenaltyParameters());
            BasicParameterFile.Register(new ParkingParameters());
            BasicParameterFile.Register(new AdditionalProductsParameters());
            BasicParameterFile.Register(new TopologyBusParameters());
            BasicParameterFile.Register(new FareBusParameters());
            BasicParameterFile.Register(new HighSecurityList());
            BasicParameterFile.Register(new MaxiTravelTime());
            BasicParameterFile.Register(new LocalAgentList());

            _fpStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 4, "FpStatus", "OFPMIS", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
            _fpError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 5, "FpError", "OFPDEC", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
            _fpMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 6, "FpMetaStatus", "FPMMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
            _fpMetaStatus.SetMetaStatusLinkage("", "FpStatus;FpError");

            _prgStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 7, "PrgStatus", "PROMIS", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
            _prgError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 8, "PrgError", "PRODEC", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
            _prgMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 9, "PrgMetaStatus", "PROMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
            _prgMetaStatus.SetMetaStatusLinkage("", "PrgStatus;PrgError");

            _parametersMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 34, "ParametersMissing", "PARMIS", AlarmStatus.Alarm, OneEvent.OneEventType.MetaAlarm);
// TODO: remove these unnecessary #if's
#if ! _HHD_
            _parametersMissing.SetMetaAlarmLinkage(Configuration.ReadStringParameter("SetParametersMissing", "AgentListMissing;MediaDenyListMissing;RangeDenyListMissing;EquipmentDenyListMissing;TopologyMissing;FaresMissing;OverallMissing;EquipmentParametersMissing;TicketSaleParametersMissing"));
#else
            _parametersMissing.SetMetaAlarmLinkage("AgentListMissing;MediaDenyListMissing;RangeDenyListMissing;EquipmentDenyListMissing;TopologyMissing;FaresMissing;OverallMissing;EquipmentParametersMissing");
#endif

            _parametersError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 35, "ParametersError", "PARDEC", AlarmStatus.Alarm, OneEvent.OneEventType.MetaAlarm);
#if ! _HHD_
            _parametersError.SetMetaAlarmLinkage(Configuration.ReadStringParameter("SetParametersError", "AgentListError;MediaDenyListError;RangeDenyListError;EquipmentDenyListError;TopologyError;FaresError;OverallError;EquipmentParametersError;TicketSaleParametersError"));
#else
            _parametersError.SetMetaAlarmLinkage("AgentListError;MediaDenyListError;RangeDenyListError;EquipmentDenyListError;TopologyError;FaresError;OverallError;EquipmentParametersError");
#endif

            _parametersActivationError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 37, "ParametersActivationError", "EODFAI", AlarmStatus.Alarm, OneEvent.OneEventType.MetaAlarm);
#if ! _HHD_
            _parametersActivationError.SetMetaAlarmLinkage("AgentListActivationError;MediaDenyListActivationError;RangeDenyListActivationError;EquipmentDenyListActivationError;TopologyActivationError;FaresActivationError;OverallActivationError;EquipmentParametersActivationError");            
#else
            _parametersActivationError.SetMetaAlarmLinkage(Configuration.ReadStringParameter("SetParametersActivationError", "AgentListActivationError;MediaDenyListActivationError;RangeDenyListActivationError;EquipmentDenyListActivationError;TopologyActivationError;FaresActivationError;OverallActivationError;EquipmentParametersActivationError;TicketSaleParametersActivationError"));
#endif

            _parametersMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 36, "ParametersMetaStatus", "PARMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
            _parametersMetaStatus.SetMetaStatusLinkage("", Configuration.ReadStringParameter("SetParametersMetaStatus", "ParametersMissing;ParametersError;ParametersActivationError"));


            _globalMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 38, "EODMetaStatus", "METEOD", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
            _globalMetaStatus.SetMetaStatusLinkage("", Configuration.ReadStringParameter("SetGlobalEODMetaStatus", "ParametersMissing;ParametersError;ParametersActivationError;TicketKeysMissing;TicketKeysError;FpStatus;FpError;PrgStatus;PrgError"));

            if (!Config._bTreatTicketSaleParameterInEOD)
            {
                TicketsSaleParameters._ticketSaleParametersActivation.SetAlarm(false);
                TicketsSaleParameters._ticketSaleParametersError.SetAlarm(false);
                TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(false);
            }
            if (!Config._bTreatTVMEquipmentParametersInEOD)
            {
                TVMEquipmentParameters._equipmentParametersActivation.SetAlarm(false);
                TVMEquipmentParameters._equipmentParametersError.SetAlarm(false);
                TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(false);
            }
            InitializeEODParams();
        }

        private void RegisterMessages_ForParameters()
        {
            string MMIChannel = "MMIChannel";
            string CoreChannel = "CoreChannel";

            Communication.AddEventsToReceive(ThreadName, "GetListParkingLines;GetListParkingStations;GetListBusLines;GetListBusStations;GetSingleTicketPrice;GetTravelDurationFromFareTiers", this);
            Communication.AddEventToReceive(ThreadName, "GetListLines;GetListStations;GetListProducts;GetListPenalties;GetListAdditionalProducts;GetParkingDurationPrice;GetListParkingProducts", this);
            Communication.AddEventsToReceive(ThreadName, "GetListPricesOnRestOfLine;GetEODMetaStatus", this);
            Communication.AddEventToReceive(ThreadName, "ActivateNewParameterFile", this);

            Communication.AddEventsToExternal("GetListLinesAnswer;GetListStationsAnswer;GetListProductsAnswer;GetListPenaltiesAnswer;GetListAdditionalProductsAnswer;GetListParkingProductsAnswer", MMIChannel);
            Communication.AddEventsToExternal("GetParkingDurationPriceAnswer;GetListParkingLinesAnswer;GetListParkingStationsAnswer;GetListBusLinesAnswer;GetListBusStationsAnswer", MMIChannel);
            Communication.AddEventsToExternal("GetSingleTicketPriceAnswer;GetTravelDurationFromFareTiersAnswer;GetListPricesOnRestOfLineAnswer", MMIChannel);

            Communication.AddEventToExternal("ActivateNewParameterFileAnswer", CoreChannel);

            Communication.AddEventsToExternal("EODMetaStatus;EquipmentParams", MMIChannel);
            Communication.AddEventsToExternal("TopologyParametersUpdated;ParametersUpdated", MMIChannel);
        }
#if _HHD_
        private void InitializeEODParams()
        {
            try
            {
                try
                {
                    AgentList.Initialise();
                    AgentList._agentListMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("AgentList", 1);
                }
                catch
                {
                    AgentList._agentListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("AgentList", 0);
                }

                try
                {
                    MediaDenyList.Initialise();
                    MediaDenyList._mediaDenyListMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("MediaDenyList", 1);
                }
                catch
                {
                    MediaDenyList._mediaDenyListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("MediaDenyList", 0);
                }

                try
                {
                    RangeDenyList.Initialise();
                    RangeDenyList._rangeDenyListMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("RangeDenyList", 1);
                }
                catch
                {
                    RangeDenyList._rangeDenyListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("RangeDenyList", 0);
                }

                try
                {
                    EquipmentDenyList.Initialise();
                    EquipmentDenyList._equipmentDenyListMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("EquipmentDenyList", 1);
                }
                catch
                {
                    EquipmentDenyList._equipmentDenyListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("EquipmentDenyList", 0);
                }

                try
                {
                    TopologyParameters.Initialise();
                    TopologyParameters._topologyMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("TopologyParameters", 1);
                }
                catch
                {
                    TopologyParameters._topologyMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("TopologyParameters", 0);
                }

                try
                {
                    OverallParameters.Initialise();
                    OverallParameters._overallMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("OverallParameters", 1);
                }
                catch
                {
                    OverallParameters._overallMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("OverallParameters", 0);
                }



                //try
                //{
                //    TVMEquipmentParameters.Initialise();
                //    TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(false);
                //    EODFileStatusList.UpdateStatus("EquipmentParameters", 1);
                //}
                //catch
                //{
                //    TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(true);
                //    EODFileStatusList.UpdateStatus("EquipmentParameters", 0);
                //}
                //  Debug.Assert(false);
                TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(false);

                if (Config._bTreatTicketSaleParameterInEOD)
                {
                    Logging.Trace("TtMain.InitialiseEOD. Ticket Sale Parameters in EOD");
                    try
                    {
                        Logging.Trace("TtMain.InitialiseEOD.before Ticket Sale Parameters");
                        FareProductSpecs.Load(false);
                        Logging.Trace("TtMain.InitialiseEOD.after Ticket Sale Parameters");
                        SharedData._fpSpecsRepository = FareProductSpecs.GetInstance();
                        TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(false);
                        EODFileStatusList.UpdateStatus("TicketSaleParamters", 1);
                    }
                    catch (Exception e6)
                    {
                        Logging.Log(LogLevel.Error, "TTMain.InitialiseEOD.TicketSaleParameters Exception " + e6.Message);
                        TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(true);
                        EODFileStatusList.UpdateStatus("TicketSaleParamters", 0);
                    }
                    Logging.Trace("TtMain.InitialiseEOD  Ticket Sale Parameters terminated");
                }
                else
                {
                    FareProductSpecs.Load(true);
                    SharedData._fpSpecsRepository = FareProductSpecs.GetInstance();
                    TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("TicketSaleParamters", 1);
                }

                try
                {
                    FareParameters.Initialise();
                    FareParameters._faresMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("FareParameters", 1);
                }
                catch
                {
                    FareParameters._faresMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("FareParameters", 0);
                }

                //Initialise additional data
                BasicParameterFile.Instance("PenaltyParameters").Load();
                BasicParameterFile.Instance("ParkingParameters").Load();
                BasicParameterFile.Instance("AdditionalProducts").Load();
                BasicParameterFile.Instance("TopologyBusParameters").Load();
                BasicParameterFile.Instance("FareBusParameters").Load();
                BasicParameterFile.Instance("HighSecurityList").Load();
                BasicParameterFile.Instance("MaxiTravelTime").Load();
                BasicParameterFile.Instance("LocalAgentList").Load();

                //Update different levels
                _parametersMissing.UpdateMetaAlarm();
                _parametersError.UpdateMetaAlarm();
                _parametersActivationError.UpdateMetaAlarm();
                _parametersMetaStatus.UpdateMetaStatus();
                _globalMetaStatus.UpdateMetaStatus();
                Communication.SendMessage(ThreadName, "Status", "EODMetaStatus", Convert.ToString(_parametersMetaStatus.Value), EODFileStatusList.EODMetaStatus());


                IFSEventsList.SaveIfHasChangedSinceLastSave("TTComponent");
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Critical, "TTMain Cannot Load Parameters File " + e.Message);
            }
        }
#else
        private void InitializeEODParams()
        {
            try
            {
                try
                {
                    AgentList.Initialise();
                    AgentList._agentListMissing.SetAlarm(false);
                    AgentList._agentListActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("AgentList", 1);
                }
                catch
                {
                    AgentList._agentListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("AgentList", 0);
                }

                try
                {
                    MediaDenyList.Initialise();
                    MediaDenyList._mediaDenyListMissing.SetAlarm(false);
                    MediaDenyList._mediaDenyListActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("MediaDenyList", 1);
                }
                catch
                {
                    MediaDenyList._mediaDenyListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("MediaDenyList", 0);
                }

                try
                {
                    RangeDenyList.Initialise();
                    RangeDenyList._rangeDenyListMissing.SetAlarm(false);
                    RangeDenyList._rangeDenyListActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("RangeDenyList", 1);
                }
                catch
                {
                    RangeDenyList._rangeDenyListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("RangeDenyList", 0);
                }

                try
                {
                    EquipmentDenyList.Initialise();
                    EquipmentDenyList._equipmentDenyListMissing.SetAlarm(false);
                    EquipmentDenyList._equipmentDenyListActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("EquipmentDenyList", 1);
                }
                catch
                {
                    EquipmentDenyList._equipmentDenyListMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("EquipmentDenyList", 0);
                }

                try
                {
                    TopologyParameters.Initialise();
                    TopologyParameters._topologyMissing.SetAlarm(false);
                    TopologyParameters._topologyActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("TopologyParameters", 1);
                }
                catch
                {
                    TopologyParameters._topologyMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("TopologyParameters", 0);
                }

                try
                {
                    OverallParameters.Initialise();
                    OverallParameters._overallMissing.SetAlarm(false);
                    OverallParameters._overallActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("OverallParameters", 1);
                }
                catch
                {
                    OverallParameters._overallMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("OverallParameters", 0);
                }
                //Initialise additional data

                BasicParameterFile.Instance("PenaltyParameters").Load();
                BasicParameterFile.Instance("ParkingParameters").Load();
                BasicParameterFile.Instance("AdditionalProducts").Load();
                BasicParameterFile.Instance("TopologyBusParameters").Load();
                BasicParameterFile.Instance("FareBusParameters").Load();
                BasicParameterFile.Instance("HighSecurityList").Load();
                BasicParameterFile.Instance("MaxiTravelTime").Load();
                BasicParameterFile.Instance("LocalAgentList").Load();

                if (Config._bTreatTVMEquipmentParametersInEOD)
                {
                    try
                    {
                        TVMEquipmentParameters.Initialise();
                        TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(false);
                        TVMEquipmentParameters._equipmentParametersActivation.SetAlarm(false);
                        EODFileStatusList.UpdateStatus("EquipmentParameters", 1);
                    }
                    catch
                    {
                        TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(true);
                        EODFileStatusList.UpdateStatus("EquipmentParameters", 0);
                    }
                }
                else
                {
                    TVMEquipmentParameters._equipmentParametersMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("EquipmentParameters", 1);
                }
                if (Config._bTreatTicketSaleParameterInEOD)
                {
                    Logging.Trace("TtMain.InitialiseEOD. Ticket Sale Parameters in EOD");
                    try
                    {
                        Logging.Trace("TtMain.InitialiseEOD.before Ticket Sale Parameters");
                        FareProductSpecs.Load(false);
                        Logging.Trace("TtMain.InitialiseEOD.after Ticket Sale Parameters");
                        SharedData._fpSpecsRepository = FareProductSpecs.GetInstance();
                        TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(false);
                        TicketsSaleParameters._ticketSaleParametersActivation.SetAlarm(false);
                        EODFileStatusList.UpdateStatus("TicketSaleParamters", 1);
                    }
                    catch (Exception e6)
                    {
                        Logging.Log(LogLevel.Error, "TTMain.InitialiseEOD.TicketSaleParameters Exception " + e6.Message);
                        TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(true);
                        EODFileStatusList.UpdateStatus("TicketSaleParamters", 0);
                    }
                    Logging.Trace("TtMain.InitialiseEOD  Ticket Sale Parameters terminated");
                }
                else
                {
                    FareProductSpecs.Load(true);
                    SharedData._fpSpecsRepository = FareProductSpecs.GetInstance();
                    TicketsSaleParameters._ticketSaleParametersMissing.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("TicketSaleParamters", 1);
                }

                try
                {
                    FareParameters.Initialise();
                    FareParameters._faresMissing.SetAlarm(false);
                    FareParameters._faresActivation.SetAlarm(false);
                    EODFileStatusList.UpdateStatus("FareParameters", 1);
                }
                catch
                {
                    FareParameters._faresMissing.SetAlarm(true);
                    EODFileStatusList.UpdateStatus("FareParameters", 0);
                }

                //Update different levels
                _parametersMissing.UpdateMetaAlarm();
                _parametersError.UpdateMetaAlarm();
                _parametersActivationError.UpdateMetaAlarm();
                _parametersMetaStatus.UpdateMetaStatus();
                _globalMetaStatus.UpdateMetaStatus();
                Communication.SendMessage(ThreadName, "Status", "EODMetaStatus", Convert.ToString(_parametersMetaStatus.Value), EODFileStatusList.EODMetaStatus());


                IFSEventsList.SaveIfHasChangedSinceLastSave("TTComponent");
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Critical, "TTMain Cannot Load Parameters File " + e.Message);
            }
        }
#endif
    }
}