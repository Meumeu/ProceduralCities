﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProceduralCities
{
	public class PlanetDatabase
	{

		static PlanetDatabase _instance;
		public int Seed;
		GameScenes CurrentScene;

		internal Dictionary<string, KSPPlanet> InhabitedBodies = new Dictionary<string, KSPPlanet>();
		//internal List<WorldObject> WorldObjects = new List<WorldObject>();

		Thread Worker;
		Queue<Action> MainThreadQueue = new Queue<Action>();
		Queue<Action> WorkerQueue = new Queue<Action>();

		CelestialBody lastPlanet;
		CelestialBody lastCameraPlanet;
		Coordinates lastCoordinates;
		bool PhysicsReady;
		bool WorkerQuitRequested;

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

		Thread MainThread;
		public bool IsMainThread
		{
			get
			{
				return Thread.CurrentThread.Equals(MainThread);
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

			MainThread = Thread.CurrentThread;

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

				_instance = null;
			}
		}

		public void AddWorldObject(WorldObject wo)
		{
			System.Diagnostics.Debug.Assert(IsMainThread);
			Instance.InhabitedBodies[wo.Body.name].worldObjects.Add(wo);
		}

		// InhabitedBodies must be locked
		static void AddPlanet(KSPPlanet planet)
		{
			System.Diagnostics.Debug.Assert(Instance.IsMainThread);
			Instance.InhabitedBodies.Add(planet.Name, planet);
			QueueToWorker(() =>  Instance.DoWork_PlanetAdded(new RequestPlanetAdded(planet.Name)));
		}

		public static KSPPlanet GetPlanet(string name)
		{
			System.Diagnostics.Debug.Assert(Instance.IsMainThread);
			KSPPlanet ret = null;
			lock (Instance.InhabitedBodies)
			{
				Instance.InhabitedBodies.TryGetValue(name, out ret);
			}
			return ret;
		}

		static KSPPlanet LoadFromCache(CelestialBody body)
		{
			System.Diagnostics.Debug.Assert(Instance.IsMainThread);
			string filename = PlanetDatabase.Instance.CacheDirectory + "/" + body.name + ".cache";

			try
			{
				using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					return new KSPPlanet(body, stream);
				}
			}
			catch(Exception e)
			{
				Debug.Log(string.Format("[ProceduralCities] Cannot load {0} from cache: {1}", body.name, e.Message));
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
			}

			QueueToWorker(() => QueueToMainThread(() => PhysicsReady = true));
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
			System.Diagnostics.Debug.Assert(!Instance.IsMainThread);
			QueueToMainThread(() =>
			{
				Debug.Log("[ProceduralCities] " + message.Replace("\n", "\n[ProceduralCities] "));
			});
		}

		static public void LogException(Exception e)
		{
			System.Diagnostics.Debug.Assert(!Instance.IsMainThread);
			QueueToMainThread(() =>
			{
				Debug.LogException(e);
			});
		}

		void DoWork_PlanetAdded(RequestPlanetAdded req)
		{
			System.Diagnostics.Debug.Assert(!IsMainThread);
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
			System.Diagnostics.Debug.Assert(!IsMainThread);
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
			while(!WorkerQuitRequested)
			{
				Action act;
				Monitor.Enter(WorkerQueue);
				try
				{
					while(WorkerQueue.Count == 0)
						Monitor.Wait(WorkerQueue);
					act = WorkerQueue.Dequeue();
				}
				finally
				{
					Monitor.Exit(WorkerQueue);
				}

				try
				{
					act();
				}
				catch(Exception e)
				{
					Log("Exception during processing of request");
					LogException(e);
				}
			}
		}

		static void QueueToWorker(Action act)
		{
			System.Diagnostics.Debug.Assert(Instance.IsMainThread);
			Monitor.Enter(Instance.WorkerQueue);
			try
			{
				Instance.WorkerQueue.Enqueue(act);
				Monitor.Pulse(Instance.WorkerQueue);
			}
			finally
			{
				Monitor.Exit(Instance.WorkerQueue);
			}
		}

		static void ClearAndQueueToWorker(Action act)
		{
			System.Diagnostics.Debug.Assert(Instance.IsMainThread);
			Monitor.Enter(Instance.WorkerQueue);
			try
			{
				Instance.WorkerQueue.Clear();
				Instance.WorkerQueue.Enqueue(act);
				Monitor.Pulse(Instance.WorkerQueue);
			}
			finally
			{
				Monitor.Exit(Instance.WorkerQueue);
			}
		}

		static void DequeueFromWorker(int timeout = int.MaxValue, bool wait = false)
		{
			System.Diagnostics.Debug.Assert(Instance.IsMainThread);

			Monitor.Enter(Instance.MainThreadQueue);
			try
			{
				if (wait)
				{
					while(Instance.MainThreadQueue.Count == 0)
						Monitor.Wait(Instance.MainThreadQueue);

					Action act = Instance.MainThreadQueue.Dequeue();
					act();
				}
				else if (Instance.MainThreadQueue.Count > 0)
				{
					Action act = Instance.MainThreadQueue.Dequeue();
					act();
				}
			}
			catch(Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				Monitor.Exit(Instance.MainThreadQueue);
			}
		}

		public static void QueueToMainThread(Action act)
		{
			System.Diagnostics.Debug.Assert(!Instance.IsMainThread);

			Monitor.Enter(Instance.MainThreadQueue);
			try
			{
				Instance.MainThreadQueue.Enqueue(act);
				Monitor.Pulse(Instance.MainThreadQueue);
			}
			finally
			{
				Monitor.Exit(Instance.MainThreadQueue);
			}
		}

		public static void QueueToMainThreadSync(Action act)
		{
			System.Diagnostics.Debug.Assert(!Instance.IsMainThread);
			object monitor = new object();
			bool finished = false;
			bool error = false;

			QueueToMainThread(() =>
			{
				try
				{
					act();
				}
				catch(Exception)
				{
					Monitor.Enter(monitor);
					error = true;
					Monitor.Pulse(monitor);
					Monitor.Exit(monitor);
				}
				finally
				{
					Monitor.Enter(monitor);
					finished = true;
					Monitor.Pulse(monitor);
					Monitor.Exit(monitor);
				}
			});

			Monitor.Enter(monitor);
			try
			{
				while (!finished && !error)
					Monitor.Wait(monitor);
			}
			finally
			{
				Monitor.Exit(monitor);
			}

			if (error)
			{
				// TODO
			}
		}

		void StartWorker()
		{
			Debug.Log("[ProceduralCities] Starting worker thread");
			System.Diagnostics.Debug.Assert(Worker == null);
			Worker = new Thread(DoWork);
			WorkerQuitRequested = false;
			Worker.Start();
		}

		void StopWorker()
		{
			Debug.Log("[ProceduralCities] Stopping worker thread");
			System.Diagnostics.Debug.Assert(Worker != null);
			System.Diagnostics.Debug.Assert(IsMainThread);

			ClearAndQueueToWorker(() => WorkerQuitRequested = true);

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
			//CelestialBody camera_planet = GetCameraPlanet();
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
				lastPlanet = current_planet;
				lastCoordinates = coord;
				QueueToWorker(() => DoWork_PositionChanged(new RequestPositionChanged(current_planet.name, coord)));

				KSPPlanet planet;
				if (InhabitedBodies.TryGetValue(current_planet.name, out planet))
				{
					planet.worldObjects.UpdateVisibleObjects();
				}

				/*lock (WorldObjects)
				{
					int nbVisible = 0;
					for (int i = 0; i < WorldObjects.Count;)
					{
						double distance = Coordinates.Distance(WorldObjects[i].Position, coord) * current_planet.Radius;
						bool visible = distance < WorldObjects[i].VisibleDistance && WorldObjects[i].Planet == current_planet.name ;
						WorldObjects[i].visible = visible;
						if (visible)
							nbVisible++;

						if (WorldObjects[i].Planet != current_planet.name || distance > WorldObjects[i].UnloadDistance)
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
				}*/
			}

			/*if (camera_planet != null && camera_planet != lastCameraPlanet)
			{
				QueueToWorker(() => DoWork_PositionChanged(new RequestCameraChanged(camera_planet.name)));
				lastCameraPlanet = camera_planet;
			}*/

			DequeueFromWorker(20);
		}

		public void FixedUpdate()
		{
			while (!PhysicsReady && HighLogic.LoadedSceneIsFlight)
			{
				DequeueFromWorker(wait: true);
			}
		}

		#endregion
	}
}
