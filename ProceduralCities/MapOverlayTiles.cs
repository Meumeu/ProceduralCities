using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class MapOverlayTiles : MapOverlay
	{
		public KSPPlanet planet;

		public MapOverlayTiles()
		{
		}

		void AddTile(TileGeometry tile, Color32 c, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
		{
			int vertidx = vertices.Count;

			vertices.Add(tile.Center.Vector);
			vertices.AddRange(tile.Vertices.Select(x => (Vector3)(0.9*x.Vector + 0.1*tile.Center.Vector)));

			colors.AddRange(Enumerable.Range(0, tile.Vertices.Length + 1).Select(x => c));

			for (int i = 0; i < tile.Vertices.Length; i++)
			{
				int j = (i + 1) % tile.Vertices.Length;
				triangles.Add(vertidx);
				triangles.Add(vertidx + i + 1);
				triangles.Add(vertidx + j + 1);
			}
		}

		public void UpdateTiles(IEnumerable<KeyValuePair<int, Color32>> tilecolors)
		{
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Color32> colors = new List<Color32>();

			/*double area = planet.Tiles[planet.CurrentTile].Area;
			AddTile(planet.Tiles[planet.CurrentTile], new Color32(255, 0, 0, 128), vertices, triangles, colors);

			foreach (var i in planet.Tiles[planet.CurrentTile].Neighbours)
			{
				area += planet.Tiles[i].Area;
				AddTile(planet.Tiles[i], new Color32(0, 0, 255, 128), vertices, triangles, colors);
			}

			Utils.Log("Current tile+neighbours area: {0:F1} km2", area * Math.Pow(Body.Radius / 1000, 2));*/

			foreach(var i in tilecolors)
			{
				AddTile(Planet.Tiles[i.Key], i.Value, vertices, triangles, colors);
			}

			UpdateMesh(vertices, triangles, colors);
		}
	}
}

