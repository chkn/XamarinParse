using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using NUnit.Framework;

using Cirrus;
using Xamarin.Parse;

public abstract class ParseTestBase : TestBase {

	const string TEST_ENDPOINT = "http://localhost:{0}";
	const string TEST_APP_ID  = "TEST_APP_ID";
	const string TEST_API_KEY = "TEST_API_KEY";
	const string CONTENT_TYPE = "application/json";

	protected UTF8Encoding encoding;
	protected HttpListener server;
	protected List<string> expect_changed_keys;
	
	static List<int> usedPorts = new List<int>();
    static Random r = new Random();

    public HttpListener CreateNewListener(out int port)
    {
        HttpListener mListener;
        port = -1;
        while (true)
        {
            mListener = new HttpListener();
            port = r.Next(1025, 65535); // be nice, don't use ports bellow 1025
            if (usedPorts.Contains(port))
            {
                continue;
            }
            mListener.Prefixes.Add(string.Format("http://*:{0}/", port));
            try
            {
                mListener.Start();
            }
            catch
            {
                continue;
            }
            usedPorts.Add(port);
            break;
        }

        return mListener;
    }
	
	[TestFixtureSetUp]
	public override void TestFixtureSetUp ()
	{
		encoding = new UTF8Encoding (false);
		expect_changed_keys = new List<string> ();
	}
	
	[SetUp]
	public override void PreTest ()
	{
		int port;
		base.PreTest ();
		expect_changed_keys.Clear ();

		if (server != null)
			server.Close ();
		server = CreateNewListener (out port);
		Parse.Initialize (TEST_APP_ID, TEST_API_KEY, TimeSpan.FromSeconds (1));
		Parse.Endpoint = string.Format (TEST_ENDPOINT, port);
	}

	protected Future AssertRequest (string verb, string path, string body, int responseCode, string response)
	{
		var ctx = Future<HttpListenerContext>.FromApm (server.BeginGetContext, server.EndGetContext).Wait ();
		Console.WriteLine ("Test server got request");
		
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

		return Future.Fulfilled;
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
	
	[TearDown]
	public override void PostTest ()
	{
		base.PostTest ();
		Assert.IsEmpty (expect_changed_keys, "unreceived property changed notifications");
	}
}

