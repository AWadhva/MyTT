using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace TestMacAlo
{
    class Program
    {

        public class Crc16
        {
            const ushort polynomial = 0xA001;
            ushort[] table = new ushort[256];

            public ushort ComputeChecksum(byte[] bytes)
            {
                ushort crc = 0;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
                return crc;
            }

            public byte[] ComputeChecksumBytes(byte[] bytes)
            {
                ushort crc = ComputeChecksum(bytes);
                return BitConverter.GetBytes(crc);
            }

            public Crc16()
            {
                ushort value;
                ushort temp;
                for (ushort i = 0; i < table.Length; ++i)
                {
                    value = 0;
                    temp = i;
                    for (byte j = 0; j < 8; ++j)
                    {
                        if (((value ^ temp) & 0x0001) != 0)
                        {
                            value = (ushort)((value >> 1) ^ polynomial);
                        }
                        else
                        {
                            value >>= 1;
                        }
                        temp >>= 1;
                    }
                    table[i] = value;
                }
            }
        }
        public static byte[] EncryptDES(byte[] toEncrypt, byte[] key, byte[] iv)
        {
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream();
            cryptoProvider.IV = iv;
            cryptoProvider.Key = key;
            CryptoStream cryptoStream = null;
            cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateEncryptor(), CryptoStreamMode.Write);
            BinaryWriter writer = new BinaryWriter(cryptoStream);
            writer.Write(toEncrypt);
            writer.Flush();
            cryptoStream.FlushFinalBlock();
            writer.Flush();
            return memoryStream.GetBuffer();
        }

        static byte[] desKey ={01,0x23,0x45,0x67,0x89,0xAB,0xCD,0xEF};
        static byte[] iv = { 1, 2, 3, 4, 5, 6, 7, 8};
        static byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };

        
        static byte[] key_1 = { 0x57, 0x1B, 0x65, 0x73, 0x9E, 0x3C, 0xA8, 0x46 };


        static void Main(string[] args)
        {
            ushort res =MacAlgoAdaptor.ComputeCRC(buffer, buffer.Length);
            Console.WriteLine("\n"+res.ToString("X4"));
            Crc16 crc1 = new Crc16();
            ushort res1 = crc1.ComputeChecksum(buffer);
            Console.WriteLine("\n" + res1.ToString("X4"));

            byte[] buffer24_1 = { 0x04, 0x88, 0x42, 0x46, 0x8A, 0x6B, 0x52, 0x80, 0x33, 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x67, 0x4B, 0x67, 0x4B, 0x00, 0x90, 0x20, 0x40 };
            res = MacAlgoAdaptor.ComputeCRC(buffer24_1, buffer24_1.Length);
            Console.WriteLine("\n" + res.ToString("X4"));
            byte[] crcbuf = BitConverter.GetBytes(res);
            foreach (byte b in crcbuf) Console.Write(b.ToString("X2"));
            byte[] inblock = new byte[8];
            //Expand the 2 bytes Crc to 8 bytes inblock by repeatetion
            for (int i = 0; i < 8; i += 2)
            {
                Array.Copy(crcbuf, 0, inblock, i, 2);
            }
            Console.WriteLine("");
            foreach (byte b in inblock) Console.Write(b.ToString("X2"));
            byte[] mac = new byte[8];
            MacAlgoAdaptor.SetDeskey(key_1, 0);
            MacAlgoAdaptor.CalcDes(inblock, mac);
            Console.WriteLine("");
            foreach (byte b in mac) Console.Write(b.ToString("X2"));
            //des[1] = (byte)(res & 0xFF);
            //des[0] = (byte)((res >> 8) & 0xFF);
            //MacAlgoAdaptor.CalcDes(des, mac);
            //Console.WriteLine("");
            //foreach (byte b in mac) Console.Write(b.ToString("X2"));


            ushort crc = 10;
            MacAlgoAdaptor.SetDeskey(desKey,0);
            crcbuf = BitConverter.GetBytes(crc);
            foreach (byte b in crcbuf) Console.Write(b.ToString("X2"));
            byte[] outdata = new byte[32];
            MacAlgoAdaptor.CalcDes(crcbuf, outdata);
            Console.WriteLine("\n");
            foreach (byte b in outdata) Console.Write(b.ToString("X2"));
            for (int i = 0; i < 256; i++)
            {
            }
            outdata = EncryptDES(crcbuf, desKey,iv);
            Console.WriteLine("\n");
            foreach (byte b in outdata) Console.Write(b.ToString("X2"));
            Console.ReadKey();
        }
    }
}
