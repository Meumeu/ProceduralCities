using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralCities
{
	public abstract class Planet
	{
		public class Vertex
		{
			public Coordinates coord;
			public int Biome;
			public double TerrainHeight;

			// Dijkstra stuff
			public int origin;
			public double distance;
			public bool visited;

			// City generation stuff
			public double score;

			public Vertex(Coordinates c)
			{
				coord = c;
				Biome = 0;
				TerrainHeight = 0;
				origin = 0;
				distance = 0;
				score = 0;
			}
		}

		public class Biome
		{
			public string Name;
			public double Desirability;
		}

		public class City
		{
			public int Position;
		}

		public List<Vertex> Vertices;
		public int[,] Edges;

		public List<Biome> Biomes;
		public List<City> Cities;

		protected void Build()
		{
			Console.WriteLine("Building icosphere");
			Icosphere sphere = new Icosphere(8);
			Vertices = sphere.Vertices;
			Edges = sphere.Edges;

			Console.WriteLine("Computing terrain and biome");
			FillTerrainAndBiome();
			Console.WriteLine("Computing distance between terrain and water");
			ComputeDistanceToWater();
			Console.WriteLine("Building cities");
			BuildCities();
			Console.WriteLine("Computing route to cities");
			ComputeDistanceToCities();
		}

		#region Dijkstra

		IEnumerable<int> GetNeighbors(int index)
		{
			for (int i = 0; i < 6; i++)
			{
				if (Edges[index, i] == -1)
					yield break;
				yield return Edges[index, i];
			}
		}

		public void Dijkstra(List<int> origins)
		{
			// Unvisited nodes at the border
			var unvisited = new SortedDictionary<Pair<double, int>, int>();

			foreach (Vertex v in Vertices)
			{
				v.distance = double.MaxValue;
				v.visited = false;
			}

			foreach (int i in origins)
			{
				Vertices[i].distance = 0;
				Vertices[i].origin = i;
				unvisited.Add(new Pair<double, int>(0, i), 0);
			}

			while (unvisited.Count > 0)
			{
				Pair<double, int> current = unvisited.First().Key;
				unvisited.Remove(current);

				double currentDistance = current.item1;
				int currentIdx = current.item2;

				// Update distances
				foreach (int j in GetNeighbors(currentIdx))
				{
					if (Vertices[j].visited)
						continue;

					double distance = currentDistance + Coordinates.Distance(Vertices[j].coord, Vertices[currentIdx].coord); // TODO: calculer distance entre current et j
					if (Vertices[j].distance > distance)
					{
						unvisited.Remove(new Pair<double, int>(Vertices[j].distance, j));
						Vertices[j].distance = distance;
						Vertices[j].origin = currentIdx;
						unvisited.Add(new Pair<double, int>(Vertices[j].distance, j), 0);
					}
				}

				Vertices[currentIdx].visited = true;
			}
		}
		#endregion

		void ComputeDistanceToWater()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<int> ocean = new List<int>();
			for (int i = 0, n = Vertices.Count; i < n; i++)
			{
				if (Vertices[i].TerrainHeight < 0)
					ocean.Add(i);
			}

			Console.WriteLine("Found {0} ocean nodes in {1}", ocean.Count, watch.Elapsed);
			Dijkstra(ocean);
			Console.WriteLine("Pathfinding in {0}", watch.Elapsed);
		}

		void ComputeDistanceToCities()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			List<int> cities = new List<int>();
			foreach (City i in Cities)
			{
				cities.Add(i.Position);
			}

			Dijkstra(cities);
			Console.WriteLine("Pathfinding in {0}", watch.Elapsed);

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
			var rand = new System.Random(0);
			for (int i = 0; i < 50; i++)
			{
				double r = rand.NextDouble() * sumScores;
				int j = 0;
				while (r > Vertices[potentialCities[j]].score)
				{
					r -= Vertices[potentialCities[j++]].score;
				}

				City c = new City();
				c.Position = potentialCities[j];
				Cities.Add(c);
				Console.WriteLine("Founded city {0}, position: {1}, {2}, altitude: {3} m", i + 1, Vertices[potentialCities[j]].coord, Biomes[Vertices[potentialCities[j]].Biome].Name, Vertices[potentialCities[j]].TerrainHeight);
			}
		}
		#endregion

		#region Build major roads
		#endregion

		#region To be implemented by derived classes
		protected abstract void FillTerrainAndBiome();
		#endregion
	}
}
