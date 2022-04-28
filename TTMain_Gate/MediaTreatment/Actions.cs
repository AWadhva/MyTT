namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public enum ActionTaken
    {
        CheckInPermitted,
        CheckInNotPermitted_RejectCodePutByMe,
        CheckInNotPermitted_RejectCodeAlreadyPresent,
        
        CheckOutPermitted,
        CheckOutNotPermitted_RejectCodePutByMe,
        CheckOutNotPermitted_RejectCodeAlreadyPresent,

        AutoToppedUp,
        Blocked_ie_BlackListedByMe,
        AlreadyBlocked_ie_BlackListed,
        //RejectCodePutByMe,
        //RejectCodeAlreadyPresent,
        ProblemWhileRW
    }
}