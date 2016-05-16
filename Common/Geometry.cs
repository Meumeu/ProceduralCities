using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
namespace ProceduralCities
{
	public class Icosphere
	{
		public class Triangle
		{
			public int[] Children;
			public readonly int a;
			public readonly int b;
			public readonly int c;
			public readonly int level;
			public Triangle(int a, int b, int c)
			{
				this.a = a;
				this.b = b;
				this.c = c;
				level = 0;
			}
			public Triangle(int a, int b, int c, int level)
			{
				this.a = a;
				this.b = b;
				this.c = c;
				this.level = level;
			}
		}

		public class Vertex
		{
			public readonly Coordinates Coord;
			public List<int> Triangles = new List<int>();
			public List<PairIntInt> MidpointCache = new List<PairIntInt>();
			public Vertex(Coordinates c)
			{
				Coord = c;
			}
		}

		List<Triangle> triangles = new List<Triangle>();
		List<Vertex> vertices = new List<Vertex>();

		public int GetMidpoint(int a, int b)
		{
			if (a > b)
				return GetMidpoint(b, a);

			foreach (var i in vertices[a].MidpointCache)
			{
				if (i.item1 == b)
					return i.item2;
			}

			int index = AddVertex(Coordinates.LinearCombination(0.5, vertices[a].Coord, 0.5, vertices[b].Coord));
			var v = vertices[index];

			vertices[a].MidpointCache.Add(new PairIntInt(b, index));

			return index;
		}

		void Split(Triangle tri)
		{
			DebugUtils.Assert(tri.Children == null);

			int ab = GetMidpoint(tri.a, tri.b);
			int ac = GetMidpoint(tri.a, tri.c);
			int bc = GetMidpoint(tri.b, tri.c);

			tri.Children = new int[4];
			int index = triangles.Count;
			tri.Children[0] = index;
			tri.Children[1] = index + 1;
			tri.Children[2] = index + 2;
			tri.Children[3] = index + 3;

			triangles.Add(new Triangle(tri.a, ab, ac, tri.level + 1));
			triangles.Add(new Triangle(tri.b, ab, bc, tri.level + 1));
			triangles.Add(new Triangle(tri.c, bc, ac, tri.level + 1));
			triangles.Add(new Triangle(ab, ac, bc, tri.level + 1));

			vertices[tri.a].Triangles.Add(index);
			vertices[ab].Triangles.Add(index);
			vertices[ac].Triangles.Add(index);

			vertices[tri.b].Triangles.Add(index + 1);
			vertices[ab].Triangles.Add(index + 1);
			vertices[bc].Triangles.Add(index + 1);

			vertices[tri.c].Triangles.Add(index + 2);
			vertices[bc].Triangles.Add(index + 2);
			vertices[ac].Triangles.Add(index + 2);

			vertices[ab].Triangles.Add(index + 3);
			vertices[ac].Triangles.Add(index + 3);
			vertices[bc].Triangles.Add(index + 3);
		}

		int AddVertex(double x, double y, double z)
		{
			vertices.Add(new Vertex(new Coordinates(x, y, z)));
			return vertices.Count - 1;
		}

		int AddVertex(Coordinates c)
		{
			vertices.Add(new Vertex(c));
			return vertices.Count - 1;
		}

		void AddRootTriangle(int a, int b, int c)
		{
			Triangle tri = new Triangle(a, b, c);
			int index = triangles.Count;

			vertices[a].Triangles.Add(index);
			vertices[b].Triangles.Add(index);
			vertices[c].Triangles.Add(index);

			triangles.Add(tri);
		}

		public IEnumerable<Coordinates> GetCoordinates()
		{
			return vertices.Select(x => x.Coord);
		}

		public IEnumerable<Triangle> GetTriangles()
		{
			return triangles.Where(i => i.Children == null);
		}

		bool IntArrayContains(int[] arr, int lastplusone, int value)
		{
			for (int i = 0; i < lastplusone; i++)
			{
				if (arr[i] == value)
					return true;
			}
			return false;
		}

