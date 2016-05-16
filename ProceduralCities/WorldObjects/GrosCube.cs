using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class GrosCube : WorldObject
	{
		Vector3 center;

		public override void Destroy()
		{
			GameObject.Destroy(gameObject);
		}

		protected override void Initialize()
		{
			gameObject = new GameObject();
			var meshFilter = gameObject.AddComponent<MeshFilter>();
			var renderer = gameObject.AddComponent<MeshRenderer>();
			var collider = gameObject.AddComponent<BoxCollider>();
			var mesh = meshFilter.mesh;

			gameObject.layer = 15; // Local scenery, avoids showing reentry effects
			gameObject.transform.parent = Body.transform;
			gameObject.transform.localScale = Vector3.one;
			gameObject.transform.localPosition = center;
			gameObject.transform.localRotation = QuaternionD.AngleAxis(Position.Longitude * 180 / Math.PI, Vector3d.down) * QuaternionD.AngleAxis(Position.Latitude * 180 / Math.PI, Vector3d.forward);

			var material = new Material(Shader.Find("Diffuse"));
			material.mainTexture = GameDatabase.Instance.GetTexture("ProceduralCities/Assets/Road", false);
			renderer.material = material;


			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uv = new List<Vector2>();

			float taille = 10;

			vertices.Add(new Vector3(-1, -1, -1)*taille); uv.Add(new Vector2(0, 0));
			vertices.Add(new Vector3(-1, -1,  1)*taille); uv.Add(new Vector2(0, 1));
			vertices.Add(new Vector3(-1,  1, -1)*taille); uv.Add(new Vector2(1, 0));
			vertices.Add(new Vector3(-1,  1,  1)*taille); uv.Add(new Vector2(1, 1));
			vertices.Add(new Vector3( 1, -1, -1)*taille); uv.Add(new Vector2(0, 0));
			vertices.Add(new Vector3( 1, -1,  1)*taille); uv.Add(new Vector2(0, 1));
			vertices.Add(new Vector3( 1,  1, -1)*taille); uv.Add(new Vector2(1, 0));
			vertices.Add(new Vector3( 1,  1,  1)*taille); uv.Add(new Vector2(1, 1));

			triangles.AddRange(new[] { 0, 1, 2, 2, 1, 3 });
			triangles.AddRange(new[] { 4, 6, 5, 5, 6, 7 });
			triangles.AddRange(new[] { 0, 4, 5, 0, 5, 1 });
			triangles.AddRange(new[] { 2, 7, 6, 2, 3, 7 });
			triangles.AddRange(new[] { 1, 5, 7, 3, 1, 7 });
			triangles.AddRange(new[] { 6, 4, 0, 6, 0, 2 });

			mesh.Clear();

			mesh.vertices = vertices.ToArray();
			mesh.uv = uv.ToArray();
			mesh.triangles = triangles.ToArray();

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.Optimize();

			collider.center = Vector3.zero;
			collider.size = new Vector3(taille, taille, taille);
		}

		public GrosCube(CelestialBody body, Coordinates coord): base(body)
		{
			double R = Body.pqsController.GetSurfaceHeight(body.GetRelSurfaceNVector(coord.Latitude * 180 / Math.PI, coord.Longitude * 180 / Math.PI));
			center = new Vector3d(coord.x, coord.y, coord.z) * (R + 50);
			Position = coord;

			UnloadDistance = 40000;
			VisibleDistance = 30000;
		}

		public static void MakePleinDeGrosCubes(CelestialBody body)
		{
			var sphere = new Icosphere(9);

			ThreadDispatcher.QueueToMainThreadSync(() => {
				var worldObjects = body.pqsController.transform.GetComponentInChildren<ContentCatalog>();

				foreach (var i in sphere.GetCoordinates())
				{
					worldObjects.Add(new GrosCube(body, i));
				}
			});
		}
	}
}
	