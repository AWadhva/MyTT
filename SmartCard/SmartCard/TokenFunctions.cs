using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using IFS2.Equipment.Common;
using System.Threading;

namespace IFS2.Equipment.TicketingRules
{
    public class TokenFunctions
    {
        /// <summary>
        /// Function to Read the Data from Mifare Ultralight Tokens
        /// Need to specify the number of blocks to read
        /// Note : Manufacturer Data Block is added by default, 
        /// So the number of blocks to read, should be excluding the 
        /// Manufacturer data block
        /// </summary>
        /// <param name="pNbrOfBlocks"></param>
        /// <param name="pSw1"></param>
        /// <param name="pSw2"></param>
        /// <param name="pResData"></param>
        /// <returns></returns>
        static public CSC_API_ERROR ReadBlocks(CSC_READER_TYPE pReaderType,
                                               int phRw,
                                               int pNbrOfBlocks, 
                                               out byte pSw1, 
                                               out byte pSw2, 
                                               out byte[] pResData)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_NOEXEC;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            //Add Extra block for the Manufacturer data [Default]
            int totalBlocks = pNbrOfBlocks + 1;
      
            byte[] pDataIn = new byte[totalBlocks];

            //Ultralight : Frame the Number of blocks list for Read
            for (int i = 0; i < totalBlocks; i++)
            {
                pDataIn[i] = (byte)(i * CONSTANT.MIFARE_ULTRALT_FLDS);
            }
#if _BLUEBIRD_
            pResData = new byte[1];//TODO needed to add actual data
#else
            Err = Reader.IsoCommand(pReaderType,
                                        phRw,
                                        DEST_TYPE.DEST_CARD,
                                        CFunctions.getApdu(CONSTANT.MIFARE_ULTRALT_CLA, CONSTANT.MIFARE_READ_INS, CONSTANT.NULL, (byte)totalBlocks, pDataIn),
                                        3,
                                        out pSw1,
                                        out pSw2,
                                        out pResData);
            //if (pSw1 == 150 && pSw2 == 250)
            //    Logging.Log(LogLevel.Verbose, "Dumping stak pSw1 == 150 && pSw2 == 250 \n" + Environment.StackTrace);
#endif
            return Err;
        }
        static class Config
        {
            static Config()
            {
                nReadBlocksBestEffort_CountAttempts = (int)Configuration.ReadParameter("ReadBlocksBestEffortCountAttempts", "int", "3");
                nReadBlocksBestEffort_GapBetweenAttemptsInMilliSec = (int)Configuration.ReadParameter("ReadBlocksBestEffort_GapBetweenAttemptsInMilliSec", "int", "40");
                nWriteBlocksBestEffort_CountAttempts = (int)Configuration.ReadParameter("WriteBlocksBestEffortCountAttempts", "int", "3");
                nWriteBlocksBestEffort_GapBetweenAttemptsInMilliSec = (int)Configuration.ReadParameter("WriteBlocksBestEffort_GapBetweenAttemptsInMilliSec", "int", "40");
            }

            static public readonly int 
                nReadBlocksBestEffort_CountAttempts, 
                nWriteBlocksBestEffort_CountAttempts,
                nReadBlocksBestEffort_GapBetweenAttemptsInMilliSec,
                nWriteBlocksBestEffort_GapBetweenAttemptsInMilliSec;
        }
        
