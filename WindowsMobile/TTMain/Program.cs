using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using IFS2.Equipment.Common;
using System.Threading;

namespace IFS2.Equipment.TicketingRules
{
    class Program
    {
        static private string _project;
        static public string Project { get { return _project; } }

        static void Main(string[] args)
        {
            _project = (string)Configuration.ReadParameter("Project", "string", "DELHI");
            string additionalConfs = args.FirstOrDefault(x => x.StartsWith("Conf:"));

            if (additionalConfs != null)
            {
                try
                {
                    var confs = (additionalConfs.Split(':'))[1].Split(',').ToList();
                    Configuration.SetAdditionalConfigurations(confs);
                    IFS2.Equipment.Common.Parameters.SetAdditionalConfigurations(confs);
                }
                catch { }
            }

            Communication.Start();
            SetMyLogLevel.Initialise();
            Logging.Log(LogLevel.Information, "Starting Ticketing Rules Application");
            Logging.Log(LogLevel.Information, Configuration.DumpAllConfiguration());
            Logging.Log(LogLevel.Information, IFS2.Equipment.Common.Parameters.DumpAllConfiguration());

            MainTicketingRules main = null;
            Thread mainThread = null;

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
            }

            main.semStopAsked.WaitOne();
            //Console.ReadKey();
#if WindowsCE || PocketPC
            if (mainThread != null)
#else
            if (mainThread != null && mainThread.IsAlive)
#endif

                mainThread.Abort();
        }
    }
}
