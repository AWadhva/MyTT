using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{
    public static class BaseParameters
    {


        public static string Initialise(string name)
        {
            try
            {
                Logging.Log(LogLevel.Information, "BaseParameters_Initialise Starting" + name);
                string file = Disk.BaseDataDirectory + "\\CurrentXmlParameters\\"+name+".xml";
#if WindowsCE
                string s = Disk.ReadAllTextFile(file);
#else

                string s = File.ReadAllText(file);
#endif
                Logging.Log(LogLevel.Verbose, "BaseParameter_Initialise " + name + " " + Convert.ToString(s.Length));
                if (Configuration.ReadBoolParameter("VerificationOfEODSignatureAtStart_" + name, false))
                {
                    if (!Crypto.VerifyXmlFile(file, BasicParameterFile.CCPublicKey()))
                    {
                        Logging.Log(LogLevel.Error, "BaseParameters.Initialise.ErrorSignature " + name);
                        throw (new Exception("****"));
                    }
                }
                return s;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "BaseParameters_Initialise " + name+" "+e.Message);
                throw (new Exception("****"));
            }
        }

        public static bool Save(string content,string name)
        {
            try
            {
                Logging.Log(LogLevel.Information, "BaseParameter.Save " + name + " " + Convert.ToString(content.Length));
                if (content == "")
                {
                    Logging.Log(LogLevel.Error, "BaseParameters_Save " + name + " " + "Empty");
                    throw (new Exception("****"));
                }
                try
                {
                    string s = content.Substring(0, 100);
                    Logging.Log(LogLevel.Information, "BaseParameters_Save Starting " + name + " " + s);
                }
                catch { }
                string file = Disk.BaseDataDirectory + "\\CurrentXmlParameters\\" + name + ".xml";
#if WindowsCE
                Utility.WriteAllText(file, content);                               
#else
                File.WriteAllText(file,content);
#endif
                Logging.Log(LogLevel.Information, "BaseParameter_Save After write of File " + file);
                //Test to remove to verify what we have in file
                FileInfo fi = new FileInfo(file);
                Logging.Log(LogLevel.Information,"BaseParameter_Save Verification of File "+file+ " "+Convert.ToString(fi.Length));
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "BaseParameters_Save " + name + " " + e.Message);
                throw (new Exception("****"));
            }
        }
    }
}
