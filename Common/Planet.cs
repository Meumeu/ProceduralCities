using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralCities
{
	[Serializable]
	public abstract class Planet
	{
		[Serializable]
		public class Vertex
		{
			public Coordinates coord;
			public int Biome;
			public double TerrainHeight;

			// City generation stuff
			public double score;

			public Vertex(Coordinates c)
			{
				coord = c;
			}

			public Vertex()
			{
			}
		}

		[Serializable]
		public class Biome
		{
			public string Name;
			public double Desirability;

		}

		[Serializable]
		public class City
		{
			public int Position;
		}

		[Serializable]
		public class Road
		{
			public List<int> Positions = new List<int>();
		}

		public bool Built;
		public int Seed;
		Random PRNG;

		public List<Vertex> Vertices;
		public int[,] Edges;

		public List<Biome> Biomes;
		public List<City> Cities;
		public List<Road> Roads;
		public Pathfinding PathToOcean;
		public Pathfinding PathToNearestCity;

		public void Build()
		{
			if (!Built)
			{
				Log("Initializigng PRNG with seed " + Seed);
				PRNG = new System.Random(Seed);

				Log("Building icosphere");
				Icosphere sphere = new Icosphere(6);
				Vertices = sphere.Vertices;
				Edges = sphere.Edges;

				Log("Computing terrain and biome");
				FillTerrainAndBiome();

				Log("Computing distance between terrain and water");
				PathToOcean = new Pathfinding(this, Enumerable.Range(0, Vertices.Count).Where(i => Vertices[i].TerrainHeight < 0));

				Log("Building cities");
				BuildCities();

				Log("Computing zones of influence");
				PathToNearestCity = new Pathfinding(this, Cities.Select(x => x.Position));

				Log("Computing major roads");
				BuildRoads();

				Built = true;
				BuildFinished(fromCache: false);
			}
			else
			{
				BuildFinished(fromCache: true);
			}
		}

		#region Build major cities
		void BuildCities()
		{
			List<int> potentialCities = new List<int>();
			double sumScores = 0;
			for (int i = 0, n = Vertices.Count; i < n; i++)
			{
				if (Vertices[i].TerrainHeight < 0)
					continue;

				Vertices[i].score = Biomes[Vertices[i].Biome].Desirability;

				sumScores += Vertices[i].score;
				potentialCities.Add(i);
			}

			Cities = new List<City>();
			for (int i = 0; i < 50; i++)
			{
				int j;
				do
				{
					double r = PRNG.NextDouble() * sumScores;
					j = 0;
					while (r > Vertices[potentialCities[j]].score)
					{
						r -= Vertices[potentialCities[j++]].score;
					}
				} while (Cities.Any(x => x.Position == potentialCities[j]));

				City c = new City();
				c.Position = potentialCities[j];
				Cities.Add(c);
				// Log(string.Format("Founded city {0}, position: {1}, {2}, altitude: {3} m", i + 1, Vertices[potentialCities[j]].coord, Biomes[Vertices[potentialCities[j]].Biome].Name, Vertices[potentialCities[j]].TerrainHeight));
			}
		}
		#endregion

		#region Build major roads
		void BuildRoads()
		{
			// Compute zone of influence
			int[] nearestCity = new int[Vertices.Count];
			for (int i = 0, n = Vertices.Count; i < n; i++)
			{
				if (PathToNearestCity.Nodes[i].visited)
					nearestCity[i] = PathToNearestCity.GetPath(i).Last();
				else
					nearestCity[i] = -1;
			}

			// Find neighbor cities
			HashSet<Pair<int, int>> neighborCities = new HashSet<Pair<int, int>>();
			for (int i = 0, n = Vertices.Count; i < n; i++)
			{
				for (int j = 0; j < 6 && Edges[i, j] != -1; j++)
				{
					int k = Edges[i, j];
					if (nearestCity[i] == nearestCity[k])
						continue;

					if (nearestCity[i] == -1 || nearestCity[k] == -1)
						continue;

					if (k > i)
						continue;

					neighborCities.Add(new Pair<int, int>(nearestCity[i], nearestCity[k]));
				}
			}

			// Make roads
			var roadSegments = new Dictionary<int, List<int>>();
			foreach (Pair<int, int> i in neighborCities)
			{
				Pathfinding path = new Pathfinding(this, i.item1, i.item2);
				List<int> points = path.GetPath(i.item2).ToList();

				if (points.All(x => nearestCity[x] == i.item1 || nearestCity[x] == i.item2))
				{
					for (int j = 1, n = points.Count; j < n; j++)
					{
						if (roadSegments.ContainsKey(points[j - 1]))
						{
							if (!roadSegments[points[j - 1]].Contains(points[j]))
								roadSegments[points[j - 1]].Add(points[j]);
						}
						else
							roadSegments[points[j - 1]] = new[] { points[j] }.ToList();

						if (roadSegments.ContainsKey(points[j]))
						{
							if (!roadSegments[points[j]].Contains(points[j - 1]))
								roadSegments[points[j]].Add(points[j - 1]);
						}
						else
							roadSegments[points[j]] = new[] { points[j - 1] }.ToList();
					}
				}
			}

			// A road ends at an intersection (count > 2) or at a city
			var roadEnd = roadSegments.Where(x => x.Value.Count == 1 || x.Value.Count > 2 || Cities.Any(y => y.Position == x.Key)).Select(x => x.Key).ToList();

			Roads = new List<Road>();
			foreach (int i in roadEnd)
			{
				foreach (int j in roadSegments[i])
				{
					Road r = new Road();

					r.Positions.Add(i);

					int k = j;
					while (!roadEnd.Contains(k))
					{
						r.Positions.Add(k);
						k = roadSegments[k].First(x => x != r.Positions[r.Positions.Count - 2]);
					}
					r.Positions.Add(k);

					foreach(int l in r.Positions)
					{
						System.Diagnostics.Debug.Assert(r.Positions.Count(x => x == l) == 1);
					}

					Roads.Add(r);
				}

			}

			Log(string.Format("Created {0} roads", Roads.Count));
		}
		#endregion

		void FillTerrainAndBiome()
		{
			List<Coordinates> coords = Vertices.Select(x => x.coord).ToList();
			var data = GetTerrainAndBiome(coords);
			for(int i = 0, n = Vertices.Count; i < n; i++)
			{
				Vertices[i].TerrainHeight = data[i].item1;
				Vertices[i].Biome = data[i].item2;
			}
		}

		#region To be implemented by derived classes
		protected abstract List<Pair<double, int>> GetTerrainAndBiome(List<Coordinates> coords);
		protected abstract void Log(string message);
		protected virtual void BuildFinished(bool fromCache) {}
		#endregion
	}
}
