using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{
    class SaleTokenParser_Ver0 : ISTDParser
    {
        byte[] pResData;

        const int OFFSET = 1 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;
        public SaleTokenParser_Ver0(byte[] pResData_)
        {
            pResData = pResData_;
        }
        #region ISaleTokenDataParser Members

        public DateTime Initialisationdate()
        {
            return CFunctions.ConvertDosDate(OFFSET + 0, pResData);
        }

        public DateTime SaleDate()
        {
            return CFunctions.ConvertDosDate(OFFSET + 16, pResData);
        }

        public short DesignType()
        {
            return (short)CFunctions.GetBitData(OFFSET + 32, 8, pResData);
        }

        public Customer.LanguageValues Language()
        {
            return (Customer.LanguageValues)(short)CFunctions.GetBitData(OFFSET + 40, 1, pResData);
        }

        public short Owner()
        {
            return (short)CFunctions.GetBitData(OFFSET + 41, 4, pResData);
        }

        public short FareTier()
        {
            return (short)CFunctions.GetBitData(OFFSET + 45, 6, pResData);
        }

        public int Location()
        {
            return (short)CFunctions.GetBitData(OFFSET + 52, 8, pResData);
        }

        #endregion
    }

    class VTDParser_Ver0 : IVTDParser
    {
        readonly int OFFSET;
        readonly byte[] pResData;
        public VTDParser_Ver0(int vtdBlock, byte[] pResData_)
        {
            pResData = pResData_;

            if (vtdBlock == 1)
                OFFSET = 2 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;
            else
                OFFSET = 3 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;
        }

        #region IVTDParser Members

        public int SeqNum()
        {
            return (int)CFunctions.GetBitData(OFFSET + 16, 18, pResData);
        }

        public TransportApplication.StatusValues Status()
        {
            return (TransportApplication.StatusValues)(short)CFunctions.GetBitData(OFFSET + 8, 8, pResData);
        }

        public short LogicalTokenType()
        {
            return (short)CFunctions.GetBitData(OFFSET + 0, 8, pResData);
        }

        public int SaleEquipmentNumber()
        {
            return (int)CFunctions.GetBitData(OFFSET + 34, 24, pResData);
        }

        public int EntryExitStationCode()
        {
            return (int)CFunctions.GetBitData(OFFSET + 58, 8, pResData);
        }

        public DateTime LastTransactionDateTime()
        {
            DateTime dt = CFunctions.ConvertDosDate(OFFSET + 90, pResData);
            DateTime tim = CFunctions.ConvertDosTime(OFFSET + 66, pResData);
            return CFunctions.MergeDateTime(dt, tim).ToLocalTime();
        }

        public int Destination()
        {
            return (int)CFunctions.GetBitData(OFFSET + 82, 8, pResData);
        }

        public short RejectCode()
        {
            return (short)CFunctions.GetBitData(OFFSET + 108, 8, pResData);
        }

        public Validation.TypeValues EntryExitBit()
        {
            return ((short)CFunctions.GetBitData(OFFSET + 116, 1, pResData) == CONSTANT.MBC_GateEntry ? Validation.TypeValues.Entry : Validation.TypeValues.Exit);
        }

        public byte EntryExitBitRaw()
        {
            return (byte)CFunctions.GetBitData(OFFSET + 116, 1, pResData);
        }

        public bool Test()
        {
            return Convert.ToBoolean((short)CFunctions.GetBitData(OFFSET + 117, 1, pResData));
        }

        public int Amount()
        {
            // TODO: see if this is the appropriate place to convert to paise
            return 10 * (int)CFunctions.GetBitData(OFFSET + 118, 10, pResData);
        }

        public byte JourneyManagement()
        {
            return (byte)CFunctions.GetBitData(OFFSET + 106, 2, pResData);
        }

        #endregion
    }
}
