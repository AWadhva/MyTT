using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules.SmartMedia
{
    #region "Mifare DesFire EV 0/1  Card ISO commands"
    public class DesfireEVISO : MediaInterface
    {
        #region " Private members"
       
        #endregion
        public DesfireEVISO()
        {
        }
        public override void _Reset()
        {
           
        }
         private byte[] DesfireNativeWrapped_APDU(byte CLS, byte INS, byte P1, byte P2)
        {
            byte[] apdu = new byte[5];//new byte[6 + aid.Length];
            //System.Array.Copy(CLA_INS_P1_P2, 0, result, 0, CLA_INS_P1_P2.Length);
            apdu[0] = CLS;
            apdu[1] = INS;
            apdu[2] = P1;
            apdu[3] = P2;
            apdu[4] = 0x00;
           // apdu[5] = 0x00;
            string hex = BitConverter.ToString(apdu).Replace("-", string.Empty);
            //log("APDU Command : " + hex);
            return apdu;
        }         
        private byte[] DesfireNativeWrapped_APDU(byte CLS, byte INS, byte P1, byte P2, byte[] data, byte LE)
        {
            byte[] apdu = new byte[6 + data.Length];//new byte[6 + aid.Length];
            //System.Array.Copy(CLA_INS_P1_P2, 0, result, 0, CLA_INS_P1_P2.Length);
            apdu[0] = CLS;
            apdu[1] = INS;
            apdu[2] = P1;
            apdu[3] = P2;
            apdu[4] = (byte)data.Length;
            if(data.Length>0)
                System.Array.Copy(data, 0, apdu, 5, data.Length);
            apdu[apdu.Length - 1] = LE;
            string hex = BitConverter.ToString(apdu).Replace("-", string.Empty);
            //log("APDU Command : " + hex);
            return apdu;
        }
        private void logBuffer(string name, byte[] tab)
        {
            string s = "";
            if (tab!=null)
            {
                for (int i = 0; i < tab.Length; i++) s += tab[i].ToString("X2");
                Logging.Log(LogLevel.Verbose, "ReadCardData : " + name + " :" + s);
            }
        }
        /// <summary>
        ///  returns apdu for key to be changed for a selected application
        /// </summary>
        /// <param name="newkeyNo"></param>
        /// <param name="chiphered_newkey"></param>
        /// <returns></returns>
        public byte[] changeKeys(byte keyNo, byte[] chiphered_newkey)
        {
           byte[] data = new byte[1+chiphered_newkey.Length];
           data[0] = keyNo;
           Array.Copy(chiphered_newkey, 0, data, 1, chiphered_newkey.Length);

           return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_CHGKEY_INS, CONSTANT.NULL, CONSTANT.NULL, data, 0x00);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="nKeysettings"></param>
        /// <param name="nbKeys"></param>
        /// <returns></returns>
        public override byte[] _CreateApplicationAPDU(int appId, byte nKeysettings, byte nbKeys)
        {
            byte[] appidBytes = BitConverter.GetBytes(appId);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(appidBytes);

            byte[] data = { appidBytes[0], appidBytes[1], appidBytes[2], nKeysettings, nbKeys };           

            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_CREATAPP_INS, CONSTANT.NULL, CONSTANT.NULL, data, 0x00);
        }

        public override byte[] _deleteAppAPDU(int appid)
        {
            byte[] appidBytes = BitConverter.GetBytes(appid);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(appidBytes);

            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_DELAPP_INS, CONSTANT.NULL, CONSTANT.NULL, appidBytes, 0x00); ;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="filetype"></param>
        /// <param name="accessRightLSB"></param>
        /// <param name="accessRightMSB"></param>
        /// <param name="nbRecords"></param>
        /// <returns></returns>
        public override byte[] _CreateFileAPDU(byte fileId, byte fileType, byte bcommsettings, byte accessRightLSB, byte accessRightMSB, int nbRecords)
        {
            byte[] databuff = { 0 };
            byte bcmd_INS = 0xFF;
            byte[] nbRecordsBytes = BitConverter.GetBytes(nbRecords);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(nbRecordsBytes);
            switch ((DF_FILE_TYPE)fileType)
            {
                case DF_FILE_TYPE.STANDARD_DATA_FILE:
                case DF_FILE_TYPE.BACKUP_DATA_FILE:
                    if ((DF_FILE_TYPE)fileType == DF_FILE_TYPE.STANDARD_DATA_FILE)
                        bcmd_INS = 0xCD;
                    else bcmd_INS = 0xCB;
                    databuff = new byte[7];
                    databuff[0] = (byte)fileId;
                    databuff[1] = bcommsettings;//commsettings
                    databuff[2] = accessRightLSB;
                    databuff[3] = accessRightMSB;
                    databuff[4] = nbRecordsBytes[0];
                    databuff[5] = nbRecordsBytes[1];
                    databuff[6] = nbRecordsBytes[2];
                    break;
                case DF_FILE_TYPE.VALUE_FILE:
                    bcmd_INS = 0xCC;
                    databuff = new byte[17];
                    databuff[0] = (byte)fileId;
                    databuff[1] = bcommsettings;//commsettings
                    databuff[2] = accessRightLSB;
                    databuff[3] = accessRightMSB;
                    databuff[4] = 0x00;// lower limit LSB0
                    databuff[5] = 0x00;//// lower limit LSB1
                    databuff[6] = 0x00;// rlower limit LSB2
                    databuff[7] = 0x00;// lower limit MSB
                    databuff[8] = 0xFF;////upper limit LSB0
                    databuff[9] = 0xFF;// upper limit LSB1
                    databuff[10] = 0x0F;// upper limit LSB2
                    databuff[11] = 0x00;// upper limit MSB
                    databuff[12] = 0x00;//value lsb0
                    databuff[13] = 0x80;//value lsb1
                    databuff[14] = 0x00;//value lsb2
                    databuff[15] = 0x00;////value msb
                    databuff[16] = 0x01;//limited credit
                    break;
                case DF_FILE_TYPE.RECORD_FILE: // record files
                    bcmd_INS = 0xC0;
                    databuff = new byte[10];
                    databuff[0] = (byte)fileId;
                    databuff[1] = bcommsettings;//commsettings
                    databuff[2] = accessRightLSB;
                    databuff[3] = accessRightMSB;
                    databuff[4] = 0x20;// record size LSB0
                    databuff[5] = 0x00;//// record size LSB1
                    databuff[6] = 0x00;// record size MSB
                    databuff[7] = nbRecordsBytes[0];// number of records LSB0
                    databuff[8] = nbRecordsBytes[1];////number of records LSB1
                    databuff[9] = nbRecordsBytes[2];// number of records MSB
                    break;
                default:
                    break;
            }// switch (fileType)
            if (bcmd_INS != 0xFF) return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, bcmd_INS, CONSTANT.NULL, CONSTANT.NULL, databuff, 0x00);
            else
            return null;
        }

        public override byte[] _CommitAPDU()
        {
            byte[] apdu = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_COMMIT_TXN_INS, CONSTANT.NULL, CONSTANT.NULL, CONSTANT.NULL };//DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_COMMIT_TXN_INS, CONSTANT.NULL, CONSTANT.NULL);
            return apdu;
        }
        public byte[] ReadRecordsIntermediate()
        {
            byte[] apdu = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_MOREDATA_INS, 0x00, 0x00, 0x00 };
            return apdu;
        }

        public override byte[] _AuthenticateAPDU(byte keyNo)
        {
            byte[] bKeyIndex = { keyNo };
           // byte[] appidBytes = BitConverter.GetBytes(appid);
           // if (!BitConverter.IsLittleEndian) Array.Reverse(appidBytes);

            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_AUTH_INS, CONSTANT.NULL, CONSTANT.NULL, bKeyIndex, 0x00); 
        }
        public override byte[] _AuthenticateAPDU_Step2(byte[] mRndAB)
        {
            //byte[] bKeyIndex = { keyNo };
            // byte[] appidBytes = BitConverter.GetBytes(appid);
            // if (!BitConverter.IsLittleEndian) Array.Reverse(appidBytes);

            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_MOREDATA_INS, CONSTANT.NULL, CONSTANT.NULL, mRndAB, 0x00);
        }
        public override byte[] _ReadDataAPDU(byte fileid, byte fileType, int offset, int length)
        {
            byte[] offsetBytes = { 0 };
            byte[] lengthBytes = { 0 };
            byte[] databuff = { 0 };
            byte bcmd_INS = 0xFF;
            if (offset > 0)
            {
                offsetBytes = BitConverter.GetBytes(offset);
                if (!BitConverter.IsLittleEndian) Array.Reverse(offsetBytes);
            }
            if (length > 0)
            {
                lengthBytes = BitConverter.GetBytes(length);
                if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            }
            switch ((DF_FILE_TYPE)fileType)
            {
                case DF_FILE_TYPE.STANDARD_DATA_FILE:
                case DF_FILE_TYPE.BACKUP_DATA_FILE:
                    bcmd_INS = ISOCONSTANTS.DESFIRE_READ_DATAFILE_INS;                   
                    databuff = new byte[7];
                    databuff[0] = fileid;
                    //offset
                    Array.Copy(offsetBytes, 0, databuff, 1, 3);
                    //lenth
                    Array.Copy(lengthBytes, 0, databuff, 5, 3);
                    break;
                case DF_FILE_TYPE.VALUE_FILE:
                    bcmd_INS = ISOCONSTANTS.DESFIRE_GETVAL_INS;
                    break;
                default: break;
            }//switch
            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, bcmd_INS, CONSTANT.NULL, CONSTANT.NULL, databuff, 0x00);
        }
        public override byte[] _SelectAppAPDU(int appId)
        {
            byte[] appidBytes = BitConverter.GetBytes(appId);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(appidBytes);
            byte[] bappid = new byte[3];
            Array.Copy(appidBytes, 0, bappid, 0, 3);

            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_SELA_INS, CONSTANT.NULL, CONSTANT.NULL, bappid, 0x00);

        }//_SelectAppAPDU

        public override byte[] _WriteDataAPUD(byte fileid, byte fileType, int offset, byte[] bdata, int length)
        {
            byte[] offsetBytes = { 0 };
            byte[] lengthBytes = { 0 };
            byte[] databuff = { 0 };
            byte bcmd_INS = ISOCONSTANTS.DESFIRE_WRITE_DATAFILE_INS;
           // int length = length;
            if (offset > 0)
            {
                offsetBytes = BitConverter.GetBytes(offset);
                if (!BitConverter.IsLittleEndian) Array.Reverse(offsetBytes);
            }
            if (length > 0)
            {
                lengthBytes = BitConverter.GetBytes(length);
                if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            }
            switch ((DF_FILE_TYPE)fileType)
            {
                case DF_FILE_TYPE.STANDARD_DATA_FILE:
                case DF_FILE_TYPE.BACKUP_DATA_FILE:
                    {
                        int maxlen = length;
                        if (length > 52) maxlen = 52;
                        databuff = new byte[7 + maxlen];
                        databuff[0] = fileid;
                        //offset
                        if(offset>0)
                        Array.Copy(offsetBytes, 0, databuff, 1, 3);
                        //length
                        Array.Copy(lengthBytes, 0, databuff, 4, 3);

                        Array.Copy(bdata, 0, databuff, 7, maxlen);

                    }
                    break;
            }//switch ((DF_FILE_TYPE)fileType)

            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, bcmd_INS, CONSTANT.NULL, CONSTANT.NULL, databuff, 0x00);
        }
        public byte[] _WriteDataIntermediateAPDU( byte[] bdata)
        {
            
           // byte[] apdu = { ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_MOREDATA_INS, 0x00, 0x00, 0x00 };
            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_MOREDATA_INS, CONSTANT.NULL, CONSTANT.NULL, bdata, 0x00); ;
        }

        public override byte[] _ChangeKeySettingsAPDU(byte[] crypted_settings)
        {
            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_CHGKEYSET_INS, CONSTANT.NULL, CONSTANT.NULL, crypted_settings, 0x00); ;
        }
        public byte[] _GetApplicationIds()
        {
            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_GETAPPIDS_INS, CONSTANT.NULL, CONSTANT.NULL) ;
        }
        public byte[] _FormateCard()
        {
            return DesfireNativeWrapped_APDU(ISOCONSTANTS.DESFIRE_CLA, ISOCONSTANTS.DESFIRE_FORMAT_INS, CONSTANT.NULL, CONSTANT.NULL);
        }

    }//public class DesfireEV
    #endregion
}
