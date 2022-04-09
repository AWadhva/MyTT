using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    class Program
    {
        static private string _project;
        static public string Project { get { return _project; } }

        static void Main(string[] args)
        {
            Logging.Log(LogLevel.Information, "Starting Ticketing Rules Application");
            SetMyLogLevel.Initialise();

            _project = (string)Configuration.ReadParameter("Project", "string", "DELHI");
            string additionalConfs = args.FirstOrDefault(x => x.StartsWith("Conf:"));

            if (additionalConfs != null)
            {
                try
                {
                    Logging.Log(LogLevel.Information, "Starting with additional configuration : " + additionalConfs);
                    var confs = (additionalConfs.Split(':'))[1].Split(',').ToList();
                    Configuration.SetAdditionalConfigurations(confs);
                    IFS2.Equipment.Common.Parameters.SetAdditionalConfigurations(confs);
                }
                catch { }
            }
            Logging.Log(LogLevel.Information, "Starting Communication with others modules");
            Communication.Start();
            Logging.Log(LogLevel.Information, Configuration.DumpAllConfiguration());
            Logging.Log(LogLevel.Information, IFS2.Equipment.Common.Parameters.DumpAllConfiguration());

            MainTicketingRules main = null;
            Thread mainThread = null;
            ValidationRules.SetMacCalculator(new MacCalculator());

            try
            {
                main = new MainTicketingRules();
            }
            catch (Exception mainExc)
            {
                Logging.Log(LogLevel.Error, mainExc.Message);
                main = null;
            }

            if (main != null)
            {
                try
                {
                    mainThread = new Thread(new ThreadStart(main.ThreadProc));
                }
                catch (Exception e)
                {
                    Logging.Log(LogLevel.Error, "MainThread Starting " + e.Message);
                }
                mainThread.Start();
                Logging.Log(LogLevel.Information, "main thread launched");

                main.semStopAsked.WaitOne();
            }

            
            //Console.ReadKey();
#if WindowsCE
            if (mainThread != null)
#else
            if (mainThread != null && mainThread.IsAlive)
#endif

                mainThread.Abort();
        }
    }
}
