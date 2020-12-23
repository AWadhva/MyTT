using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using IFS2.TicketingRules.Common;
using IFS2.Equipment.Common;
using CommandLine.Utility;

namespace ReadCCHSSAMID
{
    class Program
    {
        static void PushKey()
        {
#if WindowsCE
            Console.WriteLine("Push Enter to exit");
            Console.ReadLine();
#else
            Console.WriteLine("Push a key to exit");
            Console.ReadKey();
#endif
        }


        static void Main(string[] args)
        {
            Arguments CommandLine = new Arguments(args);
#if WindowsCE
            string content = "";
            IndependantConfigurationKeyFile config = new IndependantConfigurationKeyFile();
            if (File.Exists("IFS2.Equipment.Tools.ReadCCHSSAMID.exe.config"))
            {
                try
                {
                    content = Utility.ReadAllTextFromFile("IFS2.Equipment.Tools.ReadCCHSSAMID.exe.config");
                }
                catch (Exception e5)
                {
                    Console.WriteLine("Configuration file is not found");
                    Console.WriteLine(e5.Message);
                    PushKey();
                    return;
                }
                try
                {
                    config = SerializeHelper<IndependantConfigurationKeyFile>.XMLDeserialize(content);
                }
                catch (Exception e1)
                {
                    Console.WriteLine("Error while reading configuration file "+e1.Message);
                    Console.WriteLine(content);
                    PushKey();
                    return;
                }
            }
#endif
            if (CommandLine["help"] != null)
            {
                Console.WriteLine("You shall enter :");
                Console.WriteLine("/port: the port number where is CSC Reader. By default it is COM1:");
                Console.WriteLine("/slot: the slot number where is ISAM or PSAM. By default it is slot 1");
                Console.WriteLine("/prod :  if production SAM");
                PushKey();
                return;
            }
            string comPort = "COM1:";
            if (CommandLine["port"] != null)
            {
                comPort = CommandLine["port"];
            }
            else
            {
#if WindowsCE
                comPort = config.Get("CSCReaderComPort", "COM1:");
#else
                comPort = LocalConfiguration.GetString("CSCReaderComPort", "COM1:");
#endif
            }
            int slot = 1;
            if (CommandLine["slot"] != null)
            {
                slot = Convert.ToInt32(CommandLine["slot"]);
            }
            else
            {
#if WindowsCE
                slot = config.GetInt("SAMSlotNumber", 1);
#else
                slot = LocalConfiguration.GetInt("SAMSlotNumber", 1);
#endif
            }
            bool production = false;
            if (CommandLine["prod"] != null)
            {
                production = true;
            }

            string samPinCode = "";
#if WindowsCE
            samPinCode = config.GetEncryptedParameter("ISAM_PinCode", "A9c5iunUgssqcfVxlfeDJg==");
#else
            samPinCode = LocalConfiguration.GetEncryptedParameter("ISAM_PinCode", "A9c5iunUgssqcfVxlfeDJg==");
#endif

            CCHSSAM sam = new CCHSSAM();
            int err=(int)sam.ReaderInitialise(4,comPort);
            if (err == 0)
            {
                Console.WriteLine("Reader Initialised");
                err = (int)sam.SAMInitialise(slot, production, samPinCode);
                if (err == 0)
                {
                    Console.WriteLine("SAM Initialised");
                    Console.WriteLine(Convert.ToString(sam.GetDsmId()));
                    //uint serialNumber = 0;
                    //err=(int)sam.GetDsmId(out serialNumber);
                    //if (err!=0)
                    //    Console.WriteLine("Error reading DSM ID :" + Convert.ToString(err));
                    //else
                    //    Console.WriteLine("DSMID :" + Convert.ToString(serialNumber));
                }
                else
                {
                    Console.WriteLine("SAM Not Initialised :" + Convert.ToString(err));
                }
            }
            else
            {
                Console.WriteLine("Reader Not Initialised :"+Convert.ToString(err));
            }
            PushKey();

        }
    }
}
