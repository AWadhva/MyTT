using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFS2.Equipment.TicketingRules;

namespace IFS2.Equipment.TicketingRules
{
    public sealed class DfPhyLayout
    {
        //readonly int CardLayoutMax= 
        static readonly DfPhyLayout _DfPhyLayout = new DfPhyLayout();
        
            private static sDesfireCardLayout[] DesfireLayout = new sDesfireCardLayout[]      
           {
               new sDesfireCardLayout{appId = 0x00,fileId=0x00,  arSetting = new sAccessRight{kcnRead = 0x00,kcnWrite=0x00,kcnReadWrite=0x00,kcnChangeKey=0x00},fileType=0x00, CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},////0x00, Card Manufacturer
               new sDesfireCardLayout{appId = 0xFFFFFF,fileId=0x08,arSetting = new sAccessRight{kcnRead = 40,kcnWrite=0x0F,kcnReadWrite=40,kcnChangeKey=40},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//1, standard  data file 
               new sDesfireCardLayout{appId = 0xFFFFFF,fileId=0x09,arSetting = new sAccessRight{kcnRead = 42,kcnWrite=0x0F,kcnReadWrite=41,kcnChangeKey=40},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//2,standard  data file
               new sDesfireCardLayout{appId = 0x818000,fileId=0x00,arSetting = new sAccessRight{kcnRead = 0x03,kcnWrite=0x0F,kcnReadWrite=0x02,kcnChangeKey=101},fileType=0x02,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//0, purse
               new sDesfireCardLayout{appId = 0x818000,fileId=0x01,arSetting = new sAccessRight{kcnRead = 0x03,kcnWrite=0x0F,kcnReadWrite=0x01,kcnChangeKey=101},fileType=0x01,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//1, standard  data file
               new sDesfireCardLayout{appId = 0x818000,fileId=0x08,arSetting = new sAccessRight{kcnRead = 0x03,kcnWrite=0x0F,kcnReadWrite=101,kcnChangeKey=101},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//08, standard  data file
               new sDesfireCardLayout{appId = 0x818000,fileId=0x09,arSetting = new sAccessRight{kcnRead = 0x03,kcnWrite=0x0F,kcnReadWrite=0x01,kcnChangeKey=101},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00},//09,Standard Data File) --
               new sDesfireCardLayout{appId = 0x818000,fileId=0x0A,arSetting = new sAccessRight{kcnRead = 0x03,kcnWrite=0x0F,kcnReadWrite=0x02,kcnChangeKey=101},fileType=0x00,CurrentKeyCardNumber=0x00,CurrentCommunicationByte=0x00}//0A, (Standard Data File)
               
            };

            public static DfPhyLayout Instance
            {
                get
                {
                    return _DfPhyLayout;
                }
            }
            internal void fillKeyReferenceByte(ref sKeyReferenceBytes keyrefbytes, int appID, EAccessPermission l_permission, bool bUseNewKeySet)
            {
            }
            internal void fillRefBytes(ref sDesfireCardLayout l_layout, EAccessPermission l_permission, bool bUseNewKeySet)
            {
            }
            public bool getLayoutDefinition(out sDesfireCardLayout mDesfirelayout, int aid, byte fileid, EAccessPermission l_permission, bool bUseNewKeySet)
            {
                bool l_result = false;
                mDesfirelayout = new sDesfireCardLayout();
                mDesfirelayout.appId = aid;
                mDesfirelayout.fileId = fileid;
                for (int i = 0; i < DesfireLayout.Length; i++)
                {
                    if (DesfireLayout[i].appId == aid && DesfireLayout[i].fileId == fileid)
                    {
                        mDesfirelayout.arSetting = DesfireLayout[i].arSetting;
                        mDesfirelayout.keyReferenceBytes = DesfireLayout[i].keyReferenceBytes;

                        l_result = true;
                        break;
                    }
                }
                if (l_result)
                {
                   // fillRefBytes(ref mDesfirelayout, l_permission, bUseNewKeySet);
                    switch ((int)l_permission)
                    {
                        case (int)EAccessPermission.E_AccessPermission_Read: { mDesfirelayout.CurrentKeyCardNumber = mDesfirelayout.arSetting.kcnRead; } break;
                        case (int)EAccessPermission.E_AccessPermission_Write: { mDesfirelayout.CurrentKeyCardNumber = mDesfirelayout.arSetting.kcnWrite; } break;
                        case (int)EAccessPermission.E_AccessPermission_ReadWrite: { mDesfirelayout.CurrentKeyCardNumber = mDesfirelayout.arSetting.kcnReadWrite; } break;
                        case (int)EAccessPermission.E_AccessPermission_ChangeKey: { mDesfirelayout.CurrentKeyCardNumber = mDesfirelayout.arSetting.kcnChangeKey; } break;
                        default: { mDesfirelayout.CurrentKeyCardNumber = mDesfirelayout.arSetting.kcnRead; } break;
                    }
                }
                return l_result;
            }//
            public bool GetKeyEntry(int aid, byte fileId, EAccessPermission l_permission, out byte KeyEntry)
            {
                bool l_result = false;
                KeyEntry = 0x0f;
                for (int i = 0; i < DesfireLayout.Length; i++)
                {
                    if (DesfireLayout[i].appId == aid && DesfireLayout[i].fileId == fileId)
                    {
                        l_result = true;
                        switch ((int)l_permission)
                        {
                            case (int)EAccessPermission.E_AccessPermission_Read:
                                KeyEntry = DesfireLayout[i].arSetting.kcnRead;
                                break;
                            case (int)EAccessPermission.E_AccessPermission_Write:
                                KeyEntry = DesfireLayout[i].arSetting.kcnWrite;
                                break;
                            case (int)EAccessPermission.E_AccessPermission_ChangeKey:
                                KeyEntry = DesfireLayout[i].arSetting.kcnReadWrite;
                                break;
                            case (int)EAccessPermission.E_AccessPermission_ReadWrite:
                                KeyEntry = DesfireLayout[i].arSetting.kcnChangeKey; 
                                break;
                            default: break;
                        }//swtich 
                        break;
                    }
                }//for
                return l_result;
            }
    }
}
