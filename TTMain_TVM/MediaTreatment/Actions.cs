namespace IFS2.Equipment.TicketingRules.MediaTreatment.TVM
{
    public enum ActionTaken
    {
        AddValueDone,

        Blocked_ie_BlackListedByMe,
        AlreadyBlocked_ie_BlackListed,

        GoodAgentCardDetected,
        BadAgentCardDetected,
        
        ProblemWhileRW
    }
}