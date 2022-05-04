namespace IFS2.Equipment.TicketingRules.MediaTreatment
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
        
        ProblemWhileRW
    }
}