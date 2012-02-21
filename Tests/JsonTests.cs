using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;

using Xamarin.Parse.Json;

[TestFixture]
public class JsonTests {
	[Test]
	public void TestWriteStrings ()
	{
		Assert.AreEqual ("foo".ToJson (), "\"foo\"");
		Assert.AreEqual ("\"foo\\bbar\"", "foo\bbar".ToJson (), "\\b");
		Assert.AreEqual ("\"foo\\fbar\"", "foo\fbar".ToJson (), "\\f");
		Assert.AreEqual ("\"foo\\nbar\"", "foo\nbar".ToJson (), "\\n");
		Assert.AreEqual ("\"foo\\rbar\"", "foo\rbar".ToJson (), "\\r");
		Assert.AreEqual ("\"foo\\tbar\"", "foo\tbar".ToJson (), "\\t");
		Assert.AreEqual ("\"foo\\\\bar\"", "foo\\bar".ToJson (), "\\");
	}

	[Test]
	public void TestReadStrings ()
	{
		AssertRead<string> ("foo\bbar", "\"foo\\bbar\"", "\\b");
		AssertRead<string> ("foo\fbar", "\"foo\\fbar\"", "\\f");
		AssertRead<string> ("foo\nbar", "\"foo\\nbar\"", "\\n");
		AssertRead<string> ("foo\rbar", "\"foo\\rbar\"", "\\r");
		AssertRead<string> ("foo\tbar", "\"foo\\tbar\"", "\\t");
		AssertRead<string> ("foo\\bar", "\"foo\\\\bar\"", "\\");
		AssertRead<string> ("foo\u005Cbar", "\"foo\\u005Cbar\"", "Unicode escape seq");
	}

	[Test]
	public void TestWriteValueTypes ()
	{
		Assert.AreEqual ("true", true.ToJson ());
		Assert.AreEqual ("false", false.ToJson ());
		Assert.AreEqual ("10", 10.ToJson ());
		Assert.AreEqual ("55.7", 55.7f.ToJson ());
		Assert.AreEqual ("-5", (-5).ToJson ());
	}

	[Test]
	public void TestReadValueTypes ()
	{
		AssertRead<bool>  (true, "true", "true");
		AssertRead<bool>  (false, "false", "false");
		AssertRead<int>   (10, "10", "10");
		AssertRead<float> (55.7f, "55.7", "55.7f");
		AssertRead<double>(48.2d, "48.2", "48.2d");
		AssertRead<int>   (-5, "-5", "-5");
		AssertRead<long>  ((long)(13e7), "13e7", "13e7");
	}

	[Test]
	public void TestWriteObjects ()
	{
		Assert.AreEqual ("{\"Foo\":\"bar\",\"Baz\":false,\"Xam\":{\"Nested\":10.5}}",
		              new {  Foo  = "bar",   Baz = false,  Xam=new{ Nested = 10.5f } }.ToJson ());

		Assert.AreEqual ("{\"Ten\":10,\"Hoopla\":true,\"Monkey\":\"Awesome\"}",
			new Dictionary<string,object>() { { "Ten", 10 }, { "Hoopla", true }, { "Monkey", "Awesome" } }.ToJson ());

		Assert.AreEqual ("[1,2,4,3]", new[] { 1, 2, 4, 3 }.ToJson ());
		Assert.AreEqual ("[\"hello\",10,{\"hi\":5}]",
			new List<object> () { "hello", 10, new { hi = 5 } }.ToJson ());
	}

	[Test]
	public void TestReadObjects ()
	{
		var dict = JsonReader.Parse<Dictionary<string,object>> ("{\"key1\":1,\"key2\":2}");
		Assert.That (dict.Count == 2 && ((double)dict ["key1"]) == 1 && ((double)dict ["key2"]) == 2, "IDictionary");


	}

	static void AssertRead<T> (T expected, string json, string message)
	{
		Assert.AreEqual (expected, JsonReader.Parse<T> (json), message);
	}
}

