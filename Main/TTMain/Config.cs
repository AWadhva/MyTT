using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;


namespace IFS2.Equipment.TicketingRules
{
    static class Config
    {
        static Config()
        {
            bRestartFieldForEachMediaDispense = Configuration.ReadBoolParameter("RestartFieldForEachMediaDispense", false);
            nToken_MAX_TRIALS_FOR_DETECTION = (int)Configuration.ReadParameter("Token_MAX_TRIALS_FOR_DETECTION", "int", "10");
            nSleepIntervalInMSecs_TokenNonDetection = (int)Configuration.ReadParameter("nSleepIntervalInMSecs_TokenNonDetection", "int", "100");            
            SmartIsoRetries = (int)Configuration.ReadParameter("SmartIsoRetries", "int", "3");
            nTimeOutInMilliSecForThrowTokenToBinRequestCompletion = (int)Configuration.ReadParameter("TimeOutInMilliSecForThrowTokenToBinRequestCompletion", "int", "10000");
            nTimeOutInMilliSecForThrowCSCToBinRequestCompletion = Configuration.ReadIntParameter("TimeOutInMilliSecForThrowCSCToBinRequestCompletion", 10000);
            nTimeOutInMilliSecForThrowToOTCompletion = (int)Configuration.ReadParameter("TimeOutInMilliSecForPutTokenUnderRWCompletion", "int", "10000");
            nTimeOutInMilliSecForPutTokenUnderRWCompletion = (int)Configuration.ReadParameter("TimeOutInMilliSecForPutTokenUnderRWCompletion", "int", "150000");
            nTimeOutInMilliSecForPutCSCUnderRWCompletion = Configuration.ReadIntParameter("TimeOutInMilliSecForPutCSCUnderRWCompletion", 60000);
            nTimeInMilliSecToLetEjectedTokenFromDispenserSettleProperlyInRFField = (int)Configuration.ReadParameter("TimeInMilliSecToWaitBeforeProcessingEjectedTokenFromDispenser", "int", "300");
            nTotalTimeInMilliSecToLetTTAttemptWriteToTokenFromTD = (int)Configuration.ReadParameter("TotalTimeInMilliSecToLetTTAttemptWriteToTokenFromTD", "int", "3000");
            nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD = (int)Configuration.ReadParameter("TimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD", "int", "50");
            _nTimeToSleepAfterRemovalOfMediaToPollAgain = (int)Configuration.ReadParameter("TimeToSleepAfterRemovalOfMediaToPollAgain", "int", "30");
            _nTimeInMilliSecToLetTokenGettingDetectedAfter_PutTokenUnderRWPositiveAck = (int)Configuration.ReadParameter("TimeInSecToLetTokenGettingDetectedAfter_PutTokenUnderRWPositiveAck", "int", "3")*1000;
            nTimeInMilliSecToLetTokenSettleInFieldAfterGettingDispensedFromContainer = (int)Configuration.ReadParameter("TimeInMilliSecToLetTokenSettleInFieldAfterGettingDispensedFromContainer", "int", "1");
            nTimeInMilliSecToKeepMediaHaltedAfterDisappearance = Configuration.ReadIntParameter("TimeInMilliSecToKeepMediaHaltedAfterDisappearance", 2000);
            nMaxTimeInMilliSecToGiveVendedTokenToLeaveFieldAfterReceivingPositiveThrowTokenToOTAck = (int)Configuration.ReadParameter("MaxTimeInMilliSecToGiveVendedTokenToLeaveFieldAfterReceivingPositiveThrowTokenToOTAck", "int", "5000");
            _bEODReceivedInXml = Configuration.ReadBoolParameter("EODReceivedInXmlFromCC", false);
            _bCoreCommonUsed = Configuration.ReadBoolParameter("CoreCommonFunctionalityUsed", false);
            _bTreatTicketSaleParameterInEOD = Configuration.ReadBoolParameter("TreatTicketSaleParameterInEOD", false);
            _bTreatTVMEquipmentParametersInEOD = Configuration.ReadBoolParameter("TreatTVMEquipmentParametersInEOD", true);
            _bSignCurrentXmlParameter = Configuration.ReadBoolParameter("SignCurrentXmlParameter", false);



            _bCheckForMediaExpiry = (bool)Configuration.ReadParameter("CheckForMediaExpiry", "bool", "false");
            _bTreatementOnCardDetectionOldStyle = (bool)Configuration.ReadParameter("TreatementOnCardDetectionOldStyle", "bool", "true");
            _bGenerateRejectionCSCTransactions = (bool)Configuration.ReadParameter("GenerateRejectionCSCTransactions", "bool", "false");
            _bCheckForCurrentBusinessDaySoldToken = (bool)Configuration.ReadParameter("CheckForCurrentBusinessDaySoldToken", "bool", "true");
            _bUseCallbackForMediaDetectionNRemoval = (bool)Configuration.ReadParameter("UseCallbackForDetection", "bool", "false");
            _nTimeSpanInMinutesForAllowingSJTRefund = (int)IFS2.Equipment.Common.Parameters.ReadParameter("TimeSpanInMinutesForAllowingSJTRefund", "int", "60");
            nMaxTimeInMilliSecBeforeRestartingFieldInCaseOfWTEOrRTE = Configuration.ReadIntParameter("MaxTimeInMilliSecBeforeRestartingFieldInCaseOfWTEOrRTE", 500);            
            nPingFrequencyInMilliSeconds = Configuration.ReadIntParameter("PingFrequencyInMilliSeconds", 1000);
            IsCannotReadCardErrorCodeMoreGeneric = Configuration.ReadBoolParameter("IsCannotReadCardErrorCodeMoreGeneric", true);
            ALLOW_REFUNDED_CSC_BEISSUED = Configuration.ReadBoolParameter("ALLOW_REFUNDED_CSC_BEISSUED", false);
            Allow_UsableSJTSBeIssuedAgainOnSameBusinessDayAfter_10_Mins = Configuration.ReadBoolParameter("Allow_UsableSJTSBeIssuedAgainOnSameBusinessDayAfter_10_Mins", false);
            isCardStatusOverride_Issue_Replace = (bool)Configuration.ReadParameter("IsCardStatusOverride_Issue_Replace", "bool", "false");

            _AttemptToResetTokenDispenserOnOutJam = (bool)Configuration.ReadParameter("AttemptToResetTokenDispenserOnOutJam", "bool", "true");
            bTTagFunc = (bool)Configuration.ReadParameter("TTagFunc", "bool", "true");
            int TimeInSecToPingRearReader = (int)Configuration.ReadParameter("TimeInSecToPingRearReader", "int", "5");
            TimeSpanPingRearReader = new TimeSpan(0, 0, TimeInSecToPingRearReader);

            byte b = (byte)(Configuration.ReadParameter("RFPowerRearReader", "byte", Byte.MaxValue.ToString()));
            if (b == Byte.MaxValue)
                _rfPowerRearReader = null;
            else
                _rfPowerRearReader = b;

            MaxPenaltyAmountInPaise = (int)IFS2.Equipment.Common.Parameters.ReadParameter("MaxPenaltyAmountInPaise", "int", "5000");
            TimeoutNoMediaProduced_InV3_WhenInVirtualDetectionMode_InMilliSecs = Configuration.ReadIntParameter("TimeoutNoMediaProduced_InV3_WhenInVirtualDetectionMode_InMilliSecs", 6000);            
            bCleanedUp = Configuration.ReadBoolParameter("CleanedUp", false);
            bTokenFallsDirectlyOnReader = Configuration.ReadBoolParameter("TokenFallsDirectlyOnReader", true);
            MaxCountOfMediasTreatedRecentlyToMaintain = Configuration.ReadIntParameter("MaxCountOfMediasTreatedRecentlyToMaintain", 10);
            MaxTime_InMillisec_ToLetLastMediaExhaustiveDetectionRemovalWhenInLoginState = Configuration.ReadIntParameter("MaxSecs_ToLetLastMediaExhaustiveDetectionRemovalWhenInLoginState", 0);

            _VirtualSiteId = new Dictionary<int, int>();
            for (int siteId = 1; siteId <= 500; siteId++)
            {
                int virtSiteId = Configuration.ReadIntParameter("VirtualSiteId_" + siteId, siteId);
                _VirtualSiteId[siteId] = virtSiteId;
            }
            bAllowRollbackingAddValue = Configuration.ReadBoolParameter("AllowRollbackingAddValue", false);
            bAllowRollbackingTokenIssue = Configuration.ReadBoolParameter("AllowRollbackingTokenIssue", true);

            bCheckCSCStatusInDM2ForAddValue = (bool)Configuration.ReadParameter("CheckCSCStatusInDM2ForAddValue", "bool", "true");
        }

