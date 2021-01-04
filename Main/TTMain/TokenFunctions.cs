using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules.CommonFunctions;
using System.Xml;
using IFS2.Equipment.TicketingRules.CommonTT;

namespace IFS2.Equipment.TicketingRules
{
    public partial class MainTicketingRules
    {
        LogicalMedia _logMediaTokenForLastRequestedGetTokenPrice = null;

        public bool TreatTokenMessageReceived(EventMessage eventMessage)
        {
            switch (eventMessage.EventID.ToUpper())
            {
                case "GETTOKENPRICE":
                    {
                        bool bStationBased = eventMessage._par[1] == "1";
                        bool bZoneBased = eventMessage._par[1] == "2";

                        _logMediaTokenForLastRequestedGetTokenPrice = new LogicalMedia();
                        Media m = _logMediaTokenForLastRequestedGetTokenPrice.Media;
                        LocalLastAddValue lcav = _logMediaTokenForLastRequestedGetTokenPrice.Application.LocalLastAddValue;
                        Validation val = _logMediaTokenForLastRequestedGetTokenPrice.Application.Validation;
                        Initialisation ini = _logMediaTokenForLastRequestedGetTokenPrice.Initialisation;
                        TransportApplication ta = _logMediaTokenForLastRequestedGetTokenPrice.Application.TransportApplication;
                        Customer cu = _logMediaTokenForLastRequestedGetTokenPrice.Application.Customer;

                        Products ps = _logMediaTokenForLastRequestedGetTokenPrice.Application.Products;
                        OneProduct p = new OneProduct();
                        ps.Add(p);

                        int FareTier = -1;
                        int Origin = 0;
                        int Destination = 0;
                        int SaleStn;
                        //int Zone = 0;
                        int Language = 0;
                        int Test = 0;

                        try
                        {
                            string[] splitValues = eventMessage.Attribute.Split(';');

                            if (bStationBased)
                            {
                                Origin = Convert.ToInt32(splitValues[0]);
                                Destination = Convert.ToInt32(splitValues[1]);
                                Language = Convert.ToInt32(splitValues[2]);
                                Test = Convert.ToInt32(splitValues[3]);
                            }
                            else //if (bZoneBased)
                            {
                                Origin = Convert.ToInt32(splitValues[0]);
                                FareTier = Convert.ToInt32(splitValues[1]);
                                Language = Convert.ToInt32(splitValues[2]);
                                Test = Convert.ToInt32(splitValues[3]);
                            }
                            SaleStn = Origin;
                            int tokenPrice;
                            if (bStationBased)
                                tokenPrice = SalePriceCalculation.CalculateTokenPriceSiteBased(Origin, Destination, out FareTier);
                            else
                                tokenPrice = SalePriceCalculation.CalculateTokenPriceZoneBased(FareTier);
                            if (tokenPrice <= 0)
                            {
                                Communication.SendMessage(ThreadName, "Answer", "GetTokenPriceAnswer", Convert.ToString((int)TTErrorTypes.FareTablesError), "");
                                return true;
                            }
                            
                            TokenFunctions.LoadStaticDataForIssue(_logMediaTokenForLastRequestedGetTokenPrice,
                                1 // i.e. SJT
                                );

                            SalesRules.TokenSaleUpdate(_logMediaTokenForLastRequestedGetTokenPrice, tokenPrice, Origin, Destination, (short)FareTier);
                            cu.Language = (Customer.LanguageValues)Language;
                            m.Test = Convert.ToBoolean(Test);
                            
                            string s = _logMediaTokenForLastRequestedGetTokenPrice.ToXMLString();

                            string res = "0";
                            if (tokenPrice == 0) res = Convert.ToString((int)TTErrorTypes.FareTablesError);
                            Communication.SendMessage(ThreadName, "Answer", "GetTokenPriceAnswer", res + ";" + Convert.ToString(tokenPrice), s);

                            Logging.Log(LogLevel.Verbose, ThreadName + "GetTokenPriceAnswer :" + res);
                        }
                        catch (Exception e1)
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetTokenPriceAnswer", Convert.ToString((int)TTErrorTypes.UnknownError) + ";0", "");
                            Logging.Log(LogLevel.Error, ThreadName + "GetTokenPriceAnswer Error :" + e1.Message);
                        }
                        return (true);                        
                    }
                case "GETTOKENPRICEFORFREEORPAIDEXIT":
                    {
                        if (!SharedData._fpSpecsRepository.Initialized)
                        {
                            SendMsg.GetTokenPriceAnswer(TTErrorTypes.FareTablesError, Int32.MaxValue, "");
                            return true;
                        }
                        
                        short productType = Convert.ToInt16(eventMessage._par[0]);
                        int Language = Convert.ToInt32(eventMessage._par[1]);
                        int Test = Convert.ToInt32(eventMessage._par[2]);

                        var specs = SharedData._fpSpecsRepository.GetSpecsFor(productType);
                        if (specs == null)
                        {
                            SendMsg.GetTokenPriceAnswer(TTErrorTypes.FareTablesError, Int32.MaxValue, "");
                            return true;
                        }
                        int price = specs._SalePrice.Val;
                        _logMediaTokenForLastRequestedGetTokenPrice = new LogicalMedia();

                        _logMediaTokenForLastRequestedGetTokenPrice.Reset(); // TODO: It is newly introdued. Shouldn't it be inducted in GetTokenPrice also??
                        Media m = _logMediaTokenForLastRequestedGetTokenPrice.Media;
                        LocalLastAddValue lcav = _logMediaTokenForLastRequestedGetTokenPrice.Application.LocalLastAddValue;
                        Validation val = _logMediaTokenForLastRequestedGetTokenPrice.Application.Validation;
                        Initialisation ini = _logMediaTokenForLastRequestedGetTokenPrice.Initialisation;
                        TransportApplication ta = _logMediaTokenForLastRequestedGetTokenPrice.Application.TransportApplication;
                        Customer cu = _logMediaTokenForLastRequestedGetTokenPrice.Application.Customer;

                        Products ps = _logMediaTokenForLastRequestedGetTokenPrice.Application.Products;
                        OneProduct p = new OneProduct();
                        ps.Add(p);

                        TokenFunctions.LoadStaticDataForIssue(_logMediaTokenForLastRequestedGetTokenPrice,
                                    productType
                                    );
                        SalesRules.TokenSaleUpdateForPaidOrFreeExit(_logMediaTokenForLastRequestedGetTokenPrice, price, SharedData.StationNumber);
                        cu.Language = (Customer.LanguageValues)Language;
                        m.Test = Convert.ToBoolean(Test);

                        SendMsg.GetTokenPriceAnswer(TTErrorTypes.NoError, price, _logMediaTokenForLastRequestedGetTokenPrice.ToXMLString());

                        return true;
                    }
#if !WindowsCE 
                case "GETLISTFARETIERS":
                    {
                        //List<int> lstSites = new List<int>();
                        //XmlDocument xml = new XmlDocument();
                        //xml.LoadXml(Disk.ReadAllTextFile(Disk.BaseDataDirectory + @"\CurrentXmlParameters\Topology.xml"));
                        //XmlElement root = xml.DocumentElement;
                        //XmlNodeList nodelist = root.SelectNodes("Lines/Line");
                        //foreach (XmlNode node in nodelist)
                        //{
                        //    int lineNum = Convert.ToInt32(node.SelectSingleNode("Code").InnerText);

                        //    try
                        //    {
                        //        XmlNodeList nodelist1 = node.SelectNodes("Sts/St");

                        //        foreach (XmlNode node1 in nodelist1)
                        //        {
                        //            int siteId = Convert.ToInt32(node1.SelectSingleNode("Code").InnerText);
                        //            bool active = node1.SelectSingleNode("Act").InnerText == "1";
                        //            if (active)                                        
                        //                lstSites.Add(siteId);
                        //        }
                        //    }
                        //    catch { }                            
                        //}

                        SortedDictionary<int, int> fareTierVsFare = new SortedDictionary<int, int>();
                        bool bSuccess = true;
                        Logging.Trace("ListeFareTiers " + TopologyParameters.Stations.Keys.Count.ToString());
                        foreach (int siteId in TopologyParameters.Stations.Keys)
                        {
                            if (siteId == SharedData.StationNumber)
                                continue;
                            if (!TopologyParameters.Stations[siteId].Activation)
                                continue;
                            int FareTier = FareParameters.GetFareTier(SharedData.StationNumber, siteId);
                            //Logging.Trace("Fare Tiers Value " + FareTier.ToString());
                            if (FareTier <= 0) continue;
                            //{
                            //    bSuccess = false;
                            //    break;
                            //}
                            int temp;
                            if (fareTierVsFare.TryGetValue(FareTier, out temp))
                                continue;
                            int tokenPrice = (int)SalePriceCalculation.CalculateTokenPriceZoneBased(FareTier);
                            //Logging.Trace("Price Value " + tokenPrice.ToString());
                            if (tokenPrice <= 0) continue;
                            //{
                            //    bSuccess = false;
                            //    break;
                            //}
                            //else
                                fareTierVsFare[FareTier] = tokenPrice;
                        }
                        if (bSuccess)
                        {
                            string strResult = "";

                            foreach (var entry in fareTierVsFare)
                                strResult += String.Format("{0}:{1};", entry.Key, entry.Value);
                            Communication.SendMessage("", "", "GetListFareTiersAnswer", "0", strResult);
                        }
                        else
                            Communication.SendMessage("", "", "GetListFareTiersAnswer", "1");
                        
                        return true;
                    }
#endif
                case "GETTOKENBINDATA":

                    try
                    {
                        string[] tab = eventMessage.Attribute.Split(';');
                        //We could change also serial number in buffer.
                        //See how to add serial number. But there is a problem resetting with product
                        //_logMediaToken.Reset();
                        //_logMediaToken.InitialiseLogicalMedia(eventMessage.Message);
                        //Update with serial number.

                        if (tab.Count() == 3)
                        {
                            _logMediaTokenForLastRequestedGetTokenPrice.Media.ChipSerialNumber = Convert.ToInt64(tab[0]);

                            byte[] pBlock0 = SerializeHelper<byte[]>.XMLDeserialize(tab[2]);

                            ulong mac_NotUsedByMe;
                            byte[] cb = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocks(SharedData.TokenLayoutVersion, _logMediaTokenForLastRequestedGetTokenPrice, pBlock0, out mac_NotUsedByMe));

                            string packedCmdBuf = SerializeHelper<byte[]>.XMLSerialize(cb);

                            Communication.SendMessage(ThreadName, "Data", "GetTokenBinDataAnswer", packedCmdBuf, _logMediaTokenForLastRequestedGetTokenPrice.ToXMLString());
                        }
                        else
                        {
                            Communication.SendMessage(ThreadName, "Answer", "GetTokenBinDataAnswer", "", _logMediaTokenForLastRequestedGetTokenPrice.ToXMLString());
                            Logging.Log(LogLevel.Error, ThreadName + "GetTokenBinDataAnswer Error : Error in the Input Values");
                        }

                    }
                    catch (Exception e1)
                    {
                        Communication.SendMessage(ThreadName, "Answer", "GetTokenBinDataAnswer", "", _logMediaTokenForLastRequestedGetTokenPrice.ToXMLString());
                        Logging.Log(LogLevel.Error, ThreadName + "GetTokenBinDataAnswer Error :" + e1.Message);
                    }
                    return (true);
            }
            return false;
        }
