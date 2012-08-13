using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
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
			DELETE,
			HEAD
		}

		static string endpoint, application_id, api_key;
		internal static uint timeout_msec;

		public static string RequestFailMessage = "Cannot communicate with server";

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

		public static string CurrentSessionToken {
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
			if (CurrentSessionToken != null)
				req.Headers.Set ("X-Parse-Session-Token", CurrentSessionToken);

			return req;
		}

		public static Future<HttpWebResponse> ApiCall (Verb verb, string path, string data = null, string contentType = "application/json")
		{
			Future<Stream> reqStreamFuture = null;
			Future<WebResponse> respFuture = null;

			if (data != null && verb == Verb.GET)
				path += "?" + data;

			var req = Request (verb, path).Wait ();
#if DEBUG
			Console.WriteLine ("with data: {0}", data);
#endif
			try {
				if (data != null && verb != Verb.GET) {
					var dataBytes = encoding.GetBytes (data);
					req.ContentType = contentType;
					req.ContentLength = dataBytes.Length;

					reqStreamFuture = Future<Stream>.FromApm (req.BeginGetRequestStream, req.EndGetRequestStream);
					reqStreamFuture.WithTimeout (timeout_msec).Wait ();
					if (reqStreamFuture.Status != FutureStatus.Fulfilled)
						throw new TimeoutException (RequestFailMessage);

					using (var reqStream = reqStreamFuture.Value) {
						reqStream.Write (dataBytes, 0, dataBytes.Length);
						reqStream.Close ();
					}
				}

				respFuture = Future<WebResponse>.FromApm (req.BeginGetResponse, req.EndGetResponse);
				respFuture.WithTimeout (timeout_msec).Wait ();
				if (respFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException (RequestFailMessage);

				return (HttpWebResponse)respFuture.Value;

			} catch (AggregateException aex) {
				var wex = aex.InnerExceptions [0] as WebException;
				if (wex != null)
					ThrowIfNecessary (wex).Wait ();
				throw;

			} catch (WebException wex) {
				ThrowIfNecessary (wex).Wait ();
				throw;

			} catch {
				req.Abort ();
				throw;
			}
		}

		public static Future<TResult> ApiCall<TResult> (Verb verb, string path, string data = null)
		{
			var response = ApiCall (verb, path, data).Wait ();
			return ReadResponse<TResult> (response, typeof (TResult)).Wait ();
		}

		class ResponseReaderFuture<TResult> : Future<TResult>  {
			HttpWebResponse response;
			Type resultType;

			public ResponseReaderFuture (HttpWebResponse response, Type resultType)
			{
				this.response = response;
				this.resultType = resultType;
				this.SupportsCancellation = true;

				ThreadPool.QueueUserWorkItem (Read);
			}

			void Read (object notUsed)
			{
				try {
					using (var stream = response.GetResponseStream ()) {
						var result = JsonReader.Read<TResult> (stream, resultType).Wait ();
						if (result is ParseObject)
							((ParseObject)(object)result).ResetHasLocalModifications ();
						Value = result;
						stream.Close ();
					}
				} catch (Exception e) {
					// ignore exceptions after we've already set the value, eg. some issue in stream.Close()
					if (Status == FutureStatus.Pending)
						Exception = e;
				} finally {
					response.Close ();
				}
			}
		}
		internal static Future<TResult> ReadResponse<TResult> (HttpWebResponse response, Type resultType)
		{
			// FIXME: actually make these things async instead of just wrapping in another thread
			ThrowIfNecessary (response).Wait ();
			return (new ResponseReaderFuture<TResult> (response, resultType)).Wait ();
		}

		internal static Future ThrowIfNecessary (WebException wex)
		{
			if (wex.Status == WebExceptionStatus.ConnectFailure ||
			    wex.Status == WebExceptionStatus.SendFailure)
				throw new WebException (RequestFailMessage, wex, wex.Status, wex.Response);
			return ThrowIfNecessary (wex.Response as HttpWebResponse);
		}

		internal static Future ThrowIfNecessary (HttpWebResponse response)
		{
			if (response == null)
				return Future.Fulfilled;
			var statusCode = (int)response.StatusCode;
			if (statusCode >= 400)
				throw new ParseException (statusCode, JsonReader.Read<ParseErrorResponse> (response.GetResponseStream ()).Wait ());
			return Future.Fulfilled;
		}

		public static string GetClassName (Type type)
		{
			var input = type.Name;
			return input.Replace ("+", "_").Replace ("`", "_");
		}
	}
}

