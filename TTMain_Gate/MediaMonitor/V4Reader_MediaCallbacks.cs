using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using System.Runtime.InteropServices;

namespace IFS2.Equipment.TicketingRules.MediaMonitor
{
    static class V4Reader_MediaCallbacks
    {
        class My
        {
            public int handle;
            public Action<StatusCSCEx, DateTime> MediaProduced;
            public Action<StatusCSCEx, DateTime> MediaRemoved;
        }

        static List<My> Rdrs = new List<My>();
        static public void Subscribe(int handle, Action<StatusCSCEx, DateTime> MediaProduced, Action<StatusCSCEx, DateTime> MediaRemoved)
        {
            My rdr = new My();
            rdr.handle = handle;
            rdr.MediaProduced = MediaProduced;
            rdr.MediaRemoved = MediaRemoved;
            Rdrs.Add(rdr);
        }

        static public void UnSubscribe(int handle)
        {
            Rdrs.RemoveAll(x => x.handle == handle);
        }

        static public void StatusListenerMediaProduced(
            IntPtr code, IntPtr status
            )
        {
            DateTime msgReceptionTimestamp = DateTime.Now;

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            int handle = (int)code;
            var rdr = Rdrs.Find(x => x.handle == handle);
            if (rdr != null)
                rdr.MediaProduced(new StatusCSCEx(pStatusCSC), msgReceptionTimestamp);
            Marshal.FreeHGlobal(status);
        }

        static public void StatusListenerMediaRemoved(
            IntPtr code, IntPtr status
            )
        {
            DateTime msgReceptionTimestamp = DateTime.Now;            

            StatusCSC pStatusCSC = (StatusCSC)(Marshal.PtrToStructure(status, typeof(StatusCSC)));
            int handle = (int)code;
            var rdr = Rdrs.Find(x => x.handle == handle);
            if (rdr != null)
                rdr.MediaRemoved(new StatusCSCEx(pStatusCSC), msgReceptionTimestamp);
            
            Marshal.FreeHGlobal(status);
        }
    }
}