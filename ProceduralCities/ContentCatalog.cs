using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class ContentCatalog : PQSMod
	{
		List<WorldObject> objects;

		public ContentCatalog()
		{
			objects = new List<WorldObject>();
		}

		public void Add(WorldObject obj)
		{
			lock (objects)
			{
				objects.Add(obj);
			}
		}

		public int Count
		{
			get
			{
				lock (objects)
				{
					return objects.Count;
				}
			}
		}

		public void UpdateVisibleObjects()
		{
			DebugUtils.Assert(PlanetDatabase.Instance.IsMainThread);
			var position = sphere.gameObject.transform.InverseTransformPoint(sphere.target.transform.position);
			Coordinates coord = new Coordinates(position.x, position.y, position.z);

			lock (objects)
			{
				foreach (var i in objects)
				{
					i.visible = Coordinates.Distance(coord, i.Position) * sphere.radius < i.VisibleDistance;
				}
			}
		}

		void DisableAllObjects()
		{
			lock (objects)
			{
				foreach (var i in objects)
				{
					i.visible = false;
				}
			}
		}

		void Setup()
		{
		}

		public override void OnSphereActive()
		{
			UpdateVisibleObjects();
		}

		public override void OnSphereInactive()
		{
			DisableAllObjects();
		}

		/*public override void OnSphereReset()
		{
			DisableAllObjects();
		}

		public override void OnSphereStart()
		{
			DisableAllObjects();
		}*/

		public override void OnUpdateFinished()
		{
			UpdateVisibleObjects();
		}

		public override void OnSetup()
		{
			Setup();
			DisableAllObjects();
		}
	}
}

