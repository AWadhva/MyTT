using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CryptoFlex;

namespace IFS2.Equipment.TicketingRules.SecurityModuleInitializer
{
    class CryptoFlexOnThalesReader : SAM
    {
        CryptoFlexConf conf;
        public CryptoFlexOnThalesReader(CryptoFlexConf conf_)
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
        }

        #region SAM Members

        public object Initialize(out bool bPresent, out bool bWorking)
        {
            bPresent = true;
            bWorking = true;

            cFlex = new CryptoFlexFunctions(conf.readerType, conf.hRw);
            return null;
        }

        public string GetSerialNumber()
        {
            return cFlex.GetSAMSerialNbr(samSlot).ToString();
        }

        public object GetStatus()
        {
            throw new NotImplementedException();
        }

        public bool IsPresent()
        {
            throw new NotImplementedException();
        }

        public byte[] GetCertificate(CERT_TYPE certTyp)
        {
            return cFlex.GetCertificate(samSlot, certTyp);
        }

        public byte[] EncryptUsingPrivateKey(byte[] buf, int keyNb)
        {
            return cFlex.InternalAuthDes(samSlot, keyNb, buf);
        }

        public int GetEQPLocalId()
        {
            return cFlex.GetEQPLocalId(samSlot);
        }

        CryptoFlexFunctions cFlex;
        DEST_TYPE samSlot;

        #endregion
    }

    public class CryptoFlexConf
    {
        public int hRw;
        public int slotId;
        public CSC_READER_TYPE readerType;
    }
}