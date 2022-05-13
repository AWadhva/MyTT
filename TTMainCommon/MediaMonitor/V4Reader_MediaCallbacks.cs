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
    /// <summary>
    /// Is a static class. Maintains a datastructure of each R/W's
    ///     handle vs. {MediaProduced, MediaRemoved}
    /// Each r/w should 
    /// - Register itself, when connected.
    /// - DeRegister itself, when disconnected
    /// </summary>
    static class V4Reader_MediaCallbacks
    {
        class My
        {
            public int handle;
            public Action<StatusCSCEx, DateTime> MediaProduced;
            public Action<StatusCSCEx, DateTime> MediaRemoved;
        }

        static List<My> Rdrs = new List<My>();
        static public void Register(int handle, Action<StatusCSCEx, DateTime> MediaProduced, Action<StatusCSCEx, DateTime> MediaRemoved)
        {
            My rdr = new My();
            rdr.handle = handle;
            rdr.MediaProduced = MediaProduced;
            rdr.MediaRemoved = MediaRemoved;
            Rdrs.Add(rdr);
        }

        static public void DeRegister(int handle)
        {
            Rdrs.RemoveAll(x => x.handle == handle);
        }

        /// <summary>
        /// Callback method supplied to sSmartStartPollingEx. 
        /// The callback function is executed in the context of a task that is internal to ThalesCscApi.  ThalesCscApi assumes that the callback routine immediately returns.  It is your responsibility to ensure the following assertions:
        ///- make the callback routine as fast as possible.  Don’t perform any long processing from within the callback routine.  Instead, wake an application thread with a system synchronizing object;
        ///- allocate as little memory as possible from the stack;
        ///- you cannot call any blocking (especially system) function from within the callback routine;
        ///- you cannot call any function of ThalesCscApi.  It would result in a deadlock

        /// </summary>
        /// <param name="code"> contains the handle of the reader</param>
        /// <param name="status"> of type StatusCSC</param>
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

        /// <summary>
        /// Callback method supplied to sSmartStartDetectRemovalEx. 
        /// The callback function is executed in the context of a task that is internal to ThalesCscApi.  ThalesCscApi assumes that the callback routine immediately returns.  It is your responsibility to ensure the following assertions:
        ///- make the callback routine as fast as possible.  Don’t perform any long processing from within the callback routine.  Instead, wake an application thread with a system synchronizing object;
        ///- allocate as little memory as possible from the stack;
        ///- you cannot call any blocking (especially system) function from within the callback routine;
        ///- you cannot call any function of ThalesCscApi.  It would result in a deadlock

        /// </summary>
        /// <param name="code"> contains the handle of the reader</param>
        /// <param name="status"> of type StatusCSC</param>
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