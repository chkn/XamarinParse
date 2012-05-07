using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using Cirrus;
using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	public class ParseFile {

		public string RemoteName { get; internal set; }
		public string RemoteURL { get; internal set; }

		public string LocalName { get; protected set; }
		public string ContentType { get; protected set; }

		public ParseFile (string fileName, string contentType)
		{
			this.LocalName = fileName;
			this.ContentType = contentType;
		}

		public Future Save ()
		{
			if (RemoteName != null)
				return Future.Fulfilled;

			var req = Parse.Request (Parse.Verb.POST, "files/" + Path.GetFileName (LocalName)).Wait ();
			req.ContentType = ContentType;

			using (var fileStream = new FileStream (LocalName, FileMode.Open, FileAccess.Read)) {
				var reqStreamFuture =  Future<Stream>.FromApm (req.BeginGetRequestStream, req.EndGetRequestStream);
				reqStreamFuture.WithTimeout (Parse.timeout_msec).Wait ();
				if (reqStreamFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException ("Connection to server timed out");

				using (var reqStream = reqStreamFuture.Value) {
					fileStream.CopyTo (reqStream);
					reqStream.Close ();
				}
			}

			try {
				var respFuture = Future<HttpWebResponse>.FromApm (req.BeginGetResponse, iar => (HttpWebResponse)req.EndGetResponse (iar));
				respFuture.WithTimeout (Parse.timeout_msec).Wait ();
				if (respFuture.Status != FutureStatus.Fulfilled)
					throw new TimeoutException ("Did not receive response from server");

				var resp = respFuture.Value;
				Parse.ThrowIfNecessary (resp);

				var dict = JsonReader.Read<Dictionary<string,object>> (resp.GetResponseStream ()).Wait ();
				RemoteName = (string)dict ["name"];
				RemoteURL = (string)dict ["url"];

			} catch (AggregateException aex) {
				var wex = aex.InnerExceptions [0] as WebException;
				if (wex != null)
					Parse.ThrowIfNecessary (wex.Response as HttpWebResponse);
				throw;

			} catch (WebException wex) {
				Parse.ThrowIfNecessary (wex.Response as HttpWebResponse);
				throw;
			}
			return Future.Fulfilled;
		}
	}
}

