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

		public List<Vertex> Vertices;
		public int[,] Edges;

		public List<Biome> Biomes;
		public List<City> Cities;
		public Pathfinding PathToOcean;
		public Pathfinding PathToNearestCity;

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

		void ComputeDistanceToWater()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			PathToOcean = new Pathfinding(this, Enumerable.Range(0, Vertices.Count).Where(i => Vertices[i].TerrainHeight < 0));
			Console.WriteLine("Pathfinding in {0}", watch.Elapsed);
		}

		void ComputeDistanceToCities()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			PathToNearestCity = new Pathfinding(this, Cities.Select(x => x.Position));
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
