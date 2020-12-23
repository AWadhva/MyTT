// Does DM2::Validation::Transaction Date and Time need to be updated?? If yes, then modifications need to be made


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using IFS2.Equipment.Common;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public class MediaOpReqEnableAutoTopup : MediaOpReqPreRegisteration
    {
        enum Status
        {
            NotInitiated,
            DM1_WrittenButFailed,            
            Success
        };

        Status _statusAreaDone = Status.NotInitiated;
        LogicalMedia _logicalMediaUpdatedForCurrentOp = null;

        bool bInitialiseBankTopupGenerated = false;

        public MediaOpReqEnableAutoTopup
            (MainTicketingRules parent, Int64 snum, LogicalMedia mediaDataPriorToOperation, string opParsXml) :
            base(parent, snum, mediaDataPriorToOperation)
        {
            ParseIps(opParsXml);
        }

        public override bool bIsOpCompletedEvenPartly()
        {
            return (_statusAreaDone != Status.NotInitiated);
        }

        private void ParseIps(string opParsXml)
        {
            XDocument elem = XDocument.Parse(opParsXml);
            XElement root = elem.Root;
            
            if (root.Element("Details") != null)
            {
                _autoTopupDetails = SerializeHelper<AutoTopupDetails>.XMLDeserialize(root.Element("Details").Value);
            }
        }

        private AutoTopupDetails _autoTopupDetails = null; // TODO: We need to write to CSC DM1#CardHolder file

        public override MediaOpType GetOpType()
        {
            return MediaOpType.EnableAutoTopup;
        }

        protected internal bool EvaluateIfMediaIsFitForOperation()
        {
            LogicalMedia logMediaNow = _ticketingRules.GetLogicalDataOfMediaAtFront();

            AutoReload.StatusValues activationStatusAtTimeOfRegisteration = _logicalMediaPriorToOperation.Purse.AutoReload.StatusRead;
            AutoReload.StatusValues activationStatusNow = logMediaNow.Purse.AutoReload.StatusRead;

            switch (_statusAreaDone)
            {
                case Status.NotInitiated:
                    {
                        if (activationStatusAtTimeOfRegisteration != activationStatusNow)
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
                        if (activationStatusNow == AutoReload.StatusValues.Disabled)
                            return true;

                        _statusAreaDone = Status.Success;
                        return false;
                    }
                default:
                    throw new Exception("Unexpected");
            }
        }

        Tuple<string, string> _xmlStringToSendToMMI = null;

        public Tuple<string, string> GetXmlStringForInitBankTopupToSendToMMI()
        {
            if (bInitialiseBankTopupGenerated)
                return null;

            bInitialiseBankTopupGenerated = true;

            string cchsStr = "";
            if (_ticketingRules.IsUsingCCHSSam())
            {
                FldsInitialiseBankTopup flds = new FldsInitialiseBankTopup();
                flds.bankTopupAmount = _autoTopupDetails.ReloadAmount;               
                
                //_autoTopupDetails.BankType;
                flds.bankAccountNumber = new BankAccountNumber_t(_autoTopupDetails.AccountNumber); // TODO: May be it should be .BankNumber
                flds.bankIndicator = new BankIndicator_t(_autoTopupDetails.BankNumber);
                flds.bankTopupRefNumberNumber_t = new BankTopupRefNumberNumber_t(_autoTopupDetails.BankTopupReferenceNumber);
                flds.accountHolderIDCountryCode = _autoTopupDetails.AccountHolderICountryCode;
                flds.accountHolderIDType = _autoTopupDetails.AccountHolderIdType;
                flds.accountHolderName = new PatronName_t(_autoTopupDetails.AccountHolderName);
                flds.accountHolderPersonalID = new PersonalID_Number_t(_autoTopupDetails.AccountHolderId);
                
                //flds.accountType = new AccountType_t("");
                //_autoTopupDetails.CardHolderCountryCode

                cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp,
                    TransactionType.InitialiseBankTopup,                    
                    flds,
                    false, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
            }
            return Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
        }

        public override Tuple<string, string> GetXmlStringToSendToMMI()
        {
            if (_xmlStringToSendToMMI == null)
            {
                string cchsStr = "";
                if (_ticketingRules.IsUsingCCHSSam())
                {
                    cchsStr = SmartFunctions.Instance.GetTDforCCHSGen(_logicalMediaUpdatedForCurrentOp,
                        TransactionType.EnableBankTopup,                        
                        null,
                        _statusAreaDone != Status.Success, _logicalMediaUpdatedForCurrentOp.Application.TransportApplication.Test);
                }
                _xmlStringToSendToMMI = Tuple.New(_logicalMediaUpdatedForCurrentOp.ToXMLString(), cchsStr);
            }
            return _xmlStringToSendToMMI;
        }

        public override MediaOpGen.ResultLastAttempt CorrectMediaAppeared()
        {
            _ticketingRules.TreatmentOnCardDetection2(false, true);
            var err = _ticketingRules.ErrorForJustProducedMedia;
            if (err == TTErrorTypes.CannotReadTheCardBecauseItIsNotInFieldNow || err == TTErrorTypes.CannotReadTheCard)
                return ResultLastAttempt.MediaCouldntBeRead;
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
                    Debug.Assert(false);
                    return ResultLastAttempt.MediaNoMoreFitForOperation;
                }
            }
            _logicalMediaUpdatedForCurrentOp = new LogicalMedia(_logicalMediaPriorToOperation.ToXMLString());
            _logicalMediaPriorToOperation.DESFireDelhiLayout.Hidden = true;
            SalesRules.EnableBankTopupUpdate(_logicalMediaUpdatedForCurrentOp, _autoTopupDetails);
            CommonHwMedia hwCsc = _ticketingRules.hwCsc;
            if (_statusAreaDone == Status.NotInitiated || _statusAreaDone == Status.DM1_WrittenButFailed)
            {
                if (!hwCsc.WriteLocalValidationData(_logicalMediaUpdatedForCurrentOp))
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
            else
            {
                throw new Exception("Unexpected");
            }
        }
    }
}
