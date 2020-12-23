using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace IFS2.Equipment.TicketingRules
{
    public class SecurityMgr
    {
        public byte[] sessionkey = new byte[16];
        public byte[] iso14443a_crc(byte[] Data)   // DESFireSAM crc16 do not invert the result
        {

            int bt;
            int wCrc = 0x6363;
            int j = 0;
            int t8 = 0;
            int t9 = 0;
            int tA = 0;
            int Len = Data.Length;
            int maskB = 0x0000000000000000FF;
            int maskW = 0x00000000000000FFFF;


            do
            {
                bt = Data[j++] & maskB;
                bt = (bt ^ (wCrc & 0x00FF)) & maskB;
                bt = (bt ^ (bt << 4)) & maskB;


                t8 = (bt << 8) & maskW;
                t9 = (bt << 3) & maskW;
                tA = (bt >> 4) & maskW;
                wCrc = (wCrc >> 8) ^ (t8 ^ t9 ^ tA) & maskW;
            }
            while (j < Len);


            byte[] bb = new byte[2];
            bb[0] = (byte)(wCrc & maskB);
            bb[1] = (byte)((wCrc >> 8) & maskB);
            return bb;
        }
        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public void Cryptography(byte[] data, byte[] key, out byte[] crpt_data)
        {
            var initVector = StringToByteArray("0000000000000000");

            var tdes = new TripleDESCryptoServiceProvider();
            //{
            //    Key = Key,
            //    Mode = CipherMode.CBC,
            //    Padding = PaddingMode.None,
            //    BlockSize = 64,
            //    IV = IV

            //};
            tdes.Mode = CipherMode.CBC;
            tdes.Padding = PaddingMode.None;
            tdes.BlockSize = 64;
            var decryptor = DESCryptoExtensions.CreateWeakDecryptor(tdes, key, initVector);



            byte[] data1 = new byte[8];
            byte[] dec_data;
            byte[] dec_data1;
            int i = 0;
            if (data.Length < 9)
            {
                crpt_data = decryptor.TransformFinalBlock(data, 0, data.Length);
            }
            else
            {
                byte[] data2 = new byte[8];
                byte[] data3 = new byte[8];
                for (int j = 0; j < 8; j++)
                {
                    data1[j] = data[j];
                    data2[j] = data[j + 8];
                    data3[j] = data[j + 16];
                }
                byte[] crpt_data1 = decryptor.TransformFinalBlock(data1, 0, data1.Length);
                byte[] tmp = new byte[8];
                for (int j = 0; j < 8; j++)
                {
                    tmp[j] = (byte)(crpt_data1[j] ^ data2[j]);
                }
                byte[] crpt_data2 = decryptor.TransformFinalBlock(tmp, 0, tmp.Length);

                for (int j = 0; j < 8; j++)
                {
                    tmp[j] = (byte)(crpt_data2[j] ^ data3[j]);
                }
                byte[] crpt_data3 = decryptor.TransformFinalBlock(tmp, 0, tmp.Length);

                crpt_data = new byte[data.Length];
                int index = 0;
                for (i = 0; i < 8; i++) crpt_data[index++] = crpt_data1[i];
                for (i = 0; i < 8; i++) crpt_data[index++] = crpt_data2[i];
                for (i = 0; i < 8; i++) crpt_data[index++] = crpt_data3[i];
                //encrypted 
            }
            //  
           // log("DECRYPT DATA : " + BitConverter.ToString(crpt_data).Replace("-", string.Empty));


            decryptor.Dispose();
        }
        private void fill_3DES_sessionkey(byte[] rnda, byte[] rndb)
        {
            int i = 0, index = 0;

           // log("DEC RndA : " + BitConverter.ToString(rnda).Replace("-", string.Empty));

           // log("DEC RndB : " + BitConverter.ToString(rndb).Replace("-", string.Empty));


            for (i = 0; i < 4; i++)
                sessionkey[index++] = rnda[i];
            for (i = 0; i < 4; i++)
                sessionkey[index++] = rndb[i];
            /*
            for (i = 4; i < 8; i++)
                sessionkey[index++] = rnda[i];

            for (i = 4; i < 8; i++)
                sessionkey[index++] = rndb[i];
             */
            for (i = 0; i < 8; i++)
                sessionkey[index++] = sessionkey[i];

            if (sessionkey.Length > 16)
            {
                for (i = 0; i < 8; i++)
                    sessionkey[index++] = sessionkey[i];

            }

            //log("Session Key  : " + BitConverter.ToString(sessionkey).Replace("-", string.Empty));
        }
        public byte[] CalculateRndAB_AV1(byte[] randA, byte[] RndB, byte[] Key)
        {
            // byte[] Key = new byte[16] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            var IV = StringToByteArray("0000000000000000");
            TripleDES tripleDESalg = TripleDES.Create();


            var tdes = new TripleDESCryptoServiceProvider();
            //{
            //    Key = Key,
            //    Mode = CipherMode.CBC,
            //    Padding = PaddingMode.None,
            //    BlockSize = 64,
            //    IV = IV

            //};
            tdes.Mode = CipherMode.CBC;
            tdes.Padding = PaddingMode.None;
            tdes.BlockSize = 64;
            var decryptor = DESCryptoExtensions.CreateWeakDecryptor(tdes, Key, IV);
            var encryptor = DESCryptoExtensions.CreateWeakEncryptor(tdes, Key, IV);

            byte[] RndB_dec = decryptor.TransformFinalBlock(RndB, 0, RndB.Length);
           
            // shift randB one byte left and get randB'
            byte[] r1 = new byte[8];
            for (int i = 0; i < 7; i++)
            {
                r1[i] = RndB_dec[i + 1];
            }
            r1[7] = RndB_dec[0];
            
            //New
            // concat (randA + randB')
            byte[] b1b2 = new byte[16];
            for (int i = 0; i < b1b2.Length; i++)
            {
                if (i <= 7)
                {
                    b1b2[i] = randA[i];
                }
                else
                {
                    b1b2[i] = r1[i - 8];
                }
            }
            b1b2 = encryptor.TransformFinalBlock(b1b2, 0, b1b2.Length);
            //~SKS
            return b1b2;

        }
        public byte[] CalculateRndAB(byte[] RndB, byte[] Key)
        {
            // byte[] Key = new byte[16] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
          //  byte[] IV = { 0, 0, 0, 0, 0, 0, 0, 0 };
            var IV = StringToByteArray("0000000000000000");
            TripleDES tripleDESalg = TripleDES.Create();

            var tdes = new TripleDESCryptoServiceProvider();
            byte[] b1b2 = new byte[16];
#if !_BLUEBIRD_
            tdes.Mode = CipherMode.CBC;
            tdes.Padding = PaddingMode.None;
            tdes.BlockSize = 64;
            var decryptor = DESCryptoExtensions.CreateWeakDecryptor(tdes, Key, IV);

            byte[] RndB_dec = decryptor.TransformFinalBlock(RndB, 0, RndB.Length);


            // generate randA (integer 0-7 for trying) 
            byte[] randA = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                randA[i] = Byte.Parse(i.ToString());
            }

            // decrypt randA, should XOR with IV, but IV is all 0's, not necessary

            byte[] randA_dec = decryptor.TransformFinalBlock(randA, 0, randA.Length);

            // shift randB one byte left and get randB'
            byte[] r1 = new byte[8];
            for (int i = 0; i < 7; i++)
            {
                r1[i] = RndB_dec[i + 1];
            }
            r1[7] = RndB_dec[0];

            fill_3DES_sessionkey(randA, RndB_dec);
            // xor randB' with randA and decrypt
            byte[] b2 = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                b2[i] = (byte)(randA_dec[i] ^ r1[i]);
            }

            b2 = decryptor.TransformFinalBlock(b2, 0, b2.Length);

            // concat (randA + randB')
            //byte[] b1b2 = new byte[16];

            for (int i = 0; i < b1b2.Length; i++)
            {
                if (i <= 7)
                {
                    b1b2[i] = randA_dec[i];
                }
                else
                {
                    b1b2[i] = b2[i - 8];
                }
            }
#endif
            return b1b2;

        }

        private byte[] Decrpt(byte[] datain, byte[] key)
        {
            var IV = StringToByteArray("0000000000000000");
            TripleDES tripleDESalg = TripleDES.Create();


            var tdes = new TripleDESCryptoServiceProvider();
            //{
            //    Key = Key,
            //    Mode = CipherMode.CBC,
            //    Padding = PaddingMode.None,
            //    BlockSize = 64,
            //    IV = IV

            //};
            tdes.Mode = CipherMode.CBC;
            tdes.Padding = PaddingMode.None;
            tdes.BlockSize = 64;
            var decryptor = DESCryptoExtensions.CreateWeakDecryptor(tdes, key, IV);
            byte[] datain_dec = decryptor.TransformFinalBlock(datain, 0, datain.Length);
          
            return datain_dec;

        }
    }
}
