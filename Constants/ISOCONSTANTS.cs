using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
     public static class ISOCONSTANTS
    {
       
         public const int MIN_ISO_DATA_IN_LENGTH = 4;       /* CLA INS P1 P2 */
         public const int MAX_ISO_DATA_IN_LENGTH = 245;     /* 255 - CMD DEST */        
         public const int MIN_ISO_DATA_OUT_LENGTH = 2;      /* SW1 SW2 */
         public const int MAX_ISO_DATA_OUT_LENGTH = 252;    /* 255 - CMD RET0 RET1 */    
         public const int LX_ISO_IN_LENGTH = 1;             /* LC OR LE Required */

         public const byte DEFIRE_RESPONSE_SW1 = 0x91; //SKS added on 20160509

         public const byte DESFIRE_CLA = 0x90;

        //Authenticate 
         public const byte DESFIRE_AUTH_INS = 0x0A;

        //send more data succeeding to an command
         public const byte DESFIRE_MOREDATA_INS = 0xAF;

        //ChangeKeySettings
         public const byte DESFIRE_CHGKEYSET_INS = 0x54;

        //GEt KeySettings
         public const byte DESFIRE_GETKEYSET_INS = 0x45;

        //ChangeKey
         public const byte DESFIRE_CHGKEY_INS = 0xC4;

        //GETKeyVerion
         public const byte DESFIRE_GETKEYVER_INS = 0x64;

        //Create Application
         public const byte DESFIRE_CREATAPP_INS = 0xCA;
 
        //Delete Application
         public const byte DESFIRE_DELAPP_INS = 0xDA;

        //Get APPlication IDs
         public const byte DESFIRE_GETAPPIDS_INS = 0x6A;

        //Selecte Aplication
         public const byte DESFIRE_SELA_INS = 0x5A;

        //FORMAT PICC
         public const byte DESFIRE_FORMAT_INS = 0xFC;

        //Get Verion (manufacturer related data
         public const byte DESFIRE_GETVER_INS = 0x60;

        //GET file Ids
         public const byte DESFIRE_GETFILEIDS_INS = 0x6F;

        //Get File Settings
         public const byte DESFIRE_GETFILESETT_INS = 0xF5;

        //change file setting
         public const byte DESFIRE_CHGFILESETT_INS = 0x5F;

        //Create standard data file
         public const byte DESFIRE_CREAT_STDFILE_INS = 0xCD;

        //Create backup data file
         public const byte DESFIRE_CREAT_BCKUPFILE_INS = 0xCB;

        //CreateValue File
         public const byte DESFIRE_CREAT_VALFILE_INS = 0xCC;

        //Create linear Record file
         public const byte DESFIRE_CREAT_LINEARFILE_INS = 0xC1;

        //Create Cyclic Record file
         public const byte DESFIRE_CREAT_CYCRECFILE_INS = 0xC0;

        //Delete file
         public const byte DESFIRE_DELETE_FILE_INS = 0xDF;

        //READ from standard/backup data file
         public const byte DESFIRE_READ_DATAFILE_INS = 0xBD;

        //write into standard/backup data file
         public const byte DESFIRE_WRITE_DATAFILE_INS = 0x3D;

        //Get Value - reads corrent stored value form value files
         public const byte DESFIRE_GETVAL_INS = 0x6C;

        //Credit: Credit command allows to increase a value stored in a Value File.
         public const byte DESFIRE_CREDIT_INS = 0x0C;

        //Debit command allows to decrease a value stored in a Value File
         public const byte DESFIRE_DEBIT_INS = 0xDC;

        //LimitedCredit command allows a limited increase of a value stored in a Value File without 
        //having full Read&Write permissions
         public const byte DESFIRE_LIMITEDCR_INS = 0x1C;

        //WriteRecord command allows to write data to a record in a Cyclic or Linear Record File.
         public const byte DESFIRE_WRITE_RECFILE_INS = 0x3B;

        //ReadRecords command allows to read out a set of complete records from a
        //Cyclic or Linear Record File.
         public const byte DESFIRE_READ_RECFILE_INS = 0xBB;

        //ClearRecordFile command allows to reset a Cyclic or Linear Record File to the empty state
         public const byte DESFIRE_CLEAR_RECFILE_INS = 0xEB;
 
        //CommitTransaction command allows to validate all previous write access on Backup Data Files, Value
        //Files and Record Files within one application
         public const byte DESFIRE_COMMIT_TXN_INS = 0xC7;

        //AbortTransaction command
         public const byte DESFIRE_ABORT_TXN_INS = 0xA7;
    }
}
