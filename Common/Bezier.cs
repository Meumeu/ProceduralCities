using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralCities
{
	public class Bezier : IEnumerable<Coordinates>
	{
		struct Patch
		{
			public readonly Coordinates c1;
			public readonly Coordinates c2;
			public readonly Coordinates c3;
			public readonly Coordinates c4;

			public Coordinates Eval(double t)
			{
				double t1 = (1 - t) * (1 - t) * (1 - t);
				double t2 = 3 * (1 - t) * (1 - t) * t;
				double t3 = 3 * (1 - t) * t * t;
				double t4 = t * t * t;

				return new Coordinates(
					t1 * c1.x + t2 * c2.x + t3 * c3.x + t4 * c4.x,
					t1 * c1.y + t2 * c2.y + t3 * c3.y + t4 * c4.y,
					t1 * c1.z + t2 * c2.z + t3 * c3.z + t4 * c4.z
				);
			}

			public Patch(Coordinates c1, Coordinates c2, Coordinates c3, Coordinates c4)
			{
				this.c1 = c1;
				this.c2 = c2;
				this.c3 = c3;
				this.c4 = c4;
			}
		}

		List<Patch> patches;

		public Bezier(List<Coordinates> coordinates)
		{
			if (coordinates.Count < 2)
				throw new ArgumentException("At least two coordinates are required", "coordinates");

			patches = new List<Patch>(coordinates.Count - 1);
			List<Coordinates> controlPoints = new List<Coordinates>(patches.Count * 2);

			// create additional control points
			controlPoints.Add(Coordinates.LinearCombination(0.5, coordinates[0], 0.5, coordinates[1]));
			for (int i = 1, n = coordinates.Count - 1; i < n; i++)
			{
				controlPoints.Add(Coordinates.LinearCombination(0.25, coordinates[i - 1], -0.25, coordinates[i + 1], 1, coordinates[i]));
				controlPoints.Add(Coordinates.LinearCombination(0.25, coordinates[i + 1], -0.25, coordinates[i - 1], 1, coordinates[i]));
			}
			controlPoints.Add(Coordinates.LinearCombination(0.5, coordinates[coordinates.Count - 2], 0.5, coordinates[coordinates.Count - 1]));

			// create patches
			for (int i = 0, n = coordinates.Count - 1; i < n; i++)
			{
				patches.Add(new Patch(coordinates[i], controlPoints[2 * i], controlPoints[2 * i + 1], coordinates[i + 1]));
			}
		}

		public Coordinates Eval(double t)
		{
			int index = (int)Math.Floor(t);
			if (index < 0)
				return patches[0].c1;
			else if (index >= patches.Count)
				return patches.Last().c4;

			return patches[index].Eval(t - Math.Floor(t));
		}

		public double MaxValue
		{
			get
			{
				return patches.Count;
			}
		}

		IEnumerable<Coordinates> Enumerator()
		{
			double n = MaxValue;
			for (double i = 0; i < n+1; i += 1)
			{
				yield return Eval(i);
			}
		}

		public IEnumerator<Coordinates> GetEnumerator()
		{
			return Enumerator().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Enumerator().GetEnumerator();
		}
	}
}

