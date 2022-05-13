// Would use DM1::Sale, DM2::Validation. Both have status fields

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public class MediaOpReqCSCIssue : MediaOpReqNoPreRegisteration
    {
        public MediaOpReqCSCIssue(MainTicketingRules parent, 
            string parsXml
            ) :
            base(parent, null, parsXml)
        {            
            ParseIps(parsXml);
            _family = ProductParameters.GetProductFamily(_fareProductType);
            if (_family == -1)
            {
                throw new Exception("Family not found"); // TODO: see in its client to handle such things gracefully
            }
        }

        private void ParseIps(string parsXml)
        {
            XDocument parsDoc = XDocument.Parse(parsXml);
            XElement root = parsDoc.Root;
            _fareProductType = Convert.ToInt16(root.Element("FareProductAsked").Value);
            _purseValue = Convert.ToInt32(root.Element("PurseValue").Value);
            _bTestTicket = (root.Element("TestTicket").Value == "1");
            _paymentType = (PaymentMethods)(Convert.ToInt32(root.Element("PaymentType").Value));
            _language = (Customer.LanguageValues)(Convert.ToInt32(root.Element("Language").Value));
            _fees = Convert.ToInt32(root.Element("Fees").Value);
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.CSCIssue;
        }

        Tuple<string, string, bool> []_xmlStringToSendToMMI;
        

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            throw new NotImplementedException();
        }
        
        public Tuple<string, string> GetAddValOpToStringToBeSentToMMI()
        {
            if (_xmlStringToSendToMMIAddValue == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    SmartFunctions.Instance.GetTDforCCHS(_logicalMediaUpdatedForCurrentOp, TransactionType.TPurseDirectReload, ++SharedData.TransactionSeqNo, _purseValue, out cchsStr, _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test, true);
                    _logicalMediaUpdatedForCurrentOp.EquipmentData.SequenceNumber = SharedData.TransactionSeqNo;
                    cchsStr = Utility.MakeTag("XdrData", cchsStr);
                }
                _xmlStringToSendToMMIAddValue = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr); 
            }
            return _xmlStringToSendToMMIAddValue;
        }

        Tuple<string, string> _xmlStringToSendToMMICSCIssue = null;
        Tuple<string, string> _xmlStringToSendToMMIAddValue = null;

        public Tuple<string, string> GetCSCIssueOpToStringToBeSentToMMI()
        {
            if (_xmlStringToSendToMMICSCIssue == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCIssueTxn txn = new FldsCSCIssueTxn();
                    txn.psn = (int)_logicalMediaReference.Application.LocalLastAddValue.SequenceNumber;
                    txn.ticket1StartDate = DateTime.Now;
                    txn.cscType = CSC_Type_t.CardADesfire;
                    txn.ticket1Type = _fareProductType;
                    if (_family == 60)
                        txn.issueType = IssueType.CardIssue_Deposit_CommonPurse_SVProduct;
                    else
                        txn.issueType = IssueType.CardIssue_Deposit_CommonPurse_PeriodPass;
                    //txn.ticket2StartDate = DateTime.MaxValue;
                    //txn.ticket2EndDate = DateTime.MaxValue;  
                    try
                    {
                        txn.ticket2StartDate = _logicalMediaReference.Application.Products.Product(0).EndOfValidity;
                        txn.ticket2EndDate = txn.ticket2StartDate.AddDays(-1);
                    }
                    catch (Exception e)
                    {
                        txn.ticket2StartDate = new DateTime(2080, 1, 1);
                        txn.ticket2EndDate = new DateTime(2079, 12, 31);
                        Logging.Log(LogLevel.Error, "MediaOpReqCSCIssue.GetCSCIssueOpToStringToBeSentToMMI.Error end of validity " + e.Message);
                    }
                    txn.cntRides = 0;
                    txn.blockingStatus = 1; // Unblocked
                    txn.lng = 0; // English (only value that can be assigned for this field)
                    txn.cscRemainingValue = 0;
                    txn.transactionValue = _logicalMediaReference.Application.TransportApplication.Deposit + _fees;
                    txn.cscDepositAmount = _logicalMediaReference.Application.TransportApplication.Deposit;

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaReference, TransactionType.CSCIssue, txn,
                        _statusAreaDone != Status.Success, _bTestTicket);
                }
                _xmlStringToSendToMMICSCIssue = Tuple.New(_logicalMediaReference.ToXMLString(), cchsStr); 
            }
            return _xmlStringToSendToMMICSCIssue;
        }        

        short _fareProductType;
        PaymentMethods _paymentType;
        bool _bTestTicket;
        public PaymentMethods PaymtTyp { get { return _paymentType; } }
        Customer.LanguageValues _language;
        int _fees;
        int _family;
        int _purseValue;

        enum Status
        {
            NotInitiated,
            Issue_DM2_WrittenButFailed,
            Issue_DM1_ToBeWritten,
            Issue_DM1_WrittenButFailed,
            AddVal_DM1_ToBeWritten,
            AddVal_DM1_WrittenButFailed,
            AddVal_DM2_ToBeWritten,
            AddVal_DM2_WrittenButFailed,
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;

        public override bool DoesNeedTokenDispenser()
        {
            return false;
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            TransportApplication.StatusValues taStatusNow = logMediaNow.Application.TransportApplication.StatusRead;
            Media.StatusValues mediaStatusNow = logMediaNow.Media.StatusRead;
            int purseValueNow = logMediaNow.Purse.TPurse.BalanceRead;

            switch (_statusAreaDone)
            {
                case Status.Issue_DM2_WrittenButFailed:
                    {
                        if (taStatusNow == TransportApplication.StatusValues.Issued)
                        {
                            _statusAreaDone = Status.Issue_DM1_ToBeWritten;
                            return true;
                        }
                        else if (taStatusNow == _statusAtBeginningTA)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case Status.Issue_DM1_ToBeWritten:
                case Status.Issue_DM1_WrittenButFailed:
                    {
                        if (mediaStatusNow == Media.StatusValues.Issued)
                        {
                            _statusAreaDone = Status.AddVal_DM1_ToBeWritten;
                            return true;
                        }
                        else
                        {
                            if (mediaStatusNow == _statusBeginningMedia && purseValueNow == 0)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                case Status.AddVal_DM1_ToBeWritten:
                case Status.AddVal_DM1_WrittenButFailed:
                    {
                        if (logMediaNow.Purse.TPurse.BalanceRead == 0)
                            return true;
                        else if (logMediaNow.Purse.TPurse.BalanceRead == _purseValue)
                        {
                            _statusAreaDone = Status.AddVal_DM2_ToBeWritten;
                            return true;
                        }
                        else
                            return false;
                    }
                case Status.AddVal_DM2_WrittenButFailed:
                    {
                        //DateTime dtLocLastTimestampNow = logMediaNow.Application.LocalLastAddValue.DateTimeRead;
                        //if ((DateTime.Now - dtLocLastTimestampNow).TotalSeconds < 120) // TO CHECK THAT ISSUE HAS EFFECTED IT OR NOT
                        //    return false;
                        //else
                            return true;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        // using these new variable, because in lab we are allowing CSCs with status other than Initialised be issued
        TransportApplication.StatusValues _statusAtBeginningTA = TransportApplication.StatusValues.Unknown;
        Media.StatusValues _statusBeginningMedia = Media.StatusValues.Unknown;        

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            _xmlStringToSendToMMI = new Tuple<string, string, bool>[2];
            bool _bToPutIssueTxnForMMI = false;
            bool _bToPutAddValTxnForMMI = false;
            bool _bWTEIssue = false; 
            bool _bWTEAddVal = false;

            try
            {
                LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

                if (_statusAreaDone == Status.NotInitiated)
                {
                    _ticketingRules.TreatmentOnCardDetection2(false, false);
                    var error = _ticketingRules.ErrorForJustProducedMedia;
                    if (error != TTErrorTypes.NoError)
                    {
                        if (error == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || error == TTErrorTypes.CannotReadTheCard)
                            return ResultLastAttempt.MediaCouldntBeRead;
                        else
                        {
                            SetMediaSerialNumber(0);
                            _statusAreaDone = Status.NotInitiated;
                            return ResultLastAttempt.MediaNotFoundFitForOperation;
                        }
                    }
                    _statusAtBeginningTA = _ticketingRules.GetLogicalDataOfMediaAtFront().Application.TransportApplication.Status;
                    _statusBeginningMedia = _ticketingRules.GetLogicalDataOfMediaAtFront().Media.Status;
                }

                if (new List<Status>{
                    Status.AddVal_DM1_ToBeWritten,
                    Status.AddVal_DM1_WrittenButFailed,
                    Status.AddVal_DM2_ToBeWritten,
                    Status.AddVal_DM2_WrittenButFailed}.Contains(_statusAreaDone))
                    _ticketingRules.SetReadPurpose(MediaDetectionTreatment.TOM_AnalysisForAddVal);

                _ticketingRules.TreatmentOnCardDetection2(false, true);                

                TTErrorTypes err = _ticketingRules.ErrorForJustProducedMedia;
                if (err == TTErrorTypes.CannotReadTheCard || err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow)
                    return ResultLastAttempt.MediaCouldntBeRead;
                if (_statusAreaDone != Status.NotInitiated && !EvaluateIfMediaIsFitForOperation())
                {
                    if (_statusAreaDone == Status.Success)
                    {
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                    else
                    {
                        _completionStatus = MediaUpdateCompletionStatus.MediaNoMoreFitForOperation;
                        return ResultLastAttempt.MediaNoMoreFitForOperation;
                    }
                }

                if (new List<Status>{
                    Status.NotInitiated,
                    Status.Issue_DM2_WrittenButFailed, 
                    Status.Issue_DM1_WrittenButFailed, 
                    Status.Issue_DM1_ToBeWritten}.Contains(_statusAreaDone))
                {
                    _logicalMediaReference = logMediaNow;
                    var fpSpecs = SharedData._fpSpecsRepository.GetSpecsFor(_fareProductType);
                    if (!SalesRules.CSCIssueUpdate(_logicalMediaReference, _fareProductType, _paymentType, _bTestTicket, fpSpecs._Deposit + fpSpecs._SalePrice.Val, _language, false))
                        return ResultLastAttempt.MediaCouldntBeRead;
                }

                CommonHwMedia hwCsc = _ticketingRules.hwCsc;
                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.Issue_DM2_WrittenButFailed)
                {
                    // Attempt writing DM2 (File #8 need not be and cannot be written by TOM).
                    if (!hwCsc.WriteLocalSaleData(_logicalMediaReference, false) // File #1, #3
                    || !hwCsc.WriteLocalValidationData(_logicalMediaReference) // File #2
                    )
                    {
                        // we let the status remain NotDone
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    bool bDM2Written = hwCsc.CommitModifications();
                    
                    if (!bDM2Written)
                    {
                        _statusAreaDone = Status.Issue_DM2_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                        
                        _bWTEIssue = true;
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        _bToPutIssueTxnForMMI = true;
                        _statusAreaDone = Status.Issue_DM1_ToBeWritten;
                    }
                }

                if (_statusAreaDone == Status.Issue_DM1_WrittenButFailed || _statusAreaDone == Status.Issue_DM1_ToBeWritten)
                {
                    // Attempt writing DM1
                    if (!hwCsc.UpdateTPurseData(_logicalMediaReference, _logicalMediaReference.Purse.LastAddValue.Amount, false) // File #1, #2, #5
                    || !hwCsc.WriteMainSaleData(_logicalMediaReference) // File #6                    
                    || !hwCsc.WriteCardHolderData(_logicalMediaReference)) // File #9. So, we write whole auto-topup data also in this file. This may be bad, but don't have time to be elegant
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    if (!hwCsc.CommitModifications())
                    {
                        _statusAreaDone = Status.Issue_DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();

                        _bToPutIssueTxnForMMI = true;
                        _bWTEIssue = true;
                        // Though never observed that reader writer reutrns incorrect value
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        if (_purseValue == 0)
                        {
                            // No add value needed to be done.
                            _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                            _bToPutIssueTxnForMMI = true;
                            return ResultLastAttempt.Success;
                        }
                        else
                        {
                            _statusAreaDone = Status.AddVal_DM1_ToBeWritten;

                            // It is not a good way (i.e. to recreate logical structure by re-reading it). But it has got too late now
                            _ticketingRules.SetMediaKeptOnReaderWasLastUpdatedInThisVeryCycle();
                            _ticketingRules.SetReadPurpose(MediaDetectionTreatment.TOM_AnalysisForAddVal);
                            _logicalMediaReference = new LogicalMedia(_logicalMediaReference.ToXMLString());
                            _ticketingRules.ClearDataStructuresForMediaKeptOnRW();
                            _ticketingRules.TreatmentOnCardDetection2(false, true);
                            _bToPutIssueTxnForMMI = true;
                            if (_ticketingRules.ErrorForJustProducedMedia != TTErrorTypes.NoError)                                                            
                                return ResultLastAttempt.MediaCouldntBeWritten;
                            else
                                _logicalMediaReference = _ticketingRules.GetLogicalDataOfMediaAtFront();
                        }
                    }
                }

                if (_statusAreaDone == Status.AddVal_DM1_ToBeWritten || _statusAreaDone == Status.AddVal_DM1_WrittenButFailed)
                {
                    _logicalMediaUpdatedForCurrentOp = new LogicalMedia(_ticketingRules.GetLogicalDataOfMediaAtFront().ToXMLString());

                    if (!SalesRules.AddValueUpdate(_logicalMediaUpdatedForCurrentOp, _purseValue, _paymentType))
                        return ResultLastAttempt.MediaCouldntBeRead;

                    //SharedData.CSC_oldEndOfValidityDate = _logicalMediaUpdatedForCurrentOp.Application.Products.Product(0).EndOfValidity;
                    if (
                    CommonRules.SetupProductEndOfValidity(_logicalMediaUpdatedForCurrentOp,
                        EndOfValidityMode.CurrentPlusYears,
                        DateTime.Now,
                        MainTicketingRules._nProductValidityYears
                        ) != TTErrorTypes.NoError)
                        return ResultLastAttempt.MediaCouldntBeRead;

                    // Attempt writing DM1
                    if (hwCsc.UpdateTPurseData(_logicalMediaUpdatedForCurrentOp, _purseValue, true))
                    {
                        _bToPutAddValTxnForMMI = true;
                        _statusAreaDone = Status.AddVal_DM2_ToBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.AddVal_DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        _bToPutAddValTxnForMMI = true;
                        _bWTEAddVal = true;
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }

                    Debug.Assert(_statusAreaDone == Status.AddVal_DM2_ToBeWritten || _statusAreaDone == Status.AddVal_DM2_WrittenButFailed);
                }
                // Attempt writing DM2
                if (_statusAreaDone == Status.AddVal_DM2_ToBeWritten || _statusAreaDone == Status.AddVal_DM2_WrittenButFailed)                
                    return Write_DM2_AddVal(ref _bToPutAddValTxnForMMI, ref _bWTEAddVal, hwCsc);
            }
            finally
            {
                if (_bToPutIssueTxnForMMI)
                {
                    Tuple<string, string> x = GetCSCIssueOpToStringToBeSentToMMI();
                    _xmlStringToSendToMMI[0] = Tuple.New(x.First, x.Second, _bWTEIssue);
                }
                if (_bToPutAddValTxnForMMI)
                {
                    Tuple<string, string> x = GetAddValOpToStringToBeSentToMMI();
                    _xmlStringToSendToMMI[1] = Tuple.New(x.First, x.Second, _bWTEAddVal);
                }
            }
            throw new NotImplementedException();
        }

        private ResultLastAttempt Write_DM2_AddVal(ref bool _bToPutAddValTxnForMMI, ref bool _bWTEAddVal, CommonHwMedia hwCsc)
        {
            if (hwCsc.WriteLocalSaleData(_logicalMediaUpdatedForCurrentOp, true))
            {
                Logging.Log(LogLevel.Verbose, "WriteLocalSaleData Done Successfully");
                _statusAreaDone = Status.Success;
                _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                _bToPutAddValTxnForMMI = true;
                return ResultLastAttempt.Success;
            }
            else
            {
                Logging.Log(LogLevel.Error, "WriteLocalSaleData Done Failed");
                _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                _statusAreaDone = Status.AddVal_DM2_WrittenButFailed;
                if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                    SetAtLeastSomethingWasWrittenInLastAttempt();
                _bToPutAddValTxnForMMI = true;
                _bWTEAddVal = true;
                return ResultLastAttempt.MediaCouldntBeWritten;
            }
        }

        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated 
                && _statusAreaDone != Status.Issue_DM2_WrittenButFailed
                && _statusAreaDone != Status.Issue_DM1_ToBeWritten);
        }

        internal Tuple<string, string, bool> GetIssueTxnToUploadToMMI()
        {
            return _xmlStringToSendToMMI[0];
        }

        internal Tuple<string, string, bool> GetAddValTxnToUploadToMMI()
        {
            return _xmlStringToSendToMMI[1];
        }
    }
}
