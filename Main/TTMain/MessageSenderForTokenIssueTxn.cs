using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    class MessageSenderForTokenIssueTxn : IMessageSenderForIssueTxn
    {
        #region IMessageSenderForIssueTxn Members

        public void ThrowToBin()
        {
            MainTicketingRules.SendMsg.ThrowToken(MainTicketingRules.SendMsg.ThrowTo.Bin);
            MainTicketingRules.SendMsg.TokenInBin();
        }

        public void PutMediaUnderRW()
        {
            MainTicketingRules.SendMsg.PutTokenUnderRW();
        }

        public int GetTimeoutInMilliSecForPutMediaUnderRWCompletion()
        {
            return Config.nTimeOutInMilliSecForPutTokenUnderRWCompletion;
        }
        
        public int GetTimeoutInMilliSecForThrowMediaToBinCompletion()
        {
            return Config.nTimeOutInMilliSecForThrowTokenToBinRequestCompletion;
        }

        public void MediaDistributionHaltedDueToSomeProblem()
        {
            MainTicketingRules.SendMsg.TokenError();
        }

        public void StopMediaDistributionAck()
        {
            MainTicketingRules.SendMsg.StopTokenDistributionAck();
        }

        public int GetMaxTimeInMilliSecToGiveVendedMediaToLeaveFieldAfterReceivingPositiveThrowMediaToOTAck()
        {
            return Config.nMaxTimeInMilliSecToGiveVendedTokenToLeaveFieldAfterReceivingPositiveThrowTokenToOTAck;
        }

        #endregion
    }
}
