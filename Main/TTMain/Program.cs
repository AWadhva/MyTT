using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.CryptoFlex;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
//using IFS2.Equipment.CSCReaderAdaptor;

namespace IFS2.Equipment.TicketingRules.TTMain
{
    class Program
    {
        static int _ReaderType;

        static void Main(string[] args)
        {
            CSC_API_ERROR Err;
            ReaderComm pReaderComm;
            FirmwareInfo pFirmware;
            int phRw = 1;            

            //CFunctions.PrintBitsConsole(CFunctions.ConvertToBits(3, 8));
            
            //Load the Comm params
            pReaderComm.COM_PORT = "COM2:";
            pReaderComm.COM_SPEED = 115200;

            //_ReaderType = (int)Configuration.ReadParameter("ReaderType", "int", "3");

            //FareParameters.BuildFareTierMatrix();
            //FareParameters.BuildFareGroupTable();
            //FareParameters.BuildDayTypeCalender();
            //FareParameters.BuildGlobalFareTable();

            //SalesRules Sale = new SalesRules();

            //long TokenPrice = Sale.CalculateTokenPrice(1, 47);

            _ReaderType = 4;

            Reader.ReloadReader((CSC_READER_TYPE)_ReaderType, pReaderComm, out phRw, out pFirmware);

            //CryptoFlexFunctions cflex = new CryptoFlexFunctions((CSC_READER_TYPE)_ReaderType, phRw);

            //byte[] certificate = new byte[608];
            //CertData pCertData;

            //certificate = cflex.GetCertificate(DEST_TYPE.DEST_SAM1, CERT_TYPE.LOCAL_CERT);

            //cflex.GetDataFromCert(certificate, out pCertData);

            //Console.Write(pCertData.NotAfter);
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();
            //Console.Write(pCertData.NotBefore);
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();
            //Console.Write(pCertData.Subject);
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();


            //byte[] pDataIn = new byte[] { 0x6F, 0x77, 0x20, 0x6F, 0x6C, 0x6C, 0x65, 0x48 };

            //certData = cflex.InternalAuthDes(DEST_TYPE.DEST_SAM1, 0, pDataIn);

            //String Cstr = ByteArrayToString(certData);

            //DesfireKey[] pDesKeys = new DesfireKey[13];

            //pDesKeys[0].ApplicationID = 0x01;
            //pDesKeys[0].FileID = 0x22;
            //pDesKeys[0].KeyCardNo = 0x03;
            //pDesKeys[0].KeyNumber = 0x03;
            //pDesKeys[0].KeySet = 0x01;
            //pDesKeys[0].KeyValue = new byte[] { 0x78, 0x7B, 0xCF, 0x02, 0x01, 0x3D, 0xAF, 0xAE, 0x11, 0x4F, 0x74, 0x8E, 0x26, 0x69, 0xE4, 0xFD };

            //pDesKeys[1].ApplicationID = 0x01;
            //pDesKeys[1].FileID = 0x21;
            //pDesKeys[1].KeyCardNo = 0x02;
            //pDesKeys[1].KeyNumber = 0x02;
            //pDesKeys[1].KeySet = 0x01;
            //pDesKeys[1].KeyValue = new byte[] { 0x52, 0x1A, 0x74, 0x20, 0x1D, 0xFF, 0x17, 0x49, 0xEB, 0x30, 0x7C, 0x28, 0x73, 0x77, 0x51, 0xC6 };

            //pDesKeys[2].ApplicationID = 0x01;
            //pDesKeys[2].FileID = 0x43;
            //pDesKeys[2].KeyCardNo = 0x04;
            //pDesKeys[2].KeyNumber = 0x04;
            //pDesKeys[2].KeySet = 0x01;
            //pDesKeys[2].KeyValue = new byte[] { 0x2D, 0x3F, 0x5C, 0xD7, 0x16, 0x45, 0x13, 0x9C, 0xF9, 0x56, 0x95, 0xDF, 0xD7, 0xE8, 0xCF, 0x91 };

            //pDesKeys[3].ApplicationID = 0x01;
            //pDesKeys[3].FileID = 0x15;
            //pDesKeys[3].KeyCardNo = 0x05;
            //pDesKeys[3].KeyNumber = 0x05;
            //pDesKeys[3].KeySet = 0x01;
            //pDesKeys[3].KeyValue = new byte[] { 0xA9, 0x52, 0x0B, 0xE5, 0x02, 0x75, 0x39, 0xF4, 0x94, 0xD5, 0xF9, 0x03, 0x4A, 0x5C, 0x32, 0x14 };

            //pDesKeys[4].ApplicationID = 0x01;
            //pDesKeys[4].FileID = 0x16;
            //pDesKeys[4].KeyCardNo = 0x06;
            //pDesKeys[4].KeyNumber = 0x06;
            //pDesKeys[4].KeySet = 0x01;
            //pDesKeys[4].KeyValue = new byte[] { 0x38, 0xF9, 0xC3, 0x50, 0x2B, 0x34, 0x4E, 0xB0, 0x5D, 0xEB, 0x6B, 0x93, 0x68, 0x93, 0xB3, 0x1A };

            //pDesKeys[5].ApplicationID = 0x01;
            //pDesKeys[5].FileID = 0x08;
            //pDesKeys[5].KeyCardNo = 0x08;
            //pDesKeys[5].KeyNumber = 0x08;
            //pDesKeys[5].KeySet = 0x01;
            //pDesKeys[5].KeyValue = new byte[] { 0x35, 0x1F, 0x9C, 0xB8, 0xBB, 0xED, 0x8C, 0xCB, 0x62, 0xD7, 0x4E, 0xF1, 0xFF, 0x7D, 0xA1, 0x12 };

            //pDesKeys[6].ApplicationID = 0x01;
            //pDesKeys[6].FileID = 0x09;
            //pDesKeys[6].KeyCardNo = 0x09;
            //pDesKeys[6].KeyNumber = 0x09;
            //pDesKeys[6].KeySet = 0x01;
            //pDesKeys[6].KeyValue = new byte[] { 0x03, 0x32, 0x5B, 0x5B, 0x0B, 0x2F, 0x2A, 0x25, 0x6D, 0x90, 0x8F, 0xF9, 0x97, 0x33, 0xB1, 0xD2 };

            //pDesKeys[7].ApplicationID = 0x01;
            //pDesKeys[7].FileID = 0x09;
            //pDesKeys[7].KeyCardNo = 0x09;
            //pDesKeys[7].KeyNumber = 0x09;
            //pDesKeys[7].KeySet = 0x01;
            //pDesKeys[7].KeyValue = new byte[] { 0x03, 0x32, 0x5B, 0x5B, 0x0B, 0x2F, 0x2A, 0x25, 0x6D, 0x90, 0x8F, 0xF9, 0x97, 0x33, 0xB1, 0xD2 };

            //pDesKeys[8].ApplicationID = 0x02;
            //pDesKeys[8].FileID = 0x11;
            //pDesKeys[8].KeyCardNo = 0x01;
            //pDesKeys[8].KeyNumber = 0x01;
            //pDesKeys[8].KeySet = 0x02;
            //pDesKeys[8].KeyValue = new byte[] { 0xDF, 0x25, 0xE0, 0xE0, 0x6E, 0xD6, 0x1A, 0x62, 0x75, 0x11, 0x79, 0xA0, 0x35, 0xB5, 0x7B, 0x6A };

            //pDesKeys[9].ApplicationID = 0x02;
            //pDesKeys[9].FileID = 0x12;
            //pDesKeys[9].KeyCardNo = 0x01;
            //pDesKeys[9].KeyNumber = 0x01;
            //pDesKeys[9].KeySet = 0x02;
            //pDesKeys[9].KeyValue = new byte[] { 0xDF, 0x25, 0xE0, 0xE0, 0x6E, 0xD6, 0x1A, 0x62, 0x75, 0x11, 0x79, 0xA0, 0x32, 0xB5, 0x7B, 0x6A };

            //pDesKeys[10].ApplicationID = 0x02;
            //pDesKeys[10].FileID = 0x13;
            //pDesKeys[10].KeyCardNo = 0x01;
            //pDesKeys[10].KeyNumber = 0x01;
            //pDesKeys[10].KeySet = 0x02;
            //pDesKeys[10].KeyValue = new byte[] { 0xDF, 0x25, 0xE0, 0xE0, 0x6E, 0xD6, 0x1A, 0x62, 0x75, 0x11, 0x79, 0xA0, 0x32, 0xB5, 0x7B, 0x6A };

            //pDesKeys[11].ApplicationID = 0x02;
            //pDesKeys[11].FileID = 0x08;
            //pDesKeys[11].KeyCardNo = 0x01;
            //pDesKeys[11].KeyNumber = 0x01;
            //pDesKeys[11].KeySet = 0x02;
            //pDesKeys[11].KeyValue = new byte[] { 0xDF, 0x25, 0xE0, 0xE0, 0x6E, 0xD6, 0x1A, 0x62, 0x75, 0x11, 0x79, 0xA0, 0x32, 0xB5, 0x7B, 0x6A };

            ////Card master Key
            //pDesKeys[12].ApplicationID = 0x00;
            //pDesKeys[12].FileID = 0x00;
            //pDesKeys[12].KeyCardNo = 0x00;
            //pDesKeys[12].KeyNumber = 0x00;
            //pDesKeys[12].KeySet = 0x00;
            //pDesKeys[12].KeyValue = new byte[] { 0xB0, 0xF1, 0x60, 0x3C, 0x8C, 0xA1, 0xA3, 0xC7, 0x8F, 0x66, 0x99, 0x50, 0xC0, 0x99, 0xB9, 0x50 };

            //SecurityMgr.Instance.LoadDesfireKey((CSC_READER_TYPE)_ReaderType, phRw, pDesKeys);

            ////UltraLightKey[] pTokenKey = new UltraLightKey[2];

            ////pTokenKey[1].KeyVersion = 0x02;
            ////pTokenKey[1].KeyValue = new byte[]{0x57, 0x1B, 0x65, 0x73, 0x9E, 0x3C, 0xA8, 0x46};

            ////SecurityMgr.Instance.LoadUltraLightKey(pTokenKey);

            SmartFunctions.Instance.SetReaderType(_ReaderType, phRw);
            ////TokenFunctions sToken = new TokenFunctions((CSC_READER_TYPE)_ReaderType, phRw);

            byte pSW1 = 0xFF;
            byte pSW2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

            SmartFunctions.Instance.Init(true);
            /*
            bool check;
            bool check2;
            SmartFunctions.Instance.SmartSyncDetectOk(out check, out check2);
            */
            //LogicalMedia _logMediaReloader = null;
            //DelhiDesfireEV0 hwCsc = new DelhiDesfireEV0();

            //hwCsc.ReadMediaData(_logMediaReloader);

            //InstallCard pCscCardParams = new InstallCard();

            //pCscCardParams.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            //pCscCardParams.iCardParam.xMifParam.sSize = 0;

            //ScenarioPolling[] pScenarioPolling = new ScenarioPolling[1];

            //pScenarioPolling[0].xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            //pScenarioPolling[0].ucAntenna = (byte)(CONSTANT.SMART_ANTENNA_1);
            //pScenarioPolling[0].ucRepeatNumber = (byte)1;

            //Err = Reader.InstallCard((CSC_READER_TYPE)_ReaderType,
            //                         phRw,
            //                         DEST_TYPE.DEST_CARD,
            //                         pCscCardParams);

            //V3Adaptor.fCallBackEx fcall = DelegateMethodTest;

            //ERR_CODE = V3Adaptor.sSmartConfigEx(phRw, 0x01, (byte)pScenarioPolling.Length, pScenarioPolling);

            //ERR_CODE = V3Adaptor.sSmartStartPollingEx(phRw, 0x01, AC_TYPE.AC_WITHOUT_COLLISION, CONSTANT.DETECTION_WITH_EVENT, fcall);

            SmartFunctions.Instance.SelectApplication(0x01);

            SmartFunctions.Instance.ReadPersonalizationFile(out pSW1, out pSW2, out pResData);
            short ServiceProviderRead1 = (short)CFunctions.GetBitData(120, 8, pResData);

            int pValue;

            SmartFunctions.Instance.ReadPurseFile(out pValue,out pSW1, out pSW2);

            SmartFunctions.Instance.SelectApplication(0x02);

            SmartFunctions.Instance.ReadMetroAddValueFile(out pSW1, out pSW2, out pResData);

            short ServiceProviderRead = (short)CFunctions.GetBitData(150, 8, pResData);
            DateTime DateTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(32, 32, pResData));
            int AmountRead = (int)CFunctions.GetBitData(64, 16, pResData);
            int NewBalanceRead = (int)CFunctions.GetBitData(80, 32, pResData);
            int EquipmentNumberRead = (int)CFunctions.GetBitData(112, 24, pResData);
            long SequenceNumberRead = (long)CFunctions.GetBitData(0, 32, pResData);
            short OperationTypeRead = (short)CFunctions.GetBitData(136, 6, pResData);
            int LocationRead = (int)CFunctions.GetBitData(142, 8, pResData);
            DateTime LastDateRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(158, 32, pResData));

