using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFS2.Equipment.TicketingRules
{
    public static class CONSTANT
    {
        public const int NO_ERROR = 0;
        public const int IS_ERROR =-1;

        // API Version Check CONSTANT
        public const int API_EX_MAJOR_VERSION = 3;
        public const int API_EX_MINOR_VERSION = 11;

        // MAXIMUM ATR SIZE
        public const int MAX_ATR_SIZE = 128;

        // MAX_SAM_OPTION_STRING_LEN
        public const int MAX_SAM_OPTION_STRING_LEN = 40;

        // MAX_DATA_INST_CARD
        public const int MAX_DATA_INST_CARD = 10;

        /*-------------------------------------*/
        /* Ultralight Keys Specific Constants  */
        /*-------------------------------------*/
        public const int MAX_ULTRA_KEY_VALUE = 8;

        public const int MAX_ULTRA_KEY_VERSION = 2;

        /*----------------------------------*/
        /* Desfire Keys Specific Constants  */
        /*----------------------------------*/
        public const int MAX_DESF_KEYS_LOAD = 48;

        public const int MAX_DESF_KEYS_SETS = 2;

        public const int MAX_DESF_KEYS_NBRS = 16;

        public const int MAX_DESF_KEYS_DATA = 16;

        // LABEL LENGTH
        public const int LABEL_LENGTH = 32;   

        // Status of CSC Reader
        public const byte ST_VIRGIN   = 0x01;        // Virgin state
        public const byte ST_INIT     = 0x02;        // Initialize state
        public const byte ST_POLLON   = 0x04;        // Actived Polling state
        public const byte ST_DISCR    = 0x08;        // Discrimination state
        public const byte ST_CARDON   = 0x10;        // Detected card state
        public const byte ST_DETECT_REMOVAL = 0x20;        // Card removal detection

        // SAM PROTOCOL TYPE (member ucProtocolType)
        public const byte SAM_PROTOCOL_T0        = 0x01;     // Basic T=0 protocol
        public const byte SAM_PROTOCOL_HSP       = 0x02;     // HSP protocol
        public const byte SAM_PROTOCOL_T1        = 0x03;     // T=1 protocol
        public const byte SAM_PROTOCOL_T0_EXT    = 0x04;     // Extended T=0 protocol
        public const byte SAM_PROTOCOL_T1_EXT    = 0x05;     // Extended T=1 protocol

        // Antenna type
        public const int SMART_NONE       =  0;
        public const int SMART_ANTENNA_1  =  1;
        public const int SMART_ANTENNA_2  =  2;

        /* RF field modifier */
        public const byte SMART_RF_POWER_ON    =   0x20;
        public const byte SMART_RF_POWER_OFF   =   0x00;

         public const byte FIELD_ON   =     1;
         public const byte FIELD_OFF = 0;



        public const byte DETECTION_WITHOUT_EVENT = 0x00;       /* without call back */
        public const byte DETECTION_WITH_EVENT    = 0x01;       /* with call back */
        
        public const byte NULL = 0x00;

        public const byte CSC_CLA = 0x90;
        public const byte CSC_INS = 0x04;
        //General use SW1 codes
        public const byte COMMAND_SUCCESS = 0x90;
        public const byte RESPONSE_OK = 0x61;

        /*----------------------------------*/
        /* CrytoFlex General Use Constants  */
        /*----------------------------------*/
        public const byte CFLEX_CLA = 0xC0;

        public const byte CFLEX_AUTH_INS = 0x20;
        public const byte CFLEX_SELC_INS = 0xA4;
        public const byte CFLEX_READ_INS = 0xB0;

        public const byte CFLEX_CHALG_INS = 0x84;
        public const byte CFLEX_AUTHE_INS = 0x82;
        public const byte CFLEX_AUTHI_INS = 0x88;

        public const byte CFLEX_GETR_INS = 0xC0;

        public const byte CFLEX_KEYV_NUM = 0x01;

        public const int CFLEX_FDES_CRYPTO = 128;

        /*----------------------------------*/
        /* CrytoFlex Security Use Constants */
        /*----------------------------------*/
        public const byte CFLEX_SEC_CLA = 0xF0;

        public const byte CFLEX_DESI_INS = 0x56;

        public const byte CFLEX_DKEY_NUM = 0x04;

        //Root diretory number
        public const byte CFLEX_RDIR_NUM_B1 = 0x3F;
        public const byte CFLEX_RDIR_NUM_B2 = 0x00;

        //Security diretory number
        public const byte CFLEX_SDIR_NUM_B1 = 0x4F;
        public const byte CFLEX_SDIR_NUM_B2 = 0x00;

        //Local Info file number 
        public const byte CFLEX_LINF_NUM_B1 = 0x20;
        public const byte CFLEX_LINF_NUM_B2 = 0x00;

        //Local Certificate file number 
        public const byte CFLEX_LCAF_NUM_B1 = 0x20;
        public const byte CFLEX_LCAF_NUM_B2 = 0x03;

        //CA Certificatefile number
        public const byte CFLEX_CCAF_NUM_B1 = 0x20;
        public const byte CFLEX_CCAF_NUM_B2 = 0x04;  

        //Certificate File size
        public const int CFLEX_CERT_SIZE = 608;

        //Local File size
        public const int CFLEX_LFIL_SIZE = 4;

        //Serial Number Size
        public const int CFLEX_SNBR_SIZE = 8;

        //Challenge String Length
        public const int CFLEX_CHALL_SIZE = 8;

        //Private Key Number
        public const int CFLEX_PRIVATE_KEY_NUM = 0x12;

        //#if _CCHS_SAM
        //////// CCHS SAM Constants //////////////////
        //public const byte ISAM_MAX_BUFFER_RESPONSE_SIZE = 256;
        public const byte ISAM_MAX_SIZE_FOR_TAC = 244;
        public const byte CardLayoutMax=16;

        public enum SAMType
        {
            NONE=0x00,
            NXP_SAM_AV1 = 0x01,
            NXP_SAM_AV2=0x02,
            THALES_SAM='A', // for thales crypto SAM
            ISAM = 0x49, //'I' // CCHS I-SAM
            PSAM = 0x50, //'P' //CCHS P-SAM
            MSAM = 'M' // CCHS M-SAM            

        };

        public enum ReaderType
        {
            NONE = 0x00,
            BIP_1300 = 0x01,
            BIP_1500 = 0x02,
            THALES_V3 = 0x03,
            THALES_V4 = 0x04
           
        };
       
        public  enum SAMErrors
        {
            SM_OK = 0,
            SM_INIT_ERROR,// = SM_OK + 1,
            SM_CONFIG_ERROR,// = SM_INIT_ERROR + 1,
            SM_SAMLOCKED_ERROR = 0x03,
            SM_SAMTYPE_ERROR = 0x04,           
            SM_PARAM_ERROR,// = SM_CONFIG_ERROR + 1,
            SM_FAILURE,// = SM_PARAM_ERROR + 1,
            SM_AUTHENTICATION_FAILURE=0x07,// SM_PIN_ERROR = 0x07,,
            SM_LINK_FAILURE,// = SM_AUTHENTICATION_FAILURE + 1,
            SM_MAX_ERRORS,// = SM_LINK_FAILURE + 1
            ISO7816_SW_INCORRECT_P1P2=0x86,
            ISO7816_SW_Lc_INCONSISTENT_WITH_P1P2=0x87
           // ISO7816_SW_COUNTER_PROVIDED_BY_X= 0xC
        };

        ////////////////// CCHS SAM COMMANDS  ////////////////////////

        public const byte ISAM_CLA = 0xD0;
        public const byte ISAM_INS_SEL_APPL = 0xA4;
        public const byte ISAM_INS_ACTIVATION = 0x03;
        public const byte ISAM_INS_GETSAM_STATUS = 0x05;
        public const byte ISAM_INS_GETSAM_SEQUENCE = 0x07;
        public const byte ISAM_INS_WRITESAM_SEQUENCE = 0x08;
        public const byte ISAM_INS_GENERATE_TAC = 0x10;
        public const byte ISAM_INS_GENERATE_DESFire_AUTH_CODE = 0x25;
        public const byte ISAM_INS_GENERATE_DESFire_CHANGEKEY_PKGDATA = 0x27;
        public const byte ISAM_INS_VERIFY_DESFire_RESPONSE = 0x26;
        public const byte ISAM_INS_GET_Felica_Access_Key = 0x22;
        public const byte ISAM_INS_GET_Felica_Change_Key = 0x23;
        public const byte ISAM_INS_GET_TOKEN_KEY = 0x24;
        public const byte ISAM_INS_GET_DSM_ID = 0x31;
        public const byte ISAM_INS_GET_DSM_INFO = 0x32;
        public const byte ISAM_INS_GET_KEY_PAIR_INFO = 0x41;
        public const byte ISAM_INS_GET_RSA_SIGNATURE = 0x43;
        public const byte ISAM_INS_VERIFY_SIGNATURE = 0x44;

        ////////////////// CCHS SAM GetTokenKey Spec  ////////////////////////
        public const byte ISAM_NEW_KEY = 0x00;
        public const byte ISAM_OLD_KEY = 0x01;

        //#endif

        /*-------------------------------*/
        /* sSmartISO function parameters */
        /*-------------------------------*/

        public const int MIN_ISO_DATA_IN_LENGTH = 4;       /* CLA INS P1 P2 */
        public const int MAX_ISO_DATA_IN_LENGTH = 253;     /* 255 - CMD DEST */
        public const int MIN_ISO_DATA_OUT_LENGTH = 2;      /* SW1 SW2 */
        public const int MAX_ISO_DATA_OUT_LENGTH = 252;    /* 255 - CMD RET0 RET1 */
        public const int LX_ISO_IN_LENGTH = 1;             /* LC OR LE Required */

        public const int MAX_API_DATA_OUT_LENGTH = 256;

        public const int NO_DATA_EXP_LENGTH = 0;

        /*--------------------------------*/
        /* Virtual SAM specific Constants */
        /*--------------------------------*/
        public const byte LOGICAL_SAM_CLA = 0x82;

        public const byte LOGICAL_LKEY_INS = 0xF2;

        /*-------------------------------*/
        /* Mifare specific Constants     */
        /*-------------------------------*/

        //Desfire Ev0
        public const byte MIFARE_DESFIRE_CLA = 0x82;

        //Select application 
        public const byte MIFARE_SELA_INS = 0xA4;

        //Get version .. manufacturing related data 
        public const byte MIFARE_GETN_INS = 0x20;

        //Get Value
        public const byte MIFARE_GETV_INS = 0x3C;

        //Read
        public const byte MIFARE_READ_INS = 0xB2;

        //Write
        public const byte MIFARE_WRIT_INS = 0xD2;

        //Add Value
        public const byte MIFARE_ADDV_INS = 0x36;

        //Debit Value
        public const byte MIFARE_DEBV_INS = 0x34;

        //Commit Transaction
        public const byte MIFARE_COMT_INS = 0x8E;

        //Max History Records
        public const int MIFARE_HISTORY_RECORDS = 7;

        /*-------------------------------*/
        /* UltraLight specific Constants */
        /*-------------------------------*/
        public const byte MIFARE_ULTRALT_CLA = 0x85;

        public const int MIFARE_ULTRALT_BLOC_SIZE = 16;

        public const int MIFARE_ULTRALT_FLDS = 4;

        public const int MIFARE_ULTRALT_BLOC_BITS = 128;

        /*-------------------------------------*/
        /* Ticketing Rules specific Constants  */
        /*-------------------------------------*/
        //DM1 Area Code
        public const byte DM1_AREA_CODE = 0x01;

        //DM2 Area Code
        public const byte DM2_AREA_CODE = 0x02;

        //Delhi specific layout constants
        public const int MAX_NUMBER_OF_AREAS = 2; //DM1 & DM2

        public const int MAX_NUMBER_OF_FILES_DM1 = 9;

        public const int MAX_NUMBER_OF_FILES_DM2 = 8;

        //Maximum number of DayTypes [WKD, SAT, SUN, SPE]
        public const int MAX_NUMBER_OF_DTYPE = 4;

        public const int MAX_NUMBER_OF_STATIONS = 256;

        public const int MAX_NUMBER_OF_TIERS = 32;

        public const int MAX_NUMBER_OF_CDAYS = 400;

        //Maximum number of FareGroups
        public const int MAX_NUMBER_OF_FGROUP = 12;

        //Maximum number of TicketTypes
        public const int MAX_NUMBER_OF_TTYPE = 24;

        public const byte TICKET_TYPE_TTAG = 10;

        public const byte MBC_GateEntry = 1; // Entry passed
        public const byte MBC_GateExit = 0; // Exit passed
    }
}
