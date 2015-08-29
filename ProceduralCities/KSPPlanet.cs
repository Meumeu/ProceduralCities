using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public partial class KSPPlanet : MonoBehaviour, IConfigNode
	{
		internal Utils.EditableInt seed = new Utils.EditableInt("Seed");
		internal Utils.EditableDouble gain = new Utils.EditableDouble("Gain");
		internal Utils.EditableDouble lacunarity = new Utils.EditableDouble("Lacunarity");
		internal Utils.EditableDouble frequency = new Utils.EditableDouble("Frequency");
		internal Utils.EditableDouble amplitude = new Utils.EditableDouble("Amplitude");
		internal string _name;

		SimplexNoise noise;
		CelestialBody body;

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
					body = i;
					break;
				}
			}

			if (body == null)
			{
				throw new ArgumentException("Celestial body not found: " + node.name);
			}

			_name = node.name;

			seed.Set(node.GetValue("seed"));
			gain.Set(node.GetValue("gain"));
			lacunarity.Set(node.GetValue("lacunarity"));
			frequency.Set(node.GetValue("frequency") ?? (0.1 / body.Radius).ToString());
			amplitude.Set(node.GetValue("amplitude"));

			UpdateNoise();
		}

		public void UpdateNoise()
		{
			noise = new SimplexNoise(seed, gain, lacunarity, frequency, amplitude);
		}

		public void Save(ConfigNode node)
		{
			node.name = _name;
			node.AddValue("seed", seed);
			node.AddValue("gain", gain);
			node.AddValue("lacunarity", lacunarity);
			node.AddValue("frequency", frequency);
			node.AddValue("amplitude", amplitude);
		}

		public void Update()
		{
			if (FlightGlobals.currentMainBody.name != _name)
				return;
		}

		public double GetDensity(double lat, double lon)
		{
			return Math.Abs(noise.Generate(
				body.Radius * Math.Cos(lon) * Math.Cos(lat),
				body.Radius * Math.Sin(lon) * Math.Cos(lat),
				body.Radius * Math.Sin(lat),
				8));
		}

		public double GetTerrainHeight(double lat, double lon)
		{
			return body.pqsController.GetSurfaceHeight(body.GetRelSurfaceNVector(lat * 180 / Math.PI, lon * 180 / Math.PI)) - body.Radius;
		}

		public string GetBiomeName(double lat, double lon)
		{
			if (body.BiomeMap == null)
				return "";

			return body.BiomeMap.GetAtt(lat, lon).name;
		}

		/*public Texture2D GetMap(double resolution)
		{
			double minValue = double.MaxValue;
			double maxValue = double.MinValue;

			double[,] values = new double[(int)(360 * resolution), (int)(180 * resolution)];

			for (int lon = 0; lon < 360 * resolution; lon++)
			{
				for (int lat = 0; lat < 180 * resolution; lat++)
				{
					double flon = (lon / resolution - 180) * Math.PI / 180.0;
					double flat = (90 - lat / resolution) * Math.PI / 180.0;
					if (GetTerrainHeight(flon, flat) < 0)
						values[lon, lat] = 0;
					else
						values[lon, lat] = Math.Max(0, GetDensity(flon, flat));

					minValue = Math.Min(minValue, values[lon, lat]);
					maxValue = Math.Max(maxValue, values[lon, lat]);
				}
			}

			return Utils.TextureFromArrayDensity(values, minValue, maxValue);
		}

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

		public void ExportMaps(int width, int height)
		{
			string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			using (BinaryWriter writer = new BinaryWriter(File.Open(dir + "/" + _name + ".dat", FileMode.Create)))
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
	}
}
