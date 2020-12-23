using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CryptoFlex;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace TestCSCReader
{
    public class Class1
    {
        static void Main(string[] args)
        {
            ReaderComm pReaderComm;
            CSC_API_ERROR pError;

            //Attention : change the port settings as per
            //current configuration
            pReaderComm.COM_PORT = "COM12:";
            pReaderComm.COM_SPEED = 115200;

            FirmwareInfo pFirmware;

            int phRw;

            DateTime dt = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);

            ushort dosDate = CFunctions.ToDosDate(DateTime.Now);
            ushort dosTime = CFunctions.ToDosTime(DateTime.Now);

            //Reload Reader Test
            pError = Reader.ReloadReader(CSC_READER_TYPE.V4_READER,
                                            pReaderComm,
                                            out phRw,
                                            out pFirmware);

            CryptoFlexFunctions cflex = new CryptoFlexFunctions(CSC_READER_TYPE.V4_READER, phRw);

            int epnbr = cflex.GetEQPLocalId(DEST_TYPE.DEST_SAM1);

            //Load Keys for Test
            string sk = File.ReadAllText("TestKey.xml");

            SecurityMgr.Instance.LoadKeyList(CSC_READER_TYPE.V4_READER, phRw, sk);

            ////////////////////

            //Install Card test
            //InstallCard pSamCardParams = new InstallCard();

            //pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            //pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)(DEST_TYPE.DEST_SAM1);
            //pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T0;
            //pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            //pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);

            
            //pError = pReaderFunc.InstallCard(CSC_READER_TYPE.V3_READER,
            //                                 phRw,
            //                                 DEST_TYPE.DEST_SAM1,
            //                                 pSamCardParams);

            ///////////////////

            //Iso Commands
            //byte[] pCommandApdu = new byte[] { 0xC0, 0x20, 0x00, 0x01, 0x08, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };

            //byte pSw1;
            //byte pSw2;
            //byte[] pResData;
                
            // pError = pReaderFunc.IsoCommand(CSC_READER_TYPE.V3_READER,
            //                                 phRw,
            //                                 DEST_TYPE.DEST_SAM1,
            //                                 pCommandApdu,
            //                                 out pSw1,
            //                                 out pSw2,
            //                                 out pResData);
            ///////////////////
            
            /* Token Functions Test */
            byte pSw1;
            byte pSw2;
            byte[] pResData;
            StatusCSC pStatusCSC;

            //Prepare the logical media for the Test
            LogicalMedia _logicalMediaToken = new LogicalMedia();
            LogicalMedia _logicalMediaCsc = new LogicalMedia();

            //pError = TokenFunctions.InitProbe(CSC_READER_TYPE.V4_READER,
            //                                  phRw);

            //pError = Reader.StatusCheck(CSC_READER_TYPE.V4_READER, phRw, out pStatusCSC);

            //byte[] ba = Encoding.Default.GetBytes(pStatusCSC.ucATR);

            ///* Mac Generation test */
            //byte[] testData = new byte[]{ 0x04, 0xEB, 0x93, 0xF4, 0x52, 0xDF, 0x2C, 0x80, 0x21, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            //                                0x67, 0x43, 0x67, 0x43, 0x00, 0x80, 0xB2, 0xF0 };

            //byte[] mac = SecurityMgr.Instance.GenerateMAC(testData);

            //pError = TokenFunctions.ReadBlocks(CSC_READER_TYPE.V4_READER,
            //                                   phRw,
            //                                   3,
            //                                   out pSw1,
            //                                   out pSw2,
            //                                   out pResData);



            //Stop Reader
            pError = Reader.StopReader(CSC_READER_TYPE.V4_READER,
                                            phRw);
            /////////////////
        }


    }
}
