using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{

    public class EquipmentDenyListElement
    {
        public int Reference;
        public int EqpType;
    }

    public static class EquipmentDenyList
    {
        public static OneEvent _equipmentDenyListMissing = null;
        public static OneEvent _equipmentDenyListError = null;
        public static OneEvent _equipmentDenyListActivation = null;
        public static void Start()
        {
        }

        static EquipmentDenyList()
        {
            _equipmentDenyListMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 19, "EquipmentDenyListMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "EquipmentDenyListMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _equipmentDenyListError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 20, "EquipmentDenyListError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "EquipmentDenyListError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _equipmentDenyListActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 21, "EquipmentDenyListActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "EquipmentDenyListActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }
        private static Dictionary<Int64, EquipmentDenyListElement> _equipments = null;

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("EquipmentDenyList"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "EquipmentDenyList");
        }

        public static bool LoadVersion(string content)
        {            
            if (content == "")
            {
                //File is empty or not there
                _equipmentDenyListMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _equipmentDenyListMissing.SetAlarm(false);

            if (_equipments == null) _equipments = new Dictionary<Int64, EquipmentDenyListElement>();
            else _equipments.Clear();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;

                XmlNodeList nodelist = root.SelectNodes("Eqp");
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        EquipmentDenyListElement ep = new EquipmentDenyListElement();
                        ep.Reference = Convert.ToInt32(node.SelectSingleNode("Ref").InnerText);
                        ep.EqpType = Convert.ToInt32(node.SelectSingleNode("Type").InnerText);
                        _equipments.Add(ep.Reference, ep);
                    }

                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad equipment during read of Equipments" + e.Message);
                        _equipmentDenyListError.SetAlarm(true);
                    }
                }
                _equipmentDenyListError.SetAlarm(false);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "EquipmentDenyList_LoadVersion " + ex.Message);
                _equipmentDenyListError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }
        
        // ignoring EqpType only because don't have sufficient knowledge of it.
        public static bool VerifyEquipment(int Reference, int EqpType)
        {            
            EquipmentDenyListElement ep = new EquipmentDenyListElement();

            if (_equipments != null && _equipments.TryGetValue(Reference, out ep))
            {
                return false;
            }
            return true;
        }

    }
}
