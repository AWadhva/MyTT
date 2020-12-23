using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public class cCCHSSAMInfo
    {
        public byte ServiceProvider;
        public CONSTANT.SAMType SAMType;
        public string SAMAppVersion = "";
    }

    public class cCCHSSAMTokenKey
    {
        public byte[] TokenKeyVer = new byte[2];
        public byte[] TokenKey = new byte[8];

    }

    public class cCCHSDSMInfo
    {      
        public  ulong ulDSMid;
        public byte[] ucDeviceID = new byte[10]; //defined by CCHS.
        public byte[] ucIPaddress = new byte[15];
        public byte[] ucUniqueInfo = new byte[20]; //defined by each service provider.
    }

    public class cCCHSSAM
    {
        public int status;
        public long sequenceNo;
        public long isamId;
        public cCCHSSAMTokenKey stTokenKey_Old = new cCCHSSAMTokenKey();
        public cCCHSSAMTokenKey stTokenKey_New = new cCCHSSAMTokenKey();
    }

    public class cSAMConf
    {
        public CONSTANT.SAMType mSAMType = CONSTANT.SAMType.NONE;
        public int SAM_Slot =1 ;

    }

    public class cCCHSSAMSonyKey
    {
        public byte[] bKeyVer = new byte[2];
        public byte bKeyset;
        public byte bKeyNumber;
        public byte[] bGroupKey = new byte[8];
        public byte[] bUserKey = new byte[8];
        public byte bNumberOfAreas;
        public byte[] bAreaCodeList = new byte[16];
        public byte bNumberOfService;
        public byte[] bServiceCodeList = new byte[16];
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
        public byte appId;
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
