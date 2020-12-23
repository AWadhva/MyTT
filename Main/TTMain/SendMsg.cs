using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
#if !_BLUEBIRD_
using IFS2.Equipment.CryptoFlex;
#endif
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Linq;
#if WindowsCE || PocketPC
using OpenNETCF.Threading;
#endif
using System.Diagnostics;
using System.Xml.Linq;

namespace IFS2.Equipment.TicketingRules
{
    // Base treatment for Ticketing Rules
    public partial class MainTicketingRules
    {
        static public class SendMsg
        {
            public static void TokenError()
            {
                Communication.SendMessage(ThreadName, "", "TokenError");
            }

            public static void CSCDistributionError()
            {
                Communication.SendMessage(ThreadName, "", "CardError");
            }

            public static void PutTokenUnderRW()
            {
                Communication.SendMessage(ThreadName, "", "PutTokenUnderRW");
            }

            public static void PutCSCUnderRW()
            {
                Communication.SendMessage(ThreadName, "", "MoveCscdAtCtlssPosition");
            }

            public enum ThrowTo { OutputTray = 0, Bin = 1 };
            public static void ThrowToken(ThrowTo to)
            {
                if (to == ThrowTo.Bin)
                    Communication.SendMessage(ThreadName, "", "ThrowTokenToBin");
                else if (to == ThrowTo.OutputTray)
                    Communication.SendMessage(ThreadName, "", "ThrowTokenToOT");
            }

            public static void ThrowCSC(ThrowTo to)
            {
                if (to == ThrowTo.Bin)
                    Communication.SendMessage(ThreadName, "", "ThrowCscInBin");
                else if (to == ThrowTo.OutputTray)
                    Communication.SendMessage(ThreadName, "", "ThrowCscToOut");
            }

            public static void SetThreadName(string thdName, MainTicketingRules parent)
            {
                ThreadName = thdName;
                _parent = parent;
            }
            static MainTicketingRules _parent;
            private static string ThreadName;

            internal static void TokenDistributed(bool bMoreTokensToDistribute, int nTokensAlreadyDistributed, string hopperId, string logicalData)
            {
                Communication.SendMessage(ThreadName, "", "TokenDistributed",
                    String.Format("{0};{1};{2}", bMoreTokensToDistribute ? "0" : "1",
                    nTokensAlreadyDistributed.ToString(),
                    hopperId),
                    logicalData
                    );
            }

            internal static void CSCDistributed(bool bMoreTokensToDistribute, int nCardsAlreadyDistributed, string p_3, string p_4, string p_5, string p_6)
            {
                Communication.SendMessage(ThreadName, "", "CardDistributed",
                    bMoreTokensToDistribute ? "0" : "1",
                    nCardsAlreadyDistributed.ToString(),
                    p_3, p_4, p_5, p_6);
            }

            internal static void TokenInBin()
            {
                Communication.SendMessage(ThreadName, "", "TokenInBin");
            }

            internal static void CardInBin()
            {
                Communication.SendMessage(ThreadName, "", "CardInBin");
            }

            internal static void StopTokenDistributionAck()
            {
                Communication.SendMessage(ThreadName, "", "StopTokenDistributionAck");
            }

            internal static void StopCSCDistributionAck()
            {
                Communication.SendMessage(ThreadName, "", "StopCardDistributionAck");
            }

            internal static void RemoveMedia(ReasonCodeForAskingAgentToRemoveMedia reasonCode)
            {
                Console.WriteLine("RemoveMedia = " + reasonCode.ToString());
                Communication.SendMessage(ThreadName, "", "RemoveMedia", ((int)reasonCode).ToString());
            }

            internal static void RTE_Or_WTE(RTE_Or_WTE reasonCode, long ticketPhysicalId)
            {                
                Communication.SendMessage(ThreadName, "", "RTE_Or_WTE", ((int)reasonCode).ToString(), ticketPhysicalId.ToString());
            }

            internal static void AskedMediaRemoved()
            {
                Console.WriteLine("AskedMediaRemoved");
                Communication.SendMessage(ThreadName, "", "AskedMediaRemoved");
            }

            // would be sent in ALL cases i.e.
            // a. When process got completed successfully
            // b. When process was aborted in-between by agent
            // c. When process couldn't be completed due to some reasson. Following two are identified:
            //      c.1. During or just prior to token transaction, token dispenser got bad
            //
            //In short, before setting _mediaUpdate to null, we send it.
            public static void UpdateMediaTerminated()
            {                
                Communication.SendMessage(ThreadName, "", "UpdateMediaTerminated");
            }

            internal static void UpdateMediaOpAudited(
                int opIdx,
                int opSubIdx,
                bool bWTE,
                string logicalData,
                string xdrDataForCCHS)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaOpAudited",                     
                    opIdx.ToString(),
                    opSubIdx.ToString(),
                    bWTE.ToString(),
                    TrimBadCharactersIfAny(logicalData),
                    xdrDataForCCHS == null ? "" : xdrDataForCCHS);
            }

