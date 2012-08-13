using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using Cirrus;
using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	[Serializable, ParseType ("File")]
	public class ParseFile {

		public string RemoteName { get; internal set; }
		public string RemoteURL { get; internal set; }

		public string LocalName { get; protected set; }
		public string ContentType { get; protected set; }

		internal ParseFile ()
		{
		}

		public ParseFile (string fileName, string contentType)
		{
			this.LocalName = fileName;
			this.ContentType = contentType;
		}

		public Future Save ()
		{
			if (RemoteName != null)
				return Future.Fulfilled;

			var reqFuture = Parse.Request (Parse.Verb.POST, "files/" + Path.GetFileName (LocalName));
			reqFuture.WithTimeout (Parse.timeout_msec).Wait ();
			if (reqFuture.Status != FutureStatus.Fulfilled)
				throw new TimeoutException (Parse.RequestFailMessage);
			var req = reqFuture.Value;
			req.ContentType = ContentType;

			using (var fileStream = new FileStream (LocalName, FileMode.Open, FileAccess.Read)) {
				req.ContentLength = fileStream.Length;

				var reqStreamFuture = Future<Stream>.FromApm (req.BeginGetRequestStream, req.EndGetRequestStream);
				reqStreamFuture.WithTimeout (Parse.timeout_msec).Wait ();
				if (reqStreamFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException (Parse.RequestFailMessage);

				using (var reqStream = reqStreamFuture.Value) {
					fileStream.CopyTo (reqStream);
					reqStream.Close ();
				}
			}

			try {
				var respFuture = Future<WebResponse>.FromApm (req.BeginGetResponse, req.EndGetResponse);
				respFuture.WithTimeout (Parse.timeout_msec).Wait ();
				if (respFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException (Parse.RequestFailMessage);

				var resp = (HttpWebResponse)respFuture.Value;
				Parse.ThrowIfNecessary (resp);

				var dict = JsonReader.Read<Dictionary<string,object>> (resp.GetResponseStream ()).Wait ();
				RemoteName = (string)dict ["name"];
				RemoteURL = (string)dict ["url"];

			} catch (AggregateException aex) {
				var wex = aex.InnerExceptions [0] as WebException;
				if (wex != null)
					Parse.ThrowIfNecessary (wex).Wait ();
				throw;

			} catch (WebException wex) {
				Parse.ThrowIfNecessary (wex).Wait ();
				throw;
			}
			return Future.Fulfilled;
		}
	}
}

