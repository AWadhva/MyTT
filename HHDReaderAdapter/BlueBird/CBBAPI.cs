//#define _BIP1300_

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Bluebird.RFID;


namespace IFS2.Equipment.CSCReaderAdaptor
{

  public  class CBBAPI
    {
        BBRFReader m_clsRFReader;

        public CBBAPI()
        {
            m_clsRFReader = new BBRFReader();
        }

        // Open communication device
        public bool OpenComm(String strCommDevice, byte byAutodetect, uint dwBaudRate, byte byProtocol)
        {
            CommSettings tCommSet = new CommSettings();
            tCommSet.baudrate = dwBaudRate;
            tCommSet.protocol = (sbyte)byProtocol;
            sbyte b_retv = m_clsRFReader.OpenComm(strCommDevice, byAutodetect, tCommSet);
            if (b_retv.Equals(1))
                return true;
            return false;
        }
        // Close comunication device
        public bool CloseComm()
        {
            m_clsRFReader.CloseComm();
            return true;
        }
        // Open reader device
        public bool OpenReader(byte byId, short sReaderType)
        {
            uint b_retv = m_clsRFReader.OpenReader(byId, sReaderType);
            if (b_retv.Equals(0))
                return true;
            return false;
        }
        // Close reader device
        public bool CloseReader()
        {
            uint dwRet = m_clsRFReader.CloseReader();
            if (dwRet.Equals(0))
                return true;
            return false;
        }
        // Emptys the receive buffer fo the communicate device
        public bool EmptyCommRcvBuffer()
        {
            m_clsRFReader.EmptyCommRcvBuffer();
            return true;
        }
        // Check the power state of a device
        public bool GetResumeState(ref bool bResumeStateActive)
        {
            sbyte b_retv = m_clsRFReader.GetResumeState();
            bResumeStateActive = b_retv != 0;
            return true;
        }
        // Set the current baud rate of the communication device
        public bool SetCommBaudRate(uint dwBaudRate)
        {
            m_clsRFReader.SetCommBaudRate(dwBaudRate);
            return true;
        }
        // Gets the current baud rate of the communication device
        public bool GetCommBaudRate(ref uint dwBaudRate)
        {
            dwBaudRate = m_clsRFReader.GetCommBaudRate();
            if (dwBaudRate.Equals(0x9044))
                return false;
            return true;
        }
        // Gets the current protocol of the communication device
        public bool GetCommProtocol(ref byte byProtocol)
        {
            byProtocol = (byte)m_clsRFReader.GetCommProtocol();
            if (byProtocol.Equals(87))
                return false;
            return true;
        }
        // Sets the current protocol of the communication device
        public bool SetCommProtocol(byte byProtocol)
        {
            m_clsRFReader.SetCommProtocol(byProtocol);
            return true;
        }
        // Sets the current time out of the communication device
        public bool SetCommTimeout(uint dwTimeout)
        {
            m_clsRFReader.SetCommTimeout(dwTimeout);
            return true;
        }
        // Gets the current tiem out of the communication device
        public bool GetCommTimeout(ref uint dwTimeout)
        {
            dwTimeout = m_clsRFReader.GetCommTimeout();
            if (dwTimeout.Equals(87))
                return false;
            return true;
        }
        // Gets current reader configuration
        public bool GetReaderConfig(ref ReaderSettings tReaderSet)
        {
            m_clsRFReader.GetReaderConfig(ref tReaderSet);
            return true;
        }
        // Gets the version string of a reader specified.
        public bool GetReaderType(byte[] abyDeviceVersion, ref int nNumChars)
        {
            int n = 0;
            byte[] b_buf = new byte[255];
            Array.Clear(b_buf, 0, 255);
            m_clsRFReader.GetReaderType(b_buf);

            if (nNumChars < b_buf.Length)
                n = nNumChars;
            else
                n = b_buf.Length;
            Array.Copy(b_buf, abyDeviceVersion, n);
            abyDeviceVersion[n] = (byte)'\0';
            nNumChars = n;
            return true;
        }
        // Returns the state of debug output of a reader
        public bool GetDebugOutputState(ref bool bActiveState)
        {
            sbyte b_retv = m_clsRFReader.GetDebugOutputState();
            if (b_retv.Equals(87))
                return false;
            return true;
        }
        // Sets the state of debug output of a reader
        public bool SetDebugOutputState(bool bActiveState)
        {
            m_clsRFReader.SetDebugOutputState(Convert.ToByte(bActiveState));
            return true;
        }
        // Receives debug data
        public bool GetDebugOutput(byte[] abyDebugBuf)
        {
            m_clsRFReader.GetDebugOutput(abyDebugBuf);
            return true;
        }
        // Sends a command to a reader
        public bool SendCommand(string strCmd, byte[] abyData)
        {

            uint b_retv = m_clsRFReader.SendCommand(Encoding.ASCII.GetBytes(strCmd), abyData);
            if (b_retv.Equals(1))
                return true;
            return false;
        }
        // Sends a command to a reader and receives data.
        public bool SendCommandGetData(byte[] szCmd, byte[] abyData, byte[] abyResultBuf, ref int nNumBytes)
        {
            uint b_retv = m_clsRFReader.SendCommandGetData(szCmd, abyData, abyResultBuf);

            nNumBytes = abyResultBuf[0];
            if (b_retv.Equals(1))
                return true;
            return false;
        }
        // Sends a command to a reader and receives data with time out
        public bool SendCommandGetDataTimeout(byte[] szCmd, byte[] abyData, byte[] abyResultBuf, int nTiemout, ref int nNumBytes)
        {
            uint b_retv = m_clsRFReader.SendCommandGetDataTimeout(szCmd, abyData, abyResultBuf, nTiemout);

            nNumBytes = abyResultBuf[0];
            if (b_retv.Equals(1))
                return true;
            return false;
        }
        // Receives data from a reader
        public bool GetData(byte[] abyResultBuf)
        {
            byte[] abyTempBuf = new byte[255];
            uint b_retv = m_clsRFReader.GetData(abyResultBuf);

            if (b_retv == 0 && abyResultBuf[0] > 0)
                return true;
            return false;
        }
        // Receives data from a reader with time out
        public bool GetDataTimeout(byte[] abyResultBuf, ref int nNumBytes, int nTimeout)
        {
            byte[] abyTempBuf = new byte[255];

            uint b_retv = m_clsRFReader.GetDataTimeout(abyResultBuf, (uint)nTimeout);

            nNumBytes = abyResultBuf[0];
            if (b_retv == 0 && nNumBytes > 0)
                return true;
            return false;
        }
        // Provides the command set of the DESFire tag and the DESFire SAM
        public bool SendDESFireCmd(byte byCmd, byte[] abyData,ref byte[] abyResultBuf)
        {
            uint b_retv = m_clsRFReader.DESFire(byCmd, abyData, abyResultBuf);

            if (b_retv.Equals(0))
                return true;
            return false;
        }
        // Provides the command set of the Calypso tag and Calypso SAM
        public bool SendCalypsoCmd(byte byCmd, byte[] abyData, byte[] abyResultBuf)
        {
            //uint b_retv = m_clsRFReader.Calypso(byCmd, abyData, abyResultBuf);

            //if (b_retv.Equals(0))
            //    return true;
            return false;

        }
        // Returns the DESFire SAM time out
        public bool GetDESFireSAMTimeout(ref int nTimeout)
        {
            nTimeout = m_clsRFReader.GetDESFireSAMTimeout();
            return true;
        }
        // Sets the DESFIre SAM time out
        public bool SetDESFireSAMTimeout(int nTimeout)
        {
            m_clsRFReader.SetDESFireSAMTimeout((byte)nTimeout);
            return true;
        }
        // Open the IC-Slot communication device
        public bool OpenICComm()
        {
            if (m_clsRFReader.OpenICComm())
                return true;
            return false;
        }
        // Close the IC-Slot communication device
        public bool CloseICComm()
        {
            if (m_clsRFReader.CloseICComm())
                return true;
            return false;
        }
        // Power down the IC card
        public bool ICPowerDown(bool bIsSAM, byte[] pbyResultBuf)
        {
#if _BIP1300_
            if (m_clsRFReader.ICPowerDown(bIsSAM, pbyResultBuf))
#else
            int i = 0;
            if (bIsSAM) i = 1;
            if (m_clsRFReader.ICPowerDown(i, pbyResultBuf))
#endif
                return true;
            return false;
        }
        // Power up the IC card
        public bool ICPowerOn(int bIsSAM, byte[] pbyInputParam, byte[] pbyResultBuf)
        {
#if _BIP1300_
            if (m_clsRFReader.ICPowerOn((bool)bIsSAM, pbyInputParam, pbyResultBuf))
#else
           // int i = 0;
           // if (bIsSAM) i = 1;
            if (m_clsRFReader.ICPowerOn(bIsSAM, pbyInputParam, pbyResultBuf))
#endif
                return true;
            return false;
        }
        // The parameters of the communication with the card
        public bool ICChangePPS(bool bIsSAM, byte[] pbyInputParam, byte[] pbyResultBuf)
        {
            if (m_clsRFReader.ICChangePPS(bIsSAM, pbyInputParam, pbyResultBuf))
                return true;
            return false;
        }
        // Change the baud rate of IC-Slot
        public bool ICChangebaudrate(uint dwBaudRate, byte[] pbyResultBuf)
        {
            if (m_clsRFReader.ICChangebaudrate(dwBaudRate, pbyResultBuf))
                return true;
            return false;
        }
        // Selects the operating mode
        public bool SAMSlotIOMode(bool bIsSAM, byte byMode, byte[] pbyResultBuf)
        {
            if (m_clsRFReader.SAMSlotIOMode(bIsSAM, byMode, pbyResultBuf))
                return true;
            return false;
        }
        // Sets the SAM card type and slot number and receiving stateus data from a reader
        public bool SAMDefType(byte bySAMSelectType, byte bySlotSAM, byte[] pbyResultBuf)
        {
            if (m_clsRFReader.SAMDefType(bySAMSelectType, bySlotSAM, pbyResultBuf))
                return true;
            return false;
        }
        // Sets the IC card type and slot number and receiving status data from a reader
        public bool ICDefType(byte bySAMSelectType, byte[] pbyResultBuf)
        {
            if (m_clsRFReader.ICDefType(bySAMSelectType, pbyResultBuf))
                return true;
            return false;
        }
        // Sends a command to a reader
        public bool SendSAMCommand(int bIsSAM, byte[] abyInputData)
        {
#if _BIP1300_
             bool i = false;
            if (bIsSAM>0) i = true;
            if (m_clsRFReader.SendSAMCommand(i, abyInputData))
#else
       
            if (m_clsRFReader.SendSAMCommand(bIsSAM, abyInputData))
#endif
                return true;
            return false;
        }
        // Sends a command to a reader
        public bool SendSAMCommand(bool bIsSAM, byte[] abyInputData, byte inDataLength)
        {
            byte[] mApdu = new byte[abyInputData.Length + 1];
            /* Data length*/
            mApdu[0] = inDataLength;

            /* DATA IN */
            Array.Copy(abyInputData, 0, mApdu, 1, abyInputData.Length);
#if _BIP1300_
            if (m_clsRFReader.SendSAMCommand(bIsSAM, mApdu))
#else
            int i = 0;
            if (bIsSAM) i = 1;
            if (m_clsRFReader.SendSAMCommand(i, mApdu))
#endif
                return true;
            return false;
        }
        public bool SendSAMCommand(int bIsSAM, byte[] abyInputData, byte inDataLength)
        {
            byte[] mApdu = new byte[abyInputData.Length + 2];
            /* Data length*/
            mApdu[0] = 0x00;//(inDataLength&0x00FF)>>8;
            mApdu[1] = inDataLength;

            /* DATA IN */
            Array.Copy(abyInputData, 0, mApdu, 2, abyInputData.Length);
#if _BIP1300_
             bool i = false;
            if (bIsSAM>0) i = true;
            if (m_clsRFReader.SendSAMCommand(true, mApdu))
#else

            if (m_clsRFReader.SendSAMCommand(bIsSAM, mApdu))
#endif
                return true;
            return false;
        }
        // Gets response data from a reader
        public bool GetSAMData(byte[] abyResultBuf)
        {
            if (m_clsRFReader.GetSAMData(abyResultBuf))
                return true;
            return false;
        }
        public bool GetSAMData(byte[] abyResultBuf, uint timeout)
        {
            if (m_clsRFReader.GetSAMDataTimeout(abyResultBuf, timeout))
                return true;
            return false;
        }

    }

}
