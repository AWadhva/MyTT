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
        public DelhiTokenUltralight() { }

        int _hRw = 0; // Giving it a valid value, because don't want to disturb HHD code
        public DelhiTokenUltralight(int hRw) { _hRw = hRw; }

        private Boolean MakeRecovery()
        {
            return true;
        }

        private const int NUMBITSINONEBLOCK = 16 * 8;

        protected override Boolean _ReadMediaData(LogicalMedia logMedia, MediaDetectionTreatment readTreatment)
        {
            byte[] pResData;
            if (SharedData.EquipmentType == EquipmentFamily.TOM)
                return ReadMediaData3(logMedia, readTreatment);
            else
                return ReadMediaData2(logMedia, out pResData, readTreatment);
        }

        bool ReadMediaData3(LogicalMedia logMedia, MediaDetectionTreatment readTreatment)
        {
            if (!_bTokenRead)
            {
                Err = CSC_API_ERROR.ERR_NONE;
                Media m = logMedia.Media;

                Initialisation ini = logMedia.Initialisation;
                TransportApplication ta = logMedia.Application.TransportApplication;
                LocalLastAddValue lcav = logMedia.Application.LocalLastAddValue;
                Customer cu = logMedia.Application.Customer;
                Validation val = logMedia.Application.Validation;

                Products ps = logMedia.Application.Products;
                OneProduct p = new OneProduct();
                ps.Add(p);

                var pResData = new byte[CONSTANT.MAX_ISO_DATA_OUT_LENGTH];
                pSw1 = 0xFF;
                pSw2 = 0xFF;

                //Manufacturer Block is read By Default, so Number of Blocks is excluding
                //block 0
                Err = TokenFunctions.ReadBlocks(CSC_READER_TYPE.V4_READER,
                    _hRw,
                    3, out pSw1, out pSw2, out pResData);
                if (Err != CSC_API_ERROR.ERR_NONE)
                    Logging.Log(LogLevel.Verbose, "DelhiTokenUltralight::ReadMediaData3 Err = " + Err.ToString());
                if (Err == CONSTANT.NO_ERROR && pSw1 != CONSTANT.COMMAND_SUCCESS)
                    Logging.Log(LogLevel.Verbose, "DelhiTokenUltralight::ReadMediaData3 pSw1 = " + pSw1.ToString());

                if (Err == CONSTANT.NO_ERROR && pSw1 == CONSTANT.COMMAND_SUCCESS)
                {
                    _bTokenRead = true;
                    logMedia._tokenPhysicalData = new byte[64];
                    Array.Copy(pResData, 1, logMedia._tokenPhysicalData, 0, 64);
                    // NOTE THAT pResData has 8-bits in the beginning, that DON'T BELONG TO TOKEN DATA
                    pResData = logMedia._tokenPhysicalData;

                    // Sale Block Layout
                    m.ChipSerialNumberRead = SmartFunctions.Instance.ReadSNbr();//(long)CFunctions.GetBitData(0, 56, pResData);
                    m.HardwareTypeRead = Media.HardwareTypeValues.TokenUltralight;
                    m.ChipTypeRead = Media.ChipTypeValues.UltralightC;
                    m.TypeRead = Media.TypeValues.Token;

                    short typ = (short)CFunctions.GetBitData(1 * NUMBITSINONEBLOCK + 32, 8, pResData);
#if !_HHD_
                    if (typ == CONSTANT.TICKET_TYPE_TTAG)
                    {
                        _ReadTTag(logMedia, pResData);
                        logMedia.TTag.Hidden = false;
                        return true;
                    }
#endif
                    {
#if !_HHD_
                        logMedia.TTag.Hidden = true;
#endif
                        int OFFSET = 1 * NUMBITSINONEBLOCK;
                        m.InitialisationDateRead = CFunctions.ConvertDosDate(OFFSET + 0, pResData);
                        ini.DateTimeRead = m.InitialisationDate;
                        ta.InitialisationDateRead = ini.DateTime;

                        lcav.DateTimeRead = CFunctions.ConvertDosDate(OFFSET + 16, pResData);
                        ps.Product(0).StartOfValidityRead = lcav.DateTimeRead;

                        m.DesignTypeRead = (short)CFunctions.GetBitData(OFFSET + 32, 8, pResData);

                        cu.LanguageRead = (Customer.LanguageValues)(short)CFunctions.GetBitData(OFFSET + 40, 1, pResData);

                        ta.OwnerRead = (short)CFunctions.GetBitData(OFFSET + 41, 4, pResData);
                        ini.ServiceProviderRead = ta.OwnerRead;
                        //                        m.OwnerRead = ini.ServiceProvider;
                        //                        lcav.ServiceProviderRead = m.Owner;

                        lcav.FareTiersRead = (short)CFunctions.GetBitData(OFFSET + 45, 6, pResData);
                        lcav.LocationRead = (short)CFunctions.GetBitData(OFFSET + 52, 8, pResData);
                    }
                    //Data Block Layout
                    //ps.Product(0).TypeRead = -- TODO, clerification required
                    {
                        int OFFSET;

                        int seqNumVTD1 = (int)CFunctions.GetBitData(2 * NUMBITSINONEBLOCK + 16, 18, pResData);
                        int seqNumVTD2 = (int)CFunctions.GetBitData(3 * NUMBITSINONEBLOCK + 16, 18, pResData);

                        // TODO: As per documentation: If only one of the two blocks is readable, its data are used for the current processing. 
                        // But, how to determine, that "Data is Readable"??? Perhaps a check for all-zeroes for that block would work                        
                        if (seqNumVTD1 >= seqNumVTD2)
                        {
                            OFFSET = 2 * NUMBITSINONEBLOCK;
                            ta.SequenceNumberRead = m.SequenceNumberRead = seqNumVTD1;
                        }
                        else
                        {
                            OFFSET = 3 * NUMBITSINONEBLOCK;
                            ta.SequenceNumberRead = m.SequenceNumberRead = seqNumVTD2;
                        }

                        ta.StatusRead = (TransportApplication.StatusValues)(short)CFunctions.GetBitData(OFFSET + 8, 8, pResData);

                        ps.Product(0).TypeRead = (short)CFunctions.GetBitData(OFFSET + 0, 8, pResData);

                        //lcav.EquipmentTypeRead =   // Not sure on this assignment
                        lcav.EquipmentNumberRead = (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);

                        val.LocationRead = (int)CFunctions.GetBitData(OFFSET + 58, 8, pResData);

                        DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
                        DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
                        val.LastTransactionDateTimeRead = CFunctions.MergeDateTime(dt, tim).ToLocalTime();

                        // I think not useful for sjt/free exit/paid exit. Useful only for RJT.
                        lcav.DestinationRead = (int)CFunctions.GetBitData(OFFSET + 82, 8, pResData);

                        val.RejectCodeRead = (short)CFunctions.GetBitData(OFFSET + 108, 8, pResData);

                        // Entry-exit bit
                        short temp = (short)CFunctions.GetBitData(OFFSET + 116, 1, pResData);
                        if (temp == CONSTANT.MBC_GateEntry)
                            val.EntryExitBitRead = Validation.TypeValues.Entry;
                        else if (temp == CONSTANT.MBC_GateExit)
                            val.EntryExitBitRead = Validation.TypeValues.Exit;

                        ta.TestRead = m.TestRead = Convert.ToBoolean((short)CFunctions.GetBitData(OFFSET + 117, 1, pResData));

                        //                        ta.TestRead = m.Test;                        
                        lcav.AmountRead = 10 * (int)CFunctions.GetBitData(OFFSET + 118, 10, pResData);
                    }
                    #region RawData
                    {
                        // VTD1
                        var raw = logMedia.DelhiUltralightRaw;
                        int OFFSET = 2 * NUMBITSINONEBLOCK;

                        raw.LogicalTokenType1 = (byte)CFunctions.GetBitData(OFFSET, 8, pResData);
                        raw.TokenStatus1 = (byte)CFunctions.GetBitData(OFFSET + 8, 8, pResData);
                        raw.SequenceNumber1 = (int)CFunctions.GetBitData(2 * NUMBITSINONEBLOCK + 16 + 8, 18, pResData);
                        raw.SaleEquipmentID1 = (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);
                        raw.EntryExitStationCode1 = (byte)CFunctions.GetBitData(OFFSET + 58, 8, pResData);
                        raw.DestinationStationCode1 = (byte)CFunctions.GetBitData(OFFSET + 82, 8, pResData);

                        DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
                        DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
                        raw.TransactionTS1 = CFunctions.MergeDateTime(dt, tim).ToLocalTime();

                        raw.JourneyManagement1 = (byte)CFunctions.GetBitData(OFFSET + 106, 2, pResData);
                        raw.RejectCode1 = (byte)CFunctions.GetBitData(OFFSET + 108, 8, pResData);
                        raw.EntryExitBit1 = (byte)CFunctions.GetBitData(OFFSET + 116, 1, pResData);
                        raw.TestFlag1 = (byte)CFunctions.GetBitData(OFFSET + 117, 1, pResData);
                        raw.TokenAmount1 = (short)(10 * (Int16)CFunctions.GetBitData(OFFSET + 118, 10, pResData));
                    }
                    {
                        // VTD2
                        var raw = logMedia.DelhiUltralightRaw;
                        int OFFSET = 3 * NUMBITSINONEBLOCK;

                        raw.LogicalTokenType2 = (byte)CFunctions.GetBitData(OFFSET, 8, pResData);
                        raw.TokenStatus2 = (byte)CFunctions.GetBitData(OFFSET + 8, 8, pResData);
                        raw.SequenceNumber2 = (int)CFunctions.GetBitData(3 * NUMBITSINONEBLOCK + 16 + 8, 18, pResData);
                        raw.SaleEquipmentID2 = (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);
                        raw.EntryExitStationCode2 = (byte)CFunctions.GetBitData(OFFSET + 58, 8, pResData);
                        raw.DestinationStationCode2 = (byte)CFunctions.GetBitData(OFFSET + 82, 8, pResData);

                        DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
                        DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
                        raw.TransactionTS2 = CFunctions.MergeDateTime(dt, tim).ToLocalTime();

                        raw.JourneyManagement2 = (byte)CFunctions.GetBitData(OFFSET + 106, 2, pResData);
                        raw.RejectCode2 = (byte)CFunctions.GetBitData(OFFSET + 108, 8, pResData);
                        raw.EntryExitBit2 = (byte)CFunctions.GetBitData(OFFSET + 116, 1, pResData);
                        raw.TestFlag2 = (byte)CFunctions.GetBitData(OFFSET + 117, 1, pResData);
                        raw.TokenAmount2 = (short)(10 * (Int16)CFunctions.GetBitData(OFFSET + 118, 10, pResData));
                    }
                    {
                        var raw = logMedia.DelhiUltralightRaw;
                        int OFFSET = 1 * NUMBITSINONEBLOCK;

                        raw.IssueDate = CFunctions.ConvertDosDate(OFFSET + 0, pResData);
                        raw.DateOfSale = CFunctions.ConvertDosDate(OFFSET + 16, pResData);
                        raw.PhysicalTokenType = (byte)CFunctions.GetBitData(OFFSET + 32, 8, pResData);
                        raw.LanguageBit = (byte)CFunctions.GetBitData(OFFSET + 40, 1, pResData);
                        raw.ServiceProviderID = (byte)CFunctions.GetBitData(OFFSET + 41, 4, pResData);
                        raw.FareTier = (byte)CFunctions.GetBitData(OFFSET + 45, 6, pResData);
                        raw.KeyVersion = (byte)CFunctions.GetBitData(OFFSET + 51, 1, pResData);
                        raw.SaleStationCode = (byte)CFunctions.GetBitData(OFFSET + 52, 8, pResData);
                        raw.MAC = (Int64)CFunctions.GetBitData(OFFSET + 64, 64, pResData);
                    }
                    #endregion
                }
                else
                {
                    Logging.Log(LogLevel.Error, "DelhiTokenUltralight: Read Media Data Error");
                    return false;
                }                
            }
            bool bPopulateRawData = !(//readTreatment == MediaDetectionTreatment.BasicAnalysis_TOM || 
                readTreatment == MediaDetectionTreatment.BasicAnalysis_AVM_TVM);
            logMedia.DelhiUltralightRaw.Hidden = !bPopulateRawData;

            return true;
        }

        bool ReadMediaData2(LogicalMedia logMedia, out byte[] pResData, MediaDetectionTreatment readTreatment)
        {            
            return ReadMediaData2(logMedia, out pResData, out Err, readTreatment);
        }

        bool _bTokenRead = false;

        protected override void _Reset()
        {
            _bTokenRead = false;
        }

        public bool ReadMediaData2(LogicalMedia logMedia, out byte[] pResData, out CSC_API_ERROR Err, MediaDetectionTreatment readTreatment)
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
                    m.ChipSerialNumberRead = SmartFunctions.Instance.ReadSNbr();//(long)CFunctions.GetBitData(0, 56, pResData);
                    m.HardwareTypeRead = Media.HardwareTypeValues.TokenUltralight;
                    m.ChipTypeRead = Media.ChipTypeValues.UltralightC;
                    m.TypeRead = Media.TypeValues.Token;

                    short typ = (short)CFunctions.GetBitData(1 * NUMBITSINONEBLOCK + 32, 8, pResData);
