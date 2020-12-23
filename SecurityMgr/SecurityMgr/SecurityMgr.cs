using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
#if !_BLUEBIRD_
using IFS2.Equipment.CryptoFlex;
#endif
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{
    public enum EncryptionMode : short
    {
        EN0 = 0,
        DE1 = 1
    }

    public sealed class SecurityMgr
    {
        private List<DesfireKeyRef> DesFKeyRefs = new List<DesfireKeyRef>();
        private int DesFKeyRefsNumber = 1;
        public int GetVersionsNumber { get { return DesFKeyRefsNumber; } }
        private List<UltralightKey> UltraKeys = new List<UltralightKey>();

        //Table used for CCHS SAM
        private static sDesfireCardLayout[] DesfireLayout = new sDesfireCardLayout[]      
       {
#if _BLUEBIRD_ 
           new sDesfireCardLayout{appId = 0x01,fileId=0x10,fileId_MifareDf=0x00,  arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01, CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},////0x00, Purse Linkage – (Standard Data File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x21,fileId_MifareDf=0x01,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x02,kcnReadWrite=0x02,kcnChangeKey=0x02},fileType=0x02,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//1, Sequence Number – (Value File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x22,fileId_MifareDf=0x02,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x03,kcnReadWrite=0x03,kcnChangeKey=0x03},fileType=0x02,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//2, Purse – (Value File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x43,fileId_MifareDf=0x03,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x04,kcnReadWrite=0x04,kcnChangeKey=0x04},fileType=0x04,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//3, History – (Cyclic Record File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x15,fileId_MifareDf=0x05,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x05,kcnReadWrite=0x05,kcnChangeKey=0x05},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//5, Validation – (Backup Data File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x16,fileId_MifareDf=0x06,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x06,kcnReadWrite=0x06,kcnChangeKey=0x06},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//6, Sale – (Backup Data File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x08,fileId_MifareDf=0x08,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x08,kcnReadWrite=0x08,kcnChangeKey=0x08},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//08, Personalization – (Standard Data File) -- Optional(Dm1 Agent)
           new sDesfireCardLayout{appId = 0x01,fileId=0x09,fileId_MifareDf=0x09,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x09,kcnReadWrite=0x09,kcnChangeKey=0x09},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//09, Cardholder – (Standard Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x10,fileId_MifareDf=0x00,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//0x00, Pending Fare Deduction Flag File – (Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x11,fileId_MifareDf=0x01,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //1, Sale – (Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x12,fileId_MifareDf=0x02,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //2, Validation – (Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x13,fileId_MifareDf=0x03,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //3, Sale/Add Value(Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x08,fileId_MifareDf=0x08,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},  //08, Personalization –(Standard Data File)
           new sDesfireCardLayout{appId = 0x00,fileId=0x00,fileId_MifareDf=0x00,arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //PICC - APPLICATION 0 - Master Key
           new sDesfireCardLayout{appId = 0x01,fileId=0x00,fileId_MifareDf=0x00,arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //PICC - APPLICATION 1 - Master Key
           new sDesfireCardLayout{appId = 0x02,fileId=0x00,fileId_MifareDf=0x00,arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00} //PICC - APPLICATION 2 - Master Key
#else
           new sDesfireCardLayout{appId = 0x01,fileId=0x10,  arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01, keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},////16, Purse Linkage – (Standard Data File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x21,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x02,kcnReadWrite=0x02,kcnChangeKey=0x02},fileType=0x02,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//33, Sequence Number – (Value File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x22,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x03,kcnReadWrite=0x03,kcnChangeKey=0x03},fileType=0x02,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//34, Purse – (Value File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x43,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x04,kcnReadWrite=0x04,kcnChangeKey=0x04},fileType=0x04,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//67, History – (Cyclic Record File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x15,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x05,kcnReadWrite=0x05,kcnChangeKey=0x05},fileType=0x01,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//21, Validation – (Backup Data File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x16,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x06,kcnReadWrite=0x06,kcnChangeKey=0x06},fileType=0x01,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//22, Sale – (Backup Data File)
           new sDesfireCardLayout{appId = 0x01,fileId=0x08,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x08,kcnReadWrite=0x08,kcnChangeKey=0x08},fileType=0x00,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//08, Personalization – (Standard Data File) -- Optional(Dm1 Agent)
           new sDesfireCardLayout{appId = 0x01,fileId=0x09,arSetting = new sAccessRight{kcnRead = 0x01,kcnWrite=0x09,kcnReadWrite=0x09,kcnChangeKey=0x09},fileType=0x00,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//09, Cardholder – (Standard Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x10,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//16, Pending Fare Deduction Flag File – (Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x11,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //17, Sale – (Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x12,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //18, Validation – (Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x13,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x01,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //19, Sale/Add Value(Backup Data File)
           new sDesfireCardLayout{appId = 0x02,fileId=0x08,arSetting = new sAccessRight{kcnRead = 0x02,kcnWrite=0x01,kcnReadWrite=0x01,kcnChangeKey=0x01},fileType=0x00,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},  //08, Personalization –(Standard Data File)
           new sDesfireCardLayout{appId = 0x00,fileId=0x00,arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //PICC - APPLICATION 0 - Master Key
           new sDesfireCardLayout{appId = 0x01,fileId=0x00,arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}, //PICC - APPLICATION 1 - Master Key
           new sDesfireCardLayout{appId = 0x02,fileId=0x00,arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00,keyReferenceBytes = new sKeyReferenceBytes{krbPCD=0x00,krbAid=0x00,krbRFU=0x00},CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00} //PICC - APPLICATION 2 - Master Key

#endif          
       };



        static readonly SecurityMgr _sMgr = new SecurityMgr();

        private Boolean _bIsTokenKeyLoaded = false;
        public bool IsTokenKeyLoaded { get { return _bIsTokenKeyLoaded; } }

        private int TokenActiveKeyVer;

        public static SecurityMgr Instance
        {
            get
            {
                return _sMgr;
            }
        }
        //SKS added for NFC Interface with default keys... 
        //not to be used in case of real keys

        public byte[] CalculateRndAB(byte[] RndB, byte[] Key)
        {
            // byte[] Key = new byte[16] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            byte[] IV = { 0, 0, 0, 0, 0, 0, 0, 0 };
            TripleDES tripleDESalg = TripleDES.Create();
          
            var tdes = new TripleDESCryptoServiceProvider();
            byte[] b1b2 = new byte[16];
         #if !_BLUEBIRD_
            tdes.Mode = CipherMode.CBC;
            tdes.Padding = PaddingMode.None;
            tdes.BlockSize = 64;
            var decryptor = DESCryptoExtensions.CreateWeakDecryptor(tdes, Key, IV);

            byte[] RndB_dec = decryptor.TransformFinalBlock(RndB, 0, RndB.Length);
           

            // generate randA (integer 0-7 for trying) 
            byte[] randA = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                randA[i] = Byte.Parse(i.ToString());
            }

            // decrypt randA, should XOR with IV, but IV is all 0's, not necessary

            byte[] randA_dec = decryptor.TransformFinalBlock(randA, 0, randA.Length);

            // shift randB one byte left and get randB'
            byte[] r1 = new byte[8];
            for (int i = 0; i < 7; i++)
            {
                r1[i] = RndB_dec[i + 1];
            }
            r1[7] = RndB_dec[0];

            // xor randB' with randA and decrypt
            byte[] b2 = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                b2[i] = (byte)(randA_dec[i] ^ r1[i]);
            }

            b2 = decryptor.TransformFinalBlock(b2, 0, b2.Length);

            // concat (randA + randB')
            //byte[] b1b2 = new byte[16];

            for (int i = 0; i < b1b2.Length; i++)
            {
                if (i <= 7)
                {
                    b1b2[i] = randA_dec[i];
                }
                else
                {
                    b1b2[i] = b2[i - 8];
                }
            }
#endif 
            return b1b2;

        }
        //JL : This function is never called ??? Until we have TestKey.xml is it OK but if not nothing will work.
        public void LoadKeyRefFromContext(string xmlStr)
        {
            try
            {
                XDocument xd = XDocument.Parse(xmlStr);

                foreach (XElement reference in xd.Root.Elements("Reference"))
                {
                    DesfireKeyRef desKeyR = new DesfireKeyRef();

                    desKeyR.ApplicationID = Convert.ToByte(reference.Element("ApplicationID").Value);
                    desKeyR.FileID = Convert.ToByte(reference.Element("FileID").Value);
                    desKeyR.KeyNumber = Convert.ToByte(reference.Element("KeyNumber").Value);
                    desKeyR.KeyReference = Convert.ToByte(reference.Element("KeyReference").Value);
                    desKeyR.KeyCardNo = Convert.ToByte(reference.Element("KeyCardNo").Value);
                    desKeyR.KeyCardNo = Convert.ToByte(reference.Element("KeySet").Value);

                    DesFKeyRefs.Add(desKeyR);
                }
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, "LoadKeyRefFromContext -> Fatal Error in XML Parsing" + Ex.Message);
            }

        }

        /// <summary>
        /// Function called to retrieve all value from XML file.
        /// Key set and key numbers will be changed to have correct values.
        /// The last version shall be set with key set 0
        /// The number of keysets shall be memorised for the retries on the n versions.
        /// </summary>
        /// <param name="pReaderType"></param>
        /// <param name="phRw"></param>
        /// <param name="xmlStr"></param>
#if !_BLUEBIRD_
        public void LoadKeyList(CSC_READER_TYPE pReaderType, int phRw, string xmlStr)
        {
            try
            {
                XDocument xd = XDocument.Parse(xmlStr);

                foreach (XElement media in xd.Root.Elements("Media"))
                {
                    if (media.Element("MediaType").Value == "DesfireEV0")
                    {
                        List<DesfireKey> desFKeys = new List<DesfireKey>();
                        //Recherche des versions. It shall be simply ordered but I don't know how to do and I have no time to search
                        bool ascending = false;
                        int ver = 0;
                        bool first = true;
                        int nbvers = 0;
                        foreach (XElement version in media.Element("Versions").Descendants("Version"))
                        {
                            int vers = Convert.ToInt32(version.Element("Reference").Value);
                            Logging.Log(LogLevel.Verbose, "Read Key version " + vers.ToString());
                            if (first)
                            {
                                ver = vers;
                                first = false;
                            }
                            else
                            {
                                if (vers > ver)
                                {
                                    ascending = true;
                                    ver = vers;
                                }
                            }
                            nbvers++;
                        }
                        int keyset;
                        if (ascending) keyset = nbvers - 1;
                        else keyset = 0;
                        foreach (XElement version in media.Element("Versions").Descendants("Version"))
                        {
                            int keynb = 0;
                            foreach (XElement key in version.Element("Keys").Descendants("Key"))
                            {

                                DesfireKey DKey = new DesfireKey();
                                //DKey.KeySet = Convert.ToInt32(version.Element("Reference").Value);
                                DKey.ApplicationID = Convert.ToInt32(key.Element("ApplicationID").Value);
                                if (DKey.ApplicationID < 3)
                                {
                                    DKey.FileID = Convert.ToInt32(key.Element("FileID").Value);
                                    DKey.KeyCardNo = Convert.ToInt32(key.Element("KeyCardNo").Value);
                                    //DKey.KeyNumber = Convert.ToInt32(key.Element("KeyNumber").Value);
                                    DKey.KeyNumber = keynb;
                                    DKey.KeyValue = CFunctions.StringToByteArray(key.Element("KeyValue").Value);
                                    //DKey.KeySet = Convert.ToInt32(key.Element("KeySet").Value);
                                    DKey.KeySet = keyset;
                                    desFKeys.Add(DKey);
                                    keynb++;
                                }

                            }
                            if (ascending) keyset--;
                            else keyset++;
                        }
                        CryptoFlexFunctions cFlex = new CryptoFlexFunctions((CSC_READER_TYPE)pReaderType, phRw);
                        //Call function to push the KeyList to Reader
                        LoadDesfireKey(pReaderType, phRw, cFlex, desFKeys);
                    }
                    else if (media.Element("MediaType").Value == "TokenUltralight")
                    {
                        foreach (XElement version in media.Element("Versions").Descendants("Version"))
                        {
                            UltralightKey UKey = new UltralightKey();

                            UKey.KeyVersion = Convert.ToByte(version.Element("Reference").Value);

                            foreach (XElement key in version.Element("Keys").Descendants("Key"))
                            {
                                byte[] tmpa = CFunctions.StringToByteArray(key.Element("KeyValue").Value);

                                UKey.KeyValue = new byte[tmpa.Length];

                                Array.Copy(tmpa, 0, UKey.KeyValue, 0, tmpa.Length);

                                TokenActiveKeyVer = UKey.KeyVersion;

                                UltraKeys.Add(UKey);
                            }

                            _bIsTokenKeyLoaded = true;
                        }
                    }
                    else
                    {
                        //TODO
                    }
                }
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, "LoadKeyList -> Fatal Error in XML Parsing" + Ex.Message);
                throw (new Exception("***"));
            }
        }
#endif
        /// <summary>
        /// Function to Generate Mac for token Security Control
        /// The input buffer which needs to be mac controlled,
        /// should be a consolidate of different buffers
        /// Ex : Block_0 + Block_1(8 bits) Buffer -> pDataIn
        /// </summary>
        /// <param name="pDataIn"></param>
        /// <returns></returns>
        public byte[] GenerateMAC(byte[] pDataIn)
        {
            byte[] mac = new byte[8];
            ushort crc;

            try
            {
                byte[] inblock = new byte[8];

                crc = MacAlgoAdaptor.ComputeCRC(pDataIn, pDataIn.Length);

                byte[] crcbuf = BitConverter.GetBytes(crc);

                //Expand the 2 bytes Crc to 8 bytes inblock by repeatetion
                for (int i = 0; i < 8; i += 2)
                {
                    Array.Copy(crcbuf, 0, inblock, i, 2);
                }

                byte[] keyValue;
                if (TokenKey != null)
                    keyValue = TokenKey;
                else
                {
                    keyValue = GetTokenKey((byte)TokenActiveKeyVer);
                    if (keyValue == null)
                        return null;
                }

                MacAlgoAdaptor.SetDeskey(keyValue, (short)EncryptionMode.EN0);

                MacAlgoAdaptor.CalcDes(inblock, mac);

                return mac;

            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, "GenerateMAC -> Error in Input data" + Ex.Message);
                return mac;
            }
        }
#if !_BLUEBIRD_
        internal bool LoadDesfireKey(CSC_READER_TYPE pReaderType, int phRw, CryptoFlexFunctions cFlex, List<DesfireKey> pKeys)
        {
            List<DesfireKey> pDesKeys = new List<DesfireKey>();

            byte[] pDecKeyData = new byte[CONSTANT.MAX_DESF_KEYS_DATA];

            //Extra length for KeySet + KeyNumber for VSAM Reference
            byte[] pKeyData = new byte[CONSTANT.MAX_DESF_KEYS_DATA + 2];

            InitVirtualSam(pReaderType, phRw);

            try
            {
                if (CheckKeys(pKeys))
                {
                    for (int i = 0; i < pKeys.Count; i++)
                    {
                        DesfireKey deskey = new DesfireKey();

                        deskey.ApplicationID = pKeys[i].ApplicationID;
                        deskey.FileID = pKeys[i].FileID;
                        deskey.KeySet = pKeys[i].KeySet;
                        deskey.KeyNumber = pKeys[i].KeyNumber;
                        deskey.KeyCardNo = pKeys[i].KeyCardNo;
                        pKeyData[0] = (byte)pKeys[i].KeySet;
                        pKeyData[1] = (byte)pKeys[i].KeyNumber;

                        pDesKeys.Add(deskey);

                        cFlex.DesDecrypt(DEST_TYPE.DEST_SAM1, pKeys[i].KeyValue, out pDecKeyData);

                        Array.Copy(pDecKeyData, 0, pKeyData, 2, pDecKeyData.Length);

                        bool ret = LoadKey(pReaderType, phRw, pKeyData);

                        Logging.Log(LogLevel.Information, "SecurityManager -> LoadDesfireKey: " + i + Convert.ToString(ret) + " AppID:" + deskey.ApplicationID.ToString() + " FileID:" + deskey.FileID.ToString("X2") + " KeySet:" + deskey.KeySet.ToString() + " KeyNb:" + deskey.KeyNumber.ToString() + " KeyCardNb:" + deskey.KeyCardNo.ToString("X2"));
                        //Add test if ret <0 the reader will not work.
                    }

                    //Store the Key references for the loaded Keys
                    LoadDesfireKeyRef(pDesKeys);

                    return true;
                }

                Logging.Log(LogLevel.Error, "SecurityManager -> LoadDesfireKey: Invalid Key Format");
                return false;
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, "SecurityManager -> LoadDesfireKey: Fatal Error" + Ex.Message);
                return false;
            }
        }
