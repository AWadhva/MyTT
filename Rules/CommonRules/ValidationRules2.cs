namespace IFS2.Equipment.TicketingRules
{
    public enum FareMode { Normal = 1, EEO = 2, TMO = 3, Incident = 4 }; // TODO: Check for these values
    static public partial class ValidationRules
    {
        
        public static void SetFareMode(FareMode p)
        {
            FareMode = p;
        }
        public static FareMode GetFareMode()
        {
            return FareMode;
        }
        static FareMode FareMode = FareMode.Normal;
    }
}