#if !_HHD_
        UpdateTTagRequest _ttagUpdateRequest = null;
#endif
        private TTErrorTypes AttemptTTagUpdate()
        {
            #if !_HHD_
            if (_ttagUpdateRequest == null)
                throw new Exception("AttemptTTagUpdate: No request data found");
            
            if (_logMediaToken.TTag.Hidden)
                if (!_ttagUpdateRequest._bForceWriteEvenIfMediaIsNotTTag)
                    return TTErrorTypes.TTagUpdate_NotATTag;
            var ttag = _logMediaToken.TTag;
            ttag.Hidden = false;
            ttag.TimeLastWritten = DateTime.Now;
            if (_ttagUpdateRequest._cntTokens != null)
                ttag.CountTokens = (short)(_ttagUpdateRequest._cntTokens);
            ttag.EquipmentNumber = SharedData.EquipmentNumber;
            ttag.LastOperation = _ttagUpdateRequest._op;

            if (_ttagUpdateRequest._serialNumber != null)
                ttag.SerialNumber = (int)_ttagUpdateRequest._serialNumber;
            
            if (!ttag._issueDate.AlreadyRead)
                ttag.IssueDate = _logMediaToken.Media.InitialisationDate;

            byte[] data = TokenFunctions.GetWriteCmdBuffer(TokenFunctions.GetDataBlocksForTTag(ttag));
            byte p1, p2;
            bool bSuccess;
            IFS2.Equipment.TicketingRules.CommonTT.CSC_API_ERROR err = TokenFunctions.WriteBlocks((IFS2.Equipment.TicketingRules.CommonTT.CSC_READER_TYPE)(_ReaderType), _hRw, data, out p1, out p2, out bSuccess);

            SmartFunctions.Instance.SwitchToDetectRemovalState();
            if (err == IFS2.Equipment.TicketingRules.CommonTT.CSC_API_ERROR.ERR_NONE && bSuccess)
                return TTErrorTypes.NoError;
            else
                return TTErrorTypes.MediaNotPresent; // TODO: correct it
#else
            return TTErrorTypes.NoError;
#endif

        }

        private void Handle_UpdateTTag(EventMessage eventMessage)
        {
#if !_HHD_
#if !WindowsCE
            var pars = eventMessage._par;
            
            bool bForceWriteEvenIfMediaIsNotTTag = Boolean.Parse(pars[0]);
            TTagOps op = (TTagOps)(Enum.Parse(typeof(TTagOps), pars[1]));
            int agentId = Int32.Parse(pars[2]);

            if (!Config._bUseCallbackForMediaDetectionNRemoval)
            {
                _ttagUpdateRequest = new UpdateTTagRequest(bForceWriteEvenIfMediaIsNotTTag, op, agentId, pars.Length > 3 ? pars[3] : "");

                if (_MediaDetectedState == SmartFunctions.MediaDetected.TOKEN)
                {
                    CSC_API_ERROR err2 = SmartFunctions.Instance.SwitchToCardOnState();

                    if (err2 != CSC_API_ERROR.ERR_NONE)
                        return;
                }
                else
                    return;
                var err = AttemptTTagUpdate();
                if (err == TTErrorTypes.NoError)
                {
                    SendMsg.UpdateTTagAnswer_Success(_logMediaToken.TTag);
                    _ttagUpdateRequest = null;
                }
                else
                {
                }
            }
#endif
#endif
        }
    }
}
