using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xamarin.Parse.Json;

namespace Xamarin.Parse {
	public static class Parse {

#if DEBUG
		const string ENDPOINT = "http://localhost:8080";
#else
		const string ENDPOINT = "https://api.parse.com";
#endif
		const string API_VERSION = "1";

		internal enum Verb {
			GET,
			POST,
			PUT,
			DELETE
		}

		static string application_id, api_key;
		static int timeout_msec;

		static UTF8Encoding encoding;

		public static void Initialize (string appId, string apiKey, TimeSpan requestTimeout)
		{
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

		internal static Task<HttpWebResponse> ApiCall (Verb verb, string path, string data = null)
		{
			var uri = string.Join ("/", ENDPOINT, API_VERSION, path);
			Console.WriteLine ("{0} {1} : {2}", verb, uri, data);

			var req = (HttpWebRequest)WebRequest.Create (uri);
			req.Method = verb.ToString ();
			req.ContentType = "application/json";
			req.AllowAutoRedirect = true;
			req.Timeout = timeout_msec;

			req.Headers.Set ("X-Parse-Application-Id", application_id);
			req.Headers.Set ("X-Parse-REST-API-Key", api_key);

			return Task.Factory.FromAsync<Stream> (req.BeginGetRequestStream, req.EndGetRequestStream, null)
				.ContinueWith (task => {
					var reqStream = task.Result;

					if (data != null) {
						var dataBytes = encoding.GetBytes (data);
						reqStream.Write (dataBytes, 0, dataBytes.Length);
					}

					reqStream.Close ();
					return Task.Factory.FromAsync<HttpWebResponse> (req.BeginGetResponse, iar => (HttpWebResponse)req.EndGetResponse (iar), null);
				}).Unwrap ();
		}

		internal static Task<TResult> ApiCall<TResult> (Verb verb, string path, string data = null)
		{
			return ApiCall (verb, path, data).ContinueWith (task => {
				ThrowIfNecessary (task);
				return JsonReader.Read<TResult> (task.Result.GetResponseStream ());
			});
		}

		internal static void ThrowIfNecessary (Task<HttpWebResponse> task)
		{
			try {
				var statusCode = (int)task.Result.StatusCode;
				if (statusCode >= 400)
					throw new ParseException (JsonReader.Read<ParseErrorResponse> (task.Result.GetResponseStream ()));
			} catch (AggregateException agg) {
				var inner = agg.Flatten ().InnerExceptions;
				if (inner.Count == 1 && inner [0] is WebException)
					throw new ParseException (JsonReader.Read<ParseErrorResponse> (((WebException)inner [0]).Response.GetResponseStream ()));
				else
					throw;
			}
		}

		public static string GetClassName (Type type)
		{
			var input = type.Name;
			return input.Replace ("+", "_").Replace ("`", "_");
		}
	}
}

