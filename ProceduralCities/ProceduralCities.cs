using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using KSP.UI.Screens;


namespace ProceduralCities
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
	public class ProceduralCities : ScenarioModule
	{
		static ApplicationLauncherButton toolbarButton;
		static Texture2D inactiveTexture;
		static Texture2D activeTexture;

		static bool windowVisible;
		static Rect windowPosition;
		static int windowId = GUIUtility.GetControlID(FocusType.Native);

		Dictionary<CelestialBody, KSPPlanet> planets = new Dictionary<CelestialBody, KSPPlanet>();
		string CacheDirectory;
		bool PhysicsReady;

		public ProceduralCities()
		{
			GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
			AddButton();

			CacheDirectory = Path.GetFullPath(KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/ProceduralCities");
			if (!Directory.Exists(CacheDirectory))
				Directory.CreateDirectory(CacheDirectory);

			if (!ThreadDispatcher.Loaded)
			{
				new ThreadDispatcher();
			}
		}

		void AddButton()
		{
			if (ApplicationLauncher.Instance == null)
				return;
			
			if (toolbarButton != null)
				return;

			if (inactiveTexture == null)
				inactiveTexture = GameDatabase.Instance.GetTexture("ProceduralCities/icon38-inactive", false);
			if (activeTexture == null)
				activeTexture = GameDatabase.Instance.GetTexture("ProceduralCities/icon38-active", false);

			toolbarButton = ApplicationLauncher.Instance.AddModApplication(
				() => windowVisible = true, 
				() => windowVisible = false,
				null, null,
				null, null,
				ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.TRACKSTATION,
				windowVisible ? activeTexture : inactiveTexture);
		}

		public void OnGUI()
		{
			if (!windowVisible)
				return;
			windowPosition.width = Math.Max(windowPosition.width, 300);
			windowPosition = GUILayout.Window(windowId, windowPosition, DrawWindow, "ProceduralCities");
		}

		void DrawWindow(int windowId)
		{
			//string planet = CurrentPlanet.Body.name;
			//GUILayout.Label("Current planet: " + planet);

			//KSPPlanet p = PlanetDatabase.GetPlanet(planet);

			//if (p != null)
			//{
			//	if (GUILayout.Button("Export maps"))
			//	{
			//		string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
					//var exporter = new PlanetExporter(FlightGlobals.currentMainBody);
					//exporter.Export(dir + "/" + planet + ".dat", 2048, 1024);
			//	}
			//}
			////else
			//{
			//	GUILayout.Label(planet + " is uninhabited");
			//}

			GUI.DragWindow();
		}

		KSPPlanet LastPlanet;
		public void Update()
		{
			KSPPlanet CurrentPlanet;
			planets.TryGetValue(FlightGlobals.currentMainBody ?? FlightGlobals.Bodies.FirstOrDefault(x => x.name == "Kerbin"), out CurrentPlanet);

			if (CurrentPlanet != LastPlanet)
			{
				Utils.Log("Planet changed: {0} => {1}", LastPlanet == null ? "(null)" : LastPlanet.Body.name, CurrentPlanet == null ? "(null)" : CurrentPlanet.Body.name);
				LastPlanet = CurrentPlanet;
			}

			if (CurrentPlanet != null)
			{
				if (FlightGlobals.ActiveVessel == null)
					CurrentPlanet.UpdatePosition(Coordinates.KSC);
				else
					CurrentPlanet.UpdatePosition(new Coordinates(FlightGlobals.ActiveVessel.latitude * Math.PI / 180, FlightGlobals.ActiveVessel.longitude * Math.PI / 180));
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

		ConfigNode DefaultConfiguration()
		{
			ConfigNode node = new ConfigNode();
			var n = node.AddNode("InhabitedBody");
			n.AddValue("name", "Kerbin");
			n.AddValue("seed", 12345); // TODO: random

			return node;
		}

		KSPPlanet LoadPlanet(ConfigNode node, CelestialBody body, int seed)
		{
			// TODO: safe to use ConfigNode in a worker thread?

			KSPPlanet planet = null;
			/*
			string filename = CacheDirectory + "/" + name + ".planet";
			bool FromCache = true;

			try
			{
				BinaryFormatter formatter = new BinaryFormatter();
				using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					planet = (KSPPlanet)formatter.Deserialize(stream);
				}
			}
			catch(Exception e)
			{
				Utils.Log(string.Format("Cannot load {0} from cache: {1}", name, e.Message));
				FromCache = false;
			}*/

			planet = new KSPPlanet(body, seed);

			/*if (!FromCache)
			{
				try
				{
					using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
					{
						BinaryFormatter formatter = new BinaryFormatter();
						formatter.Serialize(stream, planet);
						Utils.Log(string.Format("Saved {0} to cache", name));
					}
				}
				catch (Exception e)
				{
					Utils.Log(string.Format("Cannot save {0} to cache: {1}", name, e.Message));
				}
			}*/

			planet.Load(node);
			return planet;
		}

		public override void OnLoad(ConfigNode node)
		{
			if (node.GetNodes("InhabitedBody").Count() == 0)
				node = DefaultConfiguration();

			foreach (var i in node.GetNodes("InhabitedBody"))
			{
				string name = i.GetValue("name");
				CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(x => x.name == name);
				int seed = int.Parse(i.GetValue("seed"));

				ThreadDispatcher.QueueToWorker(() =>
				{
					KSPPlanet planet = LoadPlanet(i, body, seed);

					lock(planets)
					{
						planets.Add(planet.Body, planet);
					}
				});
			}

			ThreadDispatcher.QueueToWorker(() => ThreadDispatcher.QueueToMainThread(() => PhysicsReady = true));
		}

		public override void OnSave(ConfigNode node)
		{
			foreach (var i in planets)
			{
				var n = node.AddNode("InhabitedBody");
				n.AddValue("name", i.Value.Body.name);
				n.AddValue("seed", i.Value.Seed);
				i.Value.Save(n);
			}
		}

		void OnDestroy()
		{
			foreach (var i in planets)
			{
				i.Value.Destroy();
			}
		}
	}
}
