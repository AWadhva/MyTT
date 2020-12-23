using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;

using IFS2.Equipment.Common;

namespace IFS2.Equipment.TicketingRules
{
    public static class TicketsSaleParameters
    {
        public static OneEvent _ticketSaleParametersMissing = null;
        public static OneEvent _ticketSaleParametersError = null;
        public static OneEvent _ticketSaleParametersActivation = null;

        static TicketsSaleParameters()
        {
            _ticketSaleParametersMissing = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 39, "TicketSaleParametersMissing", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "TicketSaleParametersMissing", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _ticketSaleParametersError = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 40, "TicketSaleParametersError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "TicketSaleParametersError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
            _ticketSaleParametersActivation = new OneEvent((int)StatusConsts.TTComponent, "TTComponent", 41, "TicketSaleParametersActivationError", "", Configuration.ReadTypeValueFromDictionaries<AlarmStatus>("EODAlarmLevelStatus", "TicketSaleParametersActivationError", AlarmStatus.Alarm), OneEvent.OneEventType.StorageAlarm);
        }

        public static void Start()
        {
        }
        public static bool Initialise()
        {
            return LoadVersion(BaseParameters.Initialise("TicketSaleDefinitionList"));
        }

        public static bool Save(string content)
        {
            return BaseParameters.Save(content, "TicketSaleDefinitionList");
        }

        public static bool LoadVersion(string content)
        {
            return LoadFromXml(content);
        }

        public enum TicketValidityUnit
        {
            None,
            Day,
            Month,
            Year
        }

        public enum SalePriceType
        {
            OriginDestination=0,
            Fixed=1
        }

        public enum MediaType
        {
            RedToken,
            BlueToken,
            CSC
        }

        static Dictionary<int, OneProductSpecs> productSpecs;

        public static OneProductSpecs GetSpecsFor(int productId)
        {
            OneProductSpecs specs = null;
            productSpecs.TryGetValue(productId, out specs);
            return specs;
        }

        private static bool bInitialized = false;
        public static bool Initialized
        {
            get { return bInitialized; }
        }

        public static string GetListProductsForSale(Languages language)
        {
            try
            {
                EODGetListProductsForSale result = new EODGetListProductsForSale();
                foreach (int prod in productSpecs.Keys)
                {
                    if (IsOpen(prod))
                    {
                        //Product shall not be SV also
                        OneProductSpecs product = productSpecs[prod];
                        if (prod >= 20)
                        {
                            EODDetailOneProductForSale det = new EODDetailOneProductForSale();
                            det.Code = prod;
                            string s = ParametersDico.LongText("Products", prod, language);
                            if (s != "" && s != Consts.NoTextFoundString) det.Name = s;
                            else det.Name = product.name;
                            det.SalePriceType = (int)product.salePrice.typ;
                            det.Price = product.salePrice.Val;
                            result.Products.Add(det);
                        }
                    }
                }
                if (result.Products.Count > 0)
                {
                    return SerializeHelper<EODGetListProductsForSale>.XMLSerialize(result);
                }
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "TopologyParameters.GetListLines " + e.Message);
            }
            return "";
        }




