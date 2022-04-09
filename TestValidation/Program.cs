using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Common;

namespace TestValidation
{
    class Program
    {
        static void Main(string[] args)
        {
            LogicalMedia lm = new LogicalMedia();
                        //Madia
            Media m = lm.Media;
            m.BlockedRead = true;
            m.ChipSerialNumberRead = 123400009;
            m.ChipTypeRead = Media.ChipTypeValues.DesfireEV0;
            m.EngravedNumberRead = 14500000004567;
            m.ExpiryDateRead = new DateTime(2040, 02, 01);
            m.FormatVersionRead = 2;
            m.HardwareTypeRead = Media.HardwareTypeValues.DesfireCSC;
            m.InitialisationDateRead = new DateTime(2014, 02, 01);
            m.OperationalTypeRead = MediaDescription.OperationalTypeValues.Passenger;
            m.OwnerRead = 2;
            m.StatusRead = Media.StatusValues.Initialised;
            m.TestRead = true;
            m.TypeRead = Media.TypeValues.CSC;
            //Console.WriteLine(lm.ToString());
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();

            //Initialisation
            Initialisation ini = lm.Initialisation;
            ini.BatchReferenceRead = 345;
            ini.DateTimeRead = new DateTime(2014, 02, 01); 
            ini.EquipmentNumberRead = 123;
            ini.EquipmentTypeRead = EquipmentFamily.HHD;
            ini.ServiceProviderRead = 2;
            //Console.WriteLine(lm.ToString());
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();

            //Initialisation of Transport Application
            TransportApplication tr = lm.Application.TransportApplication;
            tr.OwnerRead = 1;
            tr.InitialisationDateRead = new DateTime(2014, 02, 01);
            tr.OperationalTypeRead = TransportApplication.OperationalTypeValues.Agent;
            tr.StatusRead = TransportApplication.StatusValues.Initialised;
            tr.TestRead = true;
            tr.FormatVersionRead = 3;
            tr.ExpiryDateRead = new DateTime(2015, 02, 01);
            tr.DepositRead = 1200;
            tr.CustomerLink = TransportApplication.CustomerLinkValues.Personalised;
            //Console.WriteLine(lm.ToString());
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();

            //Initialisation of Validation
            Validation val = lm.Application.Validation;
            val.BonusValueRead=1000;
            //val.DateTimeRead= new DateTime(2014, 08, 10,10,0,0);
            //val.LastEntryDateTimeRead = new DateTime(2014, 08, 10,9,0,0);
            val.LocationRead=67;
            //val.TripsRead=10;
            //val.TypeRead=Validation.TypeValues.Entry;
            //Console.WriteLine(lm.ToString());
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();

            //Last Add Value Data
            LocalLastAddValue lcav = lm.Application.LocalLastAddValue;
            lcav.AmountRead = 1000;
            lcav.DateTimeRead = new DateTime(2013, 7, 7);
            lcav.EquipmentNumberRead = 1234;
            lcav.EquipmentTypeRead = EquipmentFamily.TVM;
            lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.Cash;
            lcav.SequenceNumberRead = 123456789;
            lcav.ServiceProviderRead = 2;
            //Console.WriteLine(lm.ToString());
            //Console.WriteLine("Push a key to continue");
            //Console.ReadKey();

            //Customer Data
            Customer cu = lm.Application.Customer;
            cu.BirthDateRead = new DateTime(1950, 1, 1);
            cu.IDTypeRead = 1;
            cu.IDRead = "123456789";
            cu.Language = Customer.LanguageValues.Hindi;
            
            ValidationRules.ValidateFor(MediaDetectionTreatment.CheckIn, lm);

            bool x = lcav.isSomethingModified;
            lcav.ServiceProvider = 4;
            x = lcav.isSomethingModified;
        }
    }
}
