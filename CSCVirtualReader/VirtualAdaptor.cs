using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.Common.Simulators.VirtualCSCReader;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class CSCThalesVirtualReader
    {
        [XmlRoot("StatusCSC")]
        private class StatusCSC_i
        {
            [XmlElement("ucStatCSC")]
            public byte ucStatCSC=0;

            [XmlElement("ucNbDetectedCard")]
            public byte ucNbDetectedCard=0;

            [XmlElement("xCardType")]
            public int xCardType=0;

            [XmlElement("ucAntenna")]
            public byte ucAntenna=0;

            [XmlElement("ucLgATR")]
            public byte ucLgATR=0;

            [XmlElement("ucATR")]
            public string ucATR = "";

            public StatusCSC_i()
            {
            }
        };

        private bool _detectionDetection=false;
        private bool _detectionRemoval=false;
        Utility.StatusListenerDelegate listenerDetection=null;
        Utility.StatusListenerDelegate listenerRemoval=null;
        private static CSCThalesVirtualReader _instance = null;

        private static SimulationCSCReaderInterface adapter = null; 
 
        public CSCThalesVirtualReader()
        {
            adapter = new SimulationCSCReaderInterface();
            _instance = this;
        }

        public static short sSmartInstCardEx(int phRw,DEST_TYPE pDestType,InstallCard pInstCardParams)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sCSCReaderGetApiVersionEx (out int MajorVersion,out int MinorVersion)
        {
            MinorVersion = MajorVersion = 0;
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            MajorVersion = adapter.GetMajorVersion();
            MinorVersion = adapter.GetMinorVersion();
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sCSCReaderStartEx(string pszComName, int ulSpeed, out int phRw)
        {
            phRw = 0;
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return adapter.StartReader(pszComName, ulSpeed, out phRw);
        }

        public static short sCSCReaderStopEx(int phRw)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return adapter.StopReader(phRw);
        }

        public static short sCscRebootEx(int phRw)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return adapter.RebootReader(phRw);
        }

        public static short sSmartStatusEx(int phRw, out StatusCSC pStatusCSC)
        {
            pStatusCSC = new StatusCSC();
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            string s;
            short ret = adapter.Status(phRw, out s);
            StatusCSC_i csc = new StatusCSC_i();
            if (s != "") csc=SerializeHelper<StatusCSC_i>.XMLDeserialize(s);
            pStatusCSC.ucStatCSC = csc.ucStatCSC;
            pStatusCSC.ucNbDetectedCard = csc.ucNbDetectedCard;
            pStatusCSC.xCardType = csc.xCardType;
            pStatusCSC.ucAntenna = csc.ucAntenna;
            pStatusCSC.ucLgATR = csc.ucLgATR;
            pStatusCSC.ucATR = Convert.FromBase64String(csc.ucATR);
            return ret;
        }

        public static short sCscConfigEx(int phRw, out CSC_BOOTIDENT pFirmwareName)
        {
            pFirmwareName = new CSC_BOOTIDENT();
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sSmartHaltCardEx(int phRw)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sSmartConfigEx(int phRw, byte ucNumScenario, byte ucNbRecord,ScenarioPolling[] pxConfigPolling)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sSmartISOEx(int phRw, DEST_TYPE pDestReaderType, short pusInDataLen, byte[] pucDataIn, out short DataLen, out byte[] DataOut)
        {
            DataLen = 0;
            DataOut = null;
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return (short)CSC_API_ERROR.ERR_NONE;
        }


        public static short sSmartStartPollingEx(int phRw,byte ucNumScenario,AC_TYPE pxAC,byte ucSpontCall,Utility.StatusListenerDelegate listener)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            if (listener == null)
            {
                _instance._detectionDetection = false;
                _instance.listenerDetection = null;
            }
            else
            {
                _instance._detectionDetection = true;
                _instance.listenerDetection = listener;
            }
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sSmartStartDetectRemovalEx(int phRw, byte ucSpontCall)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
                _instance._detectionRemoval = false;
                _instance.listenerRemoval = null;
            return (short)CSC_API_ERROR.ERR_NONE;
        }
        public static short sSmartStartDetectRemovalEx(int phRw, byte ucSpontCall, Utility.StatusListenerDelegate listener)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            if (listener==null)
            {
                _instance._detectionRemoval=false;
                _instance.listenerRemoval = null;
            }
            else
            {
                _instance._detectionRemoval=true;
                _instance.listenerRemoval = listener;
            }
            return (short)CSC_API_ERROR.ERR_NONE;
        }

        public static short sSmartStopDetectRemovalEx(int phRw)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            _instance._detectionRemoval=false;
            return adapter.StopPolling(phRw);
        }

        public static short sSmartStopPollingEx(int phRw)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            _instance._detectionDetection = false;
            _instance._detectionRemoval = false;
            return adapter.StopPolling(phRw);
        }

       public static short sSmartFieldEx(int phRw,byte ucFieldState)
       {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return (short)CSC_API_ERROR.ERR_NONE;
       }

        public static short sCscPingEx(int phRw,short pingSize,short pongSize)
        {
            if (adapter == null) return (short)CSC_API_ERROR.ERR_INTERNAL;
            return (short)CSC_API_ERROR.ERR_NONE;
        }



        //Listening function that should be call by other program
        public void StatusListenerDelegate(int code, string status)
        {
            StatusCSC_i csc=SerializeHelper<StatusCSC_i>.XMLDeserialize(status);
            StatusCSC pStatusCSC = new StatusCSC();
            pStatusCSC.ucStatCSC = csc.ucStatCSC;
            pStatusCSC.ucNbDetectedCard = csc.ucNbDetectedCard;
            pStatusCSC.xCardType = csc.xCardType;
            pStatusCSC.ucAntenna = csc.ucAntenna;
            pStatusCSC.ucLgATR = csc.ucLgATR;
            pStatusCSC.ucATR = Convert.FromBase64String(csc.ucATR);

            if (_detectionDetection)
            {
            }
            if (_detectionRemoval)
            {
            }
        }


    }
}
