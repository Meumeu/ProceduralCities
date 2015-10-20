using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class RoadOverlay : MapOverlay
	{
		struct Road
		{
			readonly public List<Vector3> positions;
			readonly public List<Vector3> normal;
			readonly public Color32 color;
			readonly public float width;

			public Road(List<Vector3> positions, Color32 color, double width)
			{
				if (positions.Count < 2)
					throw new ArgumentOutOfRangeException("positions", "At least two points are required");

				this.positions = positions;
				this.color = color;
				this.width = (float)width;

				normal = new List<Vector3>();

				int n = positions.Count;

				normal.Add(Vector3.Cross(positions[0] + positions[1], positions[0] - positions[1]).normalized);

				for(int i = 1; i < n - 1; i++)
				{
					Vector3 n1 = Vector3.Cross(positions[i - 1] + positions[i], positions[i - 1] - positions[i]).normalized;
					Vector3 n2 = Vector3.Cross(positions[i] + positions[i + 1], positions[i] - positions[i + 1]).normalized;
					normal.Add((n1 + n2).normalized);
				}

				normal.Add(Vector3.Cross(positions[n - 2] + positions[n - 1], positions[n - 2] - positions[n - 1]).normalized);
			}
		}

		List<Road> roads;
		public readonly Color32 DefaultColor = new Color32(255, 0, 0, 128);

		public RoadOverlay()
		{
			roads = new List<Road>();
		}

		new protected void Awake()
		{
			base.Awake();
		}

		public void Clear()
		{
			lock (roads)
				roads.Clear();
		}

		public void Add(IEnumerable<Coordinates> positions, Color32 c, double width = 0.003)
		{
			Road r = new Road(positions.Select(x => (Vector3)new Vector3d(x.x, x.y, x.z)).ToList(), c, width);

			lock (roads)
				roads.Add(r);
		}

		public void UpdateMesh()
		{
			DebugUtils.Assert(PlanetDatabase.Instance.IsMainThread);
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Color32> colors = new List<Color32>();

			lock (roads)
			{
				for (int i = 0, n = roads.Count; i < n; i++)
				{
					var road = roads[i];
					int index0 = vertices.Count;

					for (int j = 0, m = road.positions.Count; j < m; j++)
					{
						vertices.Add(road.positions[j] + road.normal[j] * road.width / 2);
						vertices.Add(road.positions[j] - road.normal[j] * road.width / 2);
						colors.Add(roads[i].color);
						colors.Add(roads[i].color);
					}

					for (int j = 0, m = road.positions.Count - 1; j < m; j++)
					{
						triangles.AddRange(new[] {
							index0 + 2 * j + 0, index0 + 2 * j + 1, index0 + 2 * j + 2,
							index0 + 2 * j + 1, index0 + 2 * j + 3, index0 + 2 * j + 2
						});
					}
				}

				Debug.Log("[ProceduralCities] " + roads.Count + " roads, " + vertices.Count + " vertices, " + triangles.Count + " triangles");
			}

			UpdateMesh(vertices, triangles, colors);
		}
	}
}

