using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReaderAdaptor;

namespace IFS2.TicketingRules.Common
{
    public class CCHSSAM
    {

        private CCHSSAMManger mCCHSSAMMgr=null;
        private CSC_READER_TYPE _ReaderType;
        private int _hRw;
        private DEST_TYPE _samSlot;
        private ReaderComm _ReaderComm;
        private FirmwareInfo _FirmwareInfo;

        public CCHSSAM()
        {
            Reader.Start(false,true);   
        }

        public CSC_API_ERROR ReaderInitialise(int readerType, string comPort)
        {
            _ReaderType = (CSC_READER_TYPE)readerType;
            _ReaderComm.COM_PORT = comPort;
            _ReaderComm.COM_SPEED = 115200;
            CSC_API_ERROR Err = Reader.ReloadReader((CSC_READER_TYPE)_ReaderType, _ReaderComm, out _hRw, out _FirmwareInfo);
            return Err;
        }
        public CSC_API_ERROR SAMInitialise( int samSlot,bool production,string samPinCode)
        {
            _samSlot = (DEST_TYPE)samSlot;

            mCCHSSAMMgr = new CCHSSAMManger(_ReaderType, _hRw, production, samPinCode);
            CSC_API_ERROR Err = ResetCCHSSAM();
            return Err;
        }  

        private CSC_API_ERROR ResetCCHSSAM()
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOT_AVAIL;
            if (mCCHSSAMMgr == null) return CSC_API_ERROR.ERR_DEVICE;

            Err = mCCHSSAMMgr.ResetCCHSSAM(_samSlot,false);
            if (Err == CSC_API_ERROR.ERR_NONE)
            {
            }
            return Err;

        }

        public CSC_API_ERROR GetDSMInfo(out cCCHSDSMInfo info)
        {
            CSC_API_ERROR Err = mCCHSSAMMgr.GetDSMInfo(_samSlot, out info);
            return Err;
        }

        public CSC_API_ERROR GetDSMId(out uint dsmId)
        {
            CSC_API_ERROR Err = mCCHSSAMMgr.GetDSMID(_samSlot, out dsmId);
            return Err;
        }

        public CSC_API_ERROR GetDSMSequence(out int sequence)
        {
            CSC_API_ERROR Err = mCCHSSAMMgr.GetSAMSequence(_samSlot, out sequence);
            return Err;
        }

        public CSC_API_ERROR GetDSMStatus(out cCCHSSAMInfo info)
        {
            CSC_API_ERROR Err = mCCHSSAMMgr.GetSAMStatus(_samSlot, out info);
            return Err;
        }


        public uint GetDsmId()
        {
            return mCCHSSAMMgr.DSMId;
            //serialNumber = 0;
            //if (mCCHSSAMMgr == null) return CSC_API_ERROR.ERR_DEVICE;
            //return mCCHSSAMMgr.GetDSMID(_samSlot, out serialNumber);
        }

    }
}
