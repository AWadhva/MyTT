namespace IFS2.Equipment.TicketingRules.Gate.MediaTreatment
{
    public enum ActionTaken
    {
        CheckInPermitted,
        CheckInNotPermitted_RejectCodePutByMe,
        CheckInNotPermitted,
        
        CheckOutPermitted,
        CheckOutNotPermitted_RejectCodePutByMe,
        CheckOutNotPermitted,        

        AutoToppedUp,
        Blocked_ie_BlackListedByMe,
        AlreadyBlocked_ie_BlackListed,

        GoodAgentCardDetected,
        BadAgentCardDetected,
        
        ProblemWhileRW
    }
}