            internal static void UpdateMediaOpConcluded(int opIdx,
                int opSubIdx)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaOpConcluded",
                    opIdx.ToString(),
                    opSubIdx.ToString());
            }

            internal static void GetTokenPriceAnswer(TTErrorTypes err, int price, string logicalData)
            {
                Communication.SendMessage(ThreadName, "", "GetTokenPriceAnswer", String.Format("{0};{1}", ((int)err).ToString(), price.ToString()), logicalData);
            }

            static readonly List<TTErrorTypes> GeneralErrors = new List<TTErrorTypes>
            {
                TTErrorTypes.CannotReadTheCard,
                TTErrorTypes.Exception,
                TTErrorTypes.LastAddValueDeviceBlacklisted,
                TTErrorTypes.MediaBlocked,
                TTErrorTypes.MediaInDenyList,
                TTErrorTypes.NoProduct,
                TTErrorTypes.NotDMRCCard
            };


            internal static void BadAgentCardDetection(TTErrorTypes err, LogicalMedia logicalMedia, MediaDetectionTreatment readPurpose)
            {
                Communication.SendMessage(ThreadName, "", "BadAgentCardDetection", 
                    ((int)err).ToString(), 
                    logicalMedia.ToXMLString(), 
                    ((int)readPurpose).ToString()
                    );
            }

            internal static void AgentCardDetection(LogicalMedia logicalMedia, MediaDetectionTreatment readPurpose)
            {
                Communication.SendMessage(ThreadName, "", "AgentCardDetection", 
                    ((int)TTErrorTypes.NoError).ToString(), 
                    logicalMedia.ToXMLString(),
                    ((int)readPurpose).ToString()
                    );
            }

            internal static void CSTMediaDetection(TTErrorTypes tTErrorTypes, 
                LogicalMedia logMedia, 
                MediaDetectionTreatment readPurpose, 
                AdjustmentInfo _adjustment, 
                bool bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess)
            {
                string par3;
                if (_adjustment != null)
                    par3 = _adjustment.ToXmlString();
                else if (readPurpose == MediaDetectionTreatment.TOM_AnalysisForRefund)
                    par3 = bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess.ToString();
                else
                    par3 = "";
                Communication.SendMessage(ThreadName, "", "CSTMediaDetection",
                    ((int)tTErrorTypes).ToString(), 
                    logMedia.ToXMLString(), 
                    ((int)readPurpose).ToString(), 
                    par3);
            }

            internal static void MediaRemoved()
            {
                Communication.SendMessage(ThreadName, "", "MediaRemoved");
            }

            internal static void CannotReadToken()
            {
                Communication.SendMessage(ThreadName, "", "CSTMediaDetection", ((int)TTErrorTypes.CannotReadTheCard).ToString());
            }

            internal static void ReadUserCardSummaryAnswer(TTErrorTypes tTErrorTypes)
            {
                Communication.SendMessage(ThreadName, "", "ReadUserCardSummaryAnswer", ((int)tTErrorTypes).ToString());
            }

            internal static void CancelPutTokenUnderRW()
            {
                Communication.SendMessage(ThreadName, "", "CancelPutTokenUnderRW");
            }

            internal static void BadPassengerCardDetection(TTErrorTypes ttErrorTypes, LogicalMedia logicalMedia, string xdrData, MediaDetectionTreatment readPurpose)
            {
                string logMediaStr = TrimBadCharactersIfAny(logicalMedia.ToXMLString());
                bool bXdrExists = (xdrData != "" && xdrData != null);
                Communication.SendMessage(ThreadName, "", "BadPassengerCardDetection", 
                    ((int)ttErrorTypes).ToString(),
                    logMediaStr,
                    (bXdrExists ? "<XdrData>" + xdrData + "</XdrData>" : ""),
                    ((int)readPurpose).ToString()
                    );
            }

            internal static void GetTokenDispenserStatus()
            {
                Communication.SendMessage(ThreadName, "", "GetTokenDispenserStatus");
            }

            internal static void MediaNotFoundFitForOperation(TTErrorTypes tTErrorTypes, LogicalMedia logMedia)
            {
                Communication.SendMessage(ThreadName, "", "MediaNotFoundFitForOperation", ((int)tTErrorTypes).ToString(), logMedia.ToXMLString());
            }

            internal static void UpdateMedia_InitiatingNewOp(int idx, int subIdx)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaInitiatingNewOp", ((int)idx).ToString(), ((int)subIdx).ToString());
            }

            internal static void UpdateMediaOpCantBePerformed(int idx, int subIdx)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaOpCantBePerformed", ((int)idx).ToString(), ((int)subIdx).ToString());
            }

            private static string TrimBadCharactersIfAny(string logicalMedia)
            {
                while (true)
                {
                    try
                    {
                        XDocument.Parse(logicalMedia);
                        break;
                    }
                    catch (XmlException exp)
                    {
                        if (exp.LinePosition > 0)
                            logicalMedia = logicalMedia.Remove(exp.LinePosition - 1, 1);
                        else
                            return null;
                    }
                    catch
                    {
                        return null;
                    }
                }
                return logicalMedia;
            }

            internal static void ReadUserCardSummaryAnswer(TTErrorTypes err, LogicalMedia logicalMedia, MediaDetectionTreatment readPurpose, AdjustmentInfo adj)
            {
                string sLogicalMedia = TrimBadCharactersIfAny(logicalMedia.ToXMLString());
                Communication.SendMessage(ThreadName, "", "ReadUserCardSummaryAnswer",
                    ((int)err).ToString(),
                    sLogicalMedia,
                    ((int)readPurpose).ToString(),
                    adj != null ? adj.ToXmlString() : "");
            }

            internal static void AskTDToThrow_ToBeDispensed_Or_JustDispensed_TokenToBin()
            {
                Communication.SendMessage(ThreadName, "", "ThrowToBeDispensedTokenToBin");
            }
