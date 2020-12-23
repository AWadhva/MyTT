using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    class MessageSenderForCSCIssueTxn : IMessageSenderForIssueTxn
    {
        #region IMessageSenderForIssueTxn Members

        public void ThrowToBin()
        {
            MainTicketingRules.SendMsg.ThrowCSC(MainTicketingRules.SendMsg.ThrowTo.Bin);
            MainTicketingRules.SendMsg.CardInBin();
        }

        public void PutMediaUnderRW()
        {
            MainTicketingRules.SendMsg.PutCSCUnderRW();
        }

        public int GetTimeoutInMilliSecForPutMediaUnderRWCompletion()
        {
            return Config.nTimeOutInMilliSecForPutCSCUnderRWCompletion;
        }

        public int GetTimeoutInMilliSecForThrowMediaToBinCompletion()
        {
            return Config.nTimeOutInMilliSecForThrowCSCToBinRequestCompletion;
        }

        public void MediaDistributionHaltedDueToSomeProblem()
        {
            MainTicketingRules.SendMsg.CSCDistributionError();
        }

        public void StopMediaDistributionAck()
        {
            MainTicketingRules.SendMsg.StopCSCDistributionAck();
        }

        public int GetMaxTimeInMilliSecToGiveVendedMediaToLeaveFieldAfterReceivingPositiveThrowMediaToOTAck()
        {
            return 10 * 1000;
        }

        #endregion
    }
}