            //pResData[0] = 0x06;

            //SmartFunctions.Instance.WriteMetroSaleFile(out pSW1, out pSW2, pResData);

            //SmartFunctions.Instance.CommitTransaction(out pSW1, out pSW2);

            //sToken.InitProbe();
            //sToken.ReadBlocks(4, out pSW1, out pSW2, out pResData);

            //long ChipSerialNumberRead = (long)CFunctions.GetBitData(0, 56, pResData);

            //byte[] pDataIn = CFunctions.GetBytesFromResp(0, 56, pResData);

            //byte[] mac = SecurityMgr.Instance.GenerateMAC(0x02, pDataIn);

            
            //ulong manufacture = CFunctions.GetBitData(56, 72, pResData);   
            //ulong Mac = CFunctions.GetBitData(192, 64, pResData);


            //DateTime InitialisationDateRead = CFunctions.ConvertDosDate(128, pResData);
            //DateTime DateTimeRead = CFunctions.ConvertDosDate(144, pResData);
            //short DesignTypeRead = (short)CFunctions.GetBitData(160, 8, pResData);
            //short LanguageRead = (short)CFunctions.GetBitData(168, 1, pResData);
            //short OwnerRead = (short)CFunctions.GetBitData(169, 5, pResData);
            //short FareTiersRead = (short)CFunctions.GetBitData(174, 6, pResData);
            //short LocationRead = (short)CFunctions.GetBitData(181, 8, pResData);
            //short SequenceNumberRead = (short)CFunctions.GetBitData(272, 18, pResData);
            //int EquipmentNumberRead = (int)CFunctions.GetBitData(290, 24, pResData);
            //int pLocationRead = (int)CFunctions.GetBitData(314, 8, pResData);
            //int DestinationRead = (int)CFunctions.GetBitData(338, 8, pResData);
            //int TripsRead = (int)CFunctions.GetBitData(362, 2, pResData);
            //short RejectCodeRead = (short)CFunctions.GetBitData(364, 8, pResData);
            //short pTripsRead = (short)CFunctions.GetBitData(372, 1, pResData);
            //int AmountRead = (int)CFunctions.GetBitData(374, 10, pResData);

            
            Err = Reader.StopReader((CSC_READER_TYPE)_ReaderType,
                                    phRw);

#if WindowsCE
            Thread mainThread = null;
            MainTicketingRules main = new MainTicketingRules();
            if (main != null)
            {
                try
                {
                    mainThread = new Thread(new ThreadStart(main.ThreadProc));
                }
                catch (Exception e)
                {
                    Logging.Log(LogLevel.Error, "MainThread Starting " + e.Message);
                }
                mainThread.Start();
                Logging.Log(LogLevel.Information, "main thread launched");
            }
#endif           
        }
        
//        public static void DelegateMethodTest(int hRw,
//                                              out StatusCSC pStatusCSC)
//        {
//            Console.WriteLine("Test Pass");
//            Console.WriteLine("Push a key to continue");
//#if !WindowsCE
//            Console.ReadKey();
//#endif
            
//            /*
//            pStatusCSC.ucAntenna = 0x00;
//            pStatusCSC.ucATR = "";
//            pStatusCSC.ucLgATR = 0x00;
//            pStatusCSC.ucNbDetectedCard = 0x00;
//            pStatusCSC.ucStatCSC = 0x00;
//            pStatusCSC.xCardType = 0;
//             * */
//        }
    }
}

