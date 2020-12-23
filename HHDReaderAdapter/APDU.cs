using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace IFS2.Equipment.CSCReaderAdaptor
{
    public static class APDU
    {
        /// <summary>
        /// /* CLS INS P1 P2 - Minimum */ 
        /// </summary>
        /// <param name="pCLA"></param>
        /// <param name="pINS"></param>
        /// <param name="pP1"></param>
        /// <param name="pP2"></param>
        /// <returns></returns>
        static public byte[] getApduISO(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2)
        {
            byte[] retApdu = new byte[ISOCONSTANTS.MIN_ISO_DATA_IN_LENGTH ];

            /* CLA INS P1 P2 - formatting [Mandatory]*/
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;
         //   retApdu[4] = 0x00;

            return retApdu;
        }
         /* CLS INS P1 P2 + LC + DATA IN */
        static public byte[] getApduISO(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2,
                                        byte[] pDataIn)
        {
            byte[] retApdu = new byte[ISOCONSTANTS.MIN_ISO_DATA_IN_LENGTH + ISOCONSTANTS.LX_ISO_IN_LENGTH + pDataIn.Length];

            Array.Clear(retApdu, 0, retApdu.Length);
            /* CLA INS P1 P2 - formatting [Mandatory] */
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            /* LC */
            retApdu[4] = (byte)pDataIn.Length;

            /* DATA IN */
            Array.Copy(pDataIn, 0, retApdu, 5, pDataIn.Length);

            return retApdu;
        }
         /* CLS INS P1 P2 + LC + DATA IN + LE  */
        static public byte[] getApduISO(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2,
                                        byte[] pDataIn,
                                        byte pLE)
        {
            byte[] retApdu = new byte[ISOCONSTANTS.MIN_ISO_DATA_IN_LENGTH + ISOCONSTANTS.LX_ISO_IN_LENGTH + pDataIn.Length + ISOCONSTANTS.LX_ISO_IN_LENGTH];

            /* CLA INS P1 P2 - formatting [Mandatory]*/
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            /* LC */
            retApdu[4] = (byte)pDataIn.Length;

            /* DATA IN */
            Array.Copy(pDataIn, 0, retApdu, 5, pDataIn.Length);

            /* Length Expected - Optional */
            retApdu[retApdu.Length-1] = pLE;

            return retApdu;
        }
        /* CLS INS P1 P2 + LC (Null) + LE */
        static public byte[] getApduISO(byte pCLA,
                                        byte pINS,
                                        byte pP1,
                                        byte pP2,
                                        byte pLE)
        {
            byte[] retApdu = new byte[ISOCONSTANTS.MIN_ISO_DATA_IN_LENGTH + ISOCONSTANTS.LX_ISO_IN_LENGTH];

            /* CLA INS P1 P2 - formatting [Mandatory]*/
            retApdu[0] = pCLA;
            retApdu[1] = pINS;
            retApdu[2] = pP1;
            retApdu[3] = pP2;

            /* LC */
            //retApdu[5] = CONSTANT.NULL;

            /* LE */
            retApdu[4] = pLE;

            return retApdu;
        }
        static public byte[] getBBApdu(byte[] mApduISO)
        {
            byte[] retApdu = new byte[mApduISO.Length + 1];
            /* Data length*/
            retApdu[0] =(byte) mApduISO.Length;

            /* DATA IN */
            Array.Copy(mApduISO, 0, retApdu, 1, mApduISO.Length);
            return retApdu;
        }
    }
}
