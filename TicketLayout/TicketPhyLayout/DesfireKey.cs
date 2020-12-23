using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace IFS2.Equipment.TicketingRules
{
    public class DesfireKey
    {
        public int ApplicationID;
        public int FileID;
        public int KeySet;
        public int KeyCardNo;
        public int KeyNumber;
        public byte[] KeyValue;
    }
    public struct sAccessRight
    {
        public byte kcnRead;      //KeyCardNumber for Read
        public byte kcnWrite;     //KeyCardNumber for Write
        public byte kcnReadWrite; //KeyCardNumber for ReadWrite
        public byte kcnChangeKey; //KeyCardNumber for ChangeKey
    };
    public struct sKeyReferenceBytes
    {
        public byte krbPCD; //Aut, KeySet, RFU, AccRight
        public byte krbAid; //DMAid
        public byte krbRFU; //RFU
    };
  
    public struct sDesfireCardLayout
    {       
        public int appId;
        public byte fileId;
#if _BLUEBIRD_
        public byte fileId_MifareDf;
#endif
        public sAccessRight arSetting;//= new sAccessRight();
        public byte fileType;
        //   byte fileReferenceByte;

#if !_BLUEBIRD_
        public sKeyReferenceBytes keyReferenceBytes;
#endif
        public byte CurrentKeyCardNumber;
        public byte CurrentCommunicationByte;
    };

    public enum EAccessPermission
    {
        E_AccessPermission_Read,
        E_AccessPermission_Write,
        E_AccessPermission_ReadWrite,
        E_AccessPermission_ChangeKey
    };
}
