using IFS2.Equipment.Common;
using System;
namespace IFS2.Equipment.TicketingRules
{    
    interface IUpdateMediaOp
    {
        MediaOpGen.ResultLastAttempt CorrectMediaAppeared();
        long? GetMediaSerialNumber();
        MediaUpdateCompletionStatus GetStatus();
        void DeclarePartCompletedAsDone();
        Tuple<string, string> GetXmlStringToSendToMMI();
        MediaOpType GetOpType();
        bool bIsOpCompletedEvenPartly();
        bool IsAudited();
        void SetAudited();    
        bool bAtLeastSomethingMayHaveGotWrittenInLastAttempt();
    }

    interface IMediaCancellableOp
    {
        Tuple<string, string> GetXmlStringToSendToMMIOnCancellation();
        MediaOpGen.ResultLastCancelAttempt CorrectMediaForCancellationAppeared();        
        MediaOpGen.ResultLastCancelAttempt GetLastCancelAttempt();
    }

    interface IUpdateMediaNonPreRegisteredOp : IUpdateMediaOp
    {
        void SetMediaSerialNumber(long sNum);
        bool DoesNeedTokenDispenser();
    }

    interface IUpdateMediaPreRegisteredOp : IUpdateMediaOp { }
}