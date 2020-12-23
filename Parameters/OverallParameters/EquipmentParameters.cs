using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public static class TVMEquipmentParameters
    {
        public static OneEvent _equipmentParametersMissing = null;
        public static OneEvent _equipmentParametersError = null;
        public static OneEvent _equipmentParametersActivation = null;

        static TVMEquipmentParameters()
        {
            _equipmentParametersMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 31, "EquipmentParametersMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "EquipmentParametersMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _equipmentParametersError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 32, "EquipmentParametersError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "EquipmentParametersError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _equipmentParametersActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 33, "EquipmentParametersActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "EquipmentParametersActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }
        public static void Start()
        {
        }

        private static int _nbTransactionsDisplay;
        private static int _nbAlarmsDisplay;
        private static int _authenticationPeriod;
        private static int _transactionSendTime;
        private static int _auditSendTime;
        private static int _eventLogSendTime;
        private static int _minBankCardPayment;
        private static int _maxCashForPayment;
        private static int _maxCoinForChange;
        private static int _coinBoxAlmostFull;
        private static int _noteBoxAlmostFull;
        private static int _coinBoxFull;
        private static int _noteBoxFull;
        private static int _cardMaxErrors;
        private static int _tokenAlmostEmpty;
        private static int _numberOfWrongToken;
        private static int _agentPinEntryTimeOut;
        private static int _transactionTimeOut;

        public static int NbTransactionsDisplay { get { return _nbTransactionsDisplay; } }
        public static int NbAlarmsDisplay { get { return _nbAlarmsDisplay; } }
        public static int AuthenticationPeriod { get { return _authenticationPeriod; } }
        public static int TransactionSendTime { get { return _transactionSendTime; } }
        public static int AuditSendTime { get { return _auditSendTime; } }
        public static int EventLogSendTime { get { return _eventLogSendTime; } }
        public static int MinBankCardPayment { get { return _minBankCardPayment; } }
        public static int MaxCashForPayment { get { return _maxCashForPayment; } }
        public static int MaxCoinForChange { get { return _maxCoinForChange; } }
        public static int CoinBoxAlmostFull { get { return _coinBoxAlmostFull; } }
        public static int NoteBoxAlmostFull { get { return _noteBoxAlmostFull; } }
        public static int CoinBoxFull { get { return _coinBoxFull; } }
        public static int NoteBoxFull { get { return _noteBoxFull; } }
        public static int CardMaxErrors { get { return _cardMaxErrors; } }
        public static int TokenAlmostEmpty { get { return _tokenAlmostEmpty; } }
        public static int NumberOfWrongToken { get { return _numberOfWrongToken; } }
        public static int AgentPinEntryTimeOut { get { return _agentPinEntryTimeOut; } }
        public static int TransactionTimeOut { get { return _transactionTimeOut; } }

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("TVMEquipmentParameters"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "TVMEquipmentParameters");
        }

        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _equipmentParametersMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _equipmentParametersMissing.SetAlarm(false);

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;

                _nbTransactionsDisplay = Convert.ToInt32(root.SelectSingleNode("NbTransactionsDisplay").InnerText);
                _nbAlarmsDisplay = Convert.ToInt32(root.SelectSingleNode("NbAlarmsDisplay").InnerText);
                _authenticationPeriod = Convert.ToInt32(root.SelectSingleNode("AuthenticationPeriod").InnerText);
                _transactionSendTime = Convert.ToInt32(root.SelectSingleNode("TransactionSendTime").InnerText);
                _auditSendTime = Convert.ToInt32(root.SelectSingleNode("AuditSendTime").InnerText);
                _eventLogSendTime = Convert.ToInt32(root.SelectSingleNode("EventLogSendTime").InnerText);
                _minBankCardPayment = Convert.ToInt32(root.SelectSingleNode("MinBankCardPayment").InnerText);
                _maxCashForPayment = Convert.ToInt32(root.SelectSingleNode("MaxCashForPayment").InnerText);
                _maxCoinForChange = Convert.ToInt32(root.SelectSingleNode("MaxCoinForChange").InnerText);
                _coinBoxAlmostFull = Convert.ToInt32(root.SelectSingleNode("CoinBoxAlmostFull").InnerText);
                _noteBoxAlmostFull = Convert.ToInt32(root.SelectSingleNode("NoteBoxAlmostFull").InnerText);
                _coinBoxFull = Convert.ToInt32(root.SelectSingleNode("CoinBoxFull").InnerText);
                _noteBoxFull = Convert.ToInt32(root.SelectSingleNode("NoteBoxFull").InnerText);
                _cardMaxErrors = Convert.ToInt32(root.SelectSingleNode("CardMaxErrors").InnerText);
                _tokenAlmostEmpty = Convert.ToInt32(root.SelectSingleNode("TokenAlmostEmpty").InnerText);
                _numberOfWrongToken = Convert.ToInt32(root.SelectSingleNode("NumberOfWrongToken").InnerText);
                _agentPinEntryTimeOut = Convert.ToInt32(root.SelectSingleNode("AgentPinEntryTimeOut").InnerText);
                _transactionTimeOut = Convert.ToInt32(root.SelectSingleNode("TransactionTimeOut").InnerText);
                _equipmentParametersError.SetAlarm(false);
                Communication.SendMessage("Parameters", "Answer", "EquipmentParams", "TVMEquipmentParameters",content);
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "OverallParameters_LoadVersion " + e.Message);
                _equipmentParametersError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }
    }
}
