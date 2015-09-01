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

			// City generation stuff
			public double score;

			public Vertex(Coordinates c)
			{
				coord = c;
				Biome = 0;
				TerrainHeight = 0;
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

		public class Road
		{
			public List<int> Positions;
		}

		public List<Vertex> Vertices;
		public int[,] Edges;

		public List<Biome> Biomes;
		public List<City> Cities;
		public List<Road> Roads;
		public Pathfinding PathToOcean;
		public Pathfinding PathToNearestCity;

		public void Build()
		{
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
		}

		#region Build major cities
		void BuildCities()
		{
			Log("BuildCities: 1");
			List<int> potentialCities = new List<int>();
			double sumScores = 0;
			Log("BuildCities: 2");
			for (int i = 0, n = Vertices.Count; i < n; i++)
			{
				if (Vertices[i].TerrainHeight < 0)
					continue;

				Vertices[i].score = Biomes[Vertices[i].Biome].Desirability;

				sumScores += Vertices[i].score;
				potentialCities.Add(i);
			}
			Log("BuildCities: 3");
			Cities = new List<City>();
			var rand = new System.Random(0);
			for (int i = 0; i < 50; i++)
			{
				int j;
				do
				{
					double r = rand.NextDouble() * sumScores;
					j = 0;
					while (r > Vertices[potentialCities[j]].score)
					{
						r -= Vertices[potentialCities[j++]].score;
					}
				} while (Cities.Any(x => x.Position == potentialCities[j]));

				City c = new City();
				c.Position = potentialCities[j];
				Cities.Add(c);
				Log(string.Format("Founded city {0}, position: {1}, {2}, altitude: {3} m", i + 1, Vertices[potentialCities[j]].coord, Biomes[Vertices[potentialCities[j]].Biome].Name, Vertices[potentialCities[j]].TerrainHeight));
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
			Roads = new List<Road>();
			foreach (Pair<int, int> i in neighborCities)
			{
				Pathfinding path = new Pathfinding(this, i.item1, i.item2);
				Road r = new Road();
				r.Positions = path.GetPath(i.item2).ToList();

				bool test = r.Positions.All(x => nearestCity[x] == i.item1 || nearestCity[x] == i.item2);
				if (test)
					Roads.Add(r);
			}
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
		#endregion
	}
}
