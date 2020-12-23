using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string s;

                //s = DelhiSpecific.TreatSystemParameters("C:\\IFS2\\Data\\XdrTestFile\\afcsospb.162");
                //OverallParameters.Save(s);

                //s = DelhiSpecific.TreatTVMEquipmentParameters("C:\\IFS2\\Data\\XdrTestFile\\afceqsyb.002");
                //TVMEquipmentParameters.Save(s);

                //s = DelhiSpecific.TreatAgentList("C:\\IFS2\\Data\\XdrTestFile\\ppspaglb.147");
                //AgentList.Save(s);

                //s = DelhiSpecific.TreatEquipmentDenyList("C:\\IFS2\\Data\\XdrTestFile\\pmablk2b.087");
                //EquipmentDenyList.Save(s);

                s = DelhiSpecific.TreatMediaDenyList("C:\\IFS2\\Data\\XdrTestFile\\pspblk1b.053");
                MediaDenyList.Save(s);

                //s = DelhiSpecific.TreatRangeDenyList("C:\\IFS2\\Data\\XdrTestFile\\pspblk1b.352");
                //RangeDenyList.Save(s);

                //s = DelhiSpecific.TreatTopologyParameters("C:\\IFS2\\Data\\XdrTestFile\\afcsospb.162");
                //TopologyParameters.Save(s);

                //s = DelhiSpecific.TreatFileKeyListParameters("C:\\IFS2\\Data\\XdrTestFile\\key.key");
                //File.WriteAllText("C:\\IFS2\\Data\\CurrentXmlParameters\\TestKey.xml",s);

                //s=DelhiSpecific.TreatFareParameters("C:\\IFS2\\Data\\XdrTestFile\\ppfatabb.352");
                //FareParameters.Save(s);

            }
            catch (Exception e)
            {
            }
            Console.ReadKey();
        }
    }
}
