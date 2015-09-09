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

				var renderer = gameObject.GetComponent<MeshRenderer>();
				if (renderer == null)
					return false;

				return renderer.enabled;
			}
			set
			{
				if (!initialized)
				{
					Initialize();
					initialized = true;
				}

				System.Diagnostics.Debug.Assert(gameObject != null);
				System.Diagnostics.Debug.Assert(gameObject.GetComponent<MeshRenderer>() != null);

				if (gameObject.GetComponent<MeshRenderer>().enabled != value)
				{
					gameObject.GetComponent<MeshRenderer>().enabled = value;
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

