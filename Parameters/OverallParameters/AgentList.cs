using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{



    public static class AgentList
    {
        private static Dictionary<int, EOD_OneAgent> _agents = null;
        public static OneEvent _agentListMissing = null;
        public static OneEvent _agentListError = null;
        public static OneEvent _agentListActivation = null;

        private static bool _passwordAsDigest = false;
        public static void Start()
        {
        }

        static AgentList()
        {
            _agentListMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 10, "AgentListMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "AgentListMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _agentListError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 11, "AgentListError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "AgentListError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _agentListActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 12, "AgentListActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "AgentListActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _passwordAsDigest = Configuration.ReadBoolParameter("AgentListPasswordAsDigest", false);
        }

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("AgentList"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "AgentList");
        }

        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _agentListMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _agentListMissing.SetAlarm(false);




            //Initialisation of dictionary
            if (_agents == null) _agents = new Dictionary<int, EOD_OneAgent>();
            else _agents.Clear();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement; 


                    XmlNodeList nodelist = root.SelectNodes("Agents/Agt");
                    foreach (XmlNode node in nodelist)
                    {
                        string err = " Unknown"; ;
                        try
                        {
                            EOD_OneAgent ag = new EOD_OneAgent();
                            ag.Name = node.SelectSingleNode("Name").InnerText;
                            ag.Password = node.SelectSingleNode("Pwd").InnerText;
                            ag.AgentID = Convert.ToInt32(node.SelectSingleNode("ID").InnerText);
                            err = " " + Convert.ToString(ag.AgentID);
                            int i = Convert.ToInt32(node.SelectSingleNode("Prof").InnerText);
                            if (_passwordAsDigest) //Direct xml from BO
                            {
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
                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                    case 7:
                                    case 8:
                                    case 9:
                                        p = (AgentProfile)i;
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
                                ag.AgentProfile = p;
                            }
                            else
                            {
                                ag.AgentProfile = (AgentProfile)i;
                            }
                            string s = node.SelectSingleNode("Card").InnerText;
                            if (s == "1" || s.ToUpper() == "TRUE") ag.AgentCard = true;
                            else ag.AgentCard = false;
                            ag.Stations = node.SelectSingleNode("Sta").InnerText;
                            _agents.Add(ag.AgentID,ag);
                        }
                        catch (Exception e)
                        {
                            Logging.Log(LogLevel.Error, "Bad Agent during read of agents " + e.Message+err);
                        }
                    }
                    if (nodelist.Count == 0) _agentListError.SetAlarm(true);
                    else _agentListError.SetAlarm(false);
                    return true;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "AgentList_LoadVersion " + ex.Message);
                _agentListError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="password"></param>
        /// <returns>0 if not found else AgentProfile.</returns>
        public static AgentProfile VerifyAgent(int agentID, string password, out EOD_OneAgent agent)
        {
            //EOD_OneAgent ag = new EOD_OneAgent();
            //bAgentCard = false;
            //stations = "";
            if (_agents.TryGetValue(agentID,out agent))
            {
                bool ok = false;
                if (_passwordAsDigest) ok = (agent.Password==Crypto.SHA1CalculationOfString(password));
                else ok = (agent.Password == password);
                if (ok)
                {
                    //bAgentCard = ag.AgentCard;
                    //stations = ag.Stations;
                    return agent.AgentProfile;
                }
            }
            return AgentProfile.NotKnown;
        }

        public static bool IsValidAgent(long agentID)
        {
            EOD_OneAgent ag;
            return _agents.TryGetValue((int)agentID, out ag);
        }
   }
}