        static public readonly bool bCheckCSCStatusInDM2ForAddValue;
        static public readonly Dictionary<int, int> _VirtualSiteId;
        static public readonly bool bAllowRollbackingTokenIssue;
        static public readonly bool bAllowRollbackingAddValue;
        static public readonly bool bRestartFieldForEachMediaDispense;
        static public readonly int MaxCountOfMediasTreatedRecentlyToMaintain;
        static public readonly bool bTokenFallsDirectlyOnReader;
        static public readonly int TimeoutNoMediaProduced_InV3_WhenInVirtualDetectionMode_InMilliSecs;
        static public readonly int MaxPenaltyAmountInPaise; // TODO: Tried to locate it inside the EOD but couldn't
        static public readonly bool ALLOW_REFUNDED_CSC_BEISSUED;
        static public readonly bool Allow_UsableSJTSBeIssuedAgainOnSameBusinessDayAfter_10_Mins;        
        static public readonly int MaxTime_InMillisec_ToLetLastMediaExhaustiveDetectionRemovalWhenInLoginState;
        static public readonly bool bCleanedUp;

        static public readonly bool _bTreatementOnCardDetectionOldStyle;
        static public readonly bool _bGenerateRejectionCSCTransactions;
        static public readonly bool _bCheckForCurrentBusinessDaySoldToken;
        static public readonly int nToken_MAX_TRIALS_FOR_DETECTION;
        static public readonly int nSleepIntervalInMSecs_TokenNonDetection;
        static public readonly int nTimeOutInMilliSecForThrowTokenToBinRequestCompletion, 
            nTimeOutInMilliSecForThrowCSCToBinRequestCompletion,
            nTimeOutInMilliSecForPutTokenUnderRWCompletion,
            nTimeOutInMilliSecForPutCSCUnderRWCompletion,
            nTimeOutInMilliSecForThrowToOTCompletion,
            nTimeInMilliSecToLetEjectedTokenFromDispenserSettleProperlyInRFField,
            nTotalTimeInMilliSecToLetTTAttemptWriteToTokenFromTD,
            nTimeInMilliSecForTwoSuccessiveAttemptsToMakeOperationOnTokenFromTD,
            nTimeInMilliSecToLetTokenSettleInFieldAfterGettingDispensedFromContainer,
            _nTimeInMilliSecToLetTokenGettingDetectedAfter_PutTokenUnderRWPositiveAck,
            nMaxTimeInMilliSecToGiveVendedTokenToLeaveFieldAfterReceivingPositiveThrowTokenToOTAck,
            nMaxTimeInMilliSecBeforeRestartingFieldInCaseOfWTEOrRTE,
            nPingFrequencyInMilliSeconds
            ;
        static public readonly TimeSpan TimeSpanPingRearReader;
        static public readonly int _nTimeSpanInMinutesForAllowingSJTRefund;
        static public readonly bool bTTagFunc;
        static public readonly byte? _rfPowerRearReader;

        static public readonly bool isCardStatusOverride_Issue_Replace; // Copied from IERS.

        // Copied this from middleware packaged with Token Dispenser. Value used there is 3. Though I don't think that this variable 
        // is useful in anyway, means if reading/writing failed for 1st instatnce, then how can it be successful for subsequent retries. 
        // Will have to see, if it can be set to 1.
        static public readonly int SmartIsoRetries;
        static public readonly bool _bCheckForMediaExpiry;
        static public readonly bool _bUseCallbackForMediaDetectionNRemoval;
        static public readonly int _nTimeToSleepAfterRemovalOfMediaToPollAgain;
        static public readonly bool _AttemptToResetTokenDispenserOnOutJam;

        static public readonly bool _bEODReceivedInXml;
        static public readonly bool _bCoreCommonUsed;
        static public readonly bool _bTreatTicketSaleParameterInEOD;
        static public readonly bool _bTreatTVMEquipmentParametersInEOD;
        static public readonly bool _bSignCurrentXmlParameter;
        static public readonly int nTimeInMilliSecToKeepMediaHaltedAfterDisappearance;        
        static public readonly bool IsCannotReadCardErrorCodeMoreGeneric;        
    }
}