		public int[] GetNeighbors(int v)
		{
			var tri = vertices[v].Triangles.Where(i => triangles[i].Children == null).ToArray();
			int[] ret = new int[tri.Length];
			

			int j = 0;
			foreach (var i in tri)
			{
				DebugUtils.Assert((triangles[i].a == v || triangles[i].b == v || triangles[i].c == v));
				DebugUtils.Assert((vertices[triangles[i].a].Triangles.Contains(i)));
				DebugUtils.Assert((vertices[triangles[i].b].Triangles.Contains(i)));
				DebugUtils.Assert((vertices[triangles[i].c].Triangles.Contains(i)));
				DebugUtils.Assert(triangles[i].Children == null);

				if (triangles[i].a != v && !IntArrayContains(ret, j, triangles[i].a))
					ret[j++] = triangles[i].a;

				if (triangles[i].b != v && !IntArrayContains(ret, j, triangles[i].b))
					ret[j++] = triangles[i].b;

				if (triangles[i].c != v && !IntArrayContains(ret, j, triangles[i].c))
					ret[j++] = triangles[i].c;
			}
			DebugUtils.Assert(j == ret.Length);

			return ret;
		}

		Coordinates[] GetDual(int v)
		{
			var tri = vertices[v].Triangles.Where(i => triangles[i].Children == null).ToArray();

			Coordinates[] ret = new Coordinates[tri.Length];

			int j = 0;
			foreach (var i in tri)
			{
				DebugUtils.Assert((triangles[i].a == v || triangles[i].b == v || triangles[i].c == v));
				DebugUtils.Assert((vertices[triangles[i].a].Triangles.Contains(i)));
				DebugUtils.Assert((vertices[triangles[i].b].Triangles.Contains(i)));
				DebugUtils.Assert((vertices[triangles[i].c].Triangles.Contains(i)));
				DebugUtils.Assert(triangles[i].Children == null);

				ret[j++] = Coordinates.LinearCombination(
					1, vertices[triangles[i].a].Coord,
					1, vertices[triangles[i].b].Coord,
					1, vertices[triangles[i].c].Coord);
			}

			return ret;
		}

		public TileGeometry[] GetDual()
		{
			TileGeometry[] ret = new TileGeometry[vertices.Count];

			for (int i = 0; i < vertices.Count; i++)
			{
				var face = new TileGeometry(GetDual(i));
				DebugUtils.Assert(face.Vertices.Length == 5 || face.Vertices.Length == 6);
				face.Neighbours = GetNeighbors(i);
				ret[i] = face;
			}

			return ret;
		}

		public Icosphere(int level)
		{
			var t = (1.0 + Math.Sqrt(5.0)) / 2.0;

			AddVertex(-1,  t, 0);
			AddVertex( 1,  t, 0);
			AddVertex(-1, -t, 0);
			AddVertex( 1, -t, 0);

			AddVertex(0, -1,  t);
			AddVertex(0,  1,  t);
			AddVertex(0, -1, -t);
			AddVertex(0,  1, -t);

			AddVertex( t, 0, -1);
			AddVertex( t, 0,  1);
			AddVertex(-t, 0, -1);
			AddVertex(-t, 0,  1);

			AddRootTriangle(0, 11, 5);
			AddRootTriangle(0, 5, 1);
			AddRootTriangle(0, 1, 7);
			AddRootTriangle(0, 7, 10);
			AddRootTriangle(0, 10, 11);

			// 5 adjacent faces
			AddRootTriangle(1, 5, 9);
			AddRootTriangle(5, 11, 4);
			AddRootTriangle(11, 10, 2);
			AddRootTriangle(10, 7, 6);
			AddRootTriangle(7, 1, 8);

			// 5 faces around point 3
			AddRootTriangle(3, 9, 4);
			AddRootTriangle(3, 4, 2);
			AddRootTriangle(3, 2, 6);
			AddRootTriangle(3, 6, 8);
			AddRootTriangle(3, 8, 9);

			// 5 adjacent faces
			AddRootTriangle(4, 9, 5);
			AddRootTriangle(2, 4, 11);
			AddRootTriangle(6, 2, 10);
			AddRootTriangle(8, 6, 7);
			AddRootTriangle(9, 8, 1);

			for (int i = 0, j = 0; i < level; i++)
			{
				int n = triangles.Count;
				for (; j < n; j++)
				{
					Split(triangles[j]);
				}
			}
		}

