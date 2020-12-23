using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;

#if PocketPC
using OpenNETCF.Threading;
using System.Threading;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Xml;
using System.IO;
using System.Diagnostics;
#endif
using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{
    #region "class MainTicketingRules"
    public partial class MainTicketingRules : TaskThread , iRemoteMessageInterface
    {

        private LogicalMedia _logMediaReloader = null;
        private LogicalMedia _logMediaToken = null;

        private Boolean _IsReaderLoaded = false;
        private int _ReaderType;
        
        private static SmartFunctions.MediaDetected _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
        TTErrorTypes _errorCurMedia;
        AdjustmentInfo _adjustment;

        public DelhiDesfireEV0 hwCsc;

        public DelhiTokenUltralight hwToken;

        private ReaderComm _ReaderComm;
        private FirmwareInfo _FirmwareInfo;

        public Semaphore semStopAsked;

        public static OneEvent _ticketKeysMissing = null;
        public static OneEvent _ticketKeysError = null;
        public static OneEvent _ticketKeysMetaStatus = null;
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

        public static OneEvent _cscReloaderFailure = null, _cscReloaderFailure2 = null;
        public static OneEvent _cscReloaderIsOffLine = null, _cscReloaderIsOffLine2 = null;
        public static OneEvent _cscReloaderMetaStatus = null;
        private static OneEvent _readerSerialNumber = null;
        private static OneEvent _firmwareVersion = null;
        private static OneEvent _cscAPIVersion = null;
        private static OneEvent _cscChargeurVersion = null;

        // CCHS SAM Status
        private static OneEvent _cchsSAMType = null;

        public static OneEvent _dataSecurityModuleLocked = null;
        public static OneEvent _dataSecurityModuleFailure = null;
        public static OneEvent _dataSecurityModuleAusent = null;
        public static OneEvent _dataSecurityModuleIsOffLine = null;
        public static OneEvent _dataSecurityModuleMetaStatus = null;
        private static OneEvent _dataSecurityModuleSerialNumber = null;
        private static OneEvent _dataSecurityModuleFirmwareVersion = null;
        private static OneEvent _dataSecurityModuleDeviceNumber = null;

        private int _frequencyToAttemptReloadReaderInDisconnection;

        private XmlWriterSettings contextFile_XMLWriterSettings;

        private bool _treatTicketingKeyFile = true;
        public readonly bool _cryptoflexSAMUsage = true;
        public readonly bool _delhiCCHSSAMUsage = false;
        public readonly bool _signatureAtEachTransaction = false;
        private bool _generateXdrInTransaction = false;
        private bool _readEquipmentNumberInFile = false;

        private readonly bool _bTokenDispenseFunctionality;
        private cCCHSSAMInfo mcCCHSSAMInfo = new cCCHSSAMInfo();
       
        private MediaDetectionTreatment _readDataFor = MediaDetectionTreatment.BasicAnalysis_AVM_TVM;
        private int? _readDataForAddVal_RechargeValueRequested = null;

        SmartFunctions inst;
        static bool _bLastMediaDetectedUsingCallback = false;
        
        public enum Timers
        {
            PutTokenUnderRW,
            ThrowToken,
            TimeoutToGiveChanceForLastOpWTEToComplete,
            TimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache,
            TimeoutCancelPutTokenUnderRWAckNotRecvd,
            NoTokenDetectedPost_Positive_PutTokenUnderRWAck,
            TimeoutNoMediaDetectedPostLastMediaWasHalted,
            TimoutMediaStillMaybeInFieldPostRTEOrWTE
        }

        const int FareMode_Incident = 3;
        private RegisterEquipmentStatus _eqptStatus = null;
        
        public LogicalMedia GetLogicalDataOfMediaAtFront()
        {
            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
                return _logMediaReloader;
            else if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
                return _logMediaToken;
            else
            {
                Debug.Assert(false);
                return null;
            }
        }

        #region " Constructor MainTicketingRules"
        public MainTicketingRules()
            : base("MainTicketingRules")
        {

            try
            {
                Logging.Log(LogLevel.Information, Configuration.DumpAllConfiguration());
                Logging.Log(LogLevel.Information, IFS2.Equipment.Common.Parameters.DumpAllConfiguration());

                _bTokenDispenseFunctionality = (bool)Configuration.ReadParameter("TokenDispenseFunctionality", "bool", "false");
                SharedData.EquipmentType = (EquipmentFamily)Configuration.ReadParameter("EquipmentType", "enum", "99:Unknown");
                if (SharedData.EquipmentType == EquipmentFamily.Unknown)
                {
                    Logging.Log(LogLevel.Critical, "EquipmentType not specified. Can't launch application");
                    return;


                }

                inst = SmartFunctions.Instance;

                string MMIChannel = "MMIChannel";
                string CoreChannel = "CoreChannel";

                SendMsg.SetThreadName(ThreadName, this);

                semStopAsked = new Semaphore(0, 10000);

                _treatTicketingKeyFile = (bool)Configuration.ReadParameter("TreatTicketingKeyFile", "bool", "true");
                _cryptoflexSAMUsage = (bool)Configuration.ReadParameter("CryptoflexSAMUsage", "bool", "true");
                _delhiCCHSSAMUsage = (bool)Configuration.ReadParameter("DelhiCCHSSAMUsage", "bool", "false");
                _signatureAtEachTransaction = (bool)Configuration.ReadParameter("SignatureAtEachTransaction", "bool", "false");
                _generateXdrInTransaction = (bool)Configuration.ReadParameter("GenerateXdrInTransaction", "bool", "false");
                _readEquipmentNumberInFile = (bool)Configuration.ReadParameter("ReadEquipmentNumberInFile", "bool", "false");

                contextFile_XMLWriterSettings = new XmlWriterSettings();
                contextFile_XMLWriterSettings.Indent = false;
                contextFile_XMLWriterSettings.OmitXmlDeclaration = true;

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

            
                 //Initialisation of all the tags managed by the TT
                _ticketKeysMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 1, "TicketKeysMissing", "OCKMIS", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _ticketKeysError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 2, "TicketKeysError", "OCKDEC", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _ticketKeysMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 3, "TicketKeysMetaStatus", "OCKMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                _ticketKeysMetaStatus.SetMetaStatusLinkage("", "TicketKeysMissing;TicketKeysError");

                _fpStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 4, "FpStatus", "OFPMIS", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _fpError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 5, "FpError", "OFPDEC", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _fpMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 6, "FpMetaStatus", "OFPMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                _fpMetaStatus.SetMetaStatusLinkage("", "FpStatus;FpError");

                _prgStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 7, "PrgStatus", "PROMIS", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _prgError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 8, "PrgError", "PRODEC", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _prgMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 9, "PrgMetaStatus", "PROMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                _prgMetaStatus.SetMetaStatusLinkage("", "PrgStatus;PrgError");

                _parametersMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 34, "ParametersMissing", "PARMIS", AlarmStatus.Alarm, OneEvent.OneEventType.MetaAlarm);
                _parametersMissing.SetMetaAlarmLinkage("AgentListMissing;MediaDenyListMissing;RangeDenyListMissing;EquipmentDenyListMissing;TopologyMissing;FaresMissing;OverallMissing;EquipmentParametersMissing");

                _parametersError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 35, "ParametersError", "PARDEC", AlarmStatus.Alarm, OneEvent.OneEventType.MetaAlarm);
                _parametersError.SetMetaAlarmLinkage("AgentListError;MediaDenyListError;RangeDenyListError;EquipmentDenyListError;TopologyError;FaresError;OverallError;EquipmentParametersError");

                _parametersActivationError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 37, "ParametersActivationError", "EODFAI", AlarmStatus.Alarm, OneEvent.OneEventType.MetaAlarm);
                _parametersActivationError.SetMetaAlarmLinkage("AgentListActivationError;MediaDenyListActivationError;RangeDenyListActivationError;EquipmentDenyListActivationError;TopologyActivationError;FaresActivationError;OverallActivationError;EquipmentParametersActivationError");

                _parametersMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 36, "ParametersMetaStatus", "PARMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                _parametersMetaStatus.SetMetaStatusLinkage("", "ParametersMissing;ParametersError;ParametersActivationError");

                _globalMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 38, "EODMetaStatus", "METEOD", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                _globalMetaStatus.SetMetaStatusLinkage("", "ParametersMissing;ParametersError;ParametersActivationError;TicketKeysMissing;TicketKeysError;FpStatus;FpError;PrgStatus;PrgError");


                //Tags for CSC Reloader Management                
                _cscReloaderIsOffLine = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 1, "IsOffLine", "CS1COM", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _cscReloaderFailure = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 2, "OutOfOrder", "CS1STA", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _cscReloaderIsOffLine2 = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 3, "IsOffLine1", "CSCCOM", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _cscReloaderFailure2 = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 4, "OutOfOrder1", "CSCSTA", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);

                _cscReloaderMetaStatus = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 5, "MetaStatus", "METCSC", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus); ;
                _cscReloaderMetaStatus.SetMetaStatusLinkage("IsOffLine", "OutOfOrder");
                _readerSerialNumber = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 60, "SerialNumber", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _firmwareVersion = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 61, "FirmwareVersion", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _readerSerialNumber.SetEvent("0");
                _firmwareVersion.SetEvent("1.0");

                _cscAPIVersion = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 64, "CSCAPIVersion", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _cscChargeurVersion = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 63, "CSCChargeurVersion", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _cscAPIVersion.SetEvent("0");
                _cscChargeurVersion.SetEvent("0.0");

                
                _dataSecurityModuleFailure = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 1, "SAMError", "SMSTA", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm); ;
                _dataSecurityModuleLocked = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 2, "Blocked", "SMLOCK", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm); ;
                _dataSecurityModuleMetaStatus = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 50, "MetaStatus", "METDSM", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus); ;
                _dataSecurityModuleIsOffLine = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 3, "IsOffLine", "", AlarmStatus.Alarm, OneEvent.OneEventType.StorageAlarm); ;
                _dataSecurityModuleAusent = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 4, "SAMAusent", "", AlarmStatus.Alarm, OneEvent.OneEventType.StorageAlarm); ;
                _dataSecurityModuleSerialNumber = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 60, "SerialNumber", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _dataSecurityModuleFirmwareVersion = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 61, "FirmwareVersion", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _dataSecurityModuleDeviceNumber = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 62, "DeviceNumber", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                _dataSecurityModuleMetaStatus.SetMetaStatusLinkage("IsOffLine", "SAMError;Blocked;SAMAusent");
                _dataSecurityModuleSerialNumber.SetEvent("0");
                _dataSecurityModuleFirmwareVersion.SetEvent("1.0");
                if (_delhiCCHSSAMUsage)
                {
                    //_cchsSAMType _cchsSAMAppliVersion
                    _cchsSAMType = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 65, "CCHSSAMType", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                    //_cchsSAMAppliVersion = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 66, "CCHSAppVersion", "", AlarmStatus.Normal, OneEvent.OneEventType.StorageValue);
                    _cchsSAMType.SetEvent("I-SAM");
                    //_cchsSAMAppliVersion.SetEvent("0.0");
                }
                 SharedData.ReadContextFile();

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

                //Commands from MMI
                Communication.AddEventsToReceive(ThreadName, "GetTokenPrice;GetTokenPriceForFreeOrPaidExit;ReadUserCardSummary;ReadMediaAgain;ManualPaidAtEntryBus", this);
                Communication.AddEventsToReceive(ThreadName, "ReloadTPurseOnCard;ReadUserCardGlobalData;ReadUserCardTransactionHistory;VerifyAgentData;PaymentWithTPurse;PaymentDeduction", this);
                Communication.AddEventsToReceive(ThreadName, "DetailsOfMediaToBeRead", this);
                Communication.AddEventsToReceive(ThreadName, "Shutdown", this);
                Communication.AddEventsToReceive(ThreadName, "GetCertificate;SendEquipmentMode", this);
                Communication.AddEventsToReceive(ThreadName, "GetTokenBinData;GetAllEvents;Initialisation;ResetCSCReloader;EquipmentLocation", this);

                // todo: if ever DetectForContainerToken see light of day, replace it with DetectMedia
                Communication.AddEventsToReceive(ThreadName, "DetectForContainerToken;UpdateMedia;UpdateMediaCancelOp", this);                

                //Commands from SC TVM Interface
                Communication.AddEventToReceive(ThreadName, "GetCertificateOfEqpt", this);
                Communication.AddEventToReceive(ThreadName, "GetCertificateOfCA", this);
