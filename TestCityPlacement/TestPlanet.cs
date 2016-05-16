using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ProceduralCities
{
	class TestTerrain : PlanetData
	{
		byte[,] biomes;
		float[,] terrain;
		Planet.Biome[] biomeList;

		public TestTerrain(string map)
		{
			Console.WriteLine("Loading data");
			using (BinaryReader reader = new BinaryReader(File.Open(map, FileMode.Open)))
			{
				int width = reader.ReadInt32();
				int height = reader.ReadInt32();
				terrain = new float[height, width];

				for (int i = 0; i < height; i++)
				{
					for (int j = 0; j < width; j++)
					{
						terrain[i, j] = reader.ReadSingle();
					}
				}

				width = reader.ReadInt32();
				height = reader.ReadInt32();
				biomes = new byte[height, width];
				int nb_biomes = 0;
				for (int i = 0; i < height; i++)
				{
					for (int j = 0; j < width; j++)
					{
						biomes[i, j] = reader.ReadByte();
						nb_biomes = Math.Max(biomes[i, j] + 1, nb_biomes);
					}
				}

				biomeList = new Planet.Biome[nb_biomes];
				for (int i = 0; i < nb_biomes; i++)
				{
					double d;
					string n = reader.ReadString();
					switch (n)
					{
					case "Ice Caps":
					case "Water":
						d = 0;
						break;
					case "Deserts":
						d = 0.1;
						break;
					default:
						d = 1.0;
						break;
					}
					biomeList[i] = new Planet.Biome(n, d);
				}
			}
		}

		double GetTerrainHeight(double Latitude, double Longitude)
		{
			double i = ((Latitude + Math.PI / 2) / Math.PI * terrain.GetLength(0));
			double j = ((Longitude + Math.PI) / (2 * Math.PI) * terrain.GetLength(1)) % terrain.GetLength(1);

			int i1 = (int)i;
			int j1 = (int)j;

			if (i1 < 0)
				i1 = 0;
			else if (i1 >= terrain.GetLength(0))
				i1 = terrain.GetLength(0) - 1;

			if (j1 < 0)
				j1 += terrain.GetLength(1);

			int i2 = Math.Min(i1 + 1, terrain.GetLength(0) - 1);
			int j2 = (j1 + 1) % terrain.GetLength(1);

			double u = i - i1;
			double v = j - j1;

			// Bilinear interpolation
			return 
				(terrain[i1, j1] * (1 - u) + terrain[i2, j1] * u) * (1 - v) +
				(terrain[i1, j2] * (1 - u) + terrain[i2, j2] * u) * v;
		}

		public void GetTerrainGradient(double Latitude, double Longitude, out double x, out double y)
		{
			double i = (Latitude + Math.PI / 2) / Math.PI * terrain.GetLength(0);
			double j = ((Longitude + Math.PI) / (2 * Math.PI) * terrain.GetLength(1)) % terrain.GetLength(1);

			int i1 = (int)i;
			int j1 = (int)j;

			if (i1 < 0)
				i1 = 0;
			else if (i1 >= terrain.GetLength(0))
				i1 = terrain.GetLength(0) - 1;

			if (j1 < 0)
				j1 += terrain.GetLength(1);

			int i2 = Math.Min(i1 + 1, terrain.GetLength(0) - 1);
			int j2 = (j1 + 1) % terrain.GetLength(1);

			double u = i - i1;
			double v = j - j1;

			double x_scale = 600000 * 2 * Math.PI / terrain.GetLength(0) * Math.Cos(Latitude);
			double y_scale = 600000 * 2 * Math.PI / terrain.GetLength(1);

			x = (
				(terrain[i2, j1] - terrain[i1, j1]) * (1 - v) +
				(terrain[i2, j2] - terrain[i1, j2]) * v) * x_scale;

			y = (
				(terrain[i1, j2] * (1 - u) + terrain[i2, j2] * u) +
				(terrain[i1, j1] * (1 - u) + terrain[i2, j1] * u)) * y_scale;

		}

		byte GetBiomeId(double Latitude, double Longitude)
		{
			int i = (int)(((Latitude + Math.PI / 2) / Math.PI * biomes.GetLength(0))) % biomes.GetLength(0);
			int j = (int)(((Longitude + Math.PI) / (2 * Math.PI) * biomes.GetLength(1))) % biomes.GetLength(1);

			if (i < 0)
				i += biomes.GetLength(0);
			if (j < 0)
				j += biomes.GetLength(1);

			return biomes[i , j];
		}

		public List<Pair<double, int>> GetTerrainAndBiome(List<Coordinates> coords)
		{
			return coords.Select(x => new Pair<double, int>(
				GetTerrainHeight(x.Latitude, x.Longitude),
				GetBiomeId(x.Latitude, x.Longitude))).ToList();
		}

		public List<double> GetTerrain(List<Coordinates> coords)
		{
			return coords.Select(x => GetTerrainHeight(x.Latitude, x.Longitude)).ToList();
		}

		public Planet.Biome[] GetBiomeList()
		{
			return biomeList;
		}

		public double Radius
		{
			get	{ return 600000; }
		}
	}

	public class TestPlanet : Planet
	{
		public TestPlanet(string map) : base(new TestTerrain(map), 0)
		{
		}

		public double GetTerrainHeight(double lat, double lon)
		{
			return Terrain.GetTerrain(new List<Coordinates>() { new Coordinates(lat, lon) }).First();
		}

		#region Interface to Planet
		protected override void Log(string message)
		{
			Console.WriteLine(message);
		}
		#endregion
	}
}

