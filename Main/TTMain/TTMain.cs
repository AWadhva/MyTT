using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.CryptoFlex;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Linq;
#if WindowsCE
using OpenNETCF.Threading;
#endif
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{
    // Base treatment for Ticketing Rules
    public partial class MainTicketingRules : TaskThread, iRemoteMessageInterface, IListener
    {
        // TODO: Why there are two separate logical media objects?? Remove _logMediaToken, and use _logMediaReloader instead.
        private LogicalMedia _logMediaReloader = new LogicalMedia();
        private LogicalMedia _logMediaToken = new LogicalMedia();

        public long GetIdOfToken(long srNbr)
        {
            int id;
            if (_SerialNumVsId.TryGetValue(srNbr, out id))
                return id;
            else
                return srNbr;
        }

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

        private Boolean _IsReaderLoaded = false;
        private Boolean _IsRearReaderLoaded = false;
        private CSC_READER_TYPE _ReaderType;
        private int _hRw;
        IReader _reader = null;
        private int _hRwRearReaderAtTokenDispenser;

        private SmartFunctions.MediaDetected __MediaDetectedState;

        public SmartFunctions.MediaDetected _MediaDetectedState
        {
            get
            {
                if (_reader != null)
                    return _reader.GetMediaDetected();
                else
                    return __MediaDetectedState;
            }
            set
            {
                if (_reader != null)
                    return;
                __MediaDetectedState = value;
            }
        }

        public long _MediaSerialNbr
        {
            get
            {
                if (_reader != null)
                    return _reader.GetMediaSrNbr();
                else
                {
                    if (_MediaDetectedState == SmartFunctions.MediaDetected.NONE)
                        return 0;
                    else
                        return SmartFunctions.Instance.ReadSNbr();
                }
            }
        }

        TTErrorTypes _errorCurMedia;
        AdjustmentInfo _adjustment;

        public DelhiDesfireEV0 hwCsc;
        public DelhiTokenUltralight hwToken;

        CryptoFlexFunctions cFlex;
        CCHSSAMManger mCCHSSAMMgr;

        private ReaderComm _ReaderComm;
        private ReaderComm _ReaderCommRearReader;

        private FirmwareInfo _FirmwareInfo;

        public Semaphore semStopAsked;

        public static OneEvent _cscReloader_FrontOrOnly_Failure = null, _cscReloader_FrontNRear_Failure = null;
        public static OneEvent _cscReloader_FrontOrOnly_IsOffLine = null, _cscReloader_FrontNRear_OffLine = null;
        public static OneEvent _evtRearReaderOffline = null, _evtRearReaderOOO = null, _evtRearReaderMetaStatus = null;
        private static bool _bRearReaderStatusBroadcastOnce = false;

        private int _frequencyToAttemptReloadReaderInDisconnection;

        private XmlWriterSettings contextFile_XMLWriterSettings;

        private bool _treatTicketingKeyFile = true;
        public readonly bool _cryptoflexSAMUsage = true;
        public readonly bool _delhiCCHSSAMUsage = false;
        public readonly bool _signatureAtEachTransaction = false;
        private bool _generateXdrInTransaction = false;
        private bool _readEquipmentNumberInFile = false;
        private bool _readDeviceIDInCCHSSAM = false;

        private readonly bool _bMediaDispensingUsingRearAntennaOfPrimaryReader;
        private readonly bool _bMediaDispensingUsingRearReader;

        MediaDistributionTransaction _curMediaDistributionTxn = null;

        private bool IsMediaDistributionInProgress()
        {
            return _curMediaDistributionTxn != null;
        }

        MediaOperationsRequested _mediaUpdate = null, _mediaUpdateLast = null;
        private MediaDetectionTreatment _readDataFor;// = MediaDetectionTreatment.BasicAnalysis_AVM_TVM;
        private int? _readDataForAddVal_RechargeValueRequested = null;
        private int? _readDataForPutNewProductInExistingMedia_ProductTypeRequested = null;

        SmartFunctions inst;
        const string moduleRearReader = "RearReader";

        public static OneEvent _ticketKeysMissing = null;
        public static OneEvent _ticketKeysError = null;
        public static OneEvent _ticketKeysMetaStatus = null;



        public MainTicketingRules()
            : base("MainTicketingRules")
        {            
            try
            {
                Logging.Log(LogLevel.Information, Configuration.DumpAllConfiguration());
                Logging.Log(LogLevel.Information, IFS2.Equipment.Common.Parameters.DumpAllConfiguration());


                _bMediaDispensingUsingRearAntennaOfPrimaryReader = (bool)Configuration.ReadParameter("TokenDispenseFunctionality", "bool", "false");
                _bMediaDispensingUsingRearReader = (bool)Configuration.ReadParameter("TokenDispensingUsingRearReader", "bool", "false");

                Debug.Assert(!_bMediaDispensingUsingRearAntennaOfPrimaryReader || !_bMediaDispensingUsingRearReader); // _bTokenDispensingUsingRearAntennaOfPrimaryReader->!_bTokenDispensingUsingRearReader; _bTokenDispensingUsingRearReader->!_bTokenDispensingUsingRearAntennaOfPrimaryReader)

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
                _readDeviceIDInCCHSSAM = Configuration.ReadBoolParameter("ReadDeviceIDInCCHSSAM", false);
                try
                {
                    _readDataFor = Utility.ParseEnum<MediaDetectionTreatment>(Configuration.ReadStringParameter("ReadDataFor", "BasicAnalysis_AVM_TVM"));
                }
                catch { }

                contextFile_XMLWriterSettings = new XmlWriterSettings();
                contextFile_XMLWriterSettings.Indent = false;
                contextFile_XMLWriterSettings.OmitXmlDeclaration = true;

                if (!Configuration.ReadAndStoreDicoEnumParameter<AlarmStatus>("EODAlarmLevelStatus"))
                {
                    Logging.Log(LogLevel.Critical, "MainTicketingRules.Constructor.ErrorLoading.EODAlarmLevelStatus");
                }

                InitParameterRelated();


                //Initialisation of all the tags managed by the TT
                _ticketKeysMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 1, "TicketKeysMissing", "OCKMIS", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _ticketKeysError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 2, "TicketKeysError", "OCKDEC", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _ticketKeysMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 3, "TicketKeysMetaStatus", "OCKMET", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                _ticketKeysMetaStatus.SetMetaStatusLinkage("", "TicketKeysMissing;TicketKeysError");

                //Tags for CSC Reloader Management                
                _cscReloader_FrontOrOnly_IsOffLine = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 1, "IsOffLine", "CS1COM", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _cscReloader_FrontOrOnly_Failure = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 2, "OutOfOrder", "CS1STA", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _cscReloader_FrontNRear_OffLine = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 3, "IsOffLine1", "CSCCOM", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                _cscReloader_FrontNRear_Failure = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 4, "OutOfOrder1", "CSCSTA", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                {
                    _evtRearReaderOffline = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 6, "IsRearOffLine", "CS2COM", AlarmStatus.Warning, OneEvent.OneEventType.Alarm);
                    _evtRearReaderOOO = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 7, "RearOOO", "CS2STA", AlarmStatus.Warning, OneEvent.OneEventType.Alarm);
                    _evtRearReaderOOO.SetValue(0);
                    IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                    //_evtRearReaderOOO.SetAlarm(false);
                }

                if (_bMediaDispensingUsingRearReader)
                {
                    _evtRearReaderOffline = new OneEvent((int)StatusConsts.CSCReloaderDriver, moduleRearReader, 6, "IsRearOffLine", "CS2COM", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                    _evtRearReaderOOO = new OneEvent((int)StatusConsts.CSCReloaderDriver, moduleRearReader, 7, "RearOOO", "CS2STA", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm);
                    _evtRearReaderMetaStatus = new OneEvent((int)StatusConsts.CSCReloaderDriver, moduleRearReader, 8, "RearReaderMetaStatus", "", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
                    _evtRearReaderMetaStatus.SetMetaStatusLinkage("IsRearOffLine", "RearOOO");
                }

                _cscReloaderMetaStatus = new OneEvent((int)StatusConsts.CSCReloaderDriver, "CSCReloaderDriver", 5, "MetaStatus", "METCSC", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus); ;
                if ((!_bMediaDispensingUsingRearAntennaOfPrimaryReader && !_bMediaDispensingUsingRearReader)
                    || _bMediaDispensingUsingRearReader)
                    _cscReloaderMetaStatus.SetMetaStatusLinkage("IsOffLine", "OutOfOrder");
                else if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                    _cscReloaderMetaStatus.SetMetaStatusLinkage("IsOffLine", "OutOfOrder;RearOOO");

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
                _dataSecurityModuleAusent = new OneEvent((int)StatusConsts.DSMDriver, "DSMDriver", 4, "SAMAusent", "", AlarmStatus.Alarm, OneEvent.OneEventType.Alarm); ;
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

                MAX_TOKENS_TO_TRY_BEFORE_DECLARING_AS_OOO = (int)Configuration.ReadParameter("MAX_TOKENS_TO_TRY_BEFORE_DECLARING_AS_OOO", "int", "3");

                SharedData.ReadContextFile();

                //Commands from MMI
                Communication.AddEventsToReceive(ThreadName, "StopCardDistribution", this);
                Communication.AddEventsToReceive(ThreadName, "UseAntenna", this);
                Communication.AddEventsToReceive(ThreadName, "MediaNotFoundFitForOperationAcked", this);
                Communication.AddEventsToReceive(ThreadName, "GetTokenPrice;GetTokenPriceForFreeOrPaidExit;ReadUserCardSummary;GetListFareTiers;ManualPaidAtEntryBus", this);
                Communication.AddEventsToReceive(ThreadName, "ReloadTPurseOnCard;ReadUserCardGlobalData;ReadUserCardTransactionHistory;PaymentWithTPurse;PaymentDeduction", this);
                Communication.AddEventsToReceive(ThreadName, "DetailsOfMediaToBeRead;AgentLoggedOut", this);
                Communication.AddEventsToReceive(ThreadName, "Shutdown;GetCSCReloaderMetaStatus", this);
                Communication.AddEventsToReceive(ThreadName, "GetCertificate;SendEquipmentMode", this);
                Communication.AddEventsToReceive(ThreadName, "GetTokenBinData;ResetCSCReloader", this);
                Communication.AddEventsToReceive(ThreadName, "GetTicketingKeyMetaStatus", this);               
                
                
                Communication.AddEventsToExternal("TokenInBin;CardInBin", MMIChannel);

                Communication.AddEventsToReceive(ThreadName, "NotMainstream_GiveAllMediaSerialNumsInVicinity", this);
                Communication.AddEventsToExternal("AllMediaSerialNumsInVicinityAnswer", MMIChannel);

                //Commands from SC TVM Interface
                Communication.AddEventToReceive(ThreadName, "GetCertificateOfEqpt", this);
                Communication.AddEventToReceive(ThreadName, "GetCertificateOfCA", this);
                Communication.AddEventToReceive(ThreadName, "EncryptUsingPrivateKey", this);
                Communication.AddEventToReceive(ThreadName, "GetMachineId", this);
                Communication.AddEventToReceive(ThreadName, "GetCSCReloaderStatus", this);
                Communication.AddEventToReceive(ThreadName, "GetSAMStatus", this);
                
                
                
                Communication.AddEventToReceive(ThreadName, "NewTicketingKeysFile", this);

                
                //Messages for MMI
                Communication.AddEventsToExternal("UpdateMediaInitiatingNewOp;MediaNotFoundFitForOperation;GetTokenPriceAnswer;GetListFareTiersAnswer;ReadUserCardSummaryAnswer;CSCMediaDetection;CSTMediaDetection;GetInitialisationParams;MediaRemoved;UpdateMediaOpCantBePerformed;UpdateMediaOpAudited", MMIChannel);
                Communication.AddEventsToExternal("ReloadTPurseOnCardAnswer;ReadUserCardGlobalDataAnswer;ReadUserCardTransactionHistoryAnswer;VerifyAgentDataAnswer", MMIChannel);
                Communication.AddEventsToExternal("BadPassengerCardDetection;BadAgentCardDetection;AgentCardDetection;UpdateCardStatus", MMIChannel);

                Communication.AddEventsToExternal("GetTokenBinDataAnswer;GetAllEventsAnswer;EquipmentParams;GetMachineIdAnswer;GetSAMStatusAnswer", CoreChannel);
                Communication.AddEventsToExternal("SAMMetaStatus", MMIChannel);

                Communication.AddEventsToExternal("GetCSCReloaderStatusAnswer;GetMachineIdAnswer", MMIChannel);

                Communication.AddEventsToExternal("GetSAMStatusAnswer;CSCReloaderMetaStatus;TicketingKeyMetaStatus", MMIChannel);

                Communication.AddEventsToExternal("TopologyParametersUpdated;ParametersUpdated", MMIChannel);
                Communication.AddEventsToExternal("UpdateMediaTerminated", MMIChannel);               
                

                if (SharedData.EquipmentType == EquipmentFamily.TOM)
                {
                    Communication.AddEventsToReceive(ThreadName, "GetTokenDispenserStatusAnswer;TokenDispenserMetaStatus;PutTokenUnderRWAck;CancelPutTokenUnderRWAck", this);
                    Communication.AddEventsToExternal("PutTokenUnderRW;GetTokenDispenserStatus", CoreChannel);
                    Communication.AddEventsToExternal("RemoveMedia;AskedMediaRemoved;UpdateMediaOpConcluded", MMIChannel);
                    Communication.AddEventsToReceive(ThreadName, "UndetectableTokenRemoved", this);
                    Communication.AddEventsToExternal("CancelPutTokenUnderRW", CoreChannel);
                    Communication.AddEventsToExternal("AskAgentToChooseIfHeWantsToCompleteOperationUsingLooseTokens", MMIChannel);
                    Communication.AddEventsToReceive(ThreadName, "CompleteAFMOpUsingLooseTokens", this);
                    Communication.AddEventsToReceive(ThreadName, "UpdateMedia;UpdateMediaAbortTxn;UpdateMediaDeclarePartCompletedAsDone", this);
                    Communication.AddEventsToReceive(ThreadName, "UpdateMediaRollbackOp;UpdateMediaAbandonRollbackMode", this);
                    Communication.AddEventsToExternal("UpdateMediaRollbackOpAnswer;UpdateMediaRollbackCompletedOrAbandoned;RTE_Or_WTE;SomeMediaAppearedPostRTEOrWTEInLastCycle", MMIChannel);
                    Communication.AddEventToReceive(ThreadName, "DoOpForUnreadableCSC", this);
                    Communication.AddEventsToExternal("DoOpForUnreadableCSCReply", MMIChannel);
                    Communication.AddEventToReceive(ThreadName, "RegisterEquipmentStatus", this);

                    Communication.AddEventsToExternal("GetFareMode", MMIChannel);
                    Communication.AddEventToReceive(ThreadName, "GetFareModeReply", this);
                }

                if (_bMediaDispensingUsingRearReader)
                {
                    Communication.AddEventsToExternal("GetRearReaderStatusAnswer;RearReaderMetaStatus", MMIChannel);
                    Communication.AddEventsToReceive(ThreadName, "GetRearReaderStatus", this);
                }
                if (_bMediaDispensingUsingRearAntennaOfPrimaryReader || _bMediaDispensingUsingRearReader)
                {
                    Communication.AddEventsToReceive(ThreadName, "GetTokenDispenserStatusAnswer;TokenDispenserMetaStatus;StartTokenDistribution;StartMaintenanceTokenDistribution;StopTokenDistribution;PutTokenUnderRWAck;ThrowTokenAck", this);

                    Communication.AddEventsToExternal("PutTokenUnderRW;GetTokenDispenserStatus", CoreChannel);
                    Communication.AddEventsToExternal("TokenDispenserOutJam", CoreChannel);
                    Communication.AddEventsToExternal("ThrowTokenToBin;ThrowTokenToOT;ResetTokenDispenser", CoreChannel);
                    Communication.AddEventsToExternal("ThrowToBeDispensedTokenToBin", CoreChannel);
                    Communication.AddEventsToExternal("TokenError;TokenDistributed;StopTokenDistributionAck", MMIChannel);
                }

                Communication.AddEventsToExternal("CardError;CardDistributed;StopCardDistributionAck", MMIChannel);
                Communication.AddEventsToReceive(ThreadName, "StartCardDistribution", this);

                if (Config.bTTagFunc)
                {
                    Communication.AddEventsToReceive(ThreadName, "UpdateTTag;UpdateTTagCancel", this);
                    Communication.AddEventToExternal("UpdateTTagAnswer", MMIChannel);
                }

                Communication.AddEventsToReceive(ThreadName, "MediaProduced;MediaRemovedInt", this);

                Communication.AddEventsToExternal("MoveCscdAtCtlssPosition;ThrowCscInBin;ThrowCscToOut", CoreChannel);
                Communication.AddEventsToReceive(ThreadName, "MoveCscdAtCtlssPositionAnswer;ThrowCscAnswer;StartCSCDistribution", this);

                //Alarms for CoreChannel
                Communication.AddEventsToExternal("CSCReloaderMetaStatus;StoreAlarm;GetCSCReloaderStatusAnswer", CoreChannel);

                //Load the Comm params
                _ReaderComm.COM_PORT = (string)Configuration.ReadParameter("ComPort", "String", "COM1:");
                _ReaderComm.COM_SPEED = (int)Configuration.ReadParameter("ComSpeed", "int", "115200");
                _ReaderType = (CSC_READER_TYPE)((int)Configuration.ReadParameter("ReaderType", "int", "4"));

                if (Config.bCleanedUp)
                {
                    if (_ReaderType == CSC_READER_TYPE.V4_READER)
                        _reader = new V4Reader();
                    else if (_ReaderType == CSC_READER_TYPE.V3_READER)
                        _reader = new V3Reader(this);

                    if (_reader != null)
                        _reader.SetListener(this);
                }

                bool bLoginWithDummyAgentId = (bool)Configuration.ReadParameter("LoginWithDummyAgentId", "bool", "false");
                if (bLoginWithDummyAgentId)
                    SharedData._agentShift = new AgentShift(2, 3, AgentProfile.CashSupervisor);

                IFSEventsList.InitContextFile("TTComponent");
                IFSEventsList.InitContextFile("CSCReloaderDriver");
                IFSEventsList.InitContextFile("DSMDriver");
                if (_bMediaDispensingUsingRearReader)
                    IFSEventsList.InitContextFile(moduleRearReader);
#if !WindowsCE
                SetTimers(Enum.GetNames(typeof(Timers)).Length);
#endif
                try
                {
                    _frequencyToAttemptReloadReaderInDisconnection = (int)Configuration.ReadParameter("FrequencyToAttemptReloadReaderInDisconnection", "int", "60");
                }
                catch { }

                if (SharedData.EquipmentType != EquipmentFamily.TOM)
                    SmartFunctions.Instance.SetReaderType(CSC_READER_TYPE.V4_READER, -1);
                Logging.Log(LogLevel.Information, ThreadName + "_Started");
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, ThreadName + "_Constructor Error :" + e.Message);
                throw (e);
            }
        }

        private RegisterEquipmentStatus _eqptStatus = null;

        public void SetMediaKeptOnReaderWasLastUpdatedInThisVeryCycle()
        {
            _bTicketDataStructuresSynchedWithTicketData = false;
        }        

        Dictionary<long, int> _SerialNumVsId = new Dictionary<long, int>();
        Dictionary<int, long> _IdVsSerialNum = new Dictionary<int, long>();

        const int FareMode_Incident = 3;
        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            try
            {
                Logging.Log(LogLevel.Verbose, "Main TT : Message received " + eventMessage.EventID + " " + eventMessage.Attribute + " " + eventMessage.Message);
                if (TreatParametersMessageReceived(eventMessage)) return 0;
                if (TreatTokenMessageReceived(eventMessage)) return 0;
                if (TreatCommonMessage(eventMessage)) return 0;
                if (TreatCSCMessageReceived(eventMessage))
                {
                    //SKS: Added on 09-10-2014
                    //_logMediaReloader.Reset();
                    //  hwCsc.Reset();
                    return 0;
                }
                switch (eventMessage.EventID.ToUpper())
                {
                    case "GETFAREMODEREPLY": // used exclusively by TOM
                        _eqptStatus = SerializeHelper<RegisterEquipmentStatus>.XMLDeserialize(eventMessage._par[0]);
                        break;
                    case "REGISTEREQUIPMENTSTATUS": // used exclusively by TOM
                        _eqptStatus = SerializeHelper<RegisterEquipmentStatus>.XMLDeserialize(eventMessage._par[0]);
                        break;
                    case "USEANTENNA":
                        Handle_UseAntenna(Convert.ToInt32(eventMessage._par[0]));
                        break;
                    case "MEDIANOTFOUNDFITFOROPERATIONACKED":
                        Handle_MEDIANOTFOUNDFITFOROPERATIONACKED();
                        return 0;
                    case "NOTMAINSTREAM_GIVEALLMEDIASERIALNUMSINVICINITY":
                        {
                            Handle_GIVEALLMEDIASERIALNUMSINVICINITY(eventMessage);
                            return 0;
                        }
                    case "MEDIAREMOVEDINT":
                        {
                            long ticketPhysicalId = 0;//Convert.ToInt64(eventMessage._par[0]);
                            DateTime tsWhenMediaWasRemoved = new DateTime(Convert.ToInt64(eventMessage._par[0]));

                            Debug.Assert(_reader is V4Reader);
                            ((V4Reader)_reader).MediaRemovedInt(ticketPhysicalId, tsWhenMediaWasRemoved);

                            break;
                        }
                    case "GETTICKETINGKEYMETASTATUS":
                        SendMessage(ThreadName, "", "TicketingKeyMetaStatus", _ticketKeysMetaStatus.Value.ToString(), "");
                        break;
                    case "COMPLETEAFMOPUSINGLOOSETOKENS":
                        {
                            _reader.StartPolling();
                            Debug.Assert(_mediaUpdate != null);
                            if (_mediaUpdate != null)
                                _mediaUpdate.CompleteAFMOpUsingLooseTokens();
                            break;
                        }
                    case "MEDIAPRODUCED":
                        {
                            DateTime tsWhenMediaWasProduced = new DateTime(Convert.ToInt64(eventMessage._par[1]));
                            StatusCSC pStatusCSC = SerializeHelper<StatusCSC>.XMLDeserialize(eventMessage._par[0]);
                            if (_reader != null && _reader is ThalesReader)
                                ((ThalesReader)_reader).MediaProduced(pStatusCSC, tsWhenMediaWasProduced);

                            break;
                        }
                    case "SENDEQUIPMENTMODE":
                        //This message permits to indicate if we are in maintenance mode.
                        SharedData.EquipmentStatus = (EquipmentStatus)Convert.ToInt32(eventMessage.Attribute);
                        return 0;
                    case "UPDATEMEDIAROLLBACKOP":
                        Handle_UpdateMediaRollbackOp();
                        return 0;
                    case "UPDATEMEDIAABANDONROLLBACKMODE":
                        Handle_UpdateMediaAbandonRollbackMode();
                        return 0;
                    case "NEWTICKETINGKEYSFILE":
                        try
                        {
                            if (_treatTicketingKeyFile)
                            {
                                //To see if it is a file or directly a stream.
                                string content = DelhiSpecific.TreatFileKeyListParameters(eventMessage.Message);
                                //Temporary saving

#if WindowsCE
                            Utility.WriteAllText(Disk.BaseDataDirectory + "\\CurrentXmlParameters\\TestKey.xml", content);
#else
                                File.WriteAllText(Disk.BaseDataDirectory + "\\CurrentXmlParameters\\TestKey.xml", content);
#endif
                                File.Delete(eventMessage.Message);

                                SecurityMgr.Instance.LoadKeyList(_ReaderType, _hRw, content);
                            }
                            //Do we need to reboot reader in this case
                            _ticketKeysError.SetAlarm(false);
                            _ticketKeysMissing.SetAlarm(false);
                        }
                        catch (Exception e)
                        {
                            Logging.Log(LogLevel.Error, "Error Loading Ticketing Keys " + e.Message);
                            _ticketKeysError.SetAlarm(true);
                        }
                        _ticketKeysMetaStatus.UpdateMetaStatus();
                        if (_ticketKeysMetaStatus.HasChangedSinceLastSave)
                            SendMessage(ThreadName, "", "TicketingKeyMetaStatus", _ticketKeysMetaStatus.Value.ToString(), "");

                        _globalMetaStatus.UpdateMetaStatus();
                        IFSEventsList.SaveIfHasChangedSinceLastSave("TTComponent");
                        try
                        {
                            SmartFunctions.Instance.Init(true);
                        }
                        catch (Exception e)
                        {
                            Logging.Log(LogLevel.Error, "TTMain.TreatMessage " + e.Message);
                        }
                        return 0;
                    case "GETCERTIFICATEOFEQPT":
                        if (_cryptoflexSAMUsage)
                        {
                            try
                            {
                                byte[] eba = cFlex.GetCertificate(DEST_TYPE.DEST_SAM1,
                                                            CERT_TYPE.LOCAL_CERT);

                                string ecert = SerializeHelper<byte[]>.XMLSerialize(eba);

                                Communication.SendMessage(ThreadName, "Answer", "GetCertificateOfEqptAnswer", "1", ecert);
                            }
                            catch (Exception e1)
                            {
                                Communication.SendMessage(ThreadName, "Answer", "GetCertificateOfEqptAnswer", Convert.ToString((int)TTErrorTypes.Exception), "");
                                Logging.Log(LogLevel.Error, ThreadName + "GetCertificateOfEqptAnswer Error :" + e1.Message);
                                return (0);
                            }
                        }

                        return (0);

                    case "GETCERTIFICATEOFCA":
                        if (_cryptoflexSAMUsage)
                        {
                            try
                            {
                                byte[] cba = cFlex.GetCertificate(DEST_TYPE.DEST_SAM1,
                                                                  CERT_TYPE.CA_CERT);

                                string ccert = SerializeHelper<byte[]>.XMLSerialize(cba);

                                Communication.SendMessage(ThreadName, "Answer", "GetCertificateOfCAAnswer", "1", ccert);
                            }
                            catch (Exception e1)
                            {
                                Communication.SendMessage(ThreadName, "Answer", "GetCertificateOfCAAnswer", Convert.ToString((int)TTErrorTypes.Exception), "");
                                Logging.Log(LogLevel.Error, ThreadName + "GetCertificateOfCAAnswer Error :" + e1.Message);
                                return (0);
                            }
                        }

                        return (0);

                    case "ENCRYPTUSINGPRIVATEKEY":
                        try
                        {
                            byte[] pDataIn = SerializeHelper<byte[]>.XMLDeserialize(eventMessage.Attribute);
                            string senderMsgID = eventMessage.Message;

                            byte[] iaa = cFlex.InternalAuthDes(DEST_TYPE.DEST_SAM1,
                                                             0, // private key number
                                                             pDataIn);

                            string ecypt = SerializeHelper<byte[]>.XMLSerialize(iaa);
                            Communication.SendMessage(ThreadName, "Answer", "EncryptUsingPrivateKeyAnswer", "1;" + senderMsgID, ecypt);
                        }
                        catch (Exception e1)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "EncryptUsingPrivateKeyAnswer", Convert.ToString((int)TTErrorTypes.Exception), "");
                            Logging.Log(LogLevel.Error, ThreadName + "EncryptUsingPrivateKeyAnswer Error :" + e1.Message);
                        }

                        return (0);

                    case "STARTTOKENDISTRIBUTION":
                        {
                            Handle_StartTokenDistribution(eventMessage);

                            break;
                        }
                    case "STARTCARDDISTRIBUTION":
                        {
                            Handle_StartCSCDistribution(eventMessage);
                            break;
                        }
                    case "GETREARREADERSTATUS":
                        {
                            Handle_GetRearReaderStatus();
                            break;
                        }
                    case "STARTMAINTENANCETOKENDISTRIBUTION":
                        {
                            Handle_StartMaintenanceTokenDistribution(eventMessage);

                            break;
                        }
                    case "STARTMEDIADISTRIBUTIONINTERNAL":
                        {
                            Handle_StartMediaDistributionInternal();
                            break;
                        }
                    case "DELAYN":
                        {
                            HandleDelay(eventMessage);
                            break;
                        }
                    case "STOPTOKENDISTRIBUTION":
                    case "STOPCARDDISTRIBUTION":
                        {
                            Handle_StopMediaDistribution();
                            break;
                        }
                    case "PUTTOKENUNDERRWACK":
                        {
                            Handle_PutTokenUnderRWAck(eventMessage._par);
                            break;
                        }
                    case "THROWTOKENACK":
                        {
                            Handle_ThrowMediaAck(eventMessage._par);
                            break;
                        }
                    case "MOVECSCDATCTLSSPOSITIONANSWER":
                        inst.StopField(); // ANUJ: It is irrational. Still it is required. Else, lot of CSCs are not getting detected.
                        Handle_PutTokenUnderRWAck(eventMessage._par);
                        break;
                    case "THROWCSCANSWER":
                        Handle_ThrowMediaAck(eventMessage._par);
                        break;
                    case "GETTOKENDISPENSERSTATUSANSWER":
                        {
                            Handle_GetTokenDispenserStatusAnswer(eventMessage._par[0]);
                            break;
                        }
                    case "TOKENDISPENSERMETASTATUS":
                        {
                            Handle_TokenDispenserMetaStatus(eventMessage);
                            break;
                        }
                    case "UNDETECTABLETOKENREMOVED":
                        {
                            Handle_UndetectableTokenRemoved(eventMessage);
                            break;
                        }
                    case "UPDATEMEDIA":
                        {
                            Handle_UpdateMedia(eventMessage);
                            break;
                        }
                    case "UPDATEMEDIADECLAREPARTCOMPLETEDASDONE":
                        Handle_UpdateMediaDeclarePartCompletedAsDone(eventMessage);
                        break;
                    case "UPDATETTAG":
                        Handle_UpdateTTag(eventMessage);
                        break;
                    case "UPDATETTAGCANCEL":
                        Handle_UpdateTTagCancel();
                        break;
                    case "UPDATEMEDIAABORTTXN":
                        {
                            Handle_UpdateMediaAbortTxn();
                            break;
                        }
                    case "DETAILSOFMEDIATOBEREAD":
                        Handle_DetailsOfMediaToBeRead(eventMessage);
                        break;
                    case "CANCELPUTTOKENUNDERRWACK":
                        Handle_CancelPutTokenUnderRWAck();
                        break;
                    case "DOOPFORUNREADABLECSC":
                        Handle_DoOpForUnreadableCSC(eventMessage._par);
                        break;
                    case "GETSAMSTATUS":

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
                                Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", s2, ((int)_dataSecurityModuleMetaStatus.Value).ToString());
                            }
                            else
                            {
                                _dataSecurityModuleFailure.SetAlarm(true);
                                _dataSecurityModuleMetaStatus.UpdateMetaStatus();
                                IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");
                                Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), ((int)_dataSecurityModuleMetaStatus.Value).ToString());
                                Logging.Log(LogLevel.Error, ThreadName + "GetSAMStatusAnswer : Error reloading reader ");
                            }
                            return 0;

                        }
                        catch (Exception e1)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetSAMStatusAnswer", ConstructSAMStatusError(), "");
                            Logging.Log(LogLevel.Error, ThreadName + "GetSAMStatusAnswer Error :" + e1.Message);
                            return 0;
                        }
                    case "GETCSCRELOADERSTATUS":
                        try
                        {
                            if (!_IsReaderLoaded) ReloadReader();
                            Communication.SendMessage(ThreadName, "Answer", "GetCSCReloaderStatusAnswer", ConstructCscStatus(), Convert.ToString((int)_cscReloaderMetaStatus.Value));
                        }
                        catch (Exception)
                        {
                            _cscReloader_FrontOrOnly_Failure.SetAlarm(true);
                            _cscReloader_FrontNRear_Failure.SetAlarm(true);
                            _cscReloaderMetaStatus.UpdateMetaStatus();
                            if (IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver"))
                                SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.AlarmLevel), ConstructCscStatus());
                        }
                        return 0;
                    case "RESETCSCRELOADER":
                        try
                        {
                            //First if reader not already loaded. Make a standard Reload
                            if (!_IsReaderLoaded) ReloadReader();
                            else
                            {
                                //Try to make reset ùmore quick as possible. To call minimum commands
                                //ResetAlreadyStartedReader(); This take too much time
                                //SmartFunctions.Instance.Init();
                            }
                        }
                        catch (Exception e)
                        {
                            Logging.Log(LogLevel.Error, "TTMain.TreatMessageReceived Cannot Reset CSC Reader :" + e.Message);
                        }
                        return 0;
                    case "AGENTLOGGEDOUT":
                        Handle_AgentLoggedOut();
                        return 0;
                    case "GETCSCRELOADERMETASTATUS":
                        try
                        {
                            SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                        }
                        catch (Exception)
                        {
                        }
                        return 0;
                    case "GETMACHINEID":
                        {
                            try
                            {
                                uint result = 0;
                                string addResult = "";
                                if (_readEquipmentNumberInFile)
                                {
                                    try
                                    {
                                        string s = FileUtility.ReadContext("DeviceID");
                                        s = Utility.SearchSimpleCompleteTag(s, "Data");
                                        s = Crypto.DecryptString2(s, "LeonettiJeanIFS2.Ticketing_113*_");
                                        result = Convert.ToUInt32(s);
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
                                    result = (uint)cFlex.GetEQPLocalId(DEST_TYPE.DEST_SAM1);
                                }
                                else if (_readDeviceIDInCCHSSAM && _delhiCCHSSAMUsage)
                                {
                                    //Data have been already read normalli
                                    GetDataFromDSM eqp = new GetDataFromDSM();
                                    string s10 = "";
                                    for (int i = 0; i < 15 && mCCHSSAMMgr.mCCHSDSMInfo.ucIPaddress[i] != 0; i++) s10 += (char)mCCHSSAMMgr.mCCHSDSMInfo.ucIPaddress[i];
                                    s10 = s10.TrimEnd(' ');
                                    eqp.Location.IPAddress = s10;
                                    s10 = "";
                                    for (int i = 0; i < 10 && mCCHSSAMMgr.mCCHSDSMInfo.ucDeviceID[i] != 0; i++) s10 += (char)mCCHSSAMMgr.mCCHSDSMInfo.ucDeviceID[i];
                                    eqp.Location.ServiceProvider = Convert.ToInt32(s10.Substring(0, 2));
                                    eqp.Definition.EquipmentType = Convert.ToInt32(s10.Substring(2, 2));
                                    eqp.Definition.EquipmentSubType = Convert.ToInt32(s10.Substring(4, 1));
                                    eqp.Definition.DeviceID = (Convert.ToInt32(eqp.Definition.EquipmentType) << 16) + Convert.ToInt32(s10.Substring(5, 5));
                                    eqp.Definition.EquipmentNumber = eqp.Definition.DeviceID;
                                    eqp.Definition.EquipmentIndex = eqp.Definition.DeviceID & 0xFFFF;
                                    eqp.Definition.DSM = Convert.ToString(mCCHSSAMMgr.mCCHSDSMInfo.ulDSMid);
                                    s10 = "";
                                    for (int i = 0; i < 20 && mCCHSSAMMgr.mCCHSDSMInfo.ucUniqueInfo[i] != 0; i++) s10 += (char)mCCHSSAMMgr.mCCHSDSMInfo.ucUniqueInfo[i];
                                    eqp.Location.LineNumber = Convert.ToInt32(s10.Substring(0, 2));
                                    eqp.Location.StationNumber = Convert.ToInt32(s10.Substring(2, 3));
                                    eqp.Location.LocationNumber = Convert.ToInt32(s10.Substring(5, 5));
                                    eqp.Location.LocationName = s10.Substring(10);
                                    eqp.Location.LocationName.TrimEnd(' ');
                                    //We can make now string to send back data
                                    result = Convert.ToUInt32(eqp.Definition.DeviceID);
                                    addResult = SerializeHelper<GetDataFromDSM>.XMLSerialize(eqp);
                                    //foreach (cSAMConf samcnf in SharedData.mSAMUsed)
                                    //{
                                    //    if (samcnf.mSAMType == CONSTANT.CCHSSAMType.ISAM)
                                    //    {
                                    //        Err = (CSC_API_ERROR)mCCHSSAMMgr.SAMInstallCard((DEST_TYPE)samcnf.SAM_Slot);
                                    //        if (Err == CSC_API_ERROR.ERR_NONE)
                                    //        {
                                    //            /// Configure CCHS SAM and Switch to mode 2
                                    //            Err = mCCHSSAMMgr.ConfigCCHSSAM((DEST_TYPE)samcnf.SAM_Slot);
                                    //            SharedData.TransactionSeqNo = mCCHSSAMMgr.TxnSeqenceNo;
                                    //            result = mCCHSSAMMgr.DSMId;
                                    //            //TODO: to be checked whether we need sam status info.... now ??? it is already avaialble with CCHSSAM Manager class
                                    //        }
                                    //    }
                                    //}                                    
                                }

                                if (result > 0)
                                {
                                    SharedData.EquipmentNumber = (int)result;
                                    SharedData.EquipmentType = (EquipmentFamily)(result >> 16);
                                    if (addResult == "")
                                        Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", result.ToString(), "1");// As TVM SC interface is treating message=1 as good so changing it to 1 form 0 
                                    else
                                        Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", result.ToString(), "1", addResult);// As TVM SC interface is treating message=1 as good so changing it to 1 form 0 
                                }
                                else
                                {
                                    Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", "0", "0");
                                }
                            }
                            catch (Exception e1)
                            {
                                Communication.SendMessage(ThreadName, "Answer", "GetMachineIdAnswer", "0", Convert.ToString((int)TTErrorTypes.Exception));
                                Logging.Log(LogLevel.Error, ThreadName + "GetMachineIdAnswer Error :" + e1.Message);
                                return 0;
                            }
                        }
                        return 0;
                    case "SHUTDOWN":
                        Communication.RemoveAllEvents(ThreadName);
                        return (-1); //To terminate the process
                }
                return base.TreatMessageReceived(eventMessage);
            }
            catch (ReaderException exp)
            {
                Logging.Log(LogLevel.Information, "ReaderException = " + exp.Code.ToString());
                //Logging.Log(LogLevel.Error, "Exception string = " + exp.StackTrace);

                switch (exp.Code)
                {
                    case CSC_API_ERROR.ERR_LINK:
                        {
                            _IsReaderLoaded = false;
                            ResetCurrentLogicalMediaDataStructs();

                            if (exp.HRw == _hRw)
                            {
                                _cscReloader_FrontNRear_OffLine.SetAlarm(true);
                                _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(true);
                                if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                                {
                                    if (_evtRearReaderOffline != null)
                                        _evtRearReaderOffline.SetAlarm(true);
                                }

                                _cscReloaderMetaStatus.UpdateMetaStatus();
                                if (_cscReloaderMetaStatus.HasChangedSinceLastSave)
                                    Communication.SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                            }
                            else if (exp.HRw == _hRwRearReaderAtTokenDispenser)
                            {
                                if (_evtRearReaderOffline != null)
                                {
                                    _evtRearReaderOffline.SetAlarm(true);
                                    UpdateRearReaderMetaStatus();
                                }
                            }
                            return 0;

                        }
                    case CSC_API_ERROR.ERR_DEVICE:
                    case CSC_API_ERROR.ERR_COM:
                        {
                            _IsReaderLoaded = false;
                            ResetCurrentLogicalMediaDataStructs();

                            if (exp.HRw == _hRw)
                            {
                                _cscReloader_FrontOrOnly_Failure.SetAlarm(true);
                                _cscReloader_FrontNRear_Failure.SetAlarm(true);
                                _cscReloader_FrontNRear_OffLine.SetAlarm(true);
                                _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(true);
                                if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                                {
                                    if (_evtRearReaderOffline != null)
                                        _evtRearReaderOffline.SetAlarm(true);
                                }

                                _cscReloaderMetaStatus.UpdateMetaStatus();
                                if (_cscReloaderMetaStatus.HasChangedSinceLastSave)
                                    Communication.SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                            }
                            else if (exp.HRw == _hRwRearReaderAtTokenDispenser)
                            {
                                if (_evtRearReaderOffline != null)
                                {
                                    _evtRearReaderOffline.SetAlarm(true);
                                    UpdateRearReaderMetaStatus();
                                }
                            }

                            return 0;
                        }
                    case CSC_API_ERROR.ERR_INTERNAL:
                        {
                            _IsReaderLoaded = false;
                            ResetCurrentLogicalMediaDataStructs();

                            if (exp.HRw == _hRw)
                            {
                                _cscReloader_FrontOrOnly_Failure.SetAlarm(true);
                                _cscReloader_FrontNRear_Failure.SetAlarm(true);

                                _cscReloaderMetaStatus.UpdateMetaStatus();
                                if (_cscReloaderMetaStatus.HasChangedSinceLastSave)
                                    Communication.SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                            }
                            else if (exp.HRw == _hRwRearReaderAtTokenDispenser)
                            {
                                // TODO
                            }
                            return 0;
                        }
                    default:
                        return 0;
                }
            }
            catch (Exception exp)
            {
                //SKS: added on 09-10-2014
                ResetCurrentLogicalMediaDataStructs();
                Logging.Log(LogLevel.Error, "Exception at TreatMessageReceived " + exp.Message.ToString());
                return 0;
            }
        }

        private void Handle_AgentLoggedOut()
        {
            SharedData._agentShift = null;
            _mediaUpdate = _mediaUpdateLast = null;
        }

        private void Handle_UseAntenna(int antennaId)
        {
            if (antennaId != 1 && antennaId != 2)
            {
                Logging.Log(LogLevel.Error, "Handle_UseAntenna antennaId = " + antennaId);
                return;
            }
            inst.StopField();
            Scenario scenario;
            if (antennaId == 1)
                scenario = Scenario.SCENARIO_1;
            else if (antennaId == 2)
                scenario = Scenario.SCENARIO_2;
            else
                return;

            try
            {
                inst.StopField(GetReaderHandleInvolvedWithTokenDispensing());
                inst.StartPolling(scenario, listener);
            }
            catch (ReaderException ex)
            {
                throw ex;
            }
        }

        private void Handle_DoOpForUnreadableCSC(string[] pars)
        {
            MediaOpType op = Utility.ParseEnum<MediaOpType>(pars[0]);
            string resultToSendToMMI = "";
            XElement root = XDocument.Parse(pars[1]).Root;
            bool bTest = Boolean.Parse(root.Element("TestTicket").Value);
            long physicalId = long.Parse(root.Element("PhysicalId").Value);
            int deposit = Int32.Parse(root.Element("Deposit").Value);
            int owner = Int32.Parse(root.Element("Owner").Value);
            int fareProduct = Int32.Parse(root.Element("FareProduct").Value);

            switch (op)
            {
                case MediaOpType.CSCSurrender:
                    resultToSendToMMI = DoOpForUnreadableCSC_CSCSurrender(pars[1], bTest, physicalId, deposit, owner, fareProduct);
                    break;
                case MediaOpType.DisbleAutoTopup:
                    resultToSendToMMI = DoOpForUnreadableCSC_DisbleAutoTopup(bTest, physicalId, deposit, owner, fareProduct);
                    break;
                case MediaOpType.SettleBadDebt:
                    resultToSendToMMI = DoOpForUnreadableCSC_SettleBadDebt(pars[1], bTest, physicalId, deposit, owner, fareProduct);
                    break;
            }
            SendMsg.DoOpForUnreadableCSCReply(resultToSendToMMI);
        }

        private string DoOpForUnreadableCSC_SettleBadDebt(string xml, bool bTest, long physicalId, int deposit, int owner, int fareProduct)
        {
            FldsCSCBadDebtCashPayment flds = new FldsCSCBadDebtCashPayment();
            XElement root = XDocument.Parse(xml).Root;
            flds.badDebtAmountSettled = Convert.ToInt32(root.Element("BadDebtAmountSettled").Value);

            string result;
            SmartFunctions.Instance.GetTDforCCHSUnreadable(TransactionType.CSC_BAD_DEBT_CASH_PAYMENT, ++SharedData.TransactionSeqNo, physicalId, owner, deposit, fareProduct, flds, out result);
            return result;

        }

        private string DoOpForUnreadableCSC_DisbleAutoTopup(bool bTest, long physicalId, int deposit, int owner, int fareProduct)
        {
            string result;
            SmartFunctions.Instance.GetTDforCCHSUnreadable(TransactionType.DisableBankTopup, ++SharedData.TransactionSeqNo, physicalId, owner, deposit, fareProduct, null, out result);
            return result;
        }

        private string DoOpForUnreadableCSC_CSCSurrender(string xml, bool bTest, long physicalId, int deposit, int owner, int fareProduct)
        {
            FldsCSCSurrendered flds = new FldsCSCSurrendered();
            XElement root = XDocument.Parse(xml).Root;
            flds._patronName = new PatronName_t(root.Element("PatronName").Value);
            flds._surrenderReason = Utility.ParseEnum<IFS2.Equipment.Common.CCHS.SurrenderReason_t>(root.Element("SurrenderReason").Value);
            flds._refundMethod = IFS2.Equipment.Common.CCHS.RefundMethod_t.Cash;
            flds._cscStatus = Utility.ParseEnum<IFS2.Equipment.Common.CCHS.CSC_StatusCode_t>(root.Element("StatusCode").Value);
            string result;
            SmartFunctions.Instance.GetTDforCCHSUnreadable(TransactionType.CSC_SURRENDERED, ++SharedData.TransactionSeqNo, physicalId, owner, deposit, fareProduct, flds, out result);
            return result;
        }

        private void Handle_UpdateMediaAbandonRollbackMode()
        {
            if (_mediaUpdateLast != null)
            {
                _mediaUpdateLast.DoOnRollbackingGettingCompletedOrAbandoned();
                _mediaUpdateLast = null;
            }
        }

        private void Handle_UpdateMediaRollbackOp()
        {
            if (_mediaUpdateLast == null)
            {
                SendMsg.UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode.NoLastTxn_SoNothingToDo);
                return;
            }
            _mediaUpdateLast.UpdateMediaRollbackOp();
        }


        private void Handle_MEDIANOTFOUNDFITFOROPERATIONACKED()
        {
            Debug.Assert(_mediaUpdate != null);
            if (_mediaUpdate != null)
                _mediaUpdate.MediaNotFoundFitForOperationAcked();
        }

        private void Handle_GIVEALLMEDIASERIALNUMSINVICINITY(EventMessage eventMessage)
        {
            int hRw = GetReaderHandleInvolvedWithTokenDispensing();

            SmartFunctions.Instance.StopField(hRw);

            List<long> _serialNums = new List<long>();
            bool bTokenDetected;
            do
            {
                bTokenDetected = MediaIssueTxn_TryToDetectMedia(Config.nToken_MAX_TRIALS_FOR_DETECTION);
                if (bTokenDetected)
                {
                    _serialNums.Add(SmartFunctions.Instance.ReadSNbr());
                    SmartFunctions.Instance.HaltCard(hRw);
                    SmartFunctions.Instance.StartPollingEx(SmartFunctions.Instance.GetActiveScenario(), null, hRw);
                }
            } while (bTokenDetected);
            Communication.SendMessage(ThreadName, "", "AllMediaSerialNumsInVicinityAnswer", SerializeHelper<List<long>>.XMLSerialize(_serialNums));
        }

        private void Handle_GetRearReaderStatus()
        {
            if (_evtRearReaderMetaStatus == null)
            {
                Debug.Assert(false);
                return;
            }
            Communication.SendMessage(ThreadName, "", "GetRearReaderStatusAnswer", GetStatusRearReader());
        }

        private void Handle_UpdateTTagCancel()
        {
            _ttagUpdateRequest = null;
        }

        class UpdateTTagRequest
        {
            public TTagOps _op;
            public int _agentId;
            public short? _cntTokens = null;
            public int? _serialNumber = null;
            public bool _bForceWriteEvenIfMediaIsNotTTag;

            public UpdateTTagRequest(bool bForceWriteEvenIfMediaIsNotTTag, TTagOps op, int agentId, string additionalParamsXml)
            {
                _bForceWriteEvenIfMediaIsNotTTag = bForceWriteEvenIfMediaIsNotTTag;
                _op = op;
                _agentId = agentId;

                if (additionalParamsXml != "")
                {
                    XDocument doc = XDocument.Parse(additionalParamsXml);
                    XElement elem = doc.Root.Element("CntTokens");
                    if (elem != null)
                        _cntTokens = Convert.ToInt16(elem.Value);
                    elem = doc.Root.Element("SerialNumber");
                    if (elem != null)
                        _serialNumber = Convert.ToInt32(elem.Value);
                }
            }
        }

        private void ResetCurrentLogicalMediaDataStructs()
        {
            _logMediaReloader.Reset();
            _logMediaToken.Reset();
            hwCsc.Reset();
        }

        private void Handle_GetTokenDispenserStatusAnswer(string statusXml)
        {
            XDocument doc = XDocument.Parse(statusXml);
            XElement root = doc.Root;
            AlarmStatus al = (AlarmStatus)(Convert.ToInt32(root.Element("MetaStatus").Value));
            SharedData.IsDispenserAvailable = (al == AlarmStatus.Normal || al == AlarmStatus.Warning);
        }

        private void CheckMediaUpdateForConclusion()
        {
            Logging.Log(LogLevel.Verbose, " CheckMediaUpdateForConclusion Result = " + _mediaUpdate.IsConcluded().ToString());
            if (_mediaUpdate.IsConcluded())
            {
                SetMediaUpdateConcluded();
            }
        }

        private void SetMediaUpdateConcluded()
        {
            _mediaUpdateLast = _mediaUpdate;
            _mediaUpdate = null;
        }

        private void Handle_UpdateMediaDeclarePartCompletedAsDone(EventMessage eventMessage)
        {
            if (_mediaUpdate == null)
                return;

            int opIdx = Convert.ToInt32(eventMessage._par[0]);
            int opSubIdx = Convert.ToInt32(eventMessage._par[1]);
            long ticketPhysicalId = Convert.ToInt64(eventMessage._par[2]);

            _mediaUpdate.DeclarePartCompletedAsDone(opIdx, opSubIdx, ticketPhysicalId); // Restarting the polling if required, is taken care by the function itself
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

        private void ClearDataStructuresOnMediaRemoval()
        {
            _logMediaToken.Reset();
            _logMediaReloader.Reset();
            hwCsc.Reset();
            hwToken.Reset();
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
            _errorCurMedia = TTErrorTypes.NoError;
            _adjustment = null;
            _bTicketDataStructuresSynchedWithTicketData = true;
        }

        private void Handle_CancelPutTokenUnderRWAck()
        {
            Debug.Assert(SharedData.EquipmentType == EquipmentFamily.TOM);
            if (_mediaUpdate != null)
            {
                Debug.Assert(_mediaUpdate.IsAbortRequested());
                if (!_mediaUpdate.IsAbortRequested())
                    return;
                SetMediaUpdateConcluded();
                SendMsg.UpdateMediaTerminated();
            }
        }

        private bool _bTicketDataStructuresSynchedWithTicketData = true;

        private void Handle_StopMediaDistribution()
        {
            if (_curMediaDistributionTxn == null)
            {
                Logging.Log(LogLevel.Information, "Handle_StopTokenDistribution _curTokenTransaction == null");
                return;
            }
            if (_curMediaDistributionTxn._bPutMediaUnderRWSent)
            {
                switch (_curMediaDistributionTxn._mediaType)
                {
                    case MediaDistributionTransaction.MediaType.Token:
                        SendMsg.AskTDToThrow_ToBeDispensed_Or_JustDispensed_TokenToBin();
                        break;
                    case MediaDistributionTransaction.MediaType.CSC:
                        break;
                }
            }


            // We're taking 3 services from TD.
            // 1. PutTokenUnderRW. It is treated below explicitly
            // 2. Throw token to OT. If we are waiting for its ack, we can safely abort, because we've already informed MMI that token has been distributed
            // 3. Throw token to bin. Here too we can abort the transaction.            

            MediaIssueTxn_Abort(ReasonForAbort.StopRequestedFromMMI);
        }

        private void Handle_DetailsOfMediaToBeRead(EventMessage eventMessage)
        {
            int? _readDataForAddVal_RechargeValueRequestedEarlier = _readDataForAddVal_RechargeValueRequested;
            int? _readDataForPutNewProductInExistingMedia_ProductTypeRequestedEarlier = _readDataForPutNewProductInExistingMedia_ProductTypeRequested;

            MediaDetectionTreatment readDataFor = (MediaDetectionTreatment)(Convert.ToInt32(eventMessage._par[0]));

            if (eventMessage._par.Length >= 2)
            {
                switch (readDataFor)
                {
                    case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                        _readDataForAddVal_RechargeValueRequested = Convert.ToInt32(eventMessage._par[1]);
                        break;
                    case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia:
                        _readDataForPutNewProductInExistingMedia_ProductTypeRequested = Convert.ToInt32(eventMessage._par[1]);
                        break;
                }
            }
            else
            {
                _readDataForAddVal_RechargeValueRequested = null;
                _readDataForPutNewProductInExistingMedia_ProductTypeRequested = null;
            }

            if (readDataFor == _readDataFor
                && (
                (_readDataFor == MediaDetectionTreatment.TOM_AnalysisForAddVal && _readDataForAddVal_RechargeValueRequestedEarlier != _readDataForAddVal_RechargeValueRequested)
             || (_readDataFor == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia && _readDataForPutNewProductInExistingMedia_ProductTypeRequestedEarlier != _readDataForPutNewProductInExistingMedia_ProductTypeRequested)
                ))
            {
                _errorCurMedia = TTErrorTypes.NoError;
                _adjustment = null;

                _reader.StartPolling();
                return;
            }

            if (_MediaDetectedState != SmartFunctions.MediaDetected.NONE && !_bTicketDataStructuresSynchedWithTicketData)
            {
                // TODO: It implies that after operation on media, as soon as we return to the home state, we read the media again, which is typically unneccessary. See if some workaround can be done
                ClearDataStructuresForMediaKeptOnRW();
            }

            SetReadPurpose(readDataFor);
        }


        private void Handle_StartMaintenanceTokenDistribution(EventMessage eventMessage)
        {
            // TODO: find how in CS22, mac calc was disabled for maintainence tokens
            var pars = eventMessage._par;
            if (IsMediaDistributionInProgress())
            {
                SendMsg.TokenError();
                return;
            }

            int cntMediaAsked = Convert.ToInt32((pars[0].Split(';'))[0]);
            _curMediaDistributionTxn = new MediaDistributionTransaction(MediaDistributionTransaction.MediaType.Token, cntMediaAsked);
            _curMediaDistributionTxn._logicalData_FromMMI = pars[1];

            Logging.Log(LogLevel.Verbose, "StartMaintenanceToken nToknesRequested = " + _curMediaDistributionTxn._cntMediaRequested
                + " logical data = " + _curMediaDistributionTxn._logicalData_FromMMI);


            SmartFunctions.Instance.StopField(_hRw);

            Message("StartTokenDistribution", "", "STARTMEDIADISTRIBUTIONINTERNAL", null);

        }

        private void Handle_UndetectableTokenRemoved(EventMessage eventMessage)
        {
            Debug.Assert(_mediaUpdate != null);

            if (_mediaUpdate == null)
            {
                Logging.Log(LogLevel.Error, "Can't handle UndetectableTokenRemoved, because _mediaUpdate is null");
                return;
            }

            _mediaUpdate.NonDetectableMediaRemoved();
        }

        private const int DefaultTimeoutForUpdateOpInSec = 5 * 60;

        private void Handle_TokenDispenserMetaStatus(EventMessage eventMessage)
        {
            AlarmStatus status = (AlarmStatus)(Convert.ToInt32(eventMessage._par[0]));
            SharedData.IsDispenserAvailable = (status == AlarmStatus.Normal || status == AlarmStatus.Warning);

            if (!SharedData.IsDispenserAvailable)
            {
                if (_curMediaDistributionTxn != null)
                {
                    MediaIssueTxn_Abort(ReasonForAbort.TokenError);

                    if (status == AlarmStatus.Alarm
                        && Config._AttemptToResetTokenDispenserOnOutJam
                        && SharedData.EquipmentStatus == EquipmentStatus.InService // because we don't want to do such "dumb smartness", while agent is working.
                        )
                    {
                        XDocument doc = XDocument.Parse(eventMessage._par[1]);
                        if (doc.Root.Element("Status").Element("HasIncident").Value == "1")
                            SendTDToResetIfTokenNotThere();
                    }
                }
            }
        }

        private void SendTDToResetIfTokenNotThere()
        {
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
            Timers timer = (Timers)nTimerId;
            Logging.Log(LogLevel.Verbose, "Ticked " + timer.ToString());
            if (!IsTimerActive(timer))
            {
                Logging.Log(LogLevel.Information, "Timer " + timer.ToString() + " is not active");
                return;
            }

            StopTimer(timer);

            switch (timer)
            {
                case Timers.EnoughTimeElapsedSinceLastMediaWasKeptInExhaustiveDetectionRemovalInLoginMode:
                    {
                        Debug.Assert(_reader != null);
                        StopTimer(Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly);
                        _reader.SetState(ReaderOp.SwitchToDetectRemoval_ContentWithNonRealTime);
                        break;
                    }
                case Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved:
                    {
                        ((V3Reader)_reader).Timeout_TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved();
                        break;
                    }
                case Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly:
                    {
                        ((V3Reader)_reader).CheckForMediaRemoved();
                        break;
                    }
                case Timers.PutMediaOverRW:
                    {
                        if (SharedData.EquipmentType != EquipmentFamily.TOM)
                        {
                            Debug.Assert(false); // Token dispenser should have responded yet?? hence assert.
                            _curMediaDistributionTxn._sender.MediaDistributionHaltedDueToSomeProblem();
                            _curMediaDistributionTxn = null;

                            MediaIssueTxn_RestorePollingAtFront();
                        }
                        else if (SharedData.EquipmentType == EquipmentFamily.TOM)
                        {
                            if (_mediaUpdate != null)
                            {
                                bool bCurOpInvolvesTokenDispensr = _mediaUpdate.DoesCurrentOpInolvesTokenDispenser();

                                // Ideally, we should not reach here. We are here only when the token dispenser driver has just halted or reached some inconsitant state.
                                // Okay, for this transaction, we can gracefully ask MMI that we are aborting it. Not taking hassles that it is a multi-vending, and
                                // there are some non-token dispensing operations queued, because multi-vending is itself rare.
                                // TODO: How to communicate to MMI that don't submit more token dispensing operations??
                                if (!bCurOpInvolvesTokenDispensr)
                                {
                                    Debug.Assert(false);
                                    return;
                                }
                                SendMsg.UpdateMediaTerminated();
                                SetMediaUpdateConcluded();
                                //PutReaderInDefaultModeOfOperation();
                            }
                        }
                        break;
                    }
                case Timers.ThrowMedia:
                    {
                        Debug.Assert(false); // Token dispenser should have responded yet?? hence assert.
                        if (_curMediaDistributionTxn != null)
                            MediaIssueTxn_Abort(ReasonForAbort.TokenError);

                        break;
                    }
                case Timers.TimeoutCancelPutMediaOverAntennaAckNotRecvd:
                    {
                        // it is somewhat bad that timer is being managed by TTMain, while some stuff is by UpdateMedia.
                        Logging.Log(LogLevel.Error, "Timers.TimeoutCancelPutTokenUnderRWAckNotRecvd ticked");
                        if (_mediaUpdate != null && _mediaUpdate.IsAbortRequested())
                        {
                            SendMsg.UpdateMediaTerminated();
                            SetMediaUpdateConcluded();
                        }

                        StatusCSC status;
                        SmartFunctions.Instance.SmartSyncDetectOkPassive(out status);
                        if (status.ucStatCSC == CONSTANT.ST_INIT)
                            StartPolling(GetScenarioForTokenDispensing());
                        break;
                    }
                case Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck:
                    {
                        if (_mediaUpdate != null)
                            _mediaUpdate.NoMediaDetectedWithinAskedTimeFrame();

                        break;
                    }
                case Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList:
                    {
                        //if (_mediaUpdate != null)
                        if (_reader != null)
                            _reader.ClearIgnoreList();
                        break;
                    }
                case Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE:
                    {
                        Logging.Log(LogLevel.Verbose, "TimoutMediaStillInFieldPostRTEOrWTE A1");
                        if (_mediaUpdate != null)
                        {
                            Logging.Log(LogLevel.Verbose, "TimoutMediaStillInFieldPostRTEOrWTE A2");
                            var curOp = _mediaUpdate.GetCurrentOp();
                            if (curOp != null)
                            {
                                Logging.Log(LogLevel.Verbose, "TimoutMediaStillInFieldPostRTEOrWTE A3");

                                _reader.StopPolling();
                                if (_reader is ThalesReader)
                                    ((ThalesReader)_reader).StopField();
                                _reader.StartPolling();
                            }
                        }
                        break;
                    }
            }
        }

        public enum Timers
        {
            PutMediaOverRW,
            ThrowMedia,
            TimeoutCancelPutMediaOverAntennaAckNotRecvd,
            NoMediaDetectedPost_Positive_PutMediaUnderRWAck,
            Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList,
            TimoutMediaStillMaybeInFieldPostRTEOrWTE,
            TimerV3Reader_CheckForMediaRemoved_Aggressivly,
            TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved,
            EnoughTimeElapsedSinceLastMediaWasKeptInExhaustiveDetectionRemovalInLoginMode
        }

        bool _bIsTimerActive_TimeoutNoMediaDetectedPostLastMediaWasHalted = false;
        bool _bIsTimerActive_NoTokenDetectedPost_Positive_PutTokenUnderRWAck = false;
        bool _bTimerActive_MediaStillMaybeInFieldPostRTEOrWTEActive = false;
        bool _bTimerActive_TimerV3Reader_CheckForMediaRemovedAggresivly = false;
        bool _bTimerActive_TimerV3Reader_CheckForMediaRemovedNonAggresivly = false;

        public bool IsTimerActive(Timers timer)
        {
            switch (timer)
            {
                case Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly:
                    return _bTimerActive_TimerV3Reader_CheckForMediaRemovedAggresivly;
                case Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved:
                    return _bTimerActive_TimerV3Reader_CheckForMediaRemovedNonAggresivly;
                case Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList:
                    return _bIsTimerActive_TimeoutNoMediaDetectedPostLastMediaWasHalted;
                case Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck:
                    return _bIsTimerActive_NoTokenDetectedPost_Positive_PutTokenUnderRWAck;
                case Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE:
                    return _bTimerActive_MediaStillMaybeInFieldPostRTEOrWTEActive;
                default:
                    return true;
            }
        }

        public void StartTimer(Timers timer)
        {
            int delayInMilliSec;
            switch (timer)
            {
                case Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList:
                    delayInMilliSec = Config.nTimeInMilliSecToKeepMediaHaltedAfterDisappearance;
                    _bIsTimerActive_TimeoutNoMediaDetectedPostLastMediaWasHalted = true;
                    break;
                case Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck:
                    delayInMilliSec = Config._nTimeInMilliSecToLetTokenGettingDetectedAfter_PutTokenUnderRWPositiveAck;
                    _bIsTimerActive_NoTokenDetectedPost_Positive_PutTokenUnderRWAck = true;
                    break;
                case Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE:
                    delayInMilliSec = Config.nMaxTimeInMilliSecBeforeRestartingFieldInCaseOfWTEOrRTE;
                    _bTimerActive_MediaStillMaybeInFieldPostRTEOrWTEActive = true;
                    break;
                case Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly:
                    delayInMilliSec = 1; // bad programming. but 1 millisec is really the intended value. Because here it doesn't even signifies gap between polling. That is taken care by a sleep after PollForAnyMediaAtMoment_AndPerformActionOnIt_IfNonePresentThenStopPolling.
                    _bTimerActive_TimerV3Reader_CheckForMediaRemovedAggresivly = true;
                    break;
                case Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved:
                    delayInMilliSec = 6000;
                    _bTimerActive_TimerV3Reader_CheckForMediaRemovedNonAggresivly = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
            StartDelay((int)timer, delayInMilliSec);
        }

        public void StopTimer(Timers timer)
        {
            StopDelay((int)timer);

            switch (timer)
            {
                case Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList:
                    _bIsTimerActive_TimeoutNoMediaDetectedPostLastMediaWasHalted = false;
                    break;
                case Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck:
                    _bIsTimerActive_NoTokenDetectedPost_Positive_PutTokenUnderRWAck = false;
                    break;
                case Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE:
                    _bTimerActive_MediaStillMaybeInFieldPostRTEOrWTEActive = false;
                    break;
                case Timers.TimerV3Reader_CheckForMediaRemoved_Aggressivly:
                    _bTimerActive_TimerV3Reader_CheckForMediaRemovedAggresivly = false;
                    break;
                case Timers.TimerV3Reader_InNonAggressiveMode_TooMuchTimeElapsed_AndHaltedMediaMustHaveGotRemoved:
                    _bTimerActive_TimerV3Reader_CheckForMediaRemovedNonAggresivly = false;
                    break;
            }
        }

        private string ConstructCscStatus()
        {
            try
            {
                //Construct the XML string
                var sb = new StringBuilder();
                XmlWriter xmlWriter = XmlWriter.Create(sb, contextFile_XMLWriterSettings);
                xmlWriter.WriteStartElement("CSCReloader");
                string alarms = "IsOffLine;OutOfOrder";
                if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                {
                    alarms += ";RearOOO";
                }
                IFSEventsList.ComposeXMLForStatus(xmlWriter, "CSCReloaderDriver", "MetaStatus", alarms, "SerialNumber;CSCAPIVersion;FirmwareVersion;CSCChargeurVersion");
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

        //DateTime _LastPingAttempt = new DateTime(2000, 1, 1);

        // TODO: Take care that for TVM, they must be _lastEncounteredMedias on back antenna only
        public override void RepetitiveTreatment()
        {
            try
            {
                if (Reader.LastErrorCode != (int)CSC_API_ERROR.ERR_NONE)
                {
                    switch (Reader.LastErrorCode)
                    {
                        case (short)CSC_API_ERROR.ERR_DEVICE:
                        case (short)CSC_API_ERROR.ERR_COM:
                        case (short)CSC_API_ERROR.ERR_LINK:
                            {
                                _IsReaderLoaded = false;
                                break;
                            }
                    }
                }

                if (_IsReaderLoaded)
                {
                    if (!Config._bUseCallbackForMediaDetectionNRemoval)
                        CheckForMediaRelatedActivity();
                    else
                    {
                        // Periodic check-up
                        CSC_API_ERROR err = Reader.PingReader(_ReaderType, _hRw);
                        if (err == CSC_API_ERROR.ERR_NONE)
                        {
                            _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(false);
                            //_timeStampWhenRearReaderWasPingedLast = DateTime.Now;
                        }
                        else if (err == CSC_API_ERROR.ERR_TIMEOUT // this code is observed
                            || err == CSC_API_ERROR.ERR_LINK || err == CSC_API_ERROR.ERR_COM // Putting based on guess; hence can remove them
                            )
                        {
                            _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(true);
                            _IsReaderLoaded = false;
                        }

                        _cscReloaderMetaStatus.UpdateMetaStatus();
                        if (_cscReloaderMetaStatus.HasChangedSinceLastSave)
                        {
                            IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                            SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                        }
                    }
                }
                else
                {
                    ReloadReader();
                    if (_IsReaderLoaded)
                        if (_reader != null && _readDataFor != MediaDetectionTreatment.None)
                            _reader.StartPolling();
                }
                if (_bMediaDispensingUsingRearReader)
                {
                    bool bRearReaderOfflineStatusEarlier = _evtRearReaderOffline.Activated;

                    if (!_IsRearReaderLoaded)
                        ReloadRearReader();

#if !WindowsCE  && !MonoLinux
                    if (_IsRearReaderLoaded)
                    {
                        if (DateTime.Now - _timeStampWhenRearReaderWasPingedLast > Config.TimeSpanPingRearReader)
                        {
                            CSC_API_ERROR err = Reader.PingReader(CSC_READER_TYPE.V4_READER, _hRwRearReaderAtTokenDispenser);
                            if (err == CSC_API_ERROR.ERR_NONE)
                            {
                                _evtRearReaderOffline.SetAlarm(false);
                                _timeStampWhenRearReaderWasPingedLast = DateTime.Now;
                            }
                            else if (err == CSC_API_ERROR.ERR_TIMEOUT // this code is observed
                                || err == CSC_API_ERROR.ERR_LINK || err == CSC_API_ERROR.ERR_COM // Putting based on guess; hence can remove them
                                )
                            {
                                _evtRearReaderOffline.SetAlarm(true);
                                _IsRearReaderLoaded = false;
                            }
                        }
                    }
#endif

                    _evtRearReaderMetaStatus.UpdateMetaStatus();
                    if (_evtRearReaderMetaStatus.HasChangedSinceLastSave || !_bRearReaderStatusBroadcastOnce)
                    {
                        IFSEventsList.SaveContext(moduleRearReader);
                        SendMetaStatusRearReader();
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Main TT RepetiveTtreatment " + e.Message);
            }
            finally
            {
                //_bReadDataForChanged = false;
                base.RepetitiveTreatment();
            }
        }

        private void CheckForMediaRelatedActivity()
        {
            if ((_bMediaDispensingUsingRearAntennaOfPrimaryReader || _bMediaDispensingUsingRearReader) && IsMediaDistributionInProgress())
            {
                return;
            }

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
                 && (mediaDetected == SmartFunctions.MediaDetected.NONE
                || !bSameMedia))
            {
                ClearDataStructuresOnMediaRemoval();
            }

            if (mediaDetected != SmartFunctions.MediaDetected.NONE)
            {
                #region CommonForAllMedia

                if (bSameMedia == true)
                {
                    Logging.Log(LogLevel.Verbose, "Same media");
                    return;
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

                    if (inst.GetActiveScenario() == Scenario.SCENARIO_2)
                    {
                        bool bActivatedEarlier = _evtRearReaderOOO.Activated;
                        _evtRearReaderOOO.SetAlarm(false);
                        if (bActivatedEarlier)
                        {
                            IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                            Communication.SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                        }
                    }
                    //if (SmartFunctions.Instance.ActiveScenario == Scenario.SCENARIO_2)
                    //{
                    //    _evtRearReaderOOO.SetAlarm(false);
                    //    if (IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver"))
                    //    {
                    //        SendMsg.GetCSCReloaderStatusAnswer(ConstructCscStatus(), _cscReloaderMetaStatus);
                    //    }
                    //}
                }
                #endregion
                #region Card
                if (mediaDetected == SmartFunctions.MediaDetected.CARD)
                {

                    if (Config._bTreatementOnCardDetectionOldStyle == true)
                    {
                        _MediaDetectedState = SmartFunctions.MediaDetected.CARD; // just in case TreatmentOnCardDetection throws exception
                        var err = TreatmentOnCardDetection(_logMediaReloader, _readDataFor);
                        switch (err)
                        {
                            case TTErrorTypes.NoError:
                                {
                                    _MediaDetectedState = SmartFunctions.MediaDetected.CARD;

                                    SwitchToDetectionRemoval();
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
                                    // TODO: Treatment is debatable, and can be changed to simply SmartFunctions.Instance.SwitchToDetectionRemoval();
                                    /*   HaltCurrentCardNRestartPolling();

                                       _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
                                       
                                       SharedData.MediaTypeDetected = MediaType.None;*/
                                    _MediaDetectedState = SmartFunctions.MediaDetected.CARD;
                                    _logMediaReloader.Reset();
                                    hwCsc.Reset();
                                    SwitchToDetectionRemoval();
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

                                    StartPolling(Scenario.SCENARIO_1);

                                    _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
                                    _logMediaReloader.Reset();
                                    hwCsc.Reset();

                                    break;
                                }
                        }
                    }
                    else
                    {
                        _MediaDetectedState = SmartFunctions.MediaDetected.CARD;
                        TreatmentOnCardDetection2();
                        // TODO: One improvement that can be done is that in case of cannot read from card, make repetititive almost immediate.

                        SwitchToDetectionRemoval();
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
                                if (_ttagUpdateRequest != null)
                                {
                                    TTErrorTypes err = AttemptTTagUpdate();
                                    if (err == TTErrorTypes.NoError)
                                    {
                                        SendMsg.UpdateTTagAnswer_Success(_logMediaToken.TTag);
                                        _ttagUpdateRequest = null;
                                    }
                                    else
                                    {
                                    }
                                    SwitchToDetectionRemoval();

                                    return;
                                }

                                string s = _logMediaToken.ToXMLString();
                                Logging.Log(LogLevel.Verbose, "TOken Data: " + s);
                                if (!_logMediaToken.TTag.Hidden)
                                    Communication.SendMessage(ThreadName, "Message", "TTagDetection", s);
                                else
                                {
                                    if (0 < _logMediaToken.Initialisation.ServiceProviderRead && _logMediaToken.Initialisation.ServiceProviderRead < 4)
                                        Communication.SendMessage(ThreadName, "Message", "CSTMediaDetection", Convert.ToString((int)TTErrorTypes.NoError), s);
                                    else
                                        Communication.SendMessage(ThreadName, "Message", "CSTMediaDetection", Convert.ToString((int)TTErrorTypes.MediaNotSold), s);
                                }
                                SwitchToDetectionRemoval();
                            }
                            else
                            {
                                Logging.Log(LogLevel.Verbose, "Repetitive treatment:Same Token is there...");

                                StartPolling(Scenario.SCENARIO_1);

                                _logMediaToken.Reset();
                                hwToken.Reset();
                            }
                        }
                        else
                        {
                            Logging.Log(LogLevel.Verbose, "Repetitive treatment: Token is not there...");

                            StartPolling(Scenario.SCENARIO_1);
                            _logMediaToken.Reset();
                            hwToken.Reset();
                        }
                    }
                    else
                    {
                        _MediaDetectedState = SmartFunctions.MediaDetected.TOKEN;
                        TreatmentOnCardDetection2();

                        SwitchToDetectionRemoval();
                    }
                }
                #endregion
            }
            #region NONEDetected
            else
            {
                // Same info in two variables. Smelling bad.
                _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
                // We are here itself implies that no token transaction (vending) is in process.
                if (_bAtLeastOneMediaHalted)
                {
                    _bAtLeastOneMediaHalted = false;
                    RestartField();
                }
            }
            #endregion
            return;
        }

        public void SwitchToDetectionRemoval()
        {
            Debug.Assert(_reader == null);

            SmartFunctions.Instance.SwitchToDetectRemovalState();
            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
                hwCsc.ResetSelectedAppId();
        }

        public void GetReaderHandle(out CSC_READER_TYPE readerType, out int hRw)
        {
            readerType = _ReaderType;
            hRw = _hRw;
        }

        public void StartPolling(Scenario scenario)
        {
            Debug.Assert(_reader == null);

            SmartFunctions.Instance.StartPolling(scenario, SmartFunctions.Instance.listenerCardProduced);
            _MediaDetectedState = SmartFunctions.MediaDetected.NONE;
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
                Err = Reader.ReloadReader(_ReaderType, _ReaderComm, out _hRw, out _FirmwareInfo);

                Logging.Log(LogLevel.Verbose, ThreadName + "ReloadReader : Err State is : " + Convert.ToString((int)(CSC_API_ERROR)Err));

                if (Err == CSC_API_ERROR.ERR_NONE)
                {
                    SmartFunctions.Instance.SetReaderType(_ReaderType, _hRw);
                    // SKS:fill CSC firmware version info
                    _firmwareVersion.SetEvent(_FirmwareInfo.AppCSC);
                    _cscChargeurVersion.SetEvent(_FirmwareInfo.Chargeur);
                    _cscAPIVersion.SetEvent(SharedData.cscApiVersion);
                    //Load the Crypto Instance
                    cFlex = null;
                    if (_cryptoflexSAMUsage)
                        cFlex = new CryptoFlexFunctions(_ReaderType, _hRw);
                    //SecurityMgr.Instance.SetTokenKey(0, new byte[]{0, 1, 2, 3, 4, 5, 6, 7});
                    if (_delhiCCHSSAMUsage)
                    {
                        mCCHSSAMMgr = new CCHSSAMManger(_ReaderType, _hRw);
                        Err = ResetCCHSSAM();

                        Logging.Log(Err == CSC_API_ERROR.ERR_NONE ? LogLevel.Verbose : LogLevel.Error, "ResetCCHSSAM returned " + Err.ToString());
                    }

                    bool bStartPolling = (_reader == null);
                    SmartFunctions.Instance.Init(bStartPolling);

                    _IsReaderLoaded = true;
                    if (Err == CSC_API_ERROR.ERR_NONE)
                    {
                        _dataSecurityModuleFailure.SetAlarm(false);
                        _dataSecurityModuleIsOffLine.SetAlarm(false);
                        _dataSecurityModuleLocked.SetAlarm(false);
                        _dataSecurityModuleAusent.SetAlarm(false);
                    }
                    if (_delhiCCHSSAMUsage && Err != CSC_API_ERROR.ERR_NONE)
                    {
                        switch ((int)mCCHSSAMMgr.mCCHSSAM_Status)
                        {
                            case (int)CONSTANT.SAMErrors.SM_AUTHENTICATION_FAILURE:
                            case (int)CONSTANT.SAMErrors.SM_FAILURE:
                            case (int)CONSTANT.SAMErrors.SM_INIT_ERROR:
                                _dataSecurityModuleFailure.SetAlarm(true);
                                break;
                            case (int)CONSTANT.SAMErrors.SM_SAMLOCKED_ERROR:
                                _dataSecurityModuleLocked.SetAlarm(true);
                                break;
                            case (int)CONSTANT.SAMErrors.SM_LINK_FAILURE:
                                _dataSecurityModuleIsOffLine.SetAlarm(true);
                                _dataSecurityModuleAusent.SetAlarm(true);
                                break;
                        }
                    }

                    RoutineStuffForDSMMetaStatus();

                    _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(false); _cscReloader_FrontNRear_OffLine.SetAlarm(false);
                    _cscReloader_FrontOrOnly_Failure.SetAlarm(false); _cscReloader_FrontNRear_Failure.SetAlarm(false);
                    if (_bMediaDispensingUsingRearAntennaOfPrimaryReader)
                    {
                        Debug.Assert(_evtRearReaderOffline != null);
                        Debug.Assert(_evtRearReaderOOO != null);

                        if (_evtRearReaderOffline != null)
                            _evtRearReaderOffline.SetAlarm(false);
                    }
                }
                else if (Err == CSC_API_ERROR.ERR_DEVICE || Err == CSC_API_ERROR.ERR_LINK || Err == CSC_API_ERROR.ERR_COM || Err == CSC_API_ERROR.ERR_TIMEOUT)
                {
                    _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(true); _cscReloader_FrontNRear_OffLine.SetAlarm(true);
                    if (_evtRearReaderOffline != null)
                        _evtRearReaderOffline.SetAlarm(true);
                    Logging.Log(LogLevel.Error, ThreadName + "ReloadReader : Reader is Offline");
                }
                else
                {
                    _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(false); _cscReloader_FrontNRear_OffLine.SetAlarm(false);
                    _cscReloader_FrontOrOnly_Failure.SetAlarm(true); _cscReloader_FrontNRear_Failure.SetAlarm(true);

                    if (_evtRearReaderOffline != null)
                        _evtRearReaderOffline.SetAlarm(false);
                    Logging.Log(LogLevel.Error, ThreadName + "ReloadReader : Reader is Offline");
                }
            }
            catch (Exception e1)
            {
                _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(true); _cscReloader_FrontNRear_OffLine.SetAlarm(true);

                if (_evtRearReaderOffline != null)
                    _evtRearReaderOffline.SetAlarm(true);
                Logging.Log(LogLevel.Error, "Error in Reloading the Reader" + e1.Message);
            }
            try
            {
                _cscReloaderMetaStatus.UpdateMetaStatus();
                _dataSecurityModuleMetaStatus.UpdateMetaStatus(); // does _cscReloaderMetaStatus encompasses _dataSecurityModuleMetaStatus too??

                if (_cscReloaderMetaStatus.HasChangedSinceLastSave || bFirstCall)
                {
                    SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                    //SetRepetitiveTreatmentInterval(_cscReloaderMetaStatus.Value == (int)(AlarmStatus.Alarm) 
                    //    || _cscReloaderMetaStatus.Value == (int)AlarmStatus.NotConnected ? _frequencyToAttemptReloadReaderInDisconnection * 1000 : GetDefaultRepetitiveTreatmentInterval());
                }
                bFirstCall = false;

                IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");
            }
            catch (Exception e1)
            {
                _cscReloader_FrontOrOnly_IsOffLine.SetAlarm(true); _cscReloader_FrontNRear_OffLine.SetAlarm(true);
                if (_evtRearReaderOffline != null)
                    _evtRearReaderOffline.SetAlarm(true);
                if (_cscReloaderMetaStatus.HasChangedSinceLastSave || bFirstCall)
                {
                    SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)_cscReloaderMetaStatus.Value), ConstructCscStatus());
                }
                bFirstCall = false;

                Logging.Log(LogLevel.Error, "Error in Reloading the Reader" + e1.Message);
            }
            return _IsReaderLoaded;
        }

        void RoutineStuffForDSMMetaStatus()
        {
            _dataSecurityModuleMetaStatus.UpdateMetaStatus();
            bool bMetaStatusAffected = _dataSecurityModuleMetaStatus.HasChangedSinceLastSave;
            IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");

            // Sending CSCReloaderMetaStatus is scattered at several places. So, sending meta status of DSM unconditionally. Also, it should be broadcast after every reconnection of CSC R/W.
            SendMetaStatusDSM();
        }

        private void SendMetaStatusDSM()
        {
            // TODO: for now, we send only one parameter, because client (mmi) is not interested in anything else
            Communication.SendMessage(ThreadName, "", "SAMMetaStatus", ((int)_dataSecurityModuleMetaStatus.Value).ToString());
        }

        private CSC_API_ERROR ResetCCHSSAM()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOT_AVAIL;
            //Install SAM1 Card

            foreach (cSAMConf smcnf in SharedData.mSAMUsed)
            {
                if (smcnf.mSAMType == CONSTANT.SAMType.THALES_SAM)
                {
                    InstallCard pSamCardParams = new InstallCard();

                    pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
                    pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)(DEST_TYPE.DEST_SAM1);
                    pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T0;
                    pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
                    pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);//CHECK +1 REMOVED IN AVM-TT            

                    Err = Reader.InstallCard(_ReaderType,
                              _hRw,
                              (DEST_TYPE)smcnf.SAM_Slot,
                              pSamCardParams);
                }
                else if (smcnf.mSAMType == CONSTANT.SAMType.ISAM) //CCHS SAM
                {
                    //CCHSSAMManger mCCHSSAMMgr = new CCHSSAMManger(pReaderType, phRw);
                    if (mCCHSSAMMgr == null)
                    {
                        Logging.Log(LogLevel.Error, "CCHS SAM Object not initialized");
                        return CSC_API_ERROR.ERR_DEVICE;
                    }
                    /*      Err = (CSC_API_ERROR)mCCHSSAMMgr.SAMInstallCard((DEST_TYPE)smcnf.SAM_Slot);
                          if (Err == CSC_API_ERROR.ERR_NONE)
                          {
                              /// Configure CCHS SAM and Switch to mode 2
                              Err = mCCHSSAMMgr.ConfigCCHSSAM((DEST_TYPE)smcnf.SAM_Slot);
                              SharedData.TransactionSeqNo = mCCHSSAMMgr.TxnSeqenceNo;
                              //TODO: to be checked whether we need sam status info.... now ??? it is already avaialble with CCHSSAM Manager class
                          }
                     */

                    Err = mCCHSSAMMgr.ResetCCHSSAM((DEST_TYPE)smcnf.SAM_Slot, _readDeviceIDInCCHSSAM);
                    switch (Err)
                    {
                        case CSC_API_ERROR.ERR_NONE:
                            {
                                if (_signatureAtEachTransaction) SharedData.TransactionSeqNo = mCCHSSAMMgr.TxnSeqenceNo;
                                SharedData.mDSMId = mCCHSSAMMgr.DSMId;
                                SharedData.CompanyID = mCCHSSAMMgr.mCCHSStatusInfo.ServiceProvider;
                                cCCHSSAMTokenKey ultralightTokenKey = null;
                                mCCHSSAMMgr.GetTokenKey((DEST_TYPE)smcnf.SAM_Slot, 0, out ultralightTokenKey);
                                SecurityMgr.Instance.SetTokenKey(0, ultralightTokenKey.TokenKey);

                                break;
                            }
                        case CSC_API_ERROR.ERR_DATA:
                        case CSC_API_ERROR.ERR_DEVICE:
                            {
                                _dataSecurityModuleFailure.SetAlarm(true);
                                break;
                            }
                        case CSC_API_ERROR.ERR_TIMEOUT:
                            {
                                _dataSecurityModuleIsOffLine.SetAlarm(true);
                                break;
                            }
                    }
                }
            }
            return Err;
        }
        
        public override void OnBegin()
        {
            if (SharedData.EquipmentType == EquipmentFamily.TOM)
                Communication.SendMessage(ThreadName, "", "GetFareMode");

            hwCsc = new DelhiDesfireEV0(null);
            hwCsc.Reset();
            hwToken = new DelhiTokenUltralight(null, 0);

            _ReaderCommRearReader = new ReaderComm();
            if (_bMediaDispensingUsingRearReader)
            {
                _ReaderCommRearReader.COM_PORT = (string)Configuration.ReadParameter("ComPort_RearReader", "String", "COM2:");
                _ReaderCommRearReader.COM_SPEED = (int)Configuration.ReadParameter("ComSpeed_RearReader", "int", "115200");
            }

            if (_delhiCCHSSAMUsage)
            {
                _ticketKeysMissing.SetAlarm(false);
                _ticketKeysError.SetAlarm(false);
                _ticketKeysMetaStatus.UpdateMetaStatus();
                string strSAMInCSCSlots = (string)Configuration.ReadParameter("SM1", "string", "I:2");
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
                        _ticketKeysMissing.SetAlarm(true);
                        _ticketKeysError.SetAlarm(false);
                        _ticketKeysMetaStatus.UpdateMetaStatus();
                        SendMessage(ThreadName, "", "TicketingKeyMetaStatus", _ticketKeysMetaStatus.Value.ToString(), "");
                        _globalMetaStatus.UpdateMetaStatus();
                        IFSEventsList.SaveIfHasChangedSinceLastSave("TTComponent");
                    }
                }
            }

            //To initialise DeviceID
            SendCommand("GetMachineID");

            try
            {
                _IsReaderLoaded = false;

                if (ReloadReader())
                {
                    if (_treatTicketingKeyFile)
                        LoadKeys();
                    if (_IsReaderLoaded)
                        if (_reader != null && _readDataFor != MediaDetectionTreatment.None)
                            _reader.StartPolling();
                }
            }
            catch (Exception e1)
            {
                Logging.Log(LogLevel.Error, "Main TT OnBegin " + e1.StackTrace);
                Logging.Log(LogLevel.Error, "Main TT OnBegin " + e1.Message);
                _cscReloader_FrontOrOnly_Failure.SetAlarm(true); _cscReloader_FrontNRear_Failure.SetAlarm(true);
                _cscReloaderMetaStatus.UpdateMetaStatus();
                base.OnBegin();
            }

            SendCommand("GetInitialisationParams");

            if (_bMediaDispensingUsingRearReader)
            {
                ReloadRearReader();
                SendMsg.GetTokenDispenserStatus();
                SendMetaStatusRearReader();
            }

            if (SharedData.EquipmentType == EquipmentFamily.TOM)
                SendMsg.GetTokenDispenserStatus();

            base.OnBegin();
        }

        private void ReloadRearReader()
        {
            FirmwareInfo firmware;

            if (Reader.ReloadReaderPlain(CSC_READER_TYPE.V4_READER, _ReaderCommRearReader, out _hRwRearReaderAtTokenDispenser, out firmware, Config._rfPowerRearReader) == CSC_API_ERROR.ERR_NONE)
            {
                if (Reader.InitRearReader(_hRwRearReaderAtTokenDispenser) == CSC_API_ERROR.ERR_NONE)
                    _IsRearReaderLoaded = true;
                // Send status.
            }
            _evtRearReaderOffline.SetAlarm(!_IsRearReaderLoaded);
            UpdateRearReaderMetaStatus();
        }

        internal void LoadKeys()
        {
            try
            {
                string sk = null;
                try
                {
#if WindowsCE
					sk= Disk.ReadAllTextFile(Disk.BaseDataDirectory + "\\CurrentXmlParameters\\TestKey.xml");
#else
                    sk = File.ReadAllText(Disk.BaseDataDirectory + "\\CurrentXmlParameters\\TestKey.xml");
#endif
                    _ticketKeysMissing.SetAlarm(false);
                }
                catch
                {
                    _ticketKeysMissing.SetAlarm(true);
                }
                if (sk != null)
                {
                    SecurityMgr.Instance.LoadKeyList(_ReaderType, _hRw, sk);
                    _ticketKeysError.SetAlarm(false);
                }
            }
            catch (Exception)
            {
                _ticketKeysError.SetAlarm(true);
            }
            _ticketKeysMetaStatus.UpdateMetaStatus();
            SendMessage(ThreadName, "", "TicketingKeyMetaStatus", _ticketKeysMetaStatus.Value.ToString(), "");
            _globalMetaStatus.UpdateMetaStatus();
            IFSEventsList.SaveIfHasChangedSinceLastSave("TTComponent");
        }

        public bool IsUsingCCHSSam()
        {
            return _delhiCCHSSAMUsage;
        }

        #region IListener Members

        ReaderOp IListener.MediaDetected(SmartFunctions.MediaDetected MediaDetectedState, long SerialNumber)
        {
            Logging.Log(LogLevel.Verbose, "TTMain.MediaDetected");
            StopTimer(Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList);
            if (MediaDetectedState == SmartFunctions.MediaDetected.UNSUPPORTEDMEDIA)
            {
                //SendMsg.BadPassengerCardDetection(TTErrorTypes.HardwareTypeNotTreated);
                if (_mediaUpdate == null)
                    return ReaderOp.SwitchToDetectRemoval_ContentWithNonRealTime;
                else
                    return ReaderOp.SwitchToDetectRemoval_RealTime;
            }

            if (_mediaUpdate != null)
            {
                Logging.Log(LogLevel.Verbose, "_mediaUpdate != null");
                StopDelay((int)Timers.NoMediaDetectedPost_Positive_PutMediaUnderRWAck);

                // decision of whether to read the media or not is left to _mediaUpdate
                _mediaUpdate.MediaAppeared(_MediaDetectedState, SerialNumber);

                CheckMediaUpdateForConclusion();
                hwCsc.ResetSelectedAppId(); // Even though it is not required, it is done for extra safety.
                if (_mediaUpdate == null)
                    return ReaderOp.SomeOperationsMayBeAskedToBePerformedOnThisMedia;
                else
                {
                    if (_mediaUpdate.IsWaitingForSomeMediaToArrivePostRTEOrWTE())
                        return ReaderOp.MediaMustHaveGotAwayFromField;
                    else
                        return ReaderOp.SwitchToDetectRemoval_RealTime;
                }
            }
            else
            {
                TreatmentOnCardDetection2();

                if (_errorCurMedia == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                    return ReaderOp.MediaMustHaveGotAwayFromField;
                else if (_errorCurMedia == TTErrorTypes.CannotReadTheCard_AuthenticationFailure
                    || _errorCurMedia == TTErrorTypes.CannotReadTheCard)// && _readDataFor != MediaDetectionTreatment.None)
                    return ReaderOp.SwitchToDetectRemoval_RealTime;
                else
                {
                    if (_readDataFor == MediaDetectionTreatment.TOM_Login)
                        return ReaderOp.SwitchToDetectRemoval_RealTime;
                    else
                        return ReaderOp.SomeOperationsMayBeAskedToBePerformedOnThisMedia;
                }
                //if (!SharedData.bLoginMode)
                //    return ReaderOp.SomeOperationsMayBeAskedToBePerformedOnThisMedia;
                //else
                //{
                //    // Commenting this delay, because it is the door to enter the Non aggressive mode, which is not implemented properly yet.
                //    //StartDelay((int)Timers.EnoughTimeElapsedSinceLastMediaWasKeptInExhaustiveDetectionRemovalInLoginMode, Config.MaxTime_InMillisec_ToLetLastMediaExhaustiveDetectionRemovalWhenInLoginState);
                //    return ReaderOp.SwitchToDetectRemoval_RealTime;
                //}
            }
        }

        void IListener.MediaRemoved()
        {
            Logging.Log(LogLevel.Verbose, "TTMain.MediaRemoved");

            StopTimer(Timers.EnoughTimeElapsedSinceLastMediaWasKeptInExhaustiveDetectionRemovalInLoginMode);

            ClearDataStructuresOnMediaRemoval();
            _reader.ClearIgnoreList(); // So, that if any media is in halted state, becomes potential candidate for re-appearance when we start polling again later.
            if (_mediaUpdate != null)
            {
                Logging.Log(LogLevel.Verbose, "MEDIAREMOVEDINT _mediaUpdate is non null");
                bool bInitiatePolling = _mediaUpdate.MediaRemoved();
                if (bInitiatePolling)
                    _reader.StartPolling();
            }
            else
            {
                SendMsg.MediaRemoved();
                if (_readDataFor != MediaDetectionTreatment.None)
                    _reader.StartPolling();
            }
        }

        void IListener.FieldStopped()
        {
            Logging.Log(LogLevel.Verbose, "TTMain.FieldStopped");
            ClearDataStructuresOnMediaRemoval();
            //StopTimer(Timers.TimoutMediaStillMaybeInFieldPostRTEOrWTE);
        }

        void IListener.MediaInIgnoreListAppeared()
        {
            Logging.Log(LogLevel.Verbose, "TTMain.MediaInIgnoreListAppeared");
            StartTimer(Timers.Timeout_NoMediaDetectedPostLastMediaWasHalted_LasMediaWasPutToIgnoreList);
        }
        #endregion

        // it would not be that effective, if made field of UpdateMedia. Case: Media is not read/written properly. Agent aborts the transaction, intiates a new transaction, but had forgottent to remove the last media.
        private Queue<long> _mediasTreatedRecently = new Queue<long>();

        public void AddMediaTreatedRecently(long mediaSrNbr)
        {
            if (_mediasTreatedRecently.Contains(mediaSrNbr))
                return;

            if (_mediasTreatedRecently.Count > Config.MaxCountOfMediasTreatedRecentlyToMaintain)
                _mediasTreatedRecently.Dequeue();
            _mediasTreatedRecently.Enqueue(mediaSrNbr);
        }
        public bool WasMediaTreatedRecently(long mediaSrNbr)
        {
            return _mediasTreatedRecently.Contains(mediaSrNbr);
        }
    }
}