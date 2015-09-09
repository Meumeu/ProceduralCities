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
			public double Score;
			public int NearestCity;

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
		public double[,] EdgeCost;

		public List<Biome> Biomes;
		public List<City> Cities;
		public List<Road> Roads;
		public Pathfinding PathToOcean;
		public Pathfinding PathToNearestCity;

		public void Build()
		{
			if (!Built)
			{
				var watch = System.Diagnostics.Stopwatch.StartNew();

				Log("Initializigng PRNG with seed " + Seed);
				PRNG = new System.Random(Seed);

				Log("Building icosphere");
				Icosphere sphere = new Icosphere(8);
				Vertices = sphere.Vertices;
				//Edges = ExpandEdges(sphere.Edges);
				Edges = sphere.Edges;

				double dist = 0;
				int nbEdges = 0;
				foreach (var i in AllEdges())
				{
					dist += Coordinates.Distance(Vertices[i.item1].coord, Vertices[i.item2].coord);
					nbEdges++;
				}

				Log(string.Format("{0} edges, average length: {1:F2} km, total length: {2:F2} km", nbEdges / 2, dist * Radius() / nbEdges / 1000, dist * Radius() / 2 / 1000));


				Log("Computing terrain and biome");
				FillTerrainAndBiome();

				Log("Computing distance between terrain and water");
				PathToOcean = new Pathfinding(this, Enumerable.Range(0, Vertices.Count).Where(i => Vertices[i].TerrainHeight < 0));

				Log("Building cities");
				BuildCities();

				Log("Computing zones of influence");
				BuildZonesOfInfluence();

				Log("Computing major roads");
				BuildRoads();

				Built = true;
				BuildFinished(fromCache: false);
				Log(string.Format("{0} vertices, elapsed: {1}", Vertices.Count, watch.Elapsed));
			}
			else
			{
				BuildFinished(fromCache: true);
			}
		}

		IEnumerable<Pair<int, int>> AllEdges()
		{
			for(int i = 0, n = Edges.GetLength(0); i < n; i++)
			{
				for (int j = 0, m = Edges.GetLength(1); j < m && Edges[i, j] >= 0; j++)
				{
					if (i < Edges[i, j])
						yield return new Pair<int, int>(i, Edges[i, j]);
				}
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

				Vertices[i].Score = Biomes[Vertices[i].Biome].Desirability;

				sumScores += Vertices[i].Score;
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
					while (r > Vertices[potentialCities[j]].Score)
					{
						r -= Vertices[potentialCities[j++]].Score;
					}
				} while (Cities.Any(x => x.Position == potentialCities[j]));

				City c = new City();
				c.Position = potentialCities[j];
				Cities.Add(c);
				// Log(string.Format("Founded city {0}, position: {1}, {2}, altitude: {3} m", i + 1, Vertices[potentialCities[j]].coord, Biomes[Vertices[potentialCities[j]].Biome].Name, Vertices[potentialCities[j]].TerrainHeight));
			}
		}
		#endregion

		#region Compute zones of influence
		void BuildZonesOfInfluence()
		{
			PathToNearestCity = new Pathfinding(this, Cities.Select(x => x.Position));

			for (int i = 0, n = Vertices.Count; i < n; i++)
			{
				if (PathToNearestCity.Nodes[i].visited)
					Vertices[i].NearestCity = PathToNearestCity.GetPath(i).Last();
				else
					Vertices[i].NearestCity = -1;
			}
		}
		#endregion

		#region Find neighbors
		HashSet<Pair<int, int>> NeighborCities()
		{
			HashSet<Pair<int, int>> ret = new HashSet<Pair<int, int>>();
			foreach (var i in AllEdges())
			{
				if (Vertices[i.item1].NearestCity == Vertices[i.item2].NearestCity)
					continue;

				if (Vertices[i.item1].NearestCity == -1 || Vertices[i.item2].NearestCity == -1)
					continue;

				int min = Math.Min(Vertices[i.item1].NearestCity, Vertices[i.item2].NearestCity);
				int max = Math.Max(Vertices[i.item1].NearestCity, Vertices[i.item2].NearestCity);
				ret.Add(new Pair<int, int>(min, max));
			}

			return ret;
		}
		#endregion

		#region Build major roads
		void BuildRoads()
		{
			HashSet<Pair<int, int>> neighborCities = NeighborCities();

			// Make roads
			var roadSegments = new Dictionary<int, List<int>>();
			foreach (Pair<int, int> i in neighborCities)
			{
				Pathfinding path = new Pathfinding(this, i.item1, i.item2);
				List<int> points = path.GetPath(i.item2).ToList();

				if (points.All(x => Vertices[x].NearestCity == i.item1 || Vertices[x].NearestCity == i.item2))
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
					if (Roads.Any(road => road.Positions[road.Positions.Count - 1] == i && road.Positions[road.Positions.Count - 2] == j))
						continue;

					Road r = new Road();

					r.Positions.Add(i);

					int k = j;
					while (!roadEnd.Contains(k))
					{
						r.Positions.Add(k);
						k = roadSegments[k].First(x => x != r.Positions[r.Positions.Count - 2]);
					}
					r.Positions.Add(k);

					System.Diagnostics.Debug.Assert(r.Positions.All(x => r.Positions.Count(y => x == y) == 1));

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

			int nbSamples = 10;

			coords.Clear();
			var edges = AllEdges().Where(j => Vertices[j.item1].TerrainHeight > 0 && Vertices[j.item2].TerrainHeight > 0).ToList();

			foreach (var i in edges)
			{
				var v1 = Vertices[i.item1].coord;
				var v2 = Vertices[i.item2].coord;

				coords.AddRange(Enumerable.Range(0, nbSamples).Select(j => new Coordinates(
					v1.x * j + v2.x * (nbSamples - 1 - j),
					v1.y * j + v2.y * (nbSamples - 1 - j),
					v1.z * j + v2.z * (nbSamples - 1 - j))));
			}

			var terrain = GetTerrain(coords);

			EdgeCost = new double[Edges.GetLength(0), Edges.GetLength(1)];

			for (int i = 0, n = edges.Count; i < n; i++)
			{
				double cost = 0;
				for (int j = 1; j < nbSamples; j++)
				{
					cost += Math.Abs(terrain[i * nbSamples + j] - terrain[i * nbSamples + j - 1]);
				}
				cost /= nbSamples;

				for (int j = 0, m = Edges.GetLength(1); j < m; j++)
				{
					if (Edges[edges[i].item1, j] == edges[i].item2)
						EdgeCost[edges[i].item1, j] = cost;
					if (Edges[edges[i].item2, j] == edges[i].item1)
						EdgeCost[edges[i].item2, j] = cost;
				}
			}
		}

		#region To be implemented by derived classes
		protected abstract List<Pair<double, int>> GetTerrainAndBiome(List<Coordinates> coords);
		protected virtual List<double> GetTerrain(List<Coordinates> coords)
		{
			return GetTerrainAndBiome(coords).Select(x => x.item1).ToList();
		}

		protected virtual List<int> GetBiome(List<Coordinates> coords)
		{
			return GetTerrainAndBiome(coords).Select(x => x.item2).ToList();
		}

		public abstract double Radius();
		protected abstract void Log(string message);
		protected virtual void BuildFinished(bool fromCache) {}
		#endregion
	}
}
