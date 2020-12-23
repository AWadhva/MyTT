using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Diagnostics;
using IFS2.Equipment.Common.CCHS;

namespace IFS2.Equipment.TicketingRules
{
    public class MediaOpReqNewProduct : MediaOpReqPreRegisteration
    {
        public MediaOpReqNewProduct(MainTicketingRules ticketingRules, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(ticketingRules, snum, mediaDataPriorToOperation)
        {
            ParseIps(opParsXml);
            _family = ProductParameters.GetProductFamily(_fareProductType);
        }

        short _fareProductType;
        int _fees;
        int _family;
        

        private void ParseIps(string opParsXml)
        {
            XDocument doc = XDocument.Parse(opParsXml);
            XElement root = doc.Root;
            _fareProductType = Convert.ToInt16(root.Element("FareProductAsked").Value);
            _fees = Convert.ToInt32(root.Element("Fees").Value);
            _paymentType = (PaymentMethods)(Convert.ToInt32(root.Element("PaymentTyp").Value));
        }

        enum Status
        {
            NotInitiated,
            DM2_WrittenButFailed,
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;
        DateTime? _dtOfLastBadWriteAttempt;

        private bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();
            DateTime tsAtMediaRegisteration = _logicalMediaPriorToOperation.Application.Validation.LastTransactionDateTimeRead;
            DateTime tsNow = logMediaNow.Application.Validation.LastTransactionDateTimeRead;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (tsAtMediaRegisteration != tsNow)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                case Status.DM2_WrittenButFailed:
                    {
                        if (tsAtMediaRegisteration == tsNow)
                            return true;
                        else if (tsAtMediaRegisteration == (DateTime)_dtOfLastBadWriteAttempt)
                        {
                            _statusAreaDone = Status.Success;
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                default:
                    throw new Exception("Unexpected");
            }
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCIssueTxn txn = new FldsCSCIssueTxn();
                    txn.psn = (int)_logicalMediaUpdatedForCurrentOp.Application.LocalLastAddValue.SequenceNumber;
                    txn.ticket1StartDate = DateTime.Now;
                    txn.cscType = CSC_Type_t.CardADesfire;
                    txn.ticket1Type = _fareProductType;
                    if (_family == 60)
                        txn.issueType = IssueType.SVProductIssue;
                    else
                        txn.issueType = IssueType.TouristPass; // TODO: CCHS doc says that this enum value is not used. But it fits the best here

                    txn.ticket2StartDate = DateTime.MaxValue;
                    txn.ticket2EndDate = DateTime.MaxValue;
                    txn.cntRides = 0;
                    txn.blockingStatus = 1; // Unblocked
                    txn.lng = 0; // English (only value that can be assigned for this field)
                    txn.cscRemainingValue = _logicalMediaUpdatedForCurrentOp.Purse.TPurse.Balance;
                    txn.transactionValue = _fees;
                    txn.cscDepositAmount = _logicalMediaPriorToOperation.Application.TransportApplication.Deposit;

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp, TransactionType.CSCIssue, txn,
                        _statusAreaDone != Status.Success,
                        _logicalMediaPriorToOperation.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }

        public override MediaOpType GetOpType()
        {
            return MediaOpType.NewProduct;
        }
        
        PaymentMethods _paymentType;        
        
        public PaymentMethods PaymtTyp { get { return _paymentType; } }
        
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;        

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOperationRequestAddValue::CorrectMediaAppeared");
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;

            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
                return ResultLastAttempt.MediaCouldntBeRead;
            else if (err == TTErrorTypes.NoError)
            {
                LogicalMedia logMediaAtFront = _ticketingRules.GetLogicalDataOfMediaAtFront();
                
                if (!EvaluateIfMediaIsFitForOperation())
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

                _logicalMediaUpdatedForCurrentOp = new LogicalMedia(_logicalMediaPriorToOperation.ToXMLString());

                if (!SalesRules.CSCIssueUpdate(_logicalMediaUpdatedForCurrentOp, _fareProductType, _paymentType,
                    _logicalMediaPriorToOperation.Media.Test, _fees, Customer.LanguageValues.English, true))
                    return ResultLastAttempt.MediaCouldntBeRead;
               
                CommonHwMedia hwCsc = _ticketingRules.hwCsc;

                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM2_WrittenButFailed)
                {
                    // Attempt writing DM2 (File #8 need not be and cannot be written by TOM).
                    if (!hwCsc.WriteLocalSaleData(_logicalMediaUpdatedForCurrentOp, false) // File #1, #3
                    || !hwCsc.WriteLocalValidationData(_logicalMediaUpdatedForCurrentOp) // File #2
                    )
                    {
                        // we let the status remain NotDone
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    bool bDM2Written = hwCsc.CommitModifications();
                    // TODO: Generate txn record here irrespective of bDM2Written
                    if (!bDM2Written)
                    {
                        _statusAreaDone = Status.DM2_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;
                        _dtOfLastBadWriteAttempt = _logicalMediaUpdatedForCurrentOp.Application.Validation.LastTransactionDateTime;
                        // Though never observed that reader writer reutrns incorrect value

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();

                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }
                    else
                    {
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                }
                
                throw new Exception("Unreachable at the moment");
            }
            else
            {
                // TODO: Still make sure that polling does get initiated, so that TT doesn't remain in useless state.
                Debug.Assert(false);
                throw new Exception("Unexpected error code");
            }
        }

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }
    }
}
