using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralCities
{
	public class ContentCatalog
	{
		Octree objects = new Octree();
		Coordinates LastPosition = Coordinates.KSC;

		readonly double radius;

		public ContentCatalog(CelestialBody body)
		{
			radius = body.Radius;
		}

		public void Add(WorldObject obj)
		{
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);

			lock (objects)
			{
				objects.Add(obj);
			}

			//obj.visible = Coordinates.Distance(LastPosition, obj.Position) * sphere.radius < obj.VisibleDistance;
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

		public void UpdateVisibleObjects(Coordinates position)
		{
			DebugUtils.Assert(ThreadDispatcher.IsMainThread);
			//var position = sphere.gameObject.transform.InverseTransformPoint(sphere.target.transform.position);
			//LastPosition = new Coordinates(position.x, position.y, position.z);
			LastPosition = position;

			lock (objects)
			{
				#if DEBUG
				int n = 0;
				int nbVisible = 0;
				var watch = System.Diagnostics.Stopwatch.StartNew();
				#endif
				foreach (var i in objects.Find(LastPosition, 0.1))
				{
					i.visible = Coordinates.Distance(LastPosition, i.Position) * radius < i.VisibleDistance;
					#if DEBUG
					n++;
					if (Coordinates.Distance(LastPosition, i.Position) * radius < i.VisibleDistance)
						nbVisible++;
					#endif
				}

				#if DEBUG
				Utils.Log("UpdateVisibleObjects: considered {0} objects out of {1} in {2} ms, {3} visible", n, objects.Count, watch.ElapsedMilliseconds, nbVisible);
				#endif
			}
		}

		/*void DisableAllObjects()
		{
			lock (objects)
			{
				foreach (var i in objects)
				{
					i.visible = false;
				}
			}
		}

		/*void Setup()
		{
		}

		public override void OnUpdateFinished()
		{
			//Utils.Log("OnUpdateFinished");
			//UpdateVisibleObjects();
		}

		public override void OnSetup()
		{
			Utils.Log("OnSetup");
			Setup();
			DisableAllObjects();
		}*/
	}
}

