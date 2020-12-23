// big question is that whether the patron would have to wait for his card to be blacklisted to settle the payment. Since there is no such command "Settle Debt" while "Settle Bad Debt" exists, it implies that 
// we will perform the operation only when the card is black listed. 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Xml.Linq;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    class MediaOpReqSettleBadDebt : MediaOpReqPreRegisteration
    {
        public MediaOpReqSettleBadDebt
            (MainTicketingRules parent, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(parent, snum, mediaDataPriorToOperation)
        {
            ParseIps(opParsXml);
        }

        private int _badDebtAmtSettled;

        enum Status
        {
            NotInitiated,
            DM1_WrittenButFailed,
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;


        private void ParseIps(string opParsXml)
        {
            XDocument doc = XDocument.Parse(opParsXml);
            XElement root = doc.Root;
            _badDebtAmtSettled = Convert.ToInt32(root.Element("BadDebtAmtSettled").Value);
        }

        public override IFS2.Equipment.Common.MediaOpType GetOpType()
        {
            return MediaOpType.SettleBadDebt;
        }

        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;
        Tuple<string, string> _xmlStringToSendToMMI = null;
        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    FldsCSCBadDebtCashPayment txn = new FldsCSCBadDebtCashPayment();
                    txn.badDebtAmountSettled = _badDebtAmtSettled;

                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp,
                        TransactionType.CSC_BAD_DEBT_CASH_PAYMENT,                        
                        txn,
                        _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;

        }

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {            
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            bool bMediaBlocked = _logicalMediaPriorToOperation.Media.Blocked;
            bool bMediaBlockedNow = logMediaNow.Media.Blocked;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (!bMediaBlockedNow)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                case Status.DM1_WrittenButFailed:
                    {
                        if (!bMediaBlockedNow)
                            return true;

                        _statusAreaDone = Status.Success;
                        return false;
                    }
                default:
                    throw new Exception("Unexpected");
            }
        }

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            Logging.Log(LogLevel.Verbose, "MediaOpReqSettleBadDebt::CorrectMediaAppeared");
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

                SalesRules.BadDebtSettlementUpdate(_logicalMediaUpdatedForCurrentOp, _badDebtAmtSettled);

                CommonHwMedia hwCsc = _ticketingRules.hwCsc;

                if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
                {
                    // TODO: See if the DM1#Sequence Number too has to be updated

                    // Attempt writing DM1
                    if (!hwCsc.WriteCommonValidationFile(_logicalMediaUpdatedForCurrentOp))
                    {
                        return ResultLastAttempt.MediaCouldntBeWritten;
                    }

                    if (hwCsc.CommitModifications())
                    {
                        _statusAreaDone = Status.Success;
                        _completionStatus = MediaUpdateCompletionStatus.DoneSuccessfully;
                        return ResultLastAttempt.Success;
                    }
                    else
                    {
                        _statusAreaDone = Status.DM1_WrittenButFailed;
                        _completionStatus = MediaUpdateCompletionStatus.DoneWithWTE;

                        if (_ticketingRules.hwCsc.GetLastStatus() != CommonHwMedia.Status.Failed_MediaWasNotInField)
                            SetAtLeastSomethingWasWrittenInLastAttempt();
                        return ResultLastAttempt.MediaCouldntBeWritten;
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
    }
}
