using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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
				throw new System.Exception($"not at correct location to read {sParentElement}");

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

					throw new System.Exception($"unknown element {xr.Name} under {sParentElement} element");
				}

				if (nt == XmlNodeType.EndElement)
				{
					if (xr.Name == sParentElement)
					{
						xr.ReadEndElement();
						return t;
					}

					throw new System.Exception($"unmatched {sParentElement} element with {xr.Name}");
				}

				if (!xr.Read())
					throw new System.Exception("xml read ended before {sParentElement closed");
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
				throw new System.Exception($"not at correct location to read {sElement}");

			bool fEmptyElement = xr.IsEmptyElement;

			XmlIO.Read(xr); // read including attributes

			while (true)
			{
				XmlIO.SkipNonContent(xr);
				XmlNodeType nt = xr.NodeType;

				// PUT MMS children here
				if (nt == XmlNodeType.Element)
					throw new System.Exception($"unexpected element {xr.Name} under {sElement} element");

				if (nt == XmlNodeType.EndElement)
				{
					if (xr.Name != sElement)
						throw new System.Exception($"unmatched {sElement} element");

					xr.ReadEndElement();
					break;
				}

				if (xr.NodeType != XmlNodeType.Attribute)
					throw new System.Exception($"unexpected non attribute on <{sElement}> element");

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
					throw new System.Exception($"never encountered end {sElement} element");
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
				if (xr.NodeType == XmlNodeType.XmlDeclaration)
				{
					// we have some attribues to potentially skip
					while (xr.NodeType == XmlNodeType.XmlDeclaration || xr.NodeType == XmlNodeType.Attribute)
					{
						if (!xr.MoveToNextAttribute() && !xr.Read())
							break;
					}
				}
				else
				{
					if (!xr.MoveToNextAttribute() && !xr.Read())
						break;
				}
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
				throw new System.Exception("not at the correct node");

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

		public static bool FProcessGenericValue(string sValue, out string sVal, string sDefault)
		{
			sVal = sDefault;

			if (sValue == "null")
			{
				sVal = null;
				return true;
			}

			if (sValue != null && sValue != "null")
			{
				sVal = sValue;
				return true;
			}

			return false;
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
        	%%Function: FProcessGenericValue
        	%%Qualified: XMLIO.XmlIO.FProcessGenericValue
        	
        ----------------------------------------------------------------------------*/
		public static bool FProcessGenericValue(string sValue, out int nVal, int nDefault)
		{
			nVal = nDefault;

			if (sValue != null && sValue != "null")
			{
				nVal = Int32.Parse(sValue);
				return true;
			}

			return false;
		}

		/*----------------------------------------------------------------------------
        	%%Function: ConvertElementStringToInt
        	%%Qualified: XMLIO.XmlIO.ConvertElementStringToInt
        	
            convert the given string into an integer
        ----------------------------------------------------------------------------*/
		internal static int? ConvertElementStringToInt(string sElementString)
		{
			if (FProcessGenericValue(sElementString, out int nVal, 0))
				return nVal;

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
        	%%Function: FProcessGenericValue
        	%%Qualified: XMLIO.XmlIO.FProcessGenericValue
        	
        ----------------------------------------------------------------------------*/
		public static bool FProcessGenericValue(string sValue, out UInt64 nVal, UInt64 nDefault)
		{
			nVal = nDefault;

			if (sValue != null && sValue != "null")
			{
				nVal = UInt64.Parse(sValue);
				return true;
			}

			return false;
		}

		/*----------------------------------------------------------------------------
			%%Function: ConvertElementStringToUInt64
			%%Qualified: XMLIO.XmlIO.ConvertElementStringToUInt64

		----------------------------------------------------------------------------*/
		internal static UInt64? ConvertElementStringToUInt64(string sElementString)
		{
			if (FProcessGenericValue(sElementString, out UInt64 nVal, 0))
				return nVal;

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
        	%%Function: FProcessGenericValue
        	%%Qualified: XMLIO.XmlIO.FProcessGenericValue
        	
        ----------------------------------------------------------------------------*/
		public static bool FProcessGenericValue(string sValue, out bool fValue, bool fDefault)
		{
			fValue = fDefault;

			if (sValue == null)
				return false;

			if (sValue == "0" || sValue == "false")
				fValue = false;
			else if (sValue == "1" || sValue == "true")
				fValue = true;
			else
				throw new FormatException($"{sValue} is not a boolean value");

			return true;
		}

		/*----------------------------------------------------------------------------
        	%%Function: ConvertElementStringToBool
        	%%Qualified: XMLIO.XmlIO.ConvertElementStringToBool
        	
            convert the given string into a bool
        ----------------------------------------------------------------------------*/
		public static bool? ConvertElementStringToBool(string sElementString)
		{
			if (!FProcessGenericValue(sElementString, out bool f, false))
				return null;

			return f;
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
		public static string[] ReadElementWithChildrenElementArray(string sRootElement, XmlReader xr, string sArrayElement)
		{
			if (xr.Name != sRootElement)
				throw new System.Exception("not at the correct node");

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
					if (xr.Name != sRootElement)
						throw new System.Exception($"encountered end node not matching <{sRootElement}>");

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

						// now we should be at the EndElement for sRootElement
						if (xr.Name == sRootElement)
						{
							xr.ReadEndElement();
							return pls.ToArray();
						}

						if (xr.Name != sArrayElement)
							throw new System.Exception("not at the correct node");
					}
				}
			}

			throw new System.Exception($"didn't find string child in {sRootElement}");
		}

		public interface IContentCollector
		{
			void AddTextContent(string s);
			void AddCDataContent(string s);
		}

		public class ContentCollector : IContentCollector
		{
			private StringBuilder m_sb = new StringBuilder();

			public bool HasContent => m_sb.Length > 0;
			
			public override string ToString()
			{
				return m_sb.ToString();
			}

			public ContentCollector() { }

			public void AddTextContent(string s)
			{
				m_sb.Append(s);
			}

			public void AddCDataContent(string cdata)
			{
				m_sb.Append(cdata);
			}
		}

		// process attribute just processes the given attribute/value pair
		public delegate bool FProcessAttributeDelegate<T>(string sAttribute, string sValue, T t);

		// parse element is expected to advance the reader past the element being parsed
		public delegate bool FParseElementDelegate<T>(XmlReader xr, string sElement, T t);

		public static bool FReadElement<T>(XmlReader xr, T t, string sRootElement, FProcessAttributeDelegate<T> processAttribute, FParseElementDelegate<T> parseElement, IContentCollector contentCollect = null)
		{
			bool fRootWasEmpty = xr.IsEmptyElement;

			if (xr.Name == sRootElement)
			{
				if (fRootWasEmpty && !xr.HasAttributes)
				{
					xr.Read();
					SkipNonContent(xr);
					return false;
				}

				// prepare read the attributes
				if (!XmlIO.Read(xr))
					throw new System.Exception("can't unclosed secretFileConfig element");

				XmlIO.SkipNonContent(xr);
			}
			else
			{
				throw new System.Exception($"parsing <{sRootElement}> without <{sRootElement}>");
			}

			// the reader should already be on the <secretFile>
			while (true)
			{
				if (xr.NodeType == XmlNodeType.Attribute)
				{
					if (xr.Name != "xmlns")
					{
						if (processAttribute == null || !processAttribute(xr.Name, xr.Value, t))
							throw new System.Exception($"bad attribute {xr.Name}");
					}
					// handle xml namespace declarations here if we want...
					
					XmlIO.Read(xr);
					XmlIO.SkipNonContent(xr);
					if (fRootWasEmpty && xr.NodeType != XmlNodeType.Attribute)
						return true;

					continue;
				}

				if (fRootWasEmpty)
					throw new System.Exception("empty element with no attributes at root? shouldn't get here");

				if (xr.NodeType == XmlNodeType.Element)
				{
					if (parseElement == null)
						throw new System.Exception($"unknown element {xr.Name}");

					// if it the parse returns false, that just means it was an empty element...
					parseElement(xr, xr.Name, t);
					continue;
				}

				if (xr.NodeType == XmlNodeType.EndElement)
				{
					if (xr.Name != sRootElement)
						throw new System.Exception($"open element {sRootElement} does not match close element {xr.Name}");
					XmlIO.Read(xr);
					XmlIO.SkipNonContent(xr);
					break;
				}

				if (xr.NodeType == XmlNodeType.Text)
				{
					if (contentCollect == null)
						throw new System.Exception($"text encountered without text collector: {xr.Value}");

					contentCollect.AddTextContent(xr.Value);
					XmlIO.Read(xr);
					XmlIO.SkipNonContent(xr);
					continue;
				}

				if (xr.NodeType == XmlNodeType.CDATA)
				{
					if (contentCollect == null)
						throw new System.Exception($"cdata encountered without text collector: {xr.Value}");

					contentCollect.AddCDataContent(xr.Value);
					XmlIO.Read(xr);
					XmlIO.SkipNonContent(xr);
					continue;
				}

				throw new System.Exception($"unknown node type {xr.NodeType}");
			}

			return true;
		}
	}
}
