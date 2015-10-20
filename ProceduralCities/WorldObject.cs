using System;
using UnityEngine;

namespace ProceduralCities
{
	public abstract class WorldObject
	{
		public float UnloadDistance;
		public float VisibleDistance;
		public Coordinates Position;
		public string Planet;
		public CelestialBody Body;
		protected GameObject gameObject;
		bool initialized;

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
					DebugUtils.Assert(gameObject.GetComponent<MeshRenderer>() != null);

					gameObject.SetActive(true);

					if (gameObject.GetComponent<MeshRenderer>() != null)
						gameObject.GetComponent<MeshRenderer>().enabled = value;
					
					if (gameObject.GetComponent<MeshCollider>() != null)
						gameObject.GetComponent<MeshCollider>().enabled = value;
					
				}
				else
				{
					if (gameObject != null)
						gameObject.SetActive(false);
				}
			}
		}

		public WorldObject()
		{
		}

		public abstract void Destroy();
		protected abstract void Initialize();
	}
}