		public int CountVertices { get { return vertices.Count; } }
		public int CountTriangles { get { return triangles.Count; } }
	}

	[Serializable]
	public class TileGeometry
	{
		public readonly Coordinates Center;
		public readonly Coordinates[] Vertices;
		public int[] Neighbours;

		double AngleAround(Vector3d v1, Vector3d v2, Vector3d around)
		{
			around = around.normalized;
			Vector3d u1 = Vector3d.Exclude(around, v1).normalized;
			Vector3d u2 = Vector3d.Exclude(around, v2).normalized;

			return Math.Atan2(Vector3d.Dot(around, Vector3d.Cross(u1, u2)), Vector3d.Dot(u1, u2));
		}

		public TileGeometry(Coordinates[] vertices)
		{
			int nb = vertices.Length;
			Center = new Coordinates(vertices.Sum(i => i.x), vertices.Sum(i => i.y), vertices.Sum(i => i.z));

			Vertices = vertices.OrderBy(i => AngleAround(vertices[0].Vector, i.Vector, Center.Vector)).ToArray();
		}

		public TileGeometry(BinaryReader reader)
		{
			double lon = reader.ReadDouble();
			double lat = reader.ReadDouble();
			Center = new Coordinates(lat, lon);
			int n = reader.ReadInt32();
			Vertices = new Coordinates[n];
			Neighbours = new int[n];
			for (int i = 0; i < n; i++)
			{
				lon = reader.ReadDouble();
				lat = reader.ReadDouble();
				Vertices[i] = new Coordinates(lat, lon);
				Neighbours[i] = reader.ReadInt32();
			}
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(Center.Longitude);
			writer.Write(Center.Latitude);
			DebugUtils.Assert(Vertices.Length == Neighbours.Length);
			writer.Write(Vertices.Length);
			for (int i = 0; i < Vertices.Length; i++)
			{
				writer.Write(Vertices[i].Longitude);
				writer.Write(Vertices[i].Latitude);
				writer.Write(Neighbours[i]);
			}
		}

		public bool Contains(Vector3d v)
		{
			// works only for convex polygons
			v = v.normalized;

			if (Vector3d.Dot(v, Center.Vector) < 0)
				return false;

			v = v - Center.Vector;

			for (int i = 0; i < Vertices.Length; i++)
			{
				var v1 = Vertices[i].Vector - Center.Vector;
				var v2 = Vertices[(i + 1) % Vertices.Length].Vector - Center.Vector;
				//var normal = Vector3d.Cross(v1, v2);
				var normal = Vector3d.Exclude(v1 - v2, v1);

				if (Vector3d.Dot(v - v1, normal) > 0)
					return false;
			}

			return true;
		}

		public double Area
		{
			get
			{
				double area = 0;
				for (int i = 0; i < Vertices.Length; i++)
				{
					Vector3d u = Vertices[i].Vector - Center.Vector;
					Vector3d v = Vertices[(i + 1) % Vertices.Length].Vector - Center.Vector;

					area += Vector3d.Cross(u, v).magnitude / 2;
				}
				return area;
			}
		}

		public Coordinates[] Grid(int resolution)
		{
			Coordinates[] ret = new Coordinates[1 + Vertices.Length * resolution * (resolution - 1) / 2];
			ret[0] = Center;
			int m = 1;

			for (int i = 0, j = Vertices.Length - 1; i < Vertices.Length; j = i++)
			{
				Coordinates u = Vertices[i];
				Coordinates v = Vertices[j];
				for (int k = 1; k < resolution; k++)
				{
					for (int l = 0; l < k; l++)
					{
						ret[m++] = Coordinates.LinearCombination(l, u, k - l, v, resolution - 1 - k, Center);
					}
				}
			}

			DebugUtils.Assert(m == ret.Length);

			return ret;
		}
	}
}
