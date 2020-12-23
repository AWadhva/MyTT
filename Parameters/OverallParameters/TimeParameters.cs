using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class CalendarElement
    {
        public DateTime StartDate;
        public int DaysNumber;
        public Int32 Reference;
        public Dictionary<int, int> DayTypes = new Dictionary<int, int>();
    }

    public class TimeIntervalElement
    {
        public int DefinitionRule;
        public Int32 Reference;
        public List<TimeSpan> Time;
    }

    public static class TimeParameters
    {
        static readonly bool _bManipulateInputDateForDayTypeCalculation = Configuration.ReadBoolParameter("ManipulateInputDateForDayTypeCalculation", true);

        public static int GetDayType(int calendar,DateTime pDate)
        {
            try
            {
                if (_bManipulateInputDateForDayTypeCalculation)
                    pDate = DatesUtility.BusinessDay(pDate, new DateTime(2010, 1, 1, 2, 0, 0)).AddHours(3);
                if (pDate < _calends[calendar].StartDate) return (-1);
                if (pDate >= (_calends[calendar].StartDate.AddDays(_calends[calendar].DaysNumber)))
                {
                    if (_iDefaultUsedIfCalendarElapsed > 0) return _iDefaultUsedIfCalendarElapsed;
                    return -1;
                }
                int nbrOfDays = pDate.Subtract(_calends[calendar].StartDate).Days;
                return _calends[calendar].DayTypes[nbrOfDays];
            }
            catch
            {
                return -1;
            }
        }

        public static int GetIntervalType(int interval, DateTime pDate)
        {
            try
            {
                int i = 0;
                TimeSpan ts = new TimeSpan(pDate.Hour,pDate.Minute,pDate.Second);
                foreach (TimeSpan tsl in _times[interval].Time)
                {
                    if (ts <= tsl) return (i);
                }
                return (i);
            }
            catch
            {
                return -1;
            }
        }

        private static Dictionary<Int32, TimeIntervalElement> _times = null;
            private static Dictionary<Int32, CalendarElement> _calends = null;
            private static int _iDefaultUsedIfCalendarElapsed = Configuration.ReadIntParameter("DefaultUsedIfCalendarElapsed", 0);


            public static bool LoadCalendarsVersion(XmlElement root)
            {
                if (_calends == null) _calends = new Dictionary<Int32, CalendarElement>();
                else _calends.Clear();

                try
                {
                    XmlNodeList nodelist = root.SelectNodes("Calendars/Calendar");
                    foreach (XmlNode node in nodelist)
                    {
                        try
                        {
                            CalendarElement cl = new CalendarElement();
                            cl.DaysNumber = Convert.ToInt32(node.SelectSingleNode("DaysNumber").InnerText);
                            cl.StartDate = Convert.ToDateTime(node.SelectSingleNode("StartDate").InnerText);
                            cl.Reference = 1;
                            // Dictionary conversion
                            XmlNodeList nodeList2 = node.SelectNodes("Day");
                            foreach (XmlNode node2 in nodeList2)
                            {
                                int key = Convert.ToInt32(node2.SelectSingleNode("Offs").InnerText);
                                int value = Convert.ToInt32(node2.SelectSingleNode("Type").InnerText);
                                cl.DayTypes.Add(key,value);
                            }
                            _calends.Add(cl.Reference, cl);
                        }
                        catch (Exception e)
                        {
                            Logging.Log(LogLevel.Error, "Bad Calendar " + e.Message);
                            FareParameters._faresError.SetAlarm(true);
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                      Logging.Log(LogLevel.Error, "FareParameters_LoadCalendars " + e.Message);
                      throw (new Exception("****"));
                }
            }

        public static bool LoadTimeIntervalsVersion(XmlElement root)
        {
            if (_times == null) _times = new Dictionary<Int32, TimeIntervalElement>();
            else _times.Clear();

            try
            {
                XmlNodeList nodelist = root.SelectNodes("TimeIntervals/TimeInterval");
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        TimeIntervalElement tmi = new TimeIntervalElement();
                        tmi.DefinitionRule = Convert.ToInt32(node.SelectSingleNode("DefinitionRule").InnerText);
                        tmi.Time = new List<TimeSpan>();
                        tmi.Reference = 1;
                        XmlNodeList nodeList2 = node.SelectNodes("Time");
                        foreach (XmlNode node2 in nodeList2)
                        {
                            DateTime key = Convert.ToDateTime(node2.SelectSingleNode("Start").InnerText);
                            TimeSpan ts = new TimeSpan(key.Hour,key.Minute,key.Second);
                            tmi.Time.Add(ts);
                        }
                        _times.Add(tmi.Reference, tmi);                        
                    }
                    catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad TimeInterval" + e.Message);
                        FareParameters._faresError.SetAlarm(true);
                        return false;
                    }

                }
                FareParameters._faresError.SetAlarm(false);
                return true;
            }
            catch (Exception e)
            {
                  Logging.Log(LogLevel.Error, "FareParameters_LoadTimeInterval " + e.Message);
                  try
                  {
                      FareParameters._faresError.SetAlarm(true);
                  }
                  catch { }
                  throw (new Exception("****"));                  
            }
        }
    }
}
