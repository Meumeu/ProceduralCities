using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;
using UnityEngine;

namespace ProceduralCities
{
	public class KSPTerrain : PlanetData
	{
		public readonly CelestialBody Body;
		Planet.Biome[] biomeList;

		public KSPTerrain(CelestialBody body)
		{
			Body = body;

			// FIXME: don't hardcode values
			biomeList = Body.BiomeMap.Attributes.Select(x => new Planet.Biome(
				x.name,
				x.name == "Ice Caps" ? 0.0 : x.name == "Water" ? 0.0 : x.name == "Deserts" ? 0.1 : 1.0
			)).ToArray();
		}

		public List<Pair<double, int>> GetTerrainAndBiome(List<Coordinates> coords)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);
			List<Pair<double, int>> ret = new List<Pair<double, int>>(coords.Count);

			for(int i = 0; i < coords.Count; i += 1000)
			{
				int copy = i;
				ThreadDispatcher.QueueToMainThread(() =>
				{
					for(int j = copy; j < copy + 1000 && j < coords.Count; j++)
					{
						Coordinates c = coords[j];
						double alt = Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(c.Latitude * 180 / Math.PI, c.Longitude * 180 / Math.PI)) - Body.Radius;
						int biome = -1;

						if (Body.BiomeMap)
						{
							var attr = Body.BiomeMap.GetAtt(c.Latitude, c.Longitude);
							for (int k = 0, n = Body.BiomeMap.Attributes.Length; k < n; k++)
							{
								if (attr == Body.BiomeMap.Attributes[k])
								{
									biome = k;
									break;
								}
							}
						}

						ret.Add(new Pair<double, int>(alt, biome));
					}
				});
			}

			ThreadDispatcher.QueueToMainThreadSync(() => {});

			return ret;
		}

		public List<double> GetTerrain(List<Coordinates> coords)
		{
			return GetTerrainAndBiome(coords).Select(x => x.item1).ToList();
		}

		public Planet.Biome[] GetBiomeList()
		{
			return biomeList;
		}

		public double Radius
		{
			get { return Body.Radius; }
		}
	}

	[Serializable]
	public class KSPPlanet : Planet
	{
		[NonSerialized]
		public CelestialBody Body;
		readonly string Name;

		[NonSerialized]
		public GameObject roadOverlay;

		[NonSerialized]
		public ContentCatalog Catalog;

		public KSPPlanet(CelestialBody body, int seed) : base(new KSPTerrain(body), seed)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);

			ThreadDispatcher.Log("Constructing KSPPlanet");

			Body = body;
			Name = body.name;
			ThreadDispatcher.QueueToMainThread(() => Init());
		}

		[OnDeserialized]
		void Deserialized(StreamingContext context)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);
			Body = FlightGlobals.Bodies.Where(x => x.name == Name).First();
			ThreadDispatcher.QueueToMainThread(() => Init());
			ThreadDispatcher.Log("Deserialized {0}", Name);
		}

		void Init()
		{
			Utils.Log("Init {0}", Body.name);
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);

			roadOverlay = new GameObject();
			var ro = roadOverlay.AddComponent<RoadOverlay>();
			ro.Body = Body;

			Catalog = new ContentCatalog(Body);

			ThreadDispatcher.QueueToWorker(() => GrosCube.MakePleinDeGrosCubes(Body));

			ThreadDispatcher.QueueToWorker(() => {
				Log("Building road map overlay and roads");

				Color[] palette = new[] { Color.blue, Color.green, Color.red, Color.cyan, Color.magenta, Color.yellow };

				int n = 0;
				var tmp = System.Diagnostics.Stopwatch.StartNew();
				foreach (var i in Roads)
				{
					Color32 c = palette[n % palette.Length];
					n++;

					var road = new Bezier(i.Select(x => Vertices[x].coord).ToList(), Body.Radius);
					ro.Add(road.Rasterize(1000), new Color32(c.r, c.g, c.b, 128));

					RoadSegment.MakeSegments(Body, road);
				}

				ThreadDispatcher.QueueToMainThread(() => ro.UpdateMesh());

				ThreadDispatcher.QueueToMainThreadSync(() => {});
				Log(string.Format("{0} road segments created in {1:F0} ms", PlanetDatabase.GetPlanet(Body.name).Catalog.Count, tmp.ElapsedMilliseconds));
				Log("Road overlay created");
			});
		}

		public void Destroy()
		{
			GameObject.Destroy(roadOverlay);
		}

		public void Save(ConfigNode node)
		{
			node.AddValue("name", Body.name);
			node.AddValue("seed", Seed);
		}

		public void UpdatePosition(Coordinates coord)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);

//			Vector3d up = new Vector3d(coord.x, coord.y, coord.z);
//			Vector3d north = Vector3d.Exclude(up, new Vector3d(0, 1, 0)).normalized;
//			Vector3d east = Vector3d.Cross(north, up).normalized;
		}

		#region Interface to Planet
		protected override void Log(string message)
		{
			if (ThreadDispatcher.IsMainThread)
				Utils.Log(message);
			else
				ThreadDispatcher.Log(message);
		}
		#endregion
	}
}
