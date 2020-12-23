using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;

using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules.SmartMedia
{
    public class MediaInterface
    {

        public MediaInterface()
        {
        }//public MediaInterface()

        public virtual void _Reset()
        {
        }
        /*
        protected virtual Boolean _ReadManufacturerData(LogicalMedia logMedia)
        {
            return true;
        }*/

        public virtual byte[] _ReadDataAPDU(byte fileid, byte fileType, int offset, int length)
        {
            
            return null;
        }
        public virtual byte[] _WriteDataAPUD(byte fileid, byte fileType, int offset, byte[] bdata, int lenth)
        {
            return null;
        }

        public virtual byte[] _CommitAPDU()
        {
            return null;
        }

        public virtual byte[] _RollbackAPDU()
        {
            return null;
        }

        public virtual byte[] _CreateApplicationAPDU(int appId, byte nKeysettings, byte nbKeys)
        {
            return null;
        }

        public virtual byte[] _CreateFileAPDU(byte fileId, byte filetype, byte bcommsettings, byte accessRightLSB, byte accessRightMSB, int nbRecords)
        {
            return null;
        }
        public virtual byte[] _DeleteFileAPDU(byte fileId)
        {
            return null;
        }

        public virtual byte[] _deleteAppAPDU(int appid)
        {
            return null;
        }
        public virtual byte[] _SelectAppAPDU(int appid)
        {
            return null;
        }

        public virtual byte[] _AuthenticateAPDU(byte keyNo)
        {
            return null;
        }
        public virtual byte[] _AuthenticateAPDU_Step2(byte[] mRndAB)
        {
            return null;
        }

        public virtual byte[] _ChangeKeySettingsAPDU(byte[] crypted_settings)
        {
            return null;
        }
    }// public class MediaInterface
}
