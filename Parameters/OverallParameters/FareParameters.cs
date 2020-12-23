/* Prithvi : Temp Faretables are loaded from INI for Simulation may differ in actual scenario 
   */

using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
//using IniParser;
using IFS2.Equipment.Common;
//using IFS2.Equipment.TicketingRules.CommonFunctions;

namespace IFS2.Equipment.TicketingRules
{

    public class SurchargeElement
    {
        public string Code;
        public string Label;
        public int Price;
        public string DisplayedCode;
    }


    public static class FareParameters
    {
        public static OneEvent _faresMissing = null;
        public static OneEvent _faresError = null;
        public static OneEvent _faresActivation = null;
        public static void Start()
        {
        }

        static FareParameters()
        {
            _faresMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 25, "FaresMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus","FaresMissing",AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _faresError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 26, "FaresError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "FaresError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _faresActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 27, "FaresActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "FaresActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }

        private static int _shortestReturnTripDuration;
        private static int _goodwillAmount;
        private static int _maximumValidationAmount;
        private static int _minimumExitValue;
        private static int _shortReturnTripFare;
        private static int _paidTime;

        public static int ShortestReturnTripDuration { get { return _shortestReturnTripDuration; } }
        public static int _GoodwillAmount { get { return _goodwillAmount; } }
        public static int MaximumValidationAmount { get { return _maximumValidationAmount; } }
        public static int MinimumExitValue { get { return _minimumExitValue; } }
        public static int ShortReturnTripFare { get { return _shortReturnTripFare; } }
        public static int PaidTime { get { return _paidTime; } }

        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("FareParameters"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "FareParameters");
        }

        public static int GetFareGroup(int fareType, int dayType, int intervalType)
        {
            try
            {
                return _fareTypeMatrix[fareType][dayType][intervalType];
            }
            catch
            {
                Logging.Log(LogLevel.Error, "GetFareGroup not recoverable "+Convert.ToString(fareType)+"/"+Convert.ToString(dayType)+"/"+Convert.ToString(intervalType));
                return -1;
            }
        }

        public static int GetFareTier(int origin, int destination)
        {
            try
            {
                return _stationMatrix[origin][destination];
            }
            catch
            {
                Logging.Log(LogLevel.Error, "GetFareTier: Fare Tiers not found " + Convert.ToString(origin) + "/" + Convert.ToString(destination));
                return -1;
            }
        }

        public static int GetFareValue(int fareGroup, int fareTiers, int concession)
        {
            try
            {
                return _globalfaretable[fareGroup][fareTiers][concession];
            }
            catch
            {
                Logging.Log(LogLevel.Error, "GetFareValue: Cannot be retrieved "+Convert.ToString(fareGroup)+"/"+Convert.ToString(fareTiers)+"/"+Convert.ToString(concession));
                return -1;
            }
        }        