#endif
        public bool GetPCDKeyRef(byte pAid, byte pFileId, byte keyset, out byte pKeyReference, out byte pKeyNumber)
        {
            pKeyReference = 0x00;
            pKeyNumber = 0x00;

            for (int i = 0; i < DesFKeyRefs.Count(); i++)
            {
                if (pAid == DesFKeyRefs[i].ApplicationID && pFileId == DesFKeyRefs[i].FileID && DesFKeyRefs[i].KeySet == keyset)
                {
                    pKeyReference = DesFKeyRefs[i].KeyReference;
                    //pKeyNumber = DesFKeyRefs[i].KeyNumber;
                    pKeyNumber = DesFKeyRefs[i].KeyCardNo;
                    Logging.Log(LogLevel.Verbose, "Search Key Reference AppID:" + pAid.ToString() + " FileID:" + pFileId.ToString("X2") + " Key Nb:" + pKeyNumber.ToString() + " Key Ref:" + pKeyReference.ToString("X2"));

                    return true;
                }
            }

            return false;
        }

        public int GetTokenActiveKeyVer()
        {
            return TokenActiveKeyVer;
        }

        private void LoadDesfireKeyRef(List<DesfireKey> pKeys)
        {
            int maxkeyset = 0;
            for (int i = 0; i < pKeys.Count; i++)
            {
                DesfireKeyRef desKeyref = new DesfireKeyRef();

                desKeyref.ApplicationID = (byte)pKeys[i].ApplicationID;
                desKeyref.FileID = (byte)pKeys[i].FileID;
                desKeyref.KeyNumber = (byte)pKeys[i].KeyNumber;
                desKeyref.KeyCardNo = (byte)pKeys[i].KeyCardNo;
                desKeyref.KeySet = (byte)pKeys[i].KeySet;
                desKeyref.KeyReference = CFunctions.GetKeyReference(1, pKeys[i].KeySet, 0, pKeys[i].KeyNumber);
                if (pKeys[i].KeySet > maxkeyset) maxkeyset = pKeys[i].KeySet;
                Logging.Log(LogLevel.Verbose, "Get Key Reference AppID:" + desKeyref.ApplicationID.ToString() + " FileID:" + ((byte)pKeys[i].FileID).ToString("X2") + " Key Nb:" + desKeyref.KeyNumber.ToString() + " Key Ref:" + desKeyref.KeyReference.ToString("X2"));

                DesFKeyRefs.Add(desKeyref);
                DesFKeyRefsNumber = maxkeyset + 1;
            }
        }

        internal bool CheckTokenKeys(List<UltralightKey> pKeys)
        {
            bool IsKeyOk = true;

            int i = 0;
            while (i < pKeys.Count() && IsKeyOk)
            {
                IsKeyOk = (pKeys[i].KeyVersion <= CONSTANT.MAX_ULTRA_KEY_VERSION);
            }

            return IsKeyOk;
        }

        internal bool CheckKeys(List<DesfireKey> pKeys)
        {
            bool IsKeyOk = false;

            if (pKeys.Count() <= CONSTANT.MAX_DESF_KEYS_LOAD)
            {
                IsKeyOk = true;

                int i = 0;
                while (i < pKeys.Count() && IsKeyOk)
                {
                    IsKeyOk = (pKeys[i].KeySet <= CONSTANT.MAX_DESF_KEYS_SETS
                               && pKeys[i].KeyNumber <= CONSTANT.MAX_DESF_KEYS_NBRS
                                && pKeys[i].KeyValue.Count() == CONSTANT.MAX_DESF_KEYS_DATA);
                    i++;
                };

                return IsKeyOk;
            }

            return IsKeyOk;
        }

        internal bool LoadKey(CSC_READER_TYPE pReaderType, int phRw, byte[] pKeyData)
        {
#if _BLUEBIRD_
#else
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(pReaderType,
                                    phRw,
                                    DEST_TYPE.DEST_SAM_DESFIRE,
                                    CFunctions.getApdu(CONSTANT.LOGICAL_SAM_CLA, CONSTANT.LOGICAL_LKEY_INS, CONSTANT.NULL, CONSTANT.NULL, pKeyData),
                                    out pSw1,
                                    out pSw2,
                                    out pResData);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                return true;
            }