        /// <summary>
        /// Function to Write Data Blocks to Ultralight,
        /// to be used in Conjuntion with the data recieved from
        /// GetCmdBuffer with supplied data to be written on Token
        /// </summary>
        /// <param name="pReaderType"></param>
        /// <param name="phRw"></param>
        /// <param name="pCmdBuffer"></param>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        static public CSC_API_ERROR WriteBlocks(CSC_READER_TYPE pReaderType,
                                                int phRw,
                                                byte[] pCmdBuffer,
                                                out byte pSw1,
                                                out byte pSw2,
            out bool bSuccess)
        {
            CSC_API_ERROR Err = CSC_API_ERROR.ERR_API;
            pSw1 = 0xFF;
            pSw2 = 0xFF;
            
            bSuccess = false;
#if _BLUEBIRD_
#else
            byte[] pResData;            
            
            Err = Reader.IsoCommand(pReaderType,
                                         phRw,
                                         DEST_TYPE.DEST_CARD,
                                         pCmdBuffer,
                                         3,
                                         out pSw1,
                                         out pSw2,
                                         out pResData);
            if (Err != CSC_API_ERROR.ERR_NONE)
                return Err;
            else
            {
                if (pResData != null && pResData.Length == 1)
                {
                    if (pResData[0] == 0) // everything is written
                    {
                        bSuccess = true;
                        return Err;
                    }
                    else
                    {
//#if false
//                        System.Windows.Forms.MessageBox.Show("This token needs special attention ", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning ); 
//#endif
                    }
                }
            }            
#endif
            return Err;
        }        

        /// <summary>
        /// This Functions loads the Static Data of a Token to Logical Media
        /// </summary>
        /// <param name="pLogicalMedia"></param>
        /// <returns>bool</returns>
        static public bool LoadStaticDataForIssue(LogicalMedia pLogicalMedia, short productType)
        {
            Media m = pLogicalMedia.Media;

            Initialisation ini = pLogicalMedia.Initialisation;
            TransportApplication ta = pLogicalMedia.Application.TransportApplication;
            LocalLastAddValue lcav = pLogicalMedia.Application.LocalLastAddValue;
            Customer cu = pLogicalMedia.Application.Customer;
            Validation val = pLogicalMedia.Application.Validation;

            Products ps = pLogicalMedia.Application.Products;

            //Initial Data
            m.HardwareTypeRead = Media.HardwareTypeValues.TokenUltralight;
            m.ChipTypeRead = Media.ChipTypeValues.UltralightC;
            m.TypeRead = Media.TypeValues.Token;

            //Physical Token type
            m.DesignTypeRead = 0; // TO be Confirmed

            //Service Provider ID
            ta.OwnerRead = SharedData.ServiceProvider; //Default for DMRC
            ini.ServiceProviderRead = ta.Owner;
            m.OwnerRead = ta.Owner;

            //Logical Token Type
            ps.Product(0).TypeRead = productType;

            //Token Status
            ta.StatusRead = (TransportApplication.StatusValues)(2);
            
            val.EntryExitBitRead = Validation.TypeValues.Exit;

            //Reject Code
            val.RejectCodeRead = 0;            
            val.AgentRemainingTrips = 0;

            //Test Flag
            m.TestRead = false; // TODO: See if it is obeyed by gate, then don't take its value as default. In fact, then application should created Mac for both commercial and test tokens

            return true;
        }

        static public bool VerifyMAC(LogicalMedia pLogicalMedia)
        {
            try
            {
                byte[] MacDataBuf = new byte[2 * CONSTANT.MIFARE_ULTRALT_BLOC_SIZE - 8];
                Array.Copy(pLogicalMedia._tokenPhysicalData, MacDataBuf, 2 * CONSTANT.MIFARE_ULTRALT_BLOC_SIZE - 8);
                byte[] macbytesCalculated = SecurityMgr.Instance.GenerateMAC(MacDataBuf);
                byte[] macbytesPresent = new byte[8];
                Array.Copy(pLogicalMedia._tokenPhysicalData, 24, macbytesPresent, 0, 8);

                return macbytesCalculated.SequenceEqual(macbytesPresent);
            }
            catch (Exception exp)
            {
                Logging.Log(LogLevel.Error, "Unexpected " + exp.ToString());
                return false;
            }
        }

        /// <summary>
        /// This Functions Converts the Logical media to DataBlocks for Ultralight
        /// with Mac generated and included in the Blocks for Integrity checks in
        /// Other and Same Equipment
        /// It is Highly Dependent on the Delhi UltraLight Layout
        /// pBlock0 is the Data, currently recieved from Token Dispenser
        /// for mac calculation
        /// </summary>
        /// <param name="pLogicalMedia"></param>
        /// <param name="pBlock0"></param>
        /// <returns>Data Blocks[48]</returns>
        static public byte[] GetDataBlocksVer0(LogicalMedia logMediaRef,             
                                           byte[] pBlock0, out ulong mac)
        {
            //RespBlocs will be [1, 2, 3 of 16 bytes each]
            byte[] pResBlocs = new byte[48];

            //Block 1 data, Excluding the mac bits (16 - 8) = 8
            byte[] Block1Buf = new byte[CONSTANT.MIFARE_ULTRALT_BLOC_SIZE - 8];

            //MacDataBuffer : Block_0 data (16 bytes) + Block_1 (8 bytes)
            byte[] MacDataBuf = new byte[CONSTANT.MIFARE_ULTRALT_BLOC_SIZE + Block1Buf.Length];

            mac = 0;

            Media m = logMediaRef.Media;

            //Initialisation ini = pLogicalMedia.Initialisation;
            TransportApplication ta = logMediaRef.Application.TransportApplication;
            LocalLastAddValue lcav = logMediaRef.Application.LocalLastAddValue;
            Customer cu = logMediaRef.Application.Customer;
            Validation val = logMediaRef.Application.Validation;
            //#if !_HHD_
            TTag ttag = logMediaRef.TTag;
//#endif
            Products ps = logMediaRef.Application.Products;

            if (m.ChipSerialNumber != 0)
            {
                int j;

                var bitBuffer = new bool[CONSTANT.MIFARE_ULTRALT_BLOC_BITS * 3];

                /////////////////////////* BLOCK 1 */////////////////////////

                //Initialization Date
                int i = 0;
                {
                    ushort dt, tim;
                    ExtractTimeInDosFormat(//m.InitialisationDate, It has to be incorrect, i.e. wiped with every sale, to cover up a pre-condition for cs22 tom.
                        lcav.DateTime,
                        out dt, out tim);

                    i = CFunctions.ConvertToBits(dt, i, 16, bitBuffer);
                }

                //Date Of Sale
                {
                    ushort dt, tim;
                    ExtractTimeInDosFormat(DatesUtility.BusinessDay(lcav.DateTime, new DateTime(2010, 1, 1, 2, 0, 0)), out dt, out tim);
                    i = CFunctions.ConvertToBits(dt, i, 16, bitBuffer);
                }

                //Physical Token Type
                i = CFunctions.ConvertToBits((ulong)m.DesignType, i, 8, bitBuffer);

                //Language Bit
                switch (cu.Language)
                {
                    default:
                        j = (int)cu.Language;
                        break;
                }

                i = CFunctions.ConvertToBits((ulong)j, i, 1, bitBuffer);

                //Service Provider Id
                i = CFunctions.ConvertToBits((ulong)lcav.ServiceProvider, i, 4, bitBuffer);

                //FareTier
                i = CFunctions.ConvertToBits((ulong)lcav.FareTiers, i, 6, bitBuffer);

                //KeyVersion
                i = CFunctions.ConvertToBits((ulong)SecurityMgr.Instance.GetTokenActiveKeyVer(), i, 1, bitBuffer);

                //Sale Station Code
                i = CFunctions.ConvertToBits((ulong)lcav.Location, i, 8, bitBuffer);

                //Spare
                i = CFunctions.ConvertToBits(0, i, 4, bitBuffer);

                //Calculate Mac and Add to Buffer
                Block1Buf = CFunctions.ConvertBoolTableToBytes(bitBuffer, 8 * 8);

                // Mac Calculation begins -------------------------------------------
                // copy token serial number
                Array.Copy(pBlock0, 0, MacDataBuf, 0,
                    16
                    );

                //Copy Block1 buffer
                Array.Copy(Block1Buf, 0, MacDataBuf, 16, Block1Buf.Length);
                
                byte[] macbytes = SecurityMgr.Instance.GenerateMAC(MacDataBuf);

                mac = CFunctions.ConvertFromByteLE(macbytes);

                i = CFunctions.ConvertToBits(mac, i, 64, bitBuffer);
                // Mac Calculation Ends -------------------------------------------

                /////////////////////////* BLOCK 2 *////////////////////////////////////

                //Logical Token Type
                i = CFunctions.ConvertToBits((ulong)ps.Product(0).Type, i, 8, bitBuffer);

                //Token Status
                i = CFunctions.ConvertToBits((ulong)ta.Status, i, 8, bitBuffer);

                //Sequence Number
                var seqNumVTD1 = ExtractSeqNumVTD1(pBlock0);
                var seqNumVTD2 = ExtractSeqNumVTD2(pBlock0);

                var seqNumNew = ((seqNumVTD1 > seqNumVTD2) ? seqNumVTD1 + 1 : seqNumVTD2 + 1);
                m.SequenceNumberRead = (long)seqNumNew;

                i = CFunctions.ConvertToBits(seqNumNew, i, 18, bitBuffer);

                //TO REMOVE, Testing only
                int eqr = SharedData.EquipmentNumber;

                //Sale Equipment Id
                i = CFunctions.ConvertToBits((ulong)eqr, i, 24, bitBuffer);

                //Entry/Exit Stn Code
                i = CFunctions.ConvertToBits((byte)val.Location, i, 8, bitBuffer);

                ushort lastTxnDosDate, lastTxnDosTime;
                CommonFunctions.CFunctions.GetDosDateTime(val.LastTransactionDateTime.ToUniversalTime(), true, out lastTxnDosDate, out lastTxnDosTime);

                //Transaction Time
                i = CFunctions.ConvertToBits(lastTxnDosTime, i, 16, bitBuffer);

                //Destination Station Code
                i = CFunctions.ConvertToBits(0, i, 8, bitBuffer);

                //Transaction Date
                i = CFunctions.ConvertToBits(lastTxnDosDate, i, 16, bitBuffer);

                //Don't get mislead by document that this field is used for rjt. IT IS ALSO USED BY SJT, AND MUST BE 1 WHEN ISSUED
                i = CFunctions.ConvertToBits(1, i, 2, bitBuffer);

                //Reject Code
                i = CFunctions.ConvertToBits((ulong)val.RejectCode, i, 8, bitBuffer);

                //Entry Exit bit
                i = CFunctions.ConvertToBits(val.EntryExitBit == Validation.TypeValues.Entry ? CONSTANT.MBC_GateEntry : CONSTANT.MBC_GateExit, i, 1, bitBuffer);

                //Test Flag
                i = CFunctions.ConvertToBits((ulong)Convert.ToInt32(m.Test), i, 1, bitBuffer);

                //Token Amount
                if (lcav.Amount/10 > Math.Pow(2, 10) - 1)
                    i = CFunctions.ConvertToBits(0, i, 10, bitBuffer);
                else
                    i = CFunctions.ConvertToBits((ulong)(lcav.Amount / 10), i, 10, bitBuffer);


                ///////////////* BLOCK 3 : Replicaton of BLOCK 2 *//////////////////////

                //Logical Token Type
                i = CFunctions.ConvertToBits((ulong)ps.Product(0).Type, i, 8, bitBuffer);

                //Token Status
                i = CFunctions.ConvertToBits((ulong)ta.Status, i, 8, bitBuffer);

                //Sequence Number
                i = CFunctions.ConvertToBits(seqNumNew, i, 18, bitBuffer);

                //Sale Equipment Id
                i = CFunctions.ConvertToBits((ulong)lcav.EquipmentNumber, i, 24, bitBuffer);

                //Entry/Exit Stn Code
                i = CFunctions.ConvertToBits((byte)val.Location, i, 8, bitBuffer);

                //Transaction Time
                i = CFunctions.ConvertToBits(lastTxnDosTime, i, 16, bitBuffer);

                //(ulong)lcav.Destination
                //Destination Station Code
                i = CFunctions.ConvertToBits(0, i, 8, bitBuffer);

                //Transaction Date                
                i = CFunctions.ConvertToBits(lastTxnDosDate, i, 16, bitBuffer);

                //Don't get mislead by document that this field is used for rjt. IT IS ALSO USED BY SJT, AND MUST BE 1 WHEN ISSUED
                i = CFunctions.ConvertToBits(1, i, 2, bitBuffer);

                //Reject Code
                i = CFunctions.ConvertToBits((ulong)val.RejectCode, i, 8, bitBuffer);

                //Entry Exit bit
                i = CFunctions.ConvertToBits(val.EntryExitBit == Validation.TypeValues.Entry ? CONSTANT.MBC_GateEntry : CONSTANT.MBC_GateExit, i, 1, bitBuffer);

                //Test Flag
                i = CFunctions.ConvertToBits((ulong)Convert.ToInt32(m.Test), i, 1, bitBuffer);
                
                //Token Amount
                if (lcav.Amount / 10 > Math.Pow(2, 10) - 1)
                    i = CFunctions.ConvertToBits(0, i, 10, bitBuffer);
                else
                    i = CFunctions.ConvertToBits((ulong)(lcav.Amount / 10), i, 10, bitBuffer);

                pResBlocs = CFunctions.ConvertBoolTableToBytes(bitBuffer, 48 * 8);

                return pResBlocs;
            }

            return pResBlocs;
        }

        static public byte[] GetDataBlocksVer1(LogicalMedia logMediaRef,
                                   byte[] pBlock0, out ulong mac)
        {
            //RespBlocs will be [1, 2, 3 of 16 bytes each]
            byte[] pResBlocs = new byte[48];

            //Block 1 data, Excluding the mac bits (16 - 8) = 8
            byte[] Block1Buf = new byte[CONSTANT.MIFARE_ULTRALT_BLOC_SIZE - 8];

            //MacDataBuffer : Block_0 data (16 bytes) + Block_1 (8 bytes)
            byte[] MacDataBuf = new byte[CONSTANT.MIFARE_ULTRALT_BLOC_SIZE + Block1Buf.Length];

            mac = 0;

            Media m = logMediaRef.Media;

            //Initialisation ini = pLogicalMedia.Initialisation;
            TransportApplication ta = logMediaRef.Application.TransportApplication;
            LocalLastAddValue lcav = logMediaRef.Application.LocalLastAddValue;
            Customer cu = logMediaRef.Application.Customer;
            Validation val = logMediaRef.Application.Validation;
            //#if !_HHD_
            TTag ttag = logMediaRef.TTag;
            //#endif
            Products ps = logMediaRef.Application.Products;

            if (m.ChipSerialNumber != 0)
            {
                int j;

                var bitBuffer = new bool[CONSTANT.MIFARE_ULTRALT_BLOC_BITS * 3];

                /////////////////////////* BLOCK 1 */////////////////////////

                //Initialization Date
                int i = 0;
                {
                    ushort dt, tim;
                    ExtractTimeInDosFormat(//m.InitialisationDate, It has to be incorrect, i.e. wiped with every sale, to cover up a pre-condition for cs22 tom.
                        lcav.DateTime,
                        out dt, out tim);

                    i = CFunctions.ConvertToBits(dt, i, 16, bitBuffer);
                }

                //Date Of Sale
                {
                    ushort dt, tim;
                    ExtractTimeInDosFormat(DatesUtility.BusinessDay(lcav.DateTime, new DateTime(2010, 1, 1, 2, 0, 0)), out dt, out tim);
                    i = CFunctions.ConvertToBits(dt, i, 16, bitBuffer);
                }

                //Physical Token Type
                i = CFunctions.ConvertToBits((ulong)m.DesignType, i, 8, bitBuffer);

                //Language Bit
                switch (cu.Language)
                {
                    default:
                        j = (int)cu.Language;
                        break;
                }

                i = CFunctions.ConvertToBits((ulong)j, i, 1, bitBuffer);

                //Service Provider Id
                i = CFunctions.ConvertToBits((ulong)lcav.ServiceProvider, i, 4, bitBuffer);

                //FareTier
                i = CFunctions.ConvertToBits((ulong)lcav.FareTiers, i, 6, bitBuffer);

                //KeyVersion
                i = CFunctions.ConvertToBits((ulong)SecurityMgr.Instance.GetTokenActiveKeyVer(), i, 1, bitBuffer);

                var bufferStationCode = new bool[10];
                //Sale Station Code
                CFunctions.ConvertToBits((ulong)lcav.Location, 0, 10, bufferStationCode);
                Array.Copy(bufferStationCode, 0, bitBuffer, i, 8); i += 8; // 8-bits 

                int Version = 1;
                i = CFunctions.ConvertToBits((ulong)Version, i, 2, bitBuffer);

                Array.Copy(bufferStationCode, 8, bitBuffer, i, 2); i += 2;

                //Calculate Mac and Add to Buffer
                Block1Buf = CFunctions.ConvertBoolTableToBytes(bitBuffer, 8 * 8);

                // Mac Calculation begins -------------------------------------------
                // copy token serial number
                Array.Copy(pBlock0, 0, MacDataBuf, 0,
                    16
                    );

                //Copy Block1 buffer
                Array.Copy(Block1Buf, 0, MacDataBuf, 16, Block1Buf.Length);

                byte[] macbytes = SecurityMgr.Instance.GenerateMAC(MacDataBuf);

                mac = CFunctions.ConvertFromByteLE(macbytes);

                i = CFunctions.ConvertToBits(mac, i, 64, bitBuffer);
                // Mac Calculation Ends -------------------------------------------

                /////////////////////////* BLOCK 2 *////////////////////////////////////

                //Logical Token Type
                i = CFunctions.ConvertToBits((ulong)ps.Product(0).Type, i, 4, bitBuffer);

                //Token Status
                i = CFunctions.ConvertToBits((ulong)ta.Status, i, 4, bitBuffer);

                //Sequence Number
                var seqNumVTD1 = ExtractSeqNumVTD1(pBlock0);
                var seqNumVTD2 = ExtractSeqNumVTD2(pBlock0);

                var seqNumNew = ((seqNumVTD1 > seqNumVTD2) ? seqNumVTD1 + 1 : seqNumVTD2 + 1);
                m.SequenceNumberRead = (long)seqNumNew;

                i = CFunctions.ConvertToBits(seqNumNew, i, 18, bitBuffer);

                //Sale Equipment Id
                i = CFunctions.ConvertToBits((ulong)SharedData.EquipmentNumber, i, 24, bitBuffer);

                //Entry/Exit Stn Code
                i = CFunctions.ConvertToBits((byte)val.Location, i, 10, bitBuffer);

                ushort lastTxnDosDate, lastTxnDosTime;
                CommonFunctions.CFunctions.GetDosDateTime(val.LastTransactionDateTime.ToUniversalTime(), true, out lastTxnDosDate, out lastTxnDosTime);

                //Transaction Time
                i = CFunctions.ConvertToBits(lastTxnDosTime, i, 16, bitBuffer);

                //Destination Station Code
                i = CFunctions.ConvertToBits(0, i, 10, bitBuffer);

                //Transaction Date
                i = CFunctions.ConvertToBits(lastTxnDosDate, i, 16, bitBuffer);

                //Don't get mislead by document that this field is used for rjt. IT IS ALSO USED BY SJT, AND MUST BE 1 WHEN ISSUED
                i = CFunctions.ConvertToBits(1, i, 2, bitBuffer);

                //Reject Code
                i = CFunctions.ConvertToBits((ulong)val.RejectCode, i, 8, bitBuffer);

                //Entry Exit bit
                i = CFunctions.ConvertToBits(val.EntryExitBit == Validation.TypeValues.Entry ? CONSTANT.MBC_GateEntry : CONSTANT.MBC_GateExit, i, 1, bitBuffer);

                //Test Flag
                i = CFunctions.ConvertToBits((ulong)Convert.ToInt32(m.Test), i, 1, bitBuffer);

                //Token Amount
                i = CFunctions.ConvertToBits((ulong)(lcav.Amount / 10), i, 12, bitBuffer);


                ///////////////* BLOCK 3 : Replicaton of BLOCK 2 *//////////////////////

                //Logical Token Type
                i = CFunctions.ConvertToBits((ulong)ps.Product(0).Type, i, 4, bitBuffer);

                //Token Status
                i = CFunctions.ConvertToBits((ulong)ta.Status, i, 4, bitBuffer);

                //Sequence Number
                i = CFunctions.ConvertToBits(seqNumNew, i, 18, bitBuffer);

                //Sale Equipment Id
                i = CFunctions.ConvertToBits((ulong)lcav.EquipmentNumber, i, 24, bitBuffer);

                //Entry/Exit Stn Code
                i = CFunctions.ConvertToBits((byte)val.Location, i, 10, bitBuffer);

                //Transaction Time
                i = CFunctions.ConvertToBits(lastTxnDosTime, i, 16, bitBuffer);

                //(ulong)lcav.Destination
                //Destination Station Code
                i = CFunctions.ConvertToBits(0, i, 10, bitBuffer);

                //Transaction Date                
                i = CFunctions.ConvertToBits(lastTxnDosDate, i, 16, bitBuffer);

                //Don't get mislead by document that this field is used for rjt. IT IS ALSO USED BY SJT, AND MUST BE 1 WHEN ISSUED
                i = CFunctions.ConvertToBits(1, i, 2, bitBuffer);

                //Reject Code
                i = CFunctions.ConvertToBits((ulong)val.RejectCode, i, 8, bitBuffer);

                //Entry Exit bit
                i = CFunctions.ConvertToBits(val.EntryExitBit == Validation.TypeValues.Entry ? CONSTANT.MBC_GateEntry : CONSTANT.MBC_GateExit, i, 1, bitBuffer);

                //Test Flag
                i = CFunctions.ConvertToBits((ulong)Convert.ToInt32(m.Test), i, 1, bitBuffer);

                //Token Amount
                i = CFunctions.ConvertToBits((ulong)(lcav.Amount / 10), i, 12, bitBuffer);

                pResBlocs = CFunctions.ConvertBoolTableToBytes(bitBuffer, 48 * 8);

                return pResBlocs;
            }

            return pResBlocs;
        }

        static public byte[] GetDataBlocks(int version, LogicalMedia logMediaRef,
                                   byte[] pBlock0, out ulong mac)
        {                
            if (version == 1)
                return GetDataBlocksVer1(logMediaRef, pBlock0, out mac);
            else
                return GetDataBlocksVer0(logMediaRef, pBlock0, out mac);
        }


        private static void ExtractTimeInDosFormat(DateTime dt, out ushort lcavDosDate, out ushort lcavDosTime)
        {
//            DateTime utctime = TimeZoneInfo.ConvertTimeToUtc(dt);

            lcavDosDate = CFunctions.ToDosDate(dt);
            lcavDosTime = CFunctions.ToDosTime(dt);
        }

        static private int ExtractVersion(byte[] pBlock0)
        {
            return (int)CFunctions.GetBitData(188, 2, pBlock0);
        }

        static private ulong ExtractSeqNumVTD1(byte[] pBlock0)
        {
            int version = ExtractVersion(pBlock0);
            if (version != 1)
                return CFunctions.GetBitData(272, 18, pBlock0);
            else
                return CFunctions.GetBitData(264, 18, pBlock0);
        }

        static private ulong ExtractSeqNumVTD2(byte[] pBlock0)
        {
            int version = ExtractVersion(pBlock0);
            if (version != 1)
                return CFunctions.GetBitData(400, 18, pBlock0);
            else
                return CFunctions.GetBitData(392, 18, pBlock0);
        }

        static public byte[] GetDataBlocksForTTag(TTag ct)
        {
            // TODO: Perform Mac calculation
            byte[] pResBlocs = new byte[48];

            var bitBuffer = new bool[CONSTANT.MIFARE_ULTRALT_BLOC_BITS * 3];

            int i;
            {
                ushort dosDate, dosTime;
                CFunctions.GetDosDateTime(ct.IssueDate, false, out dosDate, out dosTime);
                CFunctions.ConvertToBits(dosDate, 0, 16, bitBuffer);
            }

            i = CFunctions.ConvertToBits(CONSTANT.TICKET_TYPE_TTAG, 32, 8, bitBuffer);

            // 2nd block (area occupied by VTD 1)
            i = CONSTANT.MIFARE_ULTRALT_BLOC_BITS;
            i = CFunctions.ConvertToBits((ulong)ct.CountTokens, i, 16, bitBuffer);
            i = CFunctions.ConvertToBits((ulong)ct.EquipmentNumber, i, 32, bitBuffer);

            {
                ushort dosDate, dosTime;
                CFunctions.GetDosDateTime(ct.TimeLastWritten, false, out dosDate, out dosTime);

                i = CFunctions.ConvertToBits(dosDate, i, 16, bitBuffer);
                i = CFunctions.ConvertToBits(dosTime, i, 16, bitBuffer);
            }
            i = CFunctions.ConvertToBits((ulong)ct.SerialNumber, i, 32, bitBuffer);
            i = CFunctions.ConvertToBits((byte)ct.LastOperation, i, 4, bitBuffer);            

            return CFunctions.ConvertBoolTableToBytes(bitBuffer, 48 * 8);            
        }
