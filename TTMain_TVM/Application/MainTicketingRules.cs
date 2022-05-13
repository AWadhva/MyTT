using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.MediaTreatment.TVM;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules : TaskThread
    {
        public MainTicketingRules()
            : base("MainTicketingRules")
        {
            InitParameterRelated();
            TVMRelatedMessages();
            app = new IFS2.Equipment.TicketingRules.TVM.Application(this, new SendMessage_ActionTransmitter());
        }

        readonly string MMIChannel = "MMIChannel";
        //readonly string CoreChannel = "CoreChannel";

        private void TVMRelatedMessages()
        {
            Communication.AddEventsToReceive(ThreadName, "ReloadTPurseOnCard", this);
            Communication.AddEventsToExternal("BadPassengerCardDetection;BadAgentCardDetection;AgentCardDetection;UpdateCardStatus", MMIChannel);
        }

        IFS2.Equipment.TicketingRules.TVM.Application app;

        public override int TreatMessageReceived(EventMessage eventMessage)
        {
            if (TreatParametersMessageReceived(eventMessage))
                return 0;
            if (TreatCommonMessage(eventMessage))
                return 0;

            switch (eventMessage.EventID)
            {
                case "ReloadTPurseOnCard":
                    long cardSerNbr = Convert.ToInt64(eventMessage.Attribute);
                    //if (Convert.ToInt64(eventMessage.Attribute) != _logMediaReloader.Media.ChipSerialNumber)
                    //{
                    //    Communication.SendMessage(ThreadName, "Answer", "UpdateCardStatus", Convert.ToString((int)TTErrorTypes.NotSameCard), "");
                    //    Logging.Log(LogLevel.Error, ThreadName + "_Card Read is not the Same");
                    //    return true;
                    //}

                    var msg_splitted = eventMessage.Message.Split(';');
                    int pValue = Convert.ToInt32(msg_splitted[0]);

                    string paymentMethodParam;
                    if (msg_splitted.Length > 1)
                        paymentMethodParam = msg_splitted[1];
                    else
                        paymentMethodParam = eventMessage._par[2];

                    PaymentMethods paymentType;
                    if (paymentMethodParam == "1")
                        paymentType = PaymentMethods.Cash;
                    else if (paymentMethodParam == "2")
                        paymentType = PaymentMethods.BankCard;
                    else
                    {
                        Debug.Assert(false);
                        paymentType = PaymentMethods.Unknown;
                    }
                    app.ReloadTPurseOnCard(cardSerNbr, pValue, paymentType);

                    return 0;
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