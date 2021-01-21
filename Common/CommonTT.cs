using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.CommonTT
{
    /*---------------------------------*/
    /* CSI READER TYPES                */
    /*---------------------------------*/
    public enum CSC_READER_TYPE
    {
        NO_READER = 0,
        V1_READER = 1,  //Not supported 
        V2_READER = 2,  //Not supported 
        V3_READER = 3,
        V4_READER = 4,
        VIRTUAL_READER = 99
    }

    /*---------------------------------*/
    /* CSI API ERROR TYPES             */
    /*---------------------------------*/
    public enum CSC_API_ERROR
    {
        ERR_NONE = 0,
        ERR_PARAM = -1,
        ERR_NOT_AVAIL = -2,
        ERR_DEVICE = -3,
        ERR_TIMEOUT = -4,
        ERR_DATA = -5,
        ERR_NOEXEC = -6,
        ERR_BUSY = -7,
        ERR_API = -8,
        ERR_COM = -9,
        ERR_LINK = -10,
        ERR_INTERNAL = -128
    }

    /*---------------------------------*/
    /* @Normal COMMUNICATION PARAMS    */
    /*---------------------------------*/
    public struct FirmwareInfo
    {
        public string Chargeur;
        public string AppCSC;
        public string Fpga1;
        public string Fpga2;
    }

    /*---------------------------------*/
    /* @Normal COMMUNICATION PARAMS    */
    /*---------------------------------*/
    public struct ReaderComm
    {
        public string COM_PORT;
        public int COM_SPEED;
    }

    /*-------------------------------------*/
    /* @Normal Certificate Data Structure  */
    /*-------------------------------------*/
    public struct CertData
    {
        public string Subject;
        public string NotBefore;
        public string NotAfter;
    }

    /*---------------------------------*/
    /* @Normal V3 INSTALL CARD PARAMS  */
    /*---------------------------------*/


#if WindowsCE
    public struct InstallCard
    {
        public int xCardType;
        public InstallCardParam iCardParam;
    };

    public struct InstallCardParam
    {       
        private static byte[] b = new byte[5 + CONSTANT.MAX_SAM_OPTION_STRING_LEN];
        private static readonly byte[] scratch = new byte[4];
        public struct Install_SAM
        {
            //public byte ucSamSelected;
            public byte ucSamSelected
            {
                get { return b[0]; }
                set { b[0] = value; }
            }
            //public byte ucProtocolType;
            public byte ucProtocolType
            {
                get { return b[1]; }
                set { b[1] = value; }
            }
            //public uint ulTimeOut;
            public uint ulTimeOut
            {
                get
                {
                    scratch[0] = b[2];
                    scratch[1] = b[3];
                    scratch[2] = b[4];
                    scratch[3] = b[5];
                    return BitConverter.ToUInt32(scratch, 0);
                }
                set
                {
                    uint s = value;
                    b[2] = (byte)s; ///((byte)value) & 0x00FF;
                    b[3] = (byte)(s >> 8); //((byte)value)&0xFF00 ;
                    b[4] = (byte)(s >> 16);
                    b[5] = (byte)(s >> 24);
                }
            }
            // [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string acOptionString
            {
                get
                {

                    //for (Int16 i = 4; i < b.Length&& b[i]!='\0'; i++)
                    //{
                    //    Console.WriteLine(b[i].ToString());
                    //}
                    string st = Encoding.UTF8.GetString(b, 4, b.Length - 5);//BitConverter.ToString(b, 4);
                   // Console.WriteLine("Got str: " + st);
                    return st;
                }
                set
                {
                    Console.WriteLine("value Got for string:" + value);
                    byte[] array = Encoding.ASCII.GetBytes(value);
                    short i = 4;
                    foreach (byte element in array)
                    {
                        //  Console.WriteLine(element.ToString());
                        b[i] = element;
                        i++;
                    }

                }

            }
        } ;

        public struct Install_Mifare1
        {
            public short sSize
            {
                get
                {
                    scratch[0] = b[0];
                    scratch[1] = b[1];
                    return BitConverter.ToInt16(scratch, 0);
                }
                set
                {
                    short s = (short)value;
                    b[1] = (byte)(s >> 8); //((byte)value)&0xFF00 ;
                    b[0] = (byte)s; ///((byte)value) & 0x00FF;
                }
            }
            public string acOptionString
            {
                get
                {
                   
                    string st = Encoding.UTF8.GetString(b, 2, b.Length - 2);//BitConverter.ToString(b, 4);
                    //Console.WriteLine("Got str: " + st);
                    return st;
                }
                set
                {
                    byte[] array = Encoding.UTF8.GetBytes(value);
                    short i = 2;
                    foreach (byte element in array)
                    {
                        b[i] = element;
                        i++;
                    }

                }

            }

        };// struct Install_Mifare1
        public Install_Mifare1 xMifParam;
        public Install_SAM xSamParam;

    }
#else

    [StructLayout(LayoutKind.Explicit)]
    public struct InstallCardParam
    {
        [FieldOffset(0)]
        public Install_Mifare1 xMifParam;

        [FieldOffset(0)]
        public Install_SAM xSamParam;
    };
    public struct InstallCard
    {
        public int xCardType;
        public InstallCardParam iCardParam;
    };
#endif

    /*Enumeration of possible targets for commands 
     All may not be applicatble, reserved for future use*/
    public enum DEST_TYPE
    {
        DEST_CARD = 0,          // CARD en 0
        DEST_SAM1 = 1,          // Les SAMs en suivant
        DEST_SAM2 = 2,
        DEST_SAM3 = 3,
        DEST_SAM4 = 4,
        DEST_SAM5 = 5,          // SAM pour MIFARE
        DEST_SAM_MIFARE = 5,    // synonym for DEST_SAM5
        DEST_GEN = 6,           // coupleur Kit CSC
        DEST_SAM6 = 7,          // pseudo SAM pour sécurité FeliCa
        DEST_SAM_FELICA = 7,    // synonym for DEST_SAM6
        DEST_SAM7 = 8,
        DEST_SAM_DESFIRE = 8,   // synonym for DEST_SAM7
        DEST_TSAM = 9,          // TSAM,
        DEST_PICC_TRANSPARENT = 11,
        MAX_READER_DEST
    };

    public enum Scenario
    {
        SCENARIO_1 = 1,
        SCENARIO_2 = 2
    };

    /* Enumeration of supported technologies
    All may not be applicatble, reserved for future use*/
    public enum CSC_TYPE
    {
        CARD_NONE = 0,
        CARD_GTML_CD97 = 1,
        CARD_SONY = 2,
        CARD_MIFARE1 = 3,
        CARD_SAM = 4,
        CARD_CTS = 5,
        CARD_14443_A = 6,
        RF_CTRL = 7,
        CARD_14443_B = 8,
        CARD_SR176 = 9,
        CARD_PICOPASS = 10,
        CARD_DEF_2 = 11,
        CARD_DEF_3 = 12,
        CARD_DEF_4 = 13,
        MAX_CARD_TYPE = 14
    };

    public enum CERT_TYPE
    {
        LOCAL_CERT = 0,
        CA_CERT = 1
    };

    public enum AC_TYPE
    {
        AC_NOT_DEFINED = 0,
        AC_WITHOUT_COLLISION = 1,
        AC_WITH_COLLISION_METHOD_1 = 2,   // Collision if more than one card
        AC_WITH_COLLISION_METHOD_2 = 3,   // RFU
        AC_WITH_COLLISION_METHOD_3 = 4,   // RFU
        MAX_AC_TYPE = 5
    };

    /*----------------------------*/
    /* @Marshal Smart card status */
    /*----------------------------*/
    //#if WindowsCE
    //    [StructLayout(LayoutKind.Sequential)]
    //    public unsafe struct InternalStatusCSC
    //    {
    //        //[MarshalAs(UnmanagedType.U1)]
    //        public byte ucStatCSC;

    //        //[MarshalAs(UnmanagedType.U1)]
    //        public byte ucNbDetectedCard;

    //        public int xCardType;

    //       // [MarshalAs(UnmanagedType.U1)]
    //        public byte ucAntenna;

    //       // [MarshalAs(UnmanagedType.U1)]
    //        public byte ucLgATR;

    //       // [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.MAX_ATR_SIZE)]
    //        public fixed byte ucATR[CONSTANT.MAX_ATR_SIZE];
    //        //public string ucATR;

    //    };
    //    public struct StatusCSC
    //    {        
    //        public byte ucStatCSC;        
    //        public byte ucNbDetectedCard;
    //        public int xCardType;
    //        public byte ucAntenna;
    //        public byte ucLgATR;            
    //        public byte[] ucATR;
    //    };
    //#else
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct StatusCSC
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte ucStatCSC;

        [MarshalAs(UnmanagedType.U1)]
        public byte ucNbDetectedCard;

        public int xCardType;

        [MarshalAs(UnmanagedType.U1)]
        public byte ucAntenna;

        [MarshalAs(UnmanagedType.U1)]
        public byte ucLgATR;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CONSTANT.MAX_ATR_SIZE)]
        public byte[] ucATR;

        public StatusCSC(StatusCSC other)
        {
            ucStatCSC = other.ucStatCSC;
            ucNbDetectedCard = other.ucNbDetectedCard;
            xCardType = other.xCardType;
            ucAntenna = other.ucAntenna;
            ucLgATR = other.ucLgATR;
            ucATR = (byte[])(other.ucATR.Clone());
        }
    };
    //#endif
