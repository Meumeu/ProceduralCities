using System;
using UnityEngine;

namespace ProceduralCities
{
	public abstract class WorldObject
	{
		public float UnloadDistance;
		public float VisibleDistance;
		public Coordinates Position;
		//public readonly string Planet;
		public readonly CelestialBody Body;
		protected GameObject gameObject;
		bool initialized;

		protected MeshRenderer Renderer
		{
			get { return gameObject.GetComponent<MeshRenderer>(); }
		}

		protected MeshCollider Collider
		{
			get { return gameObject.GetComponent<MeshCollider>(); }
		}

		public bool visible
		{
			get
			{
				if (gameObject == null)
					return false;

				return gameObject.activeSelf;
			}
			set
			{
				if (value)
				{
					if (!initialized)
					{
						Initialize();
						initialized = true;
					}

					DebugUtils.Assert(gameObject != null);
					DebugUtils.Assert(Renderer != null);

					gameObject.SetActive(true);

					if (Renderer != null)
						Renderer.enabled = value;
					
					if (Collider != null)
						Collider.enabled = value;
					
				}
				else
				{
					if (gameObject != null)
						gameObject.SetActive(false);
				}
			}
		}

		public WorldObject(CelestialBody body)
		{
			Body = body;
			UnloadDistance = float.MaxValue;
			VisibleDistance = 40000f;
		}

		public abstract void Destroy();
		protected abstract void Initialize();
	}
}

