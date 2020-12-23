using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    interface IMessageSenderForIssueTxn
    {
        void ThrowToBin();
        void PutMediaUnderRW();
        int GetTimeoutInMilliSecForPutMediaUnderRWCompletion();
        int GetTimeoutInMilliSecForThrowMediaToBinCompletion();
        void MediaDistributionHaltedDueToSomeProblem();

        void StopMediaDistributionAck();

        int GetMaxTimeInMilliSecToGiveVendedMediaToLeaveFieldAfterReceivingPositiveThrowMediaToOTAck();
    }
}
