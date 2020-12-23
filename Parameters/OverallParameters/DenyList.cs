using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using IFS2.Equipment.Common;


namespace IFS2.Equipment.TicketingRules
{
    public  class MediaDenyListElement
    {
        public int PhysicalType;
        public Int64 SerialNumber;
        public int Reason;
    }




    public static class MediaDenyList
    {
        private static Dictionary<Int64, MediaDenyListElement> _medias = null;
        public static OneEvent _mediaDenyListMissing = null;
        public static OneEvent _mediaDenyListError = null;
        public static OneEvent _mediaDenyListActivation = null;
        public static void Start()
        {
        }

        static MediaDenyList()
        {
            _mediaDenyListMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 13, "MediaDenyListMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "MediaDenyListMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _mediaDenyListError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 14, "MediaDenyListError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "MediaDenyListError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _mediaDenyListActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 15, "MediaDenyListActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "MediaDenyListActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("MediaDenyList"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "MediaDenyList");
        }

        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _mediaDenyListMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _mediaDenyListMissing.SetAlarm(false);

            if (_medias == null) _medias = new Dictionary<Int64, MediaDenyListElement>();
            else _medias.Clear();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;

                XmlNodeList nodelist = root.SelectNodes("Medias/M");
                Logging.Log(LogLevel.Verbose, "Number of elements in MediaDenyList " + nodelist.Count.ToString());
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        MediaDenyListElement md = new MediaDenyListElement();
                        md.PhysicalType = Convert.ToInt32(node.SelectSingleNode("PT").InnerText);
                        md.SerialNumber = Convert.ToInt64(node.SelectSingleNode("SN").InnerText);
                        md.Reason = Convert.ToInt32(node.SelectSingleNode("R").InnerText);
                        _medias.Add(md.SerialNumber, md);
                        //Logging.Log(LogLevel.Verbose, "Element in Blacklist " + md.SerialNumber.ToString()); //To remove after

                    }
                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad media during read of equipments " + e.Message);
                        _mediaDenyListError.SetAlarm(true);
                    }

                }
                _mediaDenyListError.SetAlarm(false);
                return true;
            }

            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "MediaDenyList_LoadVersion " + ex.Message);
                _mediaDenyListError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }

        private static MediaDenyListElement _currentMediaListElement = null;
        public static MediaDenyListElement CurrentMedia { get { return _currentMediaListElement; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="password"></param>
        /// <returns>0 if not found else AgentProfile.</returns>
        public static bool VerifyMedia(Int32 physicalType, Int64 SerialNumber)
        {
            MediaDenyListElement md = new MediaDenyListElement();
            if (_medias == null)
                return false;

            if (_medias.TryGetValue(SerialNumber, out md))
            {
                //At the moment we have only one Media Type
                //if (md.PhysicalType == physicalType)
                _currentMediaListElement = md;
                return true;
            }           
            return false;
        }
    }

       public static class Equipment
        {

        }

}






















