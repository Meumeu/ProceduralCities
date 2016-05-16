using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace ProceduralCities
{
	public interface IPlanetData
	{
		PairIntDouble[] GetTerrainAndBiome(Coordinates[] coords);

		double[] GetTerrain(Coordinates[] coords);

		Planet.Biome[] BiomeList { get; }
		double Radius { get; }
	}

	public class TileData
	{
		public enum Computationlevel
		{
			NOTHING = 0,
			TERRAIN_SAMPLED = 1,
			CITIES_PLACED = 2,
			CITY_BORDER_FOUND = 3,
			MAX = int.MaxValue
		};
		const int TerrainSamplingResolution = 6;
		const double CityScoreThreshold = 0.9;
		const double CityMinAltitude = 0;
		const double CityMaxAltitude = 2000;

		Planet planet;
		int index;
		public Computationlevel Level { get; private set; }
		System.Random PRNG;

		public TileData(Planet planet, int index)
		{
			this.planet = planet;
			this.index = index;

			Level = Computationlevel.NOTHING;
		}

		IEnumerable<TileData> Neighbours
		{
			get
			{
				return Planet.Tiles[index].Neighbours.Select(x => planet.data[x]);
			}
		}

		public Coordinates[] SamplePoints;
		public double[] SampleAltitudes;
		public int[] SampleBiomes;
		double CityScore;
		public bool? HasCity { get; private set; }
		public int ClosestCity;

		public void Load(Computationlevel reqlevel)
		{
			while (Level < reqlevel)
			{
				switch (Level)
				{
				case Computationlevel.NOTHING:
					SampleTerrain();
					break;

				case Computationlevel.TERRAIN_SAMPLED:
					PlaceCities();
					break;

				case Computationlevel.CITIES_PLACED:
					FindCityBorder();
					break;

				default:
					Level = Computationlevel.MAX;
					return;
				}
			}
		}

		void SampleTerrain()
		{
			PRNG = new System.Random(planet.TileSeeds[index]);

			SamplePoints = Planet.Tiles[index].Grid(TerrainSamplingResolution);

			var data = planet.Terrain.GetTerrainAndBiome(SamplePoints);

			SampleAltitudes = data.Select(x => x.item2).ToArray();
			SampleBiomes = data.Select(x => x.item1).ToArray();

			double minalt = SampleAltitudes.Min();
			double maxalt = SampleAltitudes.Max();

			if (maxalt < 0 || minalt > CityMaxAltitude)
				CityScore = 0;
			else
				CityScore = PRNG.NextDouble();
			
			Level = Computationlevel.TERRAIN_SAMPLED;
		}

		void PlaceCities()
		{
			if (CityScore > CityScoreThreshold)
			{
				bool tmp = true;
				foreach (var i in Neighbours)
				{
					i.Load(Computationlevel.TERRAIN_SAMPLED);
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

			Level = Computationlevel.CITIES_PLACED;
		}

		static public HashSet<int> _frontier;
		void FindCityBorder()
		{
			HashSet<int> loadedTiles = new HashSet<int>();
			HashSet<int> frontier = new HashSet<int>();

			loadedTiles.Add(index);

			bool borderfound = false;
			Pathfinding pf = null;
			int city = -1;

			while (!borderfound)
			{
				frontier = new HashSet<int>();
				foreach (var i in loadedTiles)
				{
					if (!planet.IsNodeAllowed(i))
						continue;
					
					foreach (var j in Planet.Tiles[i].Neighbours)
					{
						if (loadedTiles.Contains(j))
							continue;
						
						frontier.Add(j);
					}
				}

				if (frontier.Count == 0)
					break;

				foreach (var i in frontier)
				{
					planet.data[i].Load(Computationlevel.CITIES_PLACED);
					loadedTiles.Add(i);
				}

				var knownCities = loadedTiles.Where(x => planet.data[x].HasCity.GetValueOrDefault()).ToArray();

				if (knownCities.Length > 0)
				{
					pf = new Pathfinding(planet, knownCities);
					city = pf.GetPath(index).Last();
					borderfound = true;
					foreach (var i in frontier.Where(i => planet.IsNodeAllowed(i)))
					{
						if (pf.GetPath(i).Last() == city)
						{
							borderfound = false;
							break;
						}
					}
				}
				else
				{
					borderfound = false;
				}
			}

			if (pf != null)
			{
				city = pf.GetPath(index).Last();
				foreach (var i in loadedTiles.Where(i => planet.IsNodeAllowed(i)))
				{
					if (city == pf.GetPath(i).Last())
					{
						planet.data[i].ClosestCity = city;
						planet.data[i].Level = Computationlevel.CITY_BORDER_FOUND;
					}
				}
			}

			_frontier = frontier;

			Level = Computationlevel.CITY_BORDER_FOUND;

			foreach (var i in frontier)
			{
				DebugUtils.Assert(planet.data[i].ClosestCity != city);
			}
		}

		public bool Color(out byte r, out byte g, out byte b)
		{
			r = 0;
			g = 0;
			b = 0;

			if (Level == Computationlevel.NOTHING)
				return false;

			double minalt = SampleAltitudes.Min();
			double maxalt = SampleAltitudes.Max();

			if (minalt < 0)
			{
				r = 0;
				g = 0;
				b = 255;
			}
			else
			{
				double lambda = (maxalt - minalt) / 500;
				if (lambda < 0)
					lambda = 0;
				else if (lambda > 1)
					lambda = 1;

				r = (byte)(64 * lambda);
				g = (byte)(255 + (64 - 255) * lambda);
				b = (byte)(64 * lambda);
			}

			if (Level == Computationlevel.TERRAIN_SAMPLED)
				return true;

			if (HasCity.GetValueOrDefault())
			{
				r = 255;
				g = 0;
				b = 255;
			}

			return true;
		}
	}

	public class Planet : IPathfindingMap
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

		public readonly IPlanetData Terrain;

		public readonly Biome[] Biomes;
		public static TileGeometry[] Tiles;
		public readonly int[] TileSeeds;
		public readonly LRUCache<TileData> data;

		public double Radius { get { return Terrain.Radius; } }


		public Planet(IPlanetData terrain, int seed)
		{
			Terrain = terrain;
			Biomes = Terrain.BiomeList;
			Seed = seed;

			var PRNG = new System.Random(seed);

			int icosphereLevel = 7;

			if (Tiles == null)
			{
				string cachepath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/tiles.dat";

				try
				{
					using (var stream = new FileStream(cachepath, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						var reader = new BinaryReader(stream);

						var version = reader.ReadString();
						if (version != System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
						{
							throw new Exception(string.Format("Wrong version in cache file: found {0}, expected {1}", version, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version));
						}

						int level = reader.ReadInt32();
						if (level != icosphereLevel)
						{
							throw new Exception(string.Format("Wrong tessellation level in cache file: found {0}, expected {1}", level, icosphereLevel));
						}

						int n = reader.ReadInt32();
						Tiles = new TileGeometry[n];
						for (int i = 0; i < n; i++)
						{
							Tiles[i] = new TileGeometry(reader);
						}
					}
				}
				catch (Exception e)
				{
					Log("Cannot open geometry from cache: {0}", e.Message);

					var sphere = new Icosphere(icosphereLevel);
					Tiles = sphere.GetDual();

					try
					{
						using(var stream = new FileStream(cachepath, FileMode.Create, FileAccess.Write))
						{
							var writer = new BinaryWriter(stream);
							writer.Write(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
							writer.Write(icosphereLevel);
							writer.Write(Tiles.Length);
							for(int i = 0; i < Tiles.Length; i++)
							{
								Tiles[i].Write(writer);
							}
						}
					}
					catch(Exception e2)
					{
						Log("Cannot write geometry to cache: {0}", e2.Message);
					}
				}
			}

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

		#region Implementation of IPathfindingMap
		public IEnumerable<int> GetNeighbors(int index)
		{
			return Tiles[index].Neighbours;
		}

		public double Cost(int index1, int index2)
		{
			return Coordinates.Distance(Tiles[index1].Center, Tiles[index2].Center); // FIXME
		}

		public double Heuristic(int index1, int index2)
		{
			return Coordinates.Distance(Tiles[index1].Center, Tiles[index2].Center);
		}

		public bool IsNodeAllowed(int index)
		{
			if (!data.ContainsKey(index))
				return false;

			var node = data[index];
			if (node.Level < TileData.Computationlevel.CITIES_PLACED)
				return false;

			return (node.SampleAltitudes.Max() > 0);
		}

		public int NodeCount()
		{
			return Tiles.Length;
		}
		#endregion
	}
}