#if !_HHD_
            public static void UpdateTTagAnswer_Success(TTag logmediaUpdated)
            {
                Communication.SendMessage(ThreadName, "", "UpdateTTagAnswer", "0", logmediaUpdated.ToXMLString());
            }
#endif
            public static void UpdateTTagAnswer_Fail(TTErrorTypes errCode, bool bStillOnWork)
            {
                Communication.SendMessage(ThreadName, "", "UpdateTTagAnswer", "1", ((int)errCode).ToString(), bStillOnWork.ToString());
            }

            internal static void TokenDispenserOutJam(bool bJammed)
            {
                Communication.SendMessage(ThreadName, "", "TokenDispenserOutJam", bJammed? "1" : "0");
            }

            internal static void SomeMediaAppearedPostRTEOrWTEInLastCycle()
            {
                Communication.SendMessage(ThreadName, "", "SomeMediaAppearedPostRTEOrWTEInLastCycle");
            }

            internal static void AskAgentToChooseIfHeWantsToCompleteOperationUsingLooseTokens()
            {
                Communication.SendMessage(ThreadName, "", "AskAgentToChooseIfHeWantsToCompleteOperationUsingLooseTokens");
            }

            internal static void UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode updateMediaRollbackOpAnswerCode)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaRollbackOpAnswer", ((int)updateMediaRollbackOpAnswerCode).ToString());
            }

            internal static void UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode updateMediaRollbackOpAnswerCode, int idx, int subidx, string xmlStr, string xdrStr)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaRollbackOpAnswer", ((int)updateMediaRollbackOpAnswerCode).ToString(), idx.ToString(), subidx.ToString(), xmlStr, xdrStr);
            }

            internal static void UpdateMediaRollbackOpAnswer(UpdateMediaRollbackOpAnswerCode updateMediaRollbackOpAnswerCode, int idx, int subidx)
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaRollbackOpAnswer", ((int)updateMediaRollbackOpAnswerCode).ToString(), idx.ToString(), subidx.ToString());
            }

            internal static void UpdateMediaRollbackCompletedOrAbandoned()
            {
                Communication.SendMessage(ThreadName, "", "UpdateMediaRollbackCompletedOrAbandoned");
            }

            internal static void DoOpForUnreadableCSCReply(string resultToSendToMMI)
            {
                Communication.SendMessage(ThreadName, "", "DoOpForUnreadableCSCReply", resultToSendToMMI);
            }

            internal static void UpdateMediaOpAuditedCSCIssue(
                int zeroForIssue,                
                int opIdx,
                int opSubIdx,
                bool bWTE,
                string logicalData,
                string xdrDataForCCHS)
            {
                Communication.AddEventsToExternal("UpdateMediaOpAudited_CSCIssue", "MMIChannel");
                Communication.SendMessage(ThreadName, "", "UpdateMediaOpAudited_CSCIssue",                    
                    opIdx.ToString(),
                    opSubIdx.ToString(),
                    (!bWTE).ToString(),
                    logicalData,
                    xdrDataForCCHS == null ? "" : xdrDataForCCHS,
                    zeroForIssue.ToString());
            }

            internal static void UpdateMediaOpInitialiseBankTopup(string logicalData, string xdrForCCHS)
            {
                Communication.AddEventsToExternal("UpdateMediaOpAudited_InitialiseBankTopup", "MMIChannel");
                Communication.SendMessage(ThreadName, "", "UpdateMediaOpAudited_InitialiseBankTopup",
                    logicalData, xdrForCCHS);
            }

            internal static void CSTMediaDetectionForTokenRefund(TTErrorTypes tTErrorTypes, LogicalMedia logMedia, bool bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess, int priceAsPerEOD)
            {
                string par3 = bMediaIsTokenAndIsSubmittedForRefundAndShouldBeReturnedBackToCustomerAfterProcess.ToString();
                Communication.SendMessage(ThreadName, "", "CSTMediaDetection",
                    ((int)tTErrorTypes).ToString(),
                    logMedia.ToXMLString(),
                    ((int)MediaDetectionTreatment.TOM_AnalysisForRefund).ToString(),
                    par3,
                    priceAsPerEOD.ToString());
            }
        }
    }
}