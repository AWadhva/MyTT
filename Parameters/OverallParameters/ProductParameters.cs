using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public class ProductElement
    {
        public string Code;
        public Int32 Reference;
        public int Family;
        public int ServiceProvider;
        public bool Active;
        public List<int> Concession = null;
        public Dictionary<int, Dictionary<int, int>> FareType = null;
    }

   public static  class ProductParameters
    {

       public static int GetFareType(int product, int serviceProvider, int railCardType)
       {
           try
           {
               return _products[product].FareType[serviceProvider][railCardType];
           }
           catch
           {
               return -1;
           }
       }
       public static int GetProductFamily(int product)
       {
           try
           {
               return _products[product].Family;
           }
           catch
           {
               return -1;
           }
       }

       private static Dictionary<Int32, ProductElement> _products = null;


         public static bool LoadProductsVersion(XmlElement root)
        {

            if (_products == null) _products = new Dictionary<Int32, ProductElement>();
            else _products.Clear();
            try
            {
                XmlNodeList nodelist = root.SelectNodes("Products/Prd");
                foreach (XmlNode node in nodelist)
                {
                    try
                    {
                        ProductElement pp = new ProductElement();
                        pp.Code = node.SelectSingleNode("Code").InnerText;
                        pp.Reference = Convert.ToInt32(node.SelectSingleNode("Ref").InnerText);
                        pp.Active = Convert.ToBoolean(Convert.ToInt32(node.SelectSingleNode("Act").InnerText));
                        pp.Family = Convert.ToInt32(node.SelectSingleNode("Fam").InnerText);
                        pp.Concession = new List<int>();
                        pp.Concession.Add(1);
                        XmlNodeList nodelist2 = node.SelectNodes("SP");
                        pp.FareType = new Dictionary<int, Dictionary<int, int>>();
                        foreach (XmlNode node2 in nodelist2)
                        {
                            Dictionary<int, int> dico1 = new Dictionary<int, int>();
                            int ref1 = Convert.ToInt32(node2.SelectSingleNode("Ref").InnerText);

                            XmlNodeList nodelist3 = node2.SelectNodes("RT");
                            foreach (XmlNode node3 in nodelist3)
                            {
                                int ref2 = Convert.ToInt32(node3.SelectSingleNode("Ref").InnerText);
                                int ft = Convert.ToInt32(node3.SelectSingleNode("FTyp").InnerText);
                                dico1.Add(ref2, ft);
                            }
                            pp.FareType.Add(ref1, dico1);
                        }
                        _products.Add(pp.Reference, pp);
                    }
                     catch (Exception e)
                    {
                        Logging.Log(LogLevel.Error, "Bad Products " + e.Message);
                        FareParameters._faresError.SetAlarm(true);
                    }
                }
                return true;
            }
              catch (Exception e)
              {
                  Logging.Log(LogLevel.Error, "FareParameters_LoadProducts " + e.Message);
                  throw (new Exception("****"));

              }
        }

    }


    }

 