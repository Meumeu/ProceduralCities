using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralCities
{
	public class Octree
	{
		readonly Octree[] children = new Octree[8];
		readonly Vector3d Origin;
		readonly double HalfDimension;
		WorldObject data;

		Octree(Vector3d orig, double halfdim)
		{
			Origin = orig;
			HalfDimension = halfdim;
		}

		public Octree()
		{
			Origin = Vector3d.zero;
			HalfDimension = 1;
		}

		int GetOctant(Vector3d p)
		{
			int ret = 0;
			if (p.x > Origin.x)
				ret |= 4;
			if (p.y > Origin.y)
				ret |= 2;
			if (p.z > Origin.z)
				ret |= 1;

			DebugUtils.Assert(ret >= 0 && ret <= 7);
			return ret;
		}

		int GetOctant(Coordinates c)
		{
			return GetOctant(new Vector3d(c.x, c.y, c.z));
		}

		bool IsLeafNode
		{
			get
			{
				return children[0] == null;
			}
		}

		void PrivateAdd(WorldObject value)
		{
			if (IsLeafNode)
			{
				if (data == null)
				{
					data = value;
					return;
				}

				for (int i = 0; i < 8; i++)
				{
					Vector3d newOrigin = Origin;
					newOrigin.x += HalfDimension * (((i & 4) == 4) ? 0.5 : -0.5);
					newOrigin.y += HalfDimension * (((i & 2) == 2) ? 0.5 : -0.5);
					newOrigin.z += HalfDimension * (((i & 1) == 1) ? 0.5 : -0.5);

					children[i] = new Octree(newOrigin, HalfDimension / 2);
				}

				DebugUtils.Assert(children[GetOctant(data.Position)] != null);
				children[GetOctant(data.Position)].PrivateAdd(data);

				DebugUtils.Assert(children[GetOctant(value.Position)] != null);
				children[GetOctant(value.Position)].PrivateAdd(value);

				data = null;
			}
			else
			{
				DebugUtils.Assert(children[GetOctant(value.Position)] != null);
				children[GetOctant(value.Position)].PrivateAdd(value);
			}
		}

		public void Add(WorldObject value)
		{
			PrivateAdd(value);
			Count++;
		}

		IEnumerable<WorldObject> Find(Vector3d min, Vector3d max)
		{
			if (IsLeafNode)
			{
				if (data != null)
				{
					yield return data;
				}
			}
			else
			{
				for (int i = 0; i < 8; i++)
				{
					Vector3d halfDim = new Vector3d(children[i].HalfDimension, children[i].HalfDimension, children[i].HalfDimension);
					Vector3d cmin = children[i].Origin - halfDim;
					Vector3d cmax = children[i].Origin + halfDim;

					if (cmax.x < min.x || cmax.y < min.y || cmax.z < min.z)
						continue;

					if (cmin.x > max.x || cmin.y > max.y || cmin.z > max.z)
						continue;
					
					foreach (var j in children[i].Find(min, max))
						yield return j;
				}
			}
		}

		public int Count { get; private set; }

		public IEnumerable<WorldObject> Find(Coordinates position, double distance)
		{
			Vector3d pmin = new Vector3d(position.x - distance, position.y - distance, position.z - distance);
			Vector3d pmax = new Vector3d(position.x + distance, position.y + distance, position.z + distance);

			return Find(pmin, pmax);
		}
	}
}

