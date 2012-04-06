using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using NUnit.Framework;

using Xamarin.Parse;

namespace Tests {
	public abstract class ParseTestBase {

		const string TEST_ENDPOINT = "http://localhost:8080";
		const string TEST_APP_ID  = "TEST_APP_ID";
		const string TEST_API_KEY = "TEST_API_KEY";
		const string CONTENT_TYPE = "application/json";

		protected UTF8Encoding encoding;
		protected HttpListener server;
		protected List<string> expect_changed_keys;

		[TestFixtureSetUp]
		public void TestFixtureSetUp ()
		{
			encoding = new UTF8Encoding (false);
			server = new HttpListener ();
			expect_changed_keys = new List<string> ();

			server.Prefixes.Add ("http://*:8080/");
			server.Start ();

			Parse.Initialize (TEST_ENDPOINT, TEST_APP_ID, TEST_API_KEY, TimeSpan.FromSeconds (1));
		}

		protected void AssertRequest (string verb, string path, string body, int responseCode, string response)
		{
			var ctx = server.GetContext ();

			// check request
			Assert.AreEqual (verb.ToUpperInvariant (), ctx.Request.HttpMethod.ToUpperInvariant (), "HTTP verb");
			Assert.AreEqual (path, ctx.Request.Url.AbsolutePath, "request path");
			Assert.AreEqual (TEST_APP_ID, ctx.Request.Headers ["X-Parse-Application-Id"]);
			Assert.AreEqual (TEST_API_KEY, ctx.Request.Headers ["X-Parse-REST-API-Key"]);
			Assert.AreEqual (CONTENT_TYPE, ctx.Request.ContentType, "Content-Type header");

			var reader = new StreamReader (ctx.Request.InputStream);
			Assert.AreEqual (body, reader.ReadToEnd (), "request body");

			// make response
			ctx.Response.StatusCode = responseCode;
			ctx.Response.ContentType = CONTENT_TYPE;
			var utf8response = encoding.GetBytes (response);
			ctx.Response.OutputStream.Write (utf8response, 0, utf8response.Length);
			ctx.Response.Close ();
		}

		protected void AssertChanged<TParseObject> (TParseObject obj, string key, Action<TParseObject> assert)
			where TParseObject : INotifyPropertyChanged
		{
			expect_changed_keys.Add (key);
			obj.PropertyChanged += (s, args) => {
				if (args.PropertyName == key) {
					assert (obj);
					expect_changed_keys.Remove (key);
				}
			};
		}

		protected void EnforceAsserts ()
		{
			Assert.IsEmpty (expect_changed_keys, "expected property changed notifications");
		}
	}
}

