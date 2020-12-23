using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.BackOffice.ThalesSecLibrary;
using IFS2.Equipment.Common;

namespace TestFunctions
{
    class Program
    {
        static private byte[] serialNbrBytes = new byte[8];
        static void Main(string[] args)
        {


            var bitBuffer = new bool[256];
            int index = 0;


            //country 10 bits
            index = CFunctions.ConvertToBits(818, index, 10, bitBuffer);

            byte[] databuff = CFunctions.ConvertBoolTableToBytes(bitBuffer, 256);
            Console.WriteLine("GetBitData Result :" + databuff[0].ToString("X2")+" "+databuff[1].ToString("X2"));
           
            byte[] datain = new byte[32];
            for (int i = 0; i < 32; i++) datain[i] = 0;
            datain[0] = 0x10;
            datain[1] = 0x88;
            ulong l = CFunctions.GetBitData(3, 10, datain);
            Console.WriteLine(l.ToString());
            Console.ReadKey();
            l = CFunctions.GetBitData(1, 8, datain);
            Console.WriteLine(l.ToString());
            Console.ReadKey();
            for (int i = 0; i < 8; i++) serialNbrBytes[i] = (byte)i;
            long snbr = (long)CFunctions.GetBitData(0, 64, serialNbrBytes);
            Console.WriteLine("GetBitData Result :"+snbr.ToString("X2"));
            snbr = BitConverter.ToInt64(serialNbrBytes, 0);
            Console.WriteLine("GetBitData Result :"+snbr.ToString("X2"));
            Console.ReadKey();
        }
    }
}
