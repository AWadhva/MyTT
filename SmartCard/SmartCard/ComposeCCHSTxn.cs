using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using IFS2.Equipment.Common;
using System.Security.Cryptography;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CSCReader;

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
        public enum CCHS_TDSubType_Code
        {
            TXN_CSC_BLOCKED = 0x0020,
            TXN_CSC_BLACKLIST_ACTIONED = 0x0022,
            TXN_CSC_REJECTED = 0x0031,
            TXN_CSC_ADD_VALUE_CASH = 0x0080,
            TXN_CSC_ADD_VALUE_EFT = 0x0081
            //TXN_CSC_REPLACED = 0x0093,
            //TXN_CSC_LOST_STOLEN = 0x00CD,
            //TXN_CSC_UNBLOCKED = 0x0021,
        };

        const int _sizeInBytesGeneralHeader = 4 * (24 + 6 + 6), _sizeInBytesCscTpurse_header = 22 * 4, _sizeInBytesCSCUses_Header = 8 * 4;

        static readonly Dictionary<TransactionType, int> TxnTypeVsVariantDataLengthInBytes = new Dictionary<TransactionType, int>()
        {
            {TransactionType.CSCIssue, _sizeInBytesCSCUses_Header + 15*4},
            {TransactionType.TXN_CSC_ADD_VALUE_EFT, _sizeInBytesCscTpurse_header+ 43 * 4
                //,_sizeInBytesCscTpurse_header + 53*4
            }, // Check for correctness
            {TransactionType.AddValueCancel, _sizeInBytesCscTpurse_header + 8*4},
            {TransactionType.CashSurchargePayment, _sizeInBytesCSCUses_Header + 4*4},
            {TransactionType.EnableBankTopup, _sizeInBytesCscTpurse_header + 1*4},
            {TransactionType.DisableBankTopup, _sizeInBytesCscTpurse_header + 1*4},
            {TransactionType.TPurseBankTopupReload, _sizeInBytesCscTpurse_header + 8*4},
            {TransactionType.CSC_SURRENDERED, _sizeInBytesCSCUses_Header + 239*4},
            {TransactionType.CSCImmediateRefund, _sizeInBytesCSCUses_Header + 136 },
            {TransactionType.CSC_BAD_DEBT_CASH_PAYMENT, _sizeInBytesCscTpurse_header + 120 },
            {TransactionType.CSC_SURCHARGE_PAYMENT, _sizeInBytesCscTpurse_header + 40 },
            {TransactionType.InitialiseBankTopup, 1304},//_sizeInBytesCscTpurse_header + 1244
            {TransactionType.TPurseDeduction,_sizeInBytesCscTpurse_header+6*4+32},
            {TransactionType.BusCheckOutWithTPurse,_sizeInBytesCscTpurse_header+10*4}
        };

        static readonly Dictionary<TransactionType, short> TxnTypeVsItsCCHSSubTypeCode = new Dictionary<TransactionType, short>()
        {
            {TransactionType.CSCIssue, 0x0001},
            {TransactionType.TXN_CSC_ADD_VALUE_EFT, 0x0081},
            {TransactionType.AddValueCancel, 0x0082},
            {TransactionType.CashSurchargePayment, 0x00C6},
            {TransactionType.EnableBankTopup, 0x0049},
            {TransactionType.DisableBankTopup, 0x004B},
            {TransactionType.TPurseBankTopupReload, 0x0084},
            {TransactionType.CSC_SURRENDERED, 0x0092},
            {TransactionType.CSCImmediateRefund, 0x0090},
            {TransactionType.CSC_SURCHARGE_PAYMENT, 0x0069}, // as per 6.3.7, Not supported by current card layout. Implies that if we use it, it would be incorrect
            {TransactionType.CSC_BAD_DEBT_CASH_PAYMENT, 0x0076},
            {TransactionType.MediaReplacement, 0x0093},
            {TransactionType.InitialiseBankTopup, 0x0047},
            {TransactionType.TPurseDeduction, 0x0068},
            {TransactionType.BusCheckOutWithTPurse, 0x00B2},
            {TransactionType.MetroCheckInWithTPurse, 0x00B0},
            {TransactionType.MetroCheckInWithPass, 0x00B9},
            {TransactionType.MetroCheckOutWithTPurse, 0x0060},
            {TransactionType.MetroCheckOutWithPass, 0x0061}
        };

        static ComposeCCHSTxn()
        {
            _xdr = new XdrToXml();
        }

        //CSC Transaction Header Generation
        private static void GenerateTransactionGeneralHeader(int TxnSequenceNo, DateTime dt, int txnSubtype, byte CCHSTxnType, int TDVariantDataLengthInBytes, byte versionVariantPart)
        {
            // 6.4.1
            // string st = "";
            try
            {
                _xdr.AddInt8(0);
                _xdr.AddInt32(TxnSequenceNo); //TDSN_t  TD Sequence Number. Is taken from CCHS SAM

                int timestamp = DatesUtility.ConvertToUnixUTCTimestamp(dt);//ConvertToUnixTimestamp(dt);//TODO : to be checked whether this function gives unix time stamp of GMT 0 
                _xdr.AddInt32(timestamp);

                //Bussiness day
                DateTime startday = DateTime.Parse("1/1/1999");
                DateTime bd_day = DatesUtility.BusinessDay(DateTime.Now, OverallParameters.EndBusinessDay);//.ToString("dd/MM/yyyy");
                TimeSpan Ts_diff = bd_day.Subtract(startday);
                short days = (short)Ts_diff.Days;
                _xdr.AddInt16(days);
                int equipment_num = SharedData.EquipmentNumber & 0x0000FFFF;
                //Device ID 16(device type) +32 (Device Sequence Number_t)
                //_xdr.AddInt16((short)SharedData.EquipmentType); // Device type : TODO: to be checked
                switch (SharedData.EquipmentType)
                {
                    case EquipmentFamily.TOM:
                        _xdr.AddInt16(0xA1);
                        break;
                    case EquipmentFamily.TRcumAVM:
                        _xdr.AddInt16(0xA5); // A5 is code range for Samsung TR As of know putting this hard coded value for test 
                        break;
                    case EquipmentFamily.TVM:
                        _xdr.AddInt16(0xA2);
                        break;
                    case EquipmentFamily.RCT:
                        _xdr.AddInt16(0xA8);
                        break;
                    case EquipmentFamily.HHD:
                        _xdr.AddInt16(0xA7);
                        break;
                    case EquipmentFamily.GATE:
                        _xdr.AddInt16(0xA3);
                        break;
                    case EquipmentFamily.CVM:
                        _xdr.AddInt16(0xA9);
                        break;
                    default:
                        _xdr.AddInt16(0xA5); // ?
                        break;
                }
                _xdr.AddInt32(equipment_num); // Device Sequence Number

                //ParticipantID_t CC operator ID 8 bits
                _xdr.AddInt8((byte)SharedData.ServiceProvider);
                //8	ReferenceID_t. Shift Number             
                switch (SharedData.EquipmentType)
                {
                    case EquipmentFamily.TOM:
                    case EquipmentFamily.HHD:
                        if (SharedData._agentShift != null) _xdr.AddInt32(SharedData._agentShift.ShiftId);
                        else _xdr.AddInt32(0);
                        break;
                    default:
                        _xdr.AddInt32(0);
                        break;
                }

                //TD variant data length	16	TD_Length_t

                _xdr.AddInt16((short)TDVariantDataLengthInBytes);

                //TD_LogID_t 8bits
                _xdr.AddInt8(0);

                //8	LocationType_t station name
                //For Metro devices (E.g. DMRC, Reliance), this field will be encoded as per the value defined in
                //Device Specific PD.
                //  _xdr.AddInt8((byte)SharedData.StationNumber); // TODO: needed to verfied as per dmrc PD
                _xdr.AddInt8(0x00);// for metro station= 0x00
                //8	LocationCodeA_t 
                _xdr.AddInt8(0); // not required for AVM

                //16	LocationCodeB_t . station ID
                _xdr.AddInt16((short)SharedData.StationNumber);

                //TD Type	8	TD_Type_t Value 1 normally               
                _xdr.AddInt8(CCHSTxnType);

                //TD Sub Type	16	TD_SubType_t
                _xdr.AddInt16((short)txnSubtype);

                //TD variant data format version	8	TD_Version_t	Format version of variant part =0 for AVM/ or add value
                _xdr.AddInt8(versionVariantPart);

                //DSM ID	32	DSMID_t	Used to validate whether the data is generated by valid device.
                _xdr.AddInt32((int)SharedData.mDSMId);

                // Company Code	8	CompID_t	Company Code, for BS21 – 0x01 and BS22 – 0 x02
                _xdr.AddInt8(SharedData.CompanyID); // TODO : needed to clerify what is the AVM project code??

                AppendAgentIdToStream();

                //Shift ID	32	ShiftID_t	Identifies the shift in which the transaction is generated.
                _xdr.AddInt32(SharedData._agentShift == null ? 0 : SharedData._agentShift.ShiftId);

                //Filler	64	 	For Future Purposes
                _xdr.AddZero32(8);

            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Generate GeneralHeader Exception " + e.Message);
            }

            //return st;
        }

        private static void AppendAgentIdToStream()
        {
            //Agent ID	72	AgentID_t	Identifies the agent ID. (64+8)
            if (SharedData._agentShift == null)
            {
                _xdr.AddZero32(8); //64 bits //TODO: to be clarified whether dat in this field is nessessory
                _xdr.AddInt8(0); //8 bits
            }
            else
            {
                // It is purely hypothetical that it is like 8 ascii characters + 1 byte for agent profile
                _xdr.AddString(SharedData._agentShift.AgentId.ToString(), 8); // Also, not sure that AddString is good or not
                _xdr.AddInt8((byte)SharedData._agentShift.Profile);
            }
        }

        // 6.4.4
        private static void Generate_CSCNonMonetaryPurse_Usage_Txn_Header(LogicalMedia logMedia)
        {
            try
            {
                PhysicalSerialNumber(logMedia.Media.ChipSerialNumber);

                MediaType typ;
                switch (logMedia.Media.ChipTypeRead)
                {
                    case Media.ChipTypeValues.DesfireEV0:
                    case Media.ChipTypeValues.DesfireEV1:
                        typ = MediaType.CSC;
                        break;
                    case Media.ChipTypeValues.Sony:
                        typ = MediaType.SONY_CARD;
                        break;
                    default:
                        typ = MediaType.None; // TODO: See
                        break;
                }
                _xdr.AddInt8((byte)typ);

                //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)
                _xdr.AddInt8(0);

                //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
                _xdr.AddInt32(logMedia.Media.Owner); //TODO:

                short fareProducttype = logMedia.Application.Products.Product(0).Type;
                //Ticket Type	8	TicketType_t	Active Ticket type associated with Purse
                _xdr.AddInt8((byte)fareProducttype);

                _xdr.AddInt32(logMedia.Purse.TPurse.Balance);

                // TODO
                //Last Purse Purchased Price	16	SValueOneCent_t	The price paid for the Period Pass last purchased
                _xdr.AddInt16(0);

                //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
                _xdr.AddInt32(logMedia.Media.Owner);

                _xdr.AddInt8(0); // purse number

                short purseStartDate = 0; // TODO
                _xdr.AddInt16(purseStartDate);

                short purseValidityPeriod = 0; // TODO
                _xdr.AddInt16(purseValidityPeriod);

                byte purseLifecycleCount = 0; // TODO
                _xdr.AddInt8(purseLifecycleCount);

                byte purseClassification = 0; // TODO
                _xdr.AddInt8(purseClassification);

                byte transactionStatus = 0; // TODO
                _xdr.AddInt8(transactionStatus);

                byte auditGroup = 0; // TODO
                _xdr.AddInt8(auditGroup);

                int lastAddValueDevice = 0; // TODO
                _xdr.AddInt32(lastAddValueDevice);
            }
            catch
            {}
        }

        private static void Generate_CSCPurse_Usage_Txn_Header(LogicalMedia _logMedia, byte CSCTransactionStatus)
        {
            try
            {
                //CSC Physical ID	64	CSC_PhysicalID_t	Unique card number stored in CSC.
                PhysicalSerialNumber(_logMedia.Media.ChipSerialNumber);

                short fareProducttype = _logMedia.Application.Products.Product(0).Type; //TODO:  
                //CSC Type	8	CSC_Type_t	CSC type associated with Purse
                // _xdr.AddInt8(0);
                MediaType typ;
                switch (_logMedia.Media.ChipTypeRead)
                {
                    case Media.ChipTypeValues.DesfireEV0:
                    case Media.ChipTypeValues.DesfireEV1:
                        typ = MediaType.CSC;
                        break;
                    case Media.ChipTypeValues.Sony:
                        typ = MediaType.SONY_CARD;
                        break;
                    default:
                        typ = MediaType.None; // TODO: See
                        break;
                }
                _xdr.AddInt8((byte)typ);

                //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)
                _xdr.AddInt8(0);

                //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
                _xdr.AddInt32(_logMedia.Media.Owner); //TODO:

                //Ticket Type	8	TicketType_t	Active Ticket type associated with Purse
                _xdr.AddInt8((byte)fareProducttype);

                //Purse Life-Cycle count	8	Purse_LifeCycleID_t	Current Life Cycle of the Purse Not supported by current card layout.	0
                _xdr.AddInt8(0);

                //Purse Sequence Number (PSN)	32	PurseSequenceNumber_t                
                _xdr.AddInt32((int)Math.Abs(_logMedia.Purse.TPurse.SequenceNumber));

                //Purse Issuer ID	32	IssuerID_t	Issuer ID of the purse
                _xdr.AddInt32(_logMedia.Media.Owner);

                //Purse Number	8	PurseNumber_t	Unique Identification number assigned to the Pro duct / Purse e.g ‘1’ for ‘SV1’.The number allocated to this Purse (i.e. 0 for the AFC Monetary Purse)
                _xdr.AddInt8((byte)fareProducttype); //TODO: 

                //Last Purse Purchased Life Cycle count	8	PurseLifeCycleID_t  -- not applicable
                _xdr.AddInt8(0);

                //Last Add Value Participant ID	8	ParticipantID_t	Operator ID associated with last add value to this purse.
                _xdr.AddInt16(_logMedia.Purse.LastAddValue.ServiceProvider);

                //Last Add Value Type	8	AddValueType_t	Type of last add value associated with this purse
                _xdr.AddInt8((byte)_logMedia.Purse.LastAddValue.OperationType);

                //Last   Purse Purchased	8	PurseNumber_t	The unique number for the purse / product e.g ‘1 for ‘SV1’   last purchased.
                _xdr.AddInt8((byte)fareProducttype);

                //Last Add Value Date	16	CSC_Date_t	Date value last added to this purse
                // As per the Specification date values should be count in days calculated from 1-1-1999
                DateTime startday = DateTime.Parse("1/1/1999");
                TimeSpan Ts_diff = _logMedia.Purse.LastAddValue.DateTime.Subtract(startday);
                short days = (short)Ts_diff.Days;
                _xdr.AddInt16(days);

                //Last Add Value Amount	16	SValueOneCent_t	Value last added to purse
                _xdr.AddInt16((short)_logMedia.Purse.LastAddValue.Amount);

                //Last Add Value CSC-RW Device ID	32	CSC_RW_ID_t	CSC-RW Device ID associated with last add value to this purse.
                _xdr.AddInt32(_logMedia.Purse.LastAddValue.EquipmentNumber);

                ///Last Purse Purchased Price	16	SValueOneCent_t	The price paid for the Period Pass last purchased
                _xdr.AddInt16(0);

                //Last Purse Purchased Classification	8	PurseClassification_t	Type of Period Pass last purchased.(NA to DMRC)
                _xdr.AddInt8(0);

                //Bad Debt Sequence Number	8	BadDebtSequenceNumber _t	Sequence number associated with purse bad debt settlement
                _xdr.AddInt32((int)_logMedia.Purse.AutoReload.UnblockingSequenceNumber); 

                //Transaction Status	8	TransactionStatus_t	Indicates if the TD transaction is a normal or test transaction, and whether the update to the CSC was confirmed or unconfirmed.

                /*
                    0= Normal Transaction, CSC update confirmed
                    1 =Test Transaction, CSC update confirmed
                    2 = Normal Transaction, CSC update not confirmed
                    3 = Test Transaction, CSC update not confirmed
                    4..255 = for future use
                 */
                _xdr.AddInt8(CSCTransactionStatus); // 0= normal trasaction 

                //Audit Group	8	AuditGroupID_t	Audit Grouping associated with ticket (linking A R set update to UD Transaction generation)
                _xdr.AddInt8(0); // NA
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Critical, "Exception in CSCPurseUses Txn Header " + e.Message);
            }

        }

        /// <summary>
        /// This CSC Header will be used for Media Blocked, Media Rejected & Media Blacklist Actioned.
        /// size = 8 * 4= 32 bytes
        /// Amount is the number of 10s of paise.
        /// </summary>
        /// <param name="_logMedia"></param>
        private static void CSCUsesTxnHeader(LogicalMedia _logMedia, int DepositInNumOfTensOfPaise, int CSCTransactionStatus)
        {
            //CSC Physical ID	64	CSC_PhysicalID_t	Unique card number stored in CSC.
            PhysicalSerialNumber(_logMedia.Media.ChipSerialNumber);

            //CSC Type	8	CSC_Type_t	CSC type associated with CSC	Blank	No
            MediaType typ;
            switch (_logMedia.Media.ChipTypeRead)
            {
                case Media.ChipTypeValues.DesfireEV0:
                case Media.ChipTypeValues.DesfireEV1:
                    typ = MediaType.CSC;
                    break;
                case Media.ChipTypeValues.Sony:
                    typ = MediaType.SONY_CARD;
                    break;
                default:
                    typ = MediaType.None; // TODO: See
                    break;
            }
            _xdr.AddInt8((byte)typ);

            //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)	0
            _xdr.AddInt8(0);

            //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
            _xdr.AddInt32(_logMedia.Media.Owner); //TODO

            //CSC Deposit Reference Code	8	CSC_DepositRef_t	Code identifying the deposit amount. This will be equal to the Deposit Amount in Rupees.
            _xdr.AddInt8((byte)(DepositInNumOfTensOfPaise / 10));

            //Ticket Type	8	TicketType_t	Active Ticket type associated with CSC
            short fareProducttype = 0;
            try
            {
                fareProducttype = _logMedia.Application.Products.Product(0).Type; //TODO:                  
            }
            catch
            {
                Logging.Log(LogLevel.Error, "Exception: Unknown Fare product Type ");
            }
            _xdr.AddInt8((byte)fareProducttype);
            //Transaction Status	8	TransactionStatus_t	Indicates if the TD transaction is a normal or test transaction, and whether the update to the CSC was confirmed or unconfirmed.
            _xdr.AddInt8((byte)CSCTransactionStatus);
            /*
             * 0 = Normal Transaction, CSC update confirmed
                1 =Test Transaction, CSC update confirmed
                2 = Normal Transaction, CSC update not confirmed
                3 = Test Transaction, CSC update not confirmed
             */
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

        internal static void GenrateTACfromCCHSSam(int TxnSeq, bool updateSAMSeq)
        {
            byte[] TAC = null;
            uint tac = 0;
#if _BLUEBIRD_ 
            int ret = -1;
          ret = (int)  Reader.GenerateTAC(_xdr.Result, (int)_xdr.Result.Length - 4, out TAC);
          if (ret == 0)
          {
              Logging.Log(LogLevel.Verbose, "TAC : " + TAC[0].ToString("X2") + " " + TAC[1].ToString("X2") + " " + TAC[2].ToString("X2") + " " + TAC[3].ToString("X2"));

              tac |= (uint)TAC[3];
              tac |= (uint)TAC[2] << 8;
              tac |= (uint)TAC[1] << 16;
              tac |= (uint)TAC[0] << 24;

              _xdr.AddInt32(tac);
              if (updateSAMSeq == true)
              {
                  Reader.WriteSAMSequence(TxnSeq);
              }
          }
          else
          {
              _xdr.AddInt32(0);
              Logging.Log(LogLevel.Error, " ComposeCCHSTxn Failed to genrate TAC");
          }
#else
            CCHSSAMManger mCCHSSamManger = new CCHSSAMManger(_ReaderType, _hRW);
            foreach (cSAMConf samcnf in SharedData.mSAMUsed)
            {
                if (samcnf.mSAMType == CONSTANT.SAMType.ISAM)
                {

                    mCCHSSamManger.GenerateTAC((DEST_TYPE)samcnf.SAM_Slot, _xdr.Result, (int)_xdr.Result.Length - 4, out TAC);
                    Logging.Log(LogLevel.Verbose, "TAC : " + TAC[0].ToString("X2") + " " + TAC[1].ToString("X2") + " " + TAC[2].ToString("X2") + " " + TAC[3].ToString("X2"));

                    tac |= (uint)TAC[3];
                    tac |= (uint)TAC[2] << 8;
                    tac |= (uint)TAC[1] << 16;
                    tac |= (uint)TAC[0] << 24;

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
#endif

        }

        private static void PhysicalSerialNumber(long nb)
        {
            nb = nb << 8;
            //_xdr.AddInt32((int)((nb>>24) & 0xFFFFFFFF));
            // _xdr.AddInt32((int)((nb<<8) & 0xFFFFFFFF));
            _xdr.AddInt32((int)((nb >> 32) & 0xFFFFFFFF));
            _xdr.AddInt32((int)((nb) & 0xFFFFFFFF));
        }

        private static void TXN_CSC_REJECTED(LogicalMedia _logMedia, int Amount, int rejecttionCode, int TxnSeq)
        {
            CSCUsesTxnHeader(_logMedia, Amount, 2); // 
            //Rejection Reason	8	CSC_RejectReason_t	Reason for rejecting use of CSC on this device
            _xdr.AddInt8((byte)rejecttionCode);

            // Rejection Code	8	CSC_RejectCode_t	Rejection code associated with rejection,
            _xdr.AddInt8((byte)rejecttionCode);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record
            //          (inclusive)

            GenrateTACfromCCHSSam(TxnSeq, true);
        }

        //3.1.2.	Media Blocked (TXN_CSC_BLOCKED)
        private static void TXN_CSC_BLOCKED(LogicalMedia _logMedia, int TxnSeq)
        {
            CSCUsesTxnHeader(_logMedia, 0, 2);

            //CSC Status Code	8	CSC_StatusCode_t	Blocking Status written to CSC
            //= 1 (Unblocked) 
            if (_logMedia.Media.Blocked)
                _xdr.AddInt8((byte)_logMedia.Media.ReasonOfBlocking);
            else _xdr.AddInt8(1);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record
            //          (inclusive)

            GenrateTACfromCCHSSam(TxnSeq, true);
        }

        //
        private static void TXN_CSC_BLACKLIST_ACTIONED(LogicalMedia _logMedia, int TxnSeq)
        {
            CSCUsesTxnHeader(_logMedia, 0, 0);

            //Blacklist Reason Code	8	CSC_StatusCode_t	Blacklisting Reason Status identified in Blacklist
            //Please refer Appendix B for list of Blacklist Rea son Codes used with CCHS.
            byte reason = (byte)_logMedia.Media.ReasonOfBlocking;
            _xdr.AddInt8(reason);

            //Blacklist Action Code	8	CSC_Actions_t	The CSC Action Code as determined by reference e to the appropriate entry in the CSC Blacklist Action Table.
            //NOTE: As Blacklist Parameters does not have Action Code, so this field will carry default value as 255
            _xdr.AddInt8((byte)255);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record
            //          (inclusive)

            GenrateTACfromCCHSSam(TxnSeq, true);

        }

        private static void Generate_CSCTopup_Txn_80(LogicalMedia _logMedia, int addvalueAmount, int TxnSeq, bool bAsPartOfIssue)
        {
            //6.3.4

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

            //CSC Deposit Reference Code	8	CSC_DepositRef_t	Code  identifying the deposit amount. This will be
            //equal to the Deposit Amount in Rupees.
            if (_logMedia.Application.TransportApplication.Deposit > 0)
                _xdr.AddInt32(_logMedia.Application.TransportApplication.Deposit / 100);
            else _xdr.AddInt32(0);

            //Sales operation	8	OperationType_t //not applicable for AVM
            //not applicable
            /*
             * 1 if add-value operation is part of a sales operation
                2  if add value  operation  is part  of a replacement operation
                0 otherwise
             * */
            if (bAsPartOfIssue)
                _xdr.AddInt8(1);
            else
                _xdr.AddInt8(0);

            //TAC	32	TD_MAC_t	4 byte TAC applied to all data between fields 1 of the TD Header and the end of the TD record (inclusive)

            GenrateTACfromCCHSSam(TxnSeq, true);
        }


        public static bool TreatXDRCompatibility(LogicalMedia logMedia, 
            out string OneTxnStrData, // return value is the result of SerializeHelper<byte[]>.Serialize on a byte array (_xdr.Result)
            TransactionType type,             
            int TxnSeqeuence, int AddvalueAmnt, int hRW, CSC_READER_TYPE Readertype, bool bWTE, bool bTest, bool bAsPartOfIssue, object miscell)
        {
            _hRW = hRW;
            _ReaderType = Readertype;
            _xdr = new XdrToXml();

            //  Logging.Log(LogLevel.Verbose, "C");
            //AddValueTransaction(s);
            /*
               0= Normal Transaction, CSC update confirmed
               1 =Test Transaction, CSC update confirmed
               2 = Normal Transaction, CSC update not confirmed
               3 = Test Transaction, CSC update not confirmed
               4..255 = for future use
            */
            byte Txnstatus;

            bWTE = false; // this is necessary.
            if (bTest)
            {
                if (bWTE)
                    Txnstatus = 3;
                else
                    Txnstatus = 1;
            }
            else
            {
                if (bWTE)
                    Txnstatus = 2;
                else
                    Txnstatus = 0;
            }

            //  txnType = (TransactionType)type;
            try
            {
                //  int type = Utility.SearchSimpleCompleteTagInt(s, "", "TxT");
                // 24*4 (XDR) bytes for General header 
                //22*4 (XDR ) Bytes for CSC purse uses header
                //20*4 bytes for Media Top Up (TXN_CSC_AFF_VALUE_EFT)
                int delhiTrx = 0,TDLenght=0;
                int size = 8;
                int CCHS_TXN_Type = 0;
                // size is calculated as number of 32 bits.
                int size_generalHeader = 24 + 6 + 6, size_cscTpurse_header = 22, size_cscTopUP = 41 /*34+6+1*/, size_CSCUses_Header = 8, size_cscTopUPType_80=8;
                switch (type)
                {
                    case TransactionType.BlacklistDetection:
                        TDLenght = size_CSCUses_Header + 3;// 3 = Lenth of Blacklist Actioned field
                        size = size_generalHeader + TDLenght;
                        delhiTrx = 9;
                        CCHS_TXN_Type =(int) CCHS_TDSubType_Code.TXN_CSC_BLACKLIST_ACTIONED;
                        break;

                    case TransactionType.MediaRejection:
                        TDLenght =  size_CSCUses_Header + 3; //3= data lenght of MEdia Rejection
                        size = size_generalHeader + TDLenght;
                        CCHS_TXN_Type = (int)CCHS_TDSubType_Code.TXN_CSC_REJECTED;
                        break;

                    case TransactionType.MediaBlocked:
                        TDLenght = size_CSCUses_Header + 2;// 2 = Lenth of Blacklisted card detected field
                        size = size_generalHeader + TDLenght;
                        CCHS_TXN_Type = (int)CCHS_TDSubType_Code.TXN_CSC_BLOCKED;
                        break;
                    case TransactionType.TPurseDirectReload:
                    case TransactionType.TPurseWebTopupReload:
                        // size = 25;
#if false
                        {
                            TDLenght = size_cscTpurse_header + size_cscTopUP;
                            size = size_generalHeader + TDLenght;
                            delhiTrx = 4;
                            CCHS_TXN_Type = (int)CCHS_TDSubType_Code.TXN_CSC_ADD_VALUE_EFT;
                        }
#else
                        {
                            TDLenght = size_cscTpurse_header + size_cscTopUPType_80;
                            size = size_generalHeader + TDLenght;
                            delhiTrx = 4;
                            CCHS_TXN_Type = (int)CCHS_TDSubType_Code.TXN_CSC_ADD_VALUE_CASH;
                            break;
                        }
                    case TransactionType.TXN_CSC_ADD_VALUE_EFT:
                        {
                            int TDVariantDataLengthInBytes = TxnTypeVsVariantDataLengthInBytes[TransactionType.TXN_CSC_ADD_VALUE_EFT];
                            short CCHS_TXN_Type2 = TxnTypeVsItsCCHSSubTypeCode[TransactionType.TXN_CSC_ADD_VALUE_EFT];
                            _xdr = new XdrToXml();

                            _xdr.InitResult(_sizeInBytesGeneralHeader + TDVariantDataLengthInBytes);
                            byte variantDataFormatVersion = 0;
                            GenerateTransactionGeneralHeader(SharedData.TransactionSeqNo, DateTime.Now, (int)CCHS_TXN_Type2, 0x01, TDVariantDataLengthInBytes, variantDataFormatVersion);
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            IFS2.Equipment.Common.CCHS.BankTopupDetails bankTop = (IFS2.Equipment.Common.CCHS.BankTopupDetails)miscell;
                            FldsCSCPeformAddValueViaEFT body = new FldsCSCPeformAddValueViaEFT();
                            body.addValAmt = AddvalueAmnt;
                            body.authorizationCode = bankTop.authorizationCode;
                            body.bankTerminalId = bankTop.TerminalID;                            
                            body.depositInCents = logMedia.Application.TransportApplication.Deposit;
                            body.dtTimeBankTerminalTransaction = bankTop.dt;
                            body.operationType = OperationType_t.Other;
                            body.purseRemainingVal = logMedia.Purse.TPurse.Balance;
                            body.resoponseCode = bankTop.responseCodeFromTerminal;
                            body.systemTraceAuditNumber = bankTop.SystemTraceAuditNumber;

                            body.EmbedContentsToXdr(_xdr);
                            GenrateTACfromCCHSSam(TxnSeqeuence, true);
                            OneTxnStrData = SerializeHelper<byte[]>.XMLSerialize(_xdr.Result);
                            return true;
                        }
#endif
                        
                }

                Logging.Log(LogLevel.Verbose, "nb:" + size.ToString() + " Trx:" + delhiTrx.ToString());
                _xdr.InitResult(size * 4);
                // _xdr.AddInt32(1); //Header tag Transaction. Hypothesis that there is only one UD file type.
                GenerateTransactionGeneralHeader(TxnSeqeuence, DateTime.Now, CCHS_TXN_Type, 0x01, (TDLenght * 4), 0);
                switch (type)
                {
                    case TransactionType.BlacklistDetection:                        
                        TXN_CSC_BLACKLIST_ACTIONED(logMedia, TxnSeqeuence);
                        break;
                    case TransactionType.MediaRejection:
                        TXN_CSC_REJECTED(logMedia, AddvalueAmnt, SharedData._rejectionCode, TxnSeqeuence);
                        SharedData._rejectionCode = 0;
                        break;
                    case TransactionType.MediaBlocked:
                        TXN_CSC_BLOCKED(logMedia, TxnSeqeuence);
                        break;
                    case TransactionType.TPurseDirectReload:
                    case TransactionType.TPurseWebTopupReload:
                        {
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            // MediaTopUpData(logMedia, AddvalueAmnt, TxnSeqeuence);
                            Generate_CSCTopup_Txn_80(logMedia, AddvalueAmnt, TxnSeqeuence, bAsPartOfIssue);
                            break;
                        }
                }
               OneTxnStrData = SerializeHelper<byte[]>.XMLSerialize(_xdr.Result);
                 
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Exception in ComposeCCHS Txn TreatXDRComp " + e.Message + " Transaction:");
            }
            OneTxnStrData = null;
            return false;
        }  
        
        public static bool TreatXDRCompatibility2(LogicalMedia logMedia, out string OneTxnStrData,
            TransactionType type,
            int TxnSeqeuence, int hRW, CSC_READER_TYPE Readertype, bool bWTE, bool bTest, object parsTxnSpecific)
        {
            _hRW = hRW;
            _ReaderType = Readertype;
            _xdr = new XdrToXml();

            //  Logging.Log(LogLevel.Verbose, "C");
            //AddValueTransaction(s);
            /*
               0= Normal Transaction, CSC update confirmed
               1 =Test Transaction, CSC update confirmed
               2 = Normal Transaction, CSC update not confirmed
               3 = Test Transaction, CSC update not confirmed
               4..255 = for future use
            */
            byte Txnstatus;
            bWTE = false; // this is necessary
            if (bTest)
            {
                if (bWTE)
                    Txnstatus = 3;
                else
                    Txnstatus = 1;
            }
            else
            {
                if (bWTE)
                    Txnstatus = 2;
                else
                    Txnstatus = 0;
            }

            try
            {
                //  int type = Utility.SearchSimpleCompleteTagInt(s, "", "TxT");
                // 24*4 (XDR) bytes for General header 
                //22*4 (XDR ) Bytes for CSC purse uses header
                //20*4 bytes for Media Top Up (TXN_CSC_AFF_VALUE_EFT)
                int delhiTrx = 0;                
                short CCHS_TXN_Type;
                // How are these constants calculated; e.g. as per the document, size of M_PurseUsageHeader_t (size_cscTpurse_header) is 344 bits i.e. 43 bytes; but is specified as only 22 bytes here
                // Seems it is in terms of number of 2 byte pairs. That's why it is being multiplied by 4 instead of 8.
                //int size_generalHeader = 24 + 6 + 6, size_cscTpurse_header = 22, size_cscTopUP = 41 /*34+6+1*/, size_CSCUses_Header = 8, size_cscTopUPType_80 = 8;
                int TDVariantDataLengthInBytes = TxnTypeVsVariantDataLengthInBytes[type];
                CCHS_TXN_Type = TxnTypeVsItsCCHSSubTypeCode[type];

                int xdrLen;
                switch (type)
                {
                    case TransactionType.MetroCheckOutWithTPurse:
                    case TransactionType.MetroCheckOutWithPass:
                    case TransactionType.MetroCheckInWithPass:
                    case TransactionType.MetroCheckInWithTPurse:
                        xdrLen = 4000; // TO BE CORRECTED
                        break;
                    default:
                        xdrLen = _sizeInBytesGeneralHeader + TDVariantDataLengthInBytes;
                        break;
                }
                _xdr.InitResult(xdrLen);
                byte variantDataFormatVersion;
                switch (type)
                {
                    case TransactionType.CSC_SURRENDERED:
                    case TransactionType.CSC_BAD_DEBT_CASH_PAYMENT:
                    case TransactionType.CSC_SURCHARGE_PAYMENT:
                    case TransactionType.MediaLost:
                    case TransactionType.BusCheckOutWithTPurse:
                    case TransactionType.InitialiseBankTopup:
                    case TransactionType.MetroCheckOutWithTPurse:
                    case TransactionType.MetroCheckOutWithPass:
                    case TransactionType.MetroCheckInWithPass:
                    case TransactionType.MetroCheckInWithTPurse:
                        variantDataFormatVersion = 1;
                        break;
                    default:
                        variantDataFormatVersion = 0;
                        break;
                }
                GenerateTransactionGeneralHeader(TxnSeqeuence, DateTime.Now, (int)CCHS_TXN_Type, 0x01, TDVariantDataLengthInBytes, variantDataFormatVersion);

                switch (type)
                {
                    case TransactionType.CSCIssue:
                        {
                            // 6.3.3
                            CSCUsesTxnHeader(logMedia, logMedia.Application.TransportApplication.Deposit / 10, Txnstatus);

                            FldsCSCIssueTxn pars = (FldsCSCIssueTxn)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.EnableBankTopup:
                        {
                            // 6.3.16
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            
                            break;
                        }
                    case TransactionType.DisableBankTopup:
                        {
                            // 6.3.17
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            break;
                        }
                    case TransactionType.CSC_SURCHARGE_PAYMENT:
                        {
                            // 6.3.7
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCSurchargePayment pars = (FldsCSCSurchargePayment)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.CashSurchargePayment:
                        {
                            // 6.3.28
                            CSCUsesTxnHeader(logMedia, logMedia.Application.TransportApplication.Deposit/10, Txnstatus);
                            FldsCashSurchargePayment pars = (FldsCashSurchargePayment)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.CSCImmediateRefund:
                        {
                            // 6.3.20
                            CSCUsesTxnHeader(logMedia, logMedia.Application.TransportApplication.Deposit/10, Txnstatus);
                            FldsCSCImmediateRefund pars = (FldsCSCImmediateRefund)parsTxnSpecific;                            
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.AddValueCancel:
                        {
                            // 6.3.6
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCAddValueCancel pars = (FldsCSCAddValueCancel)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.MediaReplacement:
                        {
                            CSCUsesTxnHeader(logMedia, logMedia.Application.TransportApplication.Deposit / 10, Txnstatus);
                            FldsCSCReplacement pars = (FldsCSCReplacement)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);

                            break;
                        }
                    case TransactionType.TPurseBankTopupReload:
                        {
                            // 6.3.18
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCPeformAddValueViaBankTopup pars = (FldsCSCPeformAddValueViaBankTopup)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.CSC_SURRENDERED:
                        {
                            // 6.3.19
                            CSCUsesTxnHeader(logMedia, logMedia.Application.TransportApplication.Deposit / 10, Txnstatus);
                            FldsCSCSurrendered pars = (FldsCSCSurrendered)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);

                            break;
                        }
                    case TransactionType.InitialiseBankTopup:
                        {
                            // 6.3.15
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsInitialiseBankTopup pars = (FldsInitialiseBankTopup)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);

                            break;
                        }
                    case TransactionType.CSC_BAD_DEBT_CASH_PAYMENT:
                        {
                            //6.3.29
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCBadDebtCashPayment pars = (FldsCSCBadDebtCashPayment)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);

                            break;
                        }
                    case TransactionType.TXN_CSC_ADD_VALUE_EFT:
                        {
                            // 6.3.5
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCPeformAddValueViaEFT pars = (FldsCSCPeformAddValueViaEFT)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);

                            break;
                        }
                    case TransactionType.TPurseDeduction:
                        {
                            // 6.3.13
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCPurseDeduction pars = (FldsCSCPurseDeduction)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.BusCheckOutWithTPurse:
                        {
                            // 6.3.9
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCBusCheckOutRebate pars = (FldsCSCBusCheckOutRebate)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.MetroCheckOutWithTPurse:
                        {
                            // 6.3.10
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCTrainFareDeduction pars = (FldsCSCTrainFareDeduction)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.MetroCheckOutWithPass:
                        {
                            // 6.3.11
                            Generate_CSCNonMonetaryPurse_Usage_Txn_Header(logMedia);
                            FldsCSCTrainRideDeduction pars = (FldsCSCTrainRideDeduction)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.MetroCheckInWithTPurse:
                        {
                            // 6.3.31
                            Generate_CSCPurse_Usage_Txn_Header(logMedia, Txnstatus);
                            FldsCSCTrainEntry pars = (FldsCSCTrainEntry)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    case TransactionType.MetroCheckInWithPass:
                        {
                            // 6.3.32
                            Generate_CSCNonMonetaryPurse_Usage_Txn_Header(logMedia);
                            FldsCSCTrainPassEntry pars = (FldsCSCTrainPassEntry)parsTxnSpecific;
                            pars.EmbedContentsToXdr(_xdr);
                            break;
                        }
                    default:
                        throw new NotImplementedException();
                }
                GenrateTACfromCCHSSam(TxnSeqeuence, true);

                Logging.Log(LogLevel.Verbose, " Trx:" + delhiTrx.ToString());

                OneTxnStrData = SerializeHelper<byte[]>.XMLSerialize(_xdr.Result);
                 
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Exception in ComposeCCHS Txn TreatXDRComp " + e.Message + " Transaction:");
            }
            OneTxnStrData = null;
            return false;
        }

        ///// end of SHA1 Hashing/////

        public static bool UnreadableCSC(TransactionType type,
            int TxnSeqeuence, 
            int hRw,
            CSC_READER_TYPE Readertype, 
            bool bTest, 
            long physicalId, 
            int owner, 
            int DepositInTensOfPaise, 
            int fareProduct, 
            object parsTxnSpecific,
            out string OneTxnStrData)
        {
            _hRW = hRw;
            _ReaderType = Readertype;
            _xdr = new XdrToXml();

            byte Txnstatus;
            if (bTest)
                Txnstatus = 1;
            else
                Txnstatus = 0;

            short CCHS_TXN_Type;
            int TDVariantDataLengthInBytes = TxnTypeVsVariantDataLengthInBytes[type];
            CCHS_TXN_Type = TxnTypeVsItsCCHSSubTypeCode[type];
            _xdr.InitResult(_sizeInBytesGeneralHeader + TDVariantDataLengthInBytes);

            byte variantDataFormatVersion;
            switch (type)
            {
                case TransactionType.CSC_SURRENDERED:
                case TransactionType.CSC_BAD_DEBT_CASH_PAYMENT:
                case TransactionType.CSC_SURCHARGE_PAYMENT:
                case TransactionType.MediaLost:
                    variantDataFormatVersion = 1;
                    break;
                default:
                    variantDataFormatVersion = 0;
                    break;
            }

            GenerateTransactionGeneralHeader(TxnSeqeuence, DateTime.Now, (int)CCHS_TXN_Type, 0x01, TDVariantDataLengthInBytes, variantDataFormatVersion);

            switch (type)
            {
                case TransactionType.CSC_SURRENDERED:
                    {
                        // 6.3.19
                        //CSCUsesTxnHeader
                        PhysicalSerialNumber(physicalId);
                        _xdr.AddInt8((byte)MediaType.CSC);

                        //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)	0
                        _xdr.AddInt8(0);

                        _xdr.AddInt32(owner);

                        //CSC Deposit Reference Code	8	CSC_DepositRef_t	Code identifying the deposit amount. This will be equal to the Deposit Amount in Rupees.
                        _xdr.AddInt8((byte)(DepositInTensOfPaise / 10));

                        _xdr.AddInt8((byte)fareProduct);

                        //Transaction Status	8	TransactionStatus_t	Indicates if the TD transaction is a normal or test transaction, and whether the update to the CSC was confirmed or unconfirmed.
                        _xdr.AddInt8(Txnstatus);

                        FldsCSCSurrendered pars = (FldsCSCSurrendered)parsTxnSpecific;
                        pars.EmbedContentsToXdr(_xdr);
                        break;
                    }
                case TransactionType.CSC_BAD_DEBT_CASH_PAYMENT:
                    {
                        //6.3.29
                        Generate_CSCPurse_Usage_Txn_Header_UnreadableCSC(TxnSeqeuence, bTest, physicalId, owner, DepositInTensOfPaise, fareProduct);
                        FldsCSCBadDebtCashPayment pars = (FldsCSCBadDebtCashPayment)parsTxnSpecific;
                        pars.EmbedContentsToXdr(_xdr);

                        break;
                    }
                case TransactionType.DisableBankTopup:
                    {
                        // 6.3.17
                        Generate_CSCPurse_Usage_Txn_Header_UnreadableCSC(TxnSeqeuence, bTest, physicalId, owner, DepositInTensOfPaise, fareProduct);
                        break;
                    }
            }
            GenrateTACfromCCHSSam(TxnSeqeuence, true);            

            OneTxnStrData = SerializeHelper<byte[]>.XMLSerialize(_xdr.Result);
             
            return true;
        }

        private static void Generate_CSCPurse_Usage_Txn_Header_UnreadableCSC(int seqNum, bool bTest, long physicalId, int owner, int DepositInTensOfPaise, int fareProduct)
        {
            //CSC Physical ID	64	CSC_PhysicalID_t	Unique card number stored in CSC.
            PhysicalSerialNumber(physicalId);

            MediaType typ = MediaType.CSC;
            
            _xdr.AddInt8((byte)typ);

            //CSC Life-Cycle count	8	CSC_LifeCycleID_t	Current Life Cycle of the CSC (Not supported by current card layout.)
            _xdr.AddInt8(0);

            //CSC Issuer ID	32	IssuerID_t	Issuer ID of the CSC
            _xdr.AddInt32(owner); //TODO:

            byte fareProducttype = 0;
            //Ticket Type	8	TicketType_t	Active Ticket type associated with Purse
            _xdr.AddInt8(fareProducttype);

            //Purse Life-Cycle count	8	Purse_LifeCycleID_t	Current Life Cycle of the Purse Not supported by current card layout.	0
            _xdr.AddInt8(0);

            //Purse Sequence Number (PSN)	32	PurseSequenceNumber_t
            _xdr.AddInt32(seqNum);

            //Purse Issuer ID	32	IssuerID_t	Issuer ID of the purse
            _xdr.AddInt32(owner);

            //Purse Number	8	PurseNumber_t	Unique Identification number assigned to the Pro duct / Purse e.g ‘1’ for ‘SV1’.The number allocated to this Purse (i.e. 0 for the AFC Monetary Purse)
            _xdr.AddInt8(fareProducttype); //TODO: 

            //Last Purse Purchased Life Cycle count	8	PurseLifeCycleID_t  -- not applicable
            _xdr.AddInt8(0);

            //Last Add Value Participant ID	8	ParticipantID_t	Operator ID associated with last add value to this purse.
            _xdr.AddInt16(0);

            //Last Add Value Type	8	AddValueType_t	Type of last add value associated with this purse
            _xdr.AddInt8(0);

            //Last   Purse Purchased	8	PurseNumber_t	The unique number for the purse / product e.g ‘1 for ‘SV1’   last purchased.
            _xdr.AddInt8(0);

            //Last Add Value Date	16	CSC_Date_t	Date value last added to this purse
            // As per the Specification date values should be count in days calculated from 1-1-1999
            DateTime startday = DateTime.Parse("1/1/1999");
            //TimeSpan Ts_diff = _logMedia.Purse.LastAddValue.DateTime.Subtract(startday);
            //short days = (short)Ts_diff.Days;
            short days = 0;
            _xdr.AddInt16(days);

            //Last Add Value Amount	16	SValueOneCent_t	Value last added to purse
            //_xdr.AddInt16((short)_logMedia.Purse.LastAddValue.Amount);
            _xdr.AddInt16(0);

            //Last Add Value CSC-RW Device ID	32	CSC_RW_ID_t	CSC-RW Device ID associated with last add value to this purse.
            //_xdr.AddInt32(_logMedia.Purse.LastAddValue.EquipmentNumber);
            _xdr.AddInt32(0);

            ///Last Purse Purchased Price	16	SValueOneCent_t	The price paid for the Period Pass last purchased
            _xdr.AddInt16(0);

            //Last Purse Purchased Classification	8	PurseClassification_t	Type of Period Pass last purchased.(NA to DMRC)
            _xdr.AddInt8(0);

            //Bad Debt Sequence Number	8	BadDebtSequenceNumber _t	Sequence number associated with purse bad debt settlement
            //_xdr.AddInt32((int)_logMedia.Purse.AutoReload.UnblockingSequenceNumber);
            _xdr.AddInt32(0);

            //Transaction Status	8	TransactionStatus_t	Indicates if the TD transaction is a normal or test transaction, and whether the update to the CSC was confirmed or unconfirmed.

            /*
                0= Normal Transaction, CSC update confirmed
                1 =Test Transaction, CSC update confirmed
                2 = Normal Transaction, CSC update not confirmed
                3 = Test Transaction, CSC update not confirmed
                4..255 = for future use
             */
            if (bTest)
                _xdr.AddInt8(0); // 0= normal trasaction 
            else
                _xdr.AddInt8(1); 

            //Audit Group	8	AuditGroupID_t	Audit Grouping associated with ticket (linking A R set update to UD Transaction generation)
            _xdr.AddInt8(0); // NA
        }
    }
}
