using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralCities
{
	public class Pair<T1, T2>: IComparable<Pair<T1, T2>> where T1: IComparable<T1> where T2: IComparable<T2>
	{
		public T1 item1;
		public T2 item2;

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
	}
}

