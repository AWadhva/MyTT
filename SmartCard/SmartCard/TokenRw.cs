// Before taking its services, handle of the reader should be set by calling SetRWHandle

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{
    public class DelhiTokenUltralight : CommonHwMedia
    {
        int _hRw = 0; // Giving it a valid value, because don't want to disturb HHD code
        public DelhiTokenUltralight(SmartFunctions sf, int hRw):base(sf) { _hRw = hRw; }

        //private const int NUMBITSINONEBLOCK = 16 * 8;

        protected override Boolean _ReadMediaData(LogicalMedia logMedia, MediaDetectionTreatment readTreatment)
        {
            byte[] pResData;
            if (SharedData.EquipmentType == EquipmentFamily.TOM)
                return ReadMediaData3(logMedia, true, readTreatment);
            else
                return ReadMediaData2(logMedia, true, out pResData);
        }

        bool ReadMediaData3(LogicalMedia logMedia, bool bPopulateRawData, MediaDetectionTreatment readTreatment)
        {
            if (!_bTokenRead)
            {
                byte[] tempData;
                _bTokenRead = ReadMediaData2(logMedia, bPopulateRawData, out tempData);
                return _bTokenRead;
            }
            return true;
        }

        bool ReadMediaData2(LogicalMedia logMedia, bool bPopulateRawData, out byte[] pResData)
        {
            return ReadMediaData2(logMedia, bPopulateRawData, out pResData, out Err);
        }

        bool _bTokenRead = false;

        protected override void _Reset()
        {
            _bTokenRead = false;
        }

        public bool ReadMediaData2(LogicalMedia logMedia, bool bPopulateRawData, out byte[] pResData, out CSC_API_ERROR Err)
        {
            Err = CSC_API_ERROR.ERR_NONE;

            pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];

            {
                byte pSw1 = 0xFF;
                byte pSw2 = 0xFF;

                Media m = logMedia.Media;

                Initialisation ini = logMedia.Initialisation;
                TransportApplication ta = logMedia.Application.TransportApplication;
                LocalLastAddValue lcav = logMedia.Application.LocalLastAddValue;
                Customer cu = logMedia.Application.Customer;
                Validation val = logMedia.Application.Validation;

                Products ps = logMedia.Application.Products;
                OneProduct p = new OneProduct();
                ps.Add(p);

                //Manufacturer Block is read By Default, so Number of Blocks is excluding
                //block 0
                Err = TokenFunctions.ReadBlocks(CSC_READER_TYPE.V4_READER,
                    _hRw,
                    3, out pSw1, out pSw2, out pResData);
                if (Err != CSC_API_ERROR.ERR_NONE)
                    Logging.Log(LogLevel.Verbose, "DelhiTokenUltralight::ReadMediaData2 Err = " + Err.ToString());
                if (Err == CONSTANT.NO_ERROR && pSw1 != CONSTANT.COMMAND_SUCCESS)
                    Logging.Log(LogLevel.Verbose, "DelhiTokenUltralight::ReadMediaData2 pSw1 = " + pSw1.ToString());

                if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                {
                    logMedia._tokenPhysicalData = new byte[64];
                    Array.Copy(pResData, 1, logMedia._tokenPhysicalData, 0, 64);
                    // NOTE THAT pResData has 8-bits in the beginning, that DON'T BELONG TO TOKEN DATA
                    pResData = logMedia._tokenPhysicalData;


                    // Sale Block Layout
                    m.ChipSerialNumberRead = sf.ReadSNbr();//(long)CFunctions.GetBitData(0, 56, pResData);
                    m.HardwareTypeRead = Media.HardwareTypeValues.TokenUltralight;
                    m.ChipTypeRead = Media.ChipTypeValues.UltralightC;
                    m.TypeRead = Media.TypeValues.Token;

                    short typ = (short)CFunctions.GetBitData(1 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS + 32, 8, pResData);
#if !_HHD_
                    if (typ == CONSTANT.TICKET_TYPE_TTAG)
                    {
                        _ReadTTag(logMedia, pResData);
                        logMedia.TTag.Hidden = false;
                        return true;
                    }
#endif
                    int version = TokenFunctions.ExtractVersion(pResData);
                    ISTDParser stdParser;
                    if (version == 1)
                        stdParser = new SaleTokenParser_Ver1(pResData);
                    else
                        stdParser = new SaleTokenParser_Ver0(pResData);
                    {
#if !_HHD_
                        logMedia.TTag.Hidden = true;
#endif

                        m.InitialisationDateRead = stdParser.Initialisationdate();
                        ini.DateTimeRead = m.InitialisationDate;
                        ta.InitialisationDateRead = ini.DateTime;

                        lcav.DateTimeRead = stdParser.SaleDate();
                        ps.Product(0).StartOfValidityRead = lcav.DateTimeRead;

                        m.DesignTypeRead = stdParser.DesignType();

                        cu.LanguageRead = stdParser.Language();

                        ta.OwnerRead = stdParser.Owner();
                        ini.ServiceProviderRead = ta.OwnerRead;
                        //                        m.OwnerRead = ini.ServiceProvider;
                        //                        lcav.ServiceProviderRead = m.Owner;

                        lcav.FareTiersRead = stdParser.FareTier();
                        lcav.LocationRead = stdParser.Location();
                    }

                    IVTDParser vtdParser1, vtdParser2, vtdParser;
                    if (version == 1)
                    {
                        vtdParser1 = new VTDParser_Ver1(1, pResData);
                        vtdParser2 = new VTDParser_Ver1(2, pResData);
                    }
                    else
                    {
                        vtdParser1 = new VTDParser_Ver0(1, pResData);
                        vtdParser2 = new VTDParser_Ver0(2, pResData);
                    }

                    //Data Block Layout                    
                    {
                        int seqNumVTD1 = vtdParser1.SeqNum();// (int)TokenFunctions.ExtractSeqNumVTD1(pResData);
                        int seqNumVTD2 = vtdParser2.SeqNum(); //(int)TokenFunctions.ExtractSeqNumVTD2(pResData);                        

                        if (seqNumVTD1 >= seqNumVTD2)
                        {
                            vtdParser = vtdParser1;
                            ta.SequenceNumberRead = m.SequenceNumberRead = seqNumVTD1;
                        }
                        else
                        {
                            vtdParser = vtdParser2;
                            ta.SequenceNumberRead = m.SequenceNumberRead = seqNumVTD2;
                        }

                        ta.StatusRead = vtdParser.Status();

                        ps.Product(0).TypeRead = vtdParser.LogicalTokenType();

                        //lcav.EquipmentTypeRead =   // Not sure on this assignment
                        lcav.EquipmentNumberRead = vtdParser.SaleEquipmentNumber();

                        val.LocationRead = vtdParser.EntryExitStationCode();

                        val.LastTransactionDateTimeRead = vtdParser.LastTransactionDateTime();

                        // I think not useful for sjt/free exit/paid exit. Useful only for RJT.
                        lcav.DestinationRead = vtdParser.Destination();

                        val.RejectCodeRead = vtdParser.RejectCode();

                        // Entry-exit bit
                        val.EntryExitBitRead = vtdParser.EntryExitBit();

                        ta.TestRead = m.TestRead = vtdParser.Test();

                        lcav.AmountRead = vtdParser.Amount();
                    }

                    logMedia.DelhiUltralightRaw.Hidden = !bPopulateRawData;
                    if (bPopulateRawData)
                    {
                        {
                            // VTD1
                            var parser = vtdParser1;
                            var raw = logMedia.DelhiUltralightRaw;

                            raw.LogicalTokenType1 = (byte)parser.LogicalTokenType();
                            raw.TokenStatus1 = (byte)parser.Status();
                            raw.SequenceNumber1 = (int)parser.SeqNum();
                            raw.SaleEquipmentID1 = (int)parser.SaleEquipmentNumber();
                            raw.EntryExitStationCode1 = (short)parser.EntryExitStationCode();
                            raw.DestinationStationCode1 = (short)parser.Destination();

                            raw.TransactionTS1 = parser.LastTransactionDateTime();

                            raw.JourneyManagement1 = parser.JourneyManagement();
                            raw.RejectCode1 = (byte)parser.RejectCode();
                            raw.EntryExitBit1 = parser.EntryExitBitRaw();
                            raw.TestFlag1 = Convert.ToByte(parser.Test());
                            raw.TokenAmount1 = parser.Amount();
                        }
                        {
                            // VTD2
                            var parser = vtdParser2;
                            var raw = logMedia.DelhiUltralightRaw;

                            raw.LogicalTokenType2 = (byte)parser.LogicalTokenType();
                            raw.TokenStatus2 = (byte)parser.Status();
                            raw.SequenceNumber2 = (int)parser.SeqNum();
                            raw.SaleEquipmentID2 = (int)parser.SaleEquipmentNumber();
                            raw.EntryExitStationCode2 = (short)parser.EntryExitStationCode();
                            raw.DestinationStationCode2 = (short)parser.Destination();

                            raw.TransactionTS2 = parser.LastTransactionDateTime();

                            raw.JourneyManagement2 = parser.JourneyManagement();
                            raw.RejectCode2 = (byte)parser.RejectCode();
                            raw.EntryExitBit2 = parser.EntryExitBitRaw();
                            raw.TestFlag2 = Convert.ToByte(parser.Test());
                            raw.TokenAmount2 = parser.Amount();
                        }
                        {
                            var raw = logMedia.DelhiUltralightRaw;
                            int OFFSET = 1 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;

                            raw.IssueDate = stdParser.Initialisationdate();
                            raw.DateOfSale = stdParser.SaleDate();
                            raw.PhysicalTokenType = (byte)stdParser.DesignType();
                            raw.LanguageBit = (byte)stdParser.Language();
                            raw.ServiceProviderID = (byte)stdParser.Owner();
                            raw.FareTier = (byte)stdParser.FareTier();
                            raw.KeyVersion = (byte)CFunctions.GetBitData(OFFSET + 51, 1, pResData);
                            raw.SaleStationCode = (short)stdParser.Location();
                            raw.MAC = (Int64)CFunctions.GetBitData(OFFSET + 64, 64, pResData);
                        }
                        return true;
                    }
                }
                else
                {
                    Logging.Log(LogLevel.Error, "DelhiTokenUltralight: Read Media Data Error");
                    return false;
                }
            }

            return true;
        }
#if !_HHD_
        public bool _ReadTTag(LogicalMedia logMedia,
            byte[] pResData // it has removed the 1st byte that doesn't belong to token data
            )
        {
            var ct = logMedia.TTag;

            {
                int OFFSET = 1 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;
                ct.IssueDate = CFunctions.ConvertDosDate(OFFSET + 0, pResData);

                {
                    int size = 8;
                    int type = (int)CFunctions.GetBitData(OFFSET + 32, size, pResData);
                    if (type != CONSTANT.TICKET_TYPE_TTAG)
                        return false;
                }

                ct.ChipSerialNumber = (long)CFunctions.GetBitData(0, 56, pResData);
                OFFSET = 2 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;
                {
                    int size = 2 * 8;
                    ct.CountTokens = (short)CFunctions.GetBitData(OFFSET, size, pResData);
                    OFFSET += size;
                }
                {
                    int size = 4 * 8;
                    ct.EquipmentNumber = (int)CFunctions.GetBitData(OFFSET, size, pResData);
                    OFFSET += size;
                }
                {
                    DateTime dt = CFunctions.ConvertDosDate(OFFSET, pResData);
                    OFFSET += 2 * 8;

                    DateTime tim = CFunctions.ConvertDosTime(OFFSET, pResData);
                    OFFSET += 2 * 8;

                    ct.TimeLastWritten = CFunctions.MergeDateTime(dt, tim);
                }
                {
                    int size = 4 * 8;
                    ct.SerialNumber = (int)CFunctions.GetBitData(OFFSET, size, pResData);
                    OFFSET += size;
                }
                {
                    ct.LastOperation = (TTagOps)((byte)CFunctions.GetBitData(OFFSET, 4, pResData));
                    OFFSET += 4;
                }

                return true;
            }
        }

        public void SetRWHandle(int p)
        {
            _hRw = p;
        }

        public override Status GetLastStatus()
        {
            if (Err == CSC_API_ERROR.ERR_NONE && pSw1 == 0x90 && pSw2 == 0)
                return Status.Success;
            else if (Err == CSC_API_ERROR.ERR_TIMEOUT
                                || (pSw1 == 0x90 && pSw2 == 0xE0) //no response from card. To verify that this additional condition can be taken into consideration
                )
            {
                Logging.Log(LogLevel.Verbose, String.Format("CSC_API_ERROR.ERR_TIMEOUT psw1 = {0} psw2 = {1}", pSw1, pSw2));
                return Status.Failed_MediaWasNotInField;
            }
            else
            {
                Logging.Log(LogLevel.Verbose, String.Format("GetLastStatus() unexpected Err = {0} psw1 = {1} psw2 = {2}", Err, pSw1, pSw2));
                return Status.FailedNotCategorized;
            }
        }
#endif

        public CSC_API_ERROR WriteToToken(byte[] pCmdBuffer, out bool bSuccessTokenGlo)
        {
            Err = TokenFunctions.WriteBlocks(CSC_READER_TYPE.V4_READER,
                                    _hRw,
                                    pCmdBuffer,
                                    out pSw1,
                                    out pSw2,
                                    out bSuccessTokenGlo);
            return Err;
        }
    }
}
