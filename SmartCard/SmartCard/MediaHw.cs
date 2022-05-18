using System;
using System.Collections.Generic;
using System.Xml;

using IFS2.Equipment.Common;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using System.Diagnostics;

namespace IFS2.Equipment.TicketingRules
{
    public class CommonHwMedia
    {
        public CommonHwMedia(SmartFunctions sf_)
        {
            _simulationFile = (string) Configuration.ReadParameter("CSCSimulationFile", "string", "C:\\IFS2\\Data\\Simulation\\CSCSimulationFile.xml");
            _simulation = (Boolean)Configuration.ReadParameter("SimulationActivated", "bool", "false");
            _simulation = false; //To remove
            if (_simulation)
            {
                _xmlDocument = new XmlDocument();
                _xmlDocument.Load(_simulationFile);
                _xmlRoot = _xmlDocument.DocumentElement;
            }
            if (sf_ != null)
                sf = sf_;
            else
                sf = SmartFunctions.Instance;
        }

        protected SmartFunctions sf;        

        private XmlDocument _xmlDocument;
        private XmlElement _xmlRoot;
        protected string _simulationFile;
        protected Boolean _simulation = false;
        protected Boolean _mediaDataRead = false;
        protected Boolean _applicationDataRead = false;
        protected Boolean _agentDataRead = false;
        protected Boolean _manufacturerDataRead = false;
        protected Boolean _validationDataRead = false;
        protected Boolean _customerDataRead = false;
        protected Boolean _localSaleDataRead = false;
        protected Boolean _tPurseDataRead = false;
        protected Boolean _autoReloadDataRead = false;
        protected Boolean _tPurseHistoryDataRead = false;

        //private Boolean _IsReaderLoaded = false;
        //private int _ReaderType = 3;
        //private int _hRw = 0;

        public void Reset()
        {
            _mediaDataRead = false;
            _applicationDataRead = false;
            _agentDataRead = false;
            _manufacturerDataRead = false;
            _validationDataRead = false;
            _customerDataRead = false;
            _localSaleDataRead = false;
            _tPurseDataRead = false;
            _autoReloadDataRead = false;
            _tPurseHistoryDataRead = false;
            _Reset();
        }

        protected virtual void _Reset()
        {
        }

