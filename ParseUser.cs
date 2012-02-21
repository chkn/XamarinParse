using System;

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

		public string SessionToken {
			get { return (string) this ["sessionToken"]; }
		}

		public ParseUser ()
			: base (new[] { USERS_PATH })
		{
		}
	}
}

