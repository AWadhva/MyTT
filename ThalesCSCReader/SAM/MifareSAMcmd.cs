using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public static class MifareSAMcmd
    {
        const byte MIFARE_SAM_CLS = 0x80;
        const byte MIFARE_SAM_INS_AUTH_HOST = 0xA4;
        const byte MIFARE_SAM_INS_GETVER = 0x60;
        const byte MIFARE_SAM_INS_GET_KEYENTRY = 0x64;
        const byte MIFARE_SAM_INS_GET_KUCENTRY = 0x60;
        const byte MIFARE_SAM_INS_KILL_AUTH = 0xCA;
        const byte MIFARE_SAM_INS_ENCRIPT = 0xED;


        const byte MIFARE_SAM_INS_AUTH_PICC = 0x0A;
        const byte MIFARE_SAM_INS_CHG_KEY_PICC = 0xC4;
      //  byte[] bRandA = { 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8 };
        //static byte[] SMAuthenticateHost1(byte authmode, byte keyNum, byte keyver, byte[] Indiv)
        //{
            
        //}
    }
}
