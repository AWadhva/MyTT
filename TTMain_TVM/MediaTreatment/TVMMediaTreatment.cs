using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules.TVM.MediaTreatment
{
    class TVMMediaTreatment : IMediaTreatment
    {
        public TVMMediaTreatment(CSC_READER_TYPE rwTyp_, int hRw_,
            Action<AgentCardAction, string[]> actionTransmitter)
        {
            rwTyp = rwTyp_;
            hRw = hRw_;
        }

        CSC_READER_TYPE rwTyp;
        int hRw;
        SmartFunctions sf = new SmartFunctions();
        StatusCSCEx status;
        LogicalMedia logMedia = new LogicalMedia();
        DelhiDesfireEV0 csc;
        //TTErrorTypes validationResult;

        #region IMediaTreatment Members        

        public void Write()
        {
            throw new NotImplementedException();
        }

        public Guid Id
        {
            get { return id; }
        }
        readonly Guid id = Guid.NewGuid();

        public LogicalMedia Read(StatusCSCEx status)
        {
            sf._delhiCCHSSAMUsage = true;
            sf._cryptoflexSAMUsage = false;

            sf.SetReaderType(rwTyp, hRw);
            sf.SetSerialNbr(status.SerialNumber);

            if (status.IsNFC)
                if (Configuration.ReadBoolParameter("NFCFunctionality", false))
                    sf._IsNFCCardDetected = true;
            if (status.IsDesFire)
            {
                csc = new DelhiDesfireEV0(sf);
                if (!csc.ReadMediaData(logMedia, MediaDetectionTreatment.BasicAnalysis_AVM_TVM))
                {
                    //Transmit.FailedRead();

                    return null;
                }
                else
                    return logMedia;
            }
            else
                return null;
        }

        public TTErrorTypes Validate(LogicalMedia logMedia)
        {
            return ValidationRules.ValidateFor(MediaDetectionTreatment.TOM_AnalysisForAddVal, logMedia);
        }

        #endregion

        #region IMediaTreatment Members


        SmartFunctions IMediaTreatment.sf
        {
            get { throw new NotImplementedException(); }
        }

        public DelhiDesfireEV0 hwCSC
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}