using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.CSCReaderAdaptor
{
    public class V4Adaptor
    {
        [DllImport("ThalesCscApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern short sCSCReaderGetApiVersionEx
                               (IntPtr piMajorVersion,
                                IntPtr piMinorVersion);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sCSCReaderStartEx(
              [MarshalAs(UnmanagedType.LPStr)] string pszComName,
              int ulSpeed,
              out int phRw);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sCSCReaderStopEx(
                int phRw);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sCscRebootEx(
                int phRw);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartStatusEx(
                int phRw,
                out StatusCSC pStatusCSC);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sCscConfigEx(
            int phRw,
            out CSC_BOOTIDENT pFirmwareName);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartInstCardEx(
            int phRw,
            DEST_TYPE pDestReaderType,
            IntPtr pxInstallCard);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartHaltCardEx(
            int phRw);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartConfigEx(
            int phRw,
            byte ucNumScenario,
            byte ucNbRecord,
            [In] ScenarioPolling[] pxConfigPolling);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartISOEx(
            int phRw,
            DEST_TYPE pDestReaderType,
            short pusInDataLen,
            byte[] pucDataIn,
            IntPtr pusOutDataLen,
            IntPtr pucDataOut);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartStartPollingEx(
            int phRw,
            byte ucNumScenario,
            AC_TYPE pxAC,
            byte ucSpontCall,
            IntPtr pvCallBackEx);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartStartDetectRemovalEx(
            int phRw,
            byte ucSpontCall, // Specifies the notification method to use when the CSC removal is detected by the reader. The two possible values are:
                             // DETECTION_WITH_EVENT (the callback routine is executed) or DETECTION_WITHOUT_EVENT (no callback)
            IntPtr pvCallBackEx);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartStopDetectRemovalEx(
            int phRw);

        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartStopPollingEx(
            int phRw);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void fCallBackEx(int hRw,
                                         out StatusCSC pStatusCSC);


        [DllImport("ThalesCscApi.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static short sSmartFieldEx(
            int phRw,
            byte ucFieldState);
    }
}
