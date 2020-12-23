using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.Common;
using System.Security.Cryptography;

namespace IFS2.Equipment.TicketingRules
{
    public class CCHSSAMManger
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

        public CCHSSAMManger(CSC_READER_TYPE ReaderType, int hRw)
        {
            this.ReaderType = ReaderType;
            this.hRw = hRw;
            _ProductionSAM = (bool)Configuration.ReadParameter("IsProductionSAM", "bool", "true");
            SAM_AUTH_KEY = GetSamPinCode();
        }

        public CCHSSAMManger(CSC_READER_TYPE ReaderType, int hRw, bool production)
        {
            this.ReaderType = ReaderType;
            this.hRw = hRw;
            _ProductionSAM = production;
            SAM_AUTH_KEY = GetSamPinCode();
        }

        public CCHSSAMManger(CSC_READER_TYPE ReaderType, int hRw, CONSTANT.SAMType SAMType)
        {
            this.ReaderType = ReaderType;
            this.hRw = hRw;
            this.mSAMType = SAMType;
            _ProductionSAM = (bool)Configuration.ReadParameter("IsProductionSAM", "bool", "true");
            SAM_AUTH_KEY = GetSamPinCode();
        }

        public CCHSSAMManger(CSC_READER_TYPE ReaderType, int hRw, bool production, string samPinCode)
        {
            this.ReaderType = ReaderType;
            this.hRw = hRw;
            _ProductionSAM = production;
            SAM_AUTH_KEY = new byte[samPinCode.Length];
            for (int i = 0; i < samPinCode.Length; i++) SAM_AUTH_KEY[i] = (byte)samPinCode[i];
        }

