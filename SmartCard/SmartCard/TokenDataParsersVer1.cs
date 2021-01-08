using System;
using IFS2.Equipment.TicketingRules.CommonFunctions;
namespace IFS2.Equipment.TicketingRules
{
    class SaleTokenParser_Ver1 : ISTDParser
    {
        byte[] pResData;
        const int OFFSET = 1 * CONSTANT.MIFARE_ULTRALT_BLOC_BITS;

        public SaleTokenParser_Ver1(byte[] pResData_)
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
            short lsb = (short)CFunctions.GetBitData(OFFSET + 52, 8, pResData);
            short msb = (short)CFunctions.GetBitData(OFFSET + 62, 2, pResData);

            return 256 * msb + lsb;
        }

        #endregion
    }

    class VTDParser_Ver1 : IVTDParser
    {
        readonly int OFFSET;
        readonly byte[] pResData;
        public VTDParser_Ver1(int vtdBlock, byte[] pResData_)
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
            return (int)CFunctions.GetBitData(OFFSET + 8, 18, pResData);
        }

        public TransportApplication.StatusValues Status()
        {
            return (TransportApplication.StatusValues)(short)CFunctions.GetBitData(OFFSET + 4, 4, pResData);
        }

        public short LogicalTokenType()
        {
            return (short)CFunctions.GetBitData(OFFSET + 0, 4, pResData);
        }

        public int SaleEquipmentNumber()
        {
            return (int)CFunctions.GetBitData(OFFSET + 26, 24, pResData);
        }

        public int EntryExitStationCode()
        {
            return (int)CFunctions.GetBitData(OFFSET + 50, 10, pResData);
        }

        public DateTime LastTransactionDateTime()
        {
            DateTime dt = CFunctions.ConvertDosDate(OFFSET + 86, pResData);
            DateTime tim = CFunctions.ConvertDosTime(OFFSET + 60, pResData);
            return CFunctions.MergeDateTime(dt, tim).ToLocalTime();
        }

        public int Destination()
        {
            return (int)CFunctions.GetBitData(OFFSET + 76, 10, pResData);
        }

        public short RejectCode()
        {
            return (short)CFunctions.GetBitData(OFFSET + 104, 8, pResData);
        }

        public Validation.TypeValues EntryExitBit()
        {
            return ((short)CFunctions.GetBitData(OFFSET + 112, 1, pResData) == CONSTANT.MBC_GateEntry ? Validation.TypeValues.Entry : Validation.TypeValues.Exit);
        }

        public bool Test()
        {
            return Convert.ToBoolean((short)CFunctions.GetBitData(OFFSET + 113, 1, pResData));
        }

        public int Amount()
        {
            return 10 * (int)CFunctions.GetBitData(OFFSET + 114, 12, pResData);
        }

        public byte JourneyManagement()
        {
            return (byte)CFunctions.GetBitData(OFFSET + 102, 2, pResData);
        }

        #endregion
    }
}
/*
4	0
4	4
18	8
24	26
10	50
16	60
10	76
16	86
2	102
8	104
1	112
1	113
12	114
2	126
*/