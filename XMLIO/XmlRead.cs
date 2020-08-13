using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using NUnit.Framework;

[assembly: InternalsVisibleTo("XMLIO.Tests")]

namespace XMLIO
{
    public class XmlReadTemplates<T> where T : new()
    {
        public delegate void ParseAttribute(XmlReader xr, T t);
        public delegate T CreateFromXmlElement(XmlReader xr);

        /*----------------------------------------------------------------------------
        	%%Function: ReadListOfSingleElements
        	%%Qualified: XMLIO.XmlReadTemplates<T>.ReadListOfSingleElements
        	
        ----------------------------------------------------------------------------*/
        public static List<T> ReadListOfSingleElements(XmlReader xr, string sParentElement, string sElement, CreateFromXmlElement createFromXmlElement)
        {
            if (xr.Name != sParentElement)
                throw new Exception($"not at correct location to read {sParentElement}");

            if (xr.IsEmptyElement)
                return null;

            List<T> t = new List<T>();

            xr.ReadStartElement();

            while (true)
            {
                XmlNodeType nt = xr.NodeType;

                if (nt == XmlNodeType.Element)
                {
                    if (xr.Name == sElement)
                    {
                        t.Add(createFromXmlElement(xr));
                        continue;
                    }

                    throw new Exception($"unknown element {xr.Name} under {sParentElement} element");
                }

                if (nt == XmlNodeType.EndElement)
                {
                    if (xr.Name == sParentElement)
                    {
                        xr.ReadEndElement();
                        return t;
                    }

                    throw new Exception($"unmatched {sParentElement} element with {xr.Name}");
                }

                if (!xr.Read())
                    throw new Exception("xml read ended before {sParentElement closed");
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: ParseSingleElementWithAttributes
        	%%Qualified: XMLIO.XmlReadTemplates<T>.ParseSingleElementWithAttributes
        	
        ----------------------------------------------------------------------------*/
        public static T ParseSingleElementWithAttributes(XmlReader xr, string sElement, ParseAttribute parseAttribute)
        {
            T t = new T();

            if (xr.Name != sElement)
                throw new Exception($"not at correct location to read {sElement}");

            bool fEmptyElement = xr.IsEmptyElement;

            XmlIO.Read(xr); // read including attributes

            while (true)
            {
                XmlIO.SkipNonContent(xr);
                XmlNodeType nt = xr.NodeType;

                // PUT MMS children here
                if (nt == XmlNodeType.Element)
                    throw new Exception($"unexpected element {xr.Name} under {sElement} element");

                if (nt == XmlNodeType.EndElement)
                {
                    if (xr.Name != sElement)
                        throw new Exception($"unmatched {sElement} element");

                    xr.ReadEndElement();
                    break;
                }

                if (xr.NodeType != XmlNodeType.Attribute)
                    throw new Exception($"unexpected non attribute on <{sElement}> element");

                while (true)
                {
                    // consume all the attributes
                    parseAttribute(xr, t);
                    if (!xr.MoveToNextAttribute())
                    {
                        if (fEmptyElement)
                        {
                            xr.Read(); // get past the attribute
                            return t;
                        }

                        break; // continue till we find the end element
                    }

                    // otherwise just continue...
                }

                if (!XmlIO.Read(xr))
                    throw new Exception($"never encountered end {sElement} element");
            }

            return t;
        }
    }

    public class XmlIO
    {
        public static bool FIsContentNode(XmlNodeType nt)
        {
            if (nt == XmlNodeType.Attribute
                || nt == XmlNodeType.Element
                || nt == XmlNodeType.EndElement
                || nt == XmlNodeType.EntityReference
                || nt == XmlNodeType.CDATA
                || nt == XmlNodeType.SignificantWhitespace
                || nt == XmlNodeType.Text)
            {
                return true;
            }
            return false;
        }

        /*----------------------------------------------------------------------------
        	%%Function: SkipNonContent
        	%%Qualified: XMLIO.XmlIO.SkipNonContent
        	
            Skip over all the nodes that should be ignore when just parsing for
            elements/attributes/text content
        ----------------------------------------------------------------------------*/
        public static void SkipNonContent(XmlReader xr)
        {
            while (!FIsContentNode(xr.NodeType))
            {
                if (!xr.MoveToNextAttribute() && !xr.Read())
                    break;
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: Read
        	%%Qualified: XMLIO.XmlIO.Read
        	
            Read, including reading an attribute if its there
        ----------------------------------------------------------------------------*/
        public static bool Read(XmlReader xr)
        {
            bool f = xr.MoveToNextAttribute();

            if (f)
                return f;

            return xr.Read();
        }

        /*----------------------------------------------------------------------------
        	%%Function: StringElementReadFromXml
        	%%Qualified: XMLIO.XmlIO.StringElementReadFromXml
        ----------------------------------------------------------------------------*/
        public static string StringElementReadFromXml(XmlReader xr)
        {
            return xr.ReadElementContentAsString("string", "");
        }


        /*----------------------------------------------------------------------------
        	%%Function: ReadGenericStringElement
        	%%Qualified: XMLIO.XmlIO.ReadGenericStringElement
        	
            read the givent element as a string
        ----------------------------------------------------------------------------*/
        public static string ReadGenericStringElement(XmlReader xr, string sElement)
        {
            if (xr.Name != sElement)
                throw new Exception("not at the correct node");

            if (xr.NodeType == XmlNodeType.Attribute)
            {
                return xr.Value;
            }

            if (xr.IsEmptyElement)
            {
                xr.ReadStartElement(); // since this is both the start and an empty element
                return null;
            }

            // read for the child
            string s = xr.ReadElementContentAsString().Trim();
            if (String.IsNullOrEmpty(s))
                return null;

            // ReadElementContentAsString advances past the end element, so the parse should
            // be all set. 
            return s;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReadGenericNullableStringElement
        	%%Qualified: XMLIO.XmlIO.ReadGenericNullableStringElement
        	
            Same as ReadGenericStringElement, but recognizes that "null" represents
            the null value.
        ----------------------------------------------------------------------------*/
        public static string ReadGenericNullableStringElement(XmlReader xr, string sElement)
        {
            string s = ReadGenericStringElement(xr, sElement);

            if (s == "null")
                return null;

            return s;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ConvertElementStringToInt
        	%%Qualified: XMLIO.XmlIO.ConvertElementStringToInt
        	
            convert the given string into an integer
        ----------------------------------------------------------------------------*/
        internal static int? ConvertElementStringToInt(string sElementString)
        {
            if (sElementString != null && sElementString != "null")
                return Int32.Parse(sElementString);

            return null;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReadGenericIntElement
        	%%Qualified: XMLIO.XmlIO.ReadGenericIntElement
        	
            read the given element as an integer
        ----------------------------------------------------------------------------*/
        public static int? ReadGenericIntElement(XmlReader xr, string sElement)
        {
            return ConvertElementStringToInt(ReadGenericStringElement(xr, sElement));
        }

        /*----------------------------------------------------------------------------
        	%%Function: ConvertElementStringToUInt64
        	%%Qualified: XMLIO.XmlIO.ConvertElementStringToUInt64
        	
        ----------------------------------------------------------------------------*/
        internal static UInt64? ConvertElementStringToUInt64(string sElementString)
        {
            if (sElementString != null)
                return UInt64.Parse(sElementString);

            return null;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReadGenericUInt64Element
        	%%Qualified: XMLIO.XmlIO.ReadGenericUInt64Element
        	
        ----------------------------------------------------------------------------*/
        public static UInt64? ReadGenericUInt64Element(XmlReader xr, string sElement)
        {
            return ConvertElementStringToUInt64(ReadGenericStringElement(xr, sElement));
        }

        /*----------------------------------------------------------------------------
        	%%Function: ConvertElementStringToBool
        	%%Qualified: XMLIO.XmlIO.ConvertElementStringToBool
        	
            convert the given string into a bool
        ----------------------------------------------------------------------------*/
        public static bool? ConvertElementStringToBool(string sElementString)
        {
            if (sElementString == null)
                return null;

            if (sElementString == "0" || sElementString == "false")
                return false;
            if (sElementString == "1" || sElementString == "true")
                return true;

            throw new FormatException($"{sElementString} is not a boolean value");
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReadGenericBoolElement
        	%%Qualified: XMLIO.XmlIO.ReadGenericBoolElement
        	
            Read the given element as a boolean
        ----------------------------------------------------------------------------*/
        public static bool? ReadGenericBoolElement(XmlReader xr, string sElement)
        {
            return ConvertElementStringToBool(ReadGenericStringElement(xr, sElement));
        }

        /*----------------------------------------------------------------------------
        	%%Function: RecepientsReadElement
        	%%Qualified: XMLIO.XmlIO.RecepientsReadElement
        	
            this is either empty or has a single <string> element as a child
        ----------------------------------------------------------------------------*/
        public static string[] RecepientsReadElement(XmlReader xr)
        {
            if (xr.Name != "Recepients")
                throw new Exception("not at the correct node");

            if (xr.IsEmptyElement)
            {
                xr.ReadStartElement(); // since this is both the start and an empty element
                return null;
            }

            List<string> pls = new List<string>();
            // read for the child
            while (xr.Read())
            {
                XmlNodeType nt = xr.NodeType;

                if (nt == XmlNodeType.EndElement)
                {
                    if (xr.Name != "Recepients")
                        throw new Exception("encountered end node not matching <Recepients>");

                    // this just means that it had child text nodes that didn't matter (like whitespace or comments)
                    // its ok, just advance reader past it and return
                    xr.ReadEndElement();
                    return null;
                }

                if (nt == XmlNodeType.Element)
                {
                    while (true)
                    {
                        string s = XmlIO.StringElementReadFromXml(xr).Trim();
                        SkipNonContent(xr);

                        pls.Add(s);

                        // now we should be at the EndElement for Recepients
                        if (xr.Name == "Recepients")
                        {
                            xr.ReadEndElement();
                            return pls.ToArray();
                        }

                        if (xr.Name != "string")
                            throw new Exception("not at the correct node");
                    }
                }
            }

            throw new Exception("didn't find string child in recepients");
        }
    }
}
