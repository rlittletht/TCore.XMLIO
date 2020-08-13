using System;
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
                Assert.AreEqual(rgsExpectedReturn, RecepientsReadElement(xr));
            if (sExpectedException != null)
                RunTestExpectingException(() => RecepientsReadElement(xr), sExpectedException);
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

            RecepientsReadElement(xr);
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
    }
}