        public int SAMInstallCard(DEST_TYPE pSam)
        {
            int ret = -3;
            CSC_API_ERROR Err;

            //Install SAM1 Card 
            InstallCard pSamCardParams = new InstallCard();

            pSamCardParams.xCardType = (int)(CSC_TYPE.CARD_SAM);
            pSamCardParams.iCardParam.xSamParam.ucSamSelected = (byte)pSam;
            pSamCardParams.iCardParam.xSamParam.ucProtocolType = CONSTANT.SAM_PROTOCOL_T1;
            pSamCardParams.iCardParam.xSamParam.ulTimeOut = 60 * 1000; // TODO: check the unit. assuming it in ms for now.
            pSamCardParams.iCardParam.xSamParam.acOptionString = new string('\0', CONSTANT.MAX_SAM_OPTION_STRING_LEN + 1);//CHECK +1 REMOVED IN AVM-TT            

            Logging.Log(LogLevel.Verbose, "Before CCHS SAMInstallCard");
            Err = Reader.InstallCard(this.ReaderType,
                              this.hRw,
                              pSam,
                              pSamCardParams);
            Logging.Log(LogLevel.Verbose, "after CCHSSAMManger SAMInstallCard return code = " + Err.ToString());

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

        public byte[] GetSamPinCode()
        {
            string samPinCode = "";
            if (!_ProductionSAM)
            {
                samPinCode = Configuration.ReadEncryptedParameter("ISAM_PinCode", "A9c5iunUgssqcfVxlfeDJg==");//12345678
            }
            else
            {
                samPinCode = Configuration.ReadEncryptedParameter("ISAM_PinCode", "P3CNMZPe4UeXL5bfr8ndPg==");//FF407E02
            }
            byte[] keyBuffer = new byte[samPinCode.Length];
            for (int i = 0; i < samPinCode.Length; i++) keyBuffer[i] = (byte)samPinCode[i];
            return keyBuffer;
        }


        public CSC_API_ERROR SAMActivation(DEST_TYPE pSam)
        {
            // int ret = -3,
            int index = 0;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            //Default value
            byte[] SAM_Activation = new byte[SAM_AUTH_KEY.Length + 2]; ////TYPE(1byte) + PinCode(8bytes)+OptionalData(1byte)
            byte[] isoResponseBuffer;// = new byte[CONSTANT.ISAM_MAX_BUFFER_RESPONSE_SIZE];

            SAM_Activation[index++] = (byte)this.mSAMType; // SAM Type

            foreach (byte b in SAM_AUTH_KEY) //PinCode(8bytes
            {
                SAM_Activation[index++] = b;
            }
            SAM_Activation[index] = 0x00; //OptionalData(1byte)

            //Activate SAM
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_ACTIVATION, CONSTANT.NULL, CONSTANT.NULL, SAM_Activation),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DEVICE;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAM activation Bad data returned");
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_AUTHENTICATION_FAILURE;

                }
                else
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
            }
            //ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAM activation Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }

        public CSC_API_ERROR SAMSelectApplication(DEST_TYPE pSam)
        {
            // int ret = -3;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;

            byte[] iso_cmd_SAMSelectApp = { 0xD4, 0x10, 0x00, 0x00, 0x00, 0x00, 0x09 };

            //Select SAM Application
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(0x00, CONSTANT.ISAM_INS_SEL_APPL, 0x04, 0x0C, iso_cmd_SAMSelectApp),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAM activation Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2; //CONSTANT.SAMErrors.SM_FAILURE;

                }
                else
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
            }
            //ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAMSelectApplication Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());
            return Err;
        }

        public CSC_API_ERROR GetSAMSequence(DEST_TYPE pSam, out int sequenceNo)
        {
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;
            // DataConverter conv = CopyConv;
            //byte[] iso_cmd_SAMGetSeqNo = { 0xD4, 0x10, 0x00, 0x00, 0x00, 0x00, 0x09 };

            sequenceNo = 0;

            //Select SAM Get Sequence No
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GETSAM_SEQUENCE, CONSTANT.NULL, CONSTANT.NULL, 0x04),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAMGetSAMSequence Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2; //CONSTANT.SAMErrors.SM_FAILURE;

                }
                else
                {
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                    sequenceNo = (int)CFunctions.ConvertLittleEndian(isoResponseBuffer);
                }
            }
            //ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger SAMSelectApplication Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());
            return Err;

        }

        public CSC_API_ERROR GetSAMStatus(DEST_TYPE pSam, out cCCHSSAMInfo mSAMStatusInfo)
        {
            //int ret = -3;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;
            //iso_cmd_Get_SAM_Status[]={0xD0, 0x05, 0x00, 0x00, 0x06};
            mSAMStatusInfo = new cCCHSSAMInfo();
            mSAMStatusInfo.SAMAppVersion = "XXX";
            mSAMStatusInfo.ServiceProvider = 0x02;
            mSAMStatusInfo.SAMType = CONSTANT.SAMType.NONE;

            // Get SAM Status Info
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GETSAM_STATUS, CONSTANT.NULL, CONSTANT.NULL, 0x06),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);



            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DEVICE;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetSAMStatus Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2;//.SM_FAILURE;

                }
                else
                {
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                    //sequenceNo = (int)CFunctions.ConvertLittleEndian(isoResponseBuffer);
                    mSAMStatusInfo.ServiceProvider = isoResponseBuffer[0];
                    mSAMStatusInfo.SAMType = (CONSTANT.SAMType)isoResponseBuffer[1];
                    mSAMStatusInfo.SAMAppVersion = Encoding.ASCII.GetString(isoResponseBuffer, 2, 4);
                }
            }
            // ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetSAMStatus Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());
            return Err;
        }

        public CSC_API_ERROR WriteSAMSequence(DEST_TYPE pSam, int SequenceNo)
        {
            //int ret = -3;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;

            byte[] SequenceNobytes = CFunctions.ConvertToBytesLE(SequenceNo);// TODO: As we are reading SAM Seq in Little indian

            //Write SAM Seq. no.
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_WRITESAM_SEQUENCE, CONSTANT.NULL, CONSTANT.NULL, SequenceNobytes),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger WriteSAMSequence Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2;// CONSTANT.SAMErrors.SM_FAILURE;

                }
                else
                {
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                }
            }
            //ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger WriteSAMSequence Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }

        public CSC_API_ERROR GenerateTAC(DEST_TYPE pSam, byte[] iData, int length, out byte[] CalculatedTAC)
        {
            // int ret=-3;
            byte[] HashData;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;
            CalculatedTAC = new byte[4];
            string Hashstr = "";
            if (length > CONSTANT.ISAM_MAX_SIZE_FOR_TAC)
            {
                //TODO: Calculate SHA1 HASH bytes 
                HashData = CalculateSHA1Hash(iData, length);

                for (int a = 0; a < HashData.Length; a++)
                    Hashstr += HashData[a].ToString("X2") + " ";
                Logging.Log(LogLevel.Verbose, "Hash Code: " + Hashstr);
            }
            else
            {
                HashData = new byte[length];
                //iData.CopyTo(HashData, 0);
                Array.Copy(iData, HashData, length);
            }

            //Generate TAC
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GENERATE_TAC, CONSTANT.NULL, CONSTANT.NULL, HashData),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger GenerateTAC Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2;//.SM_FAILURE;
                }
                else
                {
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;

                    for (int i = 0; i < 4; i++) CalculatedTAC[i] = isoResponseBuffer[i];
                }
            }
            // ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger GenerateTAC Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }


        public CSC_API_ERROR GetTokenKey(DEST_TYPE pSam, short iNewKeyOldKey, out cCCHSSAMTokenKey mTokenKey)
        {
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;

            mTokenKey = new cCCHSSAMTokenKey();

            //Get Token Key.
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GET_TOKEN_KEY, (byte)iNewKeyOldKey, CONSTANT.NULL, 0x0A),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger Get Token Key Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2;//CONSTANT.SAMErrors.SM_FAILURE;

                }
                else
                {
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                    mTokenKey.TokenKeyVer[0] = isoResponseBuffer[0];
                    mTokenKey.TokenKeyVer[1] = isoResponseBuffer[1];
                    Array.Copy(isoResponseBuffer, 2, mTokenKey.TokenKey, 0, 8);
                }
            }
            //ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger Get Token Key Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }

        public CSC_API_ERROR GetDSMID(DEST_TYPE pSam, out uint mDSMID)
        {
            // int ret = -3;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;
            mDSMID = 0;
            // Get SAM DSMID 
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GET_DSM_ID, CONSTANT.NULL, CONSTANT.NULL, 0x04),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetDSMID Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2; //CONSTANT.SAMErrors.SM_FAILURE;
                }
                else
                {
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                    mDSMID = (uint)CFunctions.ConvertLittleEndian(isoResponseBuffer);

                }
            }
            // ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetDSMID Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }

        public CSC_API_ERROR GetDSMInfo(DEST_TYPE pSam, out cCCHSDSMInfo mDSMInfo)
        {
            //int ret = -3;
            CSC_API_ERROR Err;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;

            mDSMInfo = new cCCHSDSMInfo();

            // Get SAM DSMID 
            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GET_DSM_INFO, CONSTANT.NULL, CONSTANT.NULL, 0x31),
                                     out pSw1,
                                     out pSw2,
                                     out isoResponseBuffer);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetDSMIS Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2;//CONSTANT.SAMErrors.SM_FAILURE;
                }
                else
                {
                    int buffindex = 0, index = 0;
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                    byte[] ulDSMid_bytes = new byte[8];
                    try
                    {
                        for (index = 0; index < 4; index++) ulDSMid_bytes[index] = isoResponseBuffer[index];
                        mDSMInfo.ulDSMid = CFunctions.ConvertLittleEndian(ulDSMid_bytes);

                        buffindex += index;
                        for (index = 0; index < 10; index++) mDSMInfo.ucDeviceID[index] = isoResponseBuffer[buffindex + index];

                        buffindex += index;
                        for (index = 0; index < 15; index++) mDSMInfo.ucIPaddress[index] = isoResponseBuffer[buffindex + index];

                        buffindex += index;
                        for (index = 0; index < 20; index++) mDSMInfo.ucUniqueInfo[index] = isoResponseBuffer[buffindex + index];

                    }
                    catch
                    {

                    }

                }
            }
            // ret = (int)Err;
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetDSMIS Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }

        public CSC_API_ERROR GetSonyKeys(DEST_TYPE pSam, short iNewKeyOldKey, short keyindex, out cCCHSSAMSonyKey mcCCHSSAMSonyKey)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_DEVICE;
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
            byte[] isoResponseBuffer;

            mcCCHSSAMSonyKey = new cCCHSSAMSonyKey();
            //Get Sony Key
            Err = Reader.IsoCommand(this.ReaderType,
                                    this.hRw,
                                    pSam,
                                    CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GET_Felica_Access_Key, (byte)iNewKeyOldKey, (byte)keyindex, 0x36),
                                    out pSw1,
                                    out pSw2,
                                    out isoResponseBuffer);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 != CONSTANT.COMMAND_SUCCESS)
                {
                    Err = CSC_API_ERROR.ERR_DATA;
                    Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetSonyKeys Bad data returned");
                    mCCHSSAM_Status = (CONSTANT.SAMErrors)pSw2; //CONSTANT.SAMErrors.SM_FAILURE;
                }
                else
                {
                    int index = 0;
                    mCCHSSAM_Status = CONSTANT.SAMErrors.SM_OK;
                    Array.Copy(isoResponseBuffer, index, mcCCHSSAMSonyKey.bKeyVer, 0, mcCCHSSAMSonyKey.bKeyVer.Length);
                    index = index + mcCCHSSAMSonyKey.bKeyVer.Length;

                    mcCCHSSAMSonyKey.bKeyset = isoResponseBuffer[index++];

                    mcCCHSSAMSonyKey.bKeyNumber = isoResponseBuffer[index++];

                    Array.Copy(isoResponseBuffer, index, mcCCHSSAMSonyKey.bGroupKey, 0, mcCCHSSAMSonyKey.bGroupKey.Length);
                    index += mcCCHSSAMSonyKey.bGroupKey.Length;

                    Array.Copy(isoResponseBuffer, index, mcCCHSSAMSonyKey.bUserKey, 0, mcCCHSSAMSonyKey.bUserKey.Length);
                    index += mcCCHSSAMSonyKey.bUserKey.Length;

                    mcCCHSSAMSonyKey.bNumberOfAreas = isoResponseBuffer[index++];

                    Array.Copy(isoResponseBuffer, index, mcCCHSSAMSonyKey.bAreaCodeList, 0, mcCCHSSAMSonyKey.bAreaCodeList.Length);
                    index += mcCCHSSAMSonyKey.bAreaCodeList.Length;

                    mcCCHSSAMSonyKey.bNumberOfService = isoResponseBuffer[index++];

                    Array.Copy(isoResponseBuffer, index, mcCCHSSAMSonyKey.bServiceCodeList, 0, mcCCHSSAMSonyKey.bServiceCodeList.Length);
                    //index += mcCCHSSAMSonyKey.bServiceCodeList.Length;

                }
            }
            Logging.Log(LogLevel.Verbose, "CCHSSAMManger GetSonyKeys Response code: " + Err.ToString() + " CCHSSAM Status:" + mCCHSSAM_Status.ToString());

            return Err;
        }
        public CSC_API_ERROR ConfigCCHSSAM(DEST_TYPE pSam,bool withDSMInfo)
        {
            // int ret = -3;
            CSC_API_ERROR Err;
            //  byte pSw1 = 0xFF;
            //   byte pSw2 = 0xFF;
            // byte[] isoResponseBuffer;

            Err = SAMSelectApplication(pSam);

            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                Err = SAMActivation(pSam);
            }
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                Err = GetSAMSequence(pSam, out TxnSeqenceNo);
            }
            if (Err == CSC_API_ERROR.ERR_NONE) Err = GetDSMID(pSam, out DSMId);
            // Now Switch from mode1 to mode2
            if (Err == CSC_API_ERROR.ERR_NONE) Err = GetSAMStatus(pSam, out mCCHSStatusInfo);
            // Now Read DSm Data
            if (withDSMInfo)
            {
                if (Err == CSC_API_ERROR.ERR_NONE) Err = GetDSMInfo(pSam, out mCCHSDSMInfo);
            }
            return Err;

        }

        public CSC_API_ERROR ResetCCHSSAM(DEST_TYPE pSam, bool withDSMInfo)
        {
            CSC_API_ERROR Err;

            Err = (CSC_API_ERROR)SAMInstallCard(pSam);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                Err = ConfigCCHSSAM(pSam, withDSMInfo);
            }
            return Err;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pSam"></param>
        /// <param name="mAppId"></param>
        /// <param name="mFileid"></param>
        /// <param name="oldkey"></param>
        /// <param name="AccessRight"></param>
        /// <param name="rndb"></param>
        /// <param name="arAuthCode"></param>
        /// <param name="pSw1"></param>
        /// <param name="pSw2"></param>
        /// <returns></returns>
        public bool GetAuthCode(DEST_TYPE pSam, byte mAppId, byte mFileid, byte oldkey, byte mAccessRights, byte[] rndb, out byte[] arAuthCode, out byte pSw1, out byte pSw2)
        {
            CSC_API_ERROR Err;
            Logging.Log(LogLevel.Verbose, "Get Authcode for AppID- [" + mAppId + "]  FileID = [" + mFileid + "]  AccessRights[ " + mAccessRights + "]");
            arAuthCode = null;
            bool ret = true;
            pSw1 = 0xff;
            pSw2 = 0xFF;
            byte[] cmd_buff = new byte[10];
            cmd_buff[0] = oldkey;
            cmd_buff[1] = mAccessRights;
            byte[] response;

            Array.Copy(rndb, 0, cmd_buff, 2, 8);

            byte[] apdu = CFunctions.getApdu(CONSTANT.ISAM_CLA, CONSTANT.ISAM_INS_GENERATE_DESFire_AUTH_CODE, mAppId, mFileid, cmd_buff, 0x10);

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                  apdu,
                                   out pSw1,
                                   out pSw2,
                                   out response);

            if (Err == CSC_API_ERROR.ERR_NONE && pSw1 == 0x90 && pSw2 == 0x00)
            {
                //success.. copy 16 bytes Auth code  
                arAuthCode = new byte[16];
                Array.Copy(response, 0, arAuthCode, 0, 16);
            }
            else
            {
                ret = false;
                Logging.Log(LogLevel.Error, "GetAuthCode failed pSw1: " + pSw1.ToString("X0") + "pSw2: " + pSw2.ToString("X0"));
            }
            return ret;
        }
        /////// SHA1 Hashing/////

        private byte[] CalculateSHA1Hash(byte[] iData)
        {
            byte[] result;
            // byte[] data = Encoding.ASCII.GetBytes(xmlstr);
            SHA1 sha = new SHA1CryptoServiceProvider();
            // This is one implementation of the abstract class SHA1.
            result = sha.ComputeHash(iData);

            return result;
        }

        private byte[] CalculateSHA1Hash(byte[] iData, int iDatalength)
        {
            byte[] result;
            // byte[] data = Encoding.ASCII.GetBytes(xmlstr);
            SHA1 sha = new SHA1CryptoServiceProvider();
            // This is one implementation of the abstract class SHA1.
            result = sha.ComputeHash(iData, 0, iDatalength);

            return result;
        }

    }
}
