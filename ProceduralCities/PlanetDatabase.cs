using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProceduralCities
{
	public class PlanetDatabase : IConfigNode
	{
		static PlanetDatabase _instance;
		public static PlanetDatabase Instance
		{
			get
			{
				if (_instance == null)
					_instance = new PlanetDatabase();

				return _instance;
			}
		}

		private PlanetDatabase()
		{
			GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
			Start();
		}

		private void onLevelWasLoaded(GameScenes scene)
		{
			Debug.Log("[ProceduralCities] onLevelWasLoaded: " + scene);

		}

		Dictionary<string, KSPPlanet> InhabitedBodies = new Dictionary<string, KSPPlanet>();

		// InhabitedBodies must be locked
		void AddPlanet(KSPPlanet planet)
		{
			InhabitedBodies.Add(planet.Name, planet);
			QueueToWorker(new RequestPlanetAdded(planet.Name));
		}

		public void Initialize()
		{
			Debug.Log("[ProceduralCities] Initializing planets");

			var kerbinConfig = new ConfigNode("Kerbin");
			kerbinConfig.AddValue("seed", 0);

			var kerbin = new KSPPlanet();
			kerbin.Load(kerbinConfig);

			lock (InhabitedBodies)
			{
				InhabitedBodies.Clear();
				AddPlanet(kerbin);
			}

			Debug.Log("[ProceduralCities] Done");
		}

		public void Load(ConfigNode node)
		{
			Debug.Log("[ProceduralCities] Loading planets");

			lock (InhabitedBodies)
			{
				InhabitedBodies.Clear();
				foreach(ConfigNode n in node.GetNodes("InhabitedBody"))
				{
					try
					{
						Debug.Log("[ProceduralCities] Loading " + n.name);
						var planet = new KSPPlanet();
						planet.Load(n);
						lock(InhabitedBodies)
						{
							AddPlanet(planet);
						}
						Debug.Log("[ProceduralCities] Loaded " + n.name);
					}
					catch(Exception e)
					{
						Debug.LogException(e);
					}
				}
			}

			Debug.Log("[ProceduralCities] Done");
		}

		public void Save(ConfigNode node)
		{
			Debug.Log("[ProceduralCities] Saving planets");
			lock (InhabitedBodies)
			{
				foreach (var i in InhabitedBodies)
				{
					var n = new ConfigNode(i.Key);
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

		class RequestPlanetAdded : Request
		{
			public readonly string Name;
			public RequestPlanetAdded(string Name)
			{
				this.Name = Name;
			}
		}

		Thread Worker;
		Queue<Action> MainThreadQueue = new Queue<Action>();
		Queue<Request> WorkerQueue = new Queue<Request>();

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
		}

		void DoWork()
		{
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
						return;
					}
					else if (req is RequestPlanetAdded)
					{
						DoWork_PlanetAdded(req as RequestPlanetAdded);
						Log("Planet added");
					}
					else if (req is RequestPositionChanged)
					{
						DoWork_PositionChanged(req as RequestPositionChanged);
						Log("Position changed");
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

		public static void QueueToMainThread(Action act)
		{
			lock (Instance.MainThreadQueue)
			{
				Instance.MainThreadQueue.Enqueue(act);
			}
		}

		void Start()
		{
			Debug.Log("[ProceduralCities] Starting worker thread");
			System.Diagnostics.Debug.Assert(Worker == null);
			Worker = new Thread(DoWork);
			Worker.Start();
		}

		void Stop()
		{
			Debug.Log("[ProceduralCities] Stopping worker thread");
			System.Diagnostics.Debug.Assert(Worker != null);
			QueueToWorker(new RequestQuit());
			if (!Worker.Join(5000))
			{
				Debug.Log("[ProceduralCities] Worker thread not stopped, aborting");
				Worker.Abort();
			}

			Worker = null;
		}

		CelestialBody lastPlanet;
		Coordinates lastCoordinates;

		public void Update()
		{
			// FIXME: use the camera target
			CelestialBody planet = FlightGlobals.currentMainBody;
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
			if (planet != lastPlanet || Coordinates.Distance(coord, lastCoordinates) * planet.Radius > 1000)
			{
				lastPlanet = planet;
				lastCoordinates = coord;
				QueueToWorker(new RequestPositionChanged(planet.name, coord));
			}

			lock (MainThreadQueue)
			{
				var watch = System.Diagnostics.Stopwatch.StartNew();

				while (MainThreadQueue.Count > 0 && watch.ElapsedMilliseconds < 20)
				{
					Action act = MainThreadQueue.Dequeue();

					try
					{
						act();
					}
					catch(Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
		}
		#endregion
	}
}
	