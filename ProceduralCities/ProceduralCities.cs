using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
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

		public override void OnAwake()
		{
			base.OnAwake();
			Debug.Log("[ProceduralCities] OnAwake");
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
				showWindow, hideWindow,
				null, null,
				null, null,
				ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.TRACKSTATION,
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

//		Texture2D lastComputedMap;

		void DrawWindow(int windowId)
		{
			string planet = FlightGlobals.currentMainBody.name;
			GUILayout.Label("Current planet: " + planet);

			KSPPlanet p = null;
			lock (InhabitedBodies)
			{
				if (InhabitedBodies.ContainsKey(planet))
				{
					p = InhabitedBodies[planet];
				}
			}
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
					p.ExportData(2048, 1024);
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

		public override void OnLoad(ConfigNode node)
		{
			if (!initialized)
			{
				PlanetDatabase.Instance.Initialize();
				initialized = true;
			}
			else
			{
				PlanetDatabase.Instance.Load(node);
			}
		}

		public override void OnSave(ConfigNode node)
		{
			PlanetDatabase.Instance.Save(node);
		}
	}
}
