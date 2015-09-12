using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralCities
{
	public class Bezier
	{
		List<Coordinates> coord;
		double radius;
		double length;

		double fact(int n)
		{
			double ret = 1;
			for (int i = 2; i < n; i++)
				ret *= i;

			return ret;
		}

		double Cnp(int n, int p)
		{
			return fact(n) / (fact(p) * fact(n - p));
		}

		public Bezier(List<Coordinates> coordinates, double radius)
		{
			if (coordinates.Count < 2)
				throw new ArgumentException("At least two coordinates are required", "coordinates");

			coord = coordinates;
			this.radius = radius;
			length = 0;

			for (int i = 1, n = coord.Count; i < n; i++)
			{
				length += Coordinates.Distance(coord[i - 1], coord[i]);
			}
		}

		public Coordinates Eval(double t)
		{
			if (t < 0)
				t = 0;
			else if (t > 1)
				t = 1;

			double x = 0;
			double y = 0;
			double z = 0;

			for (int i = 0; i < coord.Count; i++)
			{
				double coef = Math.Pow(t, i) * Math.Pow(1 - t, coord.Count - i - 1) * Cnp(coord.Count - 1, i);

				x += coef * coord[i].x;
				y += coef * coord[i].y;
				z += coef * coord[i].z;
			}

			return new Coordinates(x, y, z);
		}

		public IEnumerable<Coordinates> Rasterize(double distance)
		{
			double incr = distance / (radius * length);

			for (double i = 0; i < 1; i += incr)
			{
				yield return Eval(i);
			}

			yield return Eval(1);
		}
	}
}

