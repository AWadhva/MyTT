using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Bluebird.RFID;

namespace IFS2.Equipment.CSCReaderAdaptor
{
   public class RFIDReader:CBBAPI
    {
        protected byte[] m_abyCmdBuf;
        protected byte[] m_abyResponBuf;

        internal void Lcdchs(string m_strMsg)
        {
           // throw new NotImplementedException();
        }
        public RFIDReader()
        {
            m_abyCmdBuf = new byte[312];
            m_abyResponBuf = new byte[312];
            
        }
        public bool AntennaOn()
        {
            m_abyCmdBuf[0] = 0x00;
            if (SendCommand("antenna on", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }

        public bool AntennaOff()
        {
            m_abyCmdBuf[0] = 0x00;
            if (SendCommand("antenna off", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }

        public bool GetReaderVersion()
        {
            m_abyCmdBuf[0] = 0x00;
            if (SendCommand("v", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }

        public bool ResetField()
        {
            m_abyCmdBuf[0] = 0x00;
            if (SendCommand("y", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }

        public bool ResetReader()
        {
            m_abyCmdBuf[0] = 0x00;
            if (SendCommand("x", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }

        public bool SetTagType(byte byType)
        {
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byType;

            if (SendCommand("o", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }

        public bool WriteUserPort(byte byPort)
        {
            m_abyCmdBuf[0] = 0x01;
            m_abyCmdBuf[1] = byPort;

            if (SendCommand("write userport", m_abyCmdBuf)
                && GetData(m_abyResponBuf))
                return true;
            return false;
        }
    }
}
