using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;

using IFS2.Equipment.Common;
using System.Diagnostics;
using System.Xml.Linq;

using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules    
    {
        //static History _TxnHistory = new History();
        public TTErrorTypes TreatmentOnCardDetection(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                TTErrorTypes err=TTErrorTypes.NoError;
                
                string XdrDatastr = "";
                
                //Read Media Data
                bool bReadMediaDataResult = hwCsc.ReadMediaData(logMedia, readPurpose);

                if (!bReadMediaDataResult)
                {
                    Communication.SendMessage(ThreadName, "", "ReadUserCardSummaryAnswer", Convert.ToString((int)TTErrorTypes.CannotReadTheCard), "");
                    return TTErrorTypes.CannotReadTheCard;
                }

                //Make standard verifications to card (like status blacklist,end of validity ...
                //At the moment I have strange cards so.
                if (err == TTErrorTypes.NoError)
                {                    
                    err = CommonRules.CheckMediaData(logMedia);

                    if (err == TTErrorTypes.MediaInDenyList) // this itself implies that Media was not yet blocked yet
                    {
                        hwCsc.UpdateWhenMediaDetectedInDenyList(logMedia);

                        // for AVM only

                        SharedData.TransactionSeqNo++;

                        if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                            SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.BlacklistDetection, SharedData.TransactionSeqNo, 0, out XdrDatastr);
                        else SharedData.SaveContextFile();//save txn sequence No 
                       
                        logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;
                    }
                }                    
                    if (err != TTErrorTypes.NoError)
                    {
                        if ((bool)Configuration.ReadParameter("GenerateRejectionCSCTransactions", "bool", "false"))
                        {
                            if (err == TTErrorTypes.MediaBlocked)
                            {
                                XdrDatastr = "";
                                SharedData.TransactionSeqNo++;
                                if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                                    SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.MediaBlocked, SharedData.TransactionSeqNo, 0, out XdrDatastr);
                                else SharedData.SaveContextFile();//save txn sequence No 

                                logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;
                            }
                            
                            if (//err == TTErrorTypes.CardNotIssued 
                                //|| 
                                err == TTErrorTypes.LastAddValueDeviceBlacklisted 
                                //|| err == TTErrorTypes.MediaEndOfValidityReached 
                                || err == TTErrorTypes.MediaNotInitialised 
                                || err == TTErrorTypes.MediaNotSold
                                || err == TTErrorTypes.NotDMRCCard 
                                //|| err == TTErrorTypes.UnknownError
                                )
                            {
                                XdrDatastr = "";
                                int rejectioncode = (int)err;
                                switch (err)
                                {
                                    //case TTErrorTypes.CannotReadTheCard:
                                    //    rejectioncode = (int)CSC_CCHS_RejectReason.ExcessiveReadFailure;
                                    //    break;
                                    //case TTErrorTypes.CardNotIssued:
                                    //    rejectioncode = (int)CSC_CCHS_RejectReason.CSCTicketNotActive;
                                    //    break;
                                    case TTErrorTypes.NotDMRCCard:
                                        rejectioncode = (int)CSC_CCHS_RejectReason.IssureNotSupported;
                                        break;
                                    case TTErrorTypes.LastAddValueDeviceBlacklisted:
                                        rejectioncode = (int)CSC_CCHS_RejectReason.LastAddValueDeviceBlacklisted;
                                        break;
                                    case TTErrorTypes.MediaNotSold:
                                    case TTErrorTypes.MediaNotInitialised:
                                        rejectioncode = (int)CSC_CCHS_RejectReason.IllegalCSCType;
                                        break;
                                }
                                SharedData._rejectionCode = rejectioncode;
                                // all these cases are treated as Media Rejected .....
                                SharedData.TransactionSeqNo++;
                                // Do we really need to send TransactionType.MediaRejection i.e. TXN_CSC_REJECTED, because as per CCHS document, TXN_CSC_REJECTED is applicable only for Gate
                                if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                                    SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.MediaRejection, SharedData.TransactionSeqNo, 0, out XdrDatastr);
                                else SharedData.SaveContextFile();//save txn sequence No 
                                logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;
                            }
                        }
                    }

                   else
                    {
                        if (_logMediaReloader.Media.OperationalType == MediaDescription.OperationalTypeValues.Passenger)
                        {
                            return TreatCardSummary(logMedia.Media.ChipSerialNumber);
                        }
                        else if (_logMediaReloader.Media.OperationalType == MediaDescription.OperationalTypeValues.Agent)
                        {
                            if (!hwCsc.ReadAgentData(_logMediaReloader, readPurpose))
                                err = TTErrorTypes.CannotReadTheCard;

                            if (err == TTErrorTypes.NoError)
                                err = CommonRules.CheckAgentData(_logMediaReloader);


                            if (err == TTErrorTypes.NoError)
                            {
                                string s = _logMediaReloader.ToXMLString();
                                Communication.SendMessage(ThreadName, "Message", "AgentCardDetection", Convert.ToString((int)TTErrorTypes.NoError), s);
                                
                                return err;
                            }

                            Communication.SendMessage(ThreadName, "Message", "BadAgentCardDetection", Convert.ToString((int)err), _logMediaReloader.ToXMLString());
                            return err;
                        }
                    }
                if(_signatureAtEachTransaction && _generateXdrInTransaction)
                    Communication.SendMessage(ThreadName, "Message", "BadPassengerCardDetection", Convert.ToString((int)err), _logMediaReloader.ToXMLString() + "<XdrData>" + XdrDatastr + "</XdrData>", ((XdrDatastr == "" || XdrDatastr == null) ? false.ToString() : true.ToString()));
                else Communication.SendMessage(ThreadName, "Message", "BadPassengerCardDetection", Convert.ToString((int)err), _logMediaReloader.ToXMLString());

                Logging.Log(LogLevel.Verbose, "CSCFunctions_TreatmentOnCardDetection " + Convert.ToString((int)err));

                return err;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error,"CSCFunstions_TreatmentOnCardDetection "+e.Message);
                return TTErrorTypes.Exception;
            }
        }

        public void TreatmentOnCardDetection2()
        {            
            TreatmentOnCardDetection2(true, false);
            return;
        }

        int EF_GetAdjustmentDifferenceForExcessDistance()
        {
            Logging.Log(LogLevel.Verbose, "EF_GetAdjustmentDifferenceForExcessDistance");
            LogicalMedia logMedia;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
            {
                logMedia = _logMediaReloader;
            }
            else if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
            {
                logMedia = _logMediaToken;
            }
            else
            {
                Logging.Log(LogLevel.Error, "Bad media type");
                return Int32.MaxValue;
            }

            int fp = logMedia.Application.Products.Product(0).Type;
            int entryStationCode;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
            {
                logMedia = _logMediaReloader;
                Debug.Assert(false);
                return Int32.MaxValue;
            }
            else if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
            {
                logMedia = _logMediaToken;
                entryStationCode = logMedia.Application.LocalLastAddValue.LocationRead;
            }
            else
            {
                Logging.Log(LogLevel.Error, "Bad media type");
                return Int32.MaxValue;
            }

            int ActualPrice;
            if (GetVirtualSiteId(entryStationCode) == GetVirtualSiteId(SharedData.StationNumber))
                ActualPrice = FareParameters.ShortReturnTripFare;
            else
            {
                int ActualFareTier;
                ActualPrice = SalePriceCalculation.CalculatePriceSiteBased(fp, entryStationCode, SharedData.StationNumber, DateTime.Now, out ActualFareTier);
            }

            int fareTierForWhichTokenWasSold = logMedia.Application.LocalLastAddValue.FareTiers;
            
            DateTime dtTimeOfTokenSale = DateTime.Now; // It is not a harsh assumption, for which it is needed. Ideally, it is older section of vtd. but I am not sure how samsung is treating it.
                    // Also, neither in current not in future time-wise fare is going to be made applicable for tokens.
            int PaidPrice = SalePriceCalculation.CalculateTokenPriceZoneBased(fareTierForWhichTokenWasSold);//logMedia.Application.LocalLastAddValue.AmountRead;
            Logging.Log(LogLevel.Verbose, "fareTierForWhichTokenWasSold = " + fareTierForWhichTokenWasSold + " PaidPrice = " + PaidPrice);

            return Math.Max(0, ActualPrice - PaidPrice);
        }

        int GetVirtualSiteId(int siteId)
        {
            int result = siteId;
            bool bFound = Config._VirtualSiteId.TryGetValue(siteId, out result);
            return result;
        }

        public void SetReadPurpose(MediaDetectionTreatment readDataFor)
        {
            MediaDetectionTreatment readDataFor_Prior = _readDataFor;
            _readDataFor = readDataFor;
#if !_HHD_
            if (_readDataFor != MediaDetectionTreatment.TOM_AnalysisForAddVal)
                _readDataForAddVal_RechargeValueRequested = null;
            if (_readDataFor != MediaDetectionTreatment.TOM_PutNewProductInExistingMedia)
                _readDataForPutNewProductInExistingMedia_ProductTypeRequested = null;

            if (_reader != null && _IsReaderLoaded)
            {
                if (_readDataFor != MediaDetectionTreatment.None)
                {
                    _errorCurMedia = TTErrorTypes.NoError;
                    _adjustment = null;

                    _reader.StartPolling();
                }
                else if (//readDataFor_Prior != MediaDetectionTreatment.None && 
                    _readDataFor == MediaDetectionTreatment.None)
                    _reader.StopPolling(); 
            }
#endif
        }

        public TTErrorTypes ErrorForJustProducedMedia
        {
            get
            {
                return _errorCurMedia;
            }
        }

        private readonly List<TTErrorTypes> _lstErrorCodesForBadMedia = new List<TTErrorTypes> { 
            TTErrorTypes.MediaBlocked,
            TTErrorTypes.CardNotIssued,
            TTErrorTypes.MediaNotInitialised,
            TTErrorTypes.MediaNotSold,
            TTErrorTypes.NotDMRCCard,
            TTErrorTypes.LastAddValueDeviceBlacklisted,
            TTErrorTypes.MediaEndOfValidityReached 
        };
        // Generally every function should do only One Thing. But TreatOnMediaDetection2 is doing multiple things
        // a. Reading
        // b. Performing analysis conditionally (on the basis of bReadOnlyAndDontMakeAnyDecision)
        // c. Broadcasting the analysis result conditionally
        // This is not that bad, because b. is dependent on a.
        public void TreatmentOnCardDetection2(
            bool bSendStatus,
            bool bReadOnlyAndDontMakeAnyDecision
            )
        {
            Logging.Log(LogLevel.Verbose, String.Format("TreatmentOnCardDetection2 bSendStatus = {0} bReadOnlyAndDontMakeAnyDecision = {1}", bSendStatus, bReadOnlyAndDontMakeAnyDecision));
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess = false;
            SharedData._rejectionCode = 0;

            try
            {
                string XdrDatastr = "";

                if (!bReadOnlyAndDontMakeAnyDecision)
                    EF_CheckOperationValidOnTheMediaType();
                
                ReadMediaAsPerRequirements();
                
                if (bReadOnlyAndDontMakeAnyDecision)
                    return;

                if (_errorCurMedia == TTErrorTypes.CannotReadTheCard)
                {
                    if (bSendStatus)
                        SendMsg.ReadUserCardSummaryAnswer(TTErrorTypes.CannotReadTheCard);
                    return;
                }
                else if (_errorCurMedia == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                    return;

                if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
                {                    
                    if (_logMediaReloader.Media.OperationalTypeRead == MediaDescription.OperationalTypeValues.Agent)
                    {
                        switch(_readDataFor)
                        {
                            case MediaDetectionTreatment.TOM_Login:
                            case MediaDetectionTreatment.BasicAnalysis_TOM:
                                {
                                    if (bSendStatus)
                                    {
                                        if (_errorCurMedia == TTErrorTypes.NoError)
                                            SendMsg.AgentCardDetection(_logMediaReloader, _readDataFor);
                                        else
                                            SendMsg.BadAgentCardDetection(_errorCurMedia, _logMediaReloader, _readDataFor);
                                    }
                                    return;
                                }
                            case MediaDetectionTreatment.TOM_DetailedAnalyis:
                                _errorCurMedia = CommonRules.CheckAgentData(_logMediaReloader);
                                break;
                            default:
                                _errorCurMedia = TTErrorTypes.InvalidOperationalType;
                                break;
                        }
                    }
                }                

                CheckOpeartionalTypeForCSC(); // Only valid cards: Agent and Passenger
                // TODO: See EF_TOM_BS21C_Desfire_GenericCardControl, what it does and then adapt it here.
                // TODO: See EF_CSCC_IsTicketInValidityPeriod is required, and for what operations
                EF_CSCC_ControlTicketNotBlocked();
                EF_CSCC_ControlTicketNotSurrendered();
                EF_TOM_ControlCSCCIssuanceData();
                ChecksOnReadCSC_CheckForMediaStatus(); // Depending upon the operation demanded, check the status
                ChecksOnReadToken_CheckForMediaStatus();
                if (SharedData._agentShift != null)
                    CheckOnRead_TestFlag(); // if test flag is set and it is not maintainance shift, don't allow. Similarly vice-versa                
                CheckForValidTicketType(); // Product type exists in our list of table. Product type is on valid Media Type.
                CheckMediaExpiry();
                CheckForBlackListing();
                CheckOperationIsOpenForFareProduct();
                EF_FareProductResidingInCompatibleCardType();
                EF_CheckOperationValidOnTheFareProduct();
                EF_CheckForRejectCode();
                CheckForIncidentMode();
                EF_TOM_ControlTokenLifetime();
                EF_Token_VerifyMac();

                switch (_readDataFor)
                {
                    case MediaDetectionTreatment.TOM_AnalysisForRefund:
                        {
                            if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
                            {
                                if (_eqptStatus.FareMode != FareMode_Incident)
                                {
                                    SJTRefund_CheckTokenHasNotBeenUsed();
                                    SJTRefund_CheckTimeLimit();
                                    SJTRefund_SaleStationIsSameAsThisOne();
                                }
                            }
                            else
                            {
                                CSCRefund_CheckBonusIsZero();
                                CSCRefund_SeeIfAutoTopupIsEnabled();
                                CSCRefund_SeeIfExitIsNotMade();
                            }
                            break;
                        }
                    case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                        {
                            CheckAddValFeasibility();                            
                            break;
                        }
                    case MediaDetectionTreatment.TOM_AnalysisForTokenIssue:
                        {
                            // Commenting because already taken care inside ChecksOnReadToken_CheckForMediaStatus
                            //CheckTokenIssueFeasibility();
                            CheckItMustNotBeATTag();
                            break;
                        }
                    case MediaDetectionTreatment.TOM_AnalysisForCSCIssue:
                        {
                            CheckCSCIssueFeasiblity();
                            break;
                        }                    
                    case MediaDetectionTreatment.TOM_DetailedAnalyis:
                        {
                            CheckForAdjustment();
                            //CheckItMustNotBeATTag();
                            break;
                        }                    
                    case MediaDetectionTreatment.TOM_AutoTopupActivation:
                            CheckforAutoTopupEnable();
                            break;                        
                    case MediaDetectionTreatment.TOM_AutoTopupDeActivation:
                            CheckCSCIsBankTopupEnabled();
                            break;
                    case MediaDetectionTreatment.TOM_SettleBadDebt:
                            CheckCSCIsBankTopupEnabled();
                            break;
                    case MediaDetectionTreatment.TOM_AutoTopupPerform:
                            CheckCSCIsBankTopupEnabled();
                            CheckCSCForBankTopupExpiry();
                            CheckAddValFeasibility();
                            CheckforAutoTopupPerform();
                            break;
                    case MediaDetectionTreatment.AnalyisForTTagUpdate:
                        CheckItHasToBeTTag();
                        break;
                    case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia:
                        NewProductInExistingMedia_CheckForExistingProduct();
                        NewProduct_CheckForNonStoredValProduct();
                        break;
                }

                if (_errorCurMedia == TTErrorTypes.MediaInDenyList)
                {
                    logMedia.Media.Blocked = true;
                    logMedia.Media.ReasonOfBlocking = MediaDenyList.CurrentMedia.Reason;
                    logMedia.Application.TransportApplication.Blocked = true;
                    logMedia.Application.TransportApplication.ReasonOfBlocking = MediaDenyList.CurrentMedia.Reason;

                    hwCsc.UpdateWhenMediaDetectedInDenyList(logMedia);
                    bSendStatus = true;
                }
                if (bSendStatus)
                {
                    SharedData._rejectionCode = 0;
                    bool bBadMedia = _lstErrorCodesForBadMedia.Contains(_errorCurMedia);
                    
                    if (_errorCurMedia == TTErrorTypes.MediaInDenyList)
                    {
                        if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                            SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.BlacklistDetection, ++SharedData.TransactionSeqNo, 0, out XdrDatastr);
                    }
                    else if (_errorCurMedia == TTErrorTypes.MediaBlocked)
                    {
                        if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                            SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.MediaBlocked, ++SharedData.TransactionSeqNo, 0, out XdrDatastr);
                    }
                    //else if (_ttErrorCodesVsCCHSRejectCode.ContainsKey(_errorCurMedia))
                    //{
                    //    bBadMedia = true;
                    //    if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                    //    {
                    //        SharedData._rejectionCode = (int)_ttErrorCodesVsCCHSRejectCode[_errorCurMedia];
                    //        SmartFunctions.Instance.GetTDforCCHS(logMedia, TransactionType.MediaRejection, ++SharedData.TransactionSeqNo, 0, out XdrDatastr);
                    //    }
                    //    logMedia.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo; // copied from TreatementOnCardDetection. But is it okay to set the sequencenumber here??
                    //}

                    if (bBadMedia)
                        SendMsg.BadPassengerCardDetection(_errorCurMedia, logMedia, XdrDatastr, _readDataFor);
                    else
                    {
                        if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
                            SendMsg.ReadUserCardSummaryAnswer(_errorCurMedia, logMedia, _readDataFor, _adjustment);
                        else if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
                        {
                            if (_readDataFor == MediaDetectionTreatment.TOM_AnalysisForRefund)
                            {
                                int fareTier = logMedia.Application.LocalLastAddValue.FareTiersRead;
                                DateTime saleTime = logMedia.Application.LocalLastAddValue.DateTimeRead;
                                if (saleTime.Hour == 0 && saleTime.Minute == 0 && saleTime.Second == 0)
                                {
                                    // with current layout, we always reach here, because it doesn't save time component
                                    saleTime = saleTime.AddHours(5);
                                }
                                int priceAsPerEOD = SalePriceCalculation.CalculateTokenPriceZoneBased(fareTier, saleTime);
                                SendMsg.CSTMediaDetectionForTokenRefund(_errorCurMedia, logMedia, bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess, priceAsPerEOD);
                            }
                            else
                                SendMsg.CSTMediaDetection(_errorCurMedia, logMedia, _readDataFor, _adjustment, bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess);
                        }
                    }
                }
                _adjustment = null; // discarding _adjustment immediatly post sending: may be this is bad, but not too bad.

                return;                
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "CSCFunstions_TreatmentOnCardDetection 2" + e.Message);
                _errorCurMedia = TTErrorTypes.Exception; // BUT what to do of logical media data structures.
            }
        }

        private void NewProduct_CheckForNonStoredValProduct()
        {
#if !_HHD_
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            if (_readDataFor != MediaDetectionTreatment.TOM_PutNewProductInExistingMedia)
                return;
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            int purseValueExisting = logMedia.Purse.TPurse.BalanceRead;
            if (purseValueExisting > 0 && ProductParameters.GetProductFamily((int)_readDataForPutNewProductInExistingMedia_ProductTypeRequested) != 60)
                _errorCurMedia = TTErrorTypes.VendNewProductInExistingCSC_OnlySVsWithNonZeroPurseBalanceAllowed;
#endif
        }

        private void NewProductInExistingMedia_CheckForExistingProduct()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            if (_readDataFor != MediaDetectionTreatment.TOM_PutNewProductInExistingMedia)
                return;

            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            
            int existingFp = logMedia.Application.Products.Product(0).Type;
            if (existingFp == 0)
                return;
            DateTime endOfValidityOfExistingProduct = logMedia.Application.Products.Product(0).EndOfValidity;
            if (DatesUtility.BusinessDay(DateTime.Now, OverallParameters.EndBusinessDay) < endOfValidityOfExistingProduct) //  TODO: Check that this condition is correct
                _errorCurMedia = TTErrorTypes.VendNewProductInExistingCSC_ExistingProductStillValid;
        }

        private void CheckItMustNotBeATTag()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN && !_logMediaToken.TTag.Hidden)
                _errorCurMedia = TTErrorTypes.OpPatronTokenOp_ItIsTTag;
        }

        private void CheckItHasToBeTTag()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD 
                || (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN && _logMediaToken.TTag.Hidden))
                _errorCurMedia = TTErrorTypes.TTagUpdate_NotATTag;
        }

        private void CheckCSCForBankTopupExpiry()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            bool bAutoTopupExpired = (_logMediaReloader.Purse.AutoReload.ExpiryDateRead < DateTime.Now);
            if (bAutoTopupExpired)
            {
                _errorCurMedia = TTErrorTypes.OpOnBankTopupEnabledCSC_BankTopupExpiryDatePassed;
            }
        }

        private void CheckforAutoTopupPerform()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            if (_logMediaReloader.Purse.TPurse.BalanceRead > _logMediaReloader.Purse.AutoReload.ThresholdRead)                        
                _errorCurMedia = TTErrorTypes.OpPerformAutoTopup_PurseAlreadyHasGreaterValueThanThreshold;            
        }

        private void CSCRefund_CheckBonusIsZero()
        {
            if (_errorCurMedia != TTErrorTypes.NoError)
                return;
            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (logMedia.Application.Validation.BonusValue != 0)
            {
                _errorCurMedia = TTErrorTypes.OpRefund_UnhandledNonZeroBonusValue;
            }
        }

        private void CheckForIncidentMode()
        {
        }

        private void EF_CSCC_ControlTicketNotSurrendered()
        {
            if (_errorCurMedia != TTErrorTypes.NoError)
                return;
            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();            
            if (logMedia.Media.Status == Media.StatusValues.Surrendered)
            {
                _errorCurMedia = TTErrorTypes.SurrenderedMedia;
            }
        }

        private void EF_TOM_ControlCSCCIssuanceData()
        {
            if (_errorCurMedia != TTErrorTypes.NoError)
                return;
            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            // TODO: See if there is any _ReadDataFor, for which we don't want this function to exectue
            if (logMedia.Initialisation.DateTime > DateTime.Now)
            {
                _errorCurMedia = TTErrorTypes.TPERR_IssuanceReject;
            }
        }

        private void EF_CSCC_ControlTicketNotBlocked()
        {
            if (_errorCurMedia != TTErrorTypes.NoError)
                return;
            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            if (_readDataFor == MediaDetectionTreatment.TOM_SettleBadDebt)
            {
                if (!logMedia.Media.Blocked)
                _errorCurMedia = TTErrorTypes.OpSettleBadDebt_CardNotBlocked;
                return;
            }
            
            if (logMedia.Media.Blocked)
                _errorCurMedia = TTErrorTypes.MediaBlocked;            
        }

        private void CheckTokenIssueFeasibility()
        {
            if (_errorCurMedia != TTErrorTypes.NoError)
                return;

            if (_MediaDetectedState != SmartFunctions.MediaDetected.TOKEN)
                return;

            LogicalMedia logMedia = _logMediaToken;
            if (Config._bCheckForCurrentBusinessDaySoldToken)
            {
                Products ps = logMedia.Application.Products;
                TransportApplication ta = logMedia.Application.TransportApplication;
                DateTime dayOfSale = ps.Product(0).StartOfValidityRead;
                var tokenStatus = ta.StatusRead;

                if (dayOfSale == DatesUtility.BusinessDay(DateTime.Now, OverallParameters.EndBusinessDay)                    
                    && tokenStatus == TransportApplication.StatusValues.Issued)
                {
                    if (Config.Allow_UsableSJTSBeIssuedAgainOnSameBusinessDayAfter_10_Mins)
                        if (DateTime.Now - logMedia.Application.Validation.LastTransactionDateTimeRead > new TimeSpan(0, 10, 0))
                            return;
                    _errorCurMedia = TTErrorTypes.TokenAlreadyUsable;
                }
            }
        }

        private void EF_CheckOperationValidOnTheMediaType()
        {
            if (_errorCurMedia != TTErrorTypes.NoError)
                return;
            bool bOk = true;
            switch (_MediaDetectedState)
            {
                case SmartFunctions.MediaDetected.CARD:
                    {
                        switch (_readDataFor)
                        {
                            case MediaDetectionTreatment.AnalyisForTTagIssue:
                            case MediaDetectionTreatment.AnalyisForTTagUpdate:
                            case MediaDetectionTreatment.TOM_AnalysisForTokenIssue:
                                bOk = false;
                                break;
                        }
                        break;
                    }
                case SmartFunctions.MediaDetected.TOKEN:
                    {
                        switch (_readDataFor)
                        {
                            case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                            case MediaDetectionTreatment.TOM_AnalysisForCSCIssue:
                            case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia:
                            case MediaDetectionTreatment.TOM_AutoTopupActivation:
                            case MediaDetectionTreatment.TOM_AutoTopupDeActivation:
                            case MediaDetectionTreatment.TOM_AutoTopupPerform:
                                bOk = false;
                                break;
                        }
                        break;
                    }
            }
            if (!bOk)
                _errorCurMedia = TTErrorTypes.InvalidOperationForMediaType;
        }

        private void CheckForAdjustment()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) 
                return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            TTErrorCodeOnMedia rejectCode = (TTErrorCodeOnMedia)logMedia.Application.Validation.RejectCodeRead;
            if (rejectCode == TTErrorCodeOnMedia.NoError) 
                return;

            int? adjustmentAmt = null;
            int? entryExitBitPostAdjustment = null;
            byte? fareTierPostAdjustment = null;
            int? entryExitStationCodePostAdjustment = null;
            bool bCanBeAdjustableFromPurse = false;
            bool bUpdateDateTime = false;
            TTErrorCodeOnMedia rejectCodePostAdjustment = TTErrorCodeOnMedia.NoError;

            int fp = logMedia.Application.Products.Product(0).Type;
            int family = ProductParameters.GetProductFamily(fp);
            
            switch(family)
            {
                case 100:
                case 40:
                    break;
                case 80:
                    {
                        bCanBeAdjustableFromPurse = false;
                        switch (rejectCode)
                        {
                            case TTErrorCodeOnMedia.ExitNotDone:
                                {
                                    adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode);
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateExit;
                                    break;
                                }
                            case TTErrorCodeOnMedia.ExitMismatch:
                            case TTErrorCodeOnMedia.NoEntryFound:
                                {
                                    adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode);
                                    rejectCodePostAdjustment = TTErrorCodeOnMedia.RequiredExit;
                                    entryExitStationCodePostAdjustment = SharedData.StationNumber;
                                    entryExitBitPostAdjustment = (int)CONSTANT.MBC_GateEntry;
                                    bUpdateDateTime = true;

                                    break;
                                }
                            case TTErrorCodeOnMedia.ExcessTripTime:
                                {
                                    adjustmentAmt = 0;// CalculateAdjustmentChargesForExcessTripTime(logMedia, rejectCode, fp);
                                    bUpdateDateTime = true;
                                    break;
                                }
                        }
                        break;
                    }
                case 60:
                    {                        
                        switch (rejectCode)
                        {
                            case TTErrorCodeOnMedia.ExitNotDone:
                                {
                                    adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode);
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateExit;
                                    break;
                                }
                            case TTErrorCodeOnMedia.ExitMismatch:
                            case TTErrorCodeOnMedia.NoEntryFound:
                                {
                                    adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode);
                                    rejectCodePostAdjustment = TTErrorCodeOnMedia.RequiredExit;
                                    entryExitStationCodePostAdjustment = SharedData.StationNumber;
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateEntry;
                                    bUpdateDateTime = true;

                                    break;
                                }
                            case TTErrorCodeOnMedia.ExcessTripTime:
                                {
                                    adjustmentAmt = CalculateAdjustmentChargesForExcessTripTime(logMedia, rejectCode, fp);
                                    bUpdateDateTime = true;

                                    break;
                                }
                        }
                        if (adjustmentAmt != null)
                        {
                            bCanBeAdjustableFromPurse = ((int)adjustmentAmt <= logMedia.Purse.TPurse.BalanceRead);
                        }
                        break;
                    }
                case 10:
                    {
                        bCanBeAdjustableFromPurse = false;
                        bUpdateDateTime = true;

                        if (logMedia.Application.LocalLastAddValue.DateTime !=
                            DatesUtility.BusinessDay(DateTime.Now, businessdayChangeHour))
                        {
                            _errorCurMedia = TTErrorTypes.OpAdjustment_NotAdjustable_NotSameBusinessDay;
                            return;
                        }

                        switch(rejectCode)
                        {
                            case TTErrorCodeOnMedia.ExitMismatch:
                            case TTErrorCodeOnMedia.NoEntryFound:
                                {
                                    adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode);                                    
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateEntry;

                                    break;
                                }
                            case TTErrorCodeOnMedia.ExitNotDone:
                                {
                                    if (GetVirtualSiteId(logMedia.Application.LocalLastAddValue.Location) != GetVirtualSiteId(SharedData.StationNumber))
                                    {
                                        _errorCurMedia = TTErrorTypes.OpAdjustment_NotAdjustable_NotSameStation;
                                        return;
                                    }
                                    // ANUJ: Copied as it is from base code. But not sure that this error code should ever gets written on SJT. Remove it, if it is found to have bad impact.
                                    adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode);
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateExit;
                                    break;
                                }
                            case TTErrorCodeOnMedia.ExcessTripTime:
                                {
                                    adjustmentAmt = CalculateAdjustmentChargesForExcessTripTime(logMedia, rejectCode, fp);                                    
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateEntry;
                                    break;
                                }
                            case TTErrorCodeOnMedia.AmountTooLow:
                                {
                                    int fareTier;
                                    if (GetVirtualSiteId(logMedia.Application.Validation.LocationRead) == GetVirtualSiteId(SharedData.StationNumber))
                                    {
                                        adjustmentAmt = 0;
                                        //TODO: See if fareeTierPostAdjustment needs to be set or not.
                                    }
                                    else
                                    {
                                        adjustmentAmt = CommonRules.EF_EOD_GetAdjustmentCharge(fp, rejectCode) + EF_GetAdjustmentDifferenceForExcessDistance();
                                        SalePriceCalculation.CalculateTokenPriceSiteBased(logMedia.Application.Validation.LocationRead, SharedData.StationNumber, out fareTier);
                                        if (fareTier == -1)
                                        {
                                            _errorCurMedia = TTErrorTypes.FareTablesError;
                                            return;
                                        }

                                        if (fareTier > logMedia.Application.LocalLastAddValue.FareTiers)
                                        {
                                            fareTierPostAdjustment = (byte)fareTier;
                                        }
                                    }
                                    entryExitBitPostAdjustment = CONSTANT.MBC_GateEntry;
                                    break;
                                }
                        }
                        break;
                    }
                }
            if (adjustmentAmt == null)
            {
                _errorCurMedia = TTErrorTypes.OpAdjustment_NotAdjustable;
            }
            else
            {
                _adjustment = new AdjustmentInfo();
                _adjustment.Amount = Math.Min(Math.Max(0, (int)adjustmentAmt), Config.MaxPenaltyAmountInPaise);
                _adjustment._bCanUsePurse = bCanBeAdjustableFromPurse;
                _adjustment._entryExitBitPostAdjustment = entryExitBitPostAdjustment;
                _adjustment._entryExitStationCodePostAdjustment = entryExitStationCodePostAdjustment;
                _adjustment._RejectCodeOnMediaPostAdjustment = rejectCodePostAdjustment;
                _adjustment._bUpdateDateTimeOnAdjustment = bUpdateDateTime;
                _adjustment._fareTierPostAdjustment = fareTierPostAdjustment;
            }
        }

        private int CalculateAdjustmentChargesForExcessTripTime(LogicalMedia logMedia, TTErrorCodeOnMedia rejectCode, int fp)
        {
            TimeSpan durationSinceEntry = DateTime.Now - logMedia.Application.Validation.LastTransactionDateTimeRead;
            
            int entrySite;
            if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
                entrySite = logMedia.Application.LocalLastAddValue.LocationRead; // from where token is sold or adjusted
            else if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
                entrySite = logMedia.Application.Validation.LocationRead;
            else
            {
                Debug.Assert(false);
                return 0;
            }
            return CommonRules.CalculateAdjustmentChargesForExcessTripTime(rejectCode, fp, entrySite, durationSinceEntry);
        }

        void CheckforAutoTopupEnable()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            // TODO: How to find which products support auto-topup. e.g. we need to reject Tour_1/3 for it.
            bool bAutoTopupEnabled = (_logMediaReloader.Purse.AutoReload.StatusRead == AutoReload.StatusValues.Enabled);
            if (bAutoTopupEnabled)
            {
                _errorCurMedia = TTErrorTypes.OpEnableAutoTopup_CardAlreadyEnabledForAutoTopup;
            }
        }

        private void CheckCSCIsBankTopupEnabled()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            bool bAutoTopupEnabled = (_logMediaReloader.Purse.AutoReload.StatusRead == AutoReload.StatusValues.Enabled);
            if (!bAutoTopupEnabled)
            {
                _errorCurMedia = TTErrorTypes.OpOnBankTopupEnabledCSC_CSCNotEnabledForBankTopup;
            }
        }

        private void CSCRefund_SeeIfExitIsNotMade()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            bool bEntryPerformed = (_logMediaReloader.Application.Validation.EntryExitBitRead == Validation.TypeValues.Entry);
            if (bEntryPerformed)
            {
                _errorCurMedia = TTErrorTypes.OpRefund_CSC_ExitNotMade;
            }
        }

        private void CSCRefund_SeeIfAutoTopupIsEnabled()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
            {
                bool bAutoTopupEnabled = (_logMediaReloader.Purse.AutoReload.StatusRead == AutoReload.StatusValues.Enabled);
                if (bAutoTopupEnabled)
                {
                    _errorCurMedia = TTErrorTypes.OpRefund_CSC_AutoTopupEnabled;
                }
            }
        }

        private void CheckCSCIssueFeasiblity()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
            {
                if (_logMediaReloader.Media.OperationalTypeRead == MediaDescription.OperationalTypeValues.Agent)
                {
                    // Commenting not because it is bad code. Only because some the CSCs we've received have operational type as agent. Haven't written code to correct those cards.
                    _errorCurMedia = TTErrorTypes.InvalidOperationForProduct;
                    return;
                }
                else
                {
                    if (_logMediaReloader.Purse.TPurse.BalanceRead != 0)
                    {
                        // may be in such case one may suggest to be aggressive, and set the value to 0 (by appropriate credit/debit), but i prefer not to do so, and let the card unusable
                        _errorCurMedia = TTErrorTypes.OpCSCIssue_PurseValueNotZero;
                    }
                    // TODO: To be extra safe, may be we can comapre the initialization date with current date also.
                }
            }
        }

        private void SJTRefund_CheckTimeLimit()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
            {
                DateTime tokenSaleDate = _logMediaToken.Application.Validation.LastTransactionDateTime;//_logMediaToken.Application.LocalLastAddValue.DateTimeRead;
                DateTime now = DateTime.Now;//.ToUniversalTime();

                if ((now - tokenSaleDate) > TimeSpan.FromMinutes(Config._nTimeSpanInMinutesForAllowingSJTRefund))
                {
                    // TODO: May be there is a time limit between issuing the token and refunding. Couldn't locate it inside EOD.
                    _errorCurMedia = TTErrorTypes.OpRefund_SJT_TimeLimitToRefundLapsed;
                }
            }
        }
        
        private void EF_TOM_ControlTokenLifetime()
        {
            // Do nothing
            // Written to convey that the point of token expiry was considered and not found worthy to be included
        }

        private void SJTRefund_SaleStationIsSameAsThisOne()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
            {
                if (GetVirtualSiteId(_logMediaToken.Application.LocalLastAddValue.LocationRead) != GetVirtualSiteId(SharedData.StationNumber))
                {                    
                    _errorCurMedia = TTErrorTypes.OpRefund_SJT_NotSameStation;
                }
            }
        }

        private bool bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess;
        private void SJTRefund_CheckTokenHasNotBeenUsed()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
            {
                if (_logMediaToken.Application.Validation.EntryExitBitRead == Validation.TypeValues.Entry)
                {
                    if (_eqptStatus.FareMode == FareMode_Incident)
                    {
                        bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess = true;
                        return;
                    }
                    _errorCurMedia = TTErrorTypes.OpRefund_SJT_TokenAlreadyUsed;
                }
            }
        }

        private void CheckOperationIsOpenForFareProduct()
        {
 	        if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_readDataFor == MediaDetectionTreatment.BasicAnalysis_TOM
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForTokenIssue
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForCSCIssue
                || _readDataFor == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagIssue
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagUpdate)
                return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            int fp = logMedia.Application.Products.Product(0).Type;
            //TicketsSaleParameters.OneProductSpecs
              var  specs = SharedData._fpSpecsRepository.GetSpecsFor(fp);
            switch(_readDataFor)
            {
                case MediaDetectionTreatment.TOM_AnalysisForRefund:
                    {
                        if (!specs._RefundAuthorized)
                        {
                            _errorCurMedia = TTErrorTypes.OpRefund_ProductTypesNotRefundable;
                            return;
                        }
                        break;
                    }
                default:
                    {
                        if (!specs._IsOpen)
                        {
                            _errorCurMedia = TTErrorTypes.ProductOnCardNotOpen;
                            return;
                        }
                        break;
                    }
            }
        }

        private void CheckAddValFeasibility()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            
            int purseBalance = logMedia.Purse.TPurse.BalanceRead;
            int fp = logMedia.Application.Products.Product(0).Type;
                                        
            List<int> addValuesFeasible = SharedData._fpSpecsRepository.AddValuesFeasible(fp, purseBalance);
            if (addValuesFeasible.Count == 0)
                _errorCurMedia = TTErrorTypes.OpAddValue_NoMoreTopupFeasible;
            else
            {
                if (_readDataForAddVal_RechargeValueRequested != null && !addValuesFeasible.Contains((int)_readDataForAddVal_RechargeValueRequested))
                {
                    if ((int)_readDataForAddVal_RechargeValueRequested + purseBalance > SharedData._fpSpecsRepository.GetSpecsFor(fp)._MaximumStoredValue)
                        _errorCurMedia = TTErrorTypes.TooMuchTopupAmountAsked;
                    else
                        _errorCurMedia = TTErrorTypes.OpAddValue_InvalidRechargeValueAsked;
                }
            }
        }
                                
        private void EF_CheckForRejectCode()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_readDataFor == MediaDetectionTreatment.BasicAnalysis_TOM
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForTokenIssue
                || (_readDataFor == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia)
                ) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            int rejectCode = logMedia.Application.Validation.RejectCodeRead;
            switch (_readDataFor)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                case MediaDetectionTreatment.TOM_AutoTopupPerform:
                    {
                        if (rejectCode == 0)
                            break;
                        else if (rejectCode == (int)TTErrorCodeOnMedia.AmountTooLow)
                            break;
                        else if (rejectCode == (int)TTErrorCodeOnMedia.RequiredExit)
                        {
                            // We want to minimize the cases where media's state changes
                            int fp = logMedia.Application.Products.Product(0).Type;
                            int family = ProductParameters.GetProductFamily(fp);
                            int purseVal = logMedia.Purse.TPurse.BalanceRead;

                            if (family == 60 && purseVal < FareParameters.ShortReturnTripFare)
                                break;
                        }
                        else
                            _errorCurMedia = TTErrorTypes.NeedsAdjustment;
                        break;
                    }
                case MediaDetectionTreatment.TOM_AnalysisForRefund: // IERS debars refund only for four scenarios:TPERR_ExitNotDone/TPERR_ExitMismatch/TPERR_NoEntryFound/TPERR_ExcessTripTime:
                case MediaDetectionTreatment.TOM_AutoTopupActivation:
                case MediaDetectionTreatment.TOM_AutoTopupDeActivation:                
                case MediaDetectionTreatment.TOM_CSCSurrender:
                    {
                        if (rejectCode != 0)
                        {
                            _errorCurMedia = TTErrorTypes.NeedsAdjustment;
                        }
                        break;
                    }
                //case MediaDetectionTreatment.TOM_DetailedAnalyis:
                //    {
                //        // It is implied that we need to make analysis for adjustment here itself
                //        CheckIsAdjustableOnBasisOfErrorCodeInIt();
                //        break;
                //    }
            }
        }

        private void CheckForBlackListing()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;

            if (_readDataFor == MediaDetectionTreatment.TOM_SettleBadDebt)
                return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            _errorCurMedia = CommonRules.CheckMediaDataForBlockingBlackListingEtc(logMedia);
        }

        private void CheckMediaExpiry()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
            {
                if (Config._bCheckForMediaExpiry)
                {
                    if (DateTime.Now > logMedia.Media.ExpiryDate)                    
                        _errorCurMedia = TTErrorTypes.MediaEndOfValidityReached;
                }
            }
        }

        private void ReadMediaAsPerRequirements()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            
            bool bReadMediaDataResult;
            CommonHwMedia hw;
            if (_MediaDetectedState == SmartFunctions.MediaDetected.CARD)
                hw = hwCsc;
            else if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
            {
                hw = hwToken;
                #if !_HHD_
                hwToken.SetRWHandle(_hRw);
#endif
            }
            else
                throw new NotImplementedException();

            
            bReadMediaDataResult = hw.ReadMediaData(logMedia, _readDataFor);

            if (!bReadMediaDataResult)
            {
                _bTicketDataStructuresSynchedWithTicketData = false;
                if (Config.IsCannotReadCardErrorCodeMoreGeneric)
                    _errorCurMedia = TTErrorTypes.CannotReadTheCard;
                else
                {
                    var lastStatus = hw.GetLastStatus();
                    if (lastStatus == CommonHwMedia.Status.Failed_MediaFailedToAuthenticate)
                        _errorCurMedia = TTErrorTypes.CannotReadTheCard_AuthenticationFailure;
                    else if (lastStatus == CommonHwMedia.Status.Failed_MediaWasNotInField)
                        _errorCurMedia = TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow;
                    // TODO: for ErrorNonBIM_MediaDoesntHasApplication
                    else
                        _errorCurMedia = TTErrorTypes.CannotReadTheCard;
                }
            }
            else
            {
                _bTicketDataStructuresSynchedWithTicketData = true;
                try
                {
                    if (_readDataFor == MediaDetectionTreatment.TOM_DetailedAnalyis
                    && logMedia.Media.HardwareType == Media.HardwareTypeValues.TokenUltralight
                        && logMedia.TTag.Hidden
                        && SharedData._fpSpecsRepository != null
                        && SharedData._fpSpecsRepository.Initialized
                        )
                    {
                        int product = logMedia.Application.Products.Product(0).Type;
                        int family = ProductParameters.GetProductFamily(product);
                        var specs = SharedData._fpSpecsRepository.GetSpecsFor(product);
                        if (family == 40)
                            logMedia.DelhiUltralightRaw.PriceAsPerEOD = specs._SalePrice.Val;
                        else if (family == 10)
                        {
                            int zone = logMedia.Application.LocalLastAddValue.FareTiers;
                            DateTime dtTimeOfIssuing = logMedia.Application.LocalLastAddValue.DateTime;
                            if (dtTimeOfIssuing.Hour == 0 && dtTimeOfIssuing.Minute == 0 && dtTimeOfIssuing.Second == 0)
                            {
                                // with current layout, we always reach here, because it doesn't save time component
                                dtTimeOfIssuing = dtTimeOfIssuing.AddHours(5);
                            }
                            logMedia.DelhiUltralightRaw.PriceAsPerEOD = SalePriceCalculation.CalculateTokenPriceZoneBased(zone, dtTimeOfIssuing);
                        }
                    }
                }
                catch { }
            }
        }

        private void CheckOpeartionalTypeForCSC()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;
            switch (_logMediaReloader.Media.OperationalType)
            {
                case MediaDescription.OperationalTypeValues.Agent:
                case MediaDescription.OperationalTypeValues.Passenger:
                    break;
                default:
                    {
                        _errorCurMedia = TTErrorTypes.BadCardData;                        
                        return;
                    }
            }

            if (_logMediaReloader.Media.OperationalType == MediaDescription.OperationalTypeValues.Agent)
            {
                switch (_readDataFor)
                {
                    case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                    case MediaDetectionTreatment.TOM_AutoTopupActivation:
                    case MediaDetectionTreatment.TOM_AutoTopupDeActivation:
                    case MediaDetectionTreatment.TOM_AutoTopupPerform:
                    case MediaDetectionTreatment.TOM_AnalysisForRefund:
                    case MediaDetectionTreatment.TOM_AnalysisForAdjustment:
                    case MediaDetectionTreatment.TOM_CSCSurrender:
                    case MediaDetectionTreatment.TOM_SettleBadDebt:
                    case MediaDetectionTreatment.AnalyisForTTagIssue:
                    case MediaDetectionTreatment.AnalyisForTTagUpdate:
                    case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia:
                        {
                            // Commenting not because it is bad code. Only because some the CSCs we've received have operational type as agent. Haven't written code to correct those cards.
                            _errorCurMedia = TTErrorTypes.InvalidOperationForProduct;
                            return;
                        }
                }
            }
        }

        private void CheckOnRead_TestFlag()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (_readDataFor == MediaDetectionTreatment.BasicAnalysis_TOM
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForTokenIssue
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagIssue
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagUpdate)
                return;

            if (logMedia.Media.Test)
            {
                if (SharedData._agentShift.Profile == AgentProfile.Maintenance || SharedData._agentShift.Profile == AgentProfile.MaintenanceSupervisor)
                    return;
                else
                    _errorCurMedia = TTErrorTypes.IncompatibleValueOfTestFlag;
            }
            else
            {
                if (SharedData._agentShift.Profile != AgentProfile.Maintenance && SharedData._agentShift.Profile != AgentProfile.MaintenanceSupervisor)
                    return;
                else
                    _errorCurMedia = TTErrorTypes.IncompatibleValueOfTestFlag;
            }
        }


        // TODO: See if we need to put check that the ticket is open or not. May be for not-open tickets, we can allow for refund
        private void CheckForValidTicketType()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_readDataFor == MediaDetectionTreatment.TOM_AnalysisForTokenIssue
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForCSCIssue
                || _readDataFor == MediaDetectionTreatment.BasicAnalysis_TOM
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagIssue
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagUpdate
                || _readDataFor == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia)
                return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            int product = logMedia.Application.Products.Product(0).TypeRead;
            if (product == 0)
            {
                _errorCurMedia = TTErrorTypes.NoProduct;
                return;
            }

            int family = ProductParameters.GetProductFamily(product);
            if (family == 20)
            {
                _errorCurMedia = TTErrorTypes.NotSupportedProductFamily;
                return;
            }

            var specs = SharedData._fpSpecsRepository.GetSpecsFor(logMedia.Application.Products.Product(0).TypeRead);
            if (specs == null)
            {
                _errorCurMedia = TTErrorTypes.FareTablesError;
                return;
            }

            bool bIsOpen = specs._IsOpen;
            if (!bIsOpen)
            {
                if (_readDataFor == MediaDetectionTreatment.TOM_AnalysisForAddVal
                    || _readDataFor == MediaDetectionTreatment.TOM_DetailedAnalyis
                    || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForCSCIssue
                    || _readDataFor == MediaDetectionTreatment.TOM_AutoTopupActivation
                    || _readDataFor == MediaDetectionTreatment.TOM_AutoTopupDeActivation
                    || _readDataFor == MediaDetectionTreatment.TOM_AutoTopupPerform
                    || _readDataFor == MediaDetectionTreatment.TOM_CSCSurrender
                    || _readDataFor == MediaDetectionTreatment.TOM_SettleBadDebt
                    )
                {
                    _errorCurMedia = TTErrorTypes.ProductOnCardNotOpen;
                }
                else if (_readDataFor == MediaDetectionTreatment.TOM_AnalysisForRefund)
                {
                    // Don't do
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        bool IsTokenNeverUsed()
        {           
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            Debug.Assert(_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN);
            if (_MediaDetectedState != SmartFunctions.MediaDetected.TOKEN)
                return false;
            if (logMedia.Media.Status == Media.StatusValues.Issued
                && logMedia.Application.Validation.EntryExitBitRead == Validation.TypeValues.Exit
                )
                return true;            
            else
                return false;
        }

        void EF_Token_VerifyMac()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (_MediaDetectedState != SmartFunctions.MediaDetected.TOKEN)
                return;

            switch (_readDataFor)
            {
                case MediaDetectionTreatment.TOM_DetailedAnalyis:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment:
                case MediaDetectionTreatment.TOM_AnalysisForRefund:                
                    {
                        if (!TokenFunctions.VerifyMAC(logMedia))
                            _errorCurMedia = TTErrorTypes.MACError;
                        break;
                    }
            }
        }

        void EF_TOM_ControlMediaIsRefundableOnBasisOfStatusAndRejectCode()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            Debug.Assert(_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN);

            if (logMedia.Media.StatusRead != Media.StatusValues.Issued)
            {
                _errorCurMedia = TTErrorTypes.MediaNotSold;
                return;
            }
        }

        private void EF_CheckOperationValidOnTheFareProduct()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_readDataFor == MediaDetectionTreatment.BasicAnalysis_TOM
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForTokenIssue
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForCSCIssue
                || _readDataFor == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagUpdate
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagIssue) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();
            int fp = logMedia.Application.Products.Product(0).Type;
            int family = ProductParameters.GetProductFamily(fp);
            bool bOk = true;
            switch(_readDataFor)
            {
                case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                    {
                        if (family != 60)
                            bOk = false;
                        break;
                    }
                case MediaDetectionTreatment.TOM_AnalysisForRefund:
                    {
                        // TODO: May be this check is not even required, because we already have refundAuthorized for each fare product. If this is the case, simply remove this redundant check to avoid any unnecessary issues.
                        if (family != 10 && family != 60 && family != 80) // TODO: See, if we want family 100 to be included or not.
                            bOk = false;
                        break;
                    }
                case MediaDetectionTreatment.TOM_AutoTopupActivation:
                case MediaDetectionTreatment.TOM_AutoTopupDeActivation:
                case MediaDetectionTreatment.TOM_AutoTopupPerform:
                case MediaDetectionTreatment.TOM_SettleBadDebt:
                // TODO: I think tourist card can be surrendered. but not sure. make corrections accordingly
                    {
                        if (family != 60)
                            bOk = false;
                        break;
                    }
            }
            if (!bOk)
                _errorCurMedia = TTErrorTypes.InvalidOperationForProduct;
        }

        private void EF_Token_ControlCardType()
        {
            // TODO
            // Check that if it is a Card, then its a DesfireCard
            // if it is a Token, then it is Mifare Ultralight
            // Also modify DetectTypeOfMediaNExtractSerialNumbers to for unsupported types
        }

        private void EF_FareProductResidingInCompatibleCardType()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;

            if (_readDataFor == MediaDetectionTreatment.BasicAnalysis_TOM
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForTokenIssue
                || _readDataFor == MediaDetectionTreatment.TOM_AnalysisForCSCIssue
                || _readDataFor == MediaDetectionTreatment.TOM_PutNewProductInExistingMedia
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagIssue
                || _readDataFor == MediaDetectionTreatment.AnalyisForTTagUpdate
                ) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (logMedia.Media.OperationalType == MediaDescription.OperationalTypeValues.Agent)
                return;

            int fp = logMedia.Application.Products.Product(0).Type;
            if (fp == 0)
            {
                _errorCurMedia = TTErrorTypes.NoProduct;
                return;
            }

            int family = ProductParameters.GetProductFamily(fp);
            if (family == -1)
            {
                _errorCurMedia = TTErrorTypes.FareTablesError;
                return;
            }
            bool bOk = true;
            // TODO: It should have been extracted from EOD, but I can't locate where the mapping between family and card type is provided.
            switch (family)
            {
                case 10:
                case 20:
                case 40:
                    if (_MediaDetectedState != SmartFunctions.MediaDetected.TOKEN)
                        bOk = false;
                    break;                    
                case 60:
                case 80:
                case 100:
                    if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                        bOk = false;
                    break;
            }
            if (!bOk)
            {
                _errorCurMedia = TTErrorTypes.InvalidProductForMediaType;
                return;
            }
        }

        private readonly DateTime businessdayChangeHour = new DateTime(2010, 1, 1, 2, 0, 0);
        // TODO: ANUJ: What I remember is that there is a time interval limit for token refund. See, if it is correct, or is it that it can be refunded in that business day
        private void EF_TOM_ControlIsTokenRefundableOrAdjustableOnBasisOfIssueDate()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            Debug.Assert(_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN);

            var lcav = logMedia.Application.LocalLastAddValue;
            var saleDate = lcav.DateTimeRead;
            if (saleDate != DatesUtility.BusinessDay(DateTime.Now,
                businessdayChangeHour // TODO: Keep it as configurable
                ))
            {
                _errorCurMedia = TTErrorTypes.BadDate;
                return;
            }
            if (logMedia.Application.Validation.LastTransactionDateTimeRead > DateTime.Now)
            {
                _errorCurMedia = TTErrorTypes.BadDate;
                return;
            }
        }

        Dictionary<TTErrorTypes, CSC_CCHS_RejectReason> _ttErrorCodesVsCCHSRejectCode = new Dictionary<TTErrorTypes, CSC_CCHS_RejectReason> { 
            {TTErrorTypes.MediaEndOfValidityReached, CSC_CCHS_RejectReason.TickedExpired},
            {TTErrorTypes.OpCSCIssue_MediaNotInitializedAfterRefund, CSC_CCHS_RejectReason.CSCTicketNotActive},
            {TTErrorTypes.NotDMRCCard, CSC_CCHS_RejectReason.IssureNotSupported},
            {TTErrorTypes.LastAddValueDeviceBlacklisted, CSC_CCHS_RejectReason.LastAddValueDeviceBlacklisted},
//            {TTErrorTypes.CardNotIssued, CSC_CCHS_RejectReason.CSCTicketNotActive}
        };

        private void ChecksOnReadCSC_CheckForMediaStatus()
        {
            if (_errorCurMedia != TTErrorTypes.NoError) return;
            
            LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

            if (_MediaDetectedState != SmartFunctions.MediaDetected.CARD)
                return;

            // Actaully both enums, Media.StatusValues and TransportApplication.StatusValues hold same enum values.
            var mediaStatus = logMedia.Media.StatusRead;


            //Vibhu: Checking CSC Status in Media.StatusValues and TransportApplication.StatusValues for AddValue
            var transportApplicationStatus = logMedia.Application.TransportApplication.StatusRead;

            if (Config.bCheckCSCStatusInDM2ForAddValue)
            {
                if ((_readDataFor == MediaDetectionTreatment.TOM_AnalysisForAddVal) && ((int)mediaStatus != (int)transportApplicationStatus))
                {
                    _errorCurMedia = TTErrorTypes.StatusError;
                    return;
                }
            }

            ////ANUJ: Copying this logic from IERS. Since it is relativly recent, assuming that it is good.
            //// TODO: Get it reviewed.
            //Media.StatusValues status;
            //if (mediaStatus == Media.StatusValues.NotInitialised)
            //    status = (Media.StatusValues)((int)transportApplicationStatus);
            //else
            //    status = mediaStatus;
            
            switch(_readDataFor)
            {
                case MediaDetectionTreatment.BasicAnalysis_TOM:
                    return;                
                case MediaDetectionTreatment.TOM_AnalysisForAddVal:
                case MediaDetectionTreatment.TOM_AutoTopupDeActivation:
                case MediaDetectionTreatment.TOM_AutoTopupActivation:
                case MediaDetectionTreatment.TOM_AutoTopupPerform:
                case MediaDetectionTreatment.TOM_AnalysisForAdjustment:
                case MediaDetectionTreatment.TOM_SettleBadDebt:
                case MediaDetectionTreatment.TOM_PutNewProductInExistingMedia:
                    {
                        switch (mediaStatus)
                        {
                            case Media.StatusValues.Issued:
                                break;
                            case Media.StatusValues.Initialised:
                                _errorCurMedia = TTErrorTypes.MediaNotIssued;
                                break;
                            case Media.StatusValues.Refunded:
                                _errorCurMedia = TTErrorTypes.MediaIsInRefundedState;
                                break;
                            case Media.StatusValues.Surrendered:
                                _errorCurMedia = TTErrorTypes.MediaIsInSurrenderedState;
                                break;
                            case Media.StatusValues.Expired:
                                _errorCurMedia = TTErrorTypes.MediaHasGotExpired;
                                break;
                            default:
                                _errorCurMedia = TTErrorTypes.StatusError;
                                break;
                        }
                        break;
                    }
                case MediaDetectionTreatment.TOM_CSCSurrender:
                    {
                        switch (mediaStatus)
                        {
                            case Media.StatusValues.Surrendered:
                                _errorCurMedia = TTErrorTypes.MediaAlreadySurrendered;
                                break;
                            case Media.StatusValues.Issued:
                                break;
                            case Media.StatusValues.Initialised:
                                _errorCurMedia = TTErrorTypes.MediaNotIssued;
                                break;
                            case Media.StatusValues.Refunded:
                                _errorCurMedia = TTErrorTypes.MediaIsInRefundedState;
                                break;
                            case Media.StatusValues.Expired:
                                _errorCurMedia = TTErrorTypes.MediaHasGotExpired;
                                break;
                            default:
                                _errorCurMedia = TTErrorTypes.StatusError;
                                break;
                        }
                        break;
                    }
                case MediaDetectionTreatment.TOM_AnalysisForRefund:
                    {
                        switch (mediaStatus)
                        {
                            case Media.StatusValues.Issued:
                                break;
                            case Media.StatusValues.Refunded:
                                _errorCurMedia = TTErrorTypes.MediaAlreadyRefunded;
                                break;
                            case Media.StatusValues.Initialised:
                                _errorCurMedia = TTErrorTypes.MediaNotIssued;
                                break;
                            case Media.StatusValues.Surrendered:
                                _errorCurMedia = TTErrorTypes.MediaIsInSurrenderedState;
                                break;
                            case Media.StatusValues.Expired:
                                _errorCurMedia = TTErrorTypes.MediaHasGotExpired;
                                break;
                            default:
                                _errorCurMedia = TTErrorTypes.StatusError;
                                break;
                        }
                        break;
                    }                
                case MediaDetectionTreatment.TOM_DetailedAnalyis: // for detailed analysis, generally the next logical step after detailed analysis is Adjustement. So, this code is kept keeping that in mind. But, purpose may actually be to verify the incoming stock. So, it MMI has the option to treat this code in mild manner, by simply showing error in a label, instead of popping a message box.
                    {
                        switch(mediaStatus)
                        {
                            case Media.StatusValues.Issued:
                                break;
                            case Media.StatusValues.Surrendered:
                                _errorCurMedia = TTErrorTypes.MediaIsInSurrenderedState;
                                break;
                            case Media.StatusValues.Expired:
                                _errorCurMedia = TTErrorTypes.MediaHasGotExpired;
                                break;
                            case Media.StatusValues.Refunded:
                                _errorCurMedia = TTErrorTypes.MediaIsInRefundedState;
                                break;
                            case Media.StatusValues.Initialised:
                                _errorCurMedia = TTErrorTypes.MediaNotIssued;
                                break;
                            default:
                                _errorCurMedia = TTErrorTypes.MediaNotIssued;
                                break;                            
                        }
                        return;
                    }
                case MediaDetectionTreatment.TOM_AnalysisForCSCIssue:
                    {
                        switch (mediaStatus)
                        {
                            case Media.StatusValues.Initialised:
                                return;
                            case Media.StatusValues.Surrendered:
                                _errorCurMedia = TTErrorTypes.MediaIsInSurrenderedState;
                                break;
                            case Media.StatusValues.Issued:
                                _errorCurMedia = TTErrorTypes.OpCSCIssue_MediaAlreadyIssued;
                                break;
                            case Media.StatusValues.Refunded:
                                if (Config.ALLOW_REFUNDED_CSC_BEISSUED)
                                    return;
                                _errorCurMedia = TTErrorTypes.OpCSCIssue_MediaNotInitializedAfterRefund;
                                break;
                            default:
                                _errorCurMedia = TTErrorTypes.MediaNotInitialised;
                                break;
                        }
                        break;
                    }
            }
        }

    private void ChecksOnReadToken_CheckForMediaStatus()
    {
        if (_errorCurMedia != TTErrorTypes.NoError) return;
        
        LogicalMedia logMedia = GetLogicalDataOfMediaAtFront();

        if (_MediaDetectedState != SmartFunctions.MediaDetected.TOKEN)
            return;

        var transportAppStatus = logMedia.Application.TransportApplication.StatusRead;

        switch(_readDataFor)
        {
            case MediaDetectionTreatment.BasicAnalysis_TOM:
                return;
            case MediaDetectionTreatment.TOM_AnalysisForRefund:
            case MediaDetectionTreatment.TOM_DetailedAnalyis: // for detailed analysis, generally the next logical step after detailed analysis is Adjustement. So, this code is kept keeping that in mind. But, purpose may actually be to verify the incoming stock. So, it MMI has the option to treat this code in mild manner, by simply showing error in a label, instead of popping a message box.
                {
                    if (transportAppStatus != TransportApplication.StatusValues.Issued)
                    {
                        _errorCurMedia = TTErrorTypes.TokenNotIssued;                            
                    }
                    break;
                }
            case MediaDetectionTreatment.TOM_AnalysisForTokenIssue:
                {
                    if (Config._bCheckForCurrentBusinessDaySoldToken)
                    {
                        Products ps = logMedia.Application.Products;
                        TransportApplication ta = logMedia.Application.TransportApplication;
                        DateTime dayOfSale = ps.Product(0).StartOfValidityRead;
                        var tokenStatus = ta.StatusRead;

                        if (dayOfSale == DatesUtility.BusinessDay(DateTime.Now, OverallParameters.EndBusinessDay)                            
                            && tokenStatus == TransportApplication.StatusValues.Issued)
                        {
                            if (Config.Allow_UsableSJTSBeIssuedAgainOnSameBusinessDayAfter_10_Mins)
                                if (DateTime.Now - logMedia.Application.Validation.LastTransactionDateTimeRead > new TimeSpan(0, 10, 0))
                                    return;
                            _errorCurMedia = TTErrorTypes.TokenAlreadyUsable;
                        }
                    }
                    break;
                }
        }
    }
        
        private TTErrorTypes TreatCardSummary(Int64 serialNumber)
        {
            try
            {
                // Check the card serial number
                if (Convert.ToInt64(serialNumber) != _logMediaReloader.Media.ChipSerialNumber)
                {
                    Communication.SendMessage(ThreadName, "Answer", "ReadUserCardSummaryAnswer", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                    Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
//                    return false;
                    return TTErrorTypes.NotSameCard;
                }
                bool ok;                
                ok = hwCsc.ReadTPurseData(_logMediaReloader, _readDataFor);
                if (ok) ok = hwCsc.ReadValidationData(_logMediaReloader, _readDataFor);
                if (ok) ok = hwCsc.ReadApplicationData(_logMediaReloader, _readDataFor);
                if (!ok)
                {
                    // TODO: more keenly observe. also see how it is different from BadCardDetection
                    Communication.SendMessage(ThreadName, "Answer", "ReadUserCardSummaryAnswer", Convert.ToString((int)TTErrorTypes.CannotReadTheCard), "");
                    Logging.Log(LogLevel.Error, ThreadName + "_Cannot Read Card");
//                    return true;
                    return TTErrorTypes.CannotReadTheCard;
                }
                
                TTErrorTypes err = CommonRules.IsFareProductOpen(_logMediaReloader);

                //DEBUG JL A ENLEVER
                //err = TTErrorTypes.NoError;
                //_logMediaReloader.Application.Products.Product(0).Type = 6;
                //A ENLEVER

                if (err != TTErrorTypes.NoError)
                {
                    Communication.SendMessage(ThreadName, "Answer", "ReadUserCardSummaryAnswer", Convert.ToString((int)err), "");
                    Logging.Log(LogLevel.Error, ThreadName + "No TPurse Product On The card");
//               
                    return err; // can be one of TTErrorTypes.NoProduct and TTErrorTypes.Exception
                }
                string s = _logMediaReloader.ToXMLString();
                Communication.SendMessage(ThreadName, "Answer", "ReadUserCardSummaryAnswer", "0", s);
                return TTErrorTypes.NoError;
            }
            catch (Exception e1)
            {
                Communication.SendMessage(ThreadName, "Answer", "ReadUserCardSummaryAnswer", Convert.ToString((int)TTErrorTypes.CannotReadTheCard), "");
                Logging.Log(LogLevel.Error, ThreadName + "_ReadUserCardSummary Error :" + e1.Message);
            }
            return TTErrorTypes.NoError;
        }

        static public readonly int _nProductValidityYears;
        static MainTicketingRules()
        {
            _nProductValidityYears = (int)Configuration.ReadParameter("BTypeValidityInYears", "int", "10");
        }        

        public bool TreatCSCMessageReceived(EventMessage eventMessage)
        {            
            switch (eventMessage.EventID.ToUpper())
            {
                case "RESETCARDDETECTION" :
                    SmartFunctions.Instance.Init(true);
                    _logMediaReloader.Reset();
                    hwCsc.Reset();
                    return true;
                case "READUSERCARDSUMMARY":
                    return (TreatCardSummary(Convert.ToInt64(eventMessage.Attribute)) == TTErrorTypes.NoError);
                case "RELOADTPURSEONCARD":
                    try
                    {
                        if (_MediaDetectedState == SmartFunctions.MediaDetected.NONE)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.MediaNotPresent), "");
                            Logging.Log(LogLevel.Error, ThreadName + "Card not present");
                            return true;
                        }
                        SmartFunctions.Instance.SwitchToCardOnState();
                        
                        long cardSerNbr = Convert.ToInt64(eventMessage.Attribute);
                        if (Convert.ToInt64(eventMessage.Attribute) != _logMediaReloader.Media.ChipSerialNumber)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
                            return true;
                        }

                        var msg_splitted = eventMessage.Message.Split(';');
                        int pValue = Convert.ToInt32(msg_splitted[0]);

                        string paymentMethodParam;
                        if (msg_splitted.Length > 1)
                            paymentMethodParam = msg_splitted[1];
                        else
                            paymentMethodParam = eventMessage._par[2];

                        PaymentMethods paymentType;
                        if (paymentMethodParam == "1")
                            paymentType = PaymentMethods.Cash;
                        else if (paymentMethodParam == "2")
                            paymentType = PaymentMethods.BankCard;
                        else
                        {
                            Debug.Assert(false);
                            paymentType = PaymentMethods.Unknown;
                        }

                        //Update Values of tPurse
                        SalesRules.AddValueUpdate(_logMediaReloader, pValue, paymentType//, false
                            );
                        //CommonRules.SetupMediaEndOfValidity(_logMediaReloader, EndOfValidityMode.CurrentPlusYears, DateTime.Now,1);
                        //CommonRules.SetupTransportApplicationEndOfValidity(_logMediaReloader, EndOfValidityMode.CurrentPlusYears, DateTime.Now,1);
                       //SharedData.CSC_oldEndOfValidityDate = _logMediaReloader.Application.Products.Product(0).EndOfValidity ; // SKS: to put count into CCHS Txn TPURSE Header
                        CommonRules.SetupProductEndOfValidity(_logMediaReloader, 
                            EndOfValidityMode.CurrentPlusYears,                             
                            DateTime.Now, 
                            _nProductValidityYears //ANUJ: To be removed later when we adopt TOM's Sale Definition list.
                            );

                        if (hwCsc.UpdateTPurseData(_logMediaReloader, pValue, true))
                        {
                            Logging.Log(LogLevel.Verbose, "UpdateTPurseData Done with Success total Purse Val= " + _logMediaReloader.Purse.TPurse.Balance);
                            if (hwCsc.WriteLocalSaleData(_logMediaReloader, true)) // SKS: 09-10-2014 As updated TPurse is done with sucesses, so we are lest bothered about local sale data success stauts 
                            {
                                Logging.Log(LogLevel.Verbose, "WriteLocalSaleData Done Successfully");
                            }
                            else
                            {
                                Logging.Log(LogLevel.Error, "WriteLocalSaleData Done Failed");
                            }
                            //In the doubt as we have written card. better not to answer that we cannot write card.  
                            try
                            {
                                ///TODO: increase Sequence Number
                                ///Generate TXN Data
                                ///sign TXN Data record and send to MMI
                                SharedData.TransactionSeqNo++;

                                string xmlStr = "";
                                if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                                {
                                    TransactionType txnType;
                                    if (paymentType == PaymentMethods.Cash)
                                    {
                                        txnType = TransactionType.TPurseWebTopupReload;
                                        SmartFunctions.Instance.GetTDforCCHS(_logMediaReloader, txnType, SharedData.TransactionSeqNo, pValue, out xmlStr);
                                    }
                                    else if (paymentType == PaymentMethods.BankCard)
                                    {
                                        txnType = TransactionType.TXN_CSC_ADD_VALUE_EFT;
                                        IFS2.Equipment.Common.CCHS.BankTopupDetails bankTopupDetails = SerializeHelper<IFS2.Equipment.Common.CCHS.BankTopupDetails>.XMLDeserialize(eventMessage._par[3]);
                                        SmartFunctions.Instance.GetTDforCCHSForEFTAddVal(_logMediaReloader, bankTopupDetails, SharedData.TransactionSeqNo, pValue, out xmlStr);
                                    }
                                    else
                                    {
                                        //Bad because nothing is sent back so I remove return true;
                                        //return true;
                                    }
                                }
                                else
                                    SharedData.SaveContextFile();//save txn sequence No 
                                _logMediaReloader.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;

                                //	UpdateCardStatusOnCC 
                                // Communication.SendMessage(ThreadName, "Message", "UpdateCardStatusOnCC", "0", xmlStr);
                                if (xmlStr != "" && _signatureAtEachTransaction)
                                    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString() + "<XdrData>" + xmlStr + "</XdrData>");
                                else
                                    //Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", "");
                                    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString());
                                //Restart polling only in case of
                                // SmartFunctions.Instance.Init();
                                //  SmartFunctions.Instance.HaltCard();//SKS:Commented on 2014-12-17 
                                //  SmartFunctions.Instance.StartPolling();//SKS:Commented on 2014-12-17 
                                SmartFunctions.Instance.SwitchToDetectRemovalState();//SKS:Added on 2014-12-17 
                                return true;
                            }
                            catch (Exception e1)
                            {
                                Logging.Log(LogLevel.Error, "CSCFunctions.TreatMessageReceived.ReloadTPurseOnCard.ErrorAfterCardWrite " + e1.Message);
                                Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString());
                                return true;
                            }
                            
                        }
                        Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.CardCannotBeWritten), "");
                        Logging.Log(LogLevel.Error, ThreadName + "_ReloadTPurseOnCard Last Add Value data not loaded");
                        _logMediaReloader.Reset();
                        hwCsc.Reset();
                    }
                    catch (Exception e1)
                    {
                        Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.CardCannotBeWritten), "");
                        Logging.Log(LogLevel.Error, ThreadName + "_ReloadTPurseOnCard Error :" + e1.Message);
                        _logMediaReloader.Reset();
                        hwCsc.Reset();
                    }
                    return false;
                case "PAYMENTWITHTPURSE":// amount;
                    try
                    {
                        if (_MediaDetectedState == SmartFunctions.MediaDetected.NONE)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.MediaNotPresent), "");
                            Logging.Log(LogLevel.Error, ThreadName + "Card not present");
                            return true;
                        }
                        SmartFunctions.Instance.SwitchToCardOnState();

                        long cardSerNbr = Convert.ToInt64(eventMessage.Attribute);
                        if (Convert.ToInt64(eventMessage.Attribute) != _logMediaReloader.Media.ChipSerialNumber)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
                            break;
                        }

                        var msg_splitted = eventMessage.Message.Split(';');
                        int pValue = Convert.ToInt32(msg_splitted[0]);
                        int product = Convert.ToInt32(msg_splitted[1]);
                       // int opType = Convert.ToInt32(msg_splitted[2]);
                        //Update Values of tPurse
                        SalesRules.PurseDeductionUpdate(_logMediaReloader, pValue, PaymentMethods.StoreValue);
                        OperationTypeValues ridetype = OperationTypeValues.BuyTicketFromEpurse;
                        SalesRules.AddTrasactionHistoryRecord(_logMediaReloader, ridetype, pValue);
                        //SalesRules.AddTrasactionHistoryRecord(_logMediaReloader, /*OperationTypeValues.Penalty*/ (OperationTypeValues)opType, pValue, true);
                        //SharedData.CSC_oldEndOfValidityDate = _logMediaReloader.Application.Products.Product(0).EndOfValidity; // SKS: to put count into CCHS Txn TPURSE Header
                        if (hwCsc.UpdateTPurseData(_logMediaReloader, -pValue, false)
                                && hwCsc.AppendCommonAreaPurseHistoryRecord(_logMediaReloader)
                                && hwCsc._CommitModifications())
                        //if (hwCsc.UpdateTPurseData(_logMediaReloader, -pValue, true)) //&& (hwCsc.AppendCommonAreaPurseHistoryRecord(_logMediaReloader)))
                        {                            
                            FldsCSCPurseDeduction pars = new FldsCSCPurseDeduction();
                            pars.purseRemainingVal = _logMediaReloader.Purse.TPurse.Balance;
                            pars.transactionValue = pValue;
                            pars.receiptNumber = (msg_splitted.Length > 2) ? msg_splitted[2] : "";
                            pars.purchaseCategory = product;
                            string ret = SmartFunctions.Instance.GetTDforCCHSGen(_logMediaReloader,TransactionType.TPurseDeduction,pars,false,_logMediaReloader.Application.TransportApplication.Test);
                            if  (_signatureAtEachTransaction)
                                Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString() + "<XdrData>" + ret + "</XdrData>");
                            else
                                Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString());

                            Logging.Log(LogLevel.Verbose, "PaymentwithtPurse Done with Success total Purse Val= " + _logMediaReloader.Purse.TPurse.Balance);
                            //We can try to generate transaction
                            return true;
                        }
                        //if (hwCsc._CommitModifications())
                        //{
                        //    return true;
                           
                        //}                       
                        Logging.Log(LogLevel.Error, ThreadName + "PAYMENTWITHTPURSE Failed");
                        _logMediaReloader.Reset();
                        hwCsc.Reset();

                    }
                    catch (Exception Ex)
                    {
                        Logging.Log(LogLevel.Error, ThreadName + "PAYMENTWITHTPURSE Error :" + Ex.Message);
                        _logMediaReloader.Reset();
                        hwCsc.Reset();
                    }
                    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.CardCannotBeWritten), "");
                    return false;

                case "MANUALPAIDATENTRYBUS":// amount;
                    try
                    {
                        if (_MediaDetectedState == SmartFunctions.MediaDetected.NONE)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.MediaNotPresent), "");
                            Logging.Log(LogLevel.Error, ThreadName + "Card not present");
                            return true;
                        }
                        SmartFunctions.Instance.SwitchToCardOnState();

                        long cardSerNbr = Convert.ToInt64(eventMessage.Attribute);
                        if (Convert.ToInt64(eventMessage.Attribute) != _logMediaReloader.Media.ChipSerialNumber)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
                            break;
                        }

                        var msg_splitted = eventMessage.Message.Split(';');
                        int pValue = Convert.ToInt32(msg_splitted[0]);
                        Logging.Log(LogLevel.Verbose, "pVlaue" +Convert.ToInt32(msg_splitted[0]));
                        int dest = Convert.ToInt32(msg_splitted[1]);
                        Logging.Log(LogLevel.Verbose, "dest" +Convert.ToInt32(msg_splitted[1]));

                        OperationTypeValues ridetype = OperationTypeValues.ValueDeductedInEntry;
                        //SharedData.CSC_oldEndOfValidityDate = _logMediaReloader.Application.Products.Product(0).EndOfValidity; 

                        //Update Values of tPurse
                        SalesRules.PurseDeductionUpdate(_logMediaReloader, pValue, PaymentMethods.StoreValue);
                        SalesRules.AddTrasactionHistoryRecord(_logMediaReloader, ridetype, pValue);


                        if (hwCsc.UpdateTPurseData(_logMediaReloader, -pValue, false)
                                && hwCsc.AppendCommonAreaPurseHistoryRecord(_logMediaReloader)                                
                                && hwCsc._CommitModifications())
                            {                        
                            FldsCSCBusCheckOutRebate pars = new FldsCSCBusCheckOutRebate();
                            pars.purseRemainingVal = _logMediaReloader.Purse.TPurse.Balance;
                            pars.transactionValue = pValue;
                            pars.entryLine = SharedData.LineNumber;
                            pars.entryStage = SharedData.StationNumber;
                            string ret = SmartFunctions.Instance.GetTDforCCHSGen(_logMediaReloader, TransactionType.BusCheckOutWithTPurse, pars, false, _logMediaReloader.Application.TransportApplication.Test);
                            if (_signatureAtEachTransaction)
                                Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString() + "<XdrData>" + ret + "</XdrData>");
                            else
                                Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString());
                            Logging.Log(LogLevel.Verbose, "ManualPaidAtEntryBus Done with Success total Purse Val= " + _logMediaReloader.Purse.TPurse.Balance);
                            //We can try to generate transaction
                            return true;
                        }
                        Logging.Log(LogLevel.Error, ThreadName + "PAYMENTWITHTPURSE Failed");
                        _logMediaReloader.Reset();
                        hwCsc.Reset();

                    }
                    catch (Exception Ex)
                    {
                        Logging.Log(LogLevel.Error, ThreadName + "PAYMENTWITHTPURSE Error :" + Ex.Message);
                        _logMediaReloader.Reset();
                        hwCsc.Reset();
                    }
                    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.CardCannotBeWritten), "");
                    return false;

                #region "RIDE FARE DEDUCTION"
                //SKS: Added on 20150816
                case "PAYMENTDEDUCTION":// amount;paymentmode;ridetype
                    try
                    {
                        var msg_splitted = eventMessage.Message.Split(';');
                        int pValue = -Convert.ToInt32(msg_splitted[0]);
                        
                        OperationTypeValues ridetype  = (OperationTypeValues)Convert.ToInt32(msg_splitted[1]);
                        
                        SalesRules.AddTrasactionHistoryRecord(_logMediaReloader, ridetype, pValue);
                        if (hwCsc.UpdateTPurseData(_logMediaReloader, pValue, false)
                                && hwCsc.AppendCommonAreaPurseHistoryRecord(_logMediaReloader)                                
                                && hwCsc._CommitModifications())
                            {
                                Logging.Log(LogLevel.Verbose, "UpdateTPurseData Done with Success total Purse Val= " + _logMediaReloader.Purse.TPurse.Balance);
                                if (hwCsc.WriteLocalSaleData(_logMediaReloader, true)) // SKS: 09-10-2014 As updated TPurse is done with sucesses, so we are lest bothered about local sale data success stauts 
                                {
                                    Logging.Log(LogLevel.Verbose, "WriteLocalSaleData Done Successfully");
                                }
                                else
                                {
                                    Logging.Log(LogLevel.Error, "WriteLocalSaleData Done Failed");
                                }


                                ///TODO: increase Sequence Number
                                ///Generate TXN Data
                                ///sign TXN Data record and send to MMI
                                SharedData.TransactionSeqNo++;

                                string xmlStr = "";
                                //if (_delhiCCHSSAMUsage && _signatureAtEachTransaction)
                                //{
                                //    TransactionType txnType;
                                //    if (paymentType == PaymentMethods.Cash)
                                //    {
                                //        txnType = TransactionType.TPurseWebTopupReload;
                                //        SmartFunctions.Instance.GetTDforCCHS(_logMediaReloader, txnType, SharedData.TransactionSeqNo, pValue, out xmlStr);
                                //    }
                                //    else if (paymentType == PaymentMethods.BankCard)
                                //    {
                                //        txnType = TransactionType.TXN_CSC_ADD_VALUE_EFT;
                                //        IFS2.Equipment.Common.CCHS.BankTopupDetails bankTopupDetails = SerializeHelper<IFS2.Equipment.Common.CCHS.BankTopupDetails>.XMLDeserialize(eventMessage._par[3]);
                                //        SmartFunctions.Instance.GetTDforCCHSForEFTAddVal(_logMediaReloader, bankTopupDetails, SharedData.TransactionSeqNo, pValue, out xmlStr);
                                //    }
                                //    else
                                //        txnType = TransactionType.;
                                //        return true;
                                //}
                                //else
                                //    SharedData.SaveContextFile();//save txn sequence No 
                                _logMediaReloader.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;

                                //	UpdateCardStatusOnCC 
                                // Communication.SendMessage(ThreadName, "Message", "UpdateCardStatusOnCC", "0", xmlStr);
                                if (xmlStr != "" && _signatureAtEachTransaction)
                                    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString() + "<XdrData>" + xmlStr + "</XdrData>");
                                else
                                    //Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", "");
                                    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.ToXMLString());
                                //Restart polling only in case of
                                // SmartFunctions.Instance.Init();
                                //  SmartFunctions.Instance.HaltCard();//SKS:Commented on 2014-12-17 
                                //  SmartFunctions.Instance.StartPolling();//SKS:Commented on 2014-12-17 
                                SmartFunctions.Instance.SwitchToDetectRemovalState();//SKS:Added on 2014-12-17 
                                return true;
                            }//if (hwCsc.UpdateTPurseData(_logMediaReloader,false))
                        //}                       
                        //else// error
                        //{
                        //    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.UnknownPaymentMethod), "");
                        //}
                        //Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", "0", _logMediaReloader.Purse.TPurse.Balance.ToString());
                        Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.UnknownPaymentMethod), "");
                        Logging.Log(LogLevel.Error, ThreadName + "PAYMENTDEDUCTION Failed");
                        _logMediaReloader.Reset();
                        hwCsc.Reset();

                    }
                    catch (Exception Ex)
                    {
                        Logging.Log(LogLevel.Error, ThreadName + "RIDEFAREDEDUCTION Error :" + Ex.Message);
                        _logMediaReloader.Reset();
                        hwCsc.Reset();
                    }
                    break;
                #endregion
                case "READUSERCARDGLOBALDATA":

                    try
                    {
                        TTErrorTypes err=TTErrorTypes.CannotReadTheCard;
                        // Check the card serial number
                        if (Convert.ToInt64(eventMessage.Attribute) != _logMediaReloader.Media.ChipSerialNumber)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "ReadUserCardGlobalDataAnswer", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
                            return true;
                        }
                        bool ok;
                        ok = hwCsc.ReadCustomerData(_logMediaReloader, MediaDetectionTreatment.BasicAnalysis_AVM_TVM);
                        if (ok) ok = hwCsc.ReadAutoReloadData(_logMediaReloader, MediaDetectionTreatment.BasicAnalysis_AVM_TVM);
                        if (ok) ok = hwCsc.ReadValidationData(_logMediaReloader, MediaDetectionTreatment.BasicAnalysis_AVM_TVM);
                        if (ok) ok = hwCsc.ReadLocalSaleData(_logMediaReloader, MediaDetectionTreatment.BasicAnalysis_AVM_TVM);
                        if (!ok)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "ReadUserCardGlobalDataAnswer", Convert.ToString((int)err), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Cannot Read Card");
                            return true;
                        }
                        string s = _logMediaReloader.ToXMLString();
                        Communication.SendMessage(ThreadName, "Answer", "ReadUserCardGlobalDataAnswer", "0", s);                        
                    }
                    catch (Exception e1)
                    {
                        Communication.SendMessage(ThreadName, "Answer", "ReadUserCardGlobalDataAnswer", Convert.ToString((int)TTErrorTypes.CannotReadTheCard), "");
                        Logging.Log(LogLevel.Error, ThreadName + "_ReadUserCardGlobalData Error :" + e1.Message);                        
                    }
                    return true;

                case "READUSERCARDTRANSACTIONHISTORY":

                    try
                    {
                        SmartFunctions.Instance.SwitchToCardOnState();
                        // Check the card serial number
                        if (Convert.ToInt64(eventMessage.Attribute) != _logMediaReloader.Media.ChipSerialNumber)
                        {
                           /*SKS commented on 20160516 .. no more in use
                            * try
                            {
                                _TxnHistory.List.Clear();
                            }
                            catch { }
                            */ 
                            Communication.SendMessage(ThreadName, "Answer", "ReadUserCardTransactionHistoryAnswer", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
                            return true;
                        }
                        if (hwCsc.ReadAllTPurseHistory(_logMediaReloader, MediaDetectionTreatment.BasicAnalysis_AVM_TVM))
                        {
                            //  string s = _logMediaReloader.ToXMLString();//SKS commented on 20160516
                            //  History tmpTxnHistory = _logMediaReloader.Purse.History;// SKS commented on 20160516
                            int cnt = _logMediaReloader.Purse.History.List.Count;

                            if (cnt > 0)
                                if (_logMediaReloader.Purse.History.Hidden == true) _logMediaReloader.Purse.History.Hidden = false;
                            /*SKS commented on 20160516 no more now using .Hidden property to hide history data for other media treatment
                             * if (s.Contains("</History>"))
                               {
                                   _TxnHistory.List.Clear();
                                   for (int i = 0; i < tmpTxnHistory.List.Count;i++ )
                                   {
                                       _TxnHistory.Add(tmpTxnHistory.Transaction(i));
                                   }                               
                                
                               }
                               else
                               {
                                   for (int i = 0; i < _TxnHistory.List.Count; i++)
                                   {
                                       tmpTxnHistory.Add(_TxnHistory.Transaction(i));
                                   }
                                    s = _logMediaReloader.ToXMLString();
                               }
                             */
                            string s = _logMediaReloader.ToXMLString();
                            _logMediaReloader.Purse.History.Hidden = true;
                            Communication.SendMessage(ThreadName, "Answer", "ReadUserCardTransactionHistoryAnswer", "0", s);
                            // Reset History data as we do not need further...... SKS: 2014-12-17
                            ///tmpTxnHistory.Reset();//SKS commented on 20160516
                        }
                        else
                        {
                            Communication.SendMessage(ThreadName, "Answer", "ReadUserCardTransactionHistoryAnswer", Convert.ToString((int)TTErrorTypes.CannotReadTheCard), "");
                            Logging.Log(LogLevel.Error, ThreadName + "_Cannot Read the Card");
                            return true;
                        }

                    }
                    catch (Exception e1)
                    {
                        Communication.SendMessage(ThreadName, "Answer", "ReadUserCardTransactionHistoryAnswer", Convert.ToString((int)TTErrorTypes.CannotReadTheCard), "");
                        Logging.Log(LogLevel.Error, ThreadName + "_ReadUserCardTransactionHistory Error :" + e1.Message);
                    }
                    return true;

            }
            return false;
        }
    }    
}