//                Communication.AddEventToReceive(ThreadName, "GetResponseToChallenge", this);
                Communication.AddEventToReceive(ThreadName, "EncryptUsingPrivateKey", this);
                Communication.AddEventToReceive(ThreadName, "GetMachineId", this);
                Communication.AddEventToReceive(ThreadName, "GetCSCReloaderStatus", this);
                Communication.AddEventToReceive(ThreadName, "GetSAMStatus", this);
                Communication.AddEventToReceive(ThreadName, "GetSoftwareVersion", this);
                Communication.AddEventToReceive(ThreadName, "StopApplication", this);
                Communication.AddEventToReceive(ThreadName, "ActivateNewParameterFile", this);
                Communication.AddEventToReceive(ThreadName, "NewTicketingKeysFile", this);

                Communication.AddEventsToReceive(ThreadName, "SetLogLevel;GetEODMetaStatus", this);
                //Messages for MMI
                Communication.AddEventsToExternal("GetTokenPriceAnswer;ReadUserCardSummaryAnswer;CSCMediaDetection;CSTMediaDetection;GetInitialisationParams;MediaRemoved", MMIChannel);
                Communication.AddEventsToExternal("ReloadTPurseOnCardAnswer;ReadUserCardGlobalDataAnswer;ReadUserCardTransactionHistoryAnswer;VerifyAgentDataAnswer", MMIChannel);
                Communication.AddEventsToExternal("BadPassengerCardDetection;BadAgentCardDetection;AgentCardDetection;UpdateCardStatus;EquipmentParams", MMIChannel);

                Communication.AddEventsToExternal("GetTokenBinDataAnswer;GetAllEventsAnswer;EquipmentParams;GetMachineIdAnswer;GetSAMStatusAnswer", CoreChannel);

                Communication.AddEventsToExternal("GetCSCReloaderStatusAnswer;GetSoftwareVersionAnswer;StopApplicationAnswer;GetMachineIdAnswer", MMIChannel);
                
                Communication.AddEventsToExternal("GetSAMStatusAnswer;CSCReloaderMetaStatus;TicketingKeyMetaStatus;EODMetaStatus", MMIChannel);

                Communication.AddEventsToExternal("TopologyParametersUpdated;FareParametersUpdated", MMIChannel);

                Communication.AddEventsToExternal("UpdateMediaResult;UpdateMediaCancelOpAck;UpdateMediaTerminating;UpdateMediaNoMedia", MMIChannel);

                //Alarms for CoreChannel
                Communication.AddEventsToExternal("CSCReloaderMetaStatus;StoreAlarm;GetCSCReloaderStatusAnswer;ActivateNewParameterFileAnswer", CoreChannel);

                //For EOD Messages
                Communication.AddEventsToReceive(ThreadName, "GetListLines;GetListStations;GetListProducts;GetListPenalties;GetListAdditionalProducts;GetParkingDurationPrice;GetListParkingProducts", this);
                Communication.AddEventsToReceive(ThreadName, "GetListParkingLines;GetListParkingStations;GetListBusLines;GetListBusStations;GetSingleTicketPrice;GetListPricesOnRestOfLine", this);
                Communication.AddEventsToExternal("GetListLinesAnswer;GetListStationsAnswer;GetListProductsAnswer;GetListPenaltiesAnswer;GetListAdditionalProductsAnswer;GetListParkingProductsAnswer", MMIChannel);
                Communication.AddEventsToExternal("GetParkingDurationPriceAnswer;GetListParkingLinesAnswer;GetListParkingStationsAnswer;GetListBusLinesAnswer;GetListBusStationsAnswer", MMIChannel);
                Communication.AddEventsToExternal("GetSingleTicketPriceAnswer;GetListPricesOnRestOfLineAnswer", MMIChannel);

                IFSEventsList.InitContextFile("TTComponent");
                IFSEventsList.InitContextFile("CSCReloaderDriver");
                IFSEventsList.InitContextFile("DSMDriver");
                try
                {
                    _frequencyToAttemptReloadReaderInDisconnection = (int)Configuration.ReadParameter("FrequencyToAttemptReloadReaderInDisconnection", "int", "60");
                }
                catch { }
                Logging.Log(LogLevel.Information, ThreadName + "_Started");
            }//try
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, ThreadName + "_Constructor Error :" + e.Message);
                throw (e);
            }//
        }//public MainTicketingRules()
        #endregion "Constructor MainTicketingRules"

        //JL : I don't know if they are used But I need to add because it is not compiling else
        public void StartPolling(Scenario scenario)
        {
            // this is not required
            //MainTicketingRules._tsPriorToWhenAsynchMessagesOfMediaProducedOrRemovedHasToBeIgnored = DateTime.Now;
            //SmartFunctions.Instance.StartPolling(scenario, SmartFunctions.Instance.listenerCardProduced);
            //_MediaDetectedState = SmartFunctions.MediaDetected.NONE;
        }
        public void RestartField()
        {
            StopField();
            SmartFunctions.Instance.StartField();
        }

        #region "TreatMessageReceived"
        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            try
            {
                Logging.Log(LogLevel.Verbose, "Main TT : Message received " + eventMessage.EventID + " " + eventMessage.Attribute + " " + eventMessage.Message);
                if (TreatParametersMessageReceived(eventMessage)) return 0;
                if (TreatTokenMessageReceived(eventMessage)) return 0;
                if (TreatCSCMessageReceived(eventMessage))
                {
                    //SKS: Added on 09-10-2014
                    //_logMediaReloader.Reset();
                    //  hwCsc.Reset();
                    return 0;
                }
                switch (eventMessage.EventID.ToUpper())
                {
                    case "STOPAPPLICATION":
                        Communication.SendMessage(ThreadName, "Answer", "StopApplicationAnswer", "TTApplication", "0");
                        semStopAsked.Release();
                        return (0);
                    case "SENDEQUIPMENTMODE":
                        //This message permits to indicate if we are in maintenance mode.
                        SharedData.EquipmentStatus = (EquipmentStatus)Convert.ToInt32(eventMessage.Attribute);
                        return 0;
                    case "GETMACHINEID":

                        try
                        {
                            uint result = 0;                           
                            if (_readEquipmentNumberInFile)
                            {
                                //try
                                //{
                                //    //Equipment Number is in MMI Context at the moment. It was not an Error
                                //    StreamReader sr = new StreamReader(Disk.BaseDataDirectory + "\\Context\\ContextFile_TT.txt");
                                //    string s1 = sr.ReadToEnd();
                                //    sr.Close();
                                //    sr = null;
                                //    int i = Utility.SearchSimpleCompleteTagInt(s1, "EquipmentNumber");
                                //    //int j = Utility.SearchSimpleCompleteTagInt(s1, "EquipmentFamily");
                                //    //result = (uint)(j * 65536 + i);
                                //    //shailendra: may be we need to save above info in TT context too ????
                                //    result = (uint)i;
                                //}
                                //catch (Exception e)
                                //{
                                //    Logging.Log(LogLevel.Error, "SharedData: Saving Context File Error " + e.Message);
                                //    Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", "0", Convert.ToString((int)TTErrorTypes.Exception));    // As per specification if TT does not get proper machine ID then it should send error code in message else message value =0                                
                                //    return 0;
                                //}
                                try
                                {
                                    string s = FileUtility.ReadContext("DeviceID");
                                    s = Utility.SearchSimpleCompleteTag(s, "Data");
                                    s = Crypto.DecryptString2(s, "LeonettiJeanIFS2.Ticketing_113*_");
                                    result = Convert.ToUInt32(s);
                                    SharedData.EquipmentNumber = (int)result;
                                    SharedData.EquipmentType = EquipmentFamily.HHD;
                                    SharedData.SaveContextFile();
                                }
                                catch (Exception e)
                                {
                                    Logging.Log(LogLevel.Error, "SharedData: Saving Context File Error " + e.Message);
                                    Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", "0", Convert.ToString((int)TTErrorTypes.Exception));    // As per specification if TT does not get proper machine ID then it should send error code in message else message value =0                                
                                    return 0;
                                }

                            }
                            else if (_cryptoflexSAMUsage)
                            {
                               // result = (uint)cFlex.GetEQPLocalId(DEST_TYPE.DEST_SAM1);
                            }
                            if (result > 0)
                                Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", result.ToString(), "1");// As TVM SC interface is treating message=1 as good so changing it to 1 form 0 
                            else Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", "0", "0");
                        }
                        catch (Exception e1)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", "0", Convert.ToString((int)TTErrorTypes.Exception));
                            Logging.Log(LogLevel.Error, ThreadName + "GetMachineIdAnswer Error :" + e1.Message);
                            return (0);
                        }

                        return (0);
                    case "INITIALISATION":
                        SharedData.Initialise(eventMessage.Attribute);
                        SharedData.SaveContextFile();
                        return 0;
                    case "EQUIPMENTLOCATION":
                        EquipmentLocation eqploc = SerializeHelper<EquipmentLocation>.XMLDeserialize(eventMessage.Attribute);
                        SharedData.LineNumber = eqploc.LineNumber;
                        SharedData.StationNumber = eqploc.StationNumber;
                        SharedData.ServiceProvider = (short)eqploc.ServiceProvider;
                        SharedData.SaveContextFile();
                        return 0;
                    case "GETCSCRELOADERSTATUS":
                        try
                        {
                            //Do we need to reset  the CSC. JL : I don't think so. So display Status of CSC Reader as it is.
                            //If nothing in maintenance screen shall use Reset button.
                            //if (ReloadReader())
                            //{
                            //    _IsReaderOk = true;
                            //}
                            if (!_IsReaderLoaded) ReloadReader();
                            Communication.SendMessage(ThreadName, "Answer", "GetCSCReloaderStatusAnswer", ConstructCscStatus(), Convert.ToString((int)_cscReloaderMetaStatus.Value));
                        }
                        catch (Exception)
                        {
                            _cscReloaderFailure.SetAlarm(true);
                            _cscReloaderFailure2.SetAlarm(true);
                            _cscReloaderMetaStatus.UpdateMetaStatus();
                            IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                            SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.AlarmLevel), ConstructCscStatus());
                        }
                        return 0;
                    case "RESETCSCRELOADER":
                        try
                        {
                            //First if reader not already loaded. Make a standard Reload
                            Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived RESETCSCRELOADER IsReaderLoaded :" + _IsReaderLoaded.ToString());
                            if (eventMessage.Attribute == "1")
                            {
                                // force reload...
                                _IsReaderLoaded = false;
                            }
                            if (!_IsReaderLoaded) ReloadReader();
                            else
                            {
                                Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived RESETCSCRELOADER Nothing Reset");
                                //Try to make reset ùmore quick as possible. To call minimum commands
                                //ResetAlreadyStartedReader(); This take too much time
                                //SmartFunctions.Instance.Init();
                            }
                           // Communication.SendMessage(ThreadName, "Answer", "GetCSCReloaderStatusAnswer", ConstructCscStatus(), Convert.ToString((int)_cscReloaderMetaStatus.Value));
                        }
                        catch (Exception e)
                        {
                            Logging.Log(LogLevel.Error, "TTMain.TreatMessageReceived Cannot Reset CSC Reader :" + e.Message);
                        }
                        return 0;
                    case "GETSAMSTATUS":
                       /*
                        CertData CACertificate = new CertData();  
                        CertData EQCertificate = new CertData();

                        try
                        {
                            //Remove ResetReader. Resetting reader is made on the reset command or if it has not ben initialised
                            if (!_IsReaderLoaded) ReloadReader();
                            if (_IsReaderLoaded)
                            {
                                if (_delhiCCHSSAMUsage)
                                {
                                    if (mCCHSSAMMgr != null)
                                    {
                                        _dataSecurityModuleDeviceNumber.SetValue(SharedData.EquipmentNumber);
                                        _dataSecurityModuleSerialNumber.SetEvent(mCCHSSAMMgr.DSMId.ToString());

                                        //_cchsSAMType 
                                        _cchsSAMType.SetEvent(mCCHSSAMMgr.mCCHSStatusInfo.SAMType.ToString());
                                        _dataSecurityModuleFirmwareVersion.SetEvent(mCCHSSAMMgr.mCCHSStatusInfo.SAMAppVersion.ToString());
                                    }
                                }
                                else
                                {
                                    bool b = cFlex.IsSAMBlocked(DEST_TYPE.DEST_SAM1);
                                    _dataSecurityModuleLocked.SetAlarm(b);

                                    cFlex.GetDataFromCert(cFlex.GetCertificate(DEST_TYPE.DEST_SAM1, CERT_TYPE.CA_CERT), out CACertificate);

                                    cFlex.GetDataFromCert(cFlex.GetCertificate(DEST_TYPE.DEST_SAM1, CERT_TYPE.LOCAL_CERT), out EQCertificate);
                                    _dataSecurityModuleDeviceNumber.SetValue(cFlex.GetEQPLocalId(DEST_TYPE.DEST_SAM1));
                                    _dataSecurityModuleSerialNumber.SetEvent(cFlex.GetSAMSerialNbr(DEST_TYPE.DEST_SAM1).ToString());
                                }
                                _dataSecurityModuleFailure.SetAlarm(false);
                                _dataSecurityModuleMetaStatus.UpdateMetaStatus();
                                IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");
                                string s2 = ConstructSAMStatus(CACertificate, EQCertificate);
                                Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", s2, "");
                            }
                            else
                            {
                                _dataSecurityModuleFailure.SetAlarm(true);
                                _dataSecurityModuleMetaStatus.UpdateMetaStatus();
                                IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");
                                Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), "");
                                Logging.Log(LogLevel.Error, ThreadName + "GetSAMStatusAnswer : Error reloading reader ");
                            }
                            return 0;

                        }
                        catch (Exception e1)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), "");
                            Logging.Log(LogLevel.Error, ThreadName + "GetSAMStatusAnswer Error :" + e1.Message);
                            return (0);
                        }*/
                        try
                        {
                         // mcCCHSSAMInfo = Reader.GetSAMInfo();
                         // Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), "");
                            if (!_IsReaderLoaded) ReloadReader();
                            if (_IsReaderLoaded)
                            {
                                if (_delhiCCHSSAMUsage)
                                {
                                    mcCCHSSAMInfo = Reader.GetSAMInfo();
                                    int dsmId = Reader.GetSAMId();

                                    _dataSecurityModuleDeviceNumber.SetValue(SharedData.EquipmentNumber);
                                    _dataSecurityModuleSerialNumber.SetEvent(dsmId.ToString());

                                    //_cchsSAMType 
                                    _cchsSAMType.SetEvent(mcCCHSSAMInfo.SAMType.ToString());
                                    _dataSecurityModuleFirmwareVersion.SetEvent(mcCCHSSAMInfo.SAMAppVersion.ToString());

                                    Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), "");
                                }
                            }
                        }
                        catch (Exception e1)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), "");
                            Logging.Log(LogLevel.Error, ThreadName + "GetSAMStatusAnswer Error :" + e1.Message);
                            return (0);
                        }
                        return 0;
                    case "GETALLEVENTS":
                        string K = IFSEventsList.GetStringForCC("TTComponent");
                        K += IFSEventsList.GetStringForCC("CSCReloaderDriver");
                        K += IFSEventsList.GetStringForCC("DSMDriver");

                        SendCommand("GetAllEventsAnswer", "TTApplication", K);
                        return 0;

                    case "SETLOGLEVEL":
                        {
                            if (eventMessage.Attribute == "TT")
                            {
                                SetMyLogLevel.Set(Utility.ParseEnum<LogLevel>(eventMessage.Message));
                            }
                            break;
                        }
                    case "DELAYN":
                        {
                            HandleDelay(eventMessage);
                            break;
                        }
                    case "READMEDIAAGAIN":
                        Handle_ReadMediaAgain(eventMessage);
                        break;
                    case "DETAILSOFMEDIATOBEREAD":
                        Handle_DetailsOfMediaToBeRead(eventMessage);
                        break;
                    case "AGENTLOGGEDIN":
                        Handle_AgentLoggedIn(eventMessage);
                        break;
                    case "CANCELPUTTOKENUNDERRWACK":
