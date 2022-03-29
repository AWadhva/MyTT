using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using Microsoft.Win32;
using IFS2.Equipment.Common.CCHS;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public static class SharedData
    {
        private static int _stationNumber=0;
        private static int _equipmentNumber=0;
        private static int _lineNumber=0;
        private static string _cscapiversion = "";

        private static EquipmentFamily _equipmentType = EquipmentFamily.Unknown;

        private static short _serviceProvider=2;
        private static EquipmentStatus _equipmentStatus;        

        private static int _TxnSequenceNo = 0;// to be fetched from CCHS SAM and Stored in the SAM. value will be increased and stored on TAC calculation
        public static int _odTxnSequenceNo = 0;
        public static int _rejectionCode = 0;
        public static cCCHSSAM mcCCHSSAM = new cCCHSSAM();
        //private static CONSTANT.CCHSSAMType SAMType = CONSTANT.CCHSSAMType.THALES_SAM;
        //private static int SAM_SLOT = 1;
        public static List<cSAMConf> mSAMUsed = new List<cSAMConf>();/// to support multiple SAM's 
        public static long mDSMId = 0;
        public static DateTime CSC_oldEndOfValidityDate =DateTime.Parse("1/1/1999");
        public static byte CompanyID = 0x00;

        public static EquipmentFamily EquipmentType { get { return _equipmentType; } set { _equipmentType = value; } }
        public static EquipmentStatus EquipmentStatus { get { return _equipmentStatus; } set { _equipmentStatus = value; } }
        public static int EquipmentNumber { get { return _equipmentNumber; } set { _equipmentNumber = value; } }
        public static int LineNumber { get { return _lineNumber; } set { _lineNumber = value; } }
        public static int StationNumber { get { return _stationNumber; } set { _stationNumber = value; } }
        public static short ServiceProvider { get { return _serviceProvider; } set { _serviceProvider = value; } }
        public static string cscApiVersion { get { return _cscapiversion; } set { _cscapiversion = value; } }
        public static int TokenLayoutVersion { get; set; }
        public static int DM1HistoryVersion { get; set; }        
        public static int DM2ValidationVersion { get; set; }
        public static int DM2SaleAddValueVersion { get; set; }

        public static AgentShift _agentShift = null;
        public static FareProductSpecs _fpSpecsRepository;


        public static void SaveContextFile()
        {
            try
            {
                string s = "<TTContext>";
                s += "<LineNumber>" + Convert.ToString(_lineNumber) + "</LineNumber>";
                s += "<StationNumber>" + Convert.ToString(_stationNumber) + "</StationNumber>";
                s += "<EquipmentNumber>" + Convert.ToString(_equipmentNumber) + "</EquipmentNumber>";
                s += "<EquipmentType>" + Convert.ToString((int)_equipmentType) + "</EquipmentType>";
                s += "<ServiceProvider>" + Convert.ToString(_serviceProvider) + "</ServiceProvider>";
               // s += "<ODTxnSequence>" + Convert.ToString(_odTxnSequenceNo) + "</ODTxnSequence>";
                s += "<TransactionSeq>" + Convert.ToString(_TxnSequenceNo) + "</TransactionSeq>";
                s += "</TTContext>";
                FileUtility.SaveContext("TT", s);
//#if WindowsCE
//                Utility.WriteAllText(Disk.BaseDataDirectory + "\\Context\\ContextFile_TT.txt", s);
//#else
//                File.WriteAllText(Disk.BaseDataDirectory + "\\Context\\ContextFile_TT.txt", s);
//#endif
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SharedData: Saving Context File Error " + e.Message);
            }
        }        

        private static bool _bDispenserAvailable = false;
        public static bool IsDispenserAvailable
        {
            get { return _bDispenserAvailable; }
            set
            {
                if (_bDispenserAvailable != value)
                {
                    Logging.Log(LogLevel.Information, "Dispenser status modified from " + _bDispenserAvailable + " to " + value.ToString());
                    _bDispenserAvailable = value;
                }
            }
        }

        public static void ReadContextFile()
        {
            try
            {
                string s = FileUtility.ReadContext("TT");
                if (s == "")
                {
                    string file = Disk.BaseDataDirectory + "\\Context\\ContextFile_TT.txt";
                    if (!File.Exists(file))
                    {
                        s = Utility.ReadAllTextFromFile(file);
                        FileUtility.SaveContext("TT", s);                       
                    }
                    return;
                }
                _equipmentNumber = Convert.ToInt32(Utility.SearchSimpleTag(s, "EquipmentNumber"));                
                _lineNumber = Convert.ToInt32(Utility.SearchSimpleTag(s,  "LineNumber"));
                _stationNumber = Convert.ToInt32(Utility.SearchSimpleTag(s,  "StationNumber"));
                _serviceProvider = Convert.ToInt16(Utility.SearchSimpleTag(s, "ServiceProvider"));
                try
                {
                    //_odTxnSequenceNo = Convert.ToInt32(Utility.SearchSimpleTag(s, "ODTxnSequence"));
                    _TxnSequenceNo = Convert.ToInt32(Utility.SearchSimpleTag(s, "TransactionSeq"));
                }
                catch {
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SharedData: Reading Context File Error " + e.Message);
            }
        }       

        public static void Initialise(string s)
        {
            try
            {
                //_equipmentNumber = Convert.ToInt32(Utility.SearchSimpleTag(s, "EquipmentNumber"));
                _lineNumber = Convert.ToInt32(Utility.SearchSimpleTag(s, "LineNumber"));
                _stationNumber = Convert.ToInt32(Utility.SearchSimpleTag(s, "StationNumber"));
                _serviceProvider = Convert.ToInt16(Utility.SearchSimpleTag(s, "ServiceProvider"));
                //try
                //{
                //    _TxnSequenceNo = Convert.ToInt32(Utility.SearchSimpleTag(s, "TransactionSeq"));
                //}
                //catch { _odTxnSequenceNo = 1; _TxnSequenceNo = 0; }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "SharedData: Initialising Error " + e.Message);
            }
        }

        static SharedData()
        {
            //This is a problem at this moment. Because we could have received from EOD
            //FareProductSpecs.Load(true);
            //_fpSpecsRepository = FareProductSpecs.GetInstance();

            TokenLayoutVersion = 0;
            DM1HistoryVersion = 0;
            DM2ValidationVersion = 0;
            DM2SaleAddValueVersion = 0;
        }

        public static int TransactionSeqNo
        {
            get
            {
                return _TxnSequenceNo;
            }
            set
            {
                _TxnSequenceNo = value;
              //  SaveContextFile();
            }
        }
    }

    public static class Util
    {
//        static readonly DateTime startdayCCHS = DateTime.Parse("1/1/1999");
        static public short GetDays_CSC_Date_t(DateTime dt)
        {
            return IFS2.Equipment.Common.Utility.GetDays_CSC_Date_t(dt, OverallParameters.EndBusinessDay);
            //DateTime bd_day = DatesUtility.BusinessDay(dt, OverallParameters.EndBusinessDay);
            //TimeSpan Ts_diff = bd_day.Subtract(startdayCCHS);
            //return (short)Ts_diff.Days;
        }
    }
     
    // 6.3.3
    public class FldsCSCIssueTxn
    {
        private int issuerId = 2;
        public int psn;
        public DateTime ticket1StartDate;
        public CSC_Type_t cscType = CSC_Type_t.CardADesfire;
        public int ticket1Type;
        public IssueType issueType;
        public DateTime ticket2StartDate;
        public DateTime ticket2EndDate;
        public int cntRides;
        public int blockingStatus;
        public int lng;
        public int cscRemainingValue;
        public int transactionValue;
        public int cscDepositAmount;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(issuerId);
            xdr.AddInt32(psn);
            DateTime now = DateTime.Now;
            xdr.AddInt16(Util.GetDays_CSC_Date_t(ticket1StartDate));
            xdr.AddInt8((byte)cscType);
            xdr.AddInt8((byte)ticket1Type);
            xdr.AddInt8((byte)issueType);
            xdr.AddInt16(Util.GetDays_CSC_Date_t(ticket2StartDate));
            xdr.AddInt16(Util.GetDays_CSC_Date_t(ticket2EndDate));
            xdr.AddInt8((byte)cntRides);
            xdr.AddInt8((byte)blockingStatus);
            xdr.AddInt8((byte)lng);
            xdr.AddInt32(cscRemainingValue);
            xdr.AddInt32(transactionValue);
            xdr.AddInt16((short)cscDepositAmount);
        }
    }

    // 6.3.7
    public class FldsCSCSurchargePayment
    {
        public int purseRemainingVal;
        public int surchargeAmt;
        public int surchargeTotal;
        private int transit1RemainingVal = 0;
        private int transit2RemainingVal = 0;
        public int surchargeDetails;
        public int entryLoc = 0;
        public int exitLoc = 0;
        private int fareIndicator = 0;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(purseRemainingVal);
            xdr.AddInt16((short)surchargeAmt);
            xdr.AddInt16((short)surchargeTotal);
            xdr.AddInt16((short)transit1RemainingVal);
            xdr.AddInt16((short)transit2RemainingVal);
            xdr.AddInt8((byte)surchargeDetails);
            xdr.AddInt16((short)entryLoc);
            xdr.AddInt16((short)exitLoc);
            xdr.AddInt16((short)fareIndicator);
        }
    }

    // 6.3.29
    public class FldsCashSurchargePayment
    {
        public int surchargeAmt;
        public int surchargeTotal;
        public int surchargeDetails;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt16((short)surchargeAmt);
            xdr.AddInt16((short)surchargeTotal);
            xdr.AddInt8((byte)surchargeDetails);
        }
    }

    // 6.3.20
    public class FldsCSCImmediateRefund
    {
        public int issuerId;
        public int psn;
        public int depositAmt;
        public int fees;
        public int refundTotal;
        public CSC_StatusCode_t status;        
        public RefundMethod_t refundMethod;
        public SurrenderReason_t refundReason;
        private PersonalID_Number_t personalID = new PersonalID_Number_t("");
        public Boolean_t cscCaptured;
        public int remainingVal;
        public Refund_t refundType;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(issuerId);
            xdr.AddInt32(psn);
            xdr.AddInt16((ushort)depositAmt);
            xdr.AddInt16((ushort)fees);
            xdr.AddInt16((ushort)refundTotal);
            personalID.EmbedContentsToXdr(xdr);
            xdr.AddInt16(0); // Country code. Seems not being used
            xdr.AddInt8(0); // PersonalId type. Seems not being used
            xdr.AddInt8((byte)status);            
            xdr.AddInt8((byte)refundMethod); // Refund method: Cash
            xdr.AddInt8((byte)refundReason); // Patron request and not to be replaced
            xdr.AddInt8((byte)cscCaptured);
            xdr.AddInt32(remainingVal);
            xdr.AddInt8((byte)refundType);
        }
    }

    // 6.3.20
    public class FldsCSCPurseDeduction
    {
        public int transactionValue;
        public int purseRemainingVal;
        public int transit1RemainingVal = 0;
        public int transit2RemainingVal = 0;
        public string receiptNumber = "";
        public int purchaseCategory = 0;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(transactionValue);
            xdr.AddInt32(purseRemainingVal);
            xdr.AddInt16((short)transit1RemainingVal);
            xdr.AddInt16((short)transit2RemainingVal);
            xdr.AddCompactString(receiptNumber, 32);
            xdr.AddInt16((short)purchaseCategory);
        }
    }

    // 6.3.20
    public class FldsCSCBusCheckOutRebate
    {
        public int transactionValue;
        public int purseRemainingVal;
        public int transferredProvider = 0;
        public int entryLine = 0;
        public int entryStage = 0;
        public DateTime entryTime = DateTime.Now;
        public short direction = 0;
        public int discountAmount = 0;
        public short discountReason = 0;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(transactionValue);
            xdr.AddInt32(purseRemainingVal);
            xdr.AddInt32(transferredProvider);
            xdr.AddInt32(entryLine);
            xdr.AddInt32(entryStage);
            xdr.AddDateTime(entryTime);
            xdr.AddInt16(direction);
            xdr.AddInt32(discountAmount);
            xdr.AddInt16(discountReason);
        }
    }

    // 6.3.15
    public class FldsCSCPeformAddValueViaEFT
    {        
        public int addValAmt;
        private int bonusValueAmt = 0;
        public int purseRemainingVal;
        private int transit1RemainingVal = 0;
        private int transit2RemainingVal = 0;
        public int systemTraceAuditNumber;
        public string bankTerminalId;
        public DateTime dtTimeBankTerminalTransaction;
        public string resoponseCode;
        private string pan = new String(' ', 152) ;
        public int authorizationCode;
        public int depositInCents;
        public OperationType_t operationType;

        private byte ToBCD(byte val)
        {
            if (val > 99) return 0;
            try
            {
                int res = ((val / 10) << 4) & 0xF0 + (val % 10) & 0xF;
                return (byte)res;
            }
            catch { return 0; }
        }

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(addValAmt);
            xdr.AddInt32(bonusValueAmt);
            xdr.AddInt32(purseRemainingVal);
            xdr.AddInt16((short)transit1RemainingVal);
            xdr.AddInt16((short)transit2RemainingVal);
            xdr.AddInt32(Utility.ToBcdInt(systemTraceAuditNumber));
            xdr.AddString(bankTerminalId, 8);

            byte[] dt = new byte[4];
            dt[0] = 0x00;
            dt[1] = ToBCD((byte)dtTimeBankTerminalTransaction.Day);
            dt[2] = ToBCD((byte)dtTimeBankTerminalTransaction.Month);
            dt[3] = ToBCD((byte)(dtTimeBankTerminalTransaction.Year%100));
            xdr.AddByteArr(dt);

            byte[] tim = new byte[4];
            tim[3] = 0x00;
            tim[2] = ToBCD((byte)dtTimeBankTerminalTransaction.Hour);
            tim[1] = ToBCD((byte)dtTimeBankTerminalTransaction.Minute);
            tim[0] = ToBCD((byte)dtTimeBankTerminalTransaction.Second);
            xdr.AddByteArr(tim);

            xdr.AddString(resoponseCode, 2);
            xdr.AddString(pan, 152 / 8);
            byte[] ac = new byte[3];
            //ac[3] = 0x00;
            ac[2] = ToBCD((byte)((authorizationCode >> 16) % 256));
            ac[1] = ToBCD((byte)((authorizationCode >> 8) % 256));
            ac[0] = ToBCD((byte)(authorizationCode % 256));
            xdr.AddByteArrPerByte(ac, 3);
            //if (authorizationCode > 999999)            
            //    xdr.AddByteArr(Utility.ToBcd(0, 4));
            //else
            //    xdr.AddByteArr(Utility.ToBcd(authorizationCode, 4));
                        
            xdr.AddInt8((byte)(depositInCents / 100));
            xdr.AddInt8((byte)operationType);
        }
    }
    // 6.3.18
    public class FldsCSCPeformAddValueViaBankTopup
    {        
        public int addValAmt;
        private int bonusValueAmt = 0;
        public int purseRemainingVal;
        private int transit1RemainingVal = 0;
        private int transit2RemainingVal = 0;
        private BankTopUpReason_t topupReason = BankTopUpReason_t.ThresholdReached;
        public int depositInCents;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(addValAmt);
            xdr.AddInt32(bonusValueAmt);
            xdr.AddInt32(purseRemainingVal);
            xdr.AddInt16((short)transit1RemainingVal);
            xdr.AddInt16((short)transit2RemainingVal);
            xdr.AddInt8((byte)topupReason);
            xdr.AddInt8((byte)(depositInCents / 100));
        }
    }

    // 6.3.19
    public class FldsCSCSurrendered
    {
        public PatronName_t _patronName;
        private TelephoneNumber_t _telephoneNumber = new TelephoneNumber_t("");
        private PersonalID_Number_t _personalIDNumber = new PersonalID_Number_t("");
        private byte _PersonalIDType = 0;
        private short _personalIDCountryCode = 0;
        public byte _refundLevel = 1;         
        public CSC_StatusCode_t _cscStatus;
        private AddressDetails_t part1 = new AddressDetails_t("");
        private AddressDetails_t part2 = new AddressDetails_t("");
        private AddressDetails_t part3 = new AddressDetails_t("");
        private AddressDetails_t part4 = new AddressDetails_t("");
        public RefundMethod_t _refundMethod;
        public SurrenderReason_t _surrenderReason;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            _patronName.EmbedContentsToXdr(xdr);
            _telephoneNumber.EmbedContentsToXdr(xdr);
            _personalIDNumber.EmbedContentsToXdr(xdr);
            xdr.AddInt16(_personalIDCountryCode);
            xdr.AddInt8(_PersonalIDType);
            xdr.AddInt8((byte)_refundLevel);
            xdr.AddInt8((byte)_cscStatus);
            xdr.AddInt8((byte)_surrenderReason);
            xdr.AddInt8((byte)_refundMethod);
            part1.EmbedContentsToXdr(xdr);
            part2.EmbedContentsToXdr(xdr);
            part3.EmbedContentsToXdr(xdr);
            part4.EmbedContentsToXdr(xdr);
        }
    }

    // 6.3.29
    public class FldsCSCBadDebtCashPayment
    {
        private PersonalID_Number_t _personalIDNumber = new PersonalID_Number_t("");
        private byte _PersonalIDType = 0;
        private short _personalIDCountryCode = 0;
        private string chequeNumber = new String(' ', 48) ;
        public int badDebtAmountSettled;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(badDebtAmountSettled);
            _personalIDNumber.EmbedContentsToXdr(xdr);
            xdr.AddInt8(_PersonalIDType);
            xdr.AddInt16(_personalIDCountryCode);
            xdr.AddString(chequeNumber, 48 / 8);
        }
    }

    // 6.3.21
    public class FldsCSCReplacement
    {
        public int purseIssuerId;
        public int purseSequenceNumber;
        public LogicalMedia replacedCSC;
        public ReplacementReason_t replacementReason;
        
        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(purseIssuerId);
            xdr.AddInt32(purseSequenceNumber);
            // TODO: replacedCSC
            xdr.AddInt8((byte)replacementReason);
            xdr.AddInt32(0); // 0 for operators except CC1
            xdr.AddInt32(0); // 0 for operators except CC1
            xdr.AddInt16(0); // 0 for operators except CC1
            xdr.AddInt16(0); // 0 for operators except CC1
            xdr.AddInt16(0); // 0 for operators except CC1
        }
    }

    // 6.3.6
    public class FldsCSCAddValueCancel
    {
        public int _addValueAmount;
        private int bonusAmount = 0;
        public int _purseRemianingValue_PostAddValue;
        private short _transit1RemainingValue = 0;
        private short _transit2RemainingValue = 0;
        public byte _cscDepositInRupees;
        private short _reversalReasonCode = 0;

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddInt32(_addValueAmount);
            xdr.AddInt32(bonusAmount);
            xdr.AddInt32(_purseRemianingValue_PostAddValue);
            xdr.AddInt16(_transit1RemainingValue);
            xdr.AddInt16(_transit2RemainingValue);
            xdr.AddInt8(_cscDepositInRupees);
            xdr.AddInt16(_reversalReasonCode);
        }
    }

    public static class ExtXDRCCHS
    {
        public static void EmbedContentsToXdr(this DateTime dt, XdrToXml xdr)
        {
            int nDaysSince_1stJan1999 = (int)((dt - new DateTime(1999, 1, 1)).TotalDays);
            xdr.AddInt32(nDaysSince_1stJan1999);
        }
    }

    // 6.3.15
    public class FldsInitialiseBankTopup
    {
        public PatronName_t accountHolderName;
        private TelephoneNumber_t _telephoneNumber = new TelephoneNumber_t("");
        public AccountType_t accountType = new AccountType_t("10"); // 10 stands for Savings. 11 implies Current
        private DateTime accountHolderDOB = new DateTime(1999, 1, 1);
        public PersonalID_Number_t accountHolderPersonalID = new PersonalID_Number_t("");
        public short accountHolderIDCountryCode;
        public byte accountHolderIDType;
        public BankIndicator_t bankIndicator;
        public BankAccountNumber_t bankAccountNumber;
        public int bankTopupAmount;
        public BankTopupRefNumberNumber_t bankTopupRefNumberNumber_t;
        private byte bankTopupAmountRefCode = 0;
        private int STAN_T = 0;
        private BankTerminal_TerminalID_t bankTerminal_TerminalID_t;// = new BankTerminal_TerminalID_t("ABC");
        //private int bankTerminalTransactionDate = 0;
        //private int bankTerminalTransactionTime = 0;
        private short bankTerminalCode = 0;
        private PAN_T panOfAccountHolder = new PAN_T("");
        private BranchNumber_t branchNumber = new BranchNumber_t("");
        private byte clearingID = 0;// TODO: As per documentation, its data type is BankIndicator_t, which is a 72 byte thing, used in this very structure. So, i assume it a documentation mistake. but may be it is not
        private AddressDetails_t address1 = new AddressDetails_t("");
        private AddressDetails_t address2 = new AddressDetails_t("");
        private AddressDetails_t address3 = new AddressDetails_t("");
        private AddressDetails_t address4 = new AddressDetails_t("");

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            LogOffsetToWrite("accountHolderName", xdr);
            //accountHolderName = new PatronName_t("ICICI ICICI ICICI");
            accountHolderName.EmbedContentsToXdr(xdr);

            LogOffsetToWrite("_telephoneNumber", xdr);
            _telephoneNumber.EmbedContentsToXdr(xdr);

            LogOffsetToWrite("accountType", xdr);
            accountType.EmbedContentsToXdr(xdr);

            accountHolderDOB = new DateTime(1970, 1, 1);
            LogOffsetToWrite("accountHolderDOB", xdr);
            accountHolderDOB.EmbedContentsToXdr(xdr);

            
            //accountHolderPersonalID = new PersonalID_Number_t("ICICI123");
            LogOffsetToWrite("accountHolderPersonalID", xdr);
            accountHolderPersonalID.EmbedContentsToXdr(xdr);
            
            //accountHolderIDCountryCode = 365;
            LogOffsetToWrite("accountHolderIDCountryCode", xdr);
            xdr.AddInt16(accountHolderIDCountryCode);

            //accountHolderIDType = 2;
            LogOffsetToWrite("accountHolderIDType", xdr);
            xdr.AddInt8(accountHolderIDType);

            //LogOffsetToWrite("bankIndicator", xdr);
            bankIndicator.EmbedContentsToXdr(xdr);

            
            //bankAccountNumber = new BankAccountNumber_t("56001338");
            LogOffsetToWrite("bankAccountNumber", xdr);
            bankAccountNumber.EmbedContentsToXdr(xdr);


            LogOffsetToWrite("bankTopupAmount", xdr);
            xdr.AddInt32(bankTopupAmount);

            LogOffsetToWrite("bankTopupRefNumberNumber_t", xdr);
            //bankTopupRefNumberNumber_t = new BankTopupRefNumberNumber_t("1000096268");
            bankTopupRefNumberNumber_t.EmbedContentsToXdr(xdr);

            LogOffsetToWrite("bankTopupAmountRefCode", xdr);
            //xdr.AddInt8(bankTopupAmountRefCode);
            xdr.AddInt32(bankTopupAmount/100); // !!! as supplied as ideal file: Rs. 200/ is represented as c8

            //LogOffsetToWrite("STAN_T", xdr);
            xdr.AddInt32(Utility.ToBcdInt(STAN_T));

            //LogOffsetToWrite("bankTerminal_TerminalID_t", xdr);
            bankTerminal_TerminalID_t = new BankTerminal_TerminalID_t("ABC");
            bankTerminal_TerminalID_t.EmbedContentsToXdr(xdr);

            xdr.AddString("", 2);
            xdr.AddString("1", 2);
            xdr.AddString("12125454", 8);
            xdr.AddString("", 15);
            xdr.AddInt32(0x20);
            xdr.AddString("", 2+ 39*4 + 1);

            //LogOffsetToWrite("miscell", xdr);
            //xdr.AddInt32(BitConverter.ToInt32(new byte[]{0, 1, 1, 99}, 0));
            //xdr.AddInt32(BitConverter.ToInt32(new byte[] { 0, 0, 0, 0 }, 0));
            //xdr.AddInt16(bankTerminalCode);
            //panOfAccountHolder.EmbedContentsToXdr(xdr);
            //branchNumber.EmbedContentsToXdr(xdr);
            //xdr.AddInt8(clearingID);
            //address1.EmbedContentsToXdr(xdr);
            //address2.EmbedContentsToXdr(xdr);
            //address3.EmbedContentsToXdr(xdr);
            //address4.EmbedContentsToXdr(xdr);
        }

        private static void LogOffsetToWrite(string var, XdrToXml xdr)
        {
            //Logging.Log(LogLevel.Verbose, "Before var " + var);
            //Logging.Log(LogLevel.Verbose, String.Format("{0}/{1}/{2}", xdr.OffsetWrite.ToString(), xdr.OffsetWrite.ToString("X2"), (xdr.OffsetWrite + 0x10).ToString("X2")));
        }
    }


    public enum BankTopUpReason_t
    {
        ThresholdReached = 0
    }

    public enum OperationType_t
    {
        AddValuePartOfSalesOp = 1,
        AddValuePartOfReplacementOp = 2,
        Other = 0
    }

    public enum Refund_t
    {
        Immediate = 1,
        Deferred = 2
    };

    public enum Payment_Method
    {
        Cash = 0,
        Purse = 1
    }    

    public enum Boolean_t
    {
        FALSE = 0,
        TRUE = 1
    }

    public enum CSC_Type_t
    {
        CardCSony = 3,
        CardADesfire = 6
    }

    abstract public class XdrString
    {
        string _str;
        public XdrString(string str)
        {
            _str = str;
        }

        public void EmbedContentsToXdr(XdrToXml xdr)
        {
            xdr.AddString(_str, GetLength());
        }

        public abstract int GetLength();
    }

    public class PatronName_t : XdrString
    {
        public PatronName_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 256/8;
        }
    }

    public class TelephoneNumber_t : XdrString
    {
        public TelephoneNumber_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 160/8;
        }
    }

    public class PersonalID_Number_t : XdrString
    {
        public PersonalID_Number_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 160 / 8;
        }
    }

    public class AddressDetails_t : XdrString
    {
        public AddressDetails_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 320 / 8;
        }
    }

    public class BankIndicator_t : XdrString
    {
        public BankIndicator_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 1; // NOTE: It is in violoation to documented layout (in V2.60), but in reality it is this.
            //return 72 / 8;
        }
    }

    public class BankAccountNumber_t : XdrString
    {
        public BankAccountNumber_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 120 / 8; // may have documentation mistake. It is given as 12 character Bank Account number, but has 120 bits instead of 96 bits
        }
    }

    public class BankTopupRefNumberNumber_t : XdrString
    {
        public BankTopupRefNumberNumber_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 96 / 8;
        }
    }

    public class BankTerminal_Code_t : XdrString
    {
        public BankTerminal_Code_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 16 / 8;
        }
    }

    public class BankTerminal_TerminalID_t : XdrString
    {
        public BankTerminal_TerminalID_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 64 / 8;
        }
    }

    public class PAN_T : XdrString
    {
        public PAN_T(string str) : base(str) { }

        public override int GetLength()
        {
            return 152 / 8;
        }
    }

    public class AccountType_t: XdrString
    {
        public AccountType_t(string str) : base(str) { }

        public override int GetLength()
        {
            return 16 / 8;
        }
    }

    public class BranchNumber_t : XdrString
    {
        public BranchNumber_t(string str) : base(str) { }

        public override int GetLength()
        {
            return (24 / 8)
                + 7// it is being used solely in Initialise bank topup. already trimmed 8 bytes from bank indicator. Expanding it so as to fill 1328 bytes of variant
                ;            
        }
    }

    public enum IssueType
    {
        CardIssue_Deposit_CommonPurse_SVProduct = 0,
        Cost = 1, // Not used
        CardIssue_Deposit_CommonPurse_PeriodPass = 2,
        SVProductIssue = 3,
        TouristPass_OtherProductIssue_UsingPursePayment = 4,
        TouristPass = 5 // Not used
    }

    public class AgentShift
    {
        public AgentShift(int shift, int agentId, AgentProfile profile)
        {
            _shiftId = shift;
            _agentId = agentId;
            _profile = profile;
        }

        int _shiftId;
        int _agentId;
        AgentProfile _profile;

        public int ShiftId { get { return _shiftId; } }
        public int AgentId { get { return _agentId; } }
        public AgentProfile Profile { get { return _profile; } }
    }
}
