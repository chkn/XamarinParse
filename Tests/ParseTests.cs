using System;
using System.Diagnostics;
using NUnit.Framework;

using Cirrus;
using Xamarin.Parse;

[TestFixture]
public class ParseTests : ParseTestBase {

	[Test]
	public void TestCreateObject ()
	{
		try {
			var obj = new ParseObject ("TestClass");
			obj ["foo"] = "bar";
	
			//FIXME: test createdate
			AssertChanged (obj, "ObjectId", po => Assert.AreEqual ("Ed1nuqPvcm", po.ObjectId, "objectId"));
			AssertRequest ("POST", "/1/classes/TestClass", "{\"foo\":\"bar\"}",
			               201, "{\"createdAt\":\"2011-08-20T02:06:57.931Z\",\"objectId\":\"Ed1nuqPvcm\"}");
	
			obj.Save ().Wait ();
		} finally {
			TestComplete ();
		}
	}

	[Test]
	public void TestCreateUser ()
	{
		try {
			var user = new ParseUser {
				UserName = "cooldude6",
				Password = "p_n7!-e8"
			};
			user ["phone"] = "415-392-0202";
	
			AssertChanged (user, "ObjectId", po => Assert.AreEqual ("g7y9tkhB7O", po.ObjectId, "objectId"));
			AssertChanged (user, "sessionToken", po => Assert.AreEqual ("pnktnjyb996sj4p156gjtp4im", po.SessionToken, "sessionToken"));
			AssertRequest ("POST", "/1/users", "{\"username\":\"cooldude6\",\"password\":\"p_n7!-e8\",\"phone\":\"415-392-0202\"}",
			               201, "{\"createdAt\":\"2011-08-20T02:06:57.931Z\",\"objectId\":\"g7y9tkhB7O\",\"sessionToken\":\"pnktnjyb996sj4p156gjtp4im\"}");

			user.Save ().Wait ();
		} finally {
			TestComplete ();
		}
	}

	[Test]
	public void TestFailedCreateObject ()
	{
		try {
			var obj = new ParseObject ("Thingy");
			obj ["bool"] = false;
			obj ["bl!ng"] = 10;
	
			AssertRequest ("POST", "/1/classes/Thingy", "{\"bool\":false,\"bl!ng\":10}",
			               400, "{\"code\":105,\"error\":\"invalid field name: bl!ng\"}");
			try {
				obj.Save ().Wait ();
			} catch (AggregateException e) {
				var inner = e.Flatten ().InnerExceptions;
				Assert.AreEqual (1, inner.Count);
				Assert.IsInstanceOfType (typeof (ParseException), inner [0]);
				Assert.AreEqual ("invalid field name: bl!ng", inner [0].Message);
				TestComplete ();
				return;
			}
	
			Assert.Fail ("Expected exception");
		} finally {
			TestComplete ();
		}
	}
	
	[Test]
	public void TestUpdateObjects ()
	{
		try {
			var looseObj = new ParseObjectLooselyTyped ();
			var strongObj = new ParseObjectStronglyTyped ();
			TestUpdateObjectAsync (looseObj).Wait ();
			TestUpdateObjectAsync (strongObj).Wait ();
		} finally {
			TestComplete ();
		}
	}

	Future TestUpdateObjectAsync (ParseTestObject testObject)
	{
		var other = new ParseObject ("Foo");
		other ["foo"] = true;

		testObject.SomeString = "string1";
		testObject.SomeNumber = 1;
		testObject.SomeOtherObject = other;
		
		AssertChanged (other, "ObjectId", po => Assert.AreEqual ("Ed1nuqPvc", po.ObjectId, "objectId"));
		AssertRequest ("POST", "/1/classes/Foo", "{\"foo\":true}",
		               201, "{\"createdAt\":\"2011-08-20T02:06:57.931Z\",\"objectId\":\"Ed1nuqPvc\"}");
		
		AssertChanged (testObject, "ObjectId", po => Assert.AreEqual ("Abb2ndvcH", po.ObjectId, "objectId"));
		AssertRequest ("POST", "/1/classes/TestObject", "{\"SomeString\":\"string1\",\"SomeInt\":1,\"SomeOtherObject\":{\"__type\":\"Pointer\",\"className\":\"Foo\",\"objectId\":\"Ed1nuqPvc\"}}",
		               201, "{\"createdAt\":\"2011-08-20T02:06:57.931Z\",\"objectId\":\"Abb2ndvcH\"}");

		return testObject.Save ();
	}
	
	[Ignore]
	[Test]
	public void TestQueryAllInstancesOfClass ()
	{
		var instances = Parse.Query ("Thingy");
		Console.WriteLine (instances);
	}
	
	[Ignore]
	[Test]
	public void TestAtomicIncrement ()
	{
	//	var foo = new ParseObjectWithInt ();
	//	foo.SomeInt =
	}
}

