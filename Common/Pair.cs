using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralCities
{
	/*[Serializable]
	public class Pair<T1, T2>: IComparable<Pair<T1, T2>> where T1: IComparable<T1> where T2: IComparable<T2>
	{
		public readonly T1 item1;
		public readonly T2 item2;

		public Pair(T1 item1, T2 item2)
		{
			this.item1 = item1;
			this.item2 = item2;
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", item1, item2);
		}

		public int CompareTo(Pair<T1, T2> other)
		{
			int ret = item1.CompareTo(other.item1);
			if (ret == 0)
				ret = item2.CompareTo(other.item2);
			return ret;
		}

		public override int GetHashCode()
		{
			return item1.GetHashCode() ^ item2.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			Pair<T1, T2> other = obj as Pair<T1, T2>;
			if (obj == null)
				return false;

			return item1.Equals(other.item1) && item2.Equals(other.item2);
		}
	}*/

	[Serializable]
	public struct PairIntInt
	{
		public int item1;
		public int item2;
		public PairIntInt(int a, int b)
		{
			item1 = a;
			item2 = b;
		}
	}

	[Serializable]
	public struct PairIntDouble
	{
		public int item1;
		public double item2;
		public PairIntDouble(int a, double b)
		{
			item1 = a;
			item2 = b;
		}
	}
}
