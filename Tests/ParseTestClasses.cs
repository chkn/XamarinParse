using System;

using Xamarin.Parse;

namespace Tests {

	public class ParseObjectWithInt : ParseObject {

		public int SomeInt {
			get { return (int) this ["SomeInt"]; }
			set { this ["SomeInt"] = value; }
		}

		public ParseObjectWithInt ()
			: base ("HasInt")
		{
		}
	}
}

