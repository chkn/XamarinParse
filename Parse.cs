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

		public enum Verb {
			GET,
			POST,
			PUT,
			DELETE
		}

		static string endpoint, application_id, api_key;
		internal static uint timeout_msec;

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
			timeout_msec = (uint)requestTimeout.TotalMilliseconds;
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

		public static Future<TModel> Get<TModel> (string className, string id)
			where TModel : ParseObject
		{
			return Parse.ApiCall<TModel> (Verb.GET, ParseObject.CLASSES_PATH + "/" + className + "/" + id);
		}

		public static Future<HttpWebRequest> Request (Verb verb, string path)
		{
			var uri = string.Join ("/", endpoint, API_VERSION, path);
#if DEBUG
			Console.WriteLine ("{0} {1}", verb, uri);
#endif

			var req = (HttpWebRequest)WebRequest.Create (uri);
			req.Method = verb.ToString ();
			req.AllowAutoRedirect = true;
			// This timeout is questionable at best
			//req.Timeout = (int)timeout_msec;
			//req.ReadWriteTimeout = (int)timeout_msec;

			req.Headers.Set ("X-Parse-Application-Id", application_id);
			req.Headers.Set ("X-Parse-REST-API-Key", api_key);

			return req;
		}

		public static Future<HttpWebResponse> ApiCall (Verb verb, string path, string data = null, string contentType = "application/json")
		{
			if (data != null && verb == Verb.GET)
				path += "?" + data;

			var req = Request (verb, path).Wait ();
#if DEBUG
			Console.WriteLine ("with data: {0}", data);
#endif

			if (data != null && verb != Verb.GET) {
				var dataBytes = encoding.GetBytes (data);
				req.ContentType = contentType;
				req.ContentLength = dataBytes.Length;

				var reqStreamFuture = Future<Stream>.FromApm (req.BeginGetRequestStream, req.EndGetRequestStream);
				reqStreamFuture.WithTimeout (timeout_msec).Wait ();
				if (reqStreamFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException ("Connection to server timed out");

				using (var reqStream = reqStreamFuture.Value) {
					reqStream.Write (dataBytes, 0, dataBytes.Length);
					reqStream.Close ();
				}
			}

			try {
				var respFuture = Future<HttpWebResponse>.FromApm (req.BeginGetResponse, iar => (HttpWebResponse)req.EndGetResponse (iar));
				respFuture.WithTimeout (timeout_msec).Wait ();
				if (respFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException ("Did not receive response from server");

				return respFuture.Value;

			} catch (AggregateException aex) {
				var wex = aex.InnerExceptions [0] as WebException;
				if (wex != null)
					ThrowIfNecessary (wex.Response as HttpWebResponse);
				throw;

			} catch (WebException wex) {
				ThrowIfNecessary (wex.Response as HttpWebResponse);
				throw;
			}
		}

		public static Future<TResult> ApiCall<TResult> (Verb verb, string path, string data = null)
		//	where TResult : ParseObject
		{
			var response = ApiCall (verb, path, data).Wait ();
			ThrowIfNecessary (response);

			TResult result;
			using (var stream = response.GetResponseStream ())
				result = JsonReader.Read<TResult> (stream).Wait ();
			
			//FIXME: investigate cilc bug that makes this fail- generic constraints on async methods maybe?
		//	result.updated_properties.Clear ();
			return result;
		}

		internal static void ThrowIfNecessary (HttpWebResponse response)
		{
			if (response == null)
				return;
			var statusCode = (int)response.StatusCode;
			if (statusCode >= 400)
				throw new ParseException (statusCode, JsonReader.Read<ParseErrorResponse> (response.GetResponseStream ()).Wait ());
		}

		public static string GetClassName (Type type)
		{
			var input = type.Name;
			return input.Replace ("+", "_").Replace ("`", "_");
		}
	}
}