        protected virtual Boolean _ReadManufacturerData(LogicalMedia logMedia)
        {
            return true;
        }
        protected virtual Boolean _ReadMediaData(LogicalMedia logMedia, MediaDetectionTreatment readTreatment)            
        {
            return true;
        }
        protected virtual Boolean _ReadAgentData(LogicalMedia logMedia, MediaDetectionTreatment readTreatment)
        {
            return true;
        }
        protected virtual Boolean _ReadApplicationData(LogicalMedia logMedia)
        {
            return true;
        }
        protected virtual Boolean _ReadValidationData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)            
        {
            return true;
        }
        protected virtual Boolean _ReadCustomerData(LogicalMedia logMedia)
        {
            return true;
        }
        protected virtual Boolean _ReadLocalSaleData(LogicalMedia logMedia)
        {
            return true;
        }
        protected virtual Boolean _ReadTPurseData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose
            )
        {
            return true;
        }
        protected virtual Boolean _ReadAutoReloadData(LogicalMedia logMedia
            )
        {
            return true;
        }
        protected virtual Boolean _ReadTPurseHistory(LogicalMedia logMedia, int NbrOfRecords)
        {
            return true;
        }
        protected virtual Boolean _WriteLocalSaleData(LogicalMedia logMedia, bool bCommit)
        {
            return true;
        }
        protected virtual Boolean _WriteLocalValidationData(LogicalMedia logMedia)
        {
            return true;
        }
        
        protected virtual Boolean _WriteLocalAgentPersonalization(LogicalMedia logMedia, bool bCommit)
        {
            return true;
        }

        protected virtual Boolean _WriteOneRecord(LogicalMedia logMedia, byte mApplication, byte mFileId)
        {
            return true;
        }
        protected virtual Boolean _WriteLocalSaleData(LogicalMedia logMedia)
        {
            return true;
        }

        public virtual Boolean _UpdateTPurseData(LogicalMedia logMedia, int modifyPursevalueBy, bool bCommit)
        {
            return true;
        }
        
        protected virtual Boolean _UpdateMediaEndOfValidity(LogicalMedia logMedia)
        {
            return true;
        }
        protected virtual Boolean _UpdateWhenMediaDetectedInDenyList(LogicalMedia logMedia)
        {
            return true;
        }        

        public Boolean WriteLocalSaleData(LogicalMedia logMedia, bool bCommit)
        {
            try
            {
                //if (!_localSaleDataRead) return false; To see if mandatory. Make problem at the moment
                return _WriteLocalSaleData(logMedia, bCommit);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Local Sale Data File " + e.Message);
                return false;
            }
        }

        public bool WriteLocalAgentPersonalization(LogicalMedia logMedia, bool bCommit)
        {
            try
            {
                //if (!_localSaleDataRead) return false; To see if mandatory. Make problem at the moment
                return _WriteLocalAgentPersonalization(logMedia, bCommit);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Local Sale Data File " + e.Message);
                return false;
            }
        }

        public Boolean WriteLocalValidationData(LogicalMedia logMedia)
        {
            try
            {
                //if (!_localSaleDataRead) return false; To see if mandatory. Make problem at the moment
                return _WriteLocalValidationData(logMedia);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Local Sale Data File " + e.Message);
                return false;
            }
        }
        //SKS: Added on 20150831
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logMedia"></param>
        /// <param name="mAppId"></param>
        /// <param name="mFileId"></param>
        /// <returns></returns>
        public Boolean WriteOneRecord(LogicalMedia logMedia,byte mAppId, byte mFileId)
        {
            try
            {
                //if (!_localSaleDataRead) return false; To see if mandatory. Make problem at the moment
                return _WriteOneRecord(logMedia,mAppId,mFileId);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Records File AppID[" + mAppId.ToString() + "] FileId[" + mFileId.ToString()+"] " + e.Message);
                return false;
            }
        }

        public bool AppendCommonAreaPurseHistoryRecord(LogicalMedia logMedia)
        {
            try
            {
                //if (!_localSaleDataRead) return false; To see if mandatory. Make problem at the moment
                return _AppendCommonAreaPurseHistoryRecord(logMedia);
            }
            catch (Exception)
            {                
                return false;
            }
        }

        protected virtual bool _AppendCommonAreaPurseHistoryRecord(LogicalMedia logMedia)
        {
            throw new NotImplementedException();
        }

        public Boolean UpdateTPurseData(LogicalMedia logMedia, int modifyPursevalueBy, bool bCommit)
        {
            try
            {

                  return _UpdateTPurseData(logMedia, modifyPursevalueBy, bCommit);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Tpurse Data " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Note that the all that shall be read shall have been read before. In other case can make a big error
        /// </summary>
        /// <param name="logMedia"></param>
        /// <returns></returns>
        public Boolean UpdateMediaEndOfValidity(LogicalMedia logMedia)
        {
            try
            {
                return _UpdateMediaEndOfValidity(logMedia);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in UpdateMediaEndOfValidity Data " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Note that the all that shall be read shall have been read before. In other case can make a big error
        /// </summary>
        /// <param name="logMedia"></param>
        /// <returns></returns>
        public Boolean UpdateWhenMediaDetectedInDenyList(LogicalMedia logMedia)
        {
            try
            {
                return _UpdateWhenMediaDetectedInDenyList(logMedia);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in UpdateMediaEndOfValidity Data " + e.Message);
                return false;
            }
        }

        public Boolean ReadManufacturerData(LogicalMedia logMedia
            //, MediaDataRaw detailedMediaData
            )
        {
            try
            {
                if (_manufacturerDataRead) return true;
                if (_simulation)
                {
                    XmlNode node = _xmlRoot.SelectSingleNode("//LogicalMedia/Media");
                    logMedia.Media.Initialisation("<Media>" + node.InnerXml + "</Media>");
                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadManufacturerData(logMedia))
                    {
                        _manufacturerDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Manufacturer Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadMediaData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                if (!_manufacturerDataRead)
                {
                    if (!ReadManufacturerData(logMedia)) return false;
                }
//                if (_mediaDataRead) return true;
                if (_simulation)
                {
                    XmlNode node = _xmlRoot.SelectSingleNode("//LogicalMedia/Media");
                    logMedia.Media.Initialisation("<Media>"+node.InnerXml+"</Media>");
                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadMediaData(logMedia, readPurpose))
                    {
                        _mediaDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Media Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadAgentData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose
            )
        {
            try
            {
                if (!_applicationDataRead)
                {
                    if (!ReadApplicationData(logMedia, readPurpose)) return false;
                } 
                if (_agentDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadAgentData(logMedia, readPurpose))
                    {
                        _agentDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Agent Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadApplicationData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                if (!_mediaDataRead)
                {
                    Debug.Assert(false); // Doing this to verfiy existing control flow.
                    if (!ReadMediaData(logMedia, readPurpose)) return false;
                }
                  
                if (_applicationDataRead) return true;
             
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadApplicationData(logMedia))
                    {
                        _applicationDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Application Data : " + e.Message);
                return false;
            }
        }


        public Boolean ReadValidationData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                if (!_applicationDataRead)
                {
                    if (!ReadApplicationData(logMedia, readPurpose)) return false;
                }
                if (logMedia.Application.TransportApplication.OperationalType == TransportApplication.OperationalTypeValues.Agent)
                {
                    if (!_agentDataRead)
                    {
                        if (!ReadAgentData(logMedia, readPurpose)) return false;
                    }
                }

                if (_validationDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadValidationData(logMedia, readPurpose))
                    {
                        _validationDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Validation Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadCustomerData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose
            )
        {
            try
            {
                if (!_applicationDataRead)
                {
                    if (!ReadApplicationData(logMedia, readPurpose)) return false;
                }
                if (_customerDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadCustomerData(logMedia))
                    {
                        _customerDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Customer Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadLocalSaleData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose
            )
        {
            try
            {
                if (!_applicationDataRead)
                {
                    if (!ReadApplicationData(logMedia, readPurpose)) return false;
                }
                if (_localSaleDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadLocalSaleData(logMedia))
                    {
                        _localSaleDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Local Sale Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadTPurseData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                if (!_mediaDataRead)
                {
                    if (!ReadMediaData(logMedia, readPurpose)) return false;
                }
                if (_tPurseDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadTPurseData(logMedia, readPurpose))
                    {
                        _tPurseDataRead = true;                        
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read TPurse Data : " + e.Message);
                return false;
            }
        }

        public bool WriteMainSaleData(LogicalMedia logMedia)
        {
            try
            {
                if (!_WriteMainSaleData(logMedia))
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool _WriteMainSaleData(LogicalMedia logMedia)
        {
            return true;
        }

        public bool WriteCardHolderData(LogicalMedia logMedia)
        {
            try
            {
                if (!_WriteCardHolderData(logMedia))
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool _WriteCardHolderData(LogicalMedia logMedia)
        {
            return true;
        }

        public Boolean ReadAutoReloadData(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                if (!_tPurseDataRead)
                {
                    if (!ReadTPurseData(logMedia, readPurpose)) return false;
                }
                if (_autoReloadDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    //Add code here to read physical media
                    if (_ReadAutoReloadData(logMedia))
                    {
                        _autoReloadDataRead = true;
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read AutoReload Data : " + e.Message);
                return false;
            }
        }

        public Boolean ReadAllTPurseHistory(LogicalMedia logMedia, MediaDetectionTreatment readPurpose)
        {
            try
            {
                if (!_tPurseDataRead)
                {
                    if (!ReadTPurseData(logMedia, readPurpose)) return false;
                }
                if (_tPurseHistoryDataRead) return true;
                if (_simulation)
                {

                }
                else
                {
                    if (_ReadTPurseHistory(logMedia, -1))
                    {
                        _tPurseHistoryDataRead = true;
                        logMedia.Purse.History.Hidden = false;
                        return true;
                    }
                    else return false;
                }

                return false;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Read Application Data : " + e.Message);
                return false;
            }
        }

        public bool UpdateForRefundLocal(LogicalMedia logicalMediaUpdatedForCurrentOp)
        {
            throw new NotImplementedException();
        }

        public bool UpdateForRefundCommon(LogicalMedia logicalMediaUpdatedForCurrentOp)
        {
            throw new NotImplementedException();
        }

        public bool CommitModifications()
        {
            try
            {
                return _CommitModifications();
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Local Sale Data File " + e.Message);
                return false;
            }
        }

        public virtual bool _CommitModifications()
        {
            return true;
        }

        public bool WriteCommonValidationFile(LogicalMedia logMedia)
        {
            try
            {
                return _WriteCommonValidationFile(logMedia);
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "Error in Writing Local Sale Data File " + e.Message);
                return false;
            }
        }

        protected virtual bool _WriteCommonValidationFile(LogicalMedia logMedia)
        {
            return true;
        }

        protected byte pSw1, pSw2;
        protected CSC_API_ERROR Err;
        public enum Status { 
            Success, 
            Failed_MediaWasNotInField, 
            Failed_MediaFailedToAuthenticate,
            FailedNotCategorized
        };
        virtual public Status GetLastStatus() { throw new NotImplementedException(); }
    }
}
