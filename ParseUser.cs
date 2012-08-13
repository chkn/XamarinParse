using System;
using System.Web;

using Cirrus;

namespace Xamarin.Parse {

	[Serializable]
	public class ParseUser : ParseObject {

		// the API path to user objects
		internal const string USERS_PATH = "users";

		public string UserName {
			get { return (string) this ["username"]; }
			set { this ["username"] = value; }
		}

		public string Password {
			get { return (string) this ["password"]; }
			set { this ["password"] = value; }
		}

		public string Email {
			get { return (string) this ["email"]; }
			set { this ["email"] = value; }
		}

		public bool EmailVerified {
			get { return (bool) this ["emailVerified"]; }
		}

		public string SessionToken {
			get { return (string) this ["sessionToken"]; }
		}

		public override Future Save ()
		{
			base.Save ().Wait ();
			Parse.CurrentSessionToken = SessionToken;
			return Future.Fulfilled;
		}

		public ParseUser ()
			: this (new[] { USERS_PATH })
		{
		}

		protected ParseUser (string [] path)
			: base (path)
		{
			this.pointerClassName = "_User";
		}

		public static Future<TUser> LogIn<TUser> (string userName, string password)
			where TUser : ParseUser
		{
			var data = string.Format ("username={0}&password={1}", HttpUtility.UrlEncode (userName), HttpUtility.UrlEncode (password));
			var user = Parse.ApiCall<TUser> (Parse.Verb.GET, "login", data).Wait ();
			Parse.CurrentSessionToken = user.SessionToken;
			user.ResetHasLocalModifications ();
			return user;
		}

		public static Future LogOut ()
		{
			//FIXME: need to do anything else here?
			Parse.CurrentSessionToken = null;
			return Future.Fulfilled;
		}

		public static Future ResetPassword (string email)
		{
			var data = string.Format ("{{\"email\":\"{0}\"}}", email);
			using (var resp = Parse.ApiCall (Parse.Verb.POST, "requestPasswordReset", data).Wait ())
				resp.Close ();
			return Future.Fulfilled;
		}
	}
}

