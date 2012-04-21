using System;

namespace Xamarin.Parse {

	// An exception related to any type of Parse failure.
	public class ParseException : Exception {

		public int ParseCode { get; protected set; }
		public int HttpCode { get; protected set; }

		internal ParseException (int httpStatus, ParseErrorResponse resp)
			: base (resp.error)
		{
			this.ParseCode = resp.code;
			this.HttpCode = httpStatus;
		}
	}

	struct ParseErrorResponse {
		public int code;
		public string error;
	}
}