        public static bool LoadVersion(string content)
        {
            if (content == "")
            {
                //File is empty or not there
                _faresMissing.SetAlarm(true);
                throw (new Exception("****"));
                //return false;
            }
            _faresMissing.SetAlarm(false);
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                XmlElement root = xml.DocumentElement;

                //Test for AVM
                XmlNodeList list = root.SelectNodes("Products/Prd");
                if (list == null || list.Count == 0)
                {
                    _faresMissing.SetAlarm(false);
                    return true;
                }


                LoadGeneralParameters(root);
                MediaParameters.LoadMediaTechnologyVersion(root);
                MediaParameters.LoadMediaTypeVersion(root);

                TimeParameters.LoadCalendarsVersion(root);
                TimeParameters.LoadTimeIntervalsVersion(root);
                ProductParameters.LoadProductsVersion(root);
                
                bool loadFare = (bool)Configuration.ReadParameter("EOD_LoadFareParameters","bool","true");
                if (loadFare)
                {
                    LoadSurchargeVersion(root);
                    LoadStationMatrixVersion(root);
                    LoadFareTiersMatrixVersion(root);
                    LoadFareGroupMatrixVersion(root);
                }
                _faresError.SetAlarm(false);
                return (true);

            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_LoadVersion " + e.Message);
                _faresError.SetAlarm(true);
                throw (new Exception("****"));
            }
        }

        public static bool LoadGeneralParameters(XmlElement root)
        {
            try
            {
                XmlNode node = root.SelectSingleNode("General");
                try
                {
                    _shortestReturnTripDuration = Convert.ToInt32(node.SelectSingleNode("ShortestReturnTripDuration").InnerText);
                    try
                    {
                        XmlNode x = node.SelectSingleNode("GoodwillAmount");
                        if (x != null)
                            _goodwillAmount = Convert.ToInt32(x.InnerText);
                        else
                        {
                            x = node.SelectSingleNode("BonusAmount");
                            if (x != null)
                                _goodwillAmount = Convert.ToInt32(x.InnerText);
                        }
                    }
                    catch{}

                    _maximumValidationAmount = Convert.ToInt32(node.SelectSingleNode("MaximumValidationAmount").InnerText);
                    _minimumExitValue = Convert.ToInt32(node.SelectSingleNode("MinimumExitValue").InnerText);
                    _shortReturnTripFare = Convert.ToInt32(node.SelectSingleNode("ShortReturnTripFare").InnerText);
                    _paidTime = Convert.ToInt32(node.SelectSingleNode("PaidTime").InnerText);
                }
                catch (Exception e)
                {
                    Logging.Log(LogLevel.Error, "Bad GeneralInformations" + e.Message);
                    throw (new Exception("****"));
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_LoadGeneralParameters " + e.Message);
                throw(new Exception("****"));
            }
        }

        public static Dictionary<int, SurchargeElement> _surcharges = null;

        public static bool LoadSurchargeVersion(XmlElement root)
        {

            if (_surcharges == null) _surcharges = new Dictionary<int, SurchargeElement>();
            else _surcharges.Clear();

            try
            {
                XmlNodeList nodelist = root.SelectNodes("FareTables/GlobalSurchargeList/Surcharge");
                int idx = -1;
                foreach (XmlNode node in nodelist)
                {
                    ++idx;
                    try
                    {
                        SurchargeElement sc = new SurchargeElement();
                        sc.Code = node.SelectSingleNode("Code").InnerText;
                        sc.Label = node.SelectSingleNode("Label").InnerText;
                        sc.DisplayedCode = node.SelectSingleNode("DisplayedCode").InnerText;
                        sc.Price = Convert.ToInt32(node.SelectSingleNode("Price").InnerText);
                        _surcharges[idx] = sc;
                    }
                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad Surcharges" + e.Message);
                        _faresError.SetAlarm(true);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_Surcharge " + e.Message);
                throw (new Exception("****"));
            }
        }

        private static Dictionary<int, Dictionary<int, int>> _stationMatrix = null;
        public static bool LoadStationMatrixVersion(XmlElement root)
        {
            if (_stationMatrix == null) _stationMatrix = new Dictionary<int, Dictionary<int, int>>();
            else _stationMatrix.Clear();

            try
            {

                XmlNodeList nodelist = root.SelectNodes("Matrixes/StationToStationMatrix/Cell");
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        int EntryStation = Convert.ToInt32(node.SelectSingleNode("En").InnerText);
                        int ExitStation = Convert.ToInt32(node.SelectSingleNode("Ex").InnerText);
                        int FareTiers = Convert.ToInt32(node.SelectSingleNode("FT").InnerText);
                        if (_stationMatrix.ContainsKey(EntryStation))
                        {
                            _stationMatrix[EntryStation].Add(ExitStation, FareTiers);
                        }
                        else
                        {
                            Dictionary<int,int> dico = new Dictionary<int,int>();
                            dico.Add(ExitStation, FareTiers);
                            _stationMatrix.Add(EntryStation, dico);
                        }
                    }

                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad StationMatrix " + e.Message);
                        _faresError.SetAlarm(true);
                    }

                }
                return true;
            }

            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_StationMatrix " + e.Message);
                throw (new Exception("****"));
            }
        }
         public static Dictionary<int, Dictionary<int, Dictionary<int,int>>> _fareTypeMatrix = null;
         public static bool LoadFareGroupMatrixVersion(XmlElement root)
        {
            if (_fareTypeMatrix == null) _fareTypeMatrix = new Dictionary<int, Dictionary<int,Dictionary<int,int>>>();
            else _fareTypeMatrix.Clear();
            try
            {
                XmlNodeList nodelist = root.SelectNodes("Matrixes/GlobalFareGroupTable/FTyp");
                foreach (XmlNode node in nodelist)
                {                                
                  try
                    {
                      int Ref = Convert.ToInt32(node.SelectSingleNode("Ref").InnerText);
                      Dictionary<int,Dictionary<int, int>> Dico1 = new Dictionary<int,Dictionary<int, int>>();
                      XmlNodeList nodelist2 = node.SelectNodes("DTyp");
                      foreach (XmlNode node2 in nodelist2)
                      {
                          int Ref1 = Convert.ToInt32(node2.SelectSingleNode("Ref").InnerText);
                          Dictionary<int, int> Dico2 = new Dictionary<int, int>();
                          XmlNodeList nodelist3 = node2.SelectNodes("Int");
                          foreach (XmlNode node3 in nodelist3)
                          {
                              int FGr = Convert.ToInt32(node3.SelectSingleNode("FGr").InnerText);
                              int Ref2 = Convert.ToInt32(node3.SelectSingleNode("Ref").InnerText);
                              Dico2.Add(Ref2, FGr);
                          }
                          Dico1.Add(Ref1, Dico2);
                      }
                      _fareTypeMatrix.Add(Ref, Dico1);
                  }
                  catch (Exception e)
                  {
                      Logging.Log(LogLevel.Error, "Bad StationMatrix " + e.Message);
                      _faresError.SetAlarm(true);
                  }

                }
                return true;
            }

            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "FareParameters_FareTypeMatrix " + e.Message);
                throw (new Exception("****"));

            }
        }
          public static Dictionary<int, Dictionary<int, Dictionary<int, int>>> _globalfaretable = null;
          public static bool LoadFareTiersMatrixVersion(XmlElement root)
          {
              if (_globalfaretable == null) _globalfaretable = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
              else _globalfaretable.Clear();
              try
              {
                  XmlNodeList nodelist = root.SelectNodes("FareTables/GlobalFareTable/FGr");
                  foreach (XmlNode node in nodelist)
                  {
                      try
                      {
                          int Ref = Convert.ToInt32(node.SelectSingleNode("Ref").InnerText);
                          Dictionary<int, Dictionary<int, int>> Dico1 = new Dictionary<int, Dictionary<int, int>>();
                          XmlNodeList nodelist2 = node.SelectNodes("FTie");
                          foreach (XmlNode node2 in nodelist2)
                          {
                              int Ref1 = Convert.ToInt32(node2.SelectSingleNode("Ref").InnerText);
                              Dictionary<int, int> Dico2 = new Dictionary<int, int>();
                              XmlNodeList nodelist3 = node2.SelectNodes("Conc");
                              foreach (XmlNode node3 in nodelist3)
                              {
                                  int Ref2 = Convert.ToInt32(node3.SelectSingleNode("Ref").InnerText);
                                  int FGr = Convert.ToInt32(node3.SelectSingleNode("FVa").InnerText);
                                  Dico2.Add(Ref2, FGr);
                              }
                              Dico1.Add(Ref1, Dico2);
                          }
                          _globalfaretable.Add(Ref, Dico1);
                      }
                      catch (Exception e)
                      {
                          Logging.Log(LogLevel.Error, "Bad StationMatrix " + e.Message);
                          _faresError.SetAlarm(true);
                      }

                  }
                  return true;
              }
              catch (Exception e)
              {
                  Logging.Log(LogLevel.Error, "FareParameters_FareTiersMatrix " + e.Message);
                  throw (new Exception("****"));
              }
          }

        //private static Boolean IsFareTierLoaded = false;
        //private static Boolean IsFareGroupLoaded = false;
        //private static Boolean IsDayTypeCalendarLoaded = false;
        //private static Boolean IsGlobalFareTableLoaded = false;

        ////FareGroupTable [FareGroup] [Fare Tier]
        //private static int[][] FareGroupTable = new int[CONSTANT.MAX_NUMBER_OF_FGROUP][];

        ////FareTierMatix
        //private static Int16[,] FareTierMx = new Int16[CONSTANT.MAX_NUMBER_OF_STATIONS, CONSTANT.MAX_NUMBER_OF_STATIONS];

        ////GlobalFareTable
        //private static int[][] GlobalFareTable = new int[CONSTANT.MAX_NUMBER_OF_DTYPE][];

        //private static Int16[] DayTypeCalender = new Int16[CONSTANT.MAX_NUMBER_OF_CDAYS];

        
        //public static bool BuildFareTierMatrix()
        //{
        //    IsFareTierLoaded = false;

        //    Int16[] tmpValBuf = new Int16[CONSTANT.MAX_NUMBER_OF_STATIONS * CONSTANT.MAX_NUMBER_OF_STATIONS];

        //    try
        //    {
        //        //Create an instance of a ini file parser
        //        IniParser.FileIniDataParser parser = new FileIniDataParser();

        //        //Load the INI file which also parses the INI data
        //        IniData parsedData = parser.LoadFile("c:\\FareTierList.ini");

        //        //Iterate through all the sections
        //        foreach (SectionData section in parsedData.Sections)
        //        {
        //            //Iterate through all the keys in the current section
        //            //printing the values
        //            int i = 0;

        //            foreach (KeyData key in section.Keys)
        //            {
        //                tmpValBuf[i] = Convert.ToInt16(key.Value);
        //                i++;
        //            }
        //        }

        //        for (int i = 0; i < CONSTANT.MAX_NUMBER_OF_STATIONS; i++)
        //        {
        //            int k = 0;

        //            for (int j = i * CONSTANT.MAX_NUMBER_OF_STATIONS; j < (i * CONSTANT.MAX_NUMBER_OF_STATIONS) + CONSTANT.MAX_NUMBER_OF_STATIONS; j++)
        //            {
        //                FareTierMx[i, k] = tmpValBuf[j];
        //                k++;
        //            }
        //        }

        //        IsFareTierLoaded = true;

        //        return IsFareTierLoaded;
        //    }
        //    catch (Exception Ex)
        //    {
        //        Logging.Log(LogLevel.Error, "BuildFareTierMx: Data Error");

        //        return IsFareTierLoaded;
        //    }
        //}

        //public static bool BuildFareGroupTable()
        //{
        //    IsFareGroupLoaded = false;

        //    Int32[] tmpValBuf = new Int32[CONSTANT.MAX_NUMBER_OF_TIERS * CONSTANT.MAX_NUMBER_OF_FGROUP];

        //    try
        //    {
        //        //Create an instance of a ini file parser
        //        IniParser.FileIniDataParser parser = new FileIniDataParser();

        //        //Load the INI file which also parses the INI data
        //        IniData parsedData = parser.LoadFile("c:\\FareGroupTable.ini");

        //        //Iterate through all the sections
        //        foreach (SectionData section in parsedData.Sections)
        //        {
        //            //Iterate through all the keys in the current section
        //            //printing the values
        //            int i = 0;

        //            foreach (KeyData key in section.Keys)
        //            {
        //                tmpValBuf[i] = Convert.ToInt32(key.Value);
        //                i++;
        //            }
        //        }

        //        for (int i = 0; i < CONSTANT.MAX_NUMBER_OF_FGROUP; i++)
        //        {
        //            FareGroupTable[i] = new int[CONSTANT.MAX_NUMBER_OF_TIERS];

        //            int k = i * CONSTANT.MAX_NUMBER_OF_TIERS;

        //            for (int j = 0; j < CONSTANT.MAX_NUMBER_OF_TIERS; j++)
        //            {
        //                FareGroupTable[i][j] = tmpValBuf[k];
        //                k++;
        //            }    
        //        }

        //        IsFareGroupLoaded = true;

        //        return IsFareGroupLoaded;
        //    }
        //    catch (Exception Ex)
        //    {
        //        Logging.Log(LogLevel.Error, "BuildFareGroupTable: Data Error");

        //        return IsFareGroupLoaded;
        //    }
        //}

        //public static bool BuildDayTypeCalender()
        //{
        //    IsDayTypeCalendarLoaded = false;

        //    try
        //    {
        //        //Create an instance of a ini file parser
        //        IniParser.FileIniDataParser parser = new FileIniDataParser();

        //        //Load the INI file which also parses the INI data
        //        IniData parsedData = parser.LoadFile("c:\\DayTypeCalender.ini");

        //        //Iterate through all the sections
        //        foreach (SectionData section in parsedData.Sections)
        //        {
        //            //Iterate through all the keys in the current section
        //            //printing the values
        //            int i = 0;

        //            foreach (KeyData key in section.Keys)
        //            {
        //                DayTypeCalender[i] = Convert.ToInt16(key.Value);
        //                i++;
        //            }
        //        }

        //        IsDayTypeCalendarLoaded = true;

        //        return IsDayTypeCalendarLoaded;
        //    }
        //    catch (Exception Ex)
        //    {
        //        Logging.Log(LogLevel.Error, "BuildDayTypeCalender: Data Error");

        //        return IsDayTypeCalendarLoaded;
        //    }
        //}

        //public static bool BuildGlobalFareTable()
        //{
        //    IsGlobalFareTableLoaded = false;

        //    try
        //    {
        //        //Default : FareGroup 1 is loaded for all TicketTypes on all days , to be changed
        //        for (int i = 0; i < CONSTANT.MAX_NUMBER_OF_DTYPE; i++)
        //        {
        //            GlobalFareTable[i] = new int[CONSTANT.MAX_NUMBER_OF_TTYPE + 1];

        //            for (int j = 1; j <= CONSTANT.MAX_NUMBER_OF_TTYPE; j++)
        //            {
        //                GlobalFareTable[i][j] = 1;
        //            }
        //        }

        //        IsGlobalFareTableLoaded = true;

        //        return IsGlobalFareTableLoaded;
        //    }
        //    catch (Exception Ex)
        //    {
        //        Logging.Log(LogLevel.Error, "BuildGlobalFareTable: Data Error");

        //        return IsGlobalFareTableLoaded;
        //    }
        //}





    }
}
