using System;
using NUnit.Framework;

using Xamarin.Parse;

namespace Tests {
	[TestFixture]
	public class ParseTests : ParseTestBase {

		[Test]
		public void TestCreateObject ()
		{
			var obj = new ParseObject ("TestClass");
			obj ["foo"] = "bar";
			var save = obj.Save ();

			//FIXME: test createdate
			AssertChanged (obj, "Id", po => Assert.AreEqual ("Ed1nuqPvcm", po.Id, "objectId"));
			AssertRequest ("POST", "/1/classes/TestClass", "{\"foo\":\"bar\"}",
			               201, "{\"createdAt\":\"2011-08-20T02:06:57.931Z\",\"objectId\":\"Ed1nuqPvcm\"}");
			save.Wait ();
			EnforceAsserts ();
		}

		[Test]
		public void TestCreateUser ()
		{
			var user = new ParseUser {
				UserName = "cooldude6",
				Password = "p_n7!-e8"
			};
			user ["phone"] = "415-392-0202";
			var save = user.Save ();

			AssertChanged (user, "Id", po => Assert.AreEqual ("g7y9tkhB7O", po.Id, "objectId"));
			AssertChanged (user, "sessionToken", po => Assert.AreEqual ("pnktnjyb996sj4p156gjtp4im", po.SessionToken, "sessionToken"));
			AssertRequest ("POST", "/1/users", "{\"username\":\"cooldude6\",\"password\":\"p_n7!-e8\",\"phone\":\"415-392-0202\"}",
			               201, "{\"createdAt\":\"2011-08-20T02:06:57.931Z\",\"objectId\":\"g7y9tkhB7O\",\"sessionToken\":\"pnktnjyb996sj4p156gjtp4im\"}");
			save.Wait ();
			EnforceAsserts ();
		}

		[Test]
		public void TestFailedCreateObject ()
		{
			var obj = new ParseObject ("Thingy");
			obj ["bool"] = false;
			obj ["bl!ng"] = 10;
			var save = obj.Save ();

			AssertRequest ("POST", "/1/classes/Thingy", "{\"bool\":false,\"bl!ng\":10}",
			               400, "{\"code\":105,\"error\":\"invalid field name: bl!ng\"}");
			try {
				save.Wait ();
			} catch (AggregateException e) {
				var inner = e.Flatten ().InnerExceptions;
				Assert.AreEqual (1, inner.Count);
				Assert.IsInstanceOfType (typeof (ParseException), inner [0]);
				Assert.AreEqual ("invalid field name: bl!ng", inner [0].Message);
				return;
			}
			Assert.Fail ("Expected exception");
		}

		[Test]
		public void TestQueryAllInstancesOfClass ()
		{
			var instances = Parse.Query ("Thingy");
			Console.WriteLine (instances);
		}

		[Test]
		public void TestAtomicIncrement ()
		{
			var foo = new ParseObjectWithInt ();
			foo.SomeInt =
		}
	}
}

