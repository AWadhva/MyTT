using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace IFS2.Equipment.TicketingRules
{
    public class MacAlgoAdaptor
    {
        [DllImport("MacAlgo.dll",
            EntryPoint = "?ComputeCRC@MacAlgo@@SAGQAEK@Z",
            SetLastError = true,
#if WindowsCE
            CallingConvention = CallingConvention.Winapi)]
#else
            CallingConvention = CallingConvention.Cdecl)]
#endif
         public extern static ushort ComputeCRC(
            byte [] Data,
            int DataSize);

        [DllImport("MacAlgo.dll",
            EntryPoint = "?SetDeskey@MacAlgo@@SAXQAEF@Z",
            SetLastError = true,
#if WindowsCE
            CallingConvention = CallingConvention.Winapi)]
#else
            CallingConvention = CallingConvention.Cdecl)]
#endif
        public extern static void SetDeskey(
            byte [] key,
            short edf);

        [DllImport("MacAlgo.dll",
            EntryPoint = "?CalcDes@MacAlgo@@SAXQAE0@Z",
            SetLastError = true,
#if WindowsCE
        CallingConvention = CallingConvention.Winapi)]
#else
            CallingConvention = CallingConvention.Cdecl)]
#endif
        public extern static void CalcDes(
            byte [] inblock,
            byte [] outblock);
          
    }
}
