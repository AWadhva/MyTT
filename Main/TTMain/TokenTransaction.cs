using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Linq;
#if WindowsCE
using OpenNETCF.Threading;
#endif
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    // Base treatment for Ticketing Rules
    public partial class MainTicketingRules
    {
        class MediaDistributionTransaction
        {
            public int _cntMediaLeftToBeDistributed = 0;
            public readonly int _cntMediaRequested = 0;
            public string _logicalData_FromMMI = null;

            public int _cntMediaSentToBinForCurrentMedia_SinceTheyWereUnReadable = 0; // note that it is a somewhat misfit here, but good thing is that it for sure gets reset (in fact removed) just by putting the only reference to null
            public int _cntMediaSentToBinForCurrentMedia_DueToAnyReasonIncUnreadable = 0; // note that it is a somewhat misfit here, but good thing is that it for sure gets reset (in fact removed) just by putting the only reference to null
            public SendMsg.ThrowTo? _throwTo = null;
            public string _currentTokenLogicalData = null;
            public long _currentMediaPhysicalId
            {
                get
                {
                    if (_currentTokenLogicalData == null)
                        return 0;
                    else
                    {
                        return Utility.TagValueLong(_currentTokenLogicalData, "Media>", "CSN>", "</CSN");
                    }
                }
            }

            public Tuple<string, string> _currentCSCLogicalDataAfterIssueOp = null;
            public Tuple<string, string> _currentCSCLogicalDataAfterAddValueOp = null;

            public bool _bPutMediaUnderRWSent = false;

            public string _hopperId = "0";

            public enum MediaType {Token, CSC};
            public readonly MediaType _mediaType;
#if !_HHD_
            public IMessageSenderForIssueTxn _sender;
#endif
            public short CSCIssue_ProductId = 0;
            public int CSCIssue_PurseValueAsked = 0;
            public PaymentMethods PaymentMode = PaymentMethods.Cash; // TODO: Correct it after we accept this parameter from MMI
            public Customer.LanguageValues _language = Customer.LanguageValues.Hindi;
            public IFS2.Equipment.Common.CCHS.BankTopupDetails _bankTopupDetails = null;

            public MediaDistributionTransaction(MediaType typ, int cntMediaAsked)
            {
                #if !_HHD_
                if (typ == MediaType.Token)
                    _sender = new MessageSenderForTokenIssueTxn();
                else if (typ == MediaType.CSC)
                    _sender = new MessageSenderForCSCIssueTxn();
#endif
                _mediaType = typ;
                _cntMediaRequested = _cntMediaLeftToBeDistributed = cntMediaAsked;

            }
        }
    }
}