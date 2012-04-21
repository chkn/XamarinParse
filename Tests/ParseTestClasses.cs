using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Parse;

public abstract class ParseTestObject : ParseObject { 
	public abstract string SomeString { get; set; }
	public abstract int SomeNumber { get; set; }
	public abstract ParseObject SomeOtherObject { get; set; }
	
	public ParseTestObject (string klass)
		: base (klass)
	{
	}
}

public class ParseObjectLooselyTyped : ParseTestObject {
	
	public override string SomeString {
		get { return (string) this ["SomeString"]; }
		set { this ["SomeString"] = value; }
	}

	public override int SomeNumber {
		get { return (int) this ["SomeInt"]; }
		set { this ["SomeInt"] = value; }
	}

	public override ParseObject SomeOtherObject {
		get { return (ParseObject) this ["SomeOtherObject"]; }
		set { this ["SomeOtherObject"] = value; }
	}	
	
	public ParseObjectLooselyTyped ()
		: base ("TestObject")
	{
	}
}

public class ParseObjectStronglyTyped : ParseTestObject {
	
	[ParseKey]
	public override string SomeString {
		get;
		set;
	}
	
	[ParseKey ("SomeInt")]
	public override int SomeNumber {
		get;
		set;
	}
	
	[ParseKey]
	public override ParseObject SomeOtherObject {
		get;
		set;
	}

	public ParseObjectStronglyTyped ()
		: base ("TestObject")
	{
	}
}

