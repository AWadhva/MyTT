using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules
    {
        public bool TreatParametersMessageReceived(EventMessage eventMessage)
        {
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

    }
}
