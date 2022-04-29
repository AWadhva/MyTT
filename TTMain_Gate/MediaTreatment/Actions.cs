namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public enum ActionTaken
    {
        CheckInPermitted,
        CheckInNotPermitted_RejectCodePutByMe,
        CheckInNotPermitted_RejectCodeAlreadyPresent,
        CheckInNotPermitted_LetsFinalizeTheseCodesLater,
        
        CheckOutPermitted,
        CheckOutNotPermitted_RejectCodePutByMe,
        CheckOutNotPermitted_RejectCodeAlreadyPresent,

        AutoToppedUp,
        Blocked_ie_BlackListedByMe,
        AlreadyBlocked_ie_BlackListed,
        
        ProblemWhileRW
    }
}