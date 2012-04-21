using System;
using System.Diagnostics;
using NUnit.Framework;

public abstract class TestBase {
	private bool test_complete;
	
	[TestFixtureSetUp]
	public virtual void TestFixtureSetUp ()
	{
	}

	[SetUp]
	public virtual void PreTest ()
	{
		test_complete = false;
	}
	
	protected void TestComplete ()
	{
		test_complete = true;
	}
	
	[TearDown]
	// Pump the Cirrus event loop until the test completes
	public virtual void PostTest ()
	{
		try {
			while (!test_complete)
				Cirrus.Thread.Current.RunSingleIteration ();

		} catch (Exception e) {
			Debug.WriteLine ("Test failed: " + e.Message);
			Debug.WriteLine (e.StackTrace);
			throw;
		}
	}
}


