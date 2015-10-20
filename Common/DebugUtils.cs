using System;

namespace ProceduralCities
{
	public static class DebugUtils
	{
		[System.Diagnostics.Conditional("DEBUG")]
		static public void Assert(bool condition)
		{
			if (!condition)
				throw new Exception("Assertion failure");
		}
	}
}
