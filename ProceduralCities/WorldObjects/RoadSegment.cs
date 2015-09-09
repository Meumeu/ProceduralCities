using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class RoadSegment : WorldObject
	{
		MeshFilter meshFilter;
		MeshRenderer renderer;
		Mesh mesh;

		List<Vector3d> coordinates;
		List<Vector3d> normals;
		double width;
		Vector3 center;

		private RoadSegment(CelestialBody body, List<Vector3d> coordinates, List<Vector3d> normals, double width)
		{
			System.Diagnostics.Debug.Assert(false);
			System.Diagnostics.Debug.Assert(PlanetDatabase.Instance.IsMainThread);
			UnloadDistance = float.MaxValue;
			VisibleDistance = 40000f;
			Planet = body.name;
			Body = body;
			this.coordinates = coordinates;
			this.normals = normals;
			this.width = width;

			center = coordinates.Aggregate(Vector3d.zero, (i, j) => new Vector3d(i.x + j.x, i.y + j.y, i.z + j.z)) / coordinates.Count;
			Position = new Coordinates(center.x, center.y, center.z);
		}

		protected override void Initialize()
		{
			gameObject = new GameObject();
			meshFilter = gameObject.AddComponent<MeshFilter>();
			renderer = gameObject.AddComponent<MeshRenderer>();
			mesh = meshFilter.mesh;

			gameObject.layer = 15; // Local scenery, avoids showing reentry effects
			gameObject.transform.parent = Body.transform;
			gameObject.transform.localScale = Vector3.one;
			gameObject.transform.localPosition = center;
			gameObject.transform.localRotation = Quaternion.identity;

			var material = new Material(Shader.Find("Diffuse"));
			material.mainTexture = GameDatabase.Instance.GetTexture("ProceduralCities/Assets/Road", false);
			renderer.material = material;

			List<double> terrain = coordinates.Select(x => Body.pqsController.GetSurfaceHeight(x)).ToList();

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uv = new List<Vector2>();

			for (int i = 0, n = coordinates.Count; i < n; i++)
			{
				vertices.Add(coordinates[i].normalized * (terrain[i] + 2) + normals[i] * width / 2 - center);
				vertices.Add(coordinates[i].normalized * (terrain[i] + 2) - center);
				vertices.Add(coordinates[i].normalized * (terrain[i] + 2) - normals[i] * width / 2 - center);
				uv.Add(new Vector2(1, i));
				uv.Add(new Vector2(0, i));
				uv.Add(new Vector2(1, i));
			}

			for (int i = 0, n = coordinates.Count - 1; i < n; i++)
			{
				triangles.AddRange(new[] {
					3 * i + 0,  3 * i + 3,  3 * i + 1,
					3 * i + 1,  3 * i + 4,  3 * i + 2,
					3 * i + 1,  3 * i + 3,  3 * i + 4,
					3 * i + 2,  3 * i + 4,  3 * i + 5
				});
			}

			UpdateMesh(vertices, triangles, uv);
		}

		void UpdateMesh(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Vector2> uv)
		{
			System.Diagnostics.Debug.Assert(PlanetDatabase.Instance.IsMainThread);

			mesh.Clear();

			mesh.vertices = vertices.ToArray();
			mesh.uv = uv.ToArray();
			mesh.triangles = triangles.ToArray();

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.Optimize();
			try
			{
				renderer.enabled = true;
			}
			catch(Exception)
			{
			}
		}

		static List<Vector3d> ComputeNormals(List<Vector3d> points)
		{
			if (points.Count < 2)
				throw new ArgumentOutOfRangeException("points", "At least two points are required");

			List<Vector3d> normal = new List<Vector3d>();

			int n = points.Count;

			normal.Add(Vector3d.Cross(points[0] + points[1], points[0] - points[1]).normalized);

			for(int i = 1; i < n - 1; i++)
			{
				Vector3d n1 = Vector3d.Cross(points[i - 1] + points[i], points[i - 1] - points[i]).normalized;
				Vector3d n2 = Vector3d.Cross(points[i] + points[i + 1], points[i] - points[i + 1]).normalized;
				normal.Add((n1 + n2).normalized);
			}

			normal.Add(Vector3d.Cross(points[n - 2] + points[n - 1], points[n - 2] - points[n - 1]).normalized);

			System.Diagnostics.Debug.Assert(normal.Count == points.Count);

			return normal;
		}

		public static void MakeSegments(CelestialBody body, List<Coordinates> coordinates, double segmentLength = 5000, double width = 40)
		{
			System.Diagnostics.Debug.Assert(!PlanetDatabase.Instance.IsMainThread);

			double radius = body.Radius;
			double triangleLength = width;
			List<Vector3d> positions = coordinates.Select(u => new Vector3d(u.x, u.y, u.z) * radius).ToList();

			// Compute the positions of every point on the road
			List<Vector3d> positions2 = new List<Vector3d>();
			for (int i = 1, n = positions.Count; i < n; i++)
			{
				double L = (positions[i] - positions[i - 1]).magnitude;
				int nbTriangles = (int)Math.Ceiling(L / triangleLength);
				for (int j = 0; j < nbTriangles; j++)
				{
					positions2.Add((positions[i] * j + positions[i - 1] * (nbTriangles - j)).normalized * radius);
				}
			}
			positions2.Add(positions[positions.Count - 1]);

			// Compute the normal to every point
			List<Vector3d> normal = ComputeNormals(positions2);

			// Split into segments
			int currentStart = 0;
			double currentLength = 0;
			for (int i = 1, n = positions2.Count; i < n; i++)
			{
				currentLength += (positions2[i] - positions2[i - 1]).magnitude;
				if (currentLength > segmentLength)
				{
					int start = currentStart;
					int count = i - start + 1;
					PlanetDatabase.QueueToMainThread(() =>
					{
						PlanetDatabase.Instance.AddWorldObject(
							new RoadSegment(
								body,
								positions2.GetRange(start, count),
								normal.GetRange(start, count),
								width));
					});

					currentStart = i;
					currentLength = 0;
				}
			}

			PlanetDatabase.QueueToMainThread(() =>
			{
				PlanetDatabase.Instance.AddWorldObject(new RoadSegment(
					body,
					positions2.GetRange(currentStart, positions2.Count - currentStart),
					normal.GetRange(currentStart, positions2.Count - currentStart),
					width));
			});
		}



		public override void Destroy()
		{
			GameObject.Destroy(gameObject);
		}
	}
}

