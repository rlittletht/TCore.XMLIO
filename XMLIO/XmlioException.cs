
using System;
using TCore.Exceptions;

namespace XMLIO
{
    public class XmlioException : TcException
    {
#pragma warning disable format // @formatter:off
        public XmlioException() : base(Guid.Empty) { }
        public XmlioException(Guid crids) : base(crids) { }
        public XmlioException(string errorMessage) : base(errorMessage) { }
        public XmlioException(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public XmlioException(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public XmlioException(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
    }

    public class XmlioExceptionUserCancelled : XmlioException
    {
#pragma warning disable format // @formatter:off
        public XmlioExceptionUserCancelled() : base(Guid.Empty) { }
        public XmlioExceptionUserCancelled(Guid crids) : base(crids) { }
        public XmlioExceptionUserCancelled(string errorMessage) : base(errorMessage) { }
        public XmlioExceptionUserCancelled(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public XmlioExceptionUserCancelled(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public XmlioExceptionUserCancelled(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
    }

    public class XmlioExceptionInternalParserFailure : XmlioException
    {
#pragma warning disable format // @formatter:off
        public XmlioExceptionInternalParserFailure() : base(Guid.Empty) { }
        public XmlioExceptionInternalParserFailure(Guid crids) : base(crids) { }
        public XmlioExceptionInternalParserFailure(string errorMessage) : base(errorMessage) { }
        public XmlioExceptionInternalParserFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public XmlioExceptionInternalParserFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public XmlioExceptionInternalParserFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
    }

    public class XmlioExceptionSchemaFailure: XmlioException
    {
#pragma warning disable format // @formatter:off
        public XmlioExceptionSchemaFailure() : base(Guid.Empty) { }
        public XmlioExceptionSchemaFailure(Guid crids) : base(crids) { }
        public XmlioExceptionSchemaFailure(string errorMessage) : base(errorMessage) { }
        public XmlioExceptionSchemaFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public XmlioExceptionSchemaFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public XmlioExceptionSchemaFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
    }

    public class XmlioExceptionXmlSyntaxError : XmlioException
    {
#pragma warning disable format // @formatter:off
        public XmlioExceptionXmlSyntaxError() : base(Guid.Empty) { }
        public XmlioExceptionXmlSyntaxError(Guid crids) : base(crids) { }
        public XmlioExceptionXmlSyntaxError(string errorMessage) : base(errorMessage) { }
        public XmlioExceptionXmlSyntaxError(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public XmlioExceptionXmlSyntaxError(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public XmlioExceptionXmlSyntaxError(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
    }
}
