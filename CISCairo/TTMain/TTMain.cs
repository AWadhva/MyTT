using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Xml;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Xml.Serialization;
using System.IO;

namespace IFS2.Equipment.TicketingRules
{
    public enum MediaOperation
    {
        NONE,
        INITIALIZE,
        FORMATE,
        READ,
        WRITE
    };



    public partial class MainTicketingRules : TaskThread, iRemoteMessageInterface
    {
        private ReaderComm _ReaderComm;
        private int _ReaderType;
        private bool _ReaderInitialized = false;
        public Semaphore semStopAsked;

        private FirmwareInfo _FirmwareInfo;
        private XmlWriterSettings contextFile_XMLWriterSettings;

        //private bool _treatTicketingKeyFile = true;
        private bool _isProcessingCard = false;
        public readonly bool _samUses = true, _bUseStockData = false, _bAutoInitCard = false;
        public readonly bool _signatureAtEachTransaction = false;
        private int _SAMType, _SAMSlot;
        private int _nbCardstobeInitialized = 0, _nbtotalCSCInitialized = 0, _nbReattempt = 0, _nbMaxReattempts = 2;
        private bool _IsCardInitializationRequestedPending;
        private UInt64 _cardSerialNum, _cardNumwithFailedRWAttempt = 0;
        private bool _bMediaHasDetected, _isNewVirginCardDetected, _bChangeKeysBeforeCreatingFiles;
       // private UInt16 _CardEndofValidity = 0;
        //private ParamMediaStocks mParamMediaStocks;
        private bool _isProductionSAM = false;
        private byte[] _samkey;
        private byte[] _kuckey;
        private long _samKUCthresold,_samCurrvalue,_samKUCQuota;
        private ulong _samSerialNumber = 0;
        //private byte _samkeyver, _kuckeyver;
        private AskToInitialiseCardtoTT _mCardData = null;

        private DFCairo mDFCairo;

        private SimulationCSCReloaderDriver simu = null;

        private OneEvent _repetitiveErrors = null;
        private OneEvent _last100Errors = null;

        private ulong lastSNMediaReadyToInitialise = 0;

        public MainTicketingRules()
            : base("MainTicketingRules")
        {
            //Logging.Log(LogLevel.Information, Configuration.DumpAllConfiguration());
            //Logging.Log(LogLevel.Information, Parameters.DumpAllConfiguration());

            _samKUCthresold = 0; _samCurrvalue=0; _samKUCQuota = 0;
            SharedData.EquipmentType = (EquipmentFamily)Configuration.ReadParameter("EquipmentType", "enum", "99:Unknown");
            //if (SharedData.EquipmentType == EquipmentFamily.HHD)
            //{
            //    Logging.Log(LogLevel.Critical, "EquipmentType not specified. Can't launch application");
            //    return;
            //}
            _isProcessingCard = false;
            _isNewVirginCardDetected = false;
            _cardSerialNum = 0;
            _cardNumwithFailedRWAttempt = 0;
            string MMIChannel = "MMIChannel";
            // SendMsg.SetThreadName(ThreadName, this);

            semStopAsked = new Semaphore(0, 10000);

            contextFile_XMLWriterSettings = new XmlWriterSettings();
            contextFile_XMLWriterSettings.Indent = false;
            contextFile_XMLWriterSettings.OmitXmlDeclaration = true;

            //Initialisation of all the tags managed by the TT

            //_globalMetaStatus = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 38, "EODMetaStatus", "METEOD", AlarmStatus.Alarm, OneEvent.OneEventType.MetaStatus);
            //_globalMetaStatus.SetMetaStatusLinkage("", "ParametersMissing;ParametersError;ParametersActivationError;TicketKeysMissing;TicketKeysError;FpStatus;FpError;PrgStatus;PrgError");

            IFSEventsList.InitContextFile("CSCReloaderDriver");
            _repetitiveErrors = IFSEventsList.GetEvent(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveErrors);
            _last100Errors = IFSEventsList.GetEvent(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100ErrorsCounter);
            IFSEventsList.InitContextFile("SAMDriver");

            
            //mParamMediaStocks = new ParamMediaStocks();
            //Load the Comm params
            _ReaderComm.COM_PORT = (string)Configuration.ReadParameter("ComPort", "String", "COM9:");
            _ReaderComm.COM_SPEED = (int)Configuration.ReadParameter("ComSpeed", "int", "115200");
            _ReaderType = (int)Configuration.ReadParameter("ReaderType", "int", "4");
            _SAMType = (int)Configuration.ReadParameter("SAMType", "int", "1");//0x01- NXPSAM
            _SAMSlot = (int)Configuration.ReadParameter("SAMSlot", "int", "1");
            _bUseStockData = (bool)Configuration.ReadParameter("UseStockData", "bool", "false");//
            _bChangeKeysBeforeCreatingFiles = (bool)Configuration.ReadParameter("ChangeKeysBeforeCreatingFiles", "bool", "true");
           // _bAutoInitCard = (bool)Configuration.ReadParameter("AutoInitCard", "bool", "false");
            _bChangeKeysBeforeCreatingFiles = (bool)Configuration.ReadParameter("ChangeKeysBeforeCreatingFiles", "bool", "true");

            _nbCardstobeInitialized = 0;
            _nbtotalCSCInitialized = 0;
            _IsCardInitializationRequestedPending = false;
            _bMediaHasDetected = false;

            Communication.AddEventsToReceive(ThreadName, "SetLoginMode;StopCardInitialization;InitialiseCard;MediaReadeyToInitialise", this);
            Communication.AddEventsToReceive(ThreadName, "ActivateReader;DeActivateReader;MediaInitialised;GetSAMQuota;FormateCard;;FormatCard;MediaFormatted", this);
            Communication.AddEventsToReceive(ThreadName, "SAMQuotaReloadData;MediaProduced;MediaRemoved;GetCSCReloaderStatus;GetSAMStatus;GetKUCQuota;GetKUCQuotaAnswer;UpdateReaderFirmware", this);

            Communication.AddEventsToExternal("CardError;CSCMediaDetection;CSCMediaRemoval;GetCSCReloaderStatusAnswer;CSCReloaderMetaStatus;GetSAMStatusAnswer;SAMMetaStatus;GetSAMQuotaAnswer;UpdateReaderFirmwareAnswer", MMIChannel);

            mDFCairo = new DFCairo();

            _mCardData = new AskToInitialiseCardtoTT();
            
        }//public MainTicketingRules()

