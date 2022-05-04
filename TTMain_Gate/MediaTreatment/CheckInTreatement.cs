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
    class CheckInTreatement : IMediaTreatment
    {
        // TODO: make this constructor parameters {CSC_READER_TYPE, int} of type 'object', so that this class is available for use with all readers.
        public CheckInTreatement(CSC_READER_TYPE rwTyp_, int hRw_, ISupervisor supervisor_, Action<ActionTaken, string[]> actionTransmitter)
        {
            rwTyp = rwTyp_;
            hRw = hRw_;
            supervisor = supervisor_;

            Transmit = new ActionTransmitter(actionTransmitter);
        }

        ActionTransmitter Transmit;

        CSC_READER_TYPE rwTyp;
        int hRw;
        ISupervisor supervisor;
        StatusCSCEx status;

        #region IMediaTreatment Members

        public void Do(StatusCSCEx status_)
        {
            status = status_;
            if (supervisor != null && !supervisor.MediaDetected(status))
                return;
            Resume();
        }

        public void Resume()
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
                if (!csc.ReadMediaData(logMedia, MediaDetectionTreatment.CheckIn))
                {
                    Transmit.FailedRead();
                    return;
                }
                var validationResult = ValidationRules.ValidateFor(MediaDetectionTreatment.CheckIn, logMedia);
                if (validationResult == TTErrorTypes.NeedToPerformAutoTopup)
                {
                    int amt = logMedia.Purse.AutoReload.AmountRead;
                    SalesRules.AddValueUpdate(logMedia, amt, PaymentMethods.BankTopup);
                    if (csc.Write(logMedia))
                    {
                        Transmit.AutoTopup(amt, logMedia);
                        
                        logMedia.OverlapModifiedToRead();// we don't want to waste time in re-reading the CSC
                        
                        validationResult = ValidationRules.ValidateFor(MediaDetectionTreatment.CheckIn, logMedia);
                        ValidationRules.UpdateForCheckIn(logMedia);
                    }
                    else
                        Transmit.FailedWrite();
                }
                else if (validationResult == TTErrorTypes.NoError)
                    ValidationRules.UpdateForCheckIn(logMedia);

                if (logMedia.isSomethingModified)
                {
                    if (!csc.Write(logMedia))
                        Transmit.FailedWrite();
                    else
                    {
                        if (validationResult == TTErrorTypes.NoError)
                            Transmit.CheckInPermitted(logMedia);
                        else if (validationResult == TTErrorTypes.MediaInDenyList)
                            Transmit.Blacklisted(logMedia);
                        else if (logMedia.Application.Validation.RejectCode != logMedia.Application.Validation.RejectCodeRead)
                            Transmit.CheckInNotPermitted_And_RejectCodeWrittenByMe(logMedia.Application.Validation.RejectCode, logMedia);
                        else
                            Transmit.CheckInNotPermitted(validationResult, logMedia);
                    }
                }
                else
                {
                    if (validationResult == TTErrorTypes.NoError)
                        Transmit.CheckInPermitted(logMedia);
                    else
                        Transmit.CheckInNotPermitted(validationResult, logMedia);
                }
            }
        }

        #endregion
    }
}