﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Diagnostics;
using IFS2.Equipment.TicketingRules.MediaTreatment;

namespace IFS2.Equipment.TicketingRules.Gate.MediaTreatment
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
                try
                {
                    bool bRead = csc.ReadMediaData(logMedia, MediaDetectionTreatment.CheckOut);
                    if (!bRead)
                    {
                        Transmit.FailedRead();
                        return null;
                    }
                    else
                        return logMedia;
                }
                catch { return null; }
            }
            else
                return null;            
        }

        public void Write()
        {
            if (validationResult == TTErrorTypes.NoError)
                ValidationRules.UpdateForCheckOut(logMedia);

            if (logMedia.isSomethingModified)
            {
                List<int> doesntMatter;
                if (!csc.Write(logMedia, out doesntMatter))
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

        public TTErrorTypes Validate(LogicalMedia logMedia)
        {
            validationResult = ValidationRules.ValidateFor(MediaDetectionTreatment.CheckOut, logMedia);
            return validationResult;
        }

        #endregion

        #region IMediaTreatment Members


        SmartFunctions IMediaTreatment.sf
        {
            get { return sf; }
        }

        public DelhiDesfireEV0 hwCSC
        {
            get { return hwCSC; }
        }

        #endregion
    }
}