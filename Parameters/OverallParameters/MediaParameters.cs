using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public static class MediaParameters
    {
        public class MediaTechnoElement
        {
            public int Reference;
            public int ValidityDuration;
        }

        public class MediaTypeElement
        {
            public string code;
        }

        private static Dictionary<string, MediaTypeElement> _medtypes = null;
        private static Dictionary<Int32, MediaTechnoElement> _mediatechs = null;


        public static bool LoadMediaTechnologyVersion(XmlElement root)
        {

            if (_mediatechs == null) _mediatechs = new Dictionary<Int32, MediaTechnoElement>();
            else _mediatechs.Clear();

            try
            {
                XmlNodeList nodelist = root.SelectNodes("Media/MediaTechnos/MediaTechno");
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        MediaTechnoElement mth = new MediaTechnoElement();
                        mth.Reference = Convert.ToInt32(node.SelectSingleNode("Ref").InnerText);
                        mth.ValidityDuration = Convert.ToInt32(node.SelectSingleNode("DurVal").InnerText);
                    }
                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad MediaTechnology " + e.Message);
                        FareParameters._faresError.SetAlarm(true);
                    }

                }
                return true;
            }

            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_LoadMediaTechnologies " + e.Message);
                throw(new Exception("****"));

            }
        }

        public static bool LoadMediaTypeVersion(XmlElement root)
        {
            if (_medtypes == null) _medtypes = new Dictionary<string, MediaTypeElement>();
            else _medtypes.Clear();
            try
            {
                XmlNodeList nodelist = root.SelectNodes("Media/MediaTypes/MediaType");
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        MediaTypeElement mtp = new MediaTypeElement();
                        mtp.code = node.SelectSingleNode("Code").InnerText;
                    }
                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad MediaType" + e.Message);
                        FareParameters._faresError.SetAlarm(true);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_LoadMediaType " + e.Message);
                throw(new Exception("****"));
            }
        }


    }
}
