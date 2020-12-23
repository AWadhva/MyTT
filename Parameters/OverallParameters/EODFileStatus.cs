using System;
using System.Collections.Generic;
using System.Text;
using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class EODFileStatus
    {
        public string FileName;
        public int FileStatus;
    }

    public static class EODFileStatusList
    {
        private static Dictionary<string, int> _dico = null;
        private static Dictionary<string, bool> _confOptional = null;

        static EODFileStatusList()
        {
            try
            {
                _dico = new Dictionary<string, int>();
                //AgentList;MediaDenyList;RangeDenyList;EquipmentDenyList;TopologyParameters;OverallParameters;EquipmentParameters;TicketSaleParamters;FareParameters
                //For AVM we could put : AgentList:1;MediaDenyList:1;RangeDenyList:1;EquipmentDenyList:1;TopologyParameters:1;OverallParameters:1;EquipmentParameters:1;FareParameters:1
                _confOptional = Configuration.ReadDicoBoolParameter("StatusOnOptionalAlarmEODComponent", 1);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Critical,"EODFileStatus."+e.Message);
            }
        }

        public static void UpdateStatus(string fileName, int status)
        {
            try
            {
                int sta;
                if (_dico.TryGetValue(fileName,out sta))
                {
                    _dico[fileName]=status;
                }
                else
                {
                    _dico.Add(fileName,status);
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error,"EODFileStatusList.UpdateStatus "+e.Message);
            }
        }

        public static string EODMetaStatus()
        {
            string s = "";
            try
            {
                s="<EODMetaStatus>";
                foreach (string key in _dico.Keys)
                {
                    //If component not in alarm we can add.
                    //If in alarm we see if it is mandatory to send alarm or not
                    string s1 = "1";
                    if (_dico[key] == 0)
                    {
                        if (_confOptional.ContainsKey(key))
                        {
                            if (!_confOptional[key]) s1 = "0";                            
                        }
                        else
                        {
                            s1 = "0";
                        }
                    }                   
                    s += Utility.MakeTag(key, s1);
                }
                s += "</EODMetaStatus>";
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "EODFileStatusList.EODMetaStatus " + e.Message);
                s = "";
            }
            return s;
        }
    }
}