//#endif
        /// <summary>
        /// This Function returns the Command Buffer with, 
        /// data to be written on the Token
        /// </summary>
        /// <param name="pNbrOfBlocks"></param>
        /// <param name="pDataIn"></param>
        /// <returns></returns>
        static public byte[] GetWriteCmdBuffer(byte[] pDataIn)
        {
            int NbrOfBlocks = pDataIn.Length / 16;

            //Convert the number blocks into pages
            int NbrOfPages = NbrOfBlocks * 4;

            byte[] CmdBuffer = new byte[NbrOfPages + pDataIn.Count()];

            for (int i = 0; i < NbrOfPages; i++)
            {
                //Considering we always Write from block 1
                //Page number is always set from 4
                CmdBuffer[i] = (byte)(i + 4);
            }

            Array.Copy(pDataIn, 0, CmdBuffer, NbrOfPages, pDataIn.Length);

            byte[] WriteCmdBuffer = CFunctions.getApdu(CONSTANT.MIFARE_ULTRALT_CLA, CONSTANT.MIFARE_WRIT_INS, CONSTANT.NULL, (byte)NbrOfPages, CmdBuffer);

            //System.IO.StreamWriter file = new System.IO.StreamWriter("c:\\tokenbytes.txt");            

            string s = "";
            for (int i = 0; i < WriteCmdBuffer.Length; i++) s += ((WriteCmdBuffer[i]).ToString("X2") + ' ');
            //{
            //    file.WriteLine(WriteCmdBuffer[i]);

            //}

            //file.Close();
            Logging.Log(LogLevel.Verbose, "Token Buffer : " + s);

            return WriteCmdBuffer;
        }
    }
}
