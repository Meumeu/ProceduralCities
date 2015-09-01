using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProceduralCities
{
	public class KSPPlanet : Planet
	{
		internal int Seed;
		internal string Name;

//		SimplexNoise noise;
		CelestialBody Body;

		public KSPPlanet()
		{
		}

		public void Load(ConfigNode node)
		{
			Debug.Log("[ProceduralCities] Loading " + node.name);

			foreach(var i in FlightGlobals.Bodies)
			{
				if (i.name == node.name)
				{
					Body = i;
					break;
				}
			}

			if (Body == null)
			{
				throw new ArgumentException("Celestial body not found: " + node.name);
			}

			Name = node.name;

			// FIXME: don't hardcode values
			Biomes = Body.BiomeMap.Attributes.Select(x => new Biome() {
				Name = x.name,
				Desirability = x.name == "Ice Caps" ? 0.0 : x.name == "Water" ? 0.0 : x.name == "Deserts" ? 0.1 : 1.0
			}).ToList();

			try
			{
				Seed = int.Parse(node.GetValue("seed"));
			}
			finally
			{
			}
		}

		public void Save(ConfigNode node)
		{
			node.name = Name;
			node.AddValue("seed", Seed);
		}

		public double GetTerrainHeight(double lat, double lon)
		{
			return Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(lat * 180 / Math.PI, lon * 180 / Math.PI)) - Body.Radius;
		}

		public string GetBiomeName(double lat, double lon)
		{
			if (Body.BiomeMap == null)
				return "";

			return Body.BiomeMap.GetAtt(lat, lon).name;
		}

		public int GetBiome(double lat, double lon)
		{
			if (Body.BiomeMap == null)
				return -1;

			var attr = Body.BiomeMap.GetAtt(lat, lon);
			for (int i = 0, n = Body.BiomeMap.Attributes.Length; i < n; i++)
			{
				if (attr == Body.BiomeMap.Attributes[i])
					return i;
			}

			Debug.Log("[ProceduralCities] Unknown biome ???");
			return -1;
		}

		/*
		public Texture2D GetHeightMap(double resolution)
		{
			if (body.pqsController == null)
				return null;
			
			double minValue = double.MaxValue;
			double maxValue = double.MinValue;

			double[,] values = new double[(int)(360 * resolution), (int)(180 * resolution)];

			for (int lon = 0; lon < 360 * resolution; lon++)
			{
				for (int lat = 0; lat < 180 * resolution; lat++)
				{
					double flon = (lon / resolution - 180) * Math.PI / 180.0;
					double flat = (90 - lat / resolution) * Math.PI / 180.0;
					values[lon, lat] = Math.Max(0, GetTerrainHeight(flon, flat));

					if (lon % 50 == 0 && lat % 50 == 0)
						Debug.Log("[ProceduralCities] Lon=" + (flon * 180 / Math.PI) + ", Lat=" + (flat * 180 / Math.PI) + ", alt=" + values[lon, lat]);

					minValue = Math.Min(minValue, values[lon, lat]);
					maxValue = Math.Max(maxValue, values[lon, lat]);
				}
			}

			Debug.Log("[ProceduralCities] Min altitude: " + minValue);
			Debug.Log("[ProceduralCities] Max altitude: " + maxValue);

			return Utils.TextureFromArrayHeight(values, minValue, maxValue);
		}*/

		public void ExportData(int width, int height)
		{
			string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			using (BinaryWriter writer = new BinaryWriter(File.Open(dir + "/" + Name + ".dat", FileMode.Create)))
			{
				writer.Write(width);
				writer.Write(height);
				for (int i = 0; i < height; i++)
				{
					double lat = ((double)i / (double)height) * Math.PI - Math.PI / 2;
					for (int j = 0; j < width; j++)
					{
						double lon = ((double)j / (double)width) * 2 * Math.PI - Math.PI;
						writer.Write((float)GetTerrainHeight(lat, lon));
					}
				}

				Dictionary<string, byte> biome_to_id = new Dictionary<string, byte>();
				Dictionary<byte, string> id_to_biome = new Dictionary<byte, string>();

				writer.Write(width);
				writer.Write(height);
				for (int i = 0; i < height; i++)
				{
					double lat = ((double)i / (double)height) * Math.PI  - Math.PI/2;
					for (int j = 0; j < width; j++)
					{
						double lon = ((double)j / (double)width) * 2 * Math.PI - Math.PI;
						string biome = GetBiomeName(lat, lon);
						byte id;
						if (!biome_to_id.TryGetValue(biome, out id))
						{
							id = (byte)biome_to_id.Count;
							biome_to_id.Add(biome, id);
							id_to_biome.Add(id, biome);
						}

						writer.Write(id);
					}
				}

				for(byte i = 0; i < biome_to_id.Count; i++)
				{
					writer.Write(id_to_biome[i]);
				}
			}
		}

		public void Update(Coordinates coord)
		{
			PlanetDatabase.Log(string.Format("[ProceduralCities] New position: {0} {1}", Name, coord));
		}

		#region Interface to Planet
		protected override List<Pair<double, int>> GetTerrainAndBiome(List<Coordinates> coords)
		{
			List<Pair<double, int>> ret = new List<Pair<double, int>>(coords.Count);
			bool finished = false;


			PlanetDatabase.QueueToMainThread(() =>
			{
				Monitor.Enter(ret);
				try
				{
					foreach(Coordinates i in coords)
					{
						ret.Add(new Pair<double, int>(
							GetTerrainHeight(i.Latitude, i.Longitude),
							GetBiome(i.Latitude, i.Longitude)
						));
					}
					finished = true;
					Monitor.Pulse(ret);
				}
				finally
				{
					Monitor.Exit(ret);
				}
			});

			Monitor.Enter(ret);
			while(!finished)
				Monitor.Wait(ret);
			Monitor.Exit(ret);
			PlanetDatabase.Log("Got terrain data");

			return ret;
		}

		protected override void Log(string message)
		{
			PlanetDatabase.Log(message);
		}
		#endregion
	}
}
