using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.MediaTreatment;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Common;
using Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public class AgentCardTreatment : IMediaTreatment
    {
        SmartFunctions sf;
        DelhiDesfireEV0 csc;
        LogicalMedia logMedia;

        ActionTransmitter Transmit;

        public AgentCardTreatment(SmartFunctions sf_, DelhiDesfireEV0 csc_, LogicalMedia logMedia_, Action<AgentCardAction, string[]> Transmit_)
        {
            sf = sf_;
            csc = csc_;
            logMedia = logMedia_;
            Transmit = new ActionTransmitter(Transmit_);
        }

        #region IMediaTreatment Members

        public LogicalMedia Read(StatusCSCEx status)
        {
            if (!csc.ReadAgentData(logMedia, MediaDetectionTreatment.BasicAnalysis_AVM_TVM))
                return null;
            else
                return logMedia;
        }

        public TTErrorTypes Validate(LogicalMedia logMedia)
        {
            var err = CommonRules.CheckAgentData(logMedia);

            if (err == TTErrorTypes.NoError)
                Transmit.GoodAgentCard(logMedia);
            else
                Transmit.BadAgentCard(err, logMedia);

            return err;
        }
        
        readonly Guid id = Guid.NewGuid();
        public Guid Id
        {
            get { return Id; }
        }

        public void Write()
        {
            // for now, no equipment writes anything to the agent card
        }

        SmartFunctions IMediaTreatment.sf
        {
            get { return sf; }
        }

        public DelhiDesfireEV0 hwCSC
        {
            get { return csc; }
        }

        #endregion
    }

    public enum AgentCardAction
    {
        GoodAgentCard,
        BadAgentCard
    }

    class ActionTransmitter
    {
        Action<AgentCardAction, string[]> transmitter;

        public ActionTransmitter(Action<AgentCardAction, string[]> transmitter_)
        {
            transmitter = transmitter_;
        }

        internal void GoodAgentCard(LogicalMedia logMedia)
        {
            transmitter(AgentCardAction.GoodAgentCard, new string []{logMedia.ToXMLString()});
        }

        internal void BadAgentCard(TTErrorTypes err, LogicalMedia logMedia)
        {
            transmitter(AgentCardAction.BadAgentCard, new string[]{logMedia.ToXMLString(), ((int)err).ToString()});
        }

        public void FailedRead()
        {
            
        }
    }
}