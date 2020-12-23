using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class RangeDenyListElement
    {
        public int Reason;
        public int StartPhysicalType;
        public int StartSerialNumber;
        public Int64 EndPhysicalType;
        public Int64 EndSerialNumber;

    }
    public static class RangeDenyList
    {
        private static Dictionary<Int64, RangeDenyListElement> _ranges = null;
        public static OneEvent _rangeDenyListMissing = null;
        public static OneEvent _rangeDenyListError = null;
        public static OneEvent _rangeDenyListActivation = null;
        public static void Start()
        {
        }

        static RangeDenyList()
        {
            _rangeDenyListMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 16, "RangeDenyListMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "RangeDenyListMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _rangeDenyListError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 17, "RangeDenyListError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "RangeDenyListError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _rangeDenyListActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 18, "RangeDenyListActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "RangeDenyListActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("RangeDenyList"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "RangeDenyList");
        }

        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _rangeDenyListMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _rangeDenyListMissing.SetAlarm(false);

            if (_ranges == null) _ranges = new Dictionary<Int64, RangeDenyListElement>();
            else _ranges.Clear();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;

                XmlNodeList nodelist = root.SelectNodes("Range");

                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        RangeDenyListElement rg = new RangeDenyListElement();
                        rg.StartPhysicalType = Convert.ToInt32(node.SelectSingleNode("StPT").InnerText);
                        rg.StartSerialNumber = Convert.ToInt32(node.SelectSingleNode("StSN").InnerText);
                        rg.EndPhysicalType = Convert.ToInt64(node.SelectSingleNode("EndPT").InnerText);
                        rg.EndSerialNumber = Convert.ToInt64(node.SelectSingleNode("EndSN").InnerText);
                        rg.Reason = Convert.ToInt32(node.SelectSingleNode("Reas").InnerText);
                        _ranges.Add(rg.StartSerialNumber, rg);
                    }

                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad ranges during read of Equipments" + e.Message);
                        _rangeDenyListError.SetAlarm(true);
                    }
                }
                _rangeDenyListError.SetAlarm(false);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "RangeDenyList_LoadVersion " + ex.Message);
                _rangeDenyListError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="password"></param>
        /// <returns>0 if not found else AgentProfile.</returns>
        public static bool VerifyRange(int physicalType, Int64 SerialNumber)
        {
            foreach (RangeDenyListElement rg in _ranges.Values)
            {
                if (rg.StartPhysicalType == physicalType)
                {
                    if ((SerialNumber >= rg.StartSerialNumber) && (SerialNumber <= rg.EndSerialNumber))
                    {
                        return (true);
                    }
                }
            }
            return false;
        }  

    }
}
