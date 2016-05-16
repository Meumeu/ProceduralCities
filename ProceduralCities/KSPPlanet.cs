using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class KSPPlanet : Planet
	{
		public readonly CelestialBody Body;
		public int CurrentTile { get; private set; } = -1;

		GameObject tileOverlayObj;
		MapOverlayTiles tileOverlay;
		LRUCache<Color32> TileCache = new LRUCache<Color32>(idx => new Color32(), 200);

		public KSPPlanet(CelestialBody body, int seed) : base(new Terrain(body), seed)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);

			Body = body;

			ThreadDispatcher.QueueToMainThread(() =>
			{
				tileOverlayObj = new GameObject();
				tileOverlay = tileOverlayObj.AddComponent<MapOverlayTiles>();
				tileOverlay.Body = Body;
				tileOverlay.planet = this;

				if (CurrentTile >= 0)
				{
					TileCache[CurrentTile] = new Color32(0, 0, 255, 128);
					tileOverlay.UpdateTiles(TileCache);
				}
			});
		}

		public void Destroy()
		{
			Utils.Log("Destroying map overlay");
			GameObject.Destroy(tileOverlay);
			GameObject.Destroy(tileOverlayObj);
			tileOverlayObj = null;
			tileOverlay = null;
		}

		public void Load(ConfigNode node)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);
			Utils.Log("Load KSPPlanet {0}", Body.name);
		}

		public void Save(ConfigNode node)
		{
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);
			Utils.Log("Save KSPPlanet {0}", Body.name);
		}

		Coordinates LastUpdatedPosition;
		Coordinates NextPosition;
		void UpdatePositionWorker()
		{
			lock (this)
			{
				if (NextPosition == LastUpdatedPosition)
					return;

				LastUpdatedPosition = NextPosition;
			}

			foreach (var i in FindTiles(LastUpdatedPosition, 0.05))
			{
				if (data.ContainsKey(i))
					continue;

				Utils.Log("Sampling terrain at {0}", Tiles[i].Center);
				data[i].Load(2);
			}
		}

		public void UpdatePosition(Coordinates coord)
		{
			int NextTile = FindTile(coord, CurrentTile);
			//if (NextTile == CurrentTile)
			//	return;
			CurrentTile = NextTile;

			lock (this)
				NextPosition = coord;
			
			ThreadDispatcher.QueueToWorker(() => UpdatePositionWorker());


			TileCache.Clear();
			foreach (var i in data)
			{
				var tiledata = i.Value;
				if (tiledata.SampleAltitudes == null)
					continue;
				
				double minalt = tiledata.SampleAltitudes.Min();
				double maxalt = tiledata.SampleAltitudes.Max();

				if (i.Value.HasCity.GetValueOrDefault(false))
					TileCache[i.Key] = new Color32(255, 0, 255, 128);
				else if (minalt < 0)
					TileCache[i.Key] = new Color32(0, 0, 255, 128);
				else
					TileCache[i.Key] = Color32.Lerp(new Color32(0, 255, 0, 128), new Color32(64, 64, 64, 128), (float)(maxalt - minalt) / 500);

			}
			
			/*if (CurrentTile >= 0)
				TileCache[CurrentTile] = new Color32(128, 128, 128, 128);*/
			
			/*TileCache[NextTile] = new Color32(255, 0, 0, 128);*/

			if (tileOverlay != null)
				tileOverlay.UpdateTiles(TileCache);
		}

		protected override void Log(string message)
		{
			Utils.Log(message);
		}
	}
}
