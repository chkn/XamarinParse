using System;
using System.Web;

using Cirrus;

namespace Xamarin.Parse {
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

		public ParseUser ()
			: base (new[] { USERS_PATH })
		{
			this.pointerClassName = "_User";
		}
		
		public static Future<TUser> LogIn<TUser> (string userName, string password)
			where TUser : ParseUser
		{
			var data = string.Format ("username={0}&password={1}", HttpUtility.UrlEncode (userName), HttpUtility.UrlEncode (password));
			return Parse.ApiCall<TUser> (Parse.Verb.GET, "login", data);
		}
			
	}
}

