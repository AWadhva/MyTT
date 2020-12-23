using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class LineElement
    {
        public string Name;
        public string ShortName;
        public int Code;
        public bool Activation;
        public List<int> Stations=null;
    }

    public  class StationElement
    {
        public string Name;
        public string ShortName;
        public int Index;
        public int Code;
        public int Reference;
        public bool Activation;
        public List<int> line=null;
    }

    public static class TopologyParameters
    {
        public static OneEvent _topologyMissing = null;
        public static OneEvent _topologyError = null;
        public static OneEvent _topologyActivation = null;
        public static void Start()
        {
        }

        static TopologyParameters()
        {
            _topologyMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 22, "TopologyMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "TopologyMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _topologyError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 23, "TopologyError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "TopologyError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _topologyActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 24, "TopologyActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "TopologyActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }

        public static bool LineExist(int line)
        {
            if (_lines.ContainsKey(line)) return true;
            else return false;
        }

        public static bool StationExist(int station)
        {
            if (_stations.ContainsKey(station)) return true;
            else return false;
        }

        public static string GetListStations(int lineNumber, int direction, Languages language)
        {
            try
            {
                EODGetListStationsOfOneLine result = new EODGetListStationsOfOneLine();
                //First search line
                LineElement line = _lines[lineNumber];
                if (direction == 1)
                {
                    for (int i = 0; i < line.Stations.Count; i++ )
                    {
                        StationElement station = Stations[line.Stations[i]];
                        if (station.Activation)
                        {
                            EODDetailOneStationInListStations det = new EODDetailOneStationInListStations();
                            det.Code = station.Code;
                            det.Index = station.Index;
                            det.Reference = station.Reference;
                            string s = ParametersDico.LongText("Stations", station.Code, language);
                            if (s != "" && s != Consts.NoTextFoundString) det.Name = s;
                            else det.Name = station.Name;
                            result.Stations.Add(det);
                        }
                    }
                }
                else if (direction==0)
                {
                    for (int i = line.Stations.Count-1; i >=0 ; i--)
                    {
                        StationElement station = Stations[line.Stations[i]];
                        if (station.Activation)
                        {
                            EODDetailOneStationInListStations det = new EODDetailOneStationInListStations();
                            det.Code = station.Code;
                            det.Index = station.Index;
                            det.Reference = station.Reference;
                            string s = ParametersDico.LongText("Stations", station.Code, language);
                            if (s != "" && s != Consts.NoTextFoundString) det.Name = s;
                            else det.Name = station.Name;
                            result.Stations.Add(det);
                        }
                    }
                }
                if (result.Stations.Count > 0)
                {
                    return SerializeHelper<EODGetListStationsOfOneLine>.XMLSerialize(result);
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TopologyParameters.GetListLines " + e.Message);
            }
            return "";
        }


        public static string GetListLines(Languages language)
        {
            try
            {
                EODGetListLines result = new EODGetListLines();
                foreach (LineElement line in _lines.Values)
                {
                    if (line.Activation)
                    {
                        EODDetailOneLineInListLine det = new EODDetailOneLineInListLine();
                        det.Code = line.Code;
                        string s = ParametersDico.LongText("Lines", line.Code, language);
                        if (s != "" && s != Consts.NoTextFoundString) det.Name = s;
                        else det.Name = line.Name;
                        //Now we need to take last stop to have one way direction
                        int stop = line.Stations[line.Stations.Count - 1];
                        s = ParametersDico.LongText("Stations", stop, language);
                        string s1 = TextConfiguration.ReadText("__Direction__", language);
                        if (s != "") det.NameTo = s1+":"+s;
                        else det.NameTo = s1 + ":" + _stations[stop].Name;
                        //Now we can take first stop to have return direction
                        stop = line.Stations[0];
                        s = ParametersDico.LongText("Stations", stop, language);
                        if (s != "") det.NameReturn = s1 + ":" + s;
                        else det.NameReturn = s1 + ":" + _stations[stop].Name;
                        result.Lines.Add(det);
                    }
                }
                if (result.Lines.Count > 0)
                {
                    return SerializeHelper<EODGetListLines>.XMLSerialize(result);
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TopologyParameters.GetListLines " + e.Message);
            }
            return "";
        }

        private static Dictionary<Int32, LineElement> _lines = null;
        private static Dictionary<Int32, StationElement> _stations = null;
        public static Dictionary<Int32, StationElement> Stations { get { return _stations;}}
        public static Dictionary<Int32, LineElement> Lines { get { return _lines; } }


        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("Topology"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "Topology");
        }

        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _topologyMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _topologyMissing.SetAlarm(false);

            if (_lines == null) _lines = new Dictionary<Int32, LineElement>();
            else _lines.Clear();
            if (_stations == null) _stations = new Dictionary<Int32, StationElement>();
            else _stations.Clear();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;
  
                XmlNodeList nodelist = root.SelectNodes("Lines/Line");
                if (nodelist == null)
                {
                    _topologyError.SetAlarm(false);
                    return true;
                }

                 foreach (XmlNode node in nodelist)
                 {
                    try
                    {
                        LineElement ltp = new LineElement();
                        ltp.Name = node.SelectSingleNode("Name").InnerText;
                        ltp.ShortName = node.SelectSingleNode("SName").InnerText;
                        ltp.Code = Convert.ToInt32( node.SelectSingleNode("Code").InnerText);
                        ltp.Activation = Convert.ToBoolean( Convert.ToInt32(node.SelectSingleNode("Act").InnerText));
                        ltp.Stations=new List<int>();

                        XmlNodeList nodelist1 = node.SelectNodes("Sts/St");
                    
                        foreach (XmlNode node1 in nodelist1)
                        {
                              try
                               {
                                   StationElement s;
                                    int j = Convert.ToInt32( node1.SelectSingleNode("Code").InnerText);
                                    ltp.Stations.Add(j);
                                    if (_stations.TryGetValue(j,out s))
                                    {
                                          _stations[j].line.Add(ltp.Code);
                                    }
                                    else
                                    {
                                        s = new StationElement();
                                        s.Code = j;
                                        s.Name = node1.SelectSingleNode("Name").InnerText;
                                        s.ShortName = node1.SelectSingleNode("SName").InnerText;
                                        s.Index = Convert.ToInt16( node1.SelectSingleNode("Ind").InnerText);
                                        s.Reference = Convert.ToInt16(node1.SelectSingleNode("Ref").InnerText);
                                        s.Activation = Convert.ToBoolean(Convert.ToInt32(node.SelectSingleNode("Act").InnerText));
                                        s.line = new List<int>();
                                        s.line.Add(ltp.Code);
                                        _stations.Add(s.Reference,s);
                                     }
                              }    
                              catch (Exception e)
                              {
                                      Logging.Log(LogLevel.Error, "Bad stations during read of lines " + e.Message);
                                      _topologyError.SetAlarm(true);
                              }                                         
                            }
                          _lines.Add(ltp.Code,ltp);
                 }
                 catch (Exception e)
                 {
                        Logging.Log(LogLevel.Error, "Bad stations during read of lines " + e.Message);
                        _topologyError.SetAlarm(true);
                 }                        
              }
              _topologyError.SetAlarm(false);
              return true;
            }
            catch (Exception e)
                {
                    Logging.Log(LogLevel.Error, "AgentList_LoadVersion " + e.Message);
                    _topologyError.SetAlarm(true);
                    throw (new Exception("****"));
                }
            }
    }   
           
    }
    





                               







          

                    







        




