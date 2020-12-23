using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Xml.Serialization;

using IFS2.Equipment.Common;
using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{

    public class MaxiTravelTime : BasicParameterFile
    {
        private EODTravelTimeParametersFile data = null;
        public override object Content
        {
            get { return data; }
        }
        public MaxiTravelTime()
            : base("MaxiTravelTime", 69, 
            Configuration.ReadBoolParameter("Parameters_MaxiTravelTimeExist", false),
            Configuration.ReadBoolParameter("Parameters_MaxiTravelTimeEOD", false),
            "TravelTimeParameters.xml")
        {
            data = new EODTravelTimeParametersFile();
        }

        public override bool LoadSpecific(string content)
        {
            data = SerializeHelper<EODTravelTimeParametersFile>.XMLDeserialize(content);
            Logging.Log(LogLevel.Verbose, "TravelTimeParameters " + SerializeHelper<EODTravelTimeParametersFile>.XMLSerialize(data));
            return true;
        }

        public string ListTravelTimes()
        {
            try
            {
                return SerializeHelper<EODTravelTimeParametersTravelTimeTable>.XMLSerialize(data.TravelTimeTable);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TravelTimeParameters.ListLines " + e.Message);
            }
            return "";
        }

        /// <summary>
        /// Returns default parameter in case of error on Fare tiers
        /// </summary>
        /// <param name="FareTiers"></param>
        /// <returns></returns>
        public int Duration(int FareTiers)
        {
            try
            {
                EODTravelTimeParametersTravelTimeTableDetail rec = data.TravelTimeTable.List.Find(x => x.FareTiers == FareTiers);
                if (rec!=null) return rec.Duration;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TravelTimeParameters.ListLines " + e.Message);
            }
            return FareParameters.PaidTime / 60;//Parameters.ReadIntParameter("MaximumTravelTime", 180);
        }

    }
}
