using System;

using Xamarin.Parse;

namespace Sample {
	class MainClass {

		const string APP_ID = "-";
		const string API_KEY = "-";

		public static void Main (string[] args)
		{
			Parse.Initialize (APP_ID, API_KEY, TimeSpan.FromSeconds (10));
			var testObject = new ParseObject ("TestObject");
			testObject ["foo"] = "bar";
			testObject ["baz"] = 10;
			testObject.Save().Wait();
		}
	}
}
