using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using System.Threading;
using IFS2.Equipment.TicketingRules.MediaTreatment;
using IFS2.Equipment.TicketingRules.Gate;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules : TaskThread
    {
        public MainTicketingRules()
            : base("MainTicketingRules")
        {
            InitParameterRelated();
            app = new IFS2.Equipment.TicketingRules.Gate.Application(this, new SendMessage_ActionTransmitter());
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
                        return 0;
                    }
                case "ResumeOperationOnRW":
                    {
                        int readerId = Convert.ToInt32(eventMessage._par[0]);
                        Guid messageId = new Guid(eventMessage._par[1]);

                        app.ResumeOperation(readerId, messageId);
                        return 0;
                    }
                case "GetMachineID":
                    return 0;
                case "GetCSCReloaderStatus":
                    return 0;
                case "GetSAMStatus":
                    return 0;
                case "GetCertificateOfEqpt":
                    return 0;
                case "GetCertificateOfCA":
                    return 0;
                case "GetCertificate":
                    return 0;
                case "NewTicketingKeysFile":
                    return 0;
                case "Shutdown":
                    Communication.RemoveAllEvents(ThreadName);
                    return (-1); //To terminate the process
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