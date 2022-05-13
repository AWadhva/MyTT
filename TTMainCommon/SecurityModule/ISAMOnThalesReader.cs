using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules.SecurityModuleInitializer
{
    public class ISAMOnThalesReader : SAM
    {
        public ISAMOnThalesReader(ISAMConf conf_)
        {
            conf = conf_;

            switch (conf.slotId)
            {
                case 1:
                    samSlot = DEST_TYPE.DEST_SAM1;
                    break;
                case 2:
                    samSlot = DEST_TYPE.DEST_SAM2;
                    break;
                case 3:
                    samSlot = DEST_TYPE.DEST_SAM3;
                    break;
                case 4:
                    samSlot = DEST_TYPE.DEST_SAM4;
                    break;
            }
            _ProductionSAM = conf.IsProductionSAM;
            mCCHSSAMMgr = conf.mCCHSSAMMgr;
        }

        ISAMConf conf;

        #region SAM Members

        public object Initialize(out bool bPresent, out bool bWorking)
        {
            var Err = mCCHSSAMMgr.ResetCCHSSAM(samSlot, conf._readDeviceIDInCCHSSAM);
            ISAMData data = new ISAMData();
            bPresent = bWorking = false;
            
            switch (Err)
            {
                case CSC_API_ERROR.ERR_NONE:
                    {
                        bPresent = true;
                        bWorking = true;

                        if (conf._signatureAtEachTransaction)                        
                            data.TxnSeqenceNo = mCCHSSAMMgr.TxnSeqenceNo;
                        
                        data.mDSMId = mCCHSSAMMgr.DSMId;
                        data.CompanyID = mCCHSSAMMgr.mCCHSStatusInfo.ServiceProvider;                        
                        
                        mCCHSSAMMgr.GetTokenKey(samSlot, 
                            0, // for now, let it hard code as 0. later we may have to fetch both old and new keys
                            out data.TokenKey);
                        return data;
                    }
                case CSC_API_ERROR.ERR_DATA:
                case CSC_API_ERROR.ERR_DEVICE:
                    {
                        bPresent = true;
                        bWorking = false;

                        break;
                    }
                case CSC_API_ERROR.ERR_TIMEOUT:
                    {
                        bPresent = false;
                        bWorking = false;
                        
                        break;
                    }
            }
            return null;
        }

        public object GetStatus()
        {
            throw new NotImplementedException();
        }

        public string GetSerialNumber()
        {
            throw new NotImplementedException();
        }

        public bool IsPresent()
        {
            throw new NotImplementedException();
        }

        #endregion

        CCHSSAMManger mCCHSSAMMgr;
        bool _ProductionSAM;
        DEST_TYPE samSlot;
    }

    public class ISAMConf
    {
        public int hRw;
        public int slotId;
        public CSC_READER_TYPE readerType;
        public bool IsProductionSAM;
        public bool _readDeviceIDInCCHSSAM;
        public CCHSSAMManger mCCHSSAMMgr;
		public bool _signatureAtEachTransaction;
    }
	
	public class ISAMData
	{
		public int TxnSeqenceNo;
		public uint mDSMId;
		public int CompanyID;
        public cCCHSSAMTokenKey TokenKey;
	}
}