        public bool Init()
        {
            // bool ret = false;
            Logging.Trace("TTMain.Init.Start Init procedure of Reader");
            //Initialisation of alarms for CSC Reloader
            UpdateOneAlarm(CSCReloaderDriverAlarms.IsOffLine, true);
            UpdateOneAlarm(CSCReloaderDriverAlarms.OutOfOrder, false);
            UpdateOneAlarm(CSCReloaderDriverAlarms.Last100Errors, false);
            UpdateOneAlarm(CSCReloaderDriverAlarms.Last100Warning, false);
            UpdateOneAlarm(CSCReloaderDriverAlarms.RepetitiveErrors, false);
            UpdateOneAlarm(CSCReloaderDriverAlarms.RepetitiveWarning, false);
            UpdateOneValue(CSCReloaderDriverAlarms.FirmwareVersion, "0");
            UpdateOneValue(CSCReloaderDriverAlarms.LoaderVersion, "0");
            UpdateOneValue(CSCReloaderDriverAlarms.APIVersion, "0");
            IFSEventsList.UpdateMetaStatus(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.MetaStatus);
            UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, true);
            UpdateOneAlarm(DSMDriverAlarms.SAM1Blocked, false);
            UpdateOneValue(DSMDriverAlarms.SAM1Counter, "0");
            UpdateOneValue(DSMDriverAlarms.SAM1Quota, "0");
            UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, false);
            UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, false);
            UpdateOneAlarm(DSMDriverAlarms.SAM1OutOfOrder, false);
            UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, true);
            IFSEventsList.UpdateMetaStatus(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1MetaStatus);

            //Initialisation of alarms for SAM
            _ReaderInitialized = false;
            mDFCairo.Registerlistiner(0, StatusListenerMediaProduced);
            mDFCairo.Registerlistiner(1, StatusListenerMediaRemoved);
            _FirmwareInfo = new FirmwareInfo();
            _ReaderInitialized = mDFCairo.Init(_ReaderType, _ReaderComm.COM_PORT, _SAMType, _SAMSlot, out _FirmwareInfo);
            Logging.Trace("TTMain.Init.Is Reader Initialised "+_ReaderInitialized.ToString());
            if (_ReaderInitialized)
            {
                UpdateOneValue(CSCReloaderDriverAlarms.LoaderVersion, _FirmwareInfo.Chargeur);
                UpdateOneValue(CSCReloaderDriverAlarms.FirmwareVersion, _FirmwareInfo.AppCSC);
                if (_ReaderType == 4) UpdateOneValue(CSCReloaderDriverAlarms.APIVersion, _FirmwareInfo.Fpga1);
                UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.IsOffLine, false);
                if (mDFCairo.GetReaderInstance().SAM_Activated())// If SAM is activated then only start polling
                {
                    bool ret = mDFCairo.GetReaderInstance().SAM_GetKUCQuota(0x01, out _samKUCQuota, out _samCurrvalue);
                    Logging.Log(LogLevel.Information, "SAM KUC Quota after reboot, return:" + ret.ToString() + " Curr val: " + _samCurrvalue.ToString() + " , Quota: " + _samKUCQuota.ToString());
                    if (ret)
                    {
                        UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1IsOffLine, false);
                    }
                    //check if Callback function wanted to use
                    mDFCairo.GetReaderInstance().RW_StartPolling();
                }
                else
                {
                    _ReaderInitialized = false;
                    Logging.Log(LogLevel.Error, "TTmain.Init() failed to activate SAM setting _ReaderInitialized to false..");
                }
            }
            return _ReaderInitialized;
        }

        private void UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms alarm,bool value)
        {
            try
            {
                IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)alarm, value);
                OneEvent oe = IFSEventsList.GetEvent(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.MetaStatus);
                oe.UpdateMetaStatus();
                if (oe.HasChangedSinceLastSave)
                {
                    IFSEventsList.SaveContext(ThreadName);
                    SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)oe.Value), IFSEventsList.GetEventsStatusList2("CSCReloaderDriver"));
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "Exception in UpdateOneAlarmAndMetaStatus CSCReloaderDriverAlarms :  " + ex.Message);
            }
            
        }
        private void UpdateOneAlarm(CSCReloaderDriverAlarms alarm, bool value)
        {
            IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)alarm, value);
        }
        private void UpdateOneAlarm(DSMDriverAlarms alarm, bool value)
        {
            IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)alarm, value);
        }
        private void UpdateOneValue(DSMDriverAlarms alarm, string value)
        {
            IFSEventsList.SetAttribute(StatusConsts.DSMDriver, (int)alarm, value);
        }
        private void UpdateOneValue(CSCReloaderDriverAlarms alarm, string value)
        {
            IFSEventsList.SetAttribute(StatusConsts.CSCReloaderDriver, (int)alarm, value);
        }
        private void UpdateOneAlarmAndMetaStatus(DSMDriverAlarms alarm, bool value)
        {            
            try
            {
                IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)alarm, value);
                OneEvent oe = IFSEventsList.GetEvent(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1MetaStatus);
                oe.UpdateMetaStatus();
                if (oe.HasChangedSinceLastSave)
                {
                    IFSEventsList.SaveContext(ThreadName);
                    SendMessage(ThreadName, "", "SAMMetaStatus", Convert.ToString((int)oe.Value), IFSEventsList.GetEventsStatusList2("DSMDriver"));
                }
            }
            catch(Exception ex)
            {
                Logging.Log(LogLevel.Error, "Exception in UpdateOneAlarmAndMetaStatus DSMDriverAlarms :  " + ex.Message);
            }
        }

        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            try
            {
                Logging.Log(LogLevel.Verbose, "Main TT : Message received " + eventMessage.EventID + " " + eventMessage.Attribute + " " + eventMessage.Message);
                if (Stopped) return 0;
                switch (eventMessage.EventID.ToUpper())
                {
                    case "SHUTDOWN":
                        Stopped = true;
                        IFSEventsList.SaveContext("CSCReloaderDriver");
                        return 0;
                    case "KILLAPPLICATION":
                        return 0;
                    #region "MEDIAPRODUCED"
                    case "MEDIAPRODUCED":                        
                        {
                            DateTime tsWhenMediaWasProduced = new DateTime(Convert.ToInt64(eventMessage._par[1]));
                            StatusCSC pStatusCSC = SerializeHelper<StatusCSC>.XMLDeserialize(eventMessage._par[0]);
                            bool _isCardInitOK = false, _isCardReappearedforRW = false;                            
                            ///initialization of the card can be done here ... 
                            /// if card read/write error then restart polloing and try again...
                            //Switch to removal detection state in case of success 
                            ulong SerialNum = 0;
                            
                            MediaTypeDetected detectionState;
                            mDFCairo.GetReaderInstance().RW_Extract_StatusCSC(ref pStatusCSC, out SerialNum, out detectionState);
                            
                            if (detectionState == MediaTypeDetected.CARD && SerialNum>0)
                            {
                                _bMediaHasDetected = true;
                                //Following message is only for maintenance. Should be good to check if we are in maintenance mode
                                SendCommand("CSCMediaDetection", "0", Utility.MakeTag("Media", Utility.MakeTag("CSN", SerialNum.ToString())));
                                Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced UID:" + SerialNum.ToString() + "/" + _cardSerialNum.ToString() + "/" + _cardNumwithFailedRWAttempt.ToString());
                                if (SerialNum != _cardSerialNum && SerialNum != _cardNumwithFailedRWAttempt)
                                {
                                    _cardSerialNum = SerialNum;
                                    //mark card is avaliable 
                                    //Wait for Card initialization message from Card printer
                                    _nbReattempt = 0;
                                    //check if card is vergin card
                                    _isNewVirginCardDetected = false;
                                    int result = mDFCairo.GetReaderInstance().CheckIfCardProducedisVirgin();
                                    if (result == 1) _isNewVirginCardDetected = true;
                                    if (!_isNewVirginCardDetected)
                                    {
                                        if (lastSNMediaReadyToInitialise != _cardSerialNum)
                                        {
                                            Logging.Log(LogLevel.Information, "TTMain.TreatMessageReceived.MediaProduced UID: " + _cardSerialNum.ToString() + " Produced is not a Blank card...");
                                            SendMessage(ThreadName, "", "MediaReadyToInitialise", "2", _cardSerialNum.ToString());
                                            lastSNMediaReadyToInitialise = _cardSerialNum;
                                        }

                                    }
                                    else
                                    {
                                        if (lastSNMediaReadyToInitialise != _cardSerialNum)
                                        {
                                            //A good card detected....
                                            SendMessage(ThreadName, "", "MediaReadyToInitialise", "0", _cardSerialNum.ToString());
                                            //In this case we will wait what will say core to switch to removal detection state
                                            lastSNMediaReadyToInitialise = _cardSerialNum;
                                        }
                                    }
                                    //SKS: Added on 20190318
                                    Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced Switching to card removal...");
                                    bool re = mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();
                                    Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced Switching to card removal.ret:" + re.ToString());
                                    //~SKS
                                }
                                else if (SerialNum == _cardSerialNum)
                                {
                                    if (_cardSerialNum == _cardNumwithFailedRWAttempt) // previous card with failed RW attempt presented again
                                    {
                                        if (_nbReattempt <= _nbMaxReattempts)
                                        {
                                            _isCardReappearedforRW = true;
                                            CheckForMediaRelatedActivity(MediaOperation.FORMATE, 0);
                                        }
                                        else
                                        {
                                            mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();
                                            SendMessage(ThreadName, "", "MediaInitialised", "1", SerializeHelper<AskToInitialiseCardtoTT>.XMLSerialize(_mCardData));
                                            BadInitialisationToRegister(_mCardData);
                                        }
                                    }
                                    else
                                    {
                                        //card may have already written inform to stacker to push the card
                                        //case when no previous attempts or cardInitialzation request were not recieved...
                                        Logging.Log(LogLevel.Information, "TTMain.TreatMessageReceived.MediaProduced Same media has been detected , without any failed attempt , most likely after reader re-set");
                                        if (!_IsCardInitializationRequestedPending)
                                        {
                                            _isNewVirginCardDetected = false;
                                            int result = mDFCairo.GetReaderInstance().CheckIfCardProducedisVirgin();
                                            if (result == 1) _isNewVirginCardDetected = true;
                                            Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced, Is card status virgin: " + _isNewVirginCardDetected.ToString());
                                            if (_isNewVirginCardDetected)
                                            {
                                                //A good card detected....
                                                SendMessage(ThreadName, "", "MediaReadyToInitialise", "0", _cardSerialNum.ToString());
                                                //In this case we will wait what will say core to switch to removal detection state
                                                lastSNMediaReadyToInitialise = _cardSerialNum;
                                            }
                                            else SendMessage(ThreadName, "", "MediaReadyToInitialise", "2", _cardSerialNum.ToString());
                                        }
                                        Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced Switching to card removal...");
                                        bool re = mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();
                                        Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced Switching to card removal.ret:" + re.ToString());

                                    }
                                }
                                
                                if (_IsCardInitializationRequestedPending)//|| _bAutoInitCard)
                                {
                                    int errocode = -1;

                                    if ((_isCardReappearedforRW || _isNewVirginCardDetected) && (ulong)_mCardData.MediaSerialNumber == _cardSerialNum)
                                    {
                                        _isCardInitOK = CheckForMediaRelatedActivity(MediaOperation.INITIALIZE, ref _mCardData, out errocode);
                                        if (_isCardInitOK)
                                        {

                                            _IsCardInitializationRequestedPending = false;
                                            Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced Card UID: " + _cardSerialNum.ToString() + " Initialized ...");
                                            //ReportInitializationNAK(3, textWriter.ToString(), true);
                                            SendMessage(ThreadName, "", "MediaInitialised", "0", SerializeHelper<AskToInitialiseCardtoTT>.XMLSerialize(_mCardData));
                                            GoodInitialisationToRegister(_mCardData);
                                            //}

                                            _mCardData.Clear();
                                            _nbReattempt = 0;
                                        }
                                        else
                                        {
                                            Logging.Log(LogLevel.Error, "Card UID: " + _cardSerialNum.ToString() + " Initialization failed ...");
                                            if (errocode == (int)E_TP_Errors.ERR_KUC_LIMIT_OVER && errocode == (int)E_TP_Errors.ERR_AUTH_FAILURE)
                                            {
                                                ReportInitializationNAK(errocode, SerializeHelper<AskToInitialiseCardtoTT>.XMLSerialize(_mCardData), true);
                                                if (errocode == (int)E_TP_Errors.ERR_KUC_LIMIT_OVER)
                                                {
                                                    SendMessage(ThreadName, "", "NeedsToReloadSAMQuota", "", "");
                                                    UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, true);
                                                    UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, true);
                                                    UpdateOneAlarm(DSMDriverAlarms.SAM1OutOfOrder, true);
                                                    UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);
                                                }
                                            }
                                            else
                                            {
                                                // try to restart polling..
                                                _nbReattempt++;
                                                if (_nbReattempt < _nbMaxReattempts)
                                                {
                                                    RestartPolling();
                                                    Logging.Log(LogLevel.Information, "Card UID: " + _cardSerialNum.ToString() + " Initialization Reattempt:" + _nbReattempt.ToString());
                                                }
                                                else
                                                {
                                                    _nbReattempt = 0;
                                                    Logging.Log(LogLevel.Error, "Card UID: " + _cardSerialNum.ToString() + " Initialization Max Reattempt Reached");
                                                    //SendMessage(ThreadName, "", "InitializeCardAnswer", "1", "");
                                                    ReportInitializationNAK(errocode, SerializeHelper<AskToInitialiseCardtoTT>.XMLSerialize(_mCardData), true);
                                                    //}
                                                    // ReportInitializationNAK(3, "" ,true);
                                                }
                                            }

                                        }
                                        _isNewVirginCardDetected = false;
                                        _isCardReappearedforRW = false;
                                    }
                                    else if (!_isNewVirginCardDetected && !_isCardReappearedforRW)
                                    {
                                        //card produced is not balck card ... send error message to driver
                                        // SendMessage(ThreadName, "", "InitializeCardAnswer", "1", "");
                                        ReportInitializationNAK(3, "", true);

                                    }
                                    else if ((ulong)_mCardData.MediaSerialNumber != _cardSerialNum)
                                    {
                                        string s = SerializeHelper<AskToInitialiseCardtoTT>.XMLSerialize(_mCardData);
                                        ReportInitializationNAK(5, s, true);
                                    }
                                }//if (_IsCardInitializationRequestedPending)
                            }//if (detectionState == MediaTypeDetected.CARD)
                            else
                            {
                                Logging.Log(LogLevel.Warning, "TTMain.TreatMessageReceived.MediaProduced Media Type:" + detectionState.ToString() + "  Serial Num: " + SerialNum.ToString());
                                Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced unknow card type detected Switching to card removal...");
                                bool re = mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();
                                Logging.Log(LogLevel.Verbose, "TTMain.TreatMessageReceived.MediaProduced Switching to card removal.ret:" + re.ToString());
                            }
                            return 0;
                        }//case "MEDIAPRODUCED":
                    #endregion
                    #region "MEDIAREMOVED"
                    case "MEDIAREMOVED":
                        {
                            Logging.Log(LogLevel.Verbose, "Card UID: " + _cardSerialNum.ToString() + " Removed ...");
                            SendCommand("CSCMediaRemoval", _cardSerialNum.ToString());
                            _isNewVirginCardDetected = false;
                            _bMediaHasDetected = false;
                            _cardNumwithFailedRWAttempt = 0;
                            _cardSerialNum = 0;                           
                            RestartPolling();
                        } return 0;
                    #endregion
                    case "GETCSCRELOADERSTATUS":
                        {
                            OneEvent oe = IFSEventsList.GetEvent(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.MetaStatus);
                            SendMessage(ThreadName, "", "GetCSCReloaderStatusAnswer", IFSEventsList.GetEventsStatusList2("CSCReloaderDriver"), Convert.ToString((int)oe.Value));
                            return 0;
                        }
                    case "GETSAMSTATUS":
                        {
                            OneEvent oe = IFSEventsList.GetEvent(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1MetaStatus);
                            SendMessage(ThreadName, "", "GetSAMStatusAnswer", IFSEventsList.GetEventsStatusList2("DSMDriver"), Convert.ToString((int)oe.Value));
                            return 0;
                        }
                    case "FORMATCARD":
                    case "FORMATECARD":
                        #region "FORMATCARD"
                        {
                          //  try
                            {      
                                    int errocode = -1;
                                    if (_bMediaHasDetected)
                                    {                                       

                                        bool isCardInitOK = CheckForMediaRelatedActivity(MediaOperation.FORMATE, 0);
                                        if (isCardInitOK)
                                        {
                                            _IsCardInitializationRequestedPending = false;
                                            Logging.Log(LogLevel.Verbose, " in treatment of FORMATCARD , Card UID: " + _cardSerialNum.ToString() + " Formatted ...");
                                            SendMessage(ThreadName, "", "MediaFormatted", "0", _cardSerialNum.ToString());
                                        }
                                        else
                                        {
                                            SendMessage(ThreadName, "", "MediaFormatted", errocode.ToString(), _cardSerialNum.ToString());
                                        }
                                       // mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();
                                    }
                                    else
                                    {
                                        SendMessage(ThreadName, "", "MediaFormatted", "3", "");
                                    }
                            }
                            //catch (Exception e1)
                            //{
                            //    Logging.Log(LogLevel.Error, "TTMain.TreatMessageReceived. FormatCard " + e1.Message);
                            //}
                            //_bMediaHasDetected = false;
                            return 0;
                        }
                        #endregion
                    case "RESETREADER":
                        {
                            try
                            {
                                //Repetitive errors are reset
                                _repetitiveErrors.SetAttribute("0");
                                IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveErrors, false);
                                IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveWarning, false);
                                IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Errors, false);
                                IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Warning, false);
                                string s = "";
                                for (int i = 0; i < 100; i++) s += "0";
                                _last100Errors.SetAttribute(s);
                                IFSEventsList.SaveContext("CSCReloaderReader");
                            }
                            catch (Exception ex)
                            {
                            }
                            _ReaderInitialized = false; //To force reinitialisation
                            return 0;
                        }
                    case "ACTIVATEREADER":
                        {
                            EnableDetection(false);
                            Thread.Sleep(200);
                            _cardSerialNum = 0;
                            _cardNumwithFailedRWAttempt = 0;
                            lastSNMediaReadyToInitialise = 0;
                            _isNewVirginCardDetected = false;
                            EnableDetection(true);
                        }
                        return 0;
                    case "DEACTIVATEREADER":
                        {
                            EnableDetection(false);
                            _isNewVirginCardDetected = false;
                            _bMediaHasDetected = false;
                            _cardNumwithFailedRWAttempt = 0;
                            _cardSerialNum = 0;
                        }
                        return 0;
                    case "STOPCARDINITIALISATION":
                        {
                            lastSNMediaReadyToInitialise = 0;
                        }
                        break;
                    case "INITIALISECARD":
                        #region "INITIALISECARD"
                        {
                            //try
                            {
                               // _bMediaHasDetected = true;
                                _mCardData = SerializeHelper<AskToInitialiseCardtoTT>.XMLDeserialize(eventMessage.Attribute);
                                if (Configuration.ReadBoolParameter("SimulationModeForCSCReadWrite", false))
                                {
                                    ReloadCSCReloaderSimulation();
                                    Thread.Sleep(simu.InitialiseAnswerDelay);
                                    SendCommand("MediaInitialised", simu.InitialiseAnswer.ToString(), eventMessage.Attribute);
                                    _mCardData = new AskToInitialiseCardtoTT();
                                    _mCardData.BatchNumber = 127;
                                    _mCardData.EndOfValidity = new DateTime(2040, 6, 1);
                                    _mCardData.EngravedNumber = 1234567890;
                                    _mCardData.MediaArtworkID = 1;
                                    _mCardData.MediaSerialNumber = 1122334455;
                                    if (simu.InitialiseAnswer == 0) GoodInitialisationToRegister(_mCardData);
                                    else BadInitialisationToRegister(_mCardData);
                                   // _bMediaHasDetected = false;
                                    return 0;
                                }
                                if (_isNewVirginCardDetected && ((ulong)_mCardData.MediaSerialNumber == _cardSerialNum) && _bMediaHasDetected)
                                {
                                    //Initialize
                                    int errocode = -1;
                                                                      

                                    bool isCardInitOK = CheckForMediaRelatedActivity(MediaOperation.INITIALIZE, ref _mCardData, out errocode);
                                    if (isCardInitOK)
                                    {

                                        _IsCardInitializationRequestedPending = false;
                                        Logging.Log(LogLevel.Verbose, " in treatment of INITIALIZECARD , Card UID: " + _cardSerialNum.ToString() + " Initialized ...");
                                        SendMessage(ThreadName, "", "MediaInitialised", "0", SerializeHelper <AskToInitialiseCardtoTT>.XMLSerialize(_mCardData));
                                        GoodInitialisationToRegister(_mCardData);
                                        //mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState(); //TO SEE IF IT SHALL BE OR NOT
                                    }
                                    else
                                    {                                       
                                            ReportInitializationNAK(errocode, SerializeHelper < AskToInitialiseCardtoTT>.XMLSerialize(_mCardData), true);
                                            if (errocode == (int)E_TP_Errors.ERR_KUC_LIMIT_OVER)
                                            {
                                                SendMessage(ThreadName, "", "NeedsToReloadSAMQuota", "", "");
                                                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, true);
                                                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, true);
                                                UpdateOneAlarm(DSMDriverAlarms.SAM1OutOfOrder, true);
                                                UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);
                                            }
                                    }
                                    //then go to card removal state
                                }
                                else
                                {
                                    _IsCardInitializationRequestedPending = true;
                                }

                            }
                            //catch (Exception e1)
                            //{
                            //    Logging.Log(LogLevel.Error,"TTMain.TreatMessageReceived. InitialiseCard "+e1.Message);
                            //}
                            //_bMediaHasDetected = false;
                            return 0;
                        }
                        #endregion
                    case "UPDATEREADERFIRMWARE":
                        {
                            string filepath = eventMessage.Attribute;
                            bool ret = false;
                            if (_ReaderInitialized)
                            {
                                if (filepath != null)
                                {
                                    ret = mDFCairo.GetReaderInstance().RW_UpdateFirmware(filepath);
                                }

                            }
                            LogLevel ll = LogLevel.Verbose;
                            if (!ret) ll = LogLevel.Error;
                            Logging.Log(ll, "UPDATEREADERFIRMWARE ret: " + ret.ToString());
                            SendMessage(ThreadName, "", "UpdateReaderFirmwareAnswer", ret == true ? "0" : "1", "");
                             //To force reinitialisation
                            {                                
                                _ReaderInitialized = false; //To force reinitialisation
                            }
                        }
                        return 0;
                    case "GETSAMQUOTA":
                            {
                                bool ret = false;
                                LogLevel ll = LogLevel.Verbose;
                                if (_ReaderInitialized && mDFCairo.GetReaderInstance().SAM_Activated())
                                {
                                    ret = mDFCairo.GetReaderInstance().SAM_GetKUCQuota(0x01, out _samKUCQuota, out _samCurrvalue);
                                }
                                if (!ret) ll = LogLevel.Error;

                                Logging.Log(ll, "GETKUCQUOTA ret: " + ret.ToString() + " Quota:" + _samKUCQuota.ToString() + " , CurrCount:" + _samCurrvalue.ToString());
                                if (ret)
                                {
                                    SAMManagerGetQuotaAnswer ans = new SAMManagerGetQuotaAnswer();
                                    ans.Quota = _samKUCQuota;
                                    ans.Counter = _samCurrvalue;
                                    ans.SerialNumber = _samSerialNumber;
                                    ans.EquipmentNumber = SharedData.EquipmentNumber;
                                    ans.EquipmentType = SharedData.EquipmentType;
                                    UpdateForSAmQuota();
                                    //SendMessage(ThreadName, "", "GetSAMStatusAnswer", ret == true ? "0" : "1", IFSEventsList.GetEventsStatusList2("DSMDriver")); //To see if we can remove.
                                    Communication.SendMessage(ThreadName, "", "GetSAMQuotaAnswer","0", SerializeHelper<SAMManagerGetQuotaAnswer>.XMLSerialize(ans), IFSEventsList.GetEventsStatusList2("DSMDriver"));
                                }
                                else
                                {
                                    Communication.SendMessage(ThreadName, "", "GetSAMQuotaAnswer", "1", "", IFSEventsList.GetEventsStatusList2("DSMDriver"));
                                }
                            }
                        return 0;
                    case "SAMQUOTARELOADDATA":
                        {
                            bool ret = false;
                            LogLevel ll = LogLevel.Verbose;
                            Logging.Log(ll, " SAMQUOTARELOADDATA attrib: " + eventMessage.Attribute + "   data:" + eventMessage.Message);
                            if (eventMessage.Attribute == "0")
                            {
                                byte[] encodingdata = System.Convert.FromBase64String(eventMessage.Message);
                                if (_ReaderInitialized && mDFCairo.GetReaderInstance().SAM_Activated()) 
                                    ret = mDFCairo.GetReaderInstance().SAM_ChangeKUCQuota(0x01, true, false, false, _samCurrvalue, encodingdata);
                            }
                            if (ret)
                            {
                                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, false);
                                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, false);
                                UpdateOneAlarm(DSMDriverAlarms.SAM1OutOfOrder, false);
                                UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, false);
                            }
                            else
                            {
                                ll = LogLevel.Error;
                            }
                            Logging.Log(ll, " SAMQUOTARELOADDATA result: " + ret.ToString());
                        }
                        return 0;
                    case "GETKUCQUOTA":
                            {
                                bool ret = false;
                                LogLevel ll = LogLevel.Verbose;
                                if (_ReaderInitialized && mDFCairo.GetReaderInstance().SAM_Activated())
                                {
                                   ret= mDFCairo.GetReaderInstance().SAM_GetKUCQuota(0x01, out _samKUCQuota, out _samCurrvalue);
                                }
                                if (!ret) ll = LogLevel.Error;

                                Logging.Log(ll, "GETKUCQUOTA ret: " + ret.ToString() + " Quota:" + _samKUCQuota.ToString() + " , CurrCount:" + _samCurrvalue.ToString());
                                SendMessage(ThreadName, "", "GetKUCQuotaAnswer", ret == true ? "0" : "1", (_samKUCQuota-_samCurrvalue).ToString());
                            }
                        return 0;

                    //case "UPDATECARDSTOCK":
                    //    {
                    //        try
                    //        {
                    //            if (mParamMediaStocks != null && _bUseStockData)
                    //            {
                    //                mParamMediaStocks.InitilizeTable(eventMessage.Attribute);
                    //            }
                    //        }
                    //        catch
                    //        {
                    //        }
                    //        break;
                    //    }

                }//switch (eventMessage.EventID.ToUpper())               
            }
            catch (ReaderException exp)
            {
                Logging.Log(LogLevel.Information, "ReaderException = " + exp.Code.ToString());
                switch (exp.Code)
                {
                    case CSC_API_ERROR.ERR_LINK:
                    case CSC_API_ERROR.ERR_COM:
                    case CSC_API_ERROR.ERR_INTERNAL:
                    case CSC_API_ERROR.ERR_DEVICE:
                        {                   
                           
                            UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, true);
                            UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, false);
                            UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);
                            UpdateOneAlarm(CSCReloaderDriverAlarms.IsOffLine, true);  
                            UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.OutOfOrder, true);
                        }
                        break;
                }
                if (_IsCardInitializationRequestedPending && _bMediaHasDetected)
                {
                    //error code 7 for Reader comm lost...
                    ReportInitializationNAK(7, SerializeHelper<AskToInitialiseCardtoTT>.XMLSerialize(_mCardData), true);
                    _IsCardInitializationRequestedPending = false;
                }               
            }
            catch (Exception e1)
            {
                Logging.Log(LogLevel.Error, "TTMain.TreatMessageReceived. Exception " + e1.Message);
            }
            finally
            {
                base.TreatMessageReceived(eventMessage);
            }
            return 0;
        }//public override int TreatMessageReceived(EventMessage eventMessage)

        public override void OnBegin()
        {
            try
            {
                IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1IsOffLine, true);
                //IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.IsOffLine, true);
                //IFSEventsList.UpdateMetaStatus(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.MetaStatus);

                _ReaderInitialized = false;
                Stopped = false;
                _ReaderInitialized = this.Init();
                
            }
            catch (Exception e1)
            {
                Logging.Log(LogLevel.Error, "Main TT OnBegin " + e1.StackTrace);
                Logging.Log(LogLevel.Error, "Main TT OnBegin " + e1.Message);
            }
            finally
            {
                base.OnBegin();
            }

        }

        private void ReloadCSCReloaderSimulation()
        {
            try
            {
                if (Configuration.ReadBoolParameter("SimulationModeForCSCReloader", false))
                {
                    string s = Utility.ReadAllText(Disk.BaseCodeDirectory + "\\Conf\\Simulation\\CSCReloaderDriver.xml");
                    simu = SerializeHelper<SimulationCSCReloaderDriver>.XMLDeserialize(s);
                }
            }
            catch
            {

            }
        }

        private void GoodInitialisationToRegister(AskToInitialiseCardtoTT ask)
        {
            //We can generate a transaction
            if (Configuration.ReadBoolParameter("GenerateCSCInitialisationTransaction", true))
            {
                LogicalMedia media = new LogicalMedia();
                media.Media.ChipSerialNumber = ask.MediaSerialNumber;
                media.Media.EngravedNumber = ask.EngravedNumber;
                media.Application.TransportApplication.ExpiryDate = ask.EndOfValidity;
                string s1 = TransactionsHelper.TransactionDetail(TransactionType.CSCInitialisationAnonymous, TransactionStatus.Correct, 0, ask);
                Communication.SendMessage("TTMain", "", "StoreTransaction", Convert.ToString((int)TransactionType.CSCInitialisationAnonymous), Convert.ToString((int)TransactionStatus.Correct), "0", s1,SerializeHelper<LogicalMedia>.XMLSerialize(media), "");
            }

            //Repetitive errors are reset
            _repetitiveErrors.SetAttribute("0");
            IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveErrors, false);
            IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveWarning, false);
            string s = _last100Errors.Attribute;
            while (s.Length < 100) s+="0";
            byte[] tab = Encoding.UTF8.GetBytes(s);
            //We need to shift values and then to add 0 at the end
            for (int i = 0; i < 99; i++) tab[i] = tab[i + 1];
            tab[99] = 0x30;
            //We put back as string
            s = Encoding.UTF8.GetString(tab, 0, tab.Length);
            _last100Errors.SetAttribute(s);
            int nb=0;
            for (int i = 0; i < 100; i++)
            {
                if (tab[i] != 0x30) nb++;
            }
            if (nb < Configuration.ReadIntParameter("Last100ErrorsWarningThreshold", 7)) IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Warning, false);
            if (nb < Configuration.ReadIntParameter("Last100ErrorsAlarmThreshold", 10)) IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Errors, false);
            IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
        }

        private void BadInitialisationToRegister(AskToInitialiseCardtoTT ask)
        {
            if (Configuration.ReadBoolParameter("GenerateCSCInitialisationTransaction", true))
            {
                LogicalMedia media = new LogicalMedia();
                media.Media.ChipSerialNumber = ask.MediaSerialNumber;
                media.Media.EngravedNumber = ask.EngravedNumber;
                media.Application.TransportApplication.ExpiryDate = ask.EndOfValidity;
                string s1 = TransactionsHelper.TransactionDetail(TransactionType.CSCInitialisationRejection, TransactionStatus.Correct, 0, ask);
                Communication.SendMessage("TTMain", "", "StoreTransaction", Convert.ToString((int)TransactionType.CSCInitialisationRejection), Convert.ToString((int)TransactionStatus.Correct), "0", s1, SerializeHelper<LogicalMedia>.XMLSerialize(media), "");
            }
            //Repetitive errors are reset
            int k = Convert.ToInt32(_repetitiveErrors.Attribute);
            k++;
            _repetitiveErrors.SetAttribute((k + 1).ToString());
            if (k >= Configuration.ReadIntParameter("ErrorsAlarmThreshold", 5)) IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveErrors, true);
            if (k >= Configuration.ReadIntParameter("ErrorsWarningThreshold",3)) IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveWarning, true);
            string s = _last100Errors.Attribute;
            while (s.Length < 100) s += "0";
            byte[] tab = Encoding.UTF8.GetBytes(s);
            //We need to shift values and then to add 0 at the end
            for (int i = 0; i < 99; i++) tab[i] = tab[i + 1];
            tab[99] = 0x31;
            //We put back as string
            s = Encoding.UTF8.GetString(tab, 0, tab.Length);
            _last100Errors.SetAttribute(s);
            int nb = 0;
            for (int i = 0; i < 100; i++)
            {
                if (tab[i] != 0x30) nb++;
            }
            if (nb >= Configuration.ReadIntParameter("Last100ErrorsWarningThreshold", 7)) IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Warning, true);
            if (nb >= Configuration.ReadIntParameter("Last100ErrorsAlarmThreshold", 10)) IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Errors, true);
            IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
        }



        public override void RepetitiveTreatment()
        {
            //Repetitive 
            try
            {
                if (Configuration.ReadBoolParameter("SimulationModeForCSCReloader", false))
                {
                    try
                    {
                        ReloadCSCReloaderSimulation();
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.IsOffLine, simu.IsOffLine);
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.OutOfOrder, simu.IsOutOfOrder);
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Error, simu.Error);
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveWarning, simu.RepetitiveWarning);
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.RepetitiveErrors, simu.RepetitiveErrors);
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Warning, simu.Last100Warning);
                        IFSEventsList.SetAlarm(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.Last100Errors, simu.Last100Errors);
                        IFSEventsList.SetAttribute(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.SerialNumber, simu.SerialNumber);
                        IFSEventsList.SetAttribute(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.APIVersion, simu.APIVersion);
                        IFSEventsList.SetAttribute(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.LoaderVersion, simu.LoaderVersion);
                        IFSEventsList.SetAttribute(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.FirmwareVersion, simu.FirmwareVersion);
                        OneEvent oe = IFSEventsList.GetEvent(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.MetaStatus);
                        oe.UpdateMetaStatus();
                        if (oe.HasChangedSinceLastSave)
                        {
                            IFSEventsList.SaveIfHasChangedSinceLastSave("CSCReloaderDriver");
                            SendMessage(ThreadName, "", "CSCReloaderMetaStatus", Convert.ToString((int)oe.Value), IFSEventsList.GetEventsStatusList2("CSCReloaderDriver"));
                        }
                    }
                    catch
                    {

                    }
                    //return;
                }
                if (Configuration.ReadBoolParameter("SimulationModeForDSMDriver", false))
                {
                    try
                    {
                        string s = Utility.ReadAllText(Disk.BaseCodeDirectory + "\\Conf\\Simulation\\DSMDriver.xml");
                        SimulationDSMDriver simu = SerializeHelper<SimulationDSMDriver>.XMLDeserialize(s);
                        IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1IsOffLine, simu.SAM1IsOffLine);
                        IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1OutOfOrder, simu.SAM1OutOfOrder);
                        IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1IsNotPresent, simu.SAM1IsNotPresent);
                        IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1Blocked, simu.SAM1Blocked);
                        IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1QuotaLow, simu.SAM1QuotaLow);
                        IFSEventsList.SetAlarm(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1QuotaEmpty, simu.SAM1QuotaEmpty);
                        IFSEventsList.SetAttribute(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1SerialNumber, simu.SAM1SerialNumber);
                        IFSEventsList.SetAttribute(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1Quota, simu.SAM1Quota.ToString());
                        IFSEventsList.SetAttribute(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1Counter, simu.SAM1Counter.ToString());
                        OneEvent oe = IFSEventsList.GetEvent(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1MetaStatus);
                        oe.UpdateMetaStatus();
                        if (oe.HasChangedSinceLastSave)
                        {
                            IFSEventsList.SaveIfHasChangedSinceLastSave("DSMDriver");
                            SendMessage(ThreadName, "", "SAMMetaStatus", Convert.ToString((int)oe.Value), IFSEventsList.GetEventsStatusList2("DSMDriver"));
                        }
                    }
                    catch
                    {

                    }
                    //return;
                }
                if (_ReaderInitialized &&(( !_bMediaHasDetected && !_IsCardInitializationRequestedPending) || !_isProcessingCard))
                {
                    Logging.Trace("TTMain.Repetitive.CheckingStatus");
                    CSC_API_ERROR err = mDFCairo.GetReaderInstance().RW_PingReader();
                    //OneEvent oe = IFSEventsList.GetEvent(StatusConsts.CSCReloaderDriver, (int)CSCReloaderDriverAlarms.MetaStatus);
                    if (err == CSC_API_ERROR.ERR_NONE)
                    {
                        UpdateOneAlarm(CSCReloaderDriverAlarms.IsOffLine, false);
                        UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.OutOfOrder, false);
                        if (IFSEventsList.Activated(StatusConsts.DSMDriver, new int[] { (int)DSMDriverAlarms.SAM1OutOfOrder, (int)DSMDriverAlarms.SAM1IsOffLine, (int)DSMDriverAlarms.SAM1Blocked, (int)DSMDriverAlarms.SAM1IsNotPresent }))
                        {
                           bool ret = mDFCairo.GetReaderInstance().SAM_GetKUCQuota(0x01, out _samKUCQuota, out _samCurrvalue);
                            Logging.Log(LogLevel.Information, "SAM KUC Quota after recovery: Curr val: " + _samCurrvalue.ToString() + " , Quota: " + _samKUCQuota.ToString());

                            NXP_SAM_Info dsmInfo= null;
                            mDFCairo.GetReaderInstance().SAM_GetVersion(out dsmInfo);
                                if (dsmInfo != null)
                                {
                                    //BitConverter.ToString(dsmInfo.SerialNum).Replace("-", string.Empty)
                                    _samSerialNumber = Utility.ConvertByesTabToLong(dsmInfo.SerialNum);
                                    UpdateForSAmQuota();
                                    IFSEventsList.SetAttribute(StatusConsts.DSMDriver, (int)DSMDriverAlarms.SAM1SerialNumber, _samSerialNumber.ToString());
                                }

                                if (mDFCairo.GetReaderInstance().SAM_Activated())
                                {
                                    UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, false);
                                    UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, false);
                                    UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, false);
                                }
                                else
                                {
                                    UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, true);
                                  if(ret)
                                      UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, false);
                                  else UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, true);
                                    UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);
                                }
                        }
                        
                    }
                    else if (err == CSC_API_ERROR.ERR_TIMEOUT // this code is observed
                             || err == CSC_API_ERROR.ERR_LINK || err == CSC_API_ERROR.ERR_COM // Putting based on guess; hence can remove them
                             )
                    {
                        //if (!oe.Activated)
                        //    oe.SetAlarm(true);
                        _ReaderInitialized = false;
                        _IsCardInitializationRequestedPending = false;
                        _bMediaHasDetected = false;
                        _isNewVirginCardDetected = false;
                        if (err == CSC_API_ERROR.ERR_TIMEOUT) UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.IsOffLine,true);
                        else UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.OutOfOrder, true);
                        UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, true);
                        UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, false);
                        UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);

                    }
                }
                else if (_ReaderInitialized == false)
                {
                    _FirmwareInfo.AppCSC = "";
                    _FirmwareInfo.Chargeur = "";
                    _ReaderInitialized = mDFCairo.GetReaderInstance().RW_RestartReader(out _FirmwareInfo);
                    if (_ReaderInitialized)
                    {
                        UpdateOneValue(CSCReloaderDriverAlarms.LoaderVersion, _FirmwareInfo.Chargeur);
                        UpdateOneValue(CSCReloaderDriverAlarms.FirmwareVersion, _FirmwareInfo.AppCSC);
                        if (_ReaderType == 4) UpdateOneValue(CSCReloaderDriverAlarms.APIVersion, _FirmwareInfo.Fpga1);
                        if (mDFCairo.GetReaderInstance().SAM_Activated())
                        {
                            mDFCairo.GetReaderInstance().SAM_GetKUCQuota(0x01, out _samKUCQuota, out _samCurrvalue);
                            Logging.Log(LogLevel.Information, "SAM KUC Quota after reset: Curr val: " + _samCurrvalue.ToString() + " , Quota: " + _samKUCQuota.ToString());

                            //StartPolling 
                            mDFCairo.GetReaderInstance().RW_StartPolling();
                            UpdateOneAlarm(CSCReloaderDriverAlarms.IsOffLine, false);
                            UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.OutOfOrder, false);

                            UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, true);
                            UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, false);
                            UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);
                        }
                        else
                        {
                            _ReaderInitialized = false;
                            Logging.Log(LogLevel.Error, "TTMain.Repetitive. force reload reader as SAM is not activated..");
                        }

                    }
                }
            }            
            catch (ReaderException exp)
            {
                Logging.Log(LogLevel.Error, "Repetitive Treatment ReaderException = " + exp.Code.ToString());
                switch (exp.Code)
                {
                    case CSC_API_ERROR.ERR_LINK:
                    case CSC_API_ERROR.ERR_COM:
                    case CSC_API_ERROR.ERR_INTERNAL:
                    case CSC_API_ERROR.ERR_DEVICE:
                        {
                            
                                                       
                            UpdateOneAlarm(DSMDriverAlarms.SAM1IsOffLine, true);
                            UpdateOneAlarm(DSMDriverAlarms.SAM1IsNotPresent, false);
                            UpdateOneAlarmAndMetaStatus(DSMDriverAlarms.SAM1OutOfOrder, true);
                            UpdateOneAlarm(CSCReloaderDriverAlarms.IsOffLine, true);                           
                            UpdateOneAlarmAndMetaStatus(CSCReloaderDriverAlarms.OutOfOrder, true);
                        }
                        break;
                }
                _IsCardInitializationRequestedPending = false;
                _bMediaHasDetected = false;
                _isNewVirginCardDetected = false;
                _ReaderInitialized = false;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, "TTMain.RepetitiveTreatment " + ex.Message);
            }
            finally
            {
                base.RepetitiveTreatment();
            }
        }//public override void RepetitiveTreatment()
        // in case of polling....

        private void UpdateForSAmQuota()
        {
            UpdateOneValue(DSMDriverAlarms.SAM1Counter, _samCurrvalue.ToString());
            UpdateOneValue(DSMDriverAlarms.SAM1Quota, _samKUCQuota.ToString());
            UpdateOneValue(DSMDriverAlarms.SAM1SerialNumber, _samSerialNumber.ToString());
            if (_samKUCQuota == -1)
            {
                //It means that there is no quota
                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, false);
                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, false);
            }
            else if ((_samKUCQuota - _samCurrvalue) < Configuration.ReadIntParameter("SAMQuotaLowThreshold", 40))
            {
                if ((_samKUCQuota - _samCurrvalue) <= 0)
                {
                    UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, false);
                    UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, true);
                }
                else
                {
                    UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, true);
                    UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, false);
                }
            }
            else
            {
                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaLow, false);
                UpdateOneAlarm(DSMDriverAlarms.SAM1QuotaEmpty, false);
            }
        }


        private bool CheckForMediaRelatedActivity(MediaOperation op, ref AskToInitialiseCardtoTT mCardData, out int errocode)
        {
            bool ret = false;
            _isProcessingCard = true;
            Logging.Log(LogLevel.Verbose, "CheckForMediaRelatedActivity In, Operation:" + op.ToString());
            errocode = -1;
            //SKS: Added on 20190424
            Logging.Log(LogLevel.Verbose, "CheckForMediaRelatedActivity Switching to card ...");
            int re = (int)mDFCairo.GetReaderInstance().RW_SwitchToCardOnState();
            Logging.Log(LogLevel.Verbose, "Switching to card removal.ret:" + re.ToString());
            //~SKS
           // try // removing as exception handling has to be taken care by calling function..
            {
                switch (op)
                {
                    case MediaOperation.INITIALIZE:
                        {
                            // if (_isNewVirginCardDetected)
                            {
                                ret = false;
                                DateTime mdt = mCardData.EndOfValidity;
                                UInt16 dosdate = CFunctions.ToDosDate2(mdt);
                                if (_bChangeKeysBeforeCreatingFiles)
                                    ret = mDFCairo.InitializeCardEx((uint)mCardData.EngravedNumber, (uint)mCardData.MediaArtworkID, dosdate, out errocode);
                                else
                                    ret = mDFCairo.InitializeCard((uint)mCardData.EngravedNumber, (uint)mCardData.MediaArtworkID, dosdate, out errocode);
                                if (!ret) _cardNumwithFailedRWAttempt = _cardSerialNum;
                            }
                        }
                        break;
                    case MediaOperation.FORMATE:
                        {
                            ret = mDFCairo.FormateCard(true);
                        }
                        break;
                    case MediaOperation.READ:
                        {
                        }
                        break;
                    case MediaOperation.WRITE:
                        {
                        }
                        break;
                    default:
                        {
                        }
                        break;
                }//switch

            }           
            bool r= mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();// 
            _isProcessingCard = false;
            Logging.Log(LogLevel.Verbose, "CheckForMediaRelatedActivity out, switch removal state: " + r.ToString());
            return ret;
        }
        private bool CheckForMediaRelatedActivity(MediaOperation op, UInt16 cardvalidity)
        {
            bool ret = false;
            UInt32 cardno = 1234;
            UInt32 artwork = 1;
           // MediaDetails mMediaDet;
            Logging.Log(LogLevel.Verbose, "CheckForMediaRelatedActivity In, Operation:" + op.ToString());

            //SKS: Added on 20190424
            Logging.Log(LogLevel.Verbose, "CheckForMediaRelatedActivity 1 Switching to card ...");
            int re = (int)mDFCairo.GetReaderInstance().RW_SwitchToCardOnState();
            Logging.Log(LogLevel.Verbose, "CheckForMediaRelatedActivity 1 Switching to card removal.ret:" + re.ToString());
            //~SKS
            
           // try
            {
                switch (op)
                {
                    case MediaOperation.INITIALIZE:
                        {
                            
                            {
                                ret = false;
                                int errocode = -1;
                                if(_bChangeKeysBeforeCreatingFiles)
                                    ret = mDFCairo.InitializeCardEx(cardno, artwork, cardvalidity, out errocode);
                                else
                                    ret = mDFCairo.InitializeCard(cardno, artwork, cardvalidity, out errocode);
                                if (!ret) _cardNumwithFailedRWAttempt = _cardSerialNum;
                            }
                        }
                        break;
                    case MediaOperation.FORMATE:
                        {
                            ret = mDFCairo.FormateCard(true);
                        }
                        break;
                    case MediaOperation.READ:
                        {
                        }
                        break;
                    case MediaOperation.WRITE:
                        {
                        }
                        break;
                    default:
                        {
                        }
                        break;
                }//switch

            }
            
          //  if (switchtoremovaldet)
                mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();// 
            return ret;
        }
        private void ReportInitializationNAK(int errocode,string xmldata, bool bswitchtodetectionremoval)
        {
            //_nbReattempt = 0;
            SendMessage(ThreadName, "", "InitializeCardAnswer", errocode.ToString(), xmldata);
            if(bswitchtodetectionremoval)
                mDFCairo.GetReaderInstance().RW_SwitchToDetectRemovalState();
        }

        #region "CSC Media Treatment"
        private void RestartPolling()
        {
            _isNewVirginCardDetected = false;
            mDFCairo.GetReaderInstance().RW_StopField();
            Thread.Sleep(50);
            mDFCairo.GetReaderInstance().RW_StartField();
            Thread.Sleep(50);
            mDFCairo.GetReaderInstance().RW_StartPolling();
        }

        private void EnableDetection(bool onoff)
        {
            if (onoff)
            {
                mDFCairo.GetReaderInstance().RW_StartField();
                Thread.Sleep(100);
                mDFCairo.GetReaderInstance().RW_StartPolling();
            }
            else
            {
                mDFCairo.GetReaderInstance().RW_StopField();
                Thread.Sleep(100);
               // mDFCairo.mCSCDesfireRW.RW_StopPolling();
            }
        }
        protected void StatusListenerMediaProduced(
           IntPtr code, IntPtr status
           )
        {
            DateTime msgReceptionTimestamp = DateTime.Now;
            Logging.Log(LogLevel.Verbose, "New Media Detected...");

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));

            Communication.SendMessage(ThreadName, "", "MediaProduced", SerializeHelper<StatusCSC>.XMLSerialize(pStatusCSC), msgReceptionTimestamp.Ticks.ToString());

            code = IntPtr.Zero;
            status = IntPtr.Zero;

        }
        public void StatusListenerMediaRemoved(
          IntPtr code, IntPtr status
          )
        {
            DateTime msgReceptionTimestamp = DateTime.Now;
            Logging.Log(LogLevel.Verbose, "Media Removed...");
            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            // memory occupied by status gets leaked, but we are helpless, as attempt to free it causes crash

            Communication.SendMessage("", "", "MediaRemoved",msgReceptionTimestamp.Ticks.ToString());
            code = IntPtr.Zero;
            status = IntPtr.Zero;
        }
        #endregion

    }
}
