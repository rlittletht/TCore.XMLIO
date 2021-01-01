using System;
using System.Collections.Generic;
using System.Text;

namespace XMLIO
{
	public static class Exceptions
	{
		class ErrorMessages
		{
			public const string Unknown = "Unknown exception";
			public const string UserCancelled = "User initiated cancel";
		}

		public class Exception : System.Exception
		{
			public Exception(System.Exception exc) : base(exc.Message)
			{
			}

			public Exception(string sMessage, params object[] rgo) : base(String.Format(sMessage, rgo))
			{
			}

			public Exception(System.Exception innerException, string sMessage, params object[] rgo) : base(
				String.Format(sMessage, rgo),
				innerException)
			{
			}

			public Exception() : base(ErrorMessages.Unknown)
			{
			}
		}

		public class UserCancelledException : Exception { }
	}
}
