using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules.MediaTreatment
{
    public interface IMediaTreatment
    {
        /// <summary>
        /// returns True if the media is supported and it was successfully read. 
        /// TODO: remove this function and replace its usage with two separate functions: Read and Validate
        /// </summary>
        /// <param name="status"></param>
        /// <param name="validationResult"></param>
        /// <param name="logMedia"></param>
        /// <returns></returns>
        //bool ReadAndValidate(StatusCSCEx status, out TTErrorTypes validationResult
        //    , out LogicalMedia logMedia // TODO: remove it if not needed
        //    );

        LogicalMedia Read(StatusCSCEx status);
        TTErrorTypes Validate(LogicalMedia logMedia);
        void Write();

        Guid Id { get; }
        SmartFunctions sf { get; }
        DelhiDesfireEV0 hwCSC { get; }
    }    
}