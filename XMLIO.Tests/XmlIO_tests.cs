using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace XMLIO.Tests
{
    [TestFixture]
    public class XMLIOTests : XmlIO
    {
        [Test]
        public static void TestAlwaysPass2()
        {
            Assert.IsTrue(true);
        }


        #region TESTS

        #region XmlReaderTests

        /*----------------------------------------------------------------------------
        	%%Function: SetupXmlReaderForTest
        	%%Qualified: wp2droidMsg.SmsMessage.SetupXmlReaderForTest
        	%%Contact: rlittle
        	
            take a static string representing an XML snippet, and wrap an XML reader
            around the string

            NOTE: This is not very efficient -- it decodes the string into bytes, then
            creates a memory stream (which ought to be disposed of eventually since
            it is based on IDisposable), and then we finally return. But, these are 
            tests and will run fast enough. Don't steal this code for production
            though.
        ----------------------------------------------------------------------------*/

        public static XmlReader SetupXmlReaderForTest(string sTestString)
        {
            return XmlReader.Create(new StringReader(sTestString));
        }

        public static void AdvanceReaderToTestContent(XmlReader xr, string sElementTest)
        {
            XmlNodeType nt;

            while (xr.Read())
            {
                nt = xr.NodeType;
                if (nt == XmlNodeType.Element && xr.Name == sElementTest)
                    return;
            }

            throw new Exception($"could not advance to requested element '{sElementTest}'");
        }

        public static void RunTestExpectingException(TestDelegate pfn, string sExpectedException)
        {
            if (sExpectedException == "System.Xml.XmlException")
                Assert.Throws<XmlException>(pfn);
            else if (sExpectedException == "System.Exception")
                Assert.Throws<Exception>(pfn);
            else if (sExpectedException == "System.OverflowException")
                Assert.Throws<OverflowException>(pfn);
            else if (sExpectedException == "System.ArgumentException")
                Assert.Throws<ArgumentException>(pfn);
            else if (sExpectedException == "System.FormatException")
                Assert.Throws<FormatException>(pfn);
            else if (sExpectedException != null)
                throw new Exception("unknown exception type");
        }

        [TestCase("<foo>&amp;</foo>", "foo", XmlNodeType.Text, "", null)] // entities are resolved to text
        [TestCase("<foo><![CDATA[text]]></foo>", "foo", XmlNodeType.CDATA, "", null)] // CDATA is resolved to text
        [TestCase("<foo><!-- comment before --><![CDATA[text]]></foo>", "foo", XmlNodeType.CDATA, "", null)] // CDATA is resolved to text
        [TestCase("<foo> </foo>", "foo", XmlNodeType.EndElement, "foo", null)]
        [TestCase("<foo xml:space='preserve'> </foo>", "foo", XmlNodeType.SignificantWhitespace, "", null)]
        [TestCase("<foo attr='baz'><bar/></foo>", "foo", XmlNodeType.Element, "bar", null)]
        [TestCase("<foo><!-- comment here --><bar/></foo>", "foo", XmlNodeType.Element, "bar", null)]
        [TestCase("<foo><bar/></foo>", "foo", XmlNodeType.Element, "bar", null)]
        [TestCase("<foo></foo>", "foo", XmlNodeType.EndElement, "foo", null)]
        [Test]
        public static void TestSkipNonContent(string sTest, string sWrapperElement, XmlNodeType ntExpectedNext,
            string sExpectedNext, string sExpectedException)
        {
            XmlReader xr = SetupXmlReaderForTest(sTest);
            try
            {
                AdvanceReaderToTestContent(xr, sWrapperElement);
                xr.ReadStartElement(); // advance past the wrapper elements
            }
            catch (Exception e)
            {
                if (sExpectedException != null)
                    return;
                throw e;
            }

            if (sExpectedException != null)
            {
                RunTestExpectingException(() => SkipNonContent(xr), sExpectedException);
                return;
            }

            SkipNonContent(xr);
            Assert.AreEqual(ntExpectedNext, xr.NodeType);
            Assert.AreEqual(sExpectedNext, xr.Name);
        }

        [TestCase("<string>test</string>", "test", null)]
        [TestCase("\r\n<string>test</string>", "test", null)]
        [TestCase("<string>\rtest</string>", "\ntest", null)]
        [TestCase("<foo>test</foo>", null, "System.Exception")]
        [TestCase("<string><foo>test</foo></string>", null, "System.Xml.XmlException")]
        [Test]
        public static void TestStringElementReadFromXml(string sTest, string sExpectedReturn, string sExpectedException)
        {
            XmlReader xr = SetupXmlReaderForTest(sTest);
            try
            {
                AdvanceReaderToTestContent(xr, "string");
            }
            catch (Exception e)
            {
                if (sExpectedException != null)
                    return;
                throw e;
            }

            if (sExpectedException == null)
                Assert.AreEqual(sExpectedReturn, XmlIO.StringElementReadFromXml(xr));
            if (sExpectedException != null)
                RunTestExpectingException(() => XmlIO.StringElementReadFromXml(xr), sExpectedException);
        }

        // NOTE: This does NOT test if the parser is left in a good state!!

        [TestCase("<Recepients><string>+12345</string></Recepients>", new[] { "+12345" }, null)]
        [TestCase("<Recepients><string>\r+12345</string></Recepients>", new[] { "+12345" }, null)]
        [TestCase("<Recepients><string>(111) 222-3333</string></Recepients>", new[] { "(111) 222-3333" }, null)]
        [TestCase("<Recepients>\r\n<string>+12345</string></Recepients>", new[] { "+12345" }, null)]
        [TestCase("<Recepients />", null, null)]
        [TestCase("<Recepients>\r\n\t</Recepients>", null, null)]
        [TestCase("<Recepients></Recepients>", null, null)]
        [TestCase("<Recepients><string2>+12345</string2></Recepients>", null, "System.Xml.XmlException")]
        [TestCase("<Recepients><foo/><string>+12345</string></Recepients>", null, "System.Xml.XmlException")]
        [TestCase("<Recepients xmlns:a='b'><string>+4321</string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients attr='a'><string>+4321</string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string>+4321</string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<!-- comment between --><Recepients><string>+4321</string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><!-- comment between --><string>+4321</string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string><!-- comment between -->+4321</string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string>+4321<!-- comment between --></string><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string>+4321</string><!-- comment between --><string>12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string>+4321</string><string><!-- comment between -->12345</string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string>+4321</string><string>12345<!-- comment between --></string></Recepients>", new[] { "+4321", "12345" }, null)]
        [TestCase("<Recepients><string>+4321</string><string>12345</string><!-- comment between --></Recepients>", new[] { "+4321", "12345" }, null)]
        [Test]
        public static void TestRecepientsReadElement(string sTest, string[] rgsExpectedReturn, string sExpectedException)
        {
            XmlReader xr = SetupXmlReaderForTest(sTest);
            try
            {
                AdvanceReaderToTestContent(xr, "Recepients");
            }
            catch (Exception e)
            {
                if (sExpectedException != null)
                    return;
                throw e;
            }

            if (sExpectedException == null)
                Assert.AreEqual(rgsExpectedReturn, ReadElementWithChildrenElementArray("Recepients", xr, "string"));
            if (sExpectedException != null)
                RunTestExpectingException(() => ReadElementWithChildrenElementArray("Recepients", xr, "string"), sExpectedException);
        }

        [TestCase("<bar><Recepients><string>1234</string></Recepients><foo/></bar>", XmlNodeType.Element, "foo")]
        [TestCase("<Recepients><string>1234</string></Recepients> ", XmlNodeType.Whitespace, null)]
        [TestCase("<Recepients><string>1234</string><string>4321</string></Recepients> ", XmlNodeType.Whitespace, null)]
        [TestCase("<Recepients/> ", XmlNodeType.Whitespace, null)]
        [TestCase("<bar><Recepients/><foo/></bar>", XmlNodeType.Element, "foo")]
        [TestCase("<bar><Recepients> </Recepients><foo/></bar>", XmlNodeType.Element, "foo")]
        [TestCase("<bar><Recepients/> <foo/></bar>", XmlNodeType.Whitespace, null)]
        [Test]
        public static void TestRecepientsReadElementParserReturnState(string sTest, XmlNodeType ntExpected,
            string sNameExpected)
        {
            XmlReader xr = SetupXmlReaderForTest(sTest);
            AdvanceReaderToTestContent(xr, "Recepients");

            ReadElementWithChildrenElementArray("Recepients", xr, "string");
            Assert.AreEqual(ntExpected, xr.NodeType);
            if (sNameExpected != null)
                Assert.AreEqual(sNameExpected, xr.Name);
        }

        // NOTE: This does NOT test if the parser is left in a good state!!

        [TestCase("<Foo>+14253816865</Foo>", "Foo", "+14253816865", null)]
        [TestCase("<Recepients>\r+14253816865</Recepients>", "Recepients", "+14253816865", null)]
        [TestCase("<Recepients>(425) 381-6865</Recepients>", "Recepients", "(425) 381-6865", null)]
        [TestCase("<Recepients>\r\n+14253816865</Recepients>", "Recepients", "+14253816865", null)]
        [TestCase("<Recepients />", "Recepients", null, null)]
        [TestCase("<Recepients>\r\n\t</Recepients>", "Recepients", null, null)]
        [TestCase("<Recepients></Recepients>", "Recepients", null, null)]
        [TestCase("<Recepients>+14253816865</Recepients>", "Foo", null, "System.Xml.XmlException")]
        [TestCase("<Recepients><foo/>+14253816865</Recepients>", "Recepients", null, "System.Xml.XmlException")]
        [Test]
        public static void TestReadGenericStringElement(string sTest, string sExpectedElement, string sExpectedReturn, string sExpectedException)
        {
            XmlReader xr = SetupXmlReaderForTest(sTest);
            try
            {
                AdvanceReaderToTestContent(xr, sExpectedElement);
            }
            catch (Exception)
            {
                if (sExpectedException != null)
                    return;
                throw;
            }

            if (sExpectedException == null)
                Assert.AreEqual(sExpectedReturn, ReadGenericStringElement(xr, sExpectedElement));
            if (sExpectedException != null)
                RunTestExpectingException(() => ReadGenericStringElement(xr, sExpectedElement), sExpectedException);
        }

        [TestCase("<bar><Recepients>1234</Recepients><foo/></bar>", "Recepients", XmlNodeType.Element, "foo")]
        [TestCase("<Recepients>1234</Recepients> ", "Recepients", XmlNodeType.Whitespace, null)]
        [TestCase("<Recepients/> ", "Recepients", XmlNodeType.Whitespace, null)]
        [TestCase("<bar><Recepients/><foo/></bar>", "Recepients", XmlNodeType.Element, "foo")]
        [TestCase("<bar><Recepients> </Recepients><foo/></bar>", "Recepients", XmlNodeType.Element, "foo")]
        [TestCase("<bar><Recepients/> <foo/></bar>", "Recepients", XmlNodeType.Whitespace, null)]
        [Test]
        public static void TestReadGenericStringParserReturnState(string sTest, string sExpectedElement, XmlNodeType ntExpected,
            string sNameExpected)
        {
            XmlReader xr = SetupXmlReaderForTest(sTest);
            AdvanceReaderToTestContent(xr, "Recepients");

            ReadGenericStringElement(xr, sExpectedElement);
            Assert.AreEqual(ntExpected, xr.NodeType);
            if (sNameExpected != null)
                Assert.AreEqual(sNameExpected, xr.Name);
        }

        [TestCase("123", 123, null)]
        [TestCase("-123", -123, null)]
        [TestCase("2147483647", 2147483647, null)]
        [TestCase("-2147483648", -2147483648, null)]
        [TestCase("0", 0, null)]
        [TestCase(null, null, null)]
        [TestCase("2147483648", 0, "System.OverflowException")]
        [TestCase("", 0, "System.FormatException")]
        [TestCase("a", 0, "System.FormatException")]
        [TestCase("1a", 0, "System.FormatException")]
        [Test]
        public static void TestConvertElementStringToInt(string sTest, int? nExpectedVal, string sExpectedException)
        {
            if (sExpectedException == null)
                Assert.AreEqual(nExpectedVal, ConvertElementStringToInt(sTest));
            if (sExpectedException != null)
                RunTestExpectingException(() => ConvertElementStringToInt(sTest), sExpectedException);
        }

        [TestCase("123", 123UL, null)]
        [TestCase("18446744073709551615", 18446744073709551615UL, null)]
        [TestCase("0", 0UL, null)]
        [TestCase(null, null, null)]
        [TestCase("18446744073709551616", 0UL, "System.OverflowException")]
        [TestCase("", 0UL, "System.FormatException")]
        [TestCase("a", 0UL, "System.FormatException")]
        [TestCase("1a", 0UL, "System.FormatException")]
        [Test]
        public static void TestConvertElementStringToUInt64(string sTest, UInt64? nExpectedVal, string sExpectedException)
        {
            if (sExpectedException == null)
                Assert.AreEqual(nExpectedVal, ConvertElementStringToUInt64(sTest));
            if (sExpectedException != null)
                RunTestExpectingException(() => ConvertElementStringToUInt64(sTest), sExpectedException);
        }

        [TestCase("0", false, null)]
        [TestCase("false", false, null)]
        [TestCase("1", true, null)]
        [TestCase("true", true, null)]
        [TestCase(null, null, null)]
        [TestCase("True", false, "System.FormatException")]
        [TestCase("", false, "System.FormatException")]
        [TestCase("2", false, "System.FormatException")]
        [TestCase("1 true", false, "System.FormatException")]
        [Test]
        public static void TestConvertElementStringToBool(string sTest, bool? fExpectedVal, string sExpectedException)
        {
            if (sExpectedException == null)
                Assert.AreEqual(fExpectedVal, ConvertElementStringToBool(sTest));
            if (sExpectedException != null)
                RunTestExpectingException(() => ConvertElementStringToBool(sTest), sExpectedException);
        }

        [Test]
        public static void TestAlwaysPass()
        {
            Assert.IsTrue(true);
        }

        public static string FromNullable(string s)
        {
            if (s == "<null>")
                return null;

            return s;
        }
        #endregion

        [Test]
        public static void TestReadElement_EmptyElement_MismatchedFailParse()
        {
            string sXml = "<element/>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.Throws<Exception>(()=> FReadElement<object>(xr, null, "badRoot", null, null));
        }

        [Test]
        public static void TestReadElement_EmptyElementRoot()
        {
            string sXml = "<element/>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWithNoSibling()
        {
            string sXml = "<outer><element/></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.EndElement, xr.NodeType);
            Assert.AreEqual("outer", xr.Name);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWithSiblingElement()
        {
            string sXml = "<outer><element/><element/></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.Element, xr.NodeType);
            Assert.AreEqual("element", xr.Name);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWithSiblingText()
        {
            string sXml = "<outer><element/>text</outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.Text, xr.NodeType);
            Assert.AreEqual("", xr.Name);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWithSiblingCData()
        {
            string sXml = "<outer><element/><![CDATA[test]]></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.CDATA, xr.NodeType);
            Assert.AreEqual("", xr.Name);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWithSiblingWhitespaceCData()
        {
            string sXml = "<outer><element/>\n     <![CDATA[test]]></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.CDATA, xr.NodeType);
            Assert.AreEqual("", xr.Name);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWithSiblingEntity()
        {
            string sXml = "<outer><element/>&gt;</outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.Text, xr.NodeType);
            Assert.AreEqual("", xr.Name);
        }

        [Test]
        public static void TestReadElement_EmptyElementEmbeddedWitSiblingComment()
        {
            string sXml = "<outer><element/><!-- test comment --></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsFalse(FReadElement<object>(xr, null, "element", null, null));
            Assert.AreEqual(XmlNodeType.EndElement, xr.NodeType);
            Assert.AreEqual("outer", xr.Name);
        }

        [Test]
        public static void TestReadElement_NonEmptyRootElement()
        {
            string sXml = "<outer><knownElement/></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            UnitTestCollector collector = new UnitTestCollector();

            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "outer", FProcessKnownAttributesTest, FParseKnownChildElementsTest));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
        }

        [Test]
        public static void TestReadElement_NonEmptyEmbeddedElement()
        {
            string sXml = "<outer><element><knownChild/></element></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            UnitTestCollector collector = new UnitTestCollector();
            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, FParseKnownChildElementsTest));
            Assert.AreEqual(XmlNodeType.EndElement, xr.NodeType);
            Assert.AreEqual("outer", xr.Name);
        }



        #region Element/Attribute Parsing Tests

        [Test]
        public static void TestReadElement_EmptyRootElementWithAttributes_NoProcessor()
        {
            string sXml = "<element attr='value'/>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.Throws<Exception>(() => FReadElement<object>(xr, null, "element", null, null));
        }

        public class UnitTestCollector
        {
            public Dictionary<string, string> mpAttrVal = new Dictionary<string, string>();
            public Dictionary<string, string> mpEltTextVal = new Dictionary<string, string>();
            public Dictionary<string, string> mpEltCDataVal = new Dictionary<string, string>();

            public List<string> StackChildren = new List<string>();

            public UnitTestCollector()
            {
                StackChildren.Add("root");
            }

            public void AddAttribute(string sAttr, string sValue)
            {
                mpAttrVal.Add($"{StackChildren[StackChildren.Count - 1]}_{sAttr}", sValue);
            }

            public void OpenElement(string sElement)
            {
                StackChildren.Add(sElement);
            }

            public void CloseElement(string sElement)
            {
                if (StackChildren[StackChildren.Count - 1] != sElement)
                    throw new Exception($"{StackChildren[StackChildren.Count - 1]} != {sElement}");

                StackChildren.RemoveAt(StackChildren.Count - 1);
            }
        }

        public static bool FProcessKnownAttributesTest(string sAttribute, string sValue, UnitTestCollector collector)
        {
            if (sAttribute.IndexOf("known", StringComparison.Ordinal) != -1)
                collector.AddAttribute(sAttribute, sValue);
            else
                return false;

            return true;
        }

        [Test]
        public static void TestReadElement_EmptyRootElementWithAttributes_OneKnown()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element knownAttr='value'/>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, null));
            Assert.AreEqual("value", collector.mpAttrVal["root_knownAttr"]);
        }

        [Test]
        public static void TestReadElement_EmptyRootElementWithAttributes_ThreeKnown()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element knownAttr1='value1' knownAttr2='value2' knownAttr3='value3'/>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, null));
            Assert.AreEqual("value1", collector.mpAttrVal["root_knownAttr1"]);
            Assert.AreEqual("value2", collector.mpAttrVal["root_knownAttr2"]);
            Assert.AreEqual("value3", collector.mpAttrVal["root_knownAttr3"]);
        }

        [Test]
        public static void TestReadElement_EmptyRootElementWithAttributes_OneUnknown()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element knownAttr='value' invalid='value'/>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.Throws<Exception>(() => FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, null));
        }

        public static bool FParseKnownChildElementsTest(XmlReader xr, string sElement, UnitTestCollector collector)
        {
            if (sElement.IndexOf("known", StringComparison.Ordinal) != -1)
            {
                collector.OpenElement(sElement);
                FReadElement<UnitTestCollector>(xr, collector, sElement, FProcessKnownAttributesTest, FParseKnownChildElementsTest);
                collector.CloseElement(sElement);
                return true;
            }

            return false;
        }

        // now let's test with children
        [Test]
        public static void TestReadElement_RootElementWithChild_MismatchedParent()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element><knownChild/></element2>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.Throws<XmlException>(
                () => FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, FParseKnownChildElementsTest));
        }

        [Test]
        public static void TestReadElement_RootElementWithChild()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element><knownChild/></element>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, FParseKnownChildElementsTest));
        }

        [Test]
        public static void TestReadElement_RootElementWithChildWithAttributes()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element><knownChild knownAttr1='value'/></element>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, FParseKnownChildElementsTest));
            Assert.AreEqual("value", collector.mpAttrVal["knownChild_knownAttr1"]);
        }

        [Test]
        public static void TestReadElement_RootElementWithChildrenWithAttributes()
        {
            UnitTestCollector collector = new UnitTestCollector();
            string sXml = "<element><knownChild knownAttr1='value'/><knownChild2 knownAttr2='value2'/></element>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "element");

            Assert.IsTrue(FReadElement<UnitTestCollector>(xr, collector, "element", FProcessKnownAttributesTest, FParseKnownChildElementsTest));
            Assert.AreEqual("value", collector.mpAttrVal["knownChild_knownAttr1"]);
            Assert.AreEqual("value2", collector.mpAttrVal["knownChild2_knownAttr2"]);
        }
        #endregion

        #region Content Parsing
        [Test]
        public static void TestReadElement_RootElementWithSimpleTextContent()
        {
            string sXml = "<outer>text</outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            XmlIO.ContentCollector contentCollector = new XmlIO.ContentCollector();

            Assert.IsTrue(FReadElement<object>(xr, null, "outer", null, null, contentCollector));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
            Assert.AreEqual("text", contentCollector.ToString());
            Assert.IsFalse(contentCollector.NullContent);
        }

        [Test]
        public static void TestReadElement_RootElementWithEmptyTextContent()
        {
            string sXml = "<outer> </outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            XmlIO.ContentCollector contentCollector = new XmlIO.ContentCollector();

            Assert.IsTrue(FReadElement<object>(xr, null, "outer", null, null, contentCollector));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
            Assert.AreEqual("", contentCollector.ToString());
            Assert.IsFalse(contentCollector.NullContent);
        }

        [Test]
        public static void TestReadElement_EmptyRootElement()
        {
            string sXml = "<outer />";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            XmlIO.ContentCollector contentCollector = new XmlIO.ContentCollector();

            Assert.IsFalse(FReadElement<object>(xr, null, "outer", null, null, contentCollector));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
            Assert.AreEqual("", contentCollector.ToString());
            Assert.IsTrue(contentCollector.NullContent);
        }

        [Test]
        public static void TestReadElement_EmptyRootElement_WithAttributes()
        {
            string sXml = "<outer attr='foo' />";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            XmlIO.ContentCollector contentCollector = new XmlIO.ContentCollector();

            Assert.IsTrue(FReadElement<object>(xr, null, "outer", ((attribute, value, o) => true), null, contentCollector));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
            Assert.AreEqual("", contentCollector.ToString());
            Assert.IsTrue(contentCollector.NullContent);
        }

        [Test]
        public static void TestReadElement_RootElementWithTextTwoSpacesContent()
        {
            string sXml = "<outer>text  text</outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            XmlIO.ContentCollector contentCollector = new XmlIO.ContentCollector();

            Assert.IsTrue(FReadElement<object>(xr, null, "outer", null, null, contentCollector));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
            Assert.AreEqual("text  text", contentCollector.ToString());
            Assert.IsFalse(contentCollector.NullContent);
        }

        [Test]
        public static void TestReadElement_RootElementCDataContent()
        {
            string sXml = "<outer><![CDATA[test<>]]></outer>";
            XmlReader xr = SetupXmlReaderForTest(sXml);
            AdvanceReaderToTestContent(xr, "outer");

            XmlIO.ContentCollector contentCollector = new XmlIO.ContentCollector();

            Assert.IsTrue(FReadElement<object>(xr, null, "outer", null, null, contentCollector));
            Assert.AreEqual(XmlNodeType.None, xr.NodeType);
            Assert.AreEqual("test<>", contentCollector.ToString());
            Assert.IsFalse(contentCollector.NullContent);
        }
        #endregion

        #endregion
    }
}
