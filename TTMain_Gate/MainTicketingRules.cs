using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Threading;
using IFS2.Equipment.TicketingRules.ConnectionMonitor;
using IFS2.Equipment.TicketingRules.SecurityModuleInitializer;
using IFS2.Equipment.TicketingRules.CommonTT;
using Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules : TaskThread
    {
        public MainTicketingRules()
            : base("MainTicketingRules")
        {
            InitParameterRelated();
            app = new IFS2.Equipment.TicketingRules.Gate.Application(this);
        }

        IFS2.Equipment.TicketingRules.Gate.Application app;

        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            if (TreatParametersMessageReceived(eventMessage))
                return 0;
            if (TreatCommonMessage(eventMessage))
                return 0;

            switch (eventMessage.EventID)
            {
                case "SetCheckinOrCheckOut":
                    {
                        int readerId = Convert.ToInt32(eventMessage._par[0]);
                        app.SetOperatingMode(readerId, eventMessage._par[1] == "1");
                        break;
                    }
                case "ResumeOperationOnRW":
                    {
                        int readerId = Convert.ToInt32(eventMessage._par[0]);
                        int messageId = Convert.ToInt32(eventMessage._par[0]);

                        app.ResumeOperationOnRW(readerId, messageId);
                        break;
                    }
            }
            return base.TreatMessageReceived(eventMessage);
        }        

#if WindowsCE
        public OpenNETCF.Threading.Semaphore semStopAsked = new OpenNETCF.Threading.Semaphore(0, 10000);
#else
        public Semaphore semStopAsked = new Semaphore(0, 10000);
#endif
    }
}