#if !_HHD_
                        Handle_CancelPutTokenUnderRWAck();
#endif
                        break;
                    case "SHUTDOWN":
                        Communication.RemoveAllEvents(ThreadName);
                        return (-1); //To terminate the process
                }//switch
                return base.TreatMessageReceived(eventMessage);
            }//try
            catch (Exception)
            {
                //SKS: added on 09-10-2014
                _logMediaReloader.Reset();
                _logMediaToken.Reset();
                hwCsc.Reset();
                return base.TreatMessageReceived(eventMessage);

            }
        }

        internal void SetLastMediaDetectedWithoutCallback()
        {
            throw new NotImplementedException();
        }

        internal void SetWaitingForCancelPutTokenUnderRWAck()
        {
            throw new NotImplementedException();
        }

        private void HandleDelay(EventMessage eventMessage)
        {
            var sTimerId = eventMessage._par[0];
            int nTimerId;
            try
            {
                nTimerId = Convert.ToInt32(sTimerId);
            }
            catch
            {
                return;
            }
            var timer = (Timers)nTimerId;
      /*      switch (timer)
            {
                case Timers.PutTokenUnderRW:
                    {
                        Debug.Assert(false); // Token dispenser should have responded yet?? hence assert.
                        SendMsg.TokenError();
                        _curTokenTransaction = null;

                        TokenTxn_RestorePollingAtFront_TVM();
                        break;
                    }
                case Timers.ThrowToken:
                    {
                        Debug.Assert(false); // Token dispenser should have responded yet?? hence assert.
                        SendMsg.TokenError();
                        _curTokenTransaction = null;

                        TokenTxn_RestorePollingAtFront_TVM();

                        break;
                    }
                case Timers.TimeoutToGiveChanceForLastOpWTEToComplete:
                    {
                        _mediaUpdate = null;
                        SendMsg.UpdateMedia_Terminating();
                        break;
                    }
                case Timers.TimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache:
                    {
                        Console.WriteLine("TimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache ticked");
                        _bTimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache_Ticking = false;
                        if (_curTokenTransaction == null)
                        {
                            Console.WriteLine("_UselessmediasThatMayBeInRFField cleared");
                            _UselessmediasThatMayBeInRFField.Clear();
                        }
                        break;
                    }
            }*/
        }

        #endregion "TreatMessageReceived"

        private bool _bReadDataForChanged = false;
        private void Handle_DetailsOfMediaToBeRead(EventMessage eventMessage)
        {
            MediaDetectionTreatment readDataFor = (MediaDetectionTreatment)(Convert.ToInt32(eventMessage._par[0]));
            if (_readDataFor != readDataFor)
            {
                _readDataFor = readDataFor;
                _bReadDataForChanged = true;
                RepetitiveTreatment();
            }
        }
        public void ClearDataStructuresForMediaKeptOnRW()
        {
            // we deliberatly don't reset _mediaDetectedState.
            _logMediaToken.Reset();
            _logMediaReloader.Reset();
            hwCsc.Reset();
            hwToken.Reset();
            _errorCurMedia = TTErrorTypes.NoError;
            _adjustment = null;
            _bTicketDataStructuresSynchedWithTicketData = false;
        }

        private void Handle_AgentLoggedIn(EventMessage eventMessage)
        {
            if (eventMessage._par.Length == 1)
            {
                if (eventMessage._par[0] == "-1")
                    SharedData._agentShift = null;
            }
            int shiftId = Convert.ToInt32(eventMessage._par[0]);
            int agentId = Convert.ToInt32(eventMessage._par[1]);

         //   SharedData._agentShift = new AgentShift(shiftId, agentId);
        }

        private void Handle_ReadMediaAgain(EventMessage eventMessage)
        {
            // TODO: Its a crude implementation. May be want to revise it later.
          //  SmartFunctions.Instance.StopField();
            //SmartFunctions.Instance.StartField();
            //TODO: SKS to be added as per new reader...
            _logMediaReloader.Reset();
            _logMediaToken.Reset();
            hwCsc.Reset();
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;

            RepetitiveTreatment();
        }
        private string ConstructCscStatus()
        {
            try
            {
                //Construct the XML string
                var sb = new StringBuilder();
                XmlWriter xmlWriter = XmlWriter.Create(sb, contextFile_XMLWriterSettings);
                xmlWriter.WriteStartElement("CSCReloader");
                // IFSEventsList.ComposeXMLForStatus(xmlWriter, "CSCReloaderDriver", "MetaStatus", "IsOffLine;OutOfOrder", "SerialNumber", "FirmwareVersion");
                IFSEventsList.ComposeXMLForStatus(xmlWriter, "CSCReloaderDriver", "MetaStatus", "IsOffLine;OutOfOrder", "SerialNumber;CSCAPIVersion;FirmwareVersion;CSCChargeurVersion");
                xmlWriter.WriteEndElement(); //UPS
                xmlWriter.Close();
                return sb.ToString();
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TTMain.ConstructCscStatus " + e.Message);
                return "";
            }
        }
        private string ConstructSAMStatus(CertData caCertificate, CertData eqCertificate)
        {
            try
            {
                //Construct the XML string
                var sb = new StringBuilder();
                XmlWriter xmlWriter = XmlWriter.Create(sb, contextFile_XMLWriterSettings);
                xmlWriter.WriteStartElement("SAM");
                IFSEventsList.ComposeXMLForStatus(xmlWriter, "DSMDriver", "MetaStatus", "IsOffLine;SAMError;Blocked;SAMAusent", "SerialNumber", "FirmwareVersion");
                xmlWriter.WriteElementString("DeviceNumber", Convert.ToString(_dataSecurityModuleDeviceNumber.Value));
                xmlWriter.WriteStartElement("Data");
                if (_delhiCCHSSAMUsage)
                {
                    //TODO: add info for CCHS SAM here
                }
                else
                {
                    xmlWriter.WriteElementString("CACertificateSubject", caCertificate.Subject);
                    xmlWriter.WriteElementString("CACertificateStartOfValidity", caCertificate.NotBefore);
                    xmlWriter.WriteElementString("CACertificateEndOfValidity", caCertificate.NotAfter);
                    xmlWriter.WriteElementString("EquipmentCertificateSubject", eqCertificate.Subject);
                    xmlWriter.WriteElementString("EquipmentCertificateStartOfValidity", eqCertificate.NotBefore);
                    xmlWriter.WriteElementString("EquipmentCertificateEndOfValidity", eqCertificate.NotAfter);
                }
                xmlWriter.WriteEndElement(); //Data
                xmlWriter.WriteEndElement(); //SAM
                xmlWriter.Close();
                return sb.ToString();

            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TTMain.ConstructCscStatus " + e.Message);
                return "";
            }
        }
        private string ConstructSAMStatusError()
        {
            try
            {
                //Construct the XML string
                var sb = new StringBuilder();
                XmlWriter xmlWriter = XmlWriter.Create(sb, contextFile_XMLWriterSettings);
                xmlWriter.WriteStartElement("SAM");
                IFSEventsList.ComposeXMLForStatus(xmlWriter, "DSMDriver", "MetaStatus", "IsOffLine;SAMError;Blocked;SAMAusent", "SerialNumber", "FirmwareVersion");
                xmlWriter.WriteElementString("DeviceNumber", Convert.ToString(_dataSecurityModuleDeviceNumber.Value));
                xmlWriter.WriteStartElement("Data");
                if (_delhiCCHSSAMUsage)
                {
                    //TODO: add info for CCHS SAM here
                }
                else
                {
                    xmlWriter.WriteElementString("CACertificateSubject", "Error");
                    xmlWriter.WriteElementString("CACertificateStartOfValidity", "Error");
                    xmlWriter.WriteElementString("CACertificateEndOfValidity", "Error");
                    xmlWriter.WriteElementString("EquipmentCertificateSubject", "Error");
                    xmlWriter.WriteElementString("EquipmentCertificateStartOfValidity", "Error");
                    xmlWriter.WriteElementString("EquipmentCertificateEndOfValidity", "Error");
                }
                xmlWriter.WriteEndElement(); //Data
                xmlWriter.WriteEndElement(); //SAM
                xmlWriter.Close();
                return sb.ToString();

            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TTMain.ConstructCscStatus " + e.Message);
                return "";
            }
        }

        public override void RepetitiveTreatment()
        {
            try
            {
                if (_IsReaderLoaded)
                {
                          //ReloadReader();
                          //  return;
                      if (Reader.LastErrorCode != (int)CSC_API_ERROR.ERR_NONE)
                    {
                        switch (Reader.LastErrorCode)
                        {
                            case (short)CSC_API_ERROR.ERR_DEVICE:
                                {
                                    _IsReaderLoaded = false;
                                    break;
                                }
                        }
                        if (!_IsReaderLoaded)
                        {
                        }
                    }

                    if (!Config._bUseCallbackForMediaDetectionNRemoval)
                        MediaRelatedActivity();
                    return;
                }
                else
                {
                    ReloadReader();
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Main TT RepetiveTtreatment " + e.Message);
            }
            finally
            {
                _bReadDataForChanged = false;
                base.RepetitiveTreatment();
            }
        }
        public void GetReaderHandle(out CSC_READER_TYPE readerType, out int hRw)
        {
            readerType = (CSC_READER_TYPE)_ReaderType;
            hRw = 0;
        }
        private void MediaRelatedActivity()
        {
            SmartFunctions.MediaDetected mediaDetected;
            bool bSameMedia;

            var MediaDetectedStateLast = _MediaDetectedState;
            long serialNbrLast = SmartFunctions.Instance.ReadSNbr();

            SmartFunctions.Instance.SmartSyncDetectOk(out mediaDetected, out bSameMedia, false, Scenario.SCENARIO_1);
            // NOTE: bSameMedia would be false even for following sequence
            // a. Produce Card #A
            // b. App calls SmartSyncDetectOk
            // c. User removes card and re-inserts it back
            // d. App calls SmartSyncDetectOk and gets bSameMedia == false
            // In fact, this is exactly what we want


            if ((mediaDetected == SmartFunctions.MediaDetected.NONE && MediaDetectedStateLast != SmartFunctions.MediaDetected.NONE)
                || (mediaDetected != SmartFunctions.MediaDetected.NONE && MediaDetectedStateLast != SmartFunctions.MediaDetected.NONE && serialNbrLast != SmartFunctions.Instance.ReadSNbr()))
            {
                SendMsg.MediaRemoved();
            }

            if (MediaDetectedStateLast != SmartFunctions.MediaDetected.NONE
                 && mediaDetected == SmartFunctions.MediaDetected.NONE)
            {
                _logMediaToken.Reset();
               // if(_logMediaReloader.Purse.History.List.Count>0)_logMediaReloader.Purse.History.List.Clear();//SKS Added on 20160516
                _logMediaReloader.Reset();
                hwCsc.Reset();
                hwToken.Reset();
            }

            if (mediaDetected != SmartFunctions.MediaDetected.NONE)
            {
                #region CommonForAllMedia

                if (bSameMedia == true)
                {
                    if (!_bReadDataForChanged)
                    {
                        Logging.Log(LogLevel.Verbose, "Same media");
                        return;
                    }
                    else
                        SmartFunctions.Instance.SwitchToCardOnState();
                }

                // We're here at this line of code because either of following is true
                // a. Media (New) is introduced afresh
                // b. Media (Same) was there in last cycle, but between two polling cycles, went away and reappeared
                // c. Media (Same) is there, and is asked by client(MMI) to read with different purpose
                // In all cases, we want to read the card again                        

                LogicalMedia logMediaGeneric;
                if (mediaDetected == SmartFunctions.MediaDetected.CARD)
                    logMediaGeneric = _logMediaReloader;
                else
                    logMediaGeneric = _logMediaToken;

                long newSerialNumber = SmartFunctions.Instance.ReadSNbr(); // TODO: Need to verify that it works for tokens, becasue earlier we were taking the Manufacturer block's contents instead of ATR.
                logMediaGeneric.Media.ChipSerialNumberRead = newSerialNumber;

                if (SharedData.EquipmentStatus == EquipmentStatus.Maintenance)
                {
                    string s1 = "<Media><CSN>" + Convert.ToString(newSerialNumber) + "</CSN></Media>";
                    // CSCMediaDetection name is misleading. used for tokens as well.
                    Communication.SendMessage(ThreadName, "Message", "CSCMediaDetection", Convert.ToString((int)TTErrorTypes.NoError), s1);
                }
                #endregion
                #region Card
                if (mediaDetected == SmartFunctions.MediaDetected.CARD)
                {

                    if (Config._bTreatementOnCardDetectionOldStyle == true)
                    {
                        _MediaDetectedState = SmartFunctions.MediaDetected.CARD; // just in case TreatmentOnCardDetection throws exception
#if _RW_TIME_CHECK
                        Logging.Log(LogLevel.Critical, "Start of Media Treatment:-  Ticks:" + Reader.GetTimeStamp().Ticks.ToString() + " Time:" + Reader.GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                        var err = TreatmentOnCardDetection(_logMediaReloader, _readDataFor);
#if _RW_TIME_CHECK
                        Logging.Log(LogLevel.Critical, "End of Media Treatment:-  Ticks:" + Reader.GetTimeStamp().Ticks.ToString() + " Time:" + Reader.GetTimeStamp().ToString("hh.mm.ss.ffffff"));
#endif
                        switch (err)
                        {
                            case TTErrorTypes.NoError:
                                {
                                    _MediaDetectedState = SmartFunctions.MediaDetected.CARD;

                                    SmartFunctions.Instance.SwitchToDetectRemovalState();
                                    break;
                                }
                            case TTErrorTypes.NotDMRCCard:
                            case TTErrorTypes.MediaInDenyList:
                            case TTErrorTypes.MediaEndOfValidityReached:
                            case TTErrorTypes.CardNotIssued:
                            case TTErrorTypes.NoApplicationOnCard:
                            case TTErrorTypes.MediaBlocked:
                            case TTErrorTypes.MediaNotInitialised:
                            case TTErrorTypes.MediaNotSold:
                            case TTErrorTypes.NoProduct:
                            case TTErrorTypes.LastAddValueDeviceBlacklisted:
                            case TTErrorTypes.Exception:
                            case TTErrorTypes.CannotReadTheCard:
                            case TTErrorTypes.BadAgentData:
                            case TTErrorTypes.NotSameCard:
                            case TTErrorTypes.CardCannotBeWritten:
                                {
                                    // TODO: Treatment is debatable, and can be changed to simply SmartFunctions.Instance.SwitchToDetectRemovalState();
                                    /*   HaltCurrentCardNRestartPolling();

                                       _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
                                       
                                       SharedData.MediaTypeDetected = MediaType.None;*/
                                    _MediaDetectedState = SmartFunctions.MediaDetected.CARD;
                                    _logMediaReloader.Reset();
                                    hwCsc.Reset();
                                    SmartFunctions.Instance.SwitchToDetectRemovalState();
                                    break;
                                }
                            case TTErrorTypes.UnknownError:
                            case TTErrorTypes.HardwareTypeNotTreated:
                            case TTErrorTypes.FareTablesError:
                            case TTErrorTypes.OutOfRangeValue:
                            case TTErrorTypes.TransactionNotCommited:
                            case TTErrorTypes.MediaNotPresent:
                            default:
                                {
                                    // they are irrelevant for the current context and hence control flow should never reach here. Still performing the ritual of haltcard and restart polling, just in case arising due to some regression.
                                    Debug.Assert(false);

                                    RestartPolling();

                                    _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
                                    _logMediaReloader.Reset();
                                    hwCsc.Reset();

                                    break;
                                }
                        }
                    }
                    else
                    {
                       // TreatmentOnCardDetection2(_logMediaReloader, _readDataFor, SmartFunctions.MediaDetected.CARD, false);
                        // TODO: One improvement that can be done is that in case of cannot read from card, make repetititive almost immediate.
                        _MediaDetectedState = SmartFunctions.MediaDetected.CARD;
                        SmartFunctions.Instance.SwitchToDetectRemovalState();
                    }
                }
                #endregion
                #region Token
                else if (mediaDetected == SmartFunctions.MediaDetected.TOKEN)
                {
                    if (Config._bTreatementOnCardDetectionOldStyle)
                    {
                        LogicalMedia tmpMedia = new LogicalMedia();
                        if (hwToken.ReadMediaData(tmpMedia, MediaDetectionTreatment.BasicAnalysis_AVM_TVM)) //_logMediaToken
                        {
                            if (tmpMedia.Media.ChipSerialNumber > 0) // new token detected
                            {
                                Logging.Log(LogLevel.Verbose, "@@@@@@@@@@@@@RepetitiveTreatment: A new Token Detected@@@@@@@@@@@@@@@@@@@");
                                _logMediaToken.Reset();
                                _logMediaToken = tmpMedia;

                                _MediaDetectedState = SmartFunctions.MediaDetected.TOKEN;

                                string s = _logMediaToken.ToXMLString();
                                Logging.Log(LogLevel.Verbose, "TOken Data: " + s);
                                if (0 < _logMediaToken.Initialisation.ServiceProviderRead && _logMediaToken.Initialisation.ServiceProviderRead < 4)
                                    Communication.SendMessage(ThreadName, "Message", "CSTMediaDetection", Convert.ToString((int)TTErrorTypes.NoError), s);
                                else
                                    Communication.SendMessage(ThreadName, "Message", "CSTMediaDetection", Convert.ToString((int)TTErrorTypes.MediaNotSold), s);

                                SmartFunctions.Instance.SwitchToDetectRemovalState();
                            }
                            else
                            {
                                Logging.Log(LogLevel.Verbose, "Repetitive treatment:Same Token is there...");

                                RestartPolling();

                                _logMediaToken.Reset();
                                hwToken.Reset();
                            }
                        }
                        else
                        {
                            Logging.Log(LogLevel.Verbose, "Repetitive treatment: Token is not there...");

                            RestartPolling();
                            _logMediaToken.Reset();
                            hwToken.Reset();
                        }
                    }
                    else
                    {
                        //TreatmentOnCardDetection2(_logMediaToken, _readDataFor, SmartFunctions.MediaDetected.TOKEN, false);
                        _MediaDetectedState = SmartFunctions.MediaDetected.TOKEN;
                        SmartFunctions.Instance.SwitchToDetectRemovalState();
                    }
                }
                #endregion
            }
            #region NONEDetected
            else
            {
                // Same info in two variables. Smelling bad.
                _MediaDetectedState = SmartFunctions.MediaDetected.NONE;

          /*      // We are here itself implies that no token transaction (vending) is in process.

                if (_bAtLeastOneMediaHalted)
                {
                    _bAtLeastOneMediaHalted = false;

                    SmartFunctions.Instance.StopField();
                    SmartFunctions.Instance.StartField();
                }

                if (_UselessmediasThatMayBeInRFField.Count > 0
                    && !_bTimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache_Ticking
                    )
                {
                    Console.WriteLine("_UselessmediasThatMayBeInRFField.Count = " + _UselessmediasThatMayBeInRFField.Count.ToString());
                    StartDelay((int)Timers.TimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache, 3000);
                    _bTimeoutSinceLastMediaGotDetected_SoAsToClearUselessMediasCache_Ticking = true;
                }
           */
            }
            #endregion
            return;
        }

        bool bFirstCall = true;
        private Boolean ReloadReader()
        {
            CSC_API_ERROR Err;

            _IsReaderLoaded = false;
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
            _logMediaReloader.Reset();// SKS: Added on 09-10-2014
            _logMediaToken.Reset();// SKS: Added on 09-10-2014
            hwToken.Reset();
            hwCsc.Reset();

            Logging.Log(LogLevel.Verbose, ThreadName + "ReloadReader : Reader Reload request recieved");

            try
            {

                Err = Reader.ReloadReader((CSC_READER_TYPE)_ReaderType, _ReaderComm, out _FirmwareInfo);

                Logging.Log(LogLevel.Verbose, ThreadName + "ReloadReader : Err State is : " + Convert.ToString((int)(CSC_API_ERROR)Err));

                if (Err == CSC_API_ERROR.ERR_NONE)
                {
                    SmartFunctions.Instance.SetReaderType(_ReaderType, 0);
                    // SKS:fill CSC firmware version info
                    _firmwareVersion.SetEvent(_FirmwareInfo.AppCSC);
                    _cscChargeurVersion.SetEvent(_FirmwareInfo.Chargeur);
                    _cscAPIVersion.SetEvent(SharedData.cscApiVersion);
                    //Load the Crypto Instance
                    //cFlex = null;
                    //if (_cryptoflexSAMUsage)
                    //    cFlex = new CryptoFlexFunctions((CSC_READER_TYPE)_ReaderType, _hRw);

                    if (_delhiCCHSSAMUsage)
                    {
                      //  mCCHSSAMMgr = new CCHSSAMManger((CSC_READER_TYPE)_ReaderType, _hRw);
                       // Err = ResetCCHSSAM();
                        foreach (cSAMConf smcnf in SharedData.mSAMUsed)
                        {
                            //Err = Reader.ConfigureSAM(
                            if (smcnf.mSAMType == CONSTANT.SAMType.ISAM || smcnf.mSAMType == CONSTANT.SAMType.PSAM) //CCHS SAM
                            {
                                Err = Reader.ConfigureSAM((byte)smcnf.mSAMType, smcnf.SAM_Slot);
                                Logging.Log(LogLevel.Information, "Reader.ConfigureSAM() Ret : " + Err.ToString());
                            }
                            if (Err == CSC_API_ERROR.ERR_NONE)
                            {
                                if (_signatureAtEachTransaction) SharedData.TransactionSeqNo = Reader.GetSAMSequence();
                                SharedData.mDSMId = Reader.GetSAMId();
                                SharedData.CompanyID = Reader.GetSAMInfo().ServiceProvider;
                                break;
                            }
                        }
                            
                    }
                    SmartFunctions.Instance.Init(true);

                    _IsReaderLoaded = true;
                    _dataSecurityModuleFailure.SetAlarm(false);
                    _dataSecurityModuleIsOffLine.SetAlarm(false);
                    _dataSecurityModuleLocked.SetAlarm(false);
                    _dataSecurityModuleAusent.SetAlarm(false);

                    if (_delhiCCHSSAMUsage && Err != CSC_API_ERROR.ERR_NONE)
                    {
                        //switch ((int)mCCHSSAMMgr.mCCHSSAM_Status)
                        //{
                        //    case (int)CONSTANT.SAMErrors.SM_AUTHENTICATION_FAILURE:
                        //    case (int)CONSTANT.SAMErrors.SM_FAILURE:
                        //    case (int)CONSTANT.SAMErrors.SM_INIT_ERROR:
                                _dataSecurityModuleFailure.SetAlarm(true);
                            //    break;
                            //case (int)CONSTANT.SAMErrors.SM_SAMLOCKED_ERROR:
                            //    _dataSecurityModuleLocked.SetAlarm(true);
                            //    break;
                            //case (int)CONSTANT.SAMErrors.SM_LINK_FAILURE:
                            //    _dataSecurityModuleIsOffLine.SetAlarm(true);
                            //    break;
                       // }
                       

                    }

                    _cscReloaderIsOffLine.SetAlarm(false); _cscReloaderIsOffLine2.SetAlarm(false);
                    _cscReloaderFailure.SetAlarm(false); _cscReloaderFailure2.SetAlarm(false);

                }
                else if (Err == CSC_API_ERROR.ERR_DEVICE)
                {
                    _cscReloaderIsOffLine.SetAlarm(true); _cscReloaderIsOffLine2.SetAlarm(true);
                    Logging.Log(LogLevel.Error, ThreadName + "ReloadReader : Reader is Offline");
                }
                else
                {
                    _cscReloaderIsOffLine.SetAlarm(false); _cscReloaderIsOffLine2.SetAlarm(false);
                    _cscReloaderFailure.SetAlarm(true); _cscReloaderFailure2.SetAlarm(true);
                    Logging.Log(LogLevel.Error, ThreadName + "ReloadReader : Reader is Offline");
                }
            }
            catch (Exception e1)
            {
                _cscReloaderIsOffLine.SetAlarm(true); _cscReloaderIsOffLine2.SetAlarm(true);
                Logging.Log(LogLevel.Error, "Error in Reloading the Reader" + e1.Message);
            }
            try
            {
                _cscReloaderMetaStatus.UpdateMetaStatus();
                _dataSecurityModuleMetaStatus.UpdateMetaStatus(); // does _cscReloaderMetaStatus encompasses _dataSecurityModuleMetaStatus too??

                if (_cscReloaderMetaStatus.HasChangedSinceLastSave || bFirstCall)
                {
                    SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                    SetRepetitiveTreatmentInterval(_cscReloaderMetaStatus.Value == (int)(AlarmStatus.Alarm)
                        || _cscReloaderMetaStatus.Value == (int)AlarmStatus.NotConnected ? _frequencyToAttemptReloadReaderInDisconnection * 1000 : GetDefaultRepetitiveTreatmentInterval());
                }
                bFirstCall = false;

                IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");
            }
            catch (Exception e1)
            {
                _cscReloaderIsOffLine.SetAlarm(true); _cscReloaderIsOffLine2.SetAlarm(true);
                if (_cscReloaderMetaStatus.HasChangedSinceLastSave || bFirstCall)
                {
                    SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                }
                bFirstCall = false;

                Logging.Log(LogLevel.Error, "Error in Reloading the Reader" + e1.Message);
            }
            return _IsReaderLoaded;
        }

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

        public override void OnBegin()
        {
            InitializeEODParams();//SKS:01/09/2014 Shifted here because of TT is taking much time to read and configure form big EOD xml files
            try
            {
                Directory.CreateDirectory(Disk.BaseDataDirectory + @"\CurrentXmlParameters\");
            }
            catch { }
            CSC_API_ERROR Err;

            hwCsc = new DelhiDesfireEV0();
            hwCsc.Reset();

            /// DelhiToken ...
            hwToken = new DelhiTokenUltralight();
            /// end 
            //Load the logical medias for Storage
            _logMediaReloader = new LogicalMedia();
            _logMediaToken = new LogicalMedia();

            //Load the Comm params
            _ReaderComm.COM_PORT = (string)Configuration.ReadParameter("ComPort", "String", "COM5:");
            _ReaderComm.COM_SPEED = (int)Configuration.ReadParameter("ComSpeed", "int", "115200");
            _ReaderType = (int)Configuration.ReadParameter("ReaderType", "int", "4");

            if (_delhiCCHSSAMUsage)
            {
                //      Debug.Assert(false);
                //string strSAMInCSCSlots = (string)Configuration.ReadParameter("SM", "String", "A:1;I:2"); // SAMtype:SlotNo;SAMtype:slotNo
                string strSAMInCSCSlots = (string)Configuration.ReadParameter("SM1", "string", "I:1");
                string[] sm_inslots = strSAMInCSCSlots.Split(';');
                foreach (string str in sm_inslots)
                {
                    string[] st = str.Split(':');
                    cSAMConf samcnf = new cSAMConf();
                    try
                    {
                        char ch = Convert.ToChar(st[0].Trim());
                        samcnf.mSAMType = (CONSTANT.SAMType)ch;//char.Parse(st[0]);
                        samcnf.SAM_Slot = int.Parse(st[1]);
                        SharedData.mSAMUsed.Add(samcnf);
                    }
                    catch
                    {
                        Logging.Log(LogLevel.Error, "TT MAIN ERROR while fetching SAM location data");
                    }
                }
            }

            //_ReaderComm.COM_PORT = "COM12:";
            //_ReaderComm.COM_SPEED = 115200;
            //_ReaderType = 4;

            try
            {
                _IsReaderLoaded = false;

                if (ReloadReader())
                {
                    //if (_treatTicketingKeyFile)
                    //{
                    //    LoadKeys();
                    //}

                    //It is the following function which starts the polling.
                    Err = SmartFunctions.Instance.Init(true);

                    if (Err == CSC_API_ERROR.ERR_NONE)
                    {
                        Logging.Log(LogLevel.Information, "Main TT Reader Started Polling");
                    }
                    else
                    {
                        Logging.Log(LogLevel.Error, "Main TT Reader Error in Polling Scenario");
                    }
                }
            }
            catch (Exception e1)
            {
                Logging.Log(LogLevel.Error, "Main TT OnBegin " + e1.Message);
                _cscReloaderFailure.SetAlarm(true); _cscReloaderFailure2.SetAlarm(true);
                _cscReloaderMetaStatus.UpdateMetaStatus();
                base.OnBegin();
            }

            SendCommand("GetInitialisationParams");

            base.OnBegin();
        }

        private static void HaltCurrentCardNRestartPolling()
        {
            //            Console.WriteLine("HaltCurrentCardNRestartPolling. Trace is " + Environment.StackTrace.ToString());
            SmartFunctions.Instance.HaltCard();
            SmartFunctions.Instance.ReStartPolling(1);
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;

            Thread.Sleep(20); // because if we ask immediatly for status, very likely we would still get state POLL_ON
        }
        /*
        public static void RestartPolling()
        {
            //            SmartFunctions.Instance.HaltCard();            
            SmartFunctions.Instance.ReStartPolling(1);
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
        }*/
        public void RestartPolling() { }

        internal LogicalMedia GetLogicalDataOfMediaAtFront(out SmartFunctions.MediaDetected mediaDetected)
        {
            throw new NotImplementedException();
        }
        public LogicalMedia GetLogicalDataOfMediaAtFront(out SmartFunctions.MediaDetected mediaDetected, out long srNbr)
        {
            throw new NotImplementedException();
        }

        public void StopField()
        {
            throw new NotImplementedException();
        }
        private void CheckMediaUpdateForConclusion()
        {
            throw new NotImplementedException();
        }

        internal void SwitchToDetectionRemoval()
        {
            throw new NotImplementedException();
        }

        internal void HaltCurrentCardNRestartPolling(bool p)
        {
            throw new NotImplementedException();
        }

        internal void PollForAnyMediaAtMoment_ThenStopPollingIfNoMediaIsThere(Scenario scenario)
        {
            throw new NotImplementedException();
        }

        public bool IsUsingCCHSSam()
        {
            return _delhiCCHSSAMUsage;
        }

        private void SetMediaUpdateConcluded()
        {
            throw new NotImplementedException();
        }
        internal string GetIdOfToken(long p)
        {
            throw new NotImplementedException();
        }
        internal void PollForAnyMediaAtMoment_ThenStopPolling(Scenario scenario)
        {
            throw new NotImplementedException();
        }
        internal void StartTimer(Timers timers)
        {
            throw new NotImplementedException();
        }

        internal void StopTimer(Timers timers)
        {
            throw new NotImplementedException();
        }
        internal void SetMediaKeptOnReaderWasLastUpdatedInThisVeryCycle()
        {
            throw new NotImplementedException();
        }

        internal bool IsTimerActive(Timers timers)
        {
            throw new NotImplementedException();
        }


        bool _bTicketDataStructuresSynchedWithTicketData = false;

    }
    #endregion "class MainTicketingRules"
}//namespace IFS2.Equipment.TicketingRules
