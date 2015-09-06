using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ProceduralCities
{
	[Serializable]
	public class KSPPlanet : Planet
	{
		internal string Name
		{
			get
			{
				return Body ? Body.name : "";
			}
		}

		[NonSerialized]
		CelestialBody Body;

		[NonSerialized]
		public GameObject roadOverlay;

		[NonSerialized]
		System.Diagnostics.Stopwatch watch;

		[OnDeserializing]
		private void OnDeserializing(StreamingContext context)
		{
			Debug.Log("[ProceduralCities] Deserializing KSPPlanet");
			watch = System.Diagnostics.Stopwatch.StartNew();
		}

		public KSPPlanet(CelestialBody body)
		{
			Debug.Log("[ProceduralCities] Constructing KSPPlanet");
			watch = System.Diagnostics.Stopwatch.StartNew();
			Init(body);
		}

		public void Init(CelestialBody body)
		{
			Body = body;
			roadOverlay = new GameObject();
			var ro = roadOverlay.AddComponent<RoadOverlay>();
			ro.Body = Body;
		}

		public void Destroy()
		{
			GameObject.Destroy(roadOverlay);
		}

		public void Load(ConfigNode node)
		{
			System.Diagnostics.Debug.Assert(node.GetValue("name") == Name);

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
			node.AddValue("name", Name);
			node.AddValue("seed", Seed);
		}

		/* GetTerrainHeight(double lat, double lon)
		{
			return Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(lat * 180 / Math.PI, lon * 180 / Math.PI)) - Body.Radius;
		}

		string GetBiomeName(double lat, double lon)
		{
			if (Body.BiomeMap == null)
				return "";

			return Body.BiomeMap.GetAtt(lat, lon).name;
		}

		int GetBiome(double lat, double lon)
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
		}*/

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

		/*public void ExportData(int width, int height)
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
		}*/

		protected override void BuildFinished(bool fromCache)
		{
			Log("Planet built, building road overlay");
			var ro = roadOverlay.GetComponent<RoadOverlay>();

			Color[] palette = new[] { Color.blue, Color.green, Color.red, Color.cyan, Color.magenta, Color.yellow };

			int n = 0;
			foreach (var i in Roads)
			{
				//Color32 c = Color.Lerp(palette[n % palette.Length], Color.gray, (float)Math.Floor((double)n / palette.Length) * 0.1f);
				Color32 c = palette[n % palette.Length];
				n++;

				ro.Add(i.Positions.Select(x => Vertices[x].coord), new Color32(c.r, c.g, c.b, 128));
			}

			PlanetDatabase.QueueToMainThread(() => ro.UpdateMesh());

			Log("Planet built in " + watch.ElapsedMilliseconds + " ms");

			if (!fromCache)
			{
				string filename = PlanetDatabase.Instance.CacheDirectory + "/" + Name + ".cache";
				Log("Saving planet to " + filename);
				using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
				{
					var bw = new BinaryWriter(stream);
					bw.Write(typeof(PlanetDatabase).Assembly.GetName().Version.ToString());

					IFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, this);
				}
			}
		}

		public void UpdatePosition(Coordinates coord)
		{
			PlanetDatabase.Log(string.Format("New position: {0}", coord));

			double radius = Body.Radius;
			double closestDist = double.MaxValue;
			Coordinates closest = new Coordinates(0, 0);

			foreach (Road i in Roads)
			{
				for (int j = 1, n = i.Positions.Count; j < n; j++)
				{
					Vertex v1 = Vertices[i.Positions[j - 1]];
					Vertex v2 = Vertices[i.Positions[j]];

					if (Coordinates.Distance(v1.coord, coord) < closestDist)
					{
						closestDist = Coordinates.Distance(v1.coord, coord);
						closest = v1.coord;
					}

					if (Coordinates.Distance(v1.coord, coord) * radius < 2000)
					{
						Log("Creating road at " + v1);
						RoadSegment r = new RoadSegment();

						r.Planet = Body.name;
						r.Position = v1.coord;
						r.UnloadDistance = 3000;

						// TODO: road stuff

						PlanetDatabase.QueueToMainThread(() => PlanetDatabase.AddWorldObject(r));
					}
				}
			}
			Log(string.Format("Closest road at {0} ({1:F1} km)", closest, closestDist * radius / 1000));
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
						double alt = Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(i.Latitude * 180 / Math.PI, i.Longitude * 180 / Math.PI)) - Body.Radius;
						int biome = -1;

						if (Body.BiomeMap)
						{
							var attr = Body.BiomeMap.GetAtt(i.Latitude, i.Longitude);
							for (int j = 0, n = Body.BiomeMap.Attributes.Length; j < n; j++)
							{
								if (attr == Body.BiomeMap.Attributes[j])
								{
									biome = j;
									break;
								}
							}
						}

						ret.Add(new Pair<double, int>(alt, biome));
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

			return ret;
		}

		protected override void Log(string message)
		{
			PlanetDatabase.Log(message);
		}
		#endregion
	}
}