#endif
            return false;
        }

        internal bool InitVirtualSam(CSC_READER_TYPE pReaderType, int phRw)
        {
            CSC_API_ERROR Err;

            InstallCard pCscCardParams = new InstallCard();

            pCscCardParams.xCardType = (int)(CSC_TYPE.CARD_MIFARE1);
            // pCscCardParams.iCardParam.xMifParam.sSize = 0;// SKS: Date 4-08-14
            // because now in new firmware version (1.20) of CSC V4 it is medetory to specify the SAM slot number fields of xMifParam  
            string slot = "DMSAM=1";
            pCscCardParams.iCardParam.xMifParam.acOptionString = slot;
            pCscCardParams.iCardParam.xMifParam.sSize = (short)slot.Length;

            Err = Reader.InstallCard(pReaderType,
                                    phRw,
                                    DEST_TYPE.DEST_SAM_DESFIRE,
                                    pCscCardParams);

            return (Err == CSC_API_ERROR.ERR_NONE);
        }

        internal byte[] GetTokenKey(byte pKeyVer)
        {
            //byte[] NoKey = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            if (_bIsTokenKeyLoaded)
            {
                for (int i = 0; i < UltraKeys.Count; i++)
                {
                    if (pKeyVer == UltraKeys[i].KeyVersion)
                    {
                        TokenActiveKeyVer = UltraKeys[i].KeyVersion;
                        return UltraKeys[i].KeyValue;
                    }
                }

                return null;
            }

            return null;
        }

        #region CCHSSAM
        internal byte getFileReferenceByte(byte FileId, byte FileType)
        {
            return (byte)(((0x03 & FileId) << 4) | FileId);
        }

        internal void fillKeyReferenceByte(ref sKeyReferenceBytes keyrefbytes, byte appID, EAccessPermission l_permission, bool bUseNewKeySet)
        {
            byte l_accessRight = 0x03; //Default read
            switch ((int)l_permission)
            {
                case (int)EAccessPermission.E_AccessPermission_Read: { l_accessRight = 0x03; } break;
                case (int)EAccessPermission.E_AccessPermission_Write: { l_accessRight = 0x02; } break;
                case (int)EAccessPermission.E_AccessPermission_ReadWrite: { l_accessRight = 0x01; } break;
                case (int)EAccessPermission.E_AccessPermission_ChangeKey: { l_accessRight = 0x00; } break;
                default: { l_accessRight = 0x03; } break;
            }
            keyrefbytes.krbPCD = (byte)(0x80 | ((0x03 & (bUseNewKeySet ? 0x00 : 0x01)) << 5) | l_accessRight);
            keyrefbytes.krbAid = appID;
            keyrefbytes.krbRFU = 0x00;

        }
        internal void fillRefBytes(ref sDesfireCardLayout l_layout, EAccessPermission l_permission, bool bUseNewKeySet)
        {
#if !_BLUEBIRD_
            fillKeyReferenceByte(ref l_layout.keyReferenceBytes, l_layout.appId, l_permission, bUseNewKeySet);
#endif
            switch ((int)l_permission)
            {
                case (int)EAccessPermission.E_AccessPermission_Read: { l_layout.CurrentKeyCardNumber = l_layout.arSetting.kcnRead; } break;
                case (int)EAccessPermission.E_AccessPermission_Write: { l_layout.CurrentKeyCardNumber = l_layout.arSetting.kcnWrite; } break;
                case (int)EAccessPermission.E_AccessPermission_ReadWrite: { l_layout.CurrentKeyCardNumber = l_layout.arSetting.kcnReadWrite; } break;
                case (int)EAccessPermission.E_AccessPermission_ChangeKey: { l_layout.CurrentKeyCardNumber = l_layout.arSetting.kcnChangeKey; } break;
                default: { l_layout.CurrentKeyCardNumber = l_layout.arSetting.kcnRead; } break;
            }
            l_layout.CurrentCommunicationByte = 0x00;

        }
        public bool getLayoutDefinition(out sDesfireCardLayout mDesfirelayout, byte aid, byte fileid, EAccessPermission l_permission, bool bUseNewKeySet)
        {
            bool l_result = false;
            mDesfirelayout = new sDesfireCardLayout();
            mDesfirelayout.appId = aid;
            mDesfirelayout.fileId = fileid;
            for (int i = 0; i < CONSTANT.CardLayoutMax; i++)
            {
                if (DesfireLayout[i].appId == aid && DesfireLayout[i].fileId == fileid)
                {
                    mDesfirelayout.arSetting = DesfireLayout[i].arSetting;
#if _BLUEBIRD_
                   mDesfirelayout.fileId_MifareDf = DesfireLayout[i].fileId_MifareDf;
#else
                    mDesfirelayout.keyReferenceBytes = DesfireLayout[i].keyReferenceBytes;
#endif
                    l_result = true;
                    break;
                }
            }
            if (l_result)
            {
                fillRefBytes(ref mDesfirelayout, l_permission, bUseNewKeySet);
            }
            return l_result;
        }

        #endregion

        public void SetTokenKey(int version, byte[] key)
        {
            TokenActiveKeyVer = version;
            TokenKey = key;

            _bIsTokenKeyLoaded = true;
        }

        byte[] TokenKey = null;
    }
}

