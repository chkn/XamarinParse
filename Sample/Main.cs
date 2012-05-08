using System;

using Cirrus;
using Xamarin.Parse;

namespace Sample {
	class MainClass {

		const string APP_ID = "-";
		const string API_KEY = "-";

		public static void Main (string[] args)
		{
			Parse.Initialize (APP_ID, API_KEY, TimeSpan.FromSeconds (10));
			Go ();
			Thread.Current.RunLoop ();
		}

		public void Go ()
		{
			var testObject = new ParseObject ("TestObject");
			testObject ["foo"] = "bar";
			testObject ["baz"] = 10;
			testObject.Save().Wait();
			Console.WriteLine ("Saved: {0}", testObject.ObjectId);
			Environment.Exit (0);
		}
	}
}
