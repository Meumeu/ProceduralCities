using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;


namespace ProceduralCities
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
	public class ProceduralCities : ScenarioModule
	{
		[KSPField(isPersistant = true)]
		public int Seed;

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

		public void OnDestroy()
		{
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

//		Texture2D lastComputedMap;

		void DrawWindow(int windowId)
		{
			string planet = FlightGlobals.currentMainBody == null ? "Kerbin" : FlightGlobals.currentMainBody.name;
			GUILayout.Label("Current planet: " + planet);

			KSPPlanet p = PlanetDatabase.GetPlanet(planet);

			if (p != null)
			{
//				if (lastComputedMap)
//				{
//					GUILayout.Label("Map");
//					GUILayout.Box(lastComputedMap, GUIStyle.none, new GUILayoutOption[] {
//						GUILayout.Width(lastComputedMap.width),
//						GUILayout.Height(lastComputedMap.height)
//					});
//				}

				if (GUILayout.Button("Export maps"))
				{
					string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
					var exporter = new PlanetExporter(FlightGlobals.currentMainBody);
					exporter.Export(dir + "/" + planet + ".dat", 2048, 1024);
				}
			}
			else
			{
				GUILayout.Label(planet + " is uninhabited");
			}

			GUI.DragWindow();
		}

		public void Update()
		{
			PlanetDatabase.Instance.Update();
		}

		public void FixedUpdate()
		{
			PlanetDatabase.Instance.FixedUpdate();
		}

		public override void OnLoad(ConfigNode node)
		{
			if (!PlanetDatabase.Loaded)
			{
				if (Seed == 0) // First run for this save
					Seed = 12345; // TODO: random value

				new PlanetDatabase();
				PlanetDatabase.Instance.Seed = Seed;
				PlanetDatabase.Instance.Load(node);
			}
		}

		public override void OnSave(ConfigNode node)
		{
			PlanetDatabase.Instance.Save(node);
		}
	}
}
