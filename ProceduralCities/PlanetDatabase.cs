using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ProceduralCities
{
	public class PlanetDatabase
	{
		public int Seed;
		GameScenes CurrentScene;

		Dictionary<string, KSPPlanet> InhabitedBodies = new Dictionary<string, KSPPlanet>();

		CelestialBody lastPlanet;
		Coordinates lastCoordinates;
		bool PhysicsReady;


		public readonly string CacheDirectory;

		static PlanetDatabase _instance;
		public static PlanetDatabase Instance
		{
			get
			{
				if (_instance == null)
					throw new InvalidOperationException("PlanetDatabase singleton does not exist");

				return _instance;
			}
		}

		public static bool Loaded
		{
			get
			{
				return _instance != null;
			}
		}

		public CelestialBody CurrentPlanet
		{
			get
			{
				return (CurrentScene == GameScenes.SPACECENTER || CurrentScene == GameScenes.FLIGHT) ? FlightGlobals.currentMainBody : null;
			}
		}

		public PlanetDatabase()
		{
			if (_instance != null)
				throw new InvalidOperationException("PlanetDatabase singleton already exists");

			_instance = this;

			CacheDirectory = Path.GetFullPath(KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/ProceduralCities");
			if (!Directory.Exists(CacheDirectory))
				Directory.CreateDirectory(CacheDirectory);

			GameEvents.onLevelWasLoaded.Add(LevelWasLoaded);
			new ThreadDispatcher();
		}

		private void LevelWasLoaded(GameScenes scene)
		{
			CurrentScene = scene;

			if (scene == GameScenes.MAINMENU)
			{
				GameEvents.onLevelWasLoaded.Remove(LevelWasLoaded);

				ThreadDispatcher.Destroy();

				List<KSPPlanet> planets;
				lock (InhabitedBodies)
				{
					planets = InhabitedBodies.Select(x => x.Value).ToList();
				}

				foreach (var i in planets)
				{
					i.Destroy();
				}

				_instance = null;
			}
		}

		public void AddWorldObject(WorldObject wo)
		{
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);
			GetPlanet(wo.Body.name).Catalog.Add(wo);
		}

		void AddPlanet(KSPPlanet planet)
		{
			if (ThreadDispatcher.IsMainThread)
			{
				lock (InhabitedBodies)
				{
					InhabitedBodies.Add(planet.Body.name, planet);
				}
			}
			else
			{
				ThreadDispatcher.QueueToMainThread(() => AddPlanet(planet));
			}
		}

		public static KSPPlanet GetPlanet(string name)
		{
			KSPPlanet ret = null;
			lock (Instance.InhabitedBodies)
			{
				Instance.InhabitedBodies.TryGetValue(name, out ret);
			}
			return ret;
		}

		KSPPlanet LoadFromCache(string name)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);
			string filename = PlanetDatabase.Instance.CacheDirectory + "/" + name + ".planet";

			try
			{
				KSPPlanet planet;
				BinaryFormatter formatter = new BinaryFormatter();
				using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					planet = (KSPPlanet)formatter.Deserialize(stream);
				}
				Utils.Log(string.Format("Loaded {0} from cache", name));
				return planet;
			}
			catch(Exception e)
			{
				Utils.Log(string.Format("Cannot load {0} from cache: {1}", name, e.Message));
				return null;
			}
		}

		void SaveToCache(KSPPlanet planet)
		{
			string name = planet.Body.name;
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);
			string filename = PlanetDatabase.Instance.CacheDirectory + "/" + name + ".planet";

			try
			{
				using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, planet);
					Utils.Log(string.Format("Saved {0} to cache", name));
				}
			}
			catch(Exception e)
			{
				Utils.Log(string.Format("Cannot save {0} to cache: {1}", name, e.Message));
			}
		}

		public void Load(ConfigNode node)
		{
			if (node.GetNodes("INHABITED_BODY").Count() == 0)
			{
				lock (Instance.InhabitedBodies)
				{
					Instance.InhabitedBodies.Clear();
				}

				var rand = new System.Random(Instance.Seed);
				int seed = rand.Next();
				CelestialBody body = FlightGlobals.Bodies.Where(x => x.name == "Kerbin").First();

				Utils.Log("Initializing planets (first time)");

				var kerbinConfig = new ConfigNode("InhabitedBody");
				kerbinConfig.AddValue("name", "Kerbin");
				kerbinConfig.AddValue("seed", seed);

				ThreadDispatcher.QueueToWorker(() =>
				{
					var kerbin = new KSPPlanet(body, seed);
					SaveToCache(kerbin);
					AddPlanet(kerbin);
				});

				Utils.Log("Done");
			}
			else
			{
				lock (Instance.InhabitedBodies)
				{
					foreach (var i in Instance.InhabitedBodies)
					{
						i.Value.Destroy();
					}

					Instance.InhabitedBodies.Clear();
				}

				Utils.Log("Initializing planets");
				foreach (ConfigNode n in node.GetNodes("INHABITED_BODY"))
				{
					var body = FlightGlobals.Bodies.Where(x => x.name == n.GetValue("name")).First();
					string name = body.name;
					int seed = int.Parse(n.GetValue("seed"));
					ThreadDispatcher.QueueToWorker(() =>
					{
						KSPPlanet planet = LoadFromCache(name);

						if (planet == null)
						{
							planet = new KSPPlanet(body, seed);
							SaveToCache(planet);
						}
						
						AddPlanet(planet);
					});
				}
				Utils.Log("Done");
			}

			ThreadDispatcher.QueueToWorker(() => ThreadDispatcher.QueueToMainThread(() => PhysicsReady = true));
		}

		public void Save(ConfigNode node)
		{
			lock (Instance.InhabitedBodies)
			{
				foreach (var i in Instance.InhabitedBodies)
				{
					var n = new ConfigNode("INHABITED_BODY");
					i.Value.Save(n);

					node.AddNode(n);
				}
			}
		}

		public void Update()
		{
			CelestialBody current_planet = CurrentPlanet;
			Coordinates coord;

			if (FlightGlobals.ActiveVessel == null)
			{
				coord = Coordinates.KSC;
			}
			else
			{
				coord = new Coordinates(FlightGlobals.ActiveVessel.latitude * Math.PI / 180, FlightGlobals.ActiveVessel.longitude * Math.PI / 180);
			}

			if (current_planet != lastPlanet && current_planet != null)
			{
				Utils.Log("Last planet: {0}, current planet: {1}", lastPlanet == null ? "(null)" : lastPlanet.name, current_planet.name);
			}

			// FIXME: only do this close to the ground?
			if (current_planet != null && (current_planet != lastPlanet || Coordinates.Distance(coord, lastCoordinates) * current_planet.Radius > 1000))
			{
				Utils.Log("New position: {0}", coord);
				lastPlanet = current_planet;
				lastCoordinates = coord;
				string planetName = current_planet.name;
				KSPPlanet planet = GetPlanet(current_planet.name);
				if (planet != null)
				{
					ThreadDispatcher.QueueToWorker(() => planet.UpdatePosition(coord));

					planet.Catalog.UpdateVisibleObjects(coord);
				}
			}

			ThreadDispatcher.DequeueFromWorker(20);
		}

		public void FixedUpdate()
		{
			while (!PhysicsReady && HighLogic.LoadedSceneIsFlight)
			{
				ThreadDispatcher.DequeueFromWorker(wait: true);
			}
		}
	}
}
