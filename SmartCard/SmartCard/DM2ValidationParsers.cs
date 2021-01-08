using IFS2.Equipment.TicketingRules.CommonFunctions;
using System;
namespace IFS2.Equipment.TicketingRules
{
    class DM2ValidationParser_Ver0 : IDM2ValidationParser
    {
        byte[] pResData;
        public DM2ValidationParser_Ver0(byte[] pResData_)
        {
            pResData = pResData_;
        }
        #region IDM2ValidationParser Members

        public DateTime DateOfFirstTransaction()
        {
            return CFunctions.ConvertDosDate(0, pResData);
        }

        public Validation.TypeValues EntryExitBit()
        {
            int i = (short)CFunctions.GetBitData(16, 8, pResData);
            switch (i)
            {
                case CONSTANT.MBC_GateEntry:
                    return Validation.TypeValues.Entry;
                case CONSTANT.MBC_GateExit:
                    return Validation.TypeValues.Exit;                    
                default:
                    return Validation.TypeValues.Unknown;                    
            }
        }

        public int Location()
        {
            return (int)CFunctions.GetBitData(24, 8, pResData);
        }

        public System.DateTime LastTransactionDateTime()
        {
            return CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(32, 32, pResData));
        }

        public short RejectCode()
        {
            return (short)CFunctions.GetBitData(64, 8, pResData);
        }

        public TransportApplication.StatusValues Status()
        {
            short i = (short)CFunctions.GetBitData(72, 8, pResData);
            switch (i)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    return (TransportApplication.StatusValues)i;                    
                default:
                    return TransportApplication.StatusValues.Unknown;                    
            }
        }

        public bool Test()
        {
            return Convert.ToBoolean((short)CFunctions.GetBitData(80, 1, pResData));
        }

        public int BonusValue()
        {
            return (int)CFunctions.GetBitData(81, 16, pResData);
        }

        public short AgentRemainingTrips()
        {
            return (short)CFunctions.GetBitData(249, 7, pResData);
        }

        #endregion
    }

    class DM2ValidationParser_Ver1 : IDM2ValidationParser
    {
        byte[] pResData;
        public DM2ValidationParser_Ver1(byte[] pResData_)
        {
            pResData = pResData_;
        }

        #region IDM2ValidationParser Members

        public DateTime DateOfFirstTransaction()
        {
            return CFunctions.ConvertDosDate(0, pResData);
        }

        public Validation.TypeValues EntryExitBit()
        {
            int i = (short)CFunctions.GetBitData(23, 1, pResData);
            switch (i)
            {
                case CONSTANT.MBC_GateEntry:
                    return Validation.TypeValues.Entry;
                case CONSTANT.MBC_GateExit:
                    return Validation.TypeValues.Exit;
                default:
                    return Validation.TypeValues.Unknown;
            }
        }

        public int Location()
        {
            int lsb = (int)CFunctions.GetBitData(24, 8, pResData);
            int msb = (int)CFunctions.GetBitData(19, 2, pResData);

            return 256 * msb + lsb;
        }

        public System.DateTime LastTransactionDateTime()
        {
            return CFunctions.UnixTimeStampToDateTime((int)CFunctions.GetBitData(32, 32, pResData));
        }

        public short RejectCode()
        {
            return (short)CFunctions.GetBitData(64, 8, pResData);
        }

        public TransportApplication.StatusValues Status()
        {
            short i = (short)CFunctions.GetBitData(72, 8, pResData);
            switch (i)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    return (TransportApplication.StatusValues)i;
                default:
                    return TransportApplication.StatusValues.Unknown;
            }
        }

        public bool Test()
        {
            return Convert.ToBoolean((short)CFunctions.GetBitData(80, 1, pResData));
        }

        public int BonusValue()
        {
            return (int)CFunctions.GetBitData(81, 16, pResData);
        }

        public short AgentRemainingTrips()
        {
            return (short)CFunctions.GetBitData(249, 7, pResData);
        }

        #endregion
    }
}
