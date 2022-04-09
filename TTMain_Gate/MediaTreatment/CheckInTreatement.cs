using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using Common;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    class CheckInTreatement : IMediaTreatment
    {
        public CheckInTreatement(CSC_READER_TYPE rwTyp_, int hRw_)
        {
            rwTyp = rwTyp_;
            hRw = hRw_;
        }

        CSC_READER_TYPE rwTyp;
        int hRw;

        #region IMediaTreatment Members

        public void Do(StatusCSCEx status, DateTime dt)
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
                }
                var validationResult = ValidationRules.ValidateFor(MediaDetectionTreatment.CheckIn, logMedia);
                if(validationResult == TTErrorTypes.NoError)
                    ValidationRules.UpdateForCheckIn(logMedia);
                
                if (logMedia.isSomethingModified)
                    csc.Write(logMedia);
            }
        }
        #endregion
    }
}