#if !_HHD_
                    if (typ == CONSTANT.TICKET_TYPE_TTAG)
                    {
                        _ReadTTag(logMedia, pResData);
                        logMedia.TTag.Hidden = false;
                        return true;
                    }
#endif
                    {
#if !_HHD_
                        logMedia.TTag.Hidden = true;
#endif
                        int OFFSET = 1 * NUMBITSINONEBLOCK;
                        m.InitialisationDateRead = CFunctions.ConvertDosDate(OFFSET + 0, pResData);
                        ini.DateTimeRead = m.InitialisationDate;
                        ta.InitialisationDateRead = ini.DateTime;

                        lcav.DateTimeRead = CFunctions.ConvertDosDate(OFFSET + 16, pResData);
                        ps.Product(0).StartOfValidityRead = lcav.DateTimeRead;

                        m.DesignTypeRead = (short)CFunctions.GetBitData(OFFSET + 32, 8, pResData);

                        cu.LanguageRead = (Customer.LanguageValues)(short)CFunctions.GetBitData(OFFSET + 40, 1, pResData);

                        ta.OwnerRead = (short)CFunctions.GetBitData(OFFSET + 41, 4, pResData);
                        ini.ServiceProviderRead = ta.OwnerRead;
                        //                        m.OwnerRead = ini.ServiceProvider;
                        //                        lcav.ServiceProviderRead = m.Owner;

                        lcav.FareTiersRead = (short)CFunctions.GetBitData(OFFSET + 45, 6, pResData);
                        lcav.LocationRead =  (short)CFunctions.GetBitData(OFFSET + 52, 8, pResData);
                    }
                    int version = TokenFunctions.ExtractVersion(pResData);
                    //Data Block Layout
                    //ps.Product(0).TypeRead = -- TODO, clerification required
                    {
                        int OFFSET;

                        int seqNumVTD1 = (int)TokenFunctions.ExtractSeqNumVTD1(pResData);
                        int seqNumVTD2 = (int)TokenFunctions.ExtractSeqNumVTD2(pResData);

                        byte[] vtd = new byte[16];
                        
                        if (seqNumVTD1 >= seqNumVTD2)
                        {                            
                            OFFSET = 2 * NUMBITSINONEBLOCK;
                            ta.SequenceNumberRead = m.SequenceNumberRead = seqNumVTD1;
                        }
                        else
                        {
                            OFFSET = 3 * NUMBITSINONEBLOCK;
                            ta.SequenceNumberRead = m.SequenceNumberRead = seqNumVTD2;
                        }

                        ta.StatusRead = (TransportApplication.StatusValues)(short)CFunctions.GetBitData(OFFSET + 8, 8, pResData);

                        ps.Product(0).TypeRead = (short)CFunctions.GetBitData(OFFSET + 0, 8, pResData);

                        //lcav.EquipmentTypeRead =   // Not sure on this assignment
                        lcav.EquipmentNumberRead = (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);

                        val.LocationRead = (int)CFunctions.GetBitData(OFFSET + 58, 8, pResData);

                        DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
                        DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
                        val.LastTransactionDateTimeRead = CFunctions.MergeDateTime(dt, tim).ToLocalTime();

                        // I think not useful for sjt/free exit/paid exit. Useful only for RJT.
                        lcav.DestinationRead = (int)CFunctions.GetBitData(OFFSET + 82, 8, pResData);

                        val.RejectCodeRead = (short)CFunctions.GetBitData(OFFSET + 108, 8, pResData);

                        // Entry-exit bit
                        val.EntryExitBitRead = ((short)CFunctions.GetBitData(OFFSET + 116, 1, pResData) == CONSTANT.MBC_GateEntry ? Validation.TypeValues.Entry : Validation.TypeValues.Exit);

                        ta.TestRead = m.TestRead = Convert.ToBoolean((short)CFunctions.GetBitData(OFFSET + 117, 1, pResData));

                        //                        ta.TestRead = m.Test;                        
                        lcav.AmountRead = 10 * (int)CFunctions.GetBitData(OFFSET + 118, 10, pResData);
                    }

                    bool bPopulateRawData = !(//readTreatment == MediaDetectionTreatment.BasicAnalysis_TOM || 
                        readTreatment == MediaDetectionTreatment.BasicAnalysis_AVM_TVM);
                    logMedia.DelhiUltralightRaw.Hidden = !bPopulateRawData;
                    if (bPopulateRawData)
                    {
                        {
                            // VTD1
                            var raw = logMedia.DelhiUltralightRaw;
                            int OFFSET = 2 * NUMBITSINONEBLOCK;

                            raw.LogicalTokenType1 = (byte)CFunctions.GetBitData(OFFSET, 8, pResData);
                            raw.TokenStatus1 = (byte)CFunctions.GetBitData(OFFSET + 8, 8, pResData);
                            raw.SequenceNumber1 = (int)CFunctions.GetBitData(2 * NUMBITSINONEBLOCK + 16 + 8, 18, pResData);
                            raw.SaleEquipmentID1 = (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);
                            raw.EntryExitStationCode1 = (byte)CFunctions.GetBitData(OFFSET + 58, 8, pResData);
                            raw.DestinationStationCode1 = (byte)CFunctions.GetBitData(OFFSET + 82, 8, pResData);

                            DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
                            DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
                            raw.TransactionTS1 = CFunctions.MergeDateTime(dt, tim).ToLocalTime();

                            raw.JourneyManagement1 = (byte)CFunctions.GetBitData(OFFSET + 106, 2, pResData);
                            raw.RejectCode1 = (byte)CFunctions.GetBitData(OFFSET + 108, 8, pResData);
                            raw.EntryExitBit1 = (byte)CFunctions.GetBitData(OFFSET + 116, 1, pResData);
                            raw.TestFlag1 = (byte)CFunctions.GetBitData(OFFSET + 117, 1, pResData);
                            raw.TokenAmount1 = (short)(10 * (Int16)CFunctions.GetBitData(OFFSET + 118, 10, pResData));
                        }
                        {
                            // VTD2
                            var raw = logMedia.DelhiUltralightRaw;
                            int OFFSET = 3 * NUMBITSINONEBLOCK;

                            raw.LogicalTokenType2 = (byte)CFunctions.GetBitData(OFFSET, 8, pResData);
                            raw.TokenStatus2 = (byte)CFunctions.GetBitData(OFFSET + 8, 8, pResData);
                            raw.SequenceNumber2 = (int)CFunctions.GetBitData(3 * NUMBITSINONEBLOCK + 16 + 8, 18, pResData);
                            raw.SaleEquipmentID2 = (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);
                            raw.EntryExitStationCode2 = (byte)CFunctions.GetBitData(OFFSET + 58, 8, pResData);
                            raw.DestinationStationCode2 = (byte)CFunctions.GetBitData(OFFSET + 82, 8, pResData);

                            DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
                            DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
                            raw.TransactionTS2 = CFunctions.MergeDateTime(dt, tim).ToLocalTime();

                            raw.JourneyManagement2 = (byte)CFunctions.GetBitData(OFFSET + 106, 2, pResData);
                            raw.RejectCode2 = (byte)CFunctions.GetBitData(OFFSET + 108, 8, pResData);
                            raw.EntryExitBit2 = (byte)CFunctions.GetBitData(OFFSET + 116, 1, pResData);
                            raw.TestFlag2 = (byte)CFunctions.GetBitData(OFFSET + 117, 1, pResData);
                            raw.TokenAmount2 = (short)(10 * (Int16)CFunctions.GetBitData(OFFSET + 118, 10, pResData));
                        }
                        {
                            var raw = logMedia.DelhiUltralightRaw;
                            int OFFSET = 1 * NUMBITSINONEBLOCK;

                            raw.IssueDate = CFunctions.ConvertDosDate(OFFSET + 0, pResData);
                            raw.DateOfSale = CFunctions.ConvertDosDate(OFFSET + 16, pResData);
                            raw.PhysicalTokenType = (byte)CFunctions.GetBitData(OFFSET + 32, 8, pResData);
                            raw.LanguageBit = (byte)CFunctions.GetBitData(OFFSET + 40, 1, pResData);
                            raw.ServiceProviderID = (byte)CFunctions.GetBitData(OFFSET + 41, 4, pResData);
                            raw.FareTier = (byte)CFunctions.GetBitData(OFFSET + 45, 6, pResData);
                            raw.KeyVersion = (byte)CFunctions.GetBitData(OFFSET + 51, 1, pResData);
                            raw.SaleStationCode = (byte)CFunctions.GetBitData(OFFSET + 52, 8, pResData);
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
                int OFFSET = 1 * NUMBITSINONEBLOCK; 
                ct.IssueDate = CFunctions.ConvertDosDate(OFFSET + 0, pResData);
                
                {
                    int size = 8;
                    int type = (int)CFunctions.GetBitData(OFFSET + 32, size, pResData);
                    if (type != CONSTANT.TICKET_TYPE_TTAG)
                        return false;
                }
                
                ct.ChipSerialNumber = (long)CFunctions.GetBitData(0, 56, pResData);
                OFFSET = 2 * NUMBITSINONEBLOCK;
                {
                    int size = 2 * 8;
                    ct.CountTokens = (short)CFunctions.GetBitData(OFFSET, size, pResData);
                    OFFSET += size;
                }
                {
                    int size = 4*8;
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
                    int size = 4*8;
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
