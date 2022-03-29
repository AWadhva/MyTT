using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.Parameters;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules
    {
        public static OneEvent _cscReloaderMetaStatus = null;

        private static OneEvent _readerSerialNumber = null;
        private static OneEvent _firmwareVersion = null;
        private static OneEvent _cscAPIVersion = null;
        private static OneEvent _cscChargeurVersion = null;

        // CCHS SAM Status
        private static OneEvent _cchsSAMType = null;

        public static OneEvent _dataSecurityModuleLocked = null;
        public static OneEvent _dataSecurityModuleFailure = null;
        public static OneEvent _dataSecurityModuleAusent = null;
        public static OneEvent _dataSecurityModuleIsOffLine = null;
        public static OneEvent _dataSecurityModuleMetaStatus = null;
        private static OneEvent _dataSecurityModuleSerialNumber = null;
        private static OneEvent _dataSecurityModuleFirmwareVersion = null;
        private static OneEvent _dataSecurityModuleDeviceNumber = null;


        public bool TreatCommonMessage(EventMessage eventMessage)
        {
            if (eventMessage.EventID == null || eventMessage.EventID == string.Empty)
                return false;

            switch (eventMessage.EventID.ToUpper())
            {
                case "KILLAPPLICATION":
                    semStopAsked.Release();
                    return true;
                case "STOPAPPLICATION":
                    Communication.SendMessage(ThreadName, "Answer", "StopApplicationAnswer", "TTApplication", "0");
                    semStopAsked.Release();
                    return true;
                case "GETPRODUCTFAMILY":
                    {
                        int product = Convert.ToInt32(eventMessage._par[0]);
                        Communication.SendMessage(ThreadName, "", "GetProductFamilyAnswer", ProductParameters.GetProductFamily(product).ToString());
                        return true;
                    }
                case "GETSOFTWAREVERSION":
                    string versoft = "<Software Name=\"TTApplicatipon\" >";
                    //All versions of all parts shall be added.
                    versoft += "<Module Name=\"TT Application\" Version=\"1\" />";
                    versoft += "</Software>";
                    Communication.SendMessage(ThreadName, "Answer", "GetSoftwareVersionAnswer", "TTApplication", versoft);
                    return true;
                case "INITIALISATION":
                    SharedData.Initialise(eventMessage.Attribute);
                    SharedData.SaveContextFile();
                    return true;
                case "EQUIPMENTLOCATION":
                    EquipmentLocation eqploc = SerializeHelper<EquipmentLocation>.XMLDeserialize(eventMessage.Attribute);
                    SharedData.LineNumber = eqploc.LineNumber;
                    SharedData.StationNumber = eqploc.StationNumber;
                    SharedData.ServiceProvider = (short)eqploc.ServiceProvider;
                    SharedData.SaveContextFile();
                    return true;

                case "GETALLEVENTS":
                    {
#if !WindowsCE
                        string s = IFSEventsList.GetBulkReadString("TTComponent");
                        s += IFSEventsList.GetBulkReadString("CSCReloaderDriver");
                        s += IFSEventsList.GetBulkReadString("DSMDriver");
#else
                    string s = IFSEventsList.GetStringForCC("TTComponent");
                    s += IFSEventsList.GetStringForCC("CSCReloaderDriver");
                    s += IFSEventsList.GetStringForCC("DSMDriver");
#endif
                        SendCommand("GetAllEventsAnswer", "TTApplication", s);
                    }                    
                    return true;
                case "VERIFYAGENTDATA": // TODO: see if it can be moved to Parameters.cs
                    try
                    {
                        //To add password and to test result of function.
                        //bool bAgentCard;
                        //string stations;
                        EOD_OneAgent agent;
                        AgentProfile agentProfile;
                        if (Configuration.ReadBoolParameter("Parameters_LocalAgentListToUse", false))
                        {
                            EOD_OneLocalAgent agt;
                            agentProfile = ((LocalAgentList)BasicParameterFile.Instance("LocalAgentList")).VerifyAgent(Convert.ToInt32(eventMessage.Attribute), eventMessage.Message, out agent);
                            if (agentProfile != AgentProfile.NotKnown)
                            {
                                Communication.SendMessage(ThreadName, "Answer", "VerifyAgentDataAnswer", "0", ((int)agentProfile).ToString(), agent.AgentCard.ToString(), agent.Stations, SerializeHelper<EOD_OneAgent>.XMLSerialize(agent));
                                return true;
                            }
                        }

                        agentProfile = AgentList.VerifyAgent(Convert.ToInt32(eventMessage.Attribute), eventMessage.Message, out agent);
                        if (agentProfile == AgentProfile.NotKnown)
                        {
                            //Agent is not Authorised
                            Communication.SendMessage(ThreadName, "Answer", "VerifyAgentDataAnswer", Convert.ToString((int)TTErrorTypes.BadAgentData), "");
                        }
                        else
                        {
                            Communication.SendMessage(ThreadName, "Answer", "VerifyAgentDataAnswer", "0", ((int)agentProfile).ToString(), agent.AgentCard.ToString(), agent.Stations, SerializeHelper<EOD_OneAgent>.XMLSerialize(agent));
                        }
                    }
                    catch (Exception e1)
                    {
                        Communication.SendMessage(ThreadName, "Answer", "VerifyAgentDataAnswer", Convert.ToString((int)TTErrorTypes.BadAgentData), "");
                        Logging.Log(LogLevel.Error, ThreadName + "_VerifyAgentData Error :" + e1.Message);
                        return true;
                    }
                    return true;
                case "SETLOGLEVEL":
                    {
                        if (eventMessage.Attribute == "TT")
                        {
                            SetMyLogLevel.Set(Utility.ParseEnum<LogLevel>(eventMessage.Message));
                        }
                        return true;
                    }
                case "AGENTLOGGEDIN":
                    Handle_AgentLoggedIn(eventMessage);
                    return true;
            }
            return false;
        }
        
        private void Handle_AgentLoggedIn(EventMessage eventMessage)
        {
            if (eventMessage._par.Length == 1)
            {
                if (eventMessage._par[0] == "-1")
                    SharedData._agentShift = null;
            }
            int shiftId = Convert.ToInt32(eventMessage._par[1]);
            int agentId = Convert.ToInt32(eventMessage._par[0]);
            AgentProfile profile = (AgentProfile)(Convert.ToInt32(eventMessage._par[2]));

            SharedData._agentShift = new AgentShift(shiftId, agentId, profile);
        }

        
        private void RegisterCommonMessages()
        {
            Communication.AddEventsToReceive(ThreadName, "StopApplication;AgentLoggedIn;VerifyAgentData;GetAllEvents;EquipmentLocation;Initialisation;KillApplication", this);
            Communication.AddEventsToReceive(ThreadName, "SetLogLevel", this);
            Communication.AddEventToReceive(ThreadName, "GetSoftwareVersion", this);
            Communication.AddEventsToReceive(ThreadName, "GetProductFamily", this);

            Communication.AddEventsToExternal("GetSoftwareVersionAnswer;StopApplicationAnswer", "MMIChannel");
            Communication.AddEventsToExternal("GetProductFamilyAnswer", "MMIChannel");
        }
        
    }
}
