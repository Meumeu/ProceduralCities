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
		//LRUCache<Color32> TileCache = new LRUCache<Color32>(idx => new Color32(), 200);

		public KSPPlanet(CelestialBody body, int seed) : base(new Terrain(body), seed)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);

			Body = body;
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
		int LastTile;
		void UpdatePositionWorker()
		{
			lock (this)
			{
				if (NextPosition == LastUpdatedPosition)
					return;

				LastUpdatedPosition = NextPosition;
			}

			data.RemoveOldElements(x => Coordinates.Distance(LastUpdatedPosition, Tiles[x].Center));

			#if false
			foreach (var i in FindTiles(LastUpdatedPosition, 0.05))
			{
				data[i].Load(TileData.Computationlevel.MAX);
			}
			#else
			int tile = FindTile(LastUpdatedPosition);
			if (tile == LastTile)
				return;
			LastTile = tile;

			data[LastTile].Load(TileData.Computationlevel.MAX);
			#endif
		}

		public void UpdatePosition(Coordinates coord)
		{
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);

			int NextTile = FindTile(coord, CurrentTile);
			//if (NextTile == CurrentTile)
			//	return;
			CurrentTile = NextTile;

			lock (this)
				NextPosition = coord;
			
			ThreadDispatcher.QueueToWorker(() => UpdatePositionWorker());

			if (tileOverlayObj == null)
			{
				tileOverlayObj = new GameObject();
				tileOverlay = tileOverlayObj.AddComponent<MapOverlayTiles>();
				tileOverlay.Body = Body;
				tileOverlay.planet = this;
			}

			var tileColors = new Dictionary<int, Color32>();
			/*int city = -1;
			if (data.ContainsKey(CurrentTile))
				city = data[CurrentTile].ClosestCity;*/
		
			foreach (var i in data)
			{
				byte r, g, b;
				//if (i.Value.Color(out r, out g, out b, city))
				if (i.Value.Color(out r, out g, out b))
				{	
					tileColors[i.Key] = new Color32(r, g, b, 255);
				}
			}

			if (data.ContainsKey(CurrentTile) && IsNodeAllowed(CurrentTile))
			{
				var knownCities = data.Where(x => x.Value.HasCity.GetValueOrDefault()).Select(x => x.Key).ToArray();

				if (knownCities.Length > 0)
				{
					var pf = new Pathfinding(this, knownCities, CurrentTile);
					var path = pf.GetPath(CurrentTile).ToArray();
					int city = data[CurrentTile].ClosestCity;

					if (city >= 0)
					{
						foreach (var i in data)
						{
							if (IsNodeAllowed(i.Key) && data[i.Key].Level >= TileData.Computationlevel.CITY_BORDER_FOUND && city == data[i.Key].ClosestCity && !data[i.Key].HasCity.GetValueOrDefault())
								tileColors[i.Key] = XKCDColors.Yellow;
						}

						for (int i = 0; i < path.Length - 1; i++)
						{
							tileColors[path[i]] = XKCDColors.Orange;
						}

						tileColors[CurrentTile] = XKCDColors.Pink;

						var tmp = TileData._frontier;
						if (tmp != null)
						{
							foreach (var i in tmp)
							{
								tileColors[i] = XKCDColors.BabyPukeGreen;
								//DebugUtils.Assert(data[i].ClosestCity != city);
							}
						}
					}
				}
			}

			tileOverlay.UpdateTiles(tileColors);
		}

		protected override void Log(string message)
		{
			Utils.Log(message);
		}
	}
}
