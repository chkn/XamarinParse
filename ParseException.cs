using System;

namespace Xamarin.Parse {

	// An exception related to any type of Parse failure.
	// FIXME: make an enum of the error codes, if we can figure their values out
	public class ParseException : Exception {

		internal ParseException (ParseErrorResponse resp)
			: base (resp.error)
		{
		}
	}

	struct ParseErrorResponse {
		public int code;
		public string error;
	}
}

