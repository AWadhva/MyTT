using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    public class ReaderException : Exception 
    {
        private CSC_API_ERROR _code;
        private int _hRw;
        public ReaderException(CSC_API_ERROR code, int hRw)
        {
            _code = code;
            _hRw = hRw;
        }

        public CSC_API_ERROR Code { get { return _code; } }
        public int HRw { get { return _hRw; } }
    }   
}
