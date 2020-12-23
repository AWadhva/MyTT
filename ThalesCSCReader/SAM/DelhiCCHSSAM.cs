using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class DelhiCCHSSAM
    {
        private CSC_READER_TYPE ReaderType;
        private int hRw;
        private CONSTANT.SAMType mSAMType = CONSTANT.SAMType.ISAM;
        public CONSTANT.SAMErrors mCCHSSAM_Status = CONSTANT.SAMErrors.SM_MAX_ERRORS;
        public int TxnSeqenceNo = 0;
        public uint DSMId = 0;
        public cCCHSSAMInfo mCCHSStatusInfo;
        private bool _ProductionSAM = false;
        private byte[] SAM_AUTH_KEY = null;
        public cCCHSDSMInfo mCCHSDSMInfo;

        public DelhiCCHSSAM(CSC_READER_TYPE ReaderType, int hRw, bool production, string samPinCode)
        {
            this.ReaderType = ReaderType;
            this.hRw = hRw;
            _ProductionSAM = production;
            SAM_AUTH_KEY = new byte[samPinCode.Length];
            for (int i = 0; i < samPinCode.Length; i++) SAM_AUTH_KEY[i] = (byte)samPinCode[i];
        }
        public DelhiCCHSSAM(CSC_READER_TYPE ReaderType, int hRw)
        {
        }
        public CSC_API_ERROR SAMSelectApplication(DEST_TYPE pSam)
        {
            // int ret = -3;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_PARAM ;
           // byte pSw1 = 0xFF;
           // byte pSw2 = 0xFF;

            return Err;
        }
        public CSC_API_ERROR SAMActivation(DEST_TYPE pSam)
        {
            // int ret = -3;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_PARAM; 
            //byte pSw1 = 0xFF;
           // byte pSw2 = 0xFF;

            return Err;
        }
        public byte[] SAMActivationAPDU(DEST_TYPE pSam, string samPinCode)
        {
            // int ret = -3;
            int index = 0;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_PARAM;
            //byte pSw1 = 0xFF;
            // byte pSw2 = 0xFF;
            SAM_AUTH_KEY = new byte[samPinCode.Length];
             byte[] SAM_Activation = new byte[SAM_AUTH_KEY.Length + 2];
             SAM_Activation[index++] = (byte)this.mSAMType; // SAM Type
            for (int i = 0; i < samPinCode.Length; i++) SAM_AUTH_KEY[i] = (byte)samPinCode[i];
            Array.Copy(SAM_AUTH_KEY,0,SAM_Activation,index,SAM_AUTH_KEY.Length);

            return CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_ACTIVATION, 0x00, 0x00, SAM_Activation);

            
        }
        public CSC_API_ERROR GetSAMSequence(DEST_TYPE pSam, out int sequenceNo)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_PARAM; 
           // byte pSw1 = 0xFF;
           // byte pSw2 = 0xFF;
            sequenceNo = 0;

            return Err;
        }
        public CSC_API_ERROR GetDSMID(DEST_TYPE pSam, out uint mDSMID)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_PARAM; 
           
            mDSMID = 0;

            return Err;
        }
        public CSC_API_ERROR GetSAMStatus(DEST_TYPE pSam, out cCCHSSAMInfo mSAMStatusInfo)
        {
            //int ret = -3;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_PARAM;
           // byte pSw1 = 0xFF;
           // byte pSw2 = 0xFF;
           // byte[] isoResponseBuffer;
            //iso_cmd_Get_SAM_Status[]={0xD0, 0x05, 0x00, 0x00, 0x06};
            mSAMStatusInfo = new cCCHSSAMInfo();
            mSAMStatusInfo.SAMAppVersion = "XXX";
            mSAMStatusInfo.ServiceProvider = 0x02;
            mSAMStatusInfo.SAMType = CONSTANT.SAMType.NONE;

            return Err;
        }
        public int SAMInstallCard(DEST_TYPE pSam)
        {
            int ret = -3;
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_INTERNAL;

            //Install SAM1 Card 
            InstallCard pSamCardParams = new InstallCard();

            pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)pSam;
            pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T1;
            pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);//CHECK +1 REMOVED IN AVM-TT            

            Logging.Log(LogLevel.Verbose, "Before CCHS SAMInstallCard");
/*
            Err = InstallCard(this.ReaderType,
                              this.hRw,
                              pSam,
                              pSamCardParams);
            Logging.Log(LogLevel.Verbose, "after CCHSSAMManger SAMInstallCard return code = " + Err.ToString());
*/
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
            }
            else if (Err == CSC_API_ERROR.ERR_TIMEOUT)
                mCCHSSAM_Status = CONSTANT.SAMErrors.SM_LINK_FAILURE;
            else
            {
                Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAM Installcard Bad data returned");
                mCCHSSAM_Status = CONSTANT.SAMErrors.SM_CONFIG_ERROR;
            }

            ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAM Installcard Response code: " + ret.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return ret;
        }
    }
}
