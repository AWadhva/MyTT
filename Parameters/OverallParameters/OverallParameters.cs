using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{

    public static class OverallParameters
    {
        public static OneEvent _overallMissing = null;
        public static OneEvent _overallError = null;
        public static OneEvent _overallActivation = null;
        static OverallParameters()
        {
            _overallMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 28, "OverallMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "OverallMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _overallError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 29, "OverallError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "OverallError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _overallActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 30, "OverallActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "OverallActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }

        public static void Start()
        {
        }

        private static string _data = "";
        public static string Data { get { return _data; } }

        public static DateTime StartBusinessDay { get {return _startBusinessDay;}}
        public static DateTime EndBusinessDay { get { return _endBusinessDay; } }
        public static int MaxTimeDeviation { get { return _maxTimeDeviation; } }
        public static int MaximumTokensInContainer { get { return _maximumTokensInContainer; } }

        private static DateTime _startBusinessDay;
        private static DateTime _endBusinessDay;
        private static int _maxTimeDeviation;
        private static int _maximumTokensInContainer;

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("OverallParameters"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content,"OverallParameters");
        }


        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _overallMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _overallMissing.SetAlarm(false);
            try
            {
                //We keep the content for overall parameters.
                _data = content;
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;

                //XmlNode node = root.SelectSingleNode("OverallParameters");

                _startBusinessDay = Convert.ToDateTime(root.SelectSingleNode("StartBusinessDay").InnerText);
                _endBusinessDay = Convert.ToDateTime(root.SelectSingleNode("EndBusinessDay").InnerText);
                if (root.SelectSingleNode("MaxTimeDeviation") != null)
                {
                    try { _maxTimeDeviation = Convert.ToInt32(root.SelectSingleNode("MaxTimeDeviation").InnerText); }
                    catch { _maxTimeDeviation = 60000; }
                }
                if (root.SelectSingleNode("MaximumTokensInContainer") != null)
                {
                    try { _maximumTokensInContainer = Convert.ToInt32(root.SelectSingleNode("MaximumTokensInContainer").InnerText); }
                    catch { _maximumTokensInContainer = 2000; }
                }
                _overallError.SetAlarm(false);
                //We send to others module that need it
                Communication.SendMessage("Parameters", "Answer", "EquipmentParams", "OverallParameters",content);
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "OverallParameters_LoadVersion " + e.Message);
                _overallError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }
    }
}
         


       
                          

           
             



