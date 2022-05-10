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
        public CheckOutTreatement(CSC_READER_TYPE rwTyp_, int hRw_,
            Action<ActionTaken, string[]> actionTransmitter)
        {
            rwTyp = rwTyp_;
            hRw = hRw_;
            
            Transmit = new ActionTransmitter(actionTransmitter);
        }

        readonly Guid id = Guid.NewGuid();

        ActionTransmitter Transmit;
        CSC_READER_TYPE rwTyp;
        int hRw;
        
        SmartFunctions sf = new SmartFunctions();
        LogicalMedia logMedia = new LogicalMedia();
        DelhiDesfireEV0 csc;
        TTErrorTypes validationResult;

        #region IMediaTreatment Members

        public bool ReadAndValidate(StatusCSCEx status, out TTErrorTypes validationResult_, out LogicalMedia logMedia)
        {
            try
            {
                logMedia = this.logMedia;

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
                    if (!csc.ReadMediaData(logMedia, MediaDetectionTreatment.CheckOut))
                    {
                        Transmit.FailedRead();
                        validationResult = this.validationResult = TTErrorTypes.NoError;

                        return false;
                    }
                    validationResult = this.validationResult = ValidationRules.ValidateFor(MediaDetectionTreatment.CheckOut, logMedia);

                    return true;
                }
                validationResult = TTErrorTypes.NoError;
                return false;
            }
            finally
            {
                validationResult_ = validationResult;
            }
        }

        public void ResumeWrite()
        {
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
        
        public Guid Id
        {
            get { return id; }
        }

        #endregion
    }
}