using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralCities
{
	public interface PlanetData
	{
		PairIntDouble[] GetTerrainAndBiome(Coordinates[] coords);

		double[] GetTerrain(Coordinates[] coords);

		Planet.Biome[] BiomeList { get; }
		double Radius { get; }
	}

	public class TileData
	{
		const int TerrainSamplingResolution = 6;
		const double CityScoreThreshold = 0.9;
		const double CityMinAltitude = 0;
		const double CityMaxAltitude = 2000;

		Planet planet;
		int index;
		public int ComputedLevel { get; private set; }
		System.Random PRNG;

		public TileData(Planet planet, int index)
		{
			this.planet = planet;
			this.index = index;
			PRNG = new System.Random(planet.TileSeeds[index]);
			ComputedLevel = 0;
		}

		IEnumerable<TileData> Neighbours
		{
			get
			{
				return planet.Tiles[index].Neighbours.Select(x => planet.data[x]);
			}
		}

		public Coordinates[] SamplePoints;
		public double[] SampleAltitudes;
		public int[] SampleBiomes;
		double CityScore;
		public bool? HasCity { get; private set; }

		public void Load(int level)
		{
			if (ComputedLevel >= level)
				return;

			if (ComputedLevel == 0 && level >= 1)
			{
				SamplePoints = planet.Tiles[index].Grid(TerrainSamplingResolution);

				var data = planet.Terrain.GetTerrainAndBiome(SamplePoints);

				SampleAltitudes = data.Select(x => x.item2).ToArray();
				SampleBiomes = data.Select(x => x.item1).ToArray();

				if (SampleAltitudes.Max() < 0 || SampleAltitudes.Min() > CityMaxAltitude)
					CityScore = 0;
				else
					CityScore = PRNG.NextDouble();

				ComputedLevel = 1;
			}

			if (ComputedLevel == 1 && level >= 2)
			{
				if (CityScore > CityScoreThreshold)
				{
					bool tmp = true;
					foreach (var i in Neighbours)
					{
						i.Load(1);
						tmp = tmp && i.CityScore < CityScore;
						if (!tmp)
							break;
					}
					HasCity = tmp;
				}
				else
				{
					HasCity = false;
				}

				ComputedLevel = 2;
			}

			if (ComputedLevel == 2 && level >= 3)
			{
				var ClosestCity = new Dictionary<int, PairIntDouble>();

				ComputedLevel = 3;
			}
		}

	}

	public class Planet
	{
		public class Biome
		{
			public readonly string Name;
			public readonly double Desirability;

			public Biome(string name, double desirability)
			{
				this.Name = name;
				this.Desirability = desirability;
			}
		}

		public readonly int Seed;

		public readonly PlanetData Terrain;

		public readonly Biome[] Biomes;
		public readonly TileGeometry[] Tiles;
		public readonly int[] TileSeeds;
		public readonly LRUCache<TileData> data;

		static Icosphere sphere;

		public double Radius { get { return Terrain.Radius; } }

		public Planet(PlanetData terrain, int seed)
		{
			Terrain = terrain;
			Biomes = Terrain.BiomeList;
			Seed = seed;

			if (sphere == null)
			{
				Log("Building tile geometry");
				sphere = new Icosphere(7);
			}

			#if DEBUG
			var watch = System.Diagnostics.Stopwatch.StartNew();
			#endif
			Tiles = sphere.GetDual();
			Log("{0} tiles", Tiles.Length);
			#if DEBUG
			Log("Elapsed: {0} ms", watch.ElapsedMilliseconds);
			#endif
			
			var PRNG = new System.Random(seed);
			TileSeeds = new int[Tiles.Length];
			for (int i = 0; i < Tiles.Length; i++)
			{
				TileSeeds[i] = PRNG.Next();
			}

			data = new LRUCache<TileData>(idx => new TileData(this, idx), 200);
		}

		public int FindTile(Coordinates c, int hint = -1)
		{
			int[] indices;
			if (hint < 0)
			{
				indices = Enumerable.Range(0, Tiles.Length).ToArray();
			}
			else
			{
				var tmp = Tiles[hint].Neighbours;
				indices = new int[1 + tmp.Length];
				indices[0] = hint;
				for (int i = 0; i < tmp.Length; i++)
					indices[i + 1] = tmp[i];
			}

			int bestidx = -1;
			double bestdist = double.MaxValue;
			foreach (int i in indices)
			{
				double dist = Coordinates.Distance(c, Tiles[i].Center);
				if (dist < bestdist)
				{
					bestidx = i;
					bestdist = dist;
				}
			}

			return bestidx;
		}

		public List<int> FindTiles(Coordinates c, double distance, int hint = -1)
		{
			/*List<int> ret = new List<int>();
			ret.Add(FindTile(c, hint));

			while(true)
			{
				List<int> newtiles = ret.SelectMany(x => Tiles[x].Neighbours).Where(x => Coordinates.Distance(Tiles[x].Center, c) < distance && !ret.Contains(x)).ToList();
 				if (newtiles.Count == 0)
					return ret;
				ret.AddRange(newtiles);
			}*/

			return Enumerable.Range(0, Tiles.Length).Where(x => Coordinates.Distance(Tiles[x].Center, c) < distance).ToList();
		}

		void Log(string format, params object[] args)
		{
			Log(string.Format(format, args));
		}

		#region To be implemented by derived classes
		protected virtual void Log(string message) {}
		#endregion
	}
}
