// only for those functions which aren't there as expected in ReaderFunctions
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.CSCReaderAdaptor;
using System.Runtime.InteropServices;

namespace IFS2.Equipment.TicketingRules
{
    class V4ReaderApi : IV4ReaderApi
    {
        #region IV4ReaderApi Members

        public short sCSCReaderGetApiVersionEx(out int MajorVersion, out int MinorVersion)
        {
            IntPtr piMajorVersion = Marshal.AllocHGlobal(sizeof(int)),
                piMinorVersion = Marshal.AllocHGlobal(sizeof(int));

            MajorVersion = 0;
            MinorVersion = 0;

            short result = V4Adaptor.sCSCReaderGetApiVersionEx(piMajorVersion, piMinorVersion);
            unsafe
            {
                if (result == CONSTANT.NO_ERROR)
                {
                    int* MajorVerPtr = (int*)piMajorVersion.ToPointer();
                    int* MinorVerPtr = (int*)piMinorVersion.ToPointer();

                    MajorVersion = *MajorVerPtr;
                    MinorVersion = *MinorVerPtr;
                }
                Marshal.FreeHGlobal(piMajorVersion);
                Marshal.FreeHGlobal(piMinorVersion);
            }
            return result;
        }

        public short sCSCReaderStartEx(string comPort, int speed, out int phRw)
        {
            return V4Adaptor.sCSCReaderStartEx(comPort, speed, out phRw);
        }

        public short sCSCReaderStopEx(int phRw)
        {
            return V4Adaptor.sCSCReaderStopEx(phRw);
        }

        #endregion
    }
}