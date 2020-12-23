using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.CryptoFlex
{
    public class CryptoFlexFunctions
    {
        byte[] SAM_AUTH_KEY = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };
        byte SAM_AUTH_KEY_NUM = 0x01;

        private CSC_READER_TYPE ReaderType;
        private int hRw;

        //Flags for health check of SAM1, when required
        private Boolean IsSamBlocked = false;
        private Boolean IsSamCertificateOk = false;

        public CryptoFlexFunctions(CSC_READER_TYPE ReaderType, int hRw)
        {
            this.ReaderType = ReaderType;
            this.hRw = hRw;
        }

        public bool GetDataFromCert(byte[] pCertificate, out CertData pCertData)
        {
            Boolean IsDataValid = false;

            byte[] Subject = new byte[32];
            byte[] NotBefore = new byte[15];
            byte[] NotAfter = new byte[15];

            IsDataValid = (pCertificate.Length == CONSTANT.CFLEX_CERT_SIZE);

            if(IsDataValid)
            {
                Array.Copy(pCertificate, 60, NotBefore, 0, NotBefore.Length);
                Array.Copy(pCertificate, 75, NotAfter, 0, NotAfter.Length);
                Array.Copy(pCertificate, 90, Subject, 0, Subject.Length);
            }

            pCertData.NotBefore = CFunctions.FormartDateStrFromBin(NotBefore);
            pCertData.NotAfter = CFunctions.FormartDateStrFromBin(NotAfter);
            pCertData.Subject = CFunctions.FormartSubjectFromBin(Subject);

            return IsDataValid; 
        }

        internal byte[] GetSerialNbr(DEST_TYPE pSam)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;

            byte[] pResData = new byte[CONSTANT.CFLEX_SNBR_SIZE];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
#if _BIP1300_
#else
            if (SelectRootFolder(pSam))
            {
                //Select the File :- 0002, Serial Number File
                Err = Reader.IsoCommand(this.ReaderType,
                                         this.hRw,
                                         pSam,
                                         CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_SELC_INS, CONSTANT.NULL, CONSTANT.NULL, CFunctions.concBytes(00, 02)),
                                         out pSw1,
                                         out pSw2,
                                         out pResData);

                if (pSw1 == CONSTANT.RESPONSE_OK)
                {
                    Err = Reader.IsoCommand(this.ReaderType,
                                         this.hRw,
                                         pSam,
                                         CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_READ_INS, CONSTANT.NULL, CONSTANT.NULL, (byte)CONSTANT.CFLEX_SNBR_SIZE),
                                         out pSw1,
                                         out pSw2,
                                         out pResData);
                }
            }
#endif
            return pResData;
        }

        public Int64 GetSAMSerialNbr(DEST_TYPE pSam)
        {
            byte[] lia = GetSerialNbr(pSam);
            int result = BitConverter.ToInt32(lia, 0);
            return result;
        }

        public int GetEQPLocalId(DEST_TYPE pSam)
        {
            byte[] lia = GetLocalId(pSam);
            int result = BitConverter.ToInt32(lia, 0);
            return result;
        }

        internal byte[] GetLocalId(DEST_TYPE pSam)
        {
            CSC_API_ERROR Err;

            byte[] pRetBytes = new byte[CONSTANT.CFLEX_LFIL_SIZE];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;
#if _BIP1300_
#else
            if (VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM))
            {
                if (SelectSecurityFolder(pSam))
                {
                    if (SelectLocalInfoFile(pSam))
                    {
                        Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_READ_INS, CONSTANT.NULL, CONSTANT.NULL, (byte)CONSTANT.CFLEX_LFIL_SIZE),
                                     out pSw1,
                                     out pSw2,
                                     out pRetBytes);

                        return pRetBytes;
                    }

                    Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetLocalId: Bad SAM Layout");

                    return pRetBytes;
                }

                Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetLocalId: Bad SAM Layout");

                return pRetBytes;
            }
