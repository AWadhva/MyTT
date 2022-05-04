using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    class CheckOutTreatement : IMediaTreatment
    {
        // TODO: make this constructor parameters {CSC_READER_TYPE, int} of type 'object', so that this class is available for use with all readers.
        public CheckOutTreatement(CSC_READER_TYPE rwTyp_, int hRw_, ISupervisor supervisor_, Action<ActionTaken, string[]> actionTransmitter)
        {
            rwTyp = rwTyp_;
            hRw = hRw_;
            
            Transmit = new ActionTransmitter(actionTransmitter);
        }

        ActionTransmitter Transmit;
        CSC_READER_TYPE rwTyp;
        int hRw;
        Action<ActionTaken, string[]> actionTransmitter;

        #region IMediaTreatment Members

        public void Do(StatusCSCEx status)
        {
            SmartFunctions sf = new SmartFunctions();

            sf._delhiCCHSSAMUsage = true;
            sf._cryptoflexSAMUsage = false;

            sf.SetReaderType(rwTyp, hRw);
            sf.SetSerialNbr(status.SerialNumber);

            if (status.IsNFC)
                if (Configuration.ReadBoolParameter("NFCFunctionality", false))
                    sf._IsNFCCardDetected = true;

            LogicalMedia logMedia = new LogicalMedia();
            if (status.IsDesFire)
            {
                DelhiDesfireEV0 csc = new DelhiDesfireEV0(sf);
                if (!csc.ReadMediaData(logMedia, MediaDetectionTreatment.CheckOut))
                {
                    Transmit.FailedRead();
                    return;
                }
                var validationResult = ValidationRules.ValidateFor(MediaDetectionTreatment.CheckOut, logMedia);
                if (validationResult == TTErrorTypes.NoError)
                    ValidationRules.UpdateForCheckOut(logMedia);

                if (logMedia.isSomethingModified)
                {
                    if (!csc.Write(logMedia))
                        Transmit.FailedWrite();
                    else
                    {
                        if (validationResult == TTErrorTypes.NoError)
                            Transmit.CheckOutPermitted(logMedia);
                        else if (validationResult == TTErrorTypes.MediaInDenyList)
                            Transmit.Blacklisted(logMedia);
                        else if (logMedia.Application.Validation.RejectCode != logMedia.Application.Validation.RejectCodeRead)
                            Transmit.CheckOutNotPermitted_And_RejectCodeWrittenByMe(logMedia.Application.Validation.RejectCode, logMedia);
                        else
                            Transmit.CheckOutNotPermitted(validationResult, logMedia);
                    }
                }
                else
                {
                    if (validationResult == TTErrorTypes.NoError)
                        Transmit.CheckOutPermitted(logMedia);
                    else
                        Transmit.CheckOutNotPermitted(validationResult, logMedia);
                }
            }
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}