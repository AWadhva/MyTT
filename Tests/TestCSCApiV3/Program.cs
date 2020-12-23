using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
//using IFS2.Equipment.CSCReaderAdaptor;


namespace TestCSCApiv3
{
    class Program
    {
         static void Main(string[] args)
        {
            //int ERR_CODE;

            //ReaderComm cV3Com;
            

            //// Creates a test file to capture the communication status
            //System.IO.StreamWriter cscTestfile = new System.IO.StreamWriter("c:\\cscTest.txt", true);
            //cscTestfile.WriteLine("File Started..");

            ///* CSC Api Version check *///////////
            //IntPtr piMajorVersion = Marshal.AllocHGlobal(sizeof(int)),
            //       piMinorVersion = Marshal.AllocHGlobal(sizeof(int));
           
            //ERR_CODE = V3Adaptor.sCSCReaderGetVersionEx(piMajorVersion, piMinorVersion);


            //cscTestfile.WriteLine("sCSCReaderGetVersionEx : " + ERR_CODE);
            //ERR_CODE = 99;
            ////////////////////////////////////////

            ////Start the Reader check//////////////
            //cV3Com.COM_PORT   = "COM2:";
            //cV3Com.COM_SPEED  = 115200;

            //int phCsc;

            //ERR_CODE = V3Adaptor.sCSCReaderStartEx(cV3Com.COM_PORT, cV3Com.COM_SPEED, out phCsc);

            //cscTestfile.WriteLine("sCSCReaderStartEx : " + ERR_CODE);
            //ERR_CODE = 99;
            ///////////////////////////////////////

            ////Reboot Status check////////////////
            //StatusCSC pStatusCSC;
            //int hCsc = 0;

            //ERR_CODE = V3Adaptor.sSmartStatusEx(hCsc, out pStatusCSC);

            //if (pStatusCSC.ucStatCSC == CONSTANT.ST_VIRGIN)
            //{
            //    ERR_CODE = V3Adaptor.sCscRebootEx(hCsc);
            //}

            //cscTestfile.WriteLine("sSmartStatusEx : " + ERR_CODE + pStatusCSC.ucATR);
            //ERR_CODE = 99;

            ////Reader Firmware check/////////////
            //CSC_BOOTIDENT pFirmareName;

            //ERR_CODE = V3Adaptor.sCscConfigEx(hCsc, out pFirmareName);

            //cscTestfile.WriteLine("sCscConfigEx : " + ERR_CODE + 
            //                      "Chargeur : " + Convert.ToString(pFirmareName.ucBootLabel) +
            //                      "AppCSC : " + Convert.ToString(pFirmareName.ucPrgLabel) +
            //                      "Fpga 1 : " + Convert.ToString(pFirmareName.ucFPGA1Label) +
            //                      "Fpga 2 : " + Convert.ToString(pFirmareName.ucFPGA2Label));
            //ERR_CODE = 99;
            //////////////////////////////////////

            ////Install card check///////////////
            //InstallCard pSamCardParams = new InstallCard();

            //pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            //pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)(DEST_TYPE.DEST_SAM1);
            //pSamCardParams.iCardParam.xSamParam.ucProtocolType = (byte)(CONSTANT.SAM_PROTOCOL_T0);
            //pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            //pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);

            //IntPtr piSamCardParams = Marshal.AllocHGlobal(Marshal.SizeOf(pSamCardParams));
            //Marshal.StructureToPtr(pSamCardParams, piSamCardParams, false);

            //ERR_CODE = V3Adaptor.sSmartInstCardEx(hCsc, DEST_TYPE.DEST_SAM1, piSamCardParams);

            //cscTestfile.WriteLine("sSmartInstCardEx : " + ERR_CODE);

            //ERR_CODE = 99;
            //////////////////////////////////////

            ////Smart Config check////////////////
            ////byte pucNumScenario = (byte)(1);
            ////byte pucNbRecord = (byte)(3);

            ////ScenarioPolling[] pScenarioPolling = new ScenarioPolling[3];

            ////pScenarioPolling[0].xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            ////pScenarioPolling[0].ucAntenna = (byte)(CONSTANT.SMART_ANTENNA_1);
            ////pScenarioPolling[0].ucRepeatNumber = (byte)(1);

            ////pScenarioPolling[1].xCardType = (int)(CSC_TYPE.CARD_GTML_CD97);
            ////pScenarioPolling[1].ucAntenna = (byte)(CONSTANT.SMART_ANTENNA_1);
            ////pScenarioPolling[1].ucRepeatNumber = (byte)(7);

            ////pScenarioPolling[2].xCardType = (int)(CSC_TYPE.CARD_SONY);
            ////pScenarioPolling[2].ucAntenna = (byte)(CONSTANT.SMART_ANTENNA_1);
            ////pScenarioPolling[2].ucRepeatNumber = (byte)(1);


            ////IntPtr[] piConfigPolling = new IntPtr[pScenarioPolling.Length];

            ////for (int index = 0; index < pScenarioPolling.Length; index++)
            ////{
            ////    piConfigPolling[index] = Marshal.AllocHGlobal(Marshal.SizeOf(pScenarioPolling[index]));
            ////    Marshal.StructureToPtr(pScenarioPolling[index], piConfigPolling[index], false);
            ////}

            ////ERR_CODE = V3Adaptor.sSmartConfigEx(hCsc, pucNumScenario, pucNbRecord, piConfigPolling);

            ////cscTestfile.WriteLine("sSmartConfigEx : " + ERR_CODE);
            ////ERR_CODE = 99;

            //////////////////////////////////////

            ////Reader Command Response check/////
            //string X = new string('*', CONSTANT.MAX_ISO_DATA_OUT_LENGTH);

            //IntPtr pucDataOut = Marshal.AllocHGlobal(CONSTANT.MAX_ISO_DATA_OUT_LENGTH);
            //IntPtr pusOutDataLen = Marshal.AllocHGlobal(2);           

            ////Verify CHV
            //byte[] commandApdu_chv = new byte[] { 0xC0, 0x20, 0x00, 0x01, 0x08, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };

            //ERR_CODE = V3Adaptor.sSmartISOEx(hCsc, DEST_TYPE.DEST_SAM1, Convert.ToInt16(commandApdu_chv.Length), commandApdu_chv, pusOutDataLen, pucDataOut);

            ////Select 4F00 (Security folder)
            //byte[] commandApdu_Sel1 = new byte[] { 0xC0, 0xA4, 0x00, 0x00, 0x02, 0x4F, 0x00 };

            //ERR_CODE = V3Adaptor.sSmartISOEx(hCsc, DEST_TYPE.DEST_SAM1, Convert.ToInt16(commandApdu_Sel1.Length), commandApdu_Sel1, pusOutDataLen, pucDataOut);

            ////Select 2003 (Local CA)
            //byte[] commandApdu_Sel2 = new byte[] { 0xC0, 0xA4, 0x00, 0x00, 0x02, 0x20, 0x03 };

            //ERR_CODE = V3Adaptor.sSmartISOEx(hCsc, DEST_TYPE.DEST_SAM1, Convert.ToInt16(commandApdu_Sel2.Length), commandApdu_Sel2, pusOutDataLen, pucDataOut);
            
            //unsafe
            //{
            //    byte* opArray = (byte*)pucDataOut.ToPointer();
            //    byte[] opArray_ = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            //    for (int i = 0; i < CONSTANT.MAX_ISO_DATA_OUT_LENGTH; i++)
            //    {
            //        opArray_[i] = *opArray;
            //        opArray++;
            //    }
            //}

            //cscTestfile.WriteLine("sSmartISOEx : Verify CHV : " + ERR_CODE + " Data : " + Marshal.PtrToStringAuto(pucDataOut));
            //ERR_CODE = 99;
            /////////////////////////////////////

            //cscTestfile.Close();
        }
    }
   
}
