using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using IFS2.Equipment.Common;
using System.Security.Cryptography;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    /// <summary>
    /// Class to Generate InsertOneRecord Txn ......
    /// </summary>
    public static class ComposeCCHSTxn
    {
        private static XdrToXml _xdr = null;
        private static int _hRW;
        private static CSC_READER_TYPE _ReaderType;
        // private static CCHSSAMManger mCCHSSamManger;
        // private static TransactionType txnType;

        static ComposeCCHSTxn()
        {
            _xdr = new XdrToXml();
        }
        public static bool TreatXDRCompatibility(LogicalMedia logMedia, out string OneTxnStrData, int type, int TxnSeqeuence, int AddvalueAmnt, int hRW, CSC_READER_TYPE Readertype)
        {
            _hRW = hRW;
            _ReaderType = Readertype;
            _xdr = new XdrToXml();
          //  txnType = (TransactionType)type;
            try
            {
              //  int type = Utility.SearchSimpleCompleteTagInt(s, "", "TxT");
                // 24*4 (XDR) bytes for General header 
                //22*4 (XDR ) Bytes for CSC purse uses header
                //20*4 bytes for Media Top Up (TXN_CSC_AFF_VALUE_EFT)
                int delhiTrx = 0;
                int size = 8;
                int size_generalHeader = 24, size_cscTpurse_header = 22, size_cscTopUP = 19/*20*/, size_CSCUses_Header=8;
                switch (type)
                {
                    case (int)TransactionType.BlacklistDetection:
                        size = size_generalHeader+ size_CSCUses_Header+ 3;
                        delhiTrx = 9;
                        break;
                   
                    case (int) TransactionType.MediaRejection:
                        size = size_generalHeader + size_CSCUses_Header + 3;
                        break;

                    case (int) TransactionType.MediaBlocked:
                        size = size_generalHeader + size_CSCUses_Header + 2;
                        break;
                    case (int)TransactionType.GeneralPayment:
                        size = 20;
                        delhiTrx = 13; // Find the real number
                        break;
                    case (int)TransactionType.MediaTicketKeyChange:
                        delhiTrx = 8; //Not used
                        OneTxnStrData = null;
                        return true;
                    
                    case (int)TransactionType.OperatorLogin://
                        size = 17;
                        delhiTrx = 36;
                        break;
                    case (int)TransactionType.OperatorLogoff://
                        size = 17;
                        delhiTrx = 37;
                        break;                    
                    case (int)TransactionType.TicketsPayment:
                        size = 20;
                        delhiTrx = 13; //Find the real number
                        break;
                    //case (int)TransactionType.TokenContainerIn://
                    //    size = 22;
                    //    delhiTrx = 34;
                    //    break;
                    //case (int)TransactionType.TokenContainerOut://
                    //    size = 21;
                    //    delhiTrx = 35;
                    //    break;
                    case (int)TransactionType.TPurseDirectReload:
                    case (int) TransactionType.TPurseWebTopupReload:
                       // size = 25;
                        size = size_generalHeader + size_cscTpurse_header + size_cscTopUP;
                        delhiTrx = 4;
                        break;
                }

              
                Logging.Log(LogLevel.Verbose, "nb:" + size.ToString() + " Trx:" + delhiTrx.ToString());
                _xdr.InitResult(size * 4);
               // _xdr.AddInt32(1); //Header tag Transaction. Hypothesis that there is only one UD file type.
                GenerateTransactionGeneralHeader(TxnSeqeuence, DateTime.Now, (TransactionType)type, 0x01, (size * 4));
                switch (type)
                {
                    case (int)TransactionType.BlacklistDetection:
                        //BlackList(s);
                        //TODO:                        
                        TXN_CSC_BLACKLIST_ACTIONED(logMedia, TxnSeqeuence);
                        break;
                    case (int)TransactionType.MediaRejection:
                        //TODO: to be discussed on this txn as currently TT Seems not classifying this case while detecting a card
                        break;

                    case (int) TransactionType.MediaBlocked:
                        TXN_CSC_BLOCKED(logMedia, TxnSeqeuence);
                        break;
                    case (int)TransactionType.GeneralPayment:
                        //PaymentGeneralTransaction(s);
                        break;
                   
                    case (int)TransactionType.OperatorLogin:
                    case (int)TransactionType.OperatorLogoff:
                       // _xdr.AddInt32(Utility.SearchSimpleCompleteTagInt(s, "Eqp", "Sta"));
                       // _xdr.AddString(Utility.SearchSimpleCompleteTag(s, "Agt", "ID"), 8);
                      //  _xdr.AddInt32(AgentProfileCompatibility((AgentProfile)Utility.SearchSimpleCompleteTagInt(s, "Agt", "Pr")));
                        break;
                  
                    case (int)TransactionType.TPurseDirectReload:
                    case (int)TransactionType.TPurseWebTopupReload:
                      //  Logging.Log(LogLevel.Verbose, "C");
                        //AddValueTransaction(s);
                        Generate_CSCPurse_Usage_Txn_Header(logMedia);
                        MediaTopUpData(logMedia, AddvalueAmnt, TxnSeqeuence);
                        break;
                }
                string packedCmdBuf = SerializeHelper<byte[]>.XMLSerialize(_xdr.Result);
                //Communication.SendMessage("TransactionsDriver", "Data", "SendTransaction", s, packedCmdBuf);
                //xdrStrData = packedCmdBuf;
                OneTxnStrData = ComposeXMLForTxn((TransactionType)type, packedCmdBuf);
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Critical, "TransactionManager.DelhiCompatibility " + e.Message + " Transaction:");
            }
            OneTxnStrData = null;
            return false;
        }

       
        private static void GenerateTransactionGeneralHeader(int TxnSequenceNo, DateTime dt, TransactionType txntype, byte CCHSTxnType,int TDPacketLength)
        {
           // string st = "";
             
            try
            {
                _xdr.AddInt8(0); //TD_Version_t default value is 0;
                _xdr.AddInt32(TxnSequenceNo); //TDSN_t  TD Sequence Number 
                
                int timestamp = DatesUtility.ConvertToUnixTimestamp( dt);//TODO : to be checked whether this function gives unix time stamp of GMT 0 
                _xdr.AddInt32(timestamp);
                
                //Bussiness day
                DateTime startday = DateTime.Parse("1/1/1999");
                DateTime bd_day = DatesUtility.BusinessDay(DateTime.Now, OverallParameters.EndBusinessDay);//.ToString("dd/MM/yyyy");
                TimeSpan Ts_diff = bd_day.Subtract(startday);
                short days = (short)Ts_diff.Days;
                _xdr.AddInt16(days);

                //Device ID 16(device type) +32 (Device Sequence Number_t)
                _xdr.AddInt16((short)SharedData.EquipmentType); // Device type : TODO: to be checked
                _xdr.AddInt32(SharedData.EquipmentNumber); // Device Sequence Number

                //ParticipantID_t CC operator ID 8 bits
                _xdr.AddInt8((byte)SharedData.ServiceProvider); // TODO: to be verified
                //8	ReferenceID_t 
                //TODO: making it same as participent id
                _xdr.AddInt8((byte)SharedData.ServiceProvider);

                //TD variant data length	16	TD_Length_t
             /*   int TD_DataPacket_length = 0, TD_GeneralHeader_DataLength=448;
                switch ((int)txntype)
                {
                    case (int)TransactionType.BlacklistDetection:
                        //TODO:
                        //int CSC_Usage_Transaction_Header = 136;
                        int TXN_CSC_BLACKLIST_ACTIONED = 184; // includes CSC_Usage_Transaction_Header
                        break;
                    case (int) TransactionType.MediaBlocked:
                        //TODO:
                        int TXN_CSC_BLOCKED = 176; // includes CSC_Usage_Transaction_Header
                        break;
                    case (int) TransactionType.MediaRejection:
                        //TODO;
                        int CSC_Usage_Transaction_Header = 136;
                        int TXN_CSC_REJECTED = 48;
                        TD_DataPacket_length = TD_GeneralHeader_DataLength + CSC_Usage_Transaction_Header + TXN_CSC_REJECTED;
                        break;
                    case (int) TransactionType.TPurseDirectReload:
                        //TODO:
                        int CSCPurseUsageTransactionHeader_length = 344; //in bytes
                        int Media_TOPUP_dataLength = 872; // CSCPurseUsageTransactionHeader_length + MediaTopupdata;
                        TD_DataPacket_length = TD_GeneralHeader_DataLength + Media_TOPUP_dataLength;
                        break;

                }
                */
                _xdr.AddInt16((short)TDPacketLength); //TODO: to be filled after forming  rest of TD fields

                //TD_LogID_t 8bits
                _xdr.AddInt8(0);

                //8	LocationType_t station name
                //For Metro devices (E.g. DMRC, Reliance), this field will be encoded as per the value defined in
                //Device Specific PD.
                _xdr.AddInt8((byte)SharedData.StationNumber); // TODO: needed to verfied as per dmrc PD

                //8	LocationCodeA_t 
                _xdr.AddInt8(0); // not required for AVM

                //16	LocationCodeB_t . station ID
                _xdr.AddInt16((short)SharedData.StationNumber);

                //TD Type	8	TD_Type_t
                byte TD_Transaction = 0x01; //TODO: to be picked from function params
                _xdr.AddInt8(TD_Transaction);

                //TD Sub Type	16	TD_SubType_t
                _xdr.AddInt16((short)txntype);

                //TD variant data format version	8	TD_Version_t	Format version of variant part of TD record (for default value, refer NOTE below)
                _xdr.AddInt8(0);

                //DSM ID	32	DSMID_t	Used to validate whether the data is generated by valid device.
                _xdr.AddInt32(0); // TODO: important tobe added in shared data defore adding here...

                // Company Code	8	CompID_t	Company Code, for BS21 – 0x01 and BS22 – 0 x02
                _xdr.AddInt8(0); // TODO : needed to clerify 

                //Agent ID	72	AgentID_t	Identifies the agent ID. (64+8)
                _xdr.AddZero32(2); //64 bits //TODO: to be clarified whether dat in this field is nessessory
                _xdr.AddInt8(0); //8 bits

                //Shift ID	32	ShiftID_t	Identifies the shift in which the transaction is generated.
                _xdr.AddZero32(1);

                //Filler	64	 	For Future Purposes
                _xdr.AddZero32(2);

            }
            catch
            {

            }

            //return st;
        }

        private static void Generate_CSCPurse_Usage_Txn_Header(LogicalMedia _logMedia)
        {

            //CSC Physical ID	64	CSC_PhysicalID_t	Unique card number stored in CSC.

            PhysicalSerialNumber(_logMedia.Media.ChipSerialNumber);


            //CSC Type	8	CSC_Type_t	CSC type associated with Purse
            _xdr.AddInt8(0);

            //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)
            _xdr.AddInt8(0);

            //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
            _xdr.AddInt32(_logMedia.Media.Owner); //TODO:

            //Ticket Type	8	TicketType_t	Active Ticket type associated with Purse
           short fareProducttype= _logMedia.Application.Products.Product(0).Type; //TODO:  
           _xdr.AddInt8((byte)fareProducttype);

            //Purse Life-Cycle count	8	Purse_LifeCycleID_t	Current Life Cycle of the Purse Not supported by current card layout.	0
           _xdr.AddInt8(0);

            //Purse Sequence Number (PSN)	32	PurseSequenceNumber_t
           _xdr.AddInt32((int)_logMedia.Media.SequenceNumber);

            //Purse Issuer ID	32	IssuerID_t	Issuer ID of the purse
           _xdr.AddInt32(_logMedia.Initialisation.ServiceProvider); //TODO: needed to check for good value

            //Purse Number	8	PurseNumber_t	Unique Identification number assigned to the Pro duct / Purse e.g ‘1’ for ‘SV1’.The number allocated to this Purse (i.e. 0 for the AFC Monetary Purse)
           _xdr.AddInt8((byte)fareProducttype); //TODO: 

            //Last Purse Purchased Life Cycle count	8	PurseLifeCycleID_t  -- not applicable
           _xdr.AddInt8(0);

            //Last Add Value Participant ID	8	ParticipantID_t	Operator ID associated with last add value to this purse.
           _xdr.AddInt32(_logMedia.Media.Owner);

            //Last Add Value Type	8	AddValueType_t	Type of last add value associated with this purse
           _xdr.AddInt8((byte)_logMedia.Purse.LastAddValue.OperationType);

            //Last   Purse Purchased	8	PurseNumber_t	The unique number for the purse / product e.g ‘1 for ‘SV1’   last purchased.
             _xdr.AddInt8(0);

            //Last Add Value Date	16	CSC_Date_t	Date value last added to this purse
            //TODO: Can Date be stored in 16 bits .????????????????
           DateTime lastaddvaluedate=  _logMedia.Purse.LastAddValue.DateTime;
             _xdr.AddInt16(0); // putting zero as of now 

            //Last Add Value Amount	16	SValueOneCent_t	Value last added to purse
             _xdr.AddInt16((short)_logMedia.Purse.LastAddValue.Amount);

            //Last Add Value CSC-RW Device ID	32	CSC_RW_ID_t	CSC-RW Device ID associated with last add value to this purse.
             _xdr.AddInt32(_logMedia.Purse.LastAddValue.EquipmentNumber);

            ///Last Purse Purchased Price	16	SValueOneCent_t	The price paid for the Period Pass last purchased
             _xdr.AddInt16(0);

            //Last Purse Purchased Classification	8	PurseClassification_t	Type of Period Pass last purchased.(NA to DMRC)
             _xdr.AddInt8(0);

            //Bad Debt Sequence Number	8	BadDebtSequenceNumber _t	Sequence number associated with purse bad debt settlement
             _xdr.AddInt8(0); // NA

            //Transaction Status	8	TransactionStatus_t	Indicates if the TD transaction is a normal or test transaction, and whether the update to the CSC was confirmed or unconfirmed.
            //TODO: what will be the value in either cases ?????????????
            // putting value to 1; needed to check and fix for correct value
             _xdr.AddInt8(1);

            //Audit Group	8	AuditGroupID_t	Audit Grouping associated with ticket (linking A R set update to UD Transaction generation)
             _xdr.AddInt8(0); // NA

        }

        private static void MediaTopUpData(LogicalMedia _logMedia, int addvalueAmount,int TxnSeq)
        {
           // Generate_CSCPurse_Usage_Txn_Header(_logMedia);
            //Add Value amount	32	LValueOneCent_t	EFT value to be added to Monetary CSC Purse
            _xdr.AddInt32(addvalueAmount);

            //Bonus Value Amount	32	LValueOneCent_t	Bonus Value to be added to Monetary Purse
            _xdr.AddInt32(0);

            //Monetary Purse Remaining Value	32	LValueOneCent_t	Remaining CSC Purse Value
            _xdr.AddInt32(_logMedia.Purse.TPurse.Balance);

            //Transit 1 Remaining Value	16	SValueOneCent_t	The amount of Monetary Purse value reserved for Transit use only. Associated with purchase of Park and Ride Pass 1 (Not applicable to DMRC)
            //Not applicable for AVM
            _xdr.AddInt16(0);

            //Transit 2 Remaining Value	16	SValueOneCent_t
            //Not applicable for AVM
            _xdr.AddInt16(0);

            //System Trace Audit Number	32	STAN_t	EFT  reference  number  associated  with  any  bank terminal payment.	Blank	No
            _xdr.AddZero32(1);

            //Bank Terminal ID	64	TerminalID_t	Bank terminal ID	Blank
            //Not applicable
            _xdr.AddZero32(2);

            //Date of Bank Terminal transaction	32	TransactionDate_t	Date of transaction, as reported by the Bank terminal
            _xdr.AddZero32(1); //not applicable for AVM

            //Time of Bank Terminal transaction	32	TransactionTime_t	Time of transaction, as reported by Bank terminal	Blank	No
            _xdr.AddZero32(1); //not applicable for AVM

            //Response Code from Terminal	16	Code_t	Bank terminal response code	Blank	No
             _xdr.AddInt16(0);

            //Primary Account Number	152	PAN_t	Account Number (PAN) of Bank Card used.
             _xdr.AddZero32(4);

            //Bank Terminal Authorisation Code	24	AuthorisationCode_t	6 BCD authorisation code	Blank
             _xdr.AddInt32(0);
            // _xdr.AddInt8(0);

            //CSC Deposit Reference Code	8	CSC_DepositRef_t	Code  identifying the deposit amount. This will be
            //equal to the Deposit Amount in Rupees.
             _xdr.AddInt32((int)addvalueAmount/10);

            //Sales operation	8	OperationType_t
            //not applicable
            /*
             * 1 if add-value operation is part of a sales operation
                2  if add value  operation  is part  of a replacement operation
                0 otherwise
             * */
             _xdr.AddInt8(0);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record (inclusive)
            //TODO: to be calcualted/implmentated
          //  CalculateSHA1Hash(
             GenrateTACfromCCHSSam(TxnSeq, true);
            // _xdr.AddInt32(0);
              
        }
        internal static void GenrateTACfromCCHSSam(int TxnSeq, bool updateSAMSeq)
        {
            byte[] TAC = null;
              CCHSSAMManger mCCHSSamManger = new CCHSSAMManger(_ReaderType, _hRW);
             foreach (cSAMConf samcnf in SharedData.mSAMUsed)
             {
                 if (samcnf.mSAMType == CONSTANT.CCHSSAMType.ISAM)
                 {
                     uint tac=0;
                     mCCHSSamManger.GenerateTAC((DEST_TYPE)samcnf.SAM_Slot, _xdr.Result, _xdr.Result.Length, out TAC);
                     tac |=(uint) TAC[0];
                     tac|= (uint)TAC[1]<<8;
                      tac|= (uint)TAC[2]<<16;
                      tac|= (uint)TAC[3]<<24;
                      //long index = _xdr.Position;
                      _xdr.AddInt32(tac);
                      if (updateSAMSeq == true)
                      {
                          mCCHSSamManger.WriteSAMSequence((DEST_TYPE)samcnf.SAM_Slot, TxnSeq);
                      }
                    // mCCHSSamManger
                      break;
                 }
             }
             if (TAC == null)
             {
                 _xdr.AddInt32(0);
                 Logging.Log(LogLevel.Error, " ComposeCCHSTxn Failed to genrate TAC"); 
             }


        }
      
        /// <summary>
        /// This CSC Header will be used for Media Blocked, Media Rejected & Media Blacklist Actioned.
        /// size = 8 * 4= 32 bytes
        /// </summary>
        /// <param name="_logMedia"></param>
        private static void CSCUsesTxnHeader(LogicalMedia _logMedia, int Amount)
        {
            //CSC Physical ID	64	CSC_PhysicalID_t	Unique card number stored in CSC.
            PhysicalSerialNumber(_logMedia.Media.ChipSerialNumber);

            //CSC Type	8	CSC_Type_t	CSC type associated with CSC	Blank	No
            _xdr.AddInt8(0);

            //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)	0
            _xdr.AddInt8(0);

            //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
            _xdr.AddInt32(_logMedia.Media.Owner); //TODO

            //CSC Deposit Reference Code	8	CSC_DepositRef_t	Code identifying the deposit amount. This will be
            //equal to the Deposit Amount in Rupees.
            _xdr.AddInt8((byte)(Amount/10));

            //Ticket Type	8	TicketType_t	Active Ticket type associated with CSC
            short fareProducttype = _logMedia.Application.Products.Product(0).Type; //TODO:  
            _xdr.AddInt8((byte)fareProducttype);

            //Transaction Status	8	TransactionStatus_t	Indicates if the TD transaction is a normal or test transaction, and whether the update to the CSC was confirmed or unconfirmed.
            _xdr.AddInt8(1);
        }
        private static void TXN_CSC_REJECTED(LogicalMedia _logMedia,int Amount, int rejecttionCode, int TxnSeq)
        {
            CSCUsesTxnHeader(_logMedia, Amount);
            //Rejection Reason	8	CSC_RejectReason_t	Reason for rejecting use of CSC on this device
            _xdr.AddInt8((byte)rejecttionCode);
            
           // Rejection Code	8	CSC_RejectCode_t	Rejection code associated with rejection,
            _xdr.AddInt8((byte)rejecttionCode);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record
//          (inclusive)
            //TODO:
            //_xdr.AddInt32(0);// To be calculated after discussion 
            GenrateTACfromCCHSSam(TxnSeq, true);
        }

        //3.1.2.	Media Blocked (TXN_CSC_BLOCKED)
        private static void TXN_CSC_BLOCKED(LogicalMedia _logMedia, int TxnSeq)
        {
            CSCUsesTxnHeader(_logMedia, 0);
           
            //CSC Status Code	8	CSC_StatusCode_t	Blocking Status written to CSC
            //= 1 (Unblocked) 
           if(_logMedia.Media.Blocked)
                _xdr.AddInt8(0);
           else _xdr.AddInt8(1);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record
            //          (inclusive)
            //TODO:
            //_xdr.AddInt32(0);// To be calculated after discussion 
           GenrateTACfromCCHSSam(TxnSeq, true);
        }

        //
        private static void TXN_CSC_BLACKLIST_ACTIONED(LogicalMedia _logMedia, int TxnSeq)
        {
            CSCUsesTxnHeader(_logMedia, 0);

            //Blacklist Reason Code	8	CSC_StatusCode_t	Blacklisting Reason Status identified in Blacklist
            //Please refer Appendix B for list of Blacklist Rea son Codes used with CCHS.
            byte reason = (byte)_logMedia.Media.ReasonOfBlocking;
            _xdr.AddInt8(reason);

            //Blacklist Action Code	8	CSC_Actions_t	The CSC Action Code as determined by reference e to the appropriate entry in the CSC Blacklist Action Table.
            //NOTE: As Blacklist Parameters does not have Action Code, so this field will carry default value as 255
            _xdr.AddInt8((byte)255);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record
            //          (inclusive)
            //TODO:
            //_xdr.AddInt32(0);// To be calculated after discussion 
            GenrateTACfromCCHSSam(TxnSeq, true);

        }

        private static void PhysicalSerialNumber(long nb)
        {

            long nb1 = ((nb >> 48) & 0xFF) + ((nb >> 32) & 0xFF00) + ((nb >> 16 & 0xFF0000)) + ((nb & 0xFF000000));
            long nb2 = ((nb >> 16) & 0xFF) + ((nb) & 0xFF00) + ((nb << 16) & 0xFF0000);
            _xdr.AddInt32((int)nb1);
            _xdr.AddInt32((int)nb2);
        }


        //public static string GenrateOneTxn(

        private static string ComposeXMLForTxn( TransactionType trxType, string xdrStr)
        {
            string s = "";
            try
            {
                s = "<Trx>";
                s += Utility.MakeTag("TxT", Convert.ToString((int)trxType));
                s += Utility.MakeTag("TSeq", Convert.ToString(SharedData.TransactionSeqNo));
                s += Utility.MakeTag("EqN", Convert.ToString(SharedData.EquipmentNumber));
                s += Utility.MakeTag("ESN", Convert.ToString(SharedData.EquipmentNumber));
                s += Utility.MakeTag("EqT", Convert.ToString((int)SharedData.EquipmentType));
                // DSM Number
                s += Utility.MakeTag("DSM","0");//TODO to be checked for real implementation

                s += Utility.MakeTag("Dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                s += Utility.MakeTag("BD", DatesUtility.BusinessDay(DateTime.Now, OverallParameters.EndBusinessDay).ToString("yyyy-MM-dd"));

                s += Utility.MakeTag("SP", Convert.ToString(SharedData.ServiceProvider));
                s += Utility.MakeTag("Line", Convert.ToString(SharedData.LineNumber));

                s += Utility.MakeTag("Sta", Convert.ToString(SharedData.StationNumber));
                //LLOC Logical location name of equipment.
                s += Utility.MakeTag("LLoc", " ");

                //AgID 	Logical location name of equipment.
                s += Utility.MakeTag("AgID", "");

                //AgP	Profile of Agent.
                s += Utility.MakeTag("AgP", "");

                //Data	XML field that contain additional data.
                s += Utility.MakeTag("Data", xdrStr);
                return s;
            }
            catch { return ""; }

        }

        /////// SHA1 Hashing/////

        private static byte[] CalculateSHA1Hash(string xmlstr)
        {
            byte[] result;
            byte[] data = Encoding.ASCII.GetBytes(xmlstr);
            SHA1 sha = new SHA1CryptoServiceProvider();
            // This is one implementation of the abstract class SHA1.
            result = sha.ComputeHash(data);

            return result;
        }


        ///// end of SHA1 Hashing/////


    }
}