#endif
            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetLocalId: CHV Check Failed");

            return pRetBytes;
        }

        public byte[] GetCertificate(DEST_TYPE pSam, CERT_TYPE pCertType)
        {
            byte[] pRetBytes = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

            switch (pCertType)
            {
                case CERT_TYPE.LOCAL_CERT:

                    return GetCertificateData(pSam, CONSTANT.CFLEX_LCAF_NUM_B1, CONSTANT.CFLEX_LCAF_NUM_B2);

                case CERT_TYPE.CA_CERT:

                    return GetCertificateData(pSam, CONSTANT.CFLEX_CCAF_NUM_B1, CONSTANT.CFLEX_CCAF_NUM_B2);

                default:

                    return pRetBytes;
            }
        }

        public byte[] GetChallenge(DEST_TYPE pSam, int pExpLength)
        {
            CSC_API_ERROR Err;

            byte[] pRetBytes = new byte[pExpLength];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            if (VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM))
            {
                Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_CHALG_INS, CONSTANT.NULL, CONSTANT.NULL, (byte)pExpLength),
                                     out pSw1,
                                     out pSw2,
                                     out pRetBytes);

                return pRetBytes;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetChallenge: CHV Check Failed");

            return pRetBytes;
        }

        //Check the SAM Block Status
        public bool IsSAMBlocked(DEST_TYPE pSam)
        {
            VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM);

            return IsSamBlocked;
        }

        //Check the SAM Certificate Status
        public bool IsSAMCertificateOk()
        {
            return IsSamCertificateOk;
        }

        public byte[] InternalAuthDes(DEST_TYPE pSam, int pKeyNbr, byte[] pDataIn)
        {
            CSC_API_ERROR Err;

            byte[] pRetBytes = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            //Convert to LSB
            Array.Reverse(pDataIn);

            if (VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM))
            {
                if (SelectSecurityFolder(pSam))
                {
                    Err = Reader.IsoCommand(this.ReaderType,
                                   this.hRw,
                                   pSam,
                                   CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_AUTHI_INS, CONSTANT.NULL, CONSTANT.NULL, GetPKCS1Padding(pDataIn)),
                                   out pSw1,
                                   out pSw2,
                                   out pRetBytes);

                    byte[] pRespBytes = GetResponse(pSam, pSw2);

                    //byte[] pRespBytes = new byte[128];
                    //for (int i = 0; i < 128; i++) pRespBytes[i] = 0;

                    //Convert to MSB 
                    Array.Reverse(pRespBytes);

                    return pRespBytes;
                }

                Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> InternalAuthDes: Bad SAM Layout");

                return pRetBytes;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> InternalAuthDes: CHV Check Failed");

            return pRetBytes;
        }

        public bool ExternalAuthDes(DEST_TYPE pSam, byte[] pDataIn)
        {
            CSC_API_ERROR Err;

            byte[] pRetBytes = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            if (VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM))
            {
                if (SelectSecurityFolder(pSam))
                {
                    Err = Reader.IsoCommand(this.ReaderType,
                                    this.hRw,
                                    pSam,
                                    CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_AUTHE_INS, CONSTANT.NULL, CONSTANT.NULL, pDataIn),
                                    out pSw1,
                                    out pSw2,
                                    out pRetBytes);

                    if (pSw1 == CONSTANT.COMMAND_SUCCESS)
                    {
                        return true;
                    }

                    Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> ExternalAuthDes: Command Error");

                    return false;
                }

                Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> ExternalAuthDes: Bad SAM Layout");

                return false;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> ExternalAuthDes: CHV Check Failed");

            return false;
        }

        /// <summary>
        /// Function used to Decrypt the Data using DES
        /// by CryptoFlex
        /// </summary>
        /// <param name="pSam"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool DesDecrypt(DEST_TYPE pSam,
                               byte[] pEncryptBfr,
                               out byte[] pDecryptBfr)
        {
            CSC_API_ERROR Err;

            pDecryptBfr = new byte[pEncryptBfr.Length];

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            if (VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM))
            {
                if (SelectSecurityFolder(pSam))
                {
                    Err = Reader.IsoCommand(this.ReaderType,
                                    this.hRw,
                                    pSam,
                                    CFunctions.getApdu(CONSTANT.CFLEX_SEC_CLA, CONSTANT.CFLEX_DESI_INS, 0x01, CONSTANT.CFLEX_DKEY_NUM, pEncryptBfr),
                                    out pSw1,
                                    out pSw2,
                                    out pResData);

                    if (pSw1 == CONSTANT.RESPONSE_OK)
                    {
                        pDecryptBfr = GetSecResponse(pSam, pSw2);

                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Internal Function to Verify CHV for AC: Conditions
        /// Required for Certain CryptoFlex Operations
        /// </summary>
        /// <param name="pSam"></param>
        /// <param name="pChvKey"></param>
        /// <param name="pChvKeyNum"></param>
        /// <returns></returns>
        internal bool VerifyCHV(DEST_TYPE pSam,
                                byte[] pChvKey,
                                byte pChvKeyNum)
        {
            CSC_API_ERROR Err;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_AUTH_INS, CONSTANT.NULL, pChvKeyNum, pChvKey),
                                     out pSw1,
                                     out pSw2,
                                     out pResData);

            if (pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                return true;
            }
            else if (pSw1 == 0x45 && pSw2 == 0x53)
            {
                IsSamBlocked = true;

                Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> VerifyCHV: Blocked SAM Present");

                return false;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> VerifyCHV: Failed");

            return false;

        }

        //Standard PKCS1 padding for RSA key operations
        internal byte[] GetPKCS1Padding(byte[] pDataIn)
        {
            byte[] pkcsPadded = new byte[128];

            for (int i = 0; i < pDataIn.Count(); i++)
            {
                pkcsPadded[i] = pDataIn[i];
            }

            //Seperator
            pkcsPadded[pDataIn.Count()] = 0x00;

            for (int j = pDataIn.Count() + 1; j < 126; j++)
            {
                pkcsPadded[j] = 0xFF;
            }

            pkcsPadded[126] = 0x01;
            pkcsPadded[127] = 0x00;

            return pkcsPadded;
        }

        /// <summary>
        /// Function to be Used after any CryptoFlex Read Operations
        /// or require data to be read after the operation
        /// the status code from such operation to be used as the required
        /// length of data to be recieved
        /// </summary>
        /// <param name="pSam"></param>
        /// <param name="pRespSize"></param>
        /// <returns></returns>
        internal byte[] GetResponse(DEST_TYPE pSam, byte pRespSize)
        {
            CSC_API_ERROR Err;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH + CONSTANT.CFLEX_FDES_CRYPTO];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_GETR_INS, CONSTANT.NULL, CONSTANT.NULL, pRespSize),
                                     out pSw1,
                                     out pSw2,
                                     out pResData);
            return pResData;
        }

        /// <summary>
        /// Function to be Used after any security specific operations
        /// </summary>
        /// <param name="pSam"></param>
        /// <param name="pRespSize"></param>
        /// <returns></returns>
        internal byte[] GetSecResponse(DEST_TYPE pSam, byte pRespSize)
        {
            CSC_API_ERROR Err;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH + CONSTANT.CFLEX_FDES_CRYPTO];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_SEC_CLA, CONSTANT.CFLEX_GETR_INS, CONSTANT.NULL, CONSTANT.NULL, pRespSize),
                                     out pSw1,
                                     out pSw2,
                                     out pResData);
            return pResData;
        }

        /* @Params :
         * this.ReaderType  -> V3, V4 reader (V0-V2 not supported)
         * pSam         -> Destination Sam nbr
         * ex : 0x2003 = 20 (B1), 03 (B2)
         * pB1FileNum   -> File Number B1 
         * pB2FileNum   -> File Number B2 ,  */
        internal byte[] GetCertificateData(DEST_TYPE pSam,
                                           byte pB1FileNum,
                                           byte pB2FileNum)
        {
            CSC_API_ERROR Err;

            byte[] pRetBytes = new byte[CONSTANT.CFLEX_CERT_SIZE];

            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            try
            {
                if (VerifyCHV(pSam, SAM_AUTH_KEY, SAM_AUTH_KEY_NUM))
                {
                    if (SelectSecurityFolder(pSam))
                    {
                        // SELECT LOCAL CERTIFICATE FILE
                        Err = Reader.IsoCommand(this.ReaderType,
                                                 this.hRw,
                                                 pSam,
                                                 CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_SELC_INS, CONSTANT.NULL, CONSTANT.NULL, CFunctions.concBytes(pB1FileNum, pB2FileNum)),
                                                 out pSw1,
                                                 out pSw2,
                                                 out pResData);

                        if (pSw1 == CONSTANT.RESPONSE_OK)
                        {
                            IsSamCertificateOk = true;

                            // GET CERTIFICATE FILE DATA in Batches
                            /* Read (0-249 bytes) */
                            Err = Reader.IsoCommand(this.ReaderType,
                                                     this.hRw,
                                                     pSam,
                                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_READ_INS, CONSTANT.NULL, CONSTANT.NULL, 0xFA),
                                                     out pSw1,
                                                     out pSw2,
                                                     out pResData);

                            if (pSw1 == CONSTANT.COMMAND_SUCCESS)
                            {
                                /* First Batch buffer (0-249 bytes) */
                                Array.Copy(pResData, 0, pRetBytes, 0, 250);
                            }

                            /* Read (250-499 bytes) */
                            Err = Reader.IsoCommand(this.ReaderType,
                                                     this.hRw,
                                                     pSam,
                                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_READ_INS, CONSTANT.NULL, 0xFA, 0xFA),
                                                     out pSw1,
                                                     out pSw2,
                                                     out pResData);

                            if (pSw1 == CONSTANT.COMMAND_SUCCESS)
                            {
                                /* Second Batch buffer (250-499 bytes) */
                                Array.Copy(pResData, 0, pRetBytes, 250, 250);
                            }

                            /* Read (501-608 bytes) */
                            Err = Reader.IsoCommand(this.ReaderType,
                                                     this.hRw,
                                                     pSam,
                                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_READ_INS, 0x01, 0xF4, 0x6D),
                                                     out pSw1,
                                                     out pSw2,
                                                     out pResData);

                            if (pSw1 == CONSTANT.COMMAND_SUCCESS)
                            {
                                /* Third Batch buffer (500-608 bytes) */
                                Array.Copy(pResData, 0, pRetBytes, 500, 108);
                            }

                            return pRetBytes;
                        }

                        Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetCertificateData: Bad SAM Layout");

                        return pRetBytes;
                    }
                    else
                    {
                        Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetCertificateData: Bad SAM Layout");

                        return pRetBytes;
                    }
                }
                else
                {
                    Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetCertificateData: CHV Check Failed");

                    return pRetBytes;
                }                
            }
            catch (Exception Ex)
            {
                Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> GetCertificateData: Exception.." + Ex.Message);

                return pRetBytes;
            }

        }

        internal bool SelectLocalInfoFile(DEST_TYPE pSam)
        {
            CSC_API_ERROR Err;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_SELC_INS, CONSTANT.NULL, CONSTANT.NULL, CFunctions.concBytes(CONSTANT.CFLEX_LINF_NUM_B1, CONSTANT.CFLEX_LINF_NUM_B2)),
                                     out pSw1,
                                     out pSw2,
                                     out pResData);

            if (pSw1 == CONSTANT.RESPONSE_OK)
            {
                return true;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> SelectLocalInfoFile : Error..");

            return false;

        }

        internal bool SelectRootFolder(DEST_TYPE pSam)
        {
            CSC_API_ERROR Err;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_SELC_INS, CONSTANT.NULL, CONSTANT.NULL, CFunctions.concBytes(CONSTANT.CFLEX_RDIR_NUM_B1, CONSTANT.CFLEX_RDIR_NUM_B2)),
                                     out pSw1,
                                     out pSw2,
                                     out pResData);

            if (pSw1 == CONSTANT.RESPONSE_OK)
            {
                return true;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> SelectRootFolder : Error..");

            return false;
        }

        internal bool SelectSecurityFolder(DEST_TYPE pSam)
        {
            CSC_API_ERROR Err;

            byte[] pResData = new byte[CONSTANT.MIN_ISO_DATA_OUT_LENGTH];
            byte pSw1 = 0xFF;
            byte pSw2 = 0xFF;

            Err = Reader.IsoCommand(this.ReaderType,
                                     this.hRw,
                                     pSam,
                                     CFunctions.getApdu(CONSTANT.CFLEX_CLA, CONSTANT.CFLEX_SELC_INS, CONSTANT.NULL, CONSTANT.NULL, CFunctions.concBytes(CONSTANT.CFLEX_SDIR_NUM_B1, CONSTANT.CFLEX_SDIR_NUM_B2)),
                                     out pSw1,
                                     out pSw2,
                                     out pResData);

            if (pSw1 == CONSTANT.RESPONSE_OK)
            {
                return true;
            }

            Logging.Log(LogLevel.Error, "CryptoFlexFunctions -> SelectSecurityFolder: Error..");

            return false;
        }

    }
}