#if !WindowsCE
    /*-----------------------------*/
    /* @Marshal Install_SAM Params */
    /*-----------------------------*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct Install_SAM
    {
        public byte ucSamSelected;
        public byte ucProtocolType;
        public uint ulTimeOut;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1)]
        public string acOptionString;
    };

    /*---------------------------------*/
    /* @Marshal Install_Mifare1 Params */
    /*---------------------------------*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct Install_Mifare1
    {
        public short sSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.MAX_DATA_INST_CARD)]
        public string acOptionString;
    };
#endif
    /*----------------------------*/
    /* @Marshal ScenarioPolling   */
    /*----------------------------*/
#if WindowsCE
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
#else
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
#endif
    public unsafe struct ScenarioPolling
    {
        public int xCardType;
        public byte ucAntenna;
        public byte ucRepeatNumber;
    };

    /*---------------------------------*/
    /* @Marshal V3 Firmare Information */
    /*---------------------------------*/
#if WindowsCE
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CSC_BOOTIDENT
    {
        //[MarshalAs(UnmanagedType.AnsiBStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public fixed byte ucBootLabel[33];  /* boot label */

       // [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public fixed byte ucPrgLabel[33];   /* Program label */

      //  [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public fixed byte ucFPGA1Label[33]; /* FPGA 1 label */

        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public fixed byte ucFPGA2Label[33]; /* FPGA 2 label */
    };
