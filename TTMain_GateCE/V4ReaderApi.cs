using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using IFS2.Equipment.CSCReaderAdaptor;

namespace IFS2.Equipment.TicketingRules
{
    class V4ReaderApi : IV4ReaderApi
    {        
        #region IV4ReaderApi Members

        public short sCSCReaderGetApiVersionEx(out int piMajorVersion, out int piMinorVersion)
        {
            throw new NotImplementedException();
        }

        public short sCSCReaderStartEx(string comPort, int speed, out int phRw)
        {
            IntPtr strPtr = IntPtr.Zero;

            if (comPort != String.Empty && comPort != null)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(comPort);
                strPtr = Marshal.AllocHGlobal(bytes.Length + 2);
                Marshal.Copy(bytes, 0, strPtr, bytes.Length);
                Marshal.WriteInt16(strPtr, bytes.Length, 0);
            }

            short errorCode = V4Adaptor.sCSCReaderStartEx(strPtr, speed, out phRw);
            if (strPtr != IntPtr.Zero) 
                Marshal.FreeHGlobal(strPtr);
            return errorCode;
        }

        #endregion

        #region IV4ReaderApi Members


        public short sCSCReaderStopEx(int phRw)
        {
            return V4Adaptor.sCSCReaderStopEx(phRw);
        }

        #endregion
    }
}
