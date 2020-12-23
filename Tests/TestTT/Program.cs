using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.Common;

namespace TestTT
{
    class Program
    {
        static void Main(string[] args)
        {

            bool b = Convert.ToBoolean(7);


            //Initialisation of Logical Media Structure
            Console.WriteLine("Initialisation of Logical Media Structure");
            LogicalMedia lm = new LogicalMedia();
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();
            Console.WriteLine(lm.ToXMLString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

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
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Initialisation
            Initialisation ini = lm.Initialisation;
            ini.BatchReferenceRead = 345;
            ini.DateTimeRead = new DateTime(2014, 02, 01); 
            ini.EquipmentNumberRead = 123;
            ini.EquipmentTypeRead = EquipmentFamily.HHD;
            ini.ServiceProviderRead = 2;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

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
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Initialisation of Validation
            Validation val = lm.Application.Validation;
            val.BonusValueRead=1000;
            //val.DateTimeRead= new DateTime(2014, 08, 10,10,0,0);
            //val.LastEntryDateTimeRead = new DateTime(2014, 08, 10,9,0,0);
            val.LocationRead=67;
            //val.TripsRead=10;
            //val.TypeRead=Validation.TypeValues.Entry;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Last Add Value Data
            LocalLastAddValue lcav = lm.Application.LocalLastAddValue;
            lcav.AmountRead = 1000;
            lcav.DateTimeRead = new DateTime(2013, 7, 7);
            lcav.EquipmentNumberRead = 1234;
            lcav.EquipmentTypeRead = EquipmentFamily.TVM;
            lcav.OperationTypeRead = LocalLastAddValue.OperationTypeValues.Cash;
            lcav.SequenceNumberRead = 123456789;
            lcav.ServiceProviderRead = 2;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Customer Data
            Customer cu = lm.Application.Customer;
            cu.BirthDateRead = new DateTime(1950, 1, 1);
            cu.IDTypeRead = 1;
            cu.IDRead = "123456789";
            cu.Language = Customer.LanguageValues.Hindi;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Agent Data
            Agent ag = lm.Application.Agent;
            ag.CodeRead = 123;
            ag.MaxTripsNumberRead = 23;
            ag.ProfileRead = Agent.AgentProfileValues.Maintenance;
            ag.ReferenceRead = 3456;
            ag.ServiceProviderRead = 2;
            ag.TripsExpiryDateRead = new DateTime(2017, 1, 1);
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Product Parameters
            Products ps = lm.Application.Products;
            OneProduct p = new OneProduct();
            ps.Add(p);
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();
            ps.Product(0).DurationValidityRead = 10;
            ps.Product(0).EndOfValidityRead = new DateTime(2016, 2, 2);
            ps.Product(0).StartOfValidityRead = new DateTime(2008, 1, 2);
            ps.Product(0).TypeRead = 1;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //TPurse
            TPurse tp = lm.Purse.TPurse;
            tp.BalanceRead = 4567;
            tp.SequenceNumberRead = 123456789;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Last Add Value Data
            LastAddValue lav = lm.Purse.LastAddValue;
            lav.AmountRead = 1000;
            lav.DateTimeRead = new DateTime(2013, 7, 7);
            lav.EquipmentNumberRead = 1234;
            lav.EquipmentTypeRead = EquipmentFamily.TVM;
            lav.OperationTypeRead = LastAddValue.OperationTypeValues.Cash;
            lav.SequenceNumberRead = 123456789;
            lav.ServiceProviderRead = 2;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //AutoReload
            AutoReload ar = lm.Purse.AutoReload;
            ar.AmountRead = 1000;
            ar.ExpiryDateRead = new DateTime(2023, 7, 7);
            ar.AutoTopupDateAndTimeRead = new DateTime(2013, 7, 7,9,57,0);
            ar.StatusRead = AutoReload.StatusValues.Enabled;
            ar.ThresholdRead = 150;
            ar.UnblockingSequenceNumberRead = 1;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //History
            History his = lm.Purse.History;
            OneTransaction t = new OneTransaction();
            OneTransaction t2 = new OneTransaction();
            OneTransaction t3 = new OneTransaction();
            OneTransaction t4 = new OneTransaction();
            OneTransaction t5 = new OneTransaction();
            OneTransaction t6 = new OneTransaction();
            OneTransaction t7 = new OneTransaction();
            OneTransaction t8 = new OneTransaction();
            OneTransaction t9 = new OneTransaction();
            OneTransaction t10 = new OneTransaction();
            his.Add(t);
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();
            his.Transaction(0).AmountRead = 50;
            his.Transaction(0).DateTimeRead=new DateTime(2013,07,08);
            his.Transaction(0).EquipmentNumberRead=1200;
            his.Transaction(0).EquipmentTypeRead = EquipmentFamily.TOM;
            his.Transaction(0).LocationRead=45;
            his.Transaction(0).NewBalanceRead=1250;
#if _OldCommon_
            his.Transaction(0).OperationTypeRead = OneTransaction.OperationTypeValues.NoValueDeductedInExit;
#else
            his.Transaction(0).OperationTypeRead = OperationTypeValues.NoValueDeductedInExit;
#endif
            his.Transaction(0).SequenceNumberRead=1;
            his.Transaction(0).ServiceProviderRead=2;
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();
            t2.AmountRead = 50;
            t2.DateTimeRead = new DateTime(2013, 07, 08);
            t2.EquipmentNumberRead = 1200;
            t2.EquipmentTypeRead = EquipmentFamily.TOM;
            t2.LocationRead = 45;
            t2.NewBalanceRead = 1250;
#if _OldCommon_
             t2.OperationTypeRead = OneTransaction.OperationTypeValues.NoValueDeductedInExit;
#else
            t2.OperationTypeRead = OperationTypeValues.NoValueDeductedInExit;
#endif          
            t2.SequenceNumberRead = 2;
            t2.ServiceProviderRead = 2;
            his.Add(t2);
            t3.AmountRead = 50;
            t3.DateTimeRead = new DateTime(2013, 07, 08);
            t3.EquipmentNumberRead = 1200;
            t3.EquipmentTypeRead = EquipmentFamily.TOM;
            t3.LocationRead = 45;
            t3.NewBalanceRead = 1250;
#if _OldCommon_
              t3.OperationTypeRead = OneTransaction.OperationTypeValues.NoValueDeductedInExit;
#else
            t3.OperationTypeRead = OperationTypeValues.NoValueDeductedInExit;
#endif           
            t3.ServiceProviderRead = 2;
            t3.SequenceNumberRead = 3;
            his.Add(t3);
            t4.SequenceNumberRead = 4;
            his.Add(t4);
            t5.SequenceNumberRead = 5;
            his.Add(t5);
            t6.SequenceNumberRead = 6;
            his.Add(t6);
            t7.SequenceNumberRead = 7;
            his.Add(t7);
            t8.SequenceNumberRead = 8;
            his.Add(t8);
            t9.SequenceNumberRead = 9;
            his.Add(t9);
            t10.SequenceNumberRead = 10;
            his.Add(t10);
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //
            StreamWriter sw = new StreamWriter("csc.xml");
            sw.WriteLine(lm.ToXMLString());
            sw.Close();
            sw = null;
            Console.WriteLine(lm.ToXMLString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

            //Test du Undo qui ne devrait rien changé
            lm.Undo();
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();
            //Test du Reset
            lm.Reset();
            Console.WriteLine(lm.ToString());
            Console.WriteLine("Push a key to continue");
            Console.ReadKey();

        }
    }
}
