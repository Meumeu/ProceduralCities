using System;
using UnityEngine;

namespace ProceduralCities
{
	public class WorldObject
	{
		public float UnloadDistance;
		public Coordinates Position;
		public string Planet;

		protected GameObject gameObject;
		protected MeshFilter meshFilter;
		protected MeshRenderer meshRenderer;
		protected Mesh mesh;

		public WorldObject()
		{
			gameObject = new GameObject();
			meshFilter = gameObject.AddComponent<MeshFilter>();
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			mesh = meshFilter.mesh;
		}

		public void Destroy()
		{
			GameObject.Destroy(gameObject);
		}
	}
}