#else
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct CSC_BOOTIDENT
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public string ucBootLabel;  /* boot label */

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public string ucPrgLabel;   /* Program label */

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public string ucFPGA1Label; /* FPGA 1 label */

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CONSTANT.LABEL_LENGTH + 1)]
        public string ucFPGA2Label; /* FPGA 2 label */
    };
#endif

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CSC_FW_INFO
    {

        public fixed byte blVersionName[48];
        public byte blPushButtonGD;
        public byte appStatus;
        public fixed byte appVersionName[48];
        public byte fwExecutionFlag;
        public fixed byte serialNumber[12];
    };

    public class NXP_SAM_Info
    {
        public byte VendorId;
        public byte MajorNum;
        public byte MinorNum;
        public byte[] SerialNum = new byte[7];
        public byte CryptoSettings;
        public byte Mode;

    };
    //[XmlRoot("Data")]
    //public class CardData
    //{
    //    [XmlAttribute("CSN")]
    //    public UInt64 CardUID;

    //    [XmlAttribute("Engr")]
    //    public UInt32 EngravedNum;

    //    [XmlAttribute("Artwork")]
    //    public UInt32 artwork;

    //    [XmlAttribute("EndOfValidity")]
    //    public string endofValidity;

    //    public void Clear()
    //    {
    //        CardUID = 0;
    //        EngravedNum = 0;
    //        endofValidity = "1980-01-01";
    //        artwork = 0;
    //    }


    //};
    public enum MediaTypeDetected { CARD, TOKEN, UNSUPPORTEDMEDIA, NONE };
}

