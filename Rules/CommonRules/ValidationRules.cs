using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;

using Purpose = IFS2.Equipment.Common.MediaDetectionTreatment;

// ANY Purpose represented by MediaDetectionTreatment.None
// ANY family represended by -1
// ANY fare mode represented by FareModePriv.Any

namespace IFS2.Equipment.TicketingRules
{
    class Purpose_Family
    {
        public Purpose purpose;
        public int family;        

        public override bool Equals(object o)
        {
            Purpose_Family other = (Purpose_Family)o;
            return other.purpose == purpose && other.family == family;
        }

        public override int GetHashCode()
        {
            return ((int)purpose) * 1000 + family;
        }
    }    
    
    class Purpose_Family_Mode
    {
        public Purpose purpose;
        public int family;
        public FareMode mode;        

        public override bool Equals(object o)
        {
            Purpose_Family_Mode other = (Purpose_Family_Mode)o;
            return other.purpose == purpose && other.family == family && other.mode == mode;
        }

        public override int GetHashCode()
        {
            return (int)mode*1000000 + ((int)purpose) * 1000 + family;
        }
    }

    static public partial class ValidationRules
    {
        static internal IMacCalcultor macCalculator;

        static ValidationRules()
        {
            AddValidateRule_All(Rules.AllPurpose.CheckIfMediaIsBlocked, Rules.AllPurpose.CheckMediaExpiry, Rules.AllPurpose.CheckForLastOperationEquipmentBlacklisted, Rules.AllPurpose.EF_CSCC_ControlTicketNotSurrendered, Rules.AllPurpose.EF_TOM_ControlCSCCIssuanceData, Rules.AllPurpose.CheckForMediaBlackList);
            AddValidateRule_ATreatmentType_AllFamilies(MediaDetectionTreatment.CheckIn, Rules.CheckIn.AllFamilies.CheckForOpenFareProduct, Rules.CheckIn.AllFamilies.CheckFareModeIsNotIncident);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckIn, 10, Rules.CheckIn.Fam010.AllModes.SaleStationIsSameAsThisOne, Rules.CheckIn.Fam010.AllModes.VerifyMac);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 10, FareMode.Normal, Rules.CheckIn.Fam010.Normal.CheckTokenIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 10, FareMode.EEO, Rules.CheckIn.Fam010.EEO.CheckTokenIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 10, FareMode.TMO, Rules.CheckIn.Fam010.TMO.CheckTokenIsIssued);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckIn, 40, Rules.CheckIn.Fam040.AllModes.VerifyMac);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckIn, 60, Rules.CheckIn.Fam060.AllModes.CheckCSCIsIssued, Rules.CheckIn.Fam060.AllModes.EF_CSCC_ControlRejectCode);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 60, FareMode.Normal, Rules.CheckIn.Fam060.Normal.CheckEntryExitBit, Rules.CheckIn.Fam060.Normal.TestFlagIsCompatibleWithEqptMode, Rules.CheckIn.Fam060.Normal.PurseValueIsAboveThreshold);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 60, FareMode.EEO, Rules.CheckIn.Fam060.EEO.TestFlagIsCompatibleWithEqptMode, Rules.CheckIn.Fam060.EEO.PurseValueIsAboveThreshold);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 60, FareMode.TMO, Rules.CheckIn.Fam060.TMO.CheckEntryExitBit, Rules.CheckIn.Fam060.TMO.TestFlagIsCompatibleWithEqptMode, Rules.CheckIn.Fam060.TMO.PurseValueIsAboveThreshold);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckIn, 80, Rules.CheckIn.Fam080.AllModes.CheckCSCIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 80, FareMode.Normal, Rules.CheckIn.Fam080.Normal.TestFlagIsCompatibleWithEqptMode);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 80, FareMode.EEO, Rules.CheckIn.Fam080.EEO.TestFlagIsCompatibleWithEqptMode);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckIn, 80, FareMode.TMO, Rules.CheckIn.Fam080.TMO.TestFlagIsCompatibleWithEqptMode);
            AddValidateRule_ATreatmentType_AllFamilies(MediaDetectionTreatment.CheckOut, Rules.CheckOut.AllFamilies.CheckForOpenFareProduct);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckOut, 10, Rules.CheckOut.Fam010.AllModes.VerifyMac);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 10, FareMode.Normal, Rules.CheckOut.Fam010.Normal.CheckTokenIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 10, FareMode.EEO, Rules.CheckOut.Fam010.EEO.CheckTokenIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 10, FareMode.TMO, Rules.CheckOut.Fam010.TMO.CheckTokenIsIssued);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckOut, 40, Rules.CheckOut.Fam040.AllModes.VerifyMac);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckOut, 60, Rules.CheckOut.Fam060.AllModes.CheckCSCIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 60, FareMode.Normal, Rules.CheckOut.Fam060.Normal.CheckEntryExitBit, Rules.CheckOut.Fam060.Normal.TestFlagIsCompatibleWithEqptMode, Rules.CheckOut.Fam060.Normal.CheckTravelTimeIsNotExceeded);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 60, FareMode.EEO, Rules.CheckOut.Fam060.EEO.TestFlagIsCompatibleWithEqptMode, Rules.CheckOut.Fam060.EEO.CheckTravelTimeIsNotExceeded);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 60, FareMode.TMO, Rules.CheckOut.Fam060.TMO.CheckEntryExitBit, Rules.CheckOut.Fam060.TMO.TestFlagIsCompatibleWithEqptMode);
            AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment.CheckOut, 80, Rules.CheckOut.Fam080.AllModes.CheckCSCIsIssued);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 80, FareMode.Normal, Rules.CheckOut.Fam080.Normal.TestFlagIsCompatibleWithEqptMode, Rules.CheckOut.Fam080.Normal.CheckEntryExitBit, Rules.CheckOut.Fam080.Normal.CheckTravelTimeIsNotExceeded);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 80, FareMode.EEO, Rules.CheckOut.Fam080.EEO.TestFlagIsCompatibleWithEqptMode, Rules.CheckOut.Fam080.EEO.CheckTravelTimeIsNotExceeded);
            AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment.CheckOut, 80, FareMode.TMO, Rules.CheckOut.Fam080.TMO.TestFlagIsCompatibleWithEqptMode, Rules.CheckOut.Fam080.TMO.CheckEntryExitBit);
        }
        internal delegate TTErrorTypes // ANUJ: TODO: Ideally we should have a separate set of error codes for validation. We should not mix Validation errors with TTErrorTypes
            ValRule (LogicalMedia logMedia);
        internal delegate void UpdateRule(LogicalMedia read);
        #region Rules        
        static Dictionary<Purpose, List<ValRule>> rulesForPurpose = new Dictionary<MediaDetectionTreatment, List<ValRule>>();
        static Dictionary<Purpose, List<UpdateRule>> rules2ForPurpose = new Dictionary<MediaDetectionTreatment, List<UpdateRule>>();

        static Dictionary<Purpose_Family, List<ValRule>> rules_PerPurpose_PerFamily = new Dictionary<Purpose_Family, List<ValRule>>();
        static Dictionary<Purpose_Family, List<UpdateRule>> rules2_PerPurpose_PerFamily = new Dictionary<Purpose_Family, List<UpdateRule>>();

        static Dictionary<Purpose_Family_Mode, List<ValRule>> rules_PerPurpose_PerFamily_PerMode = new Dictionary<Purpose_Family_Mode, List<ValRule>>();
        static Dictionary<Purpose_Family_Mode, List<UpdateRule>> rules2_PerPurpose_PerFamily_PerMode = new Dictionary<Purpose_Family_Mode, List<UpdateRule>>();
        #endregion

        /// <summary>
        /// Level 1: ANY type of {purpose, family, fareMode}
        /// Level 2: Treatment specific for 'purpose', but with ANY {family, fareMode}
        /// Level 3: Treatment specific for {purpose, family} but with ANY faremode
        /// Level 4: Treatment specific for {purpose, family, faremode}
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="read"></param>
        /// <param name="toWrite"></param>
        /// <returns></returns>
        static public TTErrorTypes ValidateFor(MediaDetectionTreatment purpose, LogicalMedia logMedia)
        {
            {
                var rules = new List<ValRule>();                
                
                List<ValRule> x;                

                if (rulesForPurpose.TryGetValue(MediaDetectionTreatment.None, out x))
                    rules.AddRange(x);                
                
                if (rulesForPurpose.TryGetValue(purpose, out x))
                    rules.AddRange(x);

                var result = Execute(logMedia, rules);
                if (result != TTErrorTypes.NoError)
                    return result;
            }
            int product = logMedia.Application.Products.Product(0).TypeRead;
            if (product == 0)
                return TTErrorTypes.NoProduct;

            int family = ProductParameters.GetProductFamily(product);
            if (!SupportedFamilies.Contains(family))
                return TTErrorTypes.NotSupportedProductFamily;

            {
                var rules = new List<ValRule>();       
                
                var x = new Purpose_Family();
                x.purpose = purpose;

                x.family = family; // specific family of the fare product present of media
                rules.AddRange(rules_PerPurpose_PerFamily[x]);

                var y = new Purpose_Family_Mode();
                y.purpose = purpose;
                y.family = family;
                y.mode = FareMode;
                rules.AddRange(rules_PerPurpose_PerFamily_PerMode[y]);

                var result = Execute(logMedia, rules);
                if (result != TTErrorTypes.NoError)
                    return result;
            }

            return TTErrorTypes.NoError;
        }

        readonly static List<int> SupportedFamilies = new List<int>{ 60, 80 };

        internal static void AddValidateRule_All(params ValRule [] rules)
        {
            rulesForPurpose[MediaDetectionTreatment.None] = rules.ToList();
        }
        
        internal static void AddValidateRule_ATreatmentType_AllFamilies(MediaDetectionTreatment purpose, params ValRule[] rules)
        {
            rulesForPurpose[purpose] = rules.ToList();
        }

        internal static void AddValidateRule_ATreatmentType_AFamily_AllFareModes(MediaDetectionTreatment purpose, int family, params ValRule[] rules)
        {
            Purpose_Family x = new Purpose_Family();
            x.purpose = purpose;
            x.family = family;
            
            rules_PerPurpose_PerFamily[x] = rules.ToList();
        }

        internal static void AddValidateRule_ATreatmemntType_AFamily_AFareMode(MediaDetectionTreatment purpose, int family, FareMode fareMode, params ValRule[] rules) 
        {
            Purpose_Family_Mode x = new Purpose_Family_Mode();
            x.purpose = purpose;
            x.family = family;
            x.mode = fareMode;

            rules_PerPurpose_PerFamily_PerMode[x] = rules.ToList();
        }        

        static TTErrorTypes Execute(LogicalMedia read, List<ValRule> rules)
        {
            foreach (var rul in rules)
            {
                var error = rul(read);
                if (error != TTErrorTypes.NoError)
                    return error;
            }
            
            return TTErrorTypes.NoError;
        }

        static public void SetMacCalculator(IMacCalcultor calc_)
        {
            macCalculator = calc_;
        }

        public static void UpdateForCheckIn(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;
            
            validation.EntryExitBit = Validation.TypeValues.Entry;
            validation.LastTransactionDateTime = DateTime.Now;
            validation.Location = SharedData.StationNumber;

            // TODO: see if we need to put PeriodicTicketEntry for family 80
            SalesRules.AddTrasactionHistoryRecord(logMedia, OperationTypeValues.NoValueDeductedInEntry, 0);
        }

        public static void UpdateForCheckOut(LogicalMedia logMedia)
        {
            var validation = logMedia.Application.Validation;
            int productType = logMedia.Application.Products.Product(0).Type;
            
            int notUsed;
            int fare = SalePriceCalculation.CalculatePriceSiteBased(productType, validation.LocationRead, SharedData.StationNumber, validation.LastTransactionDateTimeRead, out notUsed);
            
            validation.EntryExitBit = Validation.TypeValues.Exit;
            validation.LastTransactionDateTime = DateTime.Now;
            validation.Location = SharedData.StationNumber;            
            
            logMedia.Purse.TPurse.Balance = logMedia.Purse.TPurse.BalanceRead - fare;

            // TODO: see if we need to put PeriodicTicketExit for family 80
            SalesRules.AddTrasactionHistoryRecord(logMedia, OperationTypeValues.ValueDeductedInExit, fare);
        }
    }
}