        public static bool IsReadCardIssueDetailsRequired(int productId)
        {
            try
            {
                var specs = GetSpecsFor(productId);
                if (specs._MediaType == MediaType.BlueToken || specs._MediaType == MediaType.RedToken)
                    return true;
                /*
                if (specs._MaximumAddValue == 0
                    && specs._MaximumStoredValue == 0
                    && specs._MinimumAddValue == 0
                    && specs._StepsOfAddValue == 0)
                    return true;*/
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool LoadFromXml(string xml)
        {
            try
            {
                string s = xml.Substring(0,200);
                Logging.Log(LogLevel.Verbose, "TicketSaleParameter.LoadVersion " + s);
                bInitialized = false;
                productSpecs = null; 
                var productSpecsTemp = new Dictionary<int, OneProductSpecs>();

                XElement xRootElem = XElement.Parse(xml);
                IEnumerable<XElement> xmlProducts = xRootElem.Elements("Products");

                foreach (var xmlProduct in xmlProducts)
                {
                    var productId = Int32.Parse(xmlProduct.Element("id").Value);

                    OneProductSpecs oneProduct = new OneProductSpecs();
                    oneProduct.name = xmlProduct.Element("Name").Value;
                    oneProduct.isOpen = Boolean.Parse((xmlProduct.Element("IsOpen").Value));                    
                    oneProduct.defaultAddValue = Int32.Parse((xmlProduct.Element("DefaultAddValue").Value));
                    oneProduct.minimumAddValue = Int32.Parse((xmlProduct.Element("MinimumAddValue").Value));
                    oneProduct.maximumAddValue = Int32.Parse((xmlProduct.Element("MaximumAddValue").Value));
                    oneProduct.stepsOfAddValue = Int32.Parse((xmlProduct.Element("StepsOfAddValue").Value));
                    oneProduct.maximumStoredValue = Int32.Parse((xmlProduct.Element("MaximumStoredValue").Value));
                    try
                    {
                        oneProduct.deposit = Int32.Parse((xmlProduct.Element("Deposit").Value));
                        oneProduct.refundAuthorized = Boolean.Parse((xmlProduct.Element("RefundAuthorized").Value));
                        oneProduct.refundCharge = Int32.Parse((xmlProduct.Element("RefundCharge").Value));
                    }
                    catch { // Don't do anything because they are optional for TVM
                    }
                    {
                        oneProduct.validity = new OneProductSpecs.Validity();
                        
                        int temp = Int32.Parse((xmlProduct.Element("TicketValidityType").Value));
                        if (!Enum.IsDefined(typeof(TicketValidityUnit), temp))
                            throw new Exception("Unknown TicketValidityType");
                        oneProduct.validity.unit = (TicketValidityUnit)temp;

                        oneProduct.validity.nVal = Int32.Parse((xmlProduct.Element("TicketValidityVal").Value));
                    }

                    try
                    {
                        oneProduct.salePrice = new OneProductSpecs.SalePrice();

                        int temp = Int32.Parse((xmlProduct.Element("SalePriceType").Value));
                        if (!Enum.IsDefined(typeof(SalePriceType), temp))
                            throw new Exception("Unknown SalePriceType");
                        oneProduct.salePrice.typ = (SalePriceType)temp;

                        oneProduct.salePrice.nVal = Int32.Parse((xmlProduct.Element("SalePriceVal").Value));
                    }
                    catch
                    {
                        // Don't do anything because they are optional for TVM
                    }
                    {
                        int temp = Int32.Parse((xmlProduct.Element("MediaType").Value));
                        if (!Enum.IsDefined(typeof(MediaType), temp))
                            throw new Exception("Unknown MediaType");
                        oneProduct.mediaType = (MediaType)temp;
                    }

                    // TODO: When implement this function appropriatly, then uncomment
//                    if (!SanityCheck(oneProduct))
//                        continue; // TODO: to see if we should simply throw exception instead of moving to next fare product
                    productSpecsTemp[productId] = oneProduct;
                }
                productSpecs = productSpecsTemp;

                bInitialized = true;
                return true;
            }
            catch (Exception e)
            {
                Logging.Log(LogLevel.Error, "LoadTicketSaleParameter " + e.Message);
                return false;
            }
        }

        private static bool SanityCheck(OneProductSpecs oneProduct)
        {
            if (oneProduct._DefaultAddValue == 0
                && oneProduct._MinimumAddValue == 0)
                return false;
            if (oneProduct._DefaultAddValue < 0
                || oneProduct._MaximumAddValue < 0
                || oneProduct._MaximumStoredValue < 0
                || oneProduct._MinimumAddValue < 0
                || oneProduct._RefundCharge < 0
                || oneProduct._SalePrice.nVal < 0
                || oneProduct._StepsOfAddValue < 0
                || oneProduct._Validity.nVal < 0
                )
                return false;
            return true;
        }

        public static List<int> AddValuesFeasible(int productId, int currentValInFareProd)
        {
            if (!IsAddValueSupported(productId))
                throw new AddValueNotSupportedException();

            var specs = GetSpecsFor(productId);
            var result = new List<int>();

            if (specs._DefaultAddValue != 0)
            {
                result.Add(specs._DefaultAddValue);
                return result;
            }

            int newVal = currentValInFareProd + specs._MinimumAddValue;
            int addVal = specs._MinimumAddValue;
            while(true)
            {
                if (newVal <= specs._MaximumStoredValue && addVal <= specs._MaximumAddValue)
                    result.Add(addVal);
                else
                    break;

                newVal += specs._StepsOfAddValue;
                addVal += specs._StepsOfAddValue;
            }

            return result;
        }

        public static bool IsAddValueSupported(int productId)
        {
            try
            {
                var specs = GetSpecsFor(productId);
                if (!specs._IsOpen
                    || specs.salePrice.typ == SalePriceType.OriginDestination // this may be removed.
                    )
                    return false;

                if (specs.defaultAddValue == 0 && specs.minimumAddValue == 0)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsOpen(int productId)
        {
            try
            {
                var specs = GetSpecsFor(productId);
                if (!specs._IsOpen)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public class AddValueNotSupportedException : Exception
        {
            public override string Message
            {
                get
                {
                    return "Add valued not supported on this product";
                }
            }
        }

        public class OneProductSpecs
        {
            internal string name;

            internal bool isOpen = false;
            public bool _IsOpen
            {
                get { return isOpen; }
            }

            internal int defaultAddValue;
            public int _DefaultAddValue
            {
                get { return defaultAddValue * 100; }
            }

            internal int minimumAddValue = 0;
            public int _MinimumAddValue
            {
                get { return minimumAddValue * 100; }
            }

            internal int maximumAddValue = 0;
            public int _MaximumAddValue
            {
                get { return maximumAddValue * 100; }
            }

            internal int stepsOfAddValue = 0;
            public int _StepsOfAddValue
            {
                get { return stepsOfAddValue * 100; }
            }

            internal int maximumStoredValue = 0;
            public int _MaximumStoredValue
            {
                get { return maximumStoredValue * 100; }
            }

            internal int deposit = 0;
            public int _Deposit
            {
                get { return deposit * 100; }
            }

            internal bool refundAuthorized = false;
            public bool _RefundAuthorized
            {
                get { return refundAuthorized; }
            }

            internal int refundCharge = 0;
            public int _RefundCharge
            {
                get { return refundCharge * 100; }
            }

            public class Validity
            {
                internal TicketValidityUnit unit;
                public TicketValidityUnit ticketValidityUnit
                {
                    get { return unit; }
                }

                internal int nVal;
                public int Val
                {
                    get { return nVal; }
                }
            }

            internal Validity validity;
            public Validity _Validity
            {
                get { return validity; }
            }

            public class SalePrice
            {
                internal SalePriceType typ;
                public SalePriceType _SalePriceType
                {
                    get { return typ; }
                }

                internal int nVal;
                public int Val
                {
                    get { return nVal * 100; }
                }
            }

            internal SalePrice salePrice;
            public SalePrice _SalePrice
            {
                get { return salePrice; }
            }

            internal MediaType mediaType;
            public MediaType _MediaType
            {
                get { return mediaType; }
            }
        }
    }
}
