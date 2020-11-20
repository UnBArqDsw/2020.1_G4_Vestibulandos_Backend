using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Common.Util
{
    public class XMLHelper
    {
        /// <summary>
        /// Save the XML stream node.
        /// </summary>
        public static Dictionary<string, XElement> XmlInfo;

        /// <summary>
        /// Load XElement type system parameters and settings and cache
        /// </summary>
        /// <param name="key">Key name in the dictionary</param>
        /// <param name="element">XElement</param>
        public static void AddXElement(string key, XElement element)
        {
            if (!XmlInfo.ContainsKey(key))
            {
                XmlInfo.Add(key, element);
            }
        }

        /// <summary>
        /// Get a list of XElement file tree node segments XElement
        /// </summary>
        /// <param name="XElement">XElement file carrier</param>
        /// <param name="newroot">Independent node to find</param>
        /// <returns>XElement list</returns>
        public static List<XElement> GetXElementList(XElement xElement, string newroot)
        {
            List<XElement> xmlItemList = new List<XElement>();

            if (xElement != null)
            {
                IEnumerable<XElement> xmlItems = null;
                if ("*" == newroot)
                {
                    xmlItems = xElement.Elements();
                }
                else
                {
                    xmlItems = xElement.DescendantsAndSelf(newroot);
                }
                if (xmlItems != null)
                {
                    IEnumerator<XElement> en_xmlItems = xmlItems.GetEnumerator();
                    while (en_xmlItems.MoveNext())
                    // foreach (var xmlItem in xmlItems)
                    {
                        // xmlItemList.Insert(xmlItemList.Count, xmlItem);
                        xmlItemList.Insert(xmlItemList.Count, en_xmlItems.Current);
                    }
                }
            }

            return xmlItemList;
        }

        /// <summary>
        /// Get the node path for the specified xml node
        /// </summary>
        /// <param name="element"></param>
        public static string GetXElementNodePath(XElement element)
        {
            try
            {
                string path = element.Name.ToString();
                element = element.Parent;

                while (null != element)
                {
                    path = element.Name.ToString() + "/" + path;
                    element = element.Parent;
                }

                return path;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Get XML file tree node segment XElement
        /// </summary>
        /// <param name="XML">XML file carrier</param>
        /// <param name="newroot">Independent node to find</param>
        ///<returns></returns>
        public static XElement GetXElement(XElement XML, string newroot)
        {
            return XML.DescendantsAndSelf(newroot).Single();
        }

        /// <summary>
        /// Get XML file tree node segment XElement
        /// </summary>
        /// <param name="XML">XML file carrier</param>
        /// <param name="newroot">Independent node to find</param>
        /// <returns></returns>
        public static XElement GetSafeXElement(XElement XML, string newroot)
        {
            try
            {
                return XML.DescendantsAndSelf(newroot).Single();
            }
            catch (Exception)
            {
                throw new Exception($"Read: {newroot} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get XML file tree node segment XElement
        /// </summary>
        /// <param name="xml">XML file carrier</param>
        /// <param name="mainnode">Primary node to find</param>
        /// <param name="attribute">Primary node condition attribute name</param>
        /// <param name="value">Primary node condition attribute value</param>
        /// <returns></returns>
        public static XElement GetXElement(XElement XML, string newroot, string attribute, string value)
        {
            return XML.DescendantsAndSelf(newroot).Single(X => X.Attribute(attribute).Value == value);
        }

        /// <summary>
        /// Get XML file tree node segment XElement
        /// </summary>
        /// <param name="xml">XML file carrier</param>
        /// <param name="mainnode">Primary node to find</param>
        /// <param name="attribute">Primary node condition attribute name</param>
        /// <param name="value">Primary node condition attribute value</param>
        /// <returns></returns>
        public static XElement GetSafeXElement(XElement XML, string newroot, string attribute, string value)
        {
            try
            {
                return XML.DescendantsAndSelf(newroot).Single(X => X.Attribute(attribute).Value == value);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Read: {newroot}/{attribute}={value} failed, xml node name: {GetXElementNodePath(XML)} " + ex.Message);
            }
        }

        /// <summary>
        /// Get the property value
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static XAttribute GetSafeAttribute(XElement XML, string attribute)
        {
            try
            {
                XAttribute attrib = XML.Attribute(attribute);
                if (null == attrib)
                {
                    throw new Exception(
                        $"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
                }

                return attrib;
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get property value (string)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static string GetSafeAttributeStr(XElement XML, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            return (string)attrib;
        }

        /// <summary>
        /// Get Property Value (String) Returns the default value if it can not be retrieved
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static string GetDefAttributeStr(XElement XML, string attribute, string strdef)
        {
            XAttribute attrib = XML.Attribute(attribute);
            if (null == attrib)
                return strdef;

            return (string)attrib;
        }

        /// <summary>
        /// Get property value (integer)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static long GetSafeAttributeLong(XElement XML, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (null == str || str == "") return -1;

            try
            {
                return (long)Convert.ToDouble(str);
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get property value (integer)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static ulong GetSafeAttributeULong(XElement XML, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (null == str || str == "") return 0;

            try
            {
                return Convert.ToUInt64(str);
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get property value (integer)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static short GetSafeAttributeShort(XElement XML, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (null == str || str == "") return -1;

            try
            {
                return Convert.ToInt16(str);
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get property value (integer)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static int GetSafeAttributeInt(XElement XML, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (null == str || str == "") return -1;

            try
            {
                return Convert.ToInt32(str);
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        ///Get a 32-bit integer array of attributes
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int[] GetSafeAttributeIntArray(XElement XML, string attribute, int length = -1)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;

            if (string.IsNullOrEmpty(str))
                return null;
            try
            {
                string[] args = str.Split(',');
                if (length > args.Length && length != args.Length)
                {
                    return null;
                }
                return DataHelper.StringArray2IntArray(args);
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }
        /// <summary>
        /// Get property value (integer array)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static long[] GetSafeAttributeLongArray(XElement XML, string attribute, int length = -1)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (string.IsNullOrEmpty(str))
                return null;

            try
            {
                string[] args = str.Split(',');
                if (length > args.Length && length != args.Length)
                {
                    return null;
                }

                long[] result = new long[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    result[i] = DataHelper.SafeConvertToInt64(args[i]);
                }

                return result;
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get property value (integer array)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static double[] GetSafeAttributeDoubleArray(XElement XML, string attribute, int length = -1)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (string.IsNullOrEmpty(str))
                return null;

            try
            {
                string[] args = str.Split(',');
                if (length > args.Length && length != args.Length)
                {
                    return null;
                }

                double[] result = new double[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    result[i] = DataHelper.SafeConvertToDouble(args[i]);
                }

                return result;
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get property value (floating point)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static double GetSafeAttributeDouble(XElement XML, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, attribute);
            string str = (string)attrib;
            if (string.IsNullOrEmpty(str))
                return 0.0;

            try
            {
                return Convert.ToDouble(str);
            }
            catch (Exception)
            {
                throw new Exception($"Read attribute: {attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get xml property value
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="root"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static XAttribute GetSafeAttribute(XElement XML, string root, string attribute)
        {
            try
            {
                XAttribute attrib = XML.Element(root).Attribute(attribute);
                if (null == attrib)
                {
                    throw new Exception(
                        $"Read property: {root}/{attribute} failed, xml node name: {GetXElementNodePath(XML)}");
                }

                return attrib;
            }
            catch (Exception)
            {
                throw new Exception(
                    $"Read property: {root}/{attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get xml attribute value (string)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="root"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static string GetSafeAttributeStr(XElement XML, string root, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, root, attribute);
            return (string)attrib;
        }

        /// <summary>
        /// Get xml property value (integer value)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="root"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static long GetSafeAttributeLong(XElement XML, string root, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, root, attribute);
            string str = (string)attrib;
            if (string.IsNullOrEmpty(str))
                return -1;

            try
            {
                return (long)Convert.ToDouble(str);
            }
            catch (Exception)
            {
                throw new Exception(
                    $"Read property: {root}/{attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get xml property value (integer value)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="root"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static ulong GetSafeAttributeULong(XElement XML, string root, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, root, attribute);
            string str = (string)attrib;
            if (string.IsNullOrEmpty(str))
                return 0;

            try
            {
                return Convert.ToUInt64(str);
            }
            catch (Exception)
            {
                throw new Exception(
                    $"Read property: {root}/{attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }

        /// <summary>
        /// Get xml attribute value (floating point)
        /// </summary>
        /// <param name="XML"></param>
        /// <param name="root"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static double GetSafeAttributeDouble(XElement XML, string root, string attribute)
        {
            XAttribute attrib = GetSafeAttribute(XML, root, attribute);
            string str = (string)attrib;
            if (string.IsNullOrEmpty(str))
                return -1;

            try
            {
                return Convert.ToDouble(str);
            }
            catch (Exception)
            {
                throw new Exception(
                    $"Read property: {root}/{attribute} failed, xml node name: {GetXElementNodePath(XML)}");
            }
        }
    }
}
