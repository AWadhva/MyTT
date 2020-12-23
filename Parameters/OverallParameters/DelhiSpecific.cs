using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    //This project contains all function to transform XDR to XML for Delhi
    public static class DelhiSpecific
    {
        public static string TreatFareParameters(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                uint i;
                Logging.Log(LogLevel.Information, "DelhiSpecific_TreatSystemParameters Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                //Create Xml data
                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "FareParameters", "");
                xml.AppendChild(root);

                xdr.MoveForward(4); // File Header + File Creation Date + 1 + msgType

                XmlNode node1;
                XmlNode node2;
                XmlNode node3;
                XmlNode node4;
                XmlNode node5;
                XmlNode node6;

                //Begin Calendar
                node1 = xml.CreateNode(XmlNodeType.Element, "Calendars", "");
                root.AppendChild(node1);
                //In Delhi there is only one calendar
                node2 = xml.CreateNode(XmlNodeType.Element, "Calendar", "");
                node1.AppendChild(node2);
                //Start date of calendar
                node3 = xml.CreateNode(XmlNodeType.Element, "StartDate", "");
                node3.InnerText = xdr.ReadDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                node2.AppendChild(node3);
                node3 = xml.CreateNode(XmlNodeType.Element, "DaysNumber", "");
                i = xdr.ReadInt32();
                node3.InnerText = Convert.ToString(i);
                node2.AppendChild(node3);
                xdr.ReadInt32();
                i = xdr.ReadInt32();
                //For all the days
                for (int j = 0; j < i; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "Day", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Offs", "");
                    node4.InnerText = Convert.ToString(j);
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Type", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);
                }
                //Number of day type
                uint nbdaytype = xdr.ReadInt32();
                //ShortTripTime
                uint m1 = xdr.ReadInt32();
                //PaidTime
                uint m2 = xdr.ReadInt32();
                //11 Not used
                xdr.MoveForward(27);
                //Short return fare
                uint m3 = xdr.ReadInt32();
                //Number of time interval
                uint nbinterval = xdr.ReadInt32();
                //Number of ticket types
                uint nbtickettype = xdr.ReadInt32();
                //Number of concession
                uint nbconcession = xdr.ReadInt32();
                //Number of SP
                uint nbsp = xdr.ReadInt32();

                //Begin Time intervals
                node1 = xml.CreateNode(XmlNodeType.Element, "TimeIntervals", "");
                root.AppendChild(node1);
                //In Delhi there is only one calendar
                node2 = xml.CreateNode(XmlNodeType.Element, "TimeInterval", "");
                node1.AppendChild(node2);
                //Start date of calendar
                node3 = xml.CreateNode(XmlNodeType.Element, "DefinitionRule", "");
                node3.InnerText = Convert.ToString(xdr.ReadInt32());
                node2.AppendChild(node3);
                i = xdr.ReadInt32();
                //For all the intervals
                for (int j = 0; j < i; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "Time", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Start", "");
                    node4.InnerText = Convert.ToString(xdr.ReadDateTime().ToString("HH:mm:ss"));
                    node3.AppendChild(node4);
                }

                i = xdr.ReadInt32();
                if (i != nbtickettype)
                {
                    throw (new Exception("Inconsistent file in FareType table"));
                }

                //We have not all elements to treat this matrix. We memorise and will be treated after
                long mem1 = xdr.Position;
                xdr.MoveForward(i * 10);


                //Table concession vide
                xdr.ReadInt32();
                //Nombre fare groups
                uint nbfaregroup = xdr.ReadInt32();
                //Nombre fare type
                uint nbfaretype = xdr.ReadInt32();

                //Begin Fare group matrix. Impossible to do the matriw at this step due to mising of Fare tiers number
                long mem2 = xdr.Position;
                i = xdr.ReadInt32();
                xdr.MoveForward(i);

                //Begin Fare type
                i = xdr.ReadInt32();
                if (i != nbtickettype)
                {
                    throw (new Exception("Inconsistent file in FareType table"));
                }

                //We have now everything to make products table
                //Begin Ticket Type
                node2 = xml.CreateNode(XmlNodeType.Element, "Products", "");
                root.AppendChild(node2);
                long mem3 = xdr.Position;

                //For all the ticket types
                for (int j = 0; j < i; j++)
                {
                    xdr.Position = mem1;

                    node3 = xml.CreateNode(XmlNodeType.Element, "Prd", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Code", "");
                    node4.InnerText = xdr.ReadString(6);
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                    uint k = xdr.ReadInt32();
                    node4.InnerText = Convert.ToString(k);
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Act", "");
                    if (k == 0) node4.InnerText = "0";
                    else node4.InnerText = "1";
                    node3.AppendChild(node4);
                    //SP & rail card not used for delhi
                    xdr.ReadInt32();
                    xdr.ReadInt32();
                    node4 = xml.CreateNode(XmlNodeType.Element, "Fam", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);

                    mem1 = xdr.Position;
                    xdr.Position = mem3;

                    for (int m = 0; m < 4; m++)
                    {
                        node4 = xml.CreateNode(XmlNodeType.Element, "SP", "");
                        node3.AppendChild(node4);
                        node5 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                        node5.InnerText = Convert.ToString(m);
                        node4.AppendChild(node5);
                        for (int l = 0; l < 3; l++)
                        {
                            node5 = xml.CreateNode(XmlNodeType.Element, "RT", "");
                            node4.AppendChild(node5);
                            node6 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                            node6.InnerText = Convert.ToString(l);
                            node5.AppendChild(node6);
                            node6 = xml.CreateNode(XmlNodeType.Element, "FTyp", "");
                            node6.InnerText = Convert.ToString(xdr.ReadInt32());
                            node5.AppendChild(node6);
                        }
                    }
                    mem3 = xdr.Position;
                }
                uint nbfaretiers = xdr.ReadInt32();
                mem1 = xdr.Position;
                xdr.Position = mem2;

                node1 = xml.CreateNode(XmlNodeType.Element, "FareTables", "");
                root.AppendChild(node1);
                node2 = xml.CreateNode(XmlNodeType.Element, "GlobalFareTable", "");
                node1.AppendChild(node2);
                i = xdr.ReadInt32();
                if (i != nbfaregroup * nbfaretiers * nbconcession)
                {
                    throw (new Exception("Inconsistent file in GlobalFareTable"));
                }

                for (int j = 0; j < nbfaregroup; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "FGr", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                    //Something strange while not j+1 but it seems not to work for faregroup 1.
                    node4.InnerText = Convert.ToString(j);
                    node3.AppendChild(node4);

                    for (int k = 0; k < nbfaretiers; k++)
                    {
                        node4 = xml.CreateNode(XmlNodeType.Element, "FTie", "");
                        node3.AppendChild(node4);
                        node5 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                        //It was k+1, it seems that k is the right value.
                        node5.InnerText = Convert.ToString(k);
                        node4.AppendChild(node5);
                        node5 = xml.CreateNode(XmlNodeType.Element, "Conc", "");
                        node4.AppendChild(node5);
                        node6 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                        node6.InnerText = "1";
                        node5.AppendChild(node6);
                        node6 = xml.CreateNode(XmlNodeType.Element, "FVa", "");
                        node6.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                        node5.AppendChild(node6);
                    }
                }

                xdr.Position = mem1;
                i = xdr.ReadInt32();
                double d = Math.Sqrt(i);
                if (Math.Ceiling(d) != Math.Floor(d))
                {
                    throw (new Exception("Inconsistent file in Station to station"));
                }
                int nbstat = Convert.ToInt32(Math.Ceiling(d));

                node1 = xml.CreateNode(XmlNodeType.Element, "Matrixes", "");
                root.AppendChild(node1);
                node2 = xml.CreateNode(XmlNodeType.Element, "StationToStationMatrix", "");
                node1.AppendChild(node2);
                for (int j = 0; j < nbstat; j++)
                {
                    for (int k = 0; k < nbstat; k++)
                    {
                        node3 = xml.CreateNode(XmlNodeType.Element, "Cell", "");
                        node2.AppendChild(node3);
                        node4 = xml.CreateNode(XmlNodeType.Element, "En", "");
                        node4.InnerText = Convert.ToString(j + 1);
                        node3.AppendChild(node4);
                        node4 = xml.CreateNode(XmlNodeType.Element, "Ex", "");
                        node4.InnerText = Convert.ToString(k + 1);
                        node3.AppendChild(node4);
                        node4 = xml.CreateNode(XmlNodeType.Element, "FT", "");
                        node4.InnerText = Convert.ToString(xdr.ReadInt32());
                        node3.AppendChild(node4);
                    }
                }

                node2 = xml.CreateNode(XmlNodeType.Element, "GlobalFareGroupTable", "");
                node1.AppendChild(node2);
                i = xdr.ReadInt32();
                if (i != nbfaretype * nbdaytype * nbinterval)
                {
                    throw (new Exception("Inconsistent file in basic fare group table"));
                }
                for (int j = 0; j < nbfaretype; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "FTyp", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                    node4.InnerText = Convert.ToString(j + 1);
                    node3.AppendChild(node4);

                    for (int k = 0; k < nbdaytype; k++)
                    {
                        node4 = xml.CreateNode(XmlNodeType.Element, "DTyp", "");
                        node3.AppendChild(node4);
                        node5 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                        node5.InnerText = Convert.ToString(k);
                        node4.AppendChild(node5);
                        for (int l = 0; l < nbinterval; l++)
                        {
                            node5 = xml.CreateNode(XmlNodeType.Element, "Int", "");
                            node4.AppendChild(node5);
                            node6 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                            node6.InnerText = Convert.ToString(l);
                            node5.AppendChild(node6);
                            node6 = xml.CreateNode(XmlNodeType.Element, "FGr", "");
                            node6.InnerText = Convert.ToString(xdr.ReadInt32());
                            node5.AppendChild(node6);
                            xdr.MoveForward(5);
                        }
                    }
                }
                //Perhaps pb. Tables not used supposed to be a fixed size.
                xdr.MoveForward(73);

                uint nbmedia = xdr.ReadInt32();

                i = xdr.ReadInt32();
                if (i != nbmedia)
                {
                    throw (new Exception("Inconsistent file in media table"));
                }
                node1 = xml.CreateNode(XmlNodeType.Element, "Media", "");
                root.AppendChild(node1);
                node2 = xml.CreateNode(XmlNodeType.Element, "MediaTypes", "");
                node1.AppendChild(node2);
                for (int j = 0; j < i; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "MediaType", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Code", "");
                    node4.InnerText = xdr.ReadString(3);
                    node3.AppendChild(node4);
                }

                uint nbmediatechno = xdr.ReadInt32();
                i = xdr.ReadInt32();
                if (i != nbmediatechno)
                {
                    throw (new Exception("Inconsistent file in media table techno"));
                }

                node2 = xml.CreateNode(XmlNodeType.Element, "MediaTechnos", "");
                node1.AppendChild(node2);
                for (int j = 0; j < i; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "MediaTechno", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                    node4.InnerText = Convert.ToString(j);
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "DurVal", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "MaxTxns", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);
                }

                //Surcharge Table
                node1 = root.SelectSingleNode("FareTables");
                node2 = xml.CreateNode(XmlNodeType.Element, "GlobalSurchargeList", "");
                node1.AppendChild(node2);
                //i = xdr.ReadInt32(); No counter on this table
                for (int j = 0; j < 20; j++)
                {
                    node3 = xml.CreateNode(XmlNodeType.Element, "Surcharge", "");
                    node2.AppendChild(node3);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Code", "");
                    node4.InnerText = xdr.ReadString(3);
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Label", "");
                    node4.InnerText = xdr.ReadString(20);
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "Price", "");
                    node4.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                    node3.AppendChild(node4);
                    node4 = xml.CreateNode(XmlNodeType.Element, "DisplayedCode", "");
                    node4.InnerText = xdr.ReadString(3);
                    node3.AppendChild(node4);
                }

                //General Tag
                node1 = xml.CreateNode(XmlNodeType.Element, "General", "");
                root.AppendChild(node1);
                node2 = xml.CreateNode(XmlNodeType.Element, "GoodwillAmount", "");
                node2.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                node1.AppendChild(node2);
                node2 = xml.CreateNode(XmlNodeType.Element, "MaximumValidationAmount", "");
                node2.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                node1.AppendChild(node2);
                node2 = xml.CreateNode(XmlNodeType.Element, "MinimumExitValue", "");
                node2.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                node1.AppendChild(node2);
                node2 = xml.CreateNode(XmlNodeType.Element, "ShortestReturnTripDuration", "");
                node2.InnerText = Convert.ToString(m1);
                node1.AppendChild(node2);
                node2 = xml.CreateNode(XmlNodeType.Element, "ShortReturnTripFare", "");
                node2.InnerText = Convert.ToString(10 * m3);
                node1.AppendChild(node2);
                node2 = xml.CreateNode(XmlNodeType.Element, "PaidTime", "");
                node2.InnerText = Convert.ToString(m2);
                node1.AppendChild(node2);


                xdr.Close();
                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Fare parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }
        //Work in xdr file to create a string that corresponds to xml file attended
        public static string TreatSystemParameters(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatSystemParameters Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                //Create Xml data
                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "OverallParameters", "");
                xml.AppendChild(root);

                xdr.MoveForward(6);
                XmlNode node = xml.CreateNode(XmlNodeType.Element, "StartBusinessDay", "");
                node.InnerText = Convert.ToString(xdr.ReadDateTime().ToString("HH:mm:ss"));
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "EndBusinessDay", "");
                node.InnerText = Convert.ToString(xdr.ReadDateTime().ToString("HH:mm:ss"));
                root.AppendChild(node);

                xdr.MoveForward(2);
                node = xml.CreateNode(XmlNodeType.Element, "MaxTimeDeviation", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                //To be corrected. It is not at the right place
                xdr.MoveForward(2);
                node = xml.CreateNode(XmlNodeType.Element, "MaximumTokensInContainer", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);
                xdr.Close();

                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create overall system parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }


        public static string TreatAgentList(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatAgentlist Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "AgentListParameters", "");
                xml.AppendChild(root);

                // xdr.MoveForward();
                XmlNode node = xml.CreateNode(XmlNodeType.Element, "Agents", "");
                root.AppendChild(node);


                xdr.MoveForward(5);

                uint totalagents = xdr.ReadInt32();

                while (totalagents > 0)
                {

                    XmlNode node1 = xml.CreateNode(XmlNodeType.Element, "Agt", "");
                    node.AppendChild(node1);

                    XmlNode node2 = xml.CreateNode(XmlNodeType.Element, "Name", "");
                    node2.InnerText = Convert.ToString(xdr.ReadString(16));
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "Pwd", "");
                    node2.InnerText = Convert.ToString(xdr.ReadString(8));
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "ID", "");
                    node2.InnerText = Convert.ToString(xdr.ReadString(8));
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "Prof", "");
                    int i = xdr.ReadInt8();
                    AgentProfile p;
                    switch (i)
                    {
                        case 60:
                            p = AgentProfile.Operator;
                            break;
                        case 61:
                            p = AgentProfile.Operator;
                            break;
                        case 62:
                            p = AgentProfile.Maintenance;
                            break;
                        case 63:
                            p = AgentProfile.Supervisor;
                            break;
                        case 67:
                            p = AgentProfile.Operator;
                            break;
                        case 69:
                            p = AgentProfile.Supervisor;
                            break;
                        case 70:
                        case 64:
                        case 65:
                        case 66:
                        case 68:
                        default:
                            p = AgentProfile.NotKnown;
                            break;
                    }
                    node2.InnerText = Convert.ToString((int)p);
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "Card", "");
                    node2.InnerText = Convert.ToString(xdr.ReadInt8());
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "Sta", "");
                    node2.InnerText = Convert.ToString(xdr.ReadString(3));
                    node1.AppendChild(node2);

                    totalagents--;
                }
                xdr.Close();
                return xml.InnerXml;

            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Agent List parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }

        public static string TreatTopologyParameters(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatTopology Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "Topology", "");
                xml.AppendChild(root);

                // xdr.MoveForward();
                XmlNode node = xml.CreateNode(XmlNodeType.Element, "Lines", "");
                root.AppendChild(node);

                XmlNode node1;
                xdr.MoveForward(563);
                uint linesNumber = xdr.ReadInt32();
                uint i = linesNumber;
                long mem = 0;
                uint totalStations = 0;

                int temp = 1;
                while (linesNumber > 0)
                {
                    node1 = xml.CreateNode(XmlNodeType.Element, "Line", "");
                    node.AppendChild(node1);

                    XmlNode node2 = xml.CreateNode(XmlNodeType.Element, "Name", "");
                    node2.InnerText = Convert.ToString(xdr.ReadString(20));
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "SName", "");
                    node2.InnerText = Convert.ToString(xdr.ReadString(3));
                    node1.AppendChild(node2);

                    int lineCode = xdr.ReadInt8();
                    node2 = xml.CreateNode(XmlNodeType.Element, "Code", "");
                    node2.InnerText = Convert.ToString(lineCode);
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "Act", "");
                    node2.InnerText = Convert.ToString(xdr.ReadInt8());
                    node1.AppendChild(node2);

                    node2 = xml.CreateNode(XmlNodeType.Element, "Sts", "");
                    node1.AppendChild(node2);

                    uint stationsNumber = xdr.ReadInt32();
                    xdr.MoveForward(10);
                    mem = xdr.Position;
                    xdr.Position = (4 * 564 + 4 * i * 36);
                    totalStations = xdr.ReadInt32();

                    while (totalStations > 0)
                    {
                        if (xdr.ReadInt8() == lineCode)
                        {
                            XmlNode node3 = xml.CreateNode(XmlNodeType.Element, "St", "");
                            node2.AppendChild(node3);

                            XmlNode node4 = xml.CreateNode(XmlNodeType.Element, "Name", "");
                            node4.InnerText = xdr.ReadString(50);
                            node3.AppendChild(node4);

                            var Name = node4.InnerText;

                            node4 = xml.CreateNode(XmlNodeType.Element, "SName", "");
                            node4.InnerText = xdr.ReadString(3);
                            node3.AppendChild(node4);

                            var Mnemonic = node4.InnerText;

                            node4 = xml.CreateNode(XmlNodeType.Element, "Ind", "");
                            uint j = xdr.ReadInt32();
                            node4.InnerText = Convert.ToString(j);
                            node3.AppendChild(node4);

                            node4 = xml.CreateNode(XmlNodeType.Element, "Code", "");
                            node4.InnerText = Convert.ToString(xdr.ReadInt32());
                            node3.AppendChild(node4);

                            var Code = node4.InnerText;

                            node4 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                            node4.InnerText = Convert.ToString(j);
                            node3.AppendChild(node4);

                            // Anuj: Doing it like this, because don't want to disturb the way of fetching "Code"
                            xdr.ModeBefore(2); // now we're at beginning of StationIndex

                            xdr.ReadInt8(); // read StationIndex
                            xdr.ReadInt8(); // read CSCStationCode
                            xdr.ReadInt8(); // read UDStationCode
                            j = xdr.ReadInt8(); // read Activation

                            node4 = xml.CreateNode(XmlNodeType.Element, "Act", "");
                            node4.InnerText = j.ToString();
                            node3.AppendChild(node4);

                            xdr.ReadInt8(); // read StationType
                            xdr.ReadInt8(); // read StationZone

                            Console.WriteLine("Name = {0}; Mnemonic  = {1}; Code = {2}; Act = {3}; SNum = {4}", Name, Mnemonic, Code, j, temp++);
                        }
                        else
                        {
                            xdr.MoveForward(59);
                        }
                        totalStations--;
                    }
                    xdr.Position = mem;
                    linesNumber--;
                }
                xdr.Close();
                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create topology parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }


        public static string TreatTVMEquipmentParameters(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatTVMEquipmentParameters Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "TVMEquipmentParameters", "");
                xml.AppendChild(root);

                xdr.MoveForward(4);
                XmlNode node = xml.CreateNode(XmlNodeType.Element, "NbTransactionsDisplay", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "NbAlarmsDisplay", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "AuthenticationPeriod", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "TransactionSendTime", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "AuditSendTime", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "EventLogSendTime", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                //xdr.MoveForward(1);
                node = xml.CreateNode(XmlNodeType.Element, "MinBankCardPayment", "");
                node.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "MaxCashForPayment", "");
                node.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "MaxCoinForChange", "");
                node.InnerText = Convert.ToString(10 * xdr.ReadInt32());
                root.AppendChild(node);

                //xdr.MoveForward(1);
                node = xml.CreateNode(XmlNodeType.Element, "CoinBoxAlmostFull", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "NoteBoxAlmostFull", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "CoinBoxFull", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "NoteBoxFull", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "CardMaxErrors", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "TokenAlmostEmpty", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "NumberOfWrongToken", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "AgentPinEntryTimeOut", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);

                node = xml.CreateNode(XmlNodeType.Element, "TransactionTimeOut", "");
                node.InnerText = Convert.ToString(xdr.ReadInt32());
                root.AppendChild(node);
                xdr.Close();
                return xml.InnerXml;
            }

            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Equipment system parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }

        public static string TreatEquipmentDenyList(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatEquipmentDenyListParameters Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "EquipmentDenyList", "");
                xml.AppendChild(root);

                XmlNode node = xml.CreateNode(XmlNodeType.Element, "Equipments", "");
                root.AppendChild(node);

                xdr.MoveForward(5);

                uint totalmachines = xdr.ReadInt32();

                while (totalmachines > 0)
                {
                    XmlNode node1 = xml.CreateNode(XmlNodeType.Element, "Eqp", "");
                    node.AppendChild(node1);

                    XmlNode node2 = xml.CreateNode(XmlNodeType.Element, "Ref", "");
                    uint val;
                    val = xdr.ReadInt32();
                    val += 256 * xdr.ReadInt32();
                    uint eqpType = xdr.ReadInt32();
                    val += 65536 * eqpType;
                    node2.InnerText = Convert.ToString(val);
                    node1.AppendChild(node2);
                    node2 = xml.CreateNode(XmlNodeType.Element, "Type", "");
                    node2.InnerText = Convert.ToString(eqpType);
                    node1.AppendChild(node2);

                    totalmachines--;
                }
                xdr.Close();
                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Equipment Deny list parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }

        public static string TreatMediaDenyList(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatMediaDenyListParameters Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "MediaDenyList", "");
                xml.AppendChild(root);

                XmlNode node = xml.CreateNode(XmlNodeType.Element, "Medias", "");
                root.AppendChild(node);

                xdr.MoveForward(5);
                uint totalRanges = xdr.ReadInt32();
                uint MediasNumber = xdr.ReadInt32();

                while (MediasNumber > 0)
                {


                    uint mt = xdr.ReadInt32();
                    if (mt == 6)
                    {
                        XmlNode node1 = xml.CreateNode(XmlNodeType.Element, "M", "");
                        node.AppendChild(node1);

                        XmlNode node2 = xml.CreateNode(XmlNodeType.Element, "PT", "");
                        node2.InnerText = Convert.ToString(mt);
                        node1.AppendChild(node2);

                        node2 = xml.CreateNode(XmlNodeType.Element, "SN", "");
                        node2.InnerText = Convert.ToString((xdr.ReadInt64Inverted() >> 8));
                        node1.AppendChild(node2);

                        node2 = xml.CreateNode(XmlNodeType.Element, "R", "");
                        node2.InnerText = Convert.ToString(xdr.ReadInt16());
                        node1.AppendChild(node2);
                    }
                    else if (mt == 3)
                    {
                        xdr.MoveForward(5);
                    }
                    else
                    {
                        throw (new Exception("Unknown Media Type"));
                    }

                    MediasNumber--;
                }
                xdr.Close();
                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Media Deny List parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }

        public static string TreatRangeDenyList(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatRangeDenyList Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "RangeDenyList", "");
                xml.AppendChild(root);

                XmlNode node = xml.CreateNode(XmlNodeType.Element, "Ranges", "");
                root.AppendChild(node);

                xdr.MoveForward(5);
                uint totalRanges = xdr.ReadInt32();
                uint MediasNumber = xdr.ReadInt32();

                while (MediasNumber > 0)
                {
                    uint mt = xdr.ReadInt32();
                    if (mt == 6)
                    {
                        xdr.MoveForward(3);
                    }
                    else if (mt == 3)
                    {
                        xdr.MoveForward(5);
                    }
                    else
                    {
                        throw (new Exception("Unknown Media Type"));
                    }

                    MediasNumber--;
                }

                node = xml.CreateNode(XmlNodeType.Element, "Ranges", "");
                root.AppendChild(node);

                while (totalRanges > 0)
                {
                    XmlNode node3 = xml.CreateNode(XmlNodeType.Element, "Range", "");
                    node.AppendChild(node3);

                    XmlNode node4 = xml.CreateNode(XmlNodeType.Element, "StPT", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);

                    node4 = xml.CreateNode(XmlNodeType.Element, "StSN", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt64());
                    node3.AppendChild(node4);

                    node4 = xml.CreateNode(XmlNodeType.Element, "EndPT", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);

                    node4 = xml.CreateNode(XmlNodeType.Element, "EndSN", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt64());
                    node3.AppendChild(node4);

                    node4 = xml.CreateNode(XmlNodeType.Element, "Reas", "");
                    node4.InnerText = Convert.ToString(xdr.ReadInt32());
                    node3.AppendChild(node4);

                    totalRanges--;
                }
                xdr.Close();
                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Range Deny List parameters xml " + e.Message);
                throw (new Exception("****"));

            }
        }

        public static string TreatFileKeyListParameters(string xdrFileName)
        {
            XdrToXml xdr = null;
            try
            {
                Logging.Log(LogLevel.Information, "BDelhiSpecific_TreatFileKeyListParameters Starting" + xdrFileName);

                //Trying to open xdr Data
                xdr = new XdrToXml();
                xdr.Initialise(xdrFileName);

                XmlDocument xml = new XmlDocument();
                xml.CreateXmlDeclaration("1.0", "UTF8", "yes");
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "FileKey", "");
                xml.AppendChild(root);
                xdr.MoveForward(2);
                uint Keytypes = xdr.ReadInt32();

                while (Keytypes > 0)
                {
                    XmlNode node;
                    XmlNode node1;
                    XmlNode node2;
                    XmlNode node3;
                    XmlNode node4;
                    XmlNode node5;
                    uint keytype = xdr.ReadInt32();
                    uint numofitems = xdr.ReadInt32();
                    switch (keytype)
                    {
                        case 1:
                            xdr.MoveForward(numofitems * 37);
                            break;
                        case 2:
                            xdr.MoveForward(numofitems * 27);
                            break;
                        case 3:
                            xdr.MoveForward(numofitems * 35);
                            break;
                        case 4:
                            node = xml.CreateNode(XmlNodeType.Element, "Media", "");
                            root.AppendChild(node);
                            node1 = xml.CreateNode(XmlNodeType.Element, "MediaType", "");
                            node1.InnerText = "TokenUltralight";
                            node.AppendChild(node1);
                            node1 = xml.CreateNode(XmlNodeType.Element, "Versions", "");
                            node.AppendChild(node1);

                            for (int i = 0; i < numofitems; i++)
                            {
                                node2 = xml.CreateNode(XmlNodeType.Element, "Version", "");
                                node1.AppendChild(node2);
                                node3 = xml.CreateNode(XmlNodeType.Element, "Reference", "");
                                node3.InnerText = Convert.ToString(xdr.ReadInt32());
                                node2.AppendChild(node3);
                                node3 = xml.CreateNode(XmlNodeType.Element, "Keys", "");
                                node2.AppendChild(node3);
                                node4 = xml.CreateNode(XmlNodeType.Element, "Key", "");
                                node3.AppendChild(node4);
                                node5 = xml.CreateNode(XmlNodeType.Element, "KeyValue", "");
                                node5.InnerText = xdr.ReadHexa(8);
                                node4.AppendChild(node5);
                            }
                            break;
                        case 6:
                            node = xml.CreateNode(XmlNodeType.Element, "Media", "");
                            root.AppendChild(node);
                            node1 = xml.CreateNode(XmlNodeType.Element, "MediaType", "");
                            node1.InnerText = "DesfireEV0";
                            node.AppendChild(node1);
                            node1 = xml.CreateNode(XmlNodeType.Element, "Versions", "");
                            node.AppendChild(node1);

                            for (int i = 0; i < numofitems; i++)
                            {
                                node2 = xml.CreateNode(XmlNodeType.Element, "Version", "");
                                node1.AppendChild(node2);
                                node3 = xml.CreateNode(XmlNodeType.Element, "Reference", "");
                                node3.InnerText = Convert.ToString(xdr.ReadInt32());
                                node2.AppendChild(node3);
                                node3 = xml.CreateNode(XmlNodeType.Element, "Keys", "");
                                node2.AppendChild(node3);
                                uint nbkeys = xdr.ReadInt32();
                                for (int j = 0; j < nbkeys; j++)
                                {
                                    node4 = xml.CreateNode(XmlNodeType.Element, "Key", "");
                                    node3.AppendChild(node4);

                                    node5 = xml.CreateNode(XmlNodeType.Element, "ApplicationID", "");
                                    node5.InnerText = Convert.ToString(xdr.ReadInt32());
                                    node4.AppendChild(node5);
                                    node5 = xml.CreateNode(XmlNodeType.Element, "FileID", "");
                                    node5.InnerText = Convert.ToString(xdr.ReadInt32());
                                    node4.AppendChild(node5);
                                    node5 = xml.CreateNode(XmlNodeType.Element, "KeySet", "");
                                    node5.InnerText = Convert.ToString(xdr.ReadInt32());
                                    node4.AppendChild(node5); ;
                                    node5 = xml.CreateNode(XmlNodeType.Element, "KeyNumber", "");
                                    node5.InnerText = Convert.ToString(xdr.ReadInt32());
                                    node4.AppendChild(node5);
                                    node5 = xml.CreateNode(XmlNodeType.Element, "KeyCardNo", "");
                                    node5.InnerText = Convert.ToString(xdr.ReadInt32());
                                    node4.AppendChild(node5);
                                    node5 = xml.CreateNode(XmlNodeType.Element, "AccessRight", "");
                                    node5.InnerText = Convert.ToString(xdr.ReadInt32());
                                    node4.AppendChild(node5);
                                    node5 = xml.CreateNode(XmlNodeType.Element, "KeyValue", "");
                                    node5.InnerText = xdr.ReadHexa(16);
                                    node4.AppendChild(node5);
                                }
                            }
                            break;
                    }
                    Keytypes--;
                }
                xdr.Close();
                return xml.InnerXml;
            }
            catch (Exception e)
            {
                if (xdr != null) xdr.Close();
                Logging.Log(LogLevel.Error, "Cannot create Keyst List parameters xml " + e.Message);
                throw (new Exception("****"));
            }
        }
    }
}