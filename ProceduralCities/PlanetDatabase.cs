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
	public class PlanetDatabase
	{
		static PlanetDatabase _instance;
		public int Seed;
		GameScenes CurrentScene;

		Dictionary<string, KSPPlanet> InhabitedBodies = new Dictionary<string, KSPPlanet>();
		List<WorldObject> WorldObjects = new List<WorldObject>();

		Thread Worker;
		Queue<Action> MainThreadQueue = new Queue<Action>();
		Queue<Request> WorkerQueue = new Queue<Request>();

		CelestialBody lastPlanet;
		CelestialBody lastCameraPlanet;
		Coordinates lastCoordinates;

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

		public readonly string CacheDirectory;

		public PlanetDatabase()
		{
			if (_instance != null)
				throw new InvalidOperationException("PlanetDatabase singleton already exists");

			_instance = this;
			CacheDirectory = Path.GetFullPath(KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/ProceduralCities");
			if (!Directory.Exists(CacheDirectory))
				Directory.CreateDirectory(CacheDirectory);

			GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
			StartWorker();
		}

		private void onLevelWasLoaded(GameScenes scene)
		{
			CurrentScene = scene;

			if (scene == GameScenes.MAINMENU)
			{
				GameEvents.onLevelWasLoaded.Remove(onLevelWasLoaded);

				StopWorker();
				foreach (var i in InhabitedBodies)
				{
					i.Value.Destroy();
				}

				foreach (var i in WorldObjects)
				{
					i.Destroy();
				}

				_instance = null;
			}
		}

		public static void AddWorldObject(WorldObject wo)
		{
			Instance.WorldObjects.Add(wo);
		}

		// InhabitedBodies must be locked
		static void AddPlanet(KSPPlanet planet)
		{
			Instance.InhabitedBodies.Add(planet.Name, planet);
			QueueToWorker(new RequestPlanetAdded(planet.Name));
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

		static KSPPlanet LoadFromCache(CelestialBody body)
		{
			string filename = PlanetDatabase.Instance.CacheDirectory + "/" + body.name + ".cache";

			try
			{
				using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					var br = new BinaryReader(stream);
					if (br.ReadString() == typeof(PlanetDatabase).Assembly.GetName().Version.ToString())
					{
						IFormatter formatter = new BinaryFormatter();
						var planet = (KSPPlanet)formatter.Deserialize(stream);
						planet.Init(body);
						return planet;
					}
					else
					{
						Debug.Log("[ProceduralCities] Cannot load " + body.name + " from cache: wrong version");
					}
				}
			}
			catch(Exception e)
			{
				Debug.Log("[ProceduralCities] Cannot load " + body.name + " from cache");
				Debug.LogException(e);
			}

			return null;
		}

		public void Load(ConfigNode node)
		{
			if (node.GetNodes("INHABITED_BODY").Count() == 0)
			{
				var rand = new System.Random(Instance.Seed);

				Debug.Log("[ProceduralCities] Initializing planets");

				var kerbinConfig = new ConfigNode("InhabitedBody");
				kerbinConfig.name = "Kerbin";
				kerbinConfig.AddValue("seed", rand.Next());

				var kerbin = new KSPPlanet(FlightGlobals.Bodies.Where(x => x.name == "Kerbin").First());
				kerbin.Load(kerbinConfig);

				lock (Instance.InhabitedBodies)
				{
					Instance.InhabitedBodies.Clear();
					AddPlanet(kerbin);
				}

				Debug.Log("[ProceduralCities] Done");
			}
			else
			{
				Debug.Log("[ProceduralCities] Loading planets");

				lock (Instance.InhabitedBodies)
				{
					foreach (var i in Instance.InhabitedBodies)
					{
						i.Value.Destroy();
					}

					Instance.InhabitedBodies.Clear();
					foreach (ConfigNode n in node.GetNodes("INHABITED_BODY"))
					{
						var body = FlightGlobals.Bodies.Where(x => x.name == n.GetValue("name")).First();
						KSPPlanet planet = LoadFromCache(body);

						if (planet == null)
						{
							planet = new KSPPlanet(body);
							planet.Load(n);
						}

						AddPlanet(planet);
					}
				}

				Debug.Log("[ProceduralCities] Done");
			}
		}

		public void Save(ConfigNode node)
		{
			Debug.Log("[ProceduralCities] Saving planets");
			lock (Instance.InhabitedBodies)
			{
				foreach (var i in Instance.InhabitedBodies)
				{
					var n = new ConfigNode("INHABITED_BODY");
					i.Value.Save(n);

					node.AddNode(n);
				}
			}
			Debug.Log("[ProceduralCities] Done");
		}

		#region Multithread stuff
		abstract class Request
		{
		}

		class RequestQuit : Request
		{
		}

		class RequestPositionChanged : Request
		{
			public readonly string Planet;
			public readonly Coordinates Position;
			public RequestPositionChanged(string Planet, Coordinates Position)
			{
				this.Planet = Planet;
				this.Position = Position;
			}
		}

		class RequestCameraChanged : Request
		{
			public readonly string Planet;
			public RequestCameraChanged(string Planet)
			{
				this.Planet = Planet;
			}
		}

		class RequestPlanetAdded : Request
		{
			public readonly string Name;
			public RequestPlanetAdded(string Name)
			{
				this.Name = Name;
			}
		}

		static public void Log(string message)
		{
			QueueToMainThread(() =>
			{
				Debug.Log("[ProceduralCities] " + message.Replace("\n", "\n[ProceduralCities] "));
			});
		}

		static public void LogException(Exception e)
		{
			QueueToMainThread(() =>
			{
				Debug.LogException(e);
			});
		}

		void DoWork_PlanetAdded(RequestPlanetAdded req)
		{
			KSPPlanet planet;
			lock (InhabitedBodies)
			{
				if (!InhabitedBodies.ContainsKey(req.Name))
					return;

				planet = InhabitedBodies[req.Name];
			}

			planet.Build();
		}

		void DoWork_PositionChanged(RequestPositionChanged req)
		{
			KSPPlanet planet = null;
			lock (InhabitedBodies)
			{
				if (!InhabitedBodies.ContainsKey(req.Planet))
					return;

				planet = InhabitedBodies[req.Planet];
			}

			planet?.UpdatePosition(req.Position);
		}

		void DoWork()
		{
			Log("Worker thread started");
			while(true)
			{
				Request req;
				Monitor.Enter(WorkerQueue);
				try
				{
					while(WorkerQueue.Count == 0)
						Monitor.Wait(WorkerQueue);
					req = WorkerQueue.Dequeue();
				}
				finally
				{
					Monitor.Exit(WorkerQueue);
				}

				try
				{
					if (req is RequestQuit)
					{
						Log("Worker thread stopped");
						return;
					}
					else if (req is RequestPlanetAdded)
					{
						DoWork_PlanetAdded(req as RequestPlanetAdded);
					}
					else if (req is RequestPositionChanged)
					{
						DoWork_PositionChanged(req as RequestPositionChanged);
					}
					else if (req is RequestCameraChanged)
					{
						// TODO: stuff ?
					}
					else
					{
						Log("Unknown request");
					}
				}
				catch(Exception e)
				{
					Log("Exception during processing of request of type " + req.GetType());
					LogException(e);
				}
			}
		}

		static void QueueToWorker(Request req)
		{
			Monitor.Enter(Instance.WorkerQueue);
			try
			{
				Instance.WorkerQueue.Enqueue(req);
				Monitor.Pulse(Instance.WorkerQueue);
			}
			finally
			{
				Monitor.Exit(Instance.WorkerQueue);
			}
		}

		static void DequeueFromWorker(int timeout = int.MaxValue)
		{
			lock (Instance.MainThreadQueue)
			{
				//if (Instance.MainThreadQueue.Count > 0)
				//	Debug.Log(string.Format("[ProceduralCities] {0} actions remaining", Instance.MainThreadQueue.Count));
				var watch = System.Diagnostics.Stopwatch.StartNew();

				while (Instance.MainThreadQueue.Count > 0 && watch.ElapsedMilliseconds < timeout)
				{
					Action act = Instance.MainThreadQueue.Dequeue();

					try
					{
						act();
					}
					catch(Exception e)
					{
						Debug.LogException(e);
					}
				}

				//if (Instance.MainThreadQueue.Count > 0)
				//	Debug.Log(string.Format("[ProceduralCities] {0} actions remaining after {1} ms", Instance.MainThreadQueue.Count, watch.ElapsedMilliseconds));
			}
		}

		public static void QueueToMainThread(Action act)
		{
			lock (Instance.MainThreadQueue)
			{
				Instance.MainThreadQueue.Enqueue(act);
			}
		}

		void StartWorker()
		{
			Debug.Log("[ProceduralCities] Starting worker thread");
			System.Diagnostics.Debug.Assert(Worker == null);
			Worker = new Thread(DoWork);
			Worker.Start();
		}

		void StopWorker()
		{
			Debug.Log("[ProceduralCities] Stopping worker thread");
			System.Diagnostics.Debug.Assert(Worker != null);


			Monitor.Enter(WorkerQueue);
			try
			{
				Instance.WorkerQueue.Clear();
				Instance.WorkerQueue.Enqueue(new RequestQuit());
				Monitor.Pulse(WorkerQueue);
			}
			finally
			{
				Monitor.Exit(WorkerQueue);
			}

			// Get the last log messages
			DequeueFromWorker();

			if (Worker.Join(5000))
			{
				Debug.Log("[ProceduralCities] Worker thread stopped");
			}
			else
			{
				Debug.Log("[ProceduralCities] Worker thread not stopped, aborting");
				Worker.Abort();
			}

			Worker = null;
		}

		CelestialBody GetCameraPlanet()
		{
			if (HighLogic.LoadedSceneHasPlanetarium && MapView.MapIsEnabled)
			{
				var target = MapView.MapCamera.target;
				if (target.type == MapObject.MapObjectType.CELESTIALBODY)
					return target.celestialBody;
				else if (target.type == MapObject.MapObjectType.MANEUVERNODE)
					return target.maneuverNode.patch.referenceBody;
				else if (target.type == MapObject.MapObjectType.VESSEL)
					return target.vessel.mainBody;
				else
					return null;
			}

			return null;
		}

		public void Update()
		{
			CelestialBody current_planet = (CurrentScene == GameScenes.SPACECENTER || CurrentScene == GameScenes.FLIGHT) ? FlightGlobals.currentMainBody : null;
			CelestialBody camera_planet = GetCameraPlanet();
			Coordinates coord;

			if (FlightGlobals.ActiveVessel == null)
			{
				coord = new Coordinates(-0.001788962483527778, -1.301584137981534); // KSC
			}
			else
			{
				coord = new Coordinates(FlightGlobals.ActiveVessel.latitude * Math.PI / 180, FlightGlobals.ActiveVessel.longitude * Math.PI / 180);
			}

			// FIXME: only do this close to the ground?
			if (current_planet != null && (current_planet != lastPlanet || Coordinates.Distance(coord, lastCoordinates) * current_planet.Radius > 1000))
			{
				Debug.Log(string.Format("[ProceduralCities] Moved by {0:F3} km", Coordinates.Distance(coord, lastCoordinates) * current_planet.Radius / 1000));
				Debug.Log(string.Format("[ProceduralCities] previous: {0}, now: {1}", lastCoordinates, coord));
				lastPlanet = current_planet;
				lastCoordinates = coord;
				QueueToWorker(new RequestPositionChanged(current_planet.name, coord));

				lock (WorldObjects)
				{
					for (int i = 0; i < WorldObjects.Count;)
					{
						if (WorldObjects[i].Planet != current_planet.name || Coordinates.Distance(WorldObjects[i].Position, coord) * current_planet.Radius > WorldObjects[i].UnloadDistance)
						{
							WorldObject tmp = WorldObjects[i];
							WorldObjects[i] = WorldObjects[WorldObjects.Count - 1];
							WorldObjects.RemoveAt(WorldObjects.Count - 1);
							tmp.Destroy();
						}
						else
						{
							i++;
						}
					}
				}
			}

			if (camera_planet != null && camera_planet != lastCameraPlanet)
			{
				QueueToWorker(new RequestCameraChanged(camera_planet.name));
				lastCameraPlanet = camera_planet;
			}

			DequeueFromWorker(20);
		}

		#endregion
	}
}
	