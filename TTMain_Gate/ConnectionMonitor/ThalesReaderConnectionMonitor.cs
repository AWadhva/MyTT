using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Runtime.InteropServices;
using IFS2.Equipment.CSCReaderAdaptor;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules.ConnectionMonitor;
using IFS2.Equipment.CryptoFlex;

namespace IFS2.Equipment.TicketingRules
{
    public class ThalesReaderConnectionMonitor : ReaderConnectionMonitor
    {
        string port;        
        int comSpeed;

        int _hRw = -1;
        IV4ReaderApi api;
        V4ReaderConf conf;

        CryptoFlexFunctions cFlex;
        CCHSSAMManger mCCHSSAMMgr;

        public ThalesReaderConnectionMonitor(IV4ReaderApi api_, ISyncContext syncContext_, string port_, int comSpeed_, object otherConf)
            : base(syncContext_)
        {
            port = port_;
            comSpeed = comSpeed_;
            api = api_;

            conf = (V4ReaderConf)otherConf;
        }

        public override bool Init(out object readerDetails)
        {
            readerDetails = "";
            _hRw = -1;

            var ERR_CODE = (CSC_API_ERROR)api.sCSCReaderStartEx(port, comSpeed, out _hRw);
            if (ERR_CODE != CSC_API_ERROR.ERR_NONE)
                return false;
            
            StatusCSC pStatusCSC;
            ERR_CODE = (CSC_API_ERROR)V4Adaptor.sSmartStatusEx(_hRw, out pStatusCSC);
            if (ERR_CODE != CSC_API_ERROR.ERR_NONE)
                return false;
            {
                if (pStatusCSC.ucStatCSC != CONSTANT.ST_VIRGIN)
                    ERR_CODE = (CSC_API_ERROR)V4Adaptor.sCscRebootEx(_hRw);
                
                if (ERR_CODE != CSC_API_ERROR.ERR_NONE)
                    return false;
            }

            {
                if (conf.rfPower > 0)
                    ERR_CODE = Reader.SetReaderRFPower(conf.rfPower, _hRw);
                
                if (ERR_CODE != CSC_API_ERROR.ERR_NONE)
                    return false;
            }
            
            var details = new ThalesReader();
            details.handle = _hRw;
            
            readerDetails = details;
            return true;
        }

        public override bool Connected()
        {
            if (conf.readerTyp == CSC_READER_TYPE.V4_READER)
            {
                var err = Reader.PingReader(CSC_READER_TYPE.V4_READER, _hRw);
                bool bConnected = err == CSC_API_ERROR.ERR_NONE;
                if (!bConnected)
                    Reader.StopReader(CSC_READER_TYPE.V4_READER, _hRw);
                return bConnected;
                //return !(err == CSC_API_ERROR.ERR_DEVICE
                //    || err == CSC_API_ERROR.ERR_TIMEOUT // this code is observed
                //            || err == CSC_API_ERROR.ERR_LINK || err == CSC_API_ERROR.ERR_COM); // Putting based on guess; hence can remove them
            }
            else if (conf.readerTyp == CSC_READER_TYPE.V3_READER)
            {
                StatusCSC statusCSC = new StatusCSC();
                var err = Reader.StatusCheck(CSC_READER_TYPE.V3_READER, _hRw, ref statusCSC);
                return !(err == CSC_API_ERROR.ERR_DEVICE
                    || err == CSC_API_ERROR.ERR_TIMEOUT // this code is observed
                            || err == CSC_API_ERROR.ERR_LINK || err == CSC_API_ERROR.ERR_COM); // Putting based on guess; hence can remove them
            }
            else
                throw new NotImplementedException();
        }
    }
}