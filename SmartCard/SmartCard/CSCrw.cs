using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Collections;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public class DelhiDesfireEV0 : CommonHwMedia
    {
        private Boolean _recoveryMade = true;
        private readonly bool _bAssumePurseSeqNumAsLocal;
        public DelhiDesfireEV0()
        {
            _bAssumePurseSeqNumAsLocal = Configuration.ReadBoolParameter("AssumePurseSeqNumAsLocal", true);
        }
        
        int _applicationSelected=0;
        
        private Boolean MakeRecovery()
        {
            return true;
        }

        // TODO: Scrap these variables because baseentity are already maintaining with them whether it is read or not
        bool bDM1_PurseLinkageRead = false,
            bDM1_SequenceNumberRead = false,
            bDM1_PurseRead = false,
            bDM1_HistoryRead = false,
            bDM1_ValidationRead = false,
            bDM1_SaleRead = false,
            bDM1_PersonalizationRead = false,
            bDM1_CardHolderRead = false,
            bDM2_PendingFareDeductionFlagFileRead = false,
            bDM2_SaleRead = false,
            bDM2_ValidationRead = false,
            bDM2_SaleAddValueRead = false,
            bDM2_AgentPersonalizationRead = false;

        protected override void _Reset()
        {
            _localSaleDataFileRead = false;
            ResetSelectedAppId();
            bDM1_PurseLinkageRead = false;
            bDM1_SequenceNumberRead = false;
            bDM1_PurseRead = false;
            bDM1_HistoryRead = false;
            bDM1_ValidationRead = false;
            bDM1_SaleRead = false;
            bDM1_PersonalizationRead = false;
            bDM1_CardHolderRead = false;
            bDM2_PendingFareDeductionFlagFileRead = false;
            bDM2_SaleRead = false;
            bDM2_ValidationRead = false;
            bDM2_SaleAddValueRead = false;
            bDM2_AgentPersonalizationRead = false;
        }

        private void logBuffer(string name,byte[] tab,int len)
        {
            string s="";
            for (int i = 0; i < 32; i++) s += tab[i].ToString("X2");
            Logging.Log(LogLevel.Verbose, "ReadCardData : " + name + " :" + s);
        }
        private void logBuffer1(string name, byte[] tab, int len)
        {
            string s = "";
            for (int i = 0; i < len; i++) s += tab[i].ToString("X2");
            Logging.Log(LogLevel.Verbose, "ReadCardData : " + name + " :" + s);
        }

        protected override Boolean _ReadManufacturerData(LogicalMedia logMedia)
        {            
            Media m = logMedia.Media;
            m.ChipSerialNumberRead = SmartFunctions.Instance.ReadSNbr();
            m.ChipTypeRead = Media.ChipTypeValues.DesfireEV0;
            m.TypeRead = Media.TypeValues.CSC;
            return true;                
        }

        protected override Boolean _ReadMediaData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            TransportApplication ta = logMedia.Application.TransportApplication;
            AutoReload ar = logMedia.Purse.AutoReload;
            Customer cu = logMedia.Application.Customer;
            Initialisation ini = logMedia.Initialisation;
            Agent ag = logMedia.Application.Agent;
            LastAddValue lav = logMedia.Purse.LastAddValue;
            Media m = logMedia.Media;
            //long pSerialNbr;

            ////Store Manufacturer Data 
            ////To see because has been already read before
            //Err = SmartFunctions.Instance.ReadUID(out pSerialNbr);

            //m.ChipSerialNumberRead = SmartFunctions.Instance.ReadSNbr();
            //m.HardwareType=Media.HardwareTypeValues.DesfireCSC;
            //m.ChipTypeRead = Media.ChipTypeValues.DesfireEV0;
            //m.TypeRead = Media.TypeValues.CSC;                                
            ResetSelectedAppId();

            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            logMedia.DESFireDelhiLayout.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
            bool bSuccess;
            
            bSuccess = Read_DM1Sale_IfNeeded(logMedia, readPurpose, ta, m, ref pResData);
            if (!bSuccess)
                return false;
            
            bSuccess = Read_DM1_Validation_IfNeeded(logMedia, readPurpose, ta, ar, cu, lav, m, ref pResData);
            if (!bSuccess)
                return false;

            bSuccess = Read_DM1_Personalization_IfNeeded(logMedia, readPurpose, ta, ini, ag, m, ref pResData);
            if (!bSuccess)
                return false;
            
            bSuccess = Read_DM1_PurseLinkage_IfNeeded(logMedia, readPurpose, ref pResData);
            if (!bSuccess)
                return false;

            bSuccess = Read_DM1_History_IfNeeded(logMedia, readPurpose);
            if (!bSuccess)
                return false;

            if (!_ReadTPurseData(logMedia, readPurpose))
                return false;
            
            bSuccess = Read_DM1_CardHolder_IfNeeded(logMedia, readPurpose);
            if (!bSuccess)
                return false;

            
            #region DM2_PendingFareDeductionFlagFile
            //if (readPurpose == MediaDetectionTreatment.TOM_DetailedAnalyis)
            //{
            //    if (!_ReadLocalPendingFareDeductionFlag(logMedia))
            //        return false;
            //}
            //logMedia.DESFireDelhiLayout._df_a2_00_PendingFareDeductionFlag.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);

            #endregion

            bSuccess = Read_DM2Sale_IfNeeded(logMedia, readPurpose);
            if (!bSuccess)
                return false;

            bSuccess = Read_DM2Validation_IfNeeded(logMedia, readPurpose);
            if (!bSuccess)
                return false;

            bSuccess = Read_DM2_SaleAddValue_IfNeeded(logMedia, readPurpose);
            if (!bSuccess)
                return false;

            bSuccess = Read_DM2_AgentPersonalization_IfNeeded(logMedia, readPurpose, m);
            if (!bSuccess)
                return false;

            //----------------------
            return true;
        }

        private bool Read_DM2_AgentPersonalization_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, Media m)
        {
            if (readPurpose == MediaDetectionTreatment.TOM_DetailedAnalyis
                || m.OperationalTypeRead == MediaDescription.OperationalTypeValues.Agent)
            {                
                if (!_ReadAgentData(logMedia, readPurpose))
                    return false;
            }
            return true;
        }

        private bool Read_DM2_SaleAddValue_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            if (readPurpose == MediaDetectionTreatment.TOM_DetailedAnalyis
                || readPurpose == MediaDetectionTreatment.TOM_AutoTopupDeActivation
                || readPurpose == MediaDetectionTreatment.TOM_AnalysisForAddVal
                || readPurpose == MediaDetectionTreatment.TOM_AnalysisForCSCIssue
                || readPurpose == MediaDetectionTreatment.TOM_AnalysisForAddVal_Check
                || readPurpose == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia
                || readPurpose == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia_Check
                )
            {
                if (!_ReadLocalSaleData(logMedia))
                    return false;
            }
            return true;
        }

        private bool Read_DM2Validation_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAddVal_Check:
                    return true;
            }
            if (!_ReadValidationData(logMedia, readPurpose))
                return false;
            return true;
        }

        private bool Read_DM2Sale_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForCSCIssue:
                case MediaDetectionTreatment.TOM_AnalysisForAddVal_Check:                
                case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Purse_Check:                
                    return true;
            }
            
            if (!_ReadApplicationData(logMedia))
                return false;
            
            return true;
        }

        private bool Read_DM1_CardHolder_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            if (readPurpose == MediaDetectionTreatment.TOM_DetailedAnalyis
                || readPurpose == MediaDetectionTreatment.TOM_SettleBadDebt
                || readPurpose == MediaDetectionTreatment.TOM_CSCSurrender
                || readPurpose == MediaDetectionTreatment.TOM_AutoTopupPerform
                || readPurpose == MediaDetectionTreatment.TOM_AutoTopupDeActivation
                || readPurpose == MediaDetectionTreatment.TOM_AutoTopupActivation
                )
            {
                if (!_ReadCustomerData(logMedia))
                    return false;
            }
            logMedia.DESFireDelhiLayout._df_a1_09_cardHolder.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
            return true;
        }

        private bool Read_DM1_History_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            logMedia.Purse.History.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
            if (bDM1_HistoryRead)
                return true;
            if (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis)
                return true;

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            if (!_ReadAllTPurseHistory(logMedia))
                return false;
            bDM1_HistoryRead = true;
            return true;
        }

        private bool Read_DM1_PurseLinkage_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, ref byte[] pResData)
        {
            logMedia.DESFireDelhiLayout._df_a1_00_purseLinkage.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
            if (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis)
                return true;

            if (bDM1_PurseLinkageRead)
                return true;

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;
            Err = SmartFunctions.Instance.ReadPurseLinkageFileDelhiDesfire(out pSw1, out pSw2, out pResData);
            if (Err != CONSTANT.NO_ERROR || pSw1 != CONSTANT.COMMAND_SUCCESS)
                return false;
            bDM1_PurseLinkageRead = true;
            var raw = logMedia.DESFireDelhiLayout._df_a1_00_purseLinkage;
            raw._txnSeqNum.ValueRead = (int)CFunctions.GetBitData(0, 32, pResData);
            raw._txnTimeStamp.ValueRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(32, 32, pResData));
            raw._applicationID.ValueRead = (int)CFunctions.GetBitData(64, 24, pResData);
            raw._zeroBitCount.ValueRead = (byte)CFunctions.GetBitData(88, 8, pResData);

            return true;
        }

        private bool Read_DM1_Personalization_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, TransportApplication ta, Initialisation ini, Agent ag, Media m, ref byte[] pResData)
        {
            if (bDM1_PersonalizationRead)
                return true;

            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAddVal_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Purse_Check:
                case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia_Check:
                    return true;
            }

            // ANUJ: At the moment, I don't see that any checks have to be made while CSC issuing for the file. So, let's bypass it for CSC issuing. If required, uncomment it later.
            //if (readPurpose != MediaDetectionTreatment.TOM_AnalysisForCSCIssue) TOM needs to check for test flag before issuing CSC



            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            Err = SmartFunctions.Instance.ReadPersonalizationFile(out pSw1, out pSw2, out pResData);
            logBuffer("PersonalisationFile", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);

            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                short i;
                bDM1_PersonalizationRead = true;

                var raw = logMedia.DESFireDelhiLayout._df_a1_08_personalization;

                ini.ServiceProviderRead = (short)CFunctions.GetBitData(0, 8, pResData);

                ini.EquipmentNumberRead = (int)CFunctions.GetBitData(8, 24, pResData);
                ini.EquipmentTypeRead = _equipmentType(ini.EquipmentNumber);

                ini.BatchReferenceRead = (long)CFunctions.GetBitData(32, 16, pResData);

                m.InitialisationDateRead = CFunctions.ConvertDosDate(48, pResData);
                ini.DateTimeRead = CFunctions.ConvertDosDate(48, pResData);

                m.FormatVersionRead = (short)CFunctions.GetBitData(64, 8, pResData);

                m.EngravedNumberRead = (long)CFunctions.GetBitData(88, 32, pResData);

                m.OwnerRead = (short)CFunctions.GetBitData(120, 8, pResData);
                ta.OwnerRead = (short)CFunctions.GetBitData(120, 8, pResData);

                i = (short)CFunctions.GetBitData(128, 4, pResData);
                switch (i)
                {
                    case 2:
                        m.HardwareTypeRead = Media.HardwareTypeValues.DesfireCSC;
                        break;
                    default:
                        m.HardwareTypeRead = Media.HardwareTypeValues.Unknown;
                        break;
                }
                raw._mediaType.ValueRead = i;

                i = (short)CFunctions.GetBitData(140, 8, pResData);
                if ((i & 0x40) == 0) m.TestRead = false;
                else m.TestRead = true;
                if ((i & 0x80) == 0) m.OperationalTypeRead = MediaDescription.OperationalTypeValues.Passenger;
                else m.OperationalTypeRead = MediaDescription.OperationalTypeValues.Agent;
                if ((i & 0x80) == 0) ta.OperationalTypeRead = TransportApplication.OperationalTypeValues.Passenger;
                else ta.OperationalTypeRead = TransportApplication.OperationalTypeValues.Agent;
                ta.TestRead = m.TestRead;

                i = (short)CFunctions.GetBitData(148, 8, pResData);
                if (i != 0) ag.ServiceProviderRead = (short)CFunctions.GetBitData(148, 8, pResData);

                raw._issuecompanyID.ValueRead = ini.ServiceProviderRead;
                raw._issueEquipmentID.ValueRead = ini.EquipmentNumberRead;
                raw._issueShiftID.ValueRead = ini.BatchReferenceRead;
                raw._issueDate.ValueRead = m.InitialisationDateRead;
                raw._specificationVersion.ValueRead = m.FormatVersionRead;
                raw._engravedPhysicalnumber.ValueRead = m.EngravedNumberRead;
                raw._serviceproviderID.ValueRead = ta.OwnerRead;
                raw._agentserviceproviderID.ValueRead = ag.ServiceProviderRead;
                raw._flags.ValueRead = (byte)CFunctions.GetBitData(140, 8, pResData);
                raw._projectId.ValueRead = (short)CFunctions.GetBitData(72, 16, pResData);
                raw._zerobitcount.ValueRead = (byte)CFunctions.GetBitData(116, 8, pResData);
                return true;
            }
            else
                return false;
            // TODO: Uncomment after implementing Desfire_AID1_08_PersonalizationAgent 
            //        logMedia.DESFireDelhiLayout._df_a1_08_personalization.Hidden =
            //(readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis
            //&& m.OperationalTypeRead == Media.OperationalTypeValues.Passenger);

        }

        private bool Read_DM1_Validation_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, TransportApplication ta, AutoReload ar, Customer cu, LastAddValue lav, Media m, ref byte[] pResData)
        {
            short i;
            ulong j;

            logMedia.DESFireDelhiLayout._df_a1_05_validation.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);

            if (bDM1_ValidationRead)
                return true;

            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForCSCIssue:
                case MediaDetectionTreatment.TOM_AnalysisForAddVal_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Purse_Check:
                case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia_Check:
                    return true;
            }

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            Err = SmartFunctions.Instance.ReadValidationFile(out pSw1, out pSw2, out pResData);
            logBuffer("ValidationFile DM1", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);

            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM1_ValidationRead = true;

                j = CFunctions.GetBitData(0, 8, pResData);
                if ((j & 1) == 1) m.BlockedRead = true;
                else m.BlockedRead = false;

                i = (short)CFunctions.GetBitData(41, 1, pResData);
                switch (i)
                {
                    default:
                        cu.LanguageRead = (Customer.LanguageValues)i;
                        break;
                }
                i = (short)CFunctions.GetBitData(48, 8, pResData);
                ar.UnblockingSequenceNumberRead = (long)CFunctions.GetBitData(56, 32, pResData);

                i = (short)CFunctions.GetBitData(88, 8, pResData);
                switch (i)
                {
                    case 0:
                    case 1:
                        ta.CustomerLinkRead = (TransportApplication.CustomerLinkValues)i;
                        break;
                    default:
                        ta.CustomerLinkRead = TransportApplication.CustomerLinkValues.Unknown;
                        break;
                }

                lav.ServiceProviderRead = (short)CFunctions.GetBitData(96, 8, pResData);

                i = (short)CFunctions.GetBitData(104, 8, pResData);
                switch (i)
                {
                    default:
                        if ((i >= 0) && (i <= 15)) lav.OperationTypeRead = (LastAddValue.OperationTypeValues)i;
                        else lav.OperationTypeRead = LastAddValue.OperationTypeValues.Unknown;
                        break;
                }

                lav.DateTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(112, 32, pResData));

                lav.AmountRead = 10 * (int)CFunctions.GetBitData(144, 16, pResData);

                lav.EquipmentNumberRead = (int)CFunctions.GetBitData(160, 24, pResData);
                lav.EquipmentTypeRead = _equipmentType(lav.EquipmentNumber);

                lav.SequenceNumberRead = (long)CFunctions.GetBitData(184, 32, pResData);

                var raw = logMedia.DESFireDelhiLayout._df_a1_05_validation;
                raw._autoReloadEnabled.ValueRead = ar.StatusRead;
                raw._autoReloadUnblockingSeqNum.ValueRead = ar.UnblockingSequenceNumberRead;
                raw._blocked.ValueRead = m.BlockedRead;
                raw._customerLink.ValueRead = ta.CustomerLink;
                raw._language.ValueRead = cu.LanguageRead;
                raw._lastAddValueAmount.ValueRead = lav.AmountRead;
                raw._lastAddValueDateTime.ValueRead = lav.DateTimeRead;
                raw._lastAddValueEquipmentId.ValueRead = lav.EquipmentNumberRead;
                raw._lastAddValueSeqNum.ValueRead = lav.SequenceNumberRead;
                raw._lastAddValueServiceProviderId.ValueRead = lav.ServiceProviderRead;
                //                        raw._lastAddValueType.ValueRead = lav;                        
                return true;
            }
            else
            {
                Logging.Log(LogLevel.Error, "DelhiDesfireEV0_ApplicationData:  Data Error");

                return false;
            }
        }

        private bool Read_DM1Sale_IfNeeded(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, TransportApplication ta, Media m, ref byte[] pResData)
        {
            logMedia.DESFireDelhiLayout._df_a1_06_sale.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);

            if (bDM1_SaleRead)
                return true;

            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAddVal_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Purse_Check:
                case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia_Check:
                    return true;
            }

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            Err = SmartFunctions.Instance.ReadSaleFile(out pSw1, out pSw2, out pResData);
            logBuffer("SaleFile DM1", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);
            Logging.Log(LogLevel.Verbose, "Return of read DM1 " + Convert.ToString(pSw1) + " " + Convert.ToString(pSw2) + " " + Convert.ToString(Err));
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                short i;

                bDM1_SaleRead = true;

                ta.DepositRead = 10 * (int)CFunctions.GetBitData(0, 16, pResData); //Price are in cents in the system
                ta.ExpiryDateRead = CFunctions.ConvertDosDate(16, pResData);
                m.ExpiryDateRead = ta.ExpiryDate;
                //Attention that the enum does not correspond mandatory to the value on the card.
                //Can make an exception. Better to check values to avoid problem in time
                //m.StatusRead = (Media.StatusValues)(short)CFunctions.GetBitData(40, 8, pResData);
                byte keyVersion = (byte)CFunctions.GetBitData(32, 8, pResData); // TODO: Newly introduced. Not yet verified.
                i = (short)CFunctions.GetBitData(40, 8, pResData);
                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        m.StatusRead = (Media.StatusValues)i;
                        break;
                    default:
                        m.StatusRead = Media.StatusValues.Unknown;
                        break;
                }

                var raw = logMedia.DESFireDelhiLayout._df_a1_06_sale;
                raw._cardStatus.ValueRead = m.StatusRead;
                raw._cscDeposit.ValueRead = ta.DepositRead;
                raw._cscExpiryDate.ValueRead = m.ExpiryDateRead;
                raw._keyVersion.ValueRead = keyVersion;

                return true;
            }
            else
            {
                Logging.Log(LogLevel.Error, "DelhiDesfireEV0_ApplicationData:  Data Error");
                return false;
            }            
        }

        private bool _ReadLocalPendingFareDeductionFlag(LogicalMedia logMedia)
        {
            if (bDM2_PendingFareDeductionFlagFileRead)
                return true;

            if (!SelectApplication(CONSTANT.DM2_AREA_CODE))
                return false;

            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

            Err = SmartFunctions.Instance.ReadPendingFareDeductionFileDM2_DelhiDesfire(out pSw1, out pSw2, out pResData);

            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM2_PendingFareDeductionFlagFileRead = true;

                int offset = 0;
                var raw = logMedia.DESFireDelhiLayout._df_a2_00_PendingFareDeductionFlag;

                int len = 32;
                raw._txnSeqNum.ValueRead = (int)CFunctions.GetBitData(offset, len, pResData);
                offset += len;

                len = 32;
                raw._txnDateTime.ValueRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(offset, len, pResData));
                offset += len;

                len = 16;
                raw._txnAmount.ValueRead = (short)CFunctions.GetBitData(offset, len, pResData);
                offset += len;

                len = 32;
                raw._cscElectronicValueBeforeTxn.ValueRead = (int)CFunctions.GetBitData(offset, len, pResData);
                offset += len;

                len = 8;
                raw._linkedPurseID.ValueRead = (byte)CFunctions.GetBitData(offset, len, pResData);

                return true;
            }
            else
                return false;
        }

        private bool SelectApplication(byte applN)
        {
            try
            {
                if (_applicationSelected != applN)
                {
                    if (!SmartFunctions.Instance.SelectApplication(applN))
                    {
                        Logging.Log(LogLevel.Error, "DelhiDesfireEV0_ReadMediaData: DM1_AREA_CODE Application Not Found");
                        return false;
                    }
                    _applicationSelected = applN;
                }
                return true;
            }
            finally
            {
                GetLastErrorFromSmartFunction();
            }
        }

        protected override Boolean _ReadApplicationData(LogicalMedia logMedia)
        {
            if (bDM2_SaleRead) 
                return true;

            if (!SelectApplication(CONSTANT.DM2_AREA_CODE))
                return false;
            
            Products ps = logMedia.Application.Products;
            OneProduct p = new OneProduct();
            ps.Add(p);

            Agent ag = logMedia.Application.Agent;

            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

            Err = SmartFunctions.Instance.ReadMetroSaleFile(out pSw1, out pSw2, out pResData);
            logBuffer("SaleFile DM2", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM2_SaleRead = true;
                //Store the DM2 Sale Data
                short fpRead = (short)CFunctions.GetBitData(0, 8, pResData);

                ps.Product(0).TypeRead = fpRead;
                ps.Product(0).StartOfValidityRead = CFunctions.ConvertDosDate(8, pResData);
                ps.Product(0).EndOfValidityRead = CFunctions.ConvertDosDate(24, pResData);
                ps.Product(0).DurationValidityRead = (short)CFunctions.GetBitData(40, 8, pResData);
                ag.TripsExpiryDateRead = CFunctions.ConvertDosDate(48, pResData);
                
                
                var raw = logMedia.DESFireDelhiLayout._df_a2_01_Sale;
                raw._type.ValueRead = ps.Product(0).TypeRead;
                raw._startOfValidity.ValueRead = ps.Product(0).StartOfValidityRead;
                raw._endOfValidity.ValueRead = ps.Product(0).EndOfValidityRead;
                raw._durationValidity.ValueRead = ps.Product(0).DurationValidityRead;
                raw._agentTripsExpiryDate.ValueRead = ag.TripsExpiryDateRead;

                return  true;
            }
            Logging.Log(LogLevel.Error, "DelhiDesfireEV0_ApplicationData:  Data Error");

            return false;
        }

        protected override Boolean _ReadAgentData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            if (bDM2_AgentPersonalizationRead)
            {
                logMedia.DESFireDelhiLayout._df_a2_08_agentPersonalization.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
                return true;            
            }

            if (!SelectApplication(CONSTANT.DM2_AREA_CODE))
                return false;
            
            Agent ag = logMedia.Application.Agent;
            Err = CSC_API_ERROR.ERR_NONE;
                pSw1 = 0xFF;
                pSw2 = 0xFF;
                byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

                Err = SmartFunctions.Instance.ReadMetroPersonalization(out pSw1, out pSw2, out pResData);
                logBuffer("AgentFile DM2", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);

                if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                {
                    bDM2_AgentPersonalizationRead = true;
                    //Store the DM2 Personalization data
                    short i =(short)CFunctions.GetBitData(0, 8, pResData); 
                    switch(i)
                    {
                        case 62:
                            ag.ProfileRead=Agent.AgentProfileValues.Maintenance;
                            break;
                        case 60:
                        case 61:
                            ag.ProfileRead=Agent.AgentProfileValues.Operator;
                            break;
                        case 63:
                            ag.ProfileRead=Agent.AgentProfileValues.StationSupervisor;
                            break;
                        case 67:
                            ag.ProfileRead = Agent.AgentProfileValues.TokenCollector;
                            break;
                        case 69:
                            ag.ProfileRead = Agent.AgentProfileValues.CashCollector;
                            break;
                        default :
                            ag.ProfileRead=Agent.AgentProfileValues.Other;
                            break;
                    }

                    string agentId = "0";
                    for (int x = 0; x < 10; x++)
                    {
                        long b = (long)CFunctions.GetBitData(8+x*8, 80, pResData);
                        if ((b&0xFF) == 0) break;
                        agentId += Convert.ToChar(b & 0xFF);
                    }
                    ag.ReferenceRead = (long)Convert.ToInt64(agentId);

                    //ag.CodeRead = (long)CFunctions.GetBitData(88, 80, pResData);

                    ag.MaxTripsNumberRead = (short)CFunctions.GetBitData(168, 8, pResData);

                    var raw = logMedia.DESFireDelhiLayout._df_a2_08_agentPersonalization;
                    
                    raw._agentType.ValueRead = (byte)CFunctions.GetBitData(0, 8, pResData);
                    raw._agentNumber.ValueRead = ag.ReferenceRead;
                    raw._agentTripsLimit.ValueRead = ag.MaxTripsNumberRead;

                    raw.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
                    return  true;
                }
                Logging.Log(LogLevel.Error, "DelhiDesfireEV0_AgentData:  Data Error");

            return false;
        }

        protected override Boolean _ReadValidationData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            if (bDM2_ValidationRead) 
                return true;            

            if (!SelectApplication(CONSTANT.DM2_AREA_CODE))
                return false;
            
            Validation val = logMedia.Application.Validation;
            TransportApplication ta = logMedia.Application.TransportApplication;
            
            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            short i;
            
            Err = SmartFunctions.Instance.ReadMetroValidationFile(out pSw1, out pSw2, out pResData);
            logBuffer("Validation DM2", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM2_ValidationRead = true;
                _validationDataRead = true;
                //Store the DM2 Validation Data
                val.DateOfFirstTransactionRead = CFunctions.ConvertDosDate(0, pResData);

                i = (short)CFunctions.GetBitData(16, 8, pResData);
                switch (i)
                {
                    case CONSTANT.MBC_GateEntry:
                        val.EntryExitBitRead = Validation.TypeValues.Entry;
                        break;
                    case CONSTANT.MBC_GateExit:
                        val.EntryExitBitRead = Validation.TypeValues.Exit;
                        break;
                    default:
                        val.EntryExitBitRead = Validation.TypeValues.Unknown;
                        break;
                }

                val.LocationRead = (int)CFunctions.GetBitData(24, 8, pResData);

                val.LastTransactionDateTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(32, 32, pResData));

                val.RejectCodeRead = (short)CFunctions.GetBitData(64, 8, pResData);

                i = (short)CFunctions.GetBitData(72, 8, pResData);
                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        ta.StatusRead = (TransportApplication.StatusValues)i;
                        break;
                    default:
                        ta.StatusRead = TransportApplication.StatusValues.Unknown;
                        break;
                }

                ta.TestRead = Convert.ToBoolean((short)CFunctions.GetBitData(80, 1, pResData));

                val.BonusValueRead = (int)CFunctions.GetBitData(81, 16, pResData);

                

                AutoReload ar = logMedia.Purse.AutoReload;
                int offsetAutoTopup = 97;
                i = (short)CFunctions.GetBitData(offsetAutoTopup, 1, pResData);
                offsetAutoTopup += 1;
                if (i == 1)
                    ar.StatusRead = AutoReload.StatusValues.Enabled;
                else
                    ar.StatusRead = AutoReload.StatusValues.Disabled;

                offsetAutoTopup += 6; // reserved bits

                ar.AutoTopupDateAndTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(offsetAutoTopup, 32, pResData));
                offsetAutoTopup += 32;

                ar.ThresholdRead = 10 * (int)CFunctions.GetBitData(offsetAutoTopup, 32, pResData);
                offsetAutoTopup += 32;

                ar.AmountRead = 10 * (int)CFunctions.GetBitData(offsetAutoTopup, 32, pResData);
                offsetAutoTopup += 32;

                offsetAutoTopup += 16; // last credit topup date


                ar.ExpiryDateRead = CFunctions.ConvertDosDate(offsetAutoTopup, pResData);                
                     
                val.AgentRemainingTripsRead = (short)CFunctions.GetBitData(249, 7, pResData);

                return true;
            }
            Logging.Log(LogLevel.Error, "DelhiDesfireEV0_ValidationData:  Data Error");

            return false;
        }

        protected override Boolean _ReadCustomerData(LogicalMedia logMedia)
        {
            if (bDM1_CardHolderRead)
                return true;            

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            
            Customer cu = logMedia.Application.Customer;

            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
         
            Err = SmartFunctions.Instance.ReadCardHolderFile(out pSw1, out pSw2, out pResData);
            logBuffer("Holder DM1", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM1_CardHolderRead = true;
                //Store the DM1 Card Holder Data

                cu.BirthDateRead = CFunctions.ConvertDosDate(64, pResData);
                cu.IDTypeRead = (int)CFunctions.GetBitData(80, 8, pResData);
                cu.IDRead = CFunctions.ByteArrayToStringAssumingNullTerminated(CFunctions.GetBytesFromResp(88, 160, pResData));
                cu.ID = Utility.CleanseString(cu.IDRead);
                var raw = logMedia.DESFireDelhiLayout._df_a1_09_cardHolder;

                raw._patronDOB.ValueRead = cu.BirthDateRead;                
                raw._patronType.ValueRead = cu.IDTypeRead;
                raw._patronID.ValueRead = cu.Name;

                return true;
            }
            Logging.Log(LogLevel.Error, "DelhiDesfireEV0_CustomerData:  Data Error");

            return false;
        }

        private Boolean _localSaleDataFileRead = false;

        protected override Boolean _ReadLocalSaleData(LogicalMedia logMedia)
        {
            if (bDM2_SaleAddValueRead) 
                return true;           

            if (_localSaleDataFileRead) return true;
            if (!SelectApplication(CONSTANT.DM2_AREA_CODE))
                return false;
            
            LocalLastAddValue lcav = logMedia.Application.LocalLastAddValue;
            AutoReload ar = logMedia.Purse.AutoReload;

            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            byte[] pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

            Err = SmartFunctions.Instance.ReadMetroAddValueFile(out pSw1, out pSw2, out pResData);
            logBuffer("AddValue DM2", pResData, CONSTANT.MAX_ISO_DATA_OUT_LENGTH);
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM2_SaleAddValueRead = true;
                _localSaleDataRead = true;

                lcav.SequenceNumberRead = (long)CFunctions.GetBitData(0, 32, pResData);

                lcav.DateTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(32, 32, pResData));

                lcav.AmountRead = 10*(int)CFunctions.GetBitData(64, 16, pResData);

                lcav.NewBalanceRead = 10*(int)CFunctions.GetBitData(80, 32, pResData);

                lcav.EquipmentNumberRead = (int)CFunctions.GetBitData(112, 24, pResData);
                lcav.EquipmentTypeRead = _equipmentType(lcav.EquipmentNumber);

                short i = (short)CFunctions.GetBitData(136, 6, pResData);
                switch (i)
                {
                    case 0x30:
                        lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.Cash;
                        break;
                    case 0x31:
                        lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.BankCard;
                        break;
                    case 0x32:
                        lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.CreditCard;
                        break;
                    case 0x33:
                        lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.Bill;
                        break;
                    case 0x34:
                        lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.Bonus;
                        break;
                    default:
                        lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.Unknown;
                        break;
                }
                logMedia.DESFireDelhiLayout._df_a2_03_SaleAddValue._displayTransactionType.ValueRead = i;

                lcav.LocationRead = (int)CFunctions.GetBitData(142, 8, pResData);

                lcav.ServiceProviderRead = (short)CFunctions.GetBitData(150, 8, pResData);
                //lcav.DateTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(158, 32, pResData));
                
                _localSaleDataFileRead = true;
                return  true;
            }
            Logging.Log(LogLevel.Error, "DelhiDesfireEV0_LocalSaleData:  Data Error");

            return false;
        }

        protected override Boolean _ReadTPurseData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            TPurse tp = logMedia.Purse.TPurse;
                
            bool bSuccess = Read_DM1_SequenceNumber(logMedia, readPurpose, tp);
            if (!bSuccess)
                return false;

            bSuccess = Read_DM1_Purse(logMedia, readPurpose, tp);
            if (!bSuccess)
                return false;            

            return true;
        }

        private bool Read_DM1_Purse(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, TPurse tp)
        {
            logMedia.DESFireDelhiLayout._df_a1_02_purse.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
            if (bDM1_PurseRead)
                return true;

            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check:                
                case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia_Check:
                    return true;
            }

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            int pBalance;
            Err = SmartFunctions.Instance.ReadPurseFile(out pBalance, out pSw1, out pSw2);
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM1_PurseRead = true;
                _tPurseDataRead = true;

                tp.BalanceRead = 10 * pBalance;
                logMedia.DESFireDelhiLayout._df_a1_02_purse._mainElectronicValue.ValueRead = tp.BalanceRead;
                return true;
            }
            else
                return false;
        }

        private bool Read_DM1_SequenceNumber(LogicalMedia logMedia, MediaDetectionTreatment readPurpose, TPurse tp)
        {
            logMedia.DESFireDelhiLayout._df_a1_01_sequenceNumber.Hidden = (readPurpose != MediaDetectionTreatment.TOM_DetailedAnalyis);
            if (bDM1_SequenceNumberRead)
                return true;

            switch (readPurpose)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAddVal_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Cash_Check:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment_Purse_Check:                
                    return true;
            }

            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            long pSqN;
            Err = SmartFunctions.Instance.ReadSequenceNbrFile(out pSqN, out pSw1, out pSw2);
            Logging.Log(LogLevel.Verbose, "ReadTPurseData: err[" + Err.ToString() + "] pSw1[" + pSw1.ToString() + "]");
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM1_SequenceNumberRead = true;

                tp.SequenceNumberRead = pSqN;
                if (_bAssumePurseSeqNumAsLocal)
                    logMedia.Application.TransportApplication.SequenceNumber = pSqN;

                logMedia.DESFireDelhiLayout._df_a1_01_sequenceNumber._mainSequenceNumber.ValueRead = pSqN;
                return true;
            }
            else
                return false;
        }

        protected override Boolean _ReadAutoReloadData(LogicalMedia logMedia)
        {
            if (_localSaleDataFileRead) return true;
            return  _ReadLocalSaleData(logMedia);
        }

        private EquipmentFamily _equipmentType(int number)
        {
            int k = number >> 16;
            return _equipmentType1(k);
        }
        private EquipmentFamily _equipmentType1(int number)
        {
            switch (number)
            {
                case 1:
                case 0xA2:
                    return EquipmentFamily.TVM;
                case 6:
                case 0xA3:
                    return EquipmentFamily.GATE;
                case 0x0A:
                case 0xA4:
                    return EquipmentFamily.HHD;
                case 9:
                case 0xA1:
                    return EquipmentFamily.TOM;
                case 0x20:
                case 0xA0:
                    return EquipmentFamily.BIM;
                case 16:
                    return EquipmentFamily.TRcumAVM;
                default:
                    return EquipmentFamily.Unknown;
            }
        }

        private int _equipmentTypeValue(EquipmentFamily value)
        {
            switch (value)
            {
                case EquipmentFamily.TVM:
                case EquipmentFamily.RCT:
                case EquipmentFamily.CVM:
                    return 1;
                case EquipmentFamily.GATE:
                    return 6;
                case EquipmentFamily.HHD:
                //case EquipmentFamily.Ptd:
                    return 0x0A;
                case EquipmentFamily.TOM:
                    return 9;
                case EquipmentFamily.BIM:
                    return 0x20;
                default:
                    return 1;
            }
        }

        private int _equipmentTypeValue1(int value)
        {
            value = (value >> 16) & 0xFF;
            switch (value)
            {
                case 2://TVM
                case 5: //CVM
                case 18 : //RCT
                    return 1;
                case 4: //GATE
                    return 6;
                case 19:
                //case EquipmentTypeValues.Ptd:
                    return 0x0A;
                case 3: //TOM
                    return 9;
                case 6:
                    return 0x20;
                default:
                    return 1;
            }
        }


        protected override Boolean _ReadAllTPurseHistory(LogicalMedia logMedia)
        {            
            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;
            if (logMedia.Purse.History.List.Count > 0) logMedia.Purse.History.List.Clear();//SKS Added on 20160516
            History his = logMedia.Purse.History;            

            byte[] pResData = new byte[6*CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
            Err = CSC_API_ERROR.ERR_NONE;
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            int NbrOfRecords = 6;

            Err = SmartFunctions.Instance.ReadHistoryFile(NbrOfRecords, out pSw1, out pSw2, out pResData);
            Logging.Log(LogLevel.Verbose, "DelhiDesfireEV0_ReadAllHistory: err["+ Err.ToString() +"] pSw1["+pSw1.ToString() +"]");
            logBuffer1("HistoryFile DM1", pResData, NbrOfRecords*32);
            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
            {
                bDM1_HistoryRead = true;
                for (int index = 0; index < NbrOfRecords; index++)
                {
                    OneTransaction trans = new OneTransaction();

                    int j = index * 32 * 8;

                    //Store History Data [Index]
                    trans.SequenceNumberRead = (long)CFunctions.GetBitData(j + 0, 32, pResData);

                    trans.DateTimeRead = CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(j + 32, 32, pResData));

                    trans.AmountRead = 10 * (int)CFunctions.GetBitData(j + 64, 16, pResData);

                    trans.NewBalanceRead = 10 * (int)CFunctions.GetBitData(j + 80, 32, pResData);

                    int eqpt = (int)CFunctions.GetBitData(j + 112, 8, pResData);
                    int nb = (int)CFunctions.GetBitData(j + 120, 16, pResData);
                    trans.EquipmentTypeRead = _equipmentType1(eqpt);
                    eqpt = (int)trans.EquipmentTypeRead;
                    trans.EquipmentNumberRead = eqpt*65536 + nb;

                    short i = (short)CFunctions.GetBitData(j + 136, 6, pResData);
                    switch (i)
                    {
                        case 0:
                            trans.OperationTypeRead = OperationTypeValues.NoValueDeductedInEntry;
                            break;
                        case 1:
                            trans.OperationTypeRead = OperationTypeValues.ValueDeductedInEntry;
                            break;
                        case 0x10:
                            trans.OperationTypeRead = OperationTypeValues.NoValueDeductedInExit;
                            break;
                        case 0x11:
                            trans.OperationTypeRead = OperationTypeValues.ValueDeductedInExit;
                            break;
                        case 2:
                            trans.OperationTypeRead = OperationTypeValues.PointsOrRidesDeductedInEntry;
                            break;
                        case 3:
                            trans.OperationTypeRead = OperationTypeValues.PeriodicTicketEntry;
                            break;
                        case 4:
                            trans.OperationTypeRead = OperationTypeValues.LoyaltyPointsUsedInEntry;
                            break;
                        default:
                            trans.OperationTypeRead = (OperationTypeValues)i;
                            break;
                    }

                    trans.LocationRead = (int)CFunctions.GetBitData(j + 142, 8, pResData);

                    trans.ServiceProviderRead = (short)CFunctions.GetBitData(j + 150, 8, pResData);

                    his.Add(trans);
                }
                return true;
            }
            else
            {
                Logging.Log(LogLevel.Error, "DelhiDesfireEV0_ReadAllHistory: Data Error");
                return false;
            }
        }


        /// <summary>
        /// Get the Data buffer for Validation File DM1 Area
        /// Refer Delhi Desfire Layout for Field information
        /// File #1, #2, #5
        /// </summary>
        /// <param name="pLogicalMedia"></param>
        /// <returns></returns>
        ///         
        public override Boolean _UpdateTPurseData(LogicalMedia logMedia, int modifyPursevalueBy, bool bCommit)
        {
            if (modifyPursevalueBy > 0)
                Debug.Assert(logMedia.Purse.LastAddValue.Amount == modifyPursevalueBy);

            
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            //Attempt to Load the TPurse balance on card
            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            if (modifyPursevalueBy != 0)
            {
                //Update the Purse Value : DM1
                Err = SmartFunctions.Instance.WritePurseFile(out pSw1, out pSw2, modifyPursevalueBy / 10);

                if (Err != CONSTANT.NO_ERROR || pSw1 != CONSTANT.COMMAND_SUCCESS || pSw2 != 0)
                    return false;
            }
            //Update the Sequence Number : DM1
            Err = SmartFunctions.Instance.WriteSequenceNbrFile(out pSw1, out pSw2, (int)1);

            if (Err != CONSTANT.NO_ERROR || pSw1 != CONSTANT.COMMAND_SUCCESS || pSw2 != 0)
                return false;

            byte[] pResData = new byte[32];
            //Modification JL. In case of deduction no need to store AddValue
            if (modifyPursevalueBy > 0)
            {
                if (!_WriteCommonValidationFile(logMedia))
                    return false;
            }
            if (bCommit)
            {
                Err = SmartFunctions.Instance.CommitTransaction(out pSw1, out pSw2);
                if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return true;
        }

        protected override bool _WriteCommonValidationFile(LogicalMedia logMedia)
        {
            if (!SelectApplication(CONSTANT.DM1_AREA_CODE))
                return false;

            Media m = logMedia.Media;
            Customer cu = logMedia.Application.Customer;
            LastAddValue lav = logMedia.Purse.LastAddValue;
            AutoReload ar = logMedia.Purse.AutoReload;
            TransportApplication ta = logMedia.Application.TransportApplication;

            var bitBuffer = new bool[256];

            int i = 0;

            //Blocking Flag
            i = CFunctions.ConvertToBits((ulong)Convert.ToInt32(m.Blocked), i, 8, bitBuffer);

            //RFU
            i = CFunctions.ConvertToBits(0, i, 32, bitBuffer);

            //CSC Atomicity Flag [TODO]
            i = CFunctions.ConvertToBits(0, i, 1, bitBuffer);

            //Language Bit
            i = CFunctions.ConvertToBits((ulong)cu.Language, i, 1, bitBuffer);

            //Spare
            i = CFunctions.ConvertToBits(0, i, 6, bitBuffer);

            //Auto Reload Enable Flag
            int val;
            switch (ar.StatusRead)
            {
                case AutoReload.StatusValues.Enabled:
                case AutoReload.StatusValues.Disabled:
                    val = (int)ar.Status;
                    break;
                default:
                    val = (int)AutoReload.StatusValues.Disabled;
                    break;
            }
            i = CFunctions.ConvertToBits((ulong)val, i, 8, bitBuffer);

            //Auto Reload unblocking sequence number
            i = CFunctions.ConvertToBits((ulong)ar.UnblockingSequenceNumber, i, 32, bitBuffer);


            //Personalized / Anonymous
            i = CFunctions.ConvertToBits(0, i, 8, bitBuffer);

            //Last Add Value Service provider type
            i = CFunctions.ConvertToBits((ulong)lav.ServiceProvider, i, 8, bitBuffer);

            //Last Add Value type
            i = CFunctions.ConvertToBits((ulong)lav.OperationType, i, 8, bitBuffer);

            int unixtimestamp = CFunctions.ConvertToUnixTimestamp(lav.DateTime);

            //Last Add Value Date and Time
            i = CFunctions.ConvertToBits((ulong)unixtimestamp, i, 32, bitBuffer);

            //Last Add value amount
            i = CFunctions.ConvertToBits((ulong)lav.Amount / 10, i, 16, bitBuffer);

            //Last Add Value Equipment ID
            //i = CFunctions.ConvertToBits((ulong)_equipmentTypeValue(lav.EquipmentType), i, 8, bitBuffer);
            i = CFunctions.ConvertToBits((ulong)lav.EquipmentNumber, i, 24, bitBuffer);

            //Last Add Value SqN 
            i = CFunctions.ConvertToBits((ulong)Math.Abs(lav.SequenceNumber), i, 32, bitBuffer);//SKS: To make it compatible with TOM as tom is writing non -ve value here 

            //Spare Bits
            i = CFunctions.ConvertToBits(0, i, 40, bitBuffer);

            byte[] pResData = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);
            
            bool bSuccess = SmartFunctions.Instance.WriteDataFile(CONSTANT.DM1_AREA_CODE, 0x15, pResData);
            SmartFunctions.Instance.GetLastResult(out Err, out pSw1, out pSw2);
            return bSuccess;
        }

        protected override bool _UpdateWhenMediaDetectedInDenyList(LogicalMedia logMedia)
        {
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            if (SelectApplication(CONSTANT.DM1_AREA_CODE))
            {
                int i = 0;
                var bitBuffer = new bool[256];

                TransportApplication ta = logMedia.Application.TransportApplication;
                Media m = logMedia.Media;
                Customer cu = logMedia.Application.Customer;
                LastAddValue lav = logMedia.Purse.LastAddValue;
                AutoReload ar = logMedia.Purse.AutoReload;

                //Blocking Flag
                i = CFunctions.ConvertToBits((ulong)Convert.ToInt32(m.Blocked), i, 8, bitBuffer);

                //RFU
                i = CFunctions.ConvertToBits(0, i, 32, bitBuffer);

                //CSC Atomicity Flag [TODO]
                i = CFunctions.ConvertToBits(0, i, 1, bitBuffer);

                //Language Bit
                i = CFunctions.ConvertToBits((ulong)cu.Language, i, 1, bitBuffer);

                //Spare
                i = CFunctions.ConvertToBits(0, i, 6, bitBuffer);

                //Auto Reload Enable Flag
                int val;
                switch (ar.StatusRead)
                {
                    case AutoReload.StatusValues.Enabled:
                    case AutoReload.StatusValues.Disabled:
                        val = (int)ar.Status;
                        break;
                    default:
                        val = (int)AutoReload.StatusValues.Disabled;
                        break;
                }
                i = CFunctions.ConvertToBits((ulong)val, i, 8, bitBuffer);

                //Auto Reload unblocking sequence number
                i = CFunctions.ConvertToBits((ulong)ar.UnblockingSequenceNumber, i, 32, bitBuffer);

                //Personalized / Anonymous
                i = CFunctions.ConvertToBits(0, i, 8, bitBuffer);

                //Last Add Value Service provider type
                i = CFunctions.ConvertToBits((ulong)lav.ServiceProvider, i, 8, bitBuffer);

                //Last Add Value type
                i = CFunctions.ConvertToBits((ulong)lav.OperationType, i, 8, bitBuffer);

                int unixtimestamp = CFunctions.ConvertToUnixTimestamp(lav.DateTime);

                //Last Add Value Date and Time
                i = CFunctions.ConvertToBits((ulong)unixtimestamp, i, 32, bitBuffer);

                //Last Add value amount
                i = CFunctions.ConvertToBits((ulong)lav.Amount / 10, i, 16, bitBuffer);

                //Last Add Value Equipment ID
                //i = CFunctions.ConvertToBits((ulong)_equipmentTypeValue(lav.EquipmentType), i, 8, bitBuffer);
                i = CFunctions.ConvertToBits((ulong)lav.EquipmentNumber, i, 24, bitBuffer);

                //Last Add Value SqN 
                i = CFunctions.ConvertToBits((ulong)lav.SequenceNumber, i, 32, bitBuffer);

                //Spare Bits
                i = CFunctions.ConvertToBits(0, i, 40, bitBuffer);

                byte[] pResData = new byte[32];
                pResData = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);

                try
                {
                    if (SmartFunctions.Instance.WriteDataFile(CONSTANT.DM1_AREA_CODE, 0x15, pResData))
                    {
                        Err = SmartFunctions.Instance.CommitTransaction(out pSw1, out pSw2);
                        if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                        {
                            return true;
                        }
                    }

                }
                finally
                {
                    SmartFunctions.Instance.GetLastResult(out Err, out pSw1, out pSw2);
                }
            }
            Logging.Log(LogLevel.Error, "CSCRw _UpdateMediaEndOfValidity cannot write the card");
            return false;
        }

        private bool _tempUpdateMediaEndOfValidity(LogicalMedia logMedia)
        {
            try
            {
                TransportApplication ta = logMedia.Application.TransportApplication;
                int val;
                int i = 0;
                var bitBuffer = new bool[256];
                //Update in case of Media End of validity. Case for DELHI
                i = 0;
                i = CFunctions.ConvertToBits((ulong)ta.Deposit / 10, i, 16, bitBuffer);
                ushort dosDate = CFunctions.ToDosDate(ta.ExpiryDate);
                i = CFunctions.ConvertToBits((ulong)dosDate, i, 16, bitBuffer);
                i = CFunctions.ConvertToBits((ulong)0, i, 8, bitBuffer);
                switch (logMedia.Media.Status)
                {
                    case Media.StatusValues.NotInitialised:
                    case Media.StatusValues.Initialised:
                    case Media.StatusValues.Issued:
                    case Media.StatusValues.Refunded:
                    case Media.StatusValues.Surrendered:
                        val = (int)logMedia.Media.Status;
                        break;
                    default:
                        //Value of status is incorrect ? what to do ? there is perhaps a software BUG.
                        val = (int)logMedia.Media.Status;
                        break;
                }
                i = CFunctions.ConvertToBits((ulong)val, i, 8, bitBuffer);
                //Spare Bits
                i = CFunctions.ConvertToBits(0, i, 208, bitBuffer);
                byte[] pResData = new byte[32];
                pResData = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);
                return (SmartFunctions.Instance.WriteDataFile(CONSTANT.DM1_AREA_CODE, 0x16, pResData));

            }
            finally
            {
                GetLastErrorFromSmartFunction();
            }
        }

        private void GetLastErrorFromSmartFunction()
        {
            SmartFunctions.Instance.GetLastResult(out Err, out pSw1, out pSw2);
        }

        private bool _tempUpdateProductEndOfValidity(LogicalMedia logMedia)
        {
            int i = 0;
            var bitBuffer = new bool[256];
            TransportApplication ta = logMedia.Application.TransportApplication;
            OneProduct pr = logMedia.Application.Products.Product(0); //Only one product on DMRC
            i = CFunctions.ConvertToBits((ulong)pr.Type, i, 8, bitBuffer);
            ushort dosDate = CFunctions.ToDosDate(pr.StartOfValidity);
            i = CFunctions.ConvertToBits((ulong)dosDate, i, 16, bitBuffer);
            dosDate = CFunctions.ToDosDate(pr.EndOfValidity);
            i = CFunctions.ConvertToBits((ulong)dosDate, i, 16, bitBuffer);
            i = CFunctions.ConvertToBits((ulong)pr.DurationValidity, i, 8, bitBuffer);
            dosDate = CFunctions.ToDosDate(logMedia.Application.Agent.TripsExpiryDate);
            i = CFunctions.ConvertToBits((ulong)dosDate, i, 16, bitBuffer);
            byte[] pResData = new byte[32];
            pResData = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);

            if (SmartFunctions.Instance.WriteDataFile(CONSTANT.DM2_AREA_CODE, 0x11, pResData)) return true;
            return false;
        }

        private bool _tempUpdateLocal_SaleNAddVal_Data(LogicalMedia logMedia)
        {
            LocalLastAddValue lcav = logMedia.Application.LocalLastAddValue;
            byte[] pResData = new byte[32];
            
            AutoReload ar = logMedia.Purse.AutoReload;
            var bitBuffer = new bool[256];
            int i = 0;
            //Transaction Number
            i = CFunctions.ConvertToBits((ulong)lcav.SequenceNumber, i, 32, bitBuffer);
            int unixtimestamp = CFunctions.ConvertToUnixTimestamp(lcav.DateTime);
            //Transaction Date and Time
            i = CFunctions.ConvertToBits((ulong)unixtimestamp, i, 32, bitBuffer);
            //Value of Transaction
            i = CFunctions.ConvertToBits((ulong)lcav.Amount/10, i, 16, bitBuffer);
            //CSC Electronic value after Transaction
            i = CFunctions.ConvertToBits((ulong)lcav.NewBalance/10, i, 32, bitBuffer);
            //Last Add Value Equipment ID
            //i = CFunctions.ConvertToBits((ulong)_equipmentTypeValue(lcav.EquipmentType), i, 8, bitBuffer);
            i = CFunctions.ConvertToBits((ulong)lcav.EquipmentNumber, i, 24, bitBuffer);
            //Display Transaction Type
            i = CFunctions.ConvertToBits((ulong)GetDisplayTransactionTypeCode(lcav.OperationType), i, 6, bitBuffer);
            //Transaction Location Code
            i = CFunctions.ConvertToBits((ulong)lcav.Location, i, 8, bitBuffer);
            //Service Provider Id
            i = CFunctions.ConvertToBits((ulong)lcav.ServiceProvider, i, 8, bitBuffer);

            pResData = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);
            //Update DM2 : Last Add Value File
            if (SmartFunctions.Instance.WriteDataFile(CONSTANT.DM2_AREA_CODE, 0x13, pResData)) return true;
            else return false;
        }

        private ulong GetDisplayTransactionTypeCode(LocalLastAddValue.OperationTypeValues operationTypeValues)
        {
            switch (operationTypeValues)
            {
                case LocalLastAddValue.OperationTypeValues.Cash:
                    return 0x30;
                case LocalLastAddValue.OperationTypeValues.BankCard:
                    return 0x31;
                case LocalLastAddValue.OperationTypeValues.CreditCard:
                    return 0x32;
                case LocalLastAddValue.OperationTypeValues.Bill:
                    return 0x33;
                case LocalLastAddValue.OperationTypeValues.Bonus:
                    return 0x34;
                default:
                    return 0;
            }
        }

        private bool _tempInsertOneRecordInDM1HistoryFile(LogicalMedia logMedia)
        {
            byte[] pResData = new byte[32];
            var bitBuffer = new bool[256];
            int i = 0;
            OneTransaction onTxn ;//= new OneTransaction();
            if (logMedia.Purse.History.List.Count == 1)
            {
                onTxn = (OneTransaction)logMedia.Purse.History.Transaction(0);//Assuming only Last transaction record is there
                
                 //Transaction Number
                i = CFunctions.ConvertToBits((ulong)onTxn.SequenceNumber, i, 32, bitBuffer);
                int unixtimestamp = CFunctions.ConvertToUnixTimestamp(onTxn.DateTime);
                //Transaction Date and Time
                i = CFunctions.ConvertToBits((ulong)unixtimestamp, i, 32, bitBuffer);
                //Value of Transaction
                i = CFunctions.ConvertToBits((ulong)onTxn.Amount / 10, i, 16, bitBuffer);
                //CSC Electronic value after Transaction
                i = CFunctions.ConvertToBits((ulong)onTxn.NewBalance / 10, i, 32, bitBuffer);
                //Equipment ID 
                i = CFunctions.ConvertToBits((ulong)(_equipmentTypeValue1(onTxn.EquipmentNumber)), i, 8, bitBuffer);              
                i = CFunctions.ConvertToBits((ulong)(onTxn.EquipmentNumber&0xFFFF), i, 16, bitBuffer);
                //Display Transaction Type
                int j = 0;
                switch (onTxn.OperationType)
                {
                    case OperationTypeValues.NoValueDeductedInEntry:
                        j = 0;
                        break;
                    case OperationTypeValues.ValueDeductedInEntry:
                        j=1 ;
                        break;
                    case OperationTypeValues.NoValueDeductedInExit:
                        j=0x10 ;
                        break;
                    case OperationTypeValues.ValueDeductedInExit:
                        j=0x11 ;
                        break;
                    case OperationTypeValues.PointsOrRidesDeductedInEntry:
                        j=2 ;
                        break;
                    case OperationTypeValues.PeriodicTicketEntry:
                        j=3 ;
                        break;
                    case OperationTypeValues.LoyaltyPointsUsedInEntry:
                        j=4 ;
                        break;
                    default:
                        j= (int)onTxn.OperationType;
                        break;
                }
                i = CFunctions.ConvertToBits((ulong)j, i, 6, bitBuffer);
                //Transaction Location Code
                i = CFunctions.ConvertToBits((ulong)onTxn.Location, i, 8, bitBuffer);
                //Service Provider Id
                i = CFunctions.ConvertToBits((ulong)onTxn.ServiceProvider, i, 8, bitBuffer);
                //Spare
                i = CFunctions.ConvertToBits(0, i, 98, bitBuffer);
                pResData = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);
#if _BIP1300_
                if (SmartFunctions.Instance.WriteRecordFile(CONSTANT.DM1_AREA_CODE,0x03,0x00, pResData)) return true;
#else
                if (SmartFunctions.Instance.WriteRecordFile(CONSTANT.DM1_AREA_CODE, GetFileNum(1, 3), 0x00, pResData)) return true;
#endif
                else return false;
            }
            else
                return false;
        }

        protected override bool _UpdateMediaEndOfValidity(LogicalMedia logMedia)
        {            
            pSw1 = 0xFF;
            pSw2 = 0xFF;

            if (SelectApplication(CONSTANT.DM1_AREA_CODE))
            {
                if (_tempUpdateMediaEndOfValidity(logMedia))
                {
                    Err = SmartFunctions.Instance.CommitTransaction(out pSw1, out pSw2);
                    if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                    {
                        return true;
                    }
                }
            }
            Logging.Log(LogLevel.Error, "CSCRw _UpdateMediaEndOfValidity cannot write the card");
            return false;
        }


        protected override Boolean _WriteLocalSaleData(LogicalMedia logMedia, bool bCommit)
        {
            
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            if (SelectApplication(CONSTANT.DM2_AREA_CODE))
            {
                if (_tempUpdateProductEndOfValidity(logMedia))
                {
                    if (_tempUpdateLocal_SaleNAddVal_Data(logMedia))
                    {
                        if (bCommit)
                        {
                            Err = SmartFunctions.Instance.CommitTransaction(out pSw1, out pSw2);
                            if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            Logging.Log(LogLevel.Error, "CSCRw Write_TpurseData cannot write the card");
            return false;
        }

        protected override bool _WriteCardHolderData(LogicalMedia logMedia)
        {
            if (SelectApplication(CONSTANT.DM1_AREA_CODE))
            {
                var bitBuffer = new bool[256];
                var autoreload = logMedia.Purse.AutoReload;
                var customer = logMedia.Application.Customer;
                int i = 64;
                
                ushort dosDate = CFunctions.ToDosDate(customer.BirthDate);
                i = CFunctions.ConvertToBits((ushort)dosDate, i, 16, bitBuffer);
                i = CFunctions.ConvertToBits((ushort)customer.IDType, i, 8, bitBuffer);
                i = CFunctions.ConvertToBits(customer.ID, i, 160, bitBuffer);

                bool bSuccess = SmartFunctions.Instance.WriteDataFile(CONSTANT.DM1_AREA_CODE, GetFileNum(CONSTANT.DM1_AREA_CODE, 9), CFunctions.ConvertBoolTableToBytes(bitBuffer, 256));
                SmartFunctions.Instance.GetLastResult(out Err, out pSw1, out pSw2);                
                return bSuccess;
            }
            return false;
        }

        protected override Boolean _WriteMainSaleData(LogicalMedia logMedia)
        {           
            if (SelectApplication(CONSTANT.DM1_AREA_CODE))
            {
                var bitBuffer = new bool[256];
                int i = 0;
                i = CFunctions.ConvertToBits((ushort)(logMedia.Application.TransportApplication.Deposit/10), i, 16, bitBuffer);
                i = CFunctions.ConvertToBits(logMedia.Media.ExpiryDate.ToDosDate(), i, 16, bitBuffer);
                i = CFunctions.ConvertToBits(0, i, 8, bitBuffer); // Key version (Not used, as per doc)
                i = CFunctions.ConvertToBits((byte)logMedia.Media.Status, i, 8, bitBuffer);

                bool bSuccess = SmartFunctions.Instance.WriteDataFile(CONSTANT.DM1_AREA_CODE, GetFileNum(CONSTANT.DM1_AREA_CODE, 6), CFunctions.ConvertBoolTableToBytes(bitBuffer, 256));
                SmartFunctions.Instance.GetLastResult(out Err, out pSw1, out pSw2);
                return bSuccess;
            }
            Logging.Log(LogLevel.Error, "CSCRw _UpdateMediaEndOfValidity cannot write the card");
            return false; 
        }

        protected override bool _WriteLocalValidationData(LogicalMedia logMedia)
        {
            if (SelectApplication(CONSTANT.DM2_AREA_CODE))
            {
                var validation = logMedia.Application.Validation;
                var ta = logMedia.Application.TransportApplication;
                var ar = logMedia.Purse.AutoReload;

                var bitBuffer = new bool[256];
                int i = 0;                

                DateTime begin = new DateTime(2000, 1, 1);
                if (validation.LastTransactionDateTime < begin)
                    validation.LastTransactionDateTime = begin;
                i = CFunctions.ConvertToBits(validation.LastTransactionDateTime.ToDosDate(), i, 16, bitBuffer);
                i = CFunctions.ConvertToBits(validation.EntryExitBit == Validation.TypeValues.Entry ? CONSTANT.MBC_GateEntry : CONSTANT.MBC_GateExit , i, 8, bitBuffer);
                i = CFunctions.ConvertToBits((byte)validation.Location, i, 8, bitBuffer);
                i = CFunctions.ConvertToBits((ulong)CFunctions.ConvertToUnixTimestamp(DateTime.Now), i, 32, bitBuffer);
                i = CFunctions.ConvertToBits((byte)validation.RejectCode, i, 8, bitBuffer); // Reject code
                i = CFunctions.ConvertToBits((byte)ta.Status, i, 8, bitBuffer);
                i = CFunctions.ConvertToBits((byte)(ta.Test? 1 : 0), i, 1, bitBuffer);
                i = CFunctions.ConvertToBits((ushort)validation.BonusValue, i, 16, bitBuffer);

                ulong reserved = 0;
                i = CFunctions.ConvertToBits((byte)(ar.Status), i, 1, bitBuffer);
                i = CFunctions.ConvertToBits(reserved, i, 6, bitBuffer);
                int unixtimestamp = CFunctions.ConvertToUnixTimestamp(ar.AutoTopupDateAndTime);
                i = CFunctions.ConvertToBits((ulong)unixtimestamp, i, 32, bitBuffer);
                i = CFunctions.ConvertToBits((ushort)(ar.Threshold / 10), i, 32, bitBuffer);
                i = CFunctions.ConvertToBits((ulong)(ar.Amount / 10), i, 32, bitBuffer);
                ushort lastCreditTopupDate = CFunctions.ToDosDate(ar.AutoTopupDateAndTime);
                i = CFunctions.ConvertToBits((ushort)lastCreditTopupDate, i, 16, bitBuffer);
                ushort dosDate = CFunctions.ToDosDate(ar.ExpiryDate);
                i = CFunctions.ConvertToBits((ushort)dosDate, i, 16, bitBuffer);
                i = CFunctions.ConvertToBits(reserved, i, 17, bitBuffer);
                
                bool bSuccess = SmartFunctions.Instance.WriteDataFile(CONSTANT.DM2_AREA_CODE, GetFileNum(CONSTANT.DM2_AREA_CODE, 2), CFunctions.ConvertBoolTableToBytes(bitBuffer, 256));
                SmartFunctions.Instance.GetLastResult(out Err, out pSw1, out pSw2);
                return bSuccess;
            }
            Logging.Log(LogLevel.Error, "CSCRw _UpdateMediaEndOfValidity cannot write the card");
            return false; 
        }

        public override bool _CommitModifications()
        {
            Err = SmartFunctions.Instance.CommitTransaction(out pSw1, out pSw2);
            return (Err == CSC_API_ERROR.ERR_NONE && pSw1 == 0x90 && pSw2 == 0x00);
        }

        public void ResetSelectedAppId()
        {
            _applicationSelected = 0;
        }

        private static byte GetFileNum(byte area, byte fileNum)
        {
            if (area == 1)
            {
                switch (fileNum)
                {
                    case 0:
                        return 0x10;
                    case 1:
                        return 0x21;
                    case 2:
                        return 0x22;
                    case 3:
                        return 0x43;
                    case 5:
                        return 0x15;
                    case 6:
                        return 0x16;
                    case 8:
                        return 0x08;
                    case 9:
                        return 0x09;
                    default:
                        Debug.Assert(false);
                        return 0;
                }
            }
            else if (area == 2)
            {
                switch (fileNum)
                {
                    case 0:
                        return 0x10;
                    case 1:
                        return 0x11;
                    case 2:
                        return 0x12;
                    case 3:
                        return 0x13;
                    case 8:
                        return 0x08;
                    default:
                        Debug.Assert(false);
                        return 0;
                }
            }
            else
            {
                Debug.Assert(false);
                return 0;
            }
        }

        protected override Boolean _WriteOneRecord(LogicalMedia logMedia, byte mApplication, byte mFileId)
        {
            Err = CSC_API_ERROR.ERR_NOEXEC;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            if (SelectApplication(mApplication))
            {
                if (mApplication == CONSTANT.DM1_AREA_CODE && mFileId == 0x03)
                {
                    if (_tempInsertOneRecordInDM1HistoryFile(logMedia))
                    {
                        Err = SmartFunctions.Instance.CommitTransaction(out pSw1, out pSw2);
                        if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }// Boolean _WriteOneRecord

        protected override bool _AppendCommonAreaPurseHistoryRecord(LogicalMedia logMedia)
        {
            return _tempInsertOneRecordInDM1HistoryFile(logMedia);
        }

        public override Status GetLastStatus()
        {
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
                if (pSw1 == 0x90 && pSw2 == 0)
                    return Status.Success;
                else if (pSw1 == 0x96 && pSw2 == 0xAE)
                    return Status.Failed_MediaFailedToAuthenticate;
                else
                {
                    Logging.Log(LogLevel.Information, "GetLastStatus FailedNotCategorized Err == CSC_API_ERROR.ERR_NONE pSw1/pSw2 = " + pSw1.ToString("X2") + "/" + pSw2.ToString("X2"));
                    return Status.FailedNotCategorized;
                }
            }
            else if (Err == CSC_API_ERROR.ERR_TIMEOUT 
                //|| Err == CSC_API_ERROR.ERR_DATA
                )
                return Status.Failed_MediaWasNotInField;
            else
            {
                Logging.Log(LogLevel.Information, "GetLastStatus FailedNotCategorized Err = " + Err.ToString() + " pSw1/pSw2 = " + pSw1.ToString("X2") + "/" + pSw2.ToString("X2"));
                return Status.FailedNotCategorized;
            }
        }
    }
}
