using System;

namespace ProceduralCities
{
	public static class Utils
	{
		[System.Diagnostics.Conditional("DEBUG")]
		static protected void Assert(bool condition)
		{
			if (!condition)
				throw new Exception("Assertion failure");
		}
	}
}

