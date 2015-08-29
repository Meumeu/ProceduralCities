using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
	public class ProceduralCities : ScenarioModule
	{
		Dictionary<string, KSPPlanet> InhabitedBodies = new Dictionary<string, KSPPlanet>();
		
		//[KSPField(isPersistant = true)]
		public bool initialized = false;

		static ApplicationLauncherButton toolbarButton;
		static Texture2D inactiveTexture;
		static Texture2D activeTexture;

		static bool windowVisible;
		static Rect windowPosition;
		static int windowId = GUIUtility.GetControlID(FocusType.Native);

		public ProceduralCities()
		{
			GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
			AddButton();
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
				showWindow, hideWindow,
				null, null,
				null, null,
				ApplicationLauncher.AppScenes.ALWAYS,
				windowVisible ? activeTexture : inactiveTexture);
		}

		static void showWindow()
		{
			windowVisible = true;
		}

		static void hideWindow()
		{
			windowVisible = false;
		}

		public void OnGUI()
		{
			if (!windowVisible)
				return;
			windowPosition.width = Math.Max(windowPosition.width, 300);
			windowPosition = GUILayout.Window(windowId, windowPosition,
				DrawWindow, "ProceduralCities");
		}

		Texture2D lastComputedMap;
		Texture2D heightMap;
		Utils.EditableDouble latitude = new Utils.EditableDouble("Latitude");
		Utils.EditableDouble longitude = new Utils.EditableDouble("Longitude");

		void DrawWindow(int windowId)
		{
			GUILayout.Label("Current planet: " + FlightGlobals.currentMainBody.name);

			if (InhabitedBodies.ContainsKey(FlightGlobals.currentMainBody.name))
			{
				KSPPlanet p = InhabitedBodies[FlightGlobals.currentMainBody.name];
				p.seed.Draw();
				p.gain.Draw();
				p.lacunarity.Draw();
				p.frequency.Draw();
				p.amplitude.Draw();

				if (lastComputedMap)
				{
					GUILayout.Label("Population density");
					GUILayout.Box(lastComputedMap, GUIStyle.none, new GUILayoutOption[] {
						GUILayout.Width(lastComputedMap.width),
						GUILayout.Height(lastComputedMap.height)
					});
				}

				if (heightMap)
				{
					GUILayout.Label("Height map");
					GUILayout.Box(heightMap, GUIStyle.none, new GUILayoutOption[] {
						GUILayout.Width(heightMap.width),
						GUILayout.Height(heightMap.height)
					});
				}

				if (GUILayout.Button("Regenerate maps"))
				{
					/*p.UpdateNoise();
					lastComputedMap = p.GetMap(2.0);
					if (!heightMap)
						heightMap = p.GetHeightMap(2.0);*/
				}

				if (GUILayout.Button("Export maps"))
				{
					p.ExportMaps(2048, 1024);
				}

				latitude.Draw();
				longitude.Draw();
				if (GUILayout.Button("Here"))
				{
					latitude.Set(FlightGlobals.ActiveVessel.latitude);
					longitude.Set(FlightGlobals.ActiveVessel.longitude);
				}
				GUILayout.Label("Biome: " + FlightGlobals.currentMainBody.BiomeMap.GetAtt(latitude * Math.PI / 180, longitude * Math.PI / 180).name);
				GUILayout.Label("Altitude: " + FlightGlobals.currentMainBody.pqsController.GetSurfaceHeight(FlightGlobals.currentMainBody.GetRelSurfaceNVector(latitude, longitude)));
			}
			else
			{
				GUILayout.Label(FlightGlobals.currentMainBody.name + " is uninhabited");
			}

			GUI.DragWindow();
		}

		public void Update()
		{
		}

		public override void OnAwake()
		{
			base.OnAwake();
			Debug.Log("[ProceduralCities] OnAwake");
		}

		void Initialize()
		{
			Debug.Log("[ProceduralCities] Initializing planets");

			var kerbinConfig = new ConfigNode("Kerbin");
			kerbinConfig.AddValue("seed", 0);
			kerbinConfig.AddValue("gain", 0.5);
			kerbinConfig.AddValue("lacunarity", 2);
			kerbinConfig.AddValue("frequency", 2.0e-7);
			kerbinConfig.AddValue("amplitude", 1);

			var kerbin = new KSPPlanet();
			kerbin.Load(kerbinConfig);
			InhabitedBodies.Add("Kerbin", kerbin);

			initialized = true;
			Debug.Log("[ProceduralCities] Done");
		}

		public override void OnLoad(ConfigNode node)
		{
			if (!initialized)
			{
				Initialize();
				return;
			}

			Debug.Log("[ProceduralCities] Loading planets");
			InhabitedBodies.Clear();
			foreach(ConfigNode n in node.GetNodes("InhabitedBody"))
			{
				try
				{
					Debug.Log("[ProceduralCities] Loading " + n.name);
					var planet = new KSPPlanet();
					planet.Load(n);

					InhabitedBodies.Add(n.name, planet);
					Debug.Log("[ProceduralCities] Loaded " + n.name);
				}
				catch(Exception e)
				{
					Debug.LogException(e);
				}
			}
			Debug.Log("[ProceduralCities] Done");
		}

		public override void OnSave(ConfigNode node)
		{
			foreach (var i in InhabitedBodies)
			{
				var n = new ConfigNode(i.Key);
				i.Value.Save(n);

				node.AddNode(n);
			}
		}
	}
}
