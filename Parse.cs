using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;

using Cirrus;
using Xamarin.Parse.Json;

namespace Xamarin.Parse {
	public static class Parse {

		public const string ENDPOINT = "https://api.parse.com";
		const string API_VERSION = "1";

		internal enum Verb {
			GET,
			POST,
			PUT,
			DELETE
		}

		static string endpoint, application_id, api_key;
		static int timeout_msec;

		static UTF8Encoding encoding;
		
		public static string Endpoint {
			get { return endpoint; }
			set { endpoint = value; }
		}

		public static void Initialize (string appId, string apiKey, TimeSpan requestTimeout)
		{
			endpoint = ENDPOINT;
			application_id = appId;
			api_key = apiKey;
			timeout_msec = (int)requestTimeout.TotalMilliseconds;
			encoding = new UTF8Encoding (false);
		}

		public static ParseUser CurrentUser {
			get; internal set;
		}

		public static ParseQuery<TModel> Query<TModel> ()
			where TModel : ParseObject, new()
		{
			return new ParseQuery<TModel> (new ParseQueryProvider (new TModel ().ClassPath));
		}
		public static ParseQuery<ParseObject> Query (string className)
		{
			return new ParseQuery<ParseObject> (new ParseQueryProvider (ParseObject.CLASSES_PATH + "/" + className));
		}

		internal static Future<HttpWebResponse> ApiCall (Verb verb, string path, string data = null)
		{
			var uri = string.Join ("/", endpoint, API_VERSION, path);
			if (data != null && verb == Verb.GET)
				uri += "?" + data;
			Console.WriteLine ("{0} {1} : {2}", verb, uri, data);

			var req = (HttpWebRequest)WebRequest.Create (uri);
			req.Method = verb.ToString ();
			req.ContentType = "application/json";
			req.AllowAutoRedirect = true;
			req.Timeout = timeout_msec;

			req.Headers.Set ("X-Parse-Application-Id", application_id);
			req.Headers.Set ("X-Parse-REST-API-Key", api_key);

			if (data != null && verb != Verb.GET) {
				var dataBytes = encoding.GetBytes (data);
				req.ContentLength = dataBytes.Length;

				using (var reqStream = Future<Stream>.FromApm (req.BeginGetRequestStream, req.EndGetRequestStream).Wait ()) {
					reqStream.Write (dataBytes, 0, dataBytes.Length);
					reqStream.Close ();
				}
			}

			try {
				return Future<HttpWebResponse>.FromApm (req.BeginGetResponse, iar => (HttpWebResponse)req.EndGetResponse (iar)).Wait ();

			} catch (WebException wex) {
				ThrowIfNecessary (wex.Response as HttpWebResponse);
				throw;
			}
		}

		internal static Future<TResult> ApiCall<TResult> (Verb verb, string path, string data = null)
		{
			var response = ApiCall (verb, path, data).Wait ();
			ThrowIfNecessary (response);
			using (var stream = response.GetResponseStream ())
				return JsonReader.Read<TResult> (stream);
		}

		internal static void ThrowIfNecessary (HttpWebResponse response)
		{
			if (response == null)
				return;
			var statusCode = (int)response.StatusCode;
			if (statusCode >= 400)
				throw new ParseException (statusCode, JsonReader.Read<ParseErrorResponse> (response.GetResponseStream ()));
		}

		public static string GetClassName (Type type)
		{
			var input = type.Name;
			return input.Replace ("+", "_").Replace ("`", "_");
		}
	}
}

