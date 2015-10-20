using System;
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
			public List<Pair<int, int>> MidpointCache = new List<Pair<int, int>>();
			public Vertex(Coordinates c)
			{
				Coord = c;
			}
		}

		int Level;
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

			vertices[a].MidpointCache.Add(new Pair<int, int>(b, index));

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

		void Split(Triangle tri, int level)
		{
			if (tri.level >= level)
				return;

			if (tri.Children == null)
				Split(tri);

			foreach (var i in tri.Children)
			{
				Split(triangles[i], level);
			}
		}

		public void Split(int vertex, int level)
		{
			int tmp = Math.Max(Level, vertices[vertex].Triangles.Select(x => triangles[x].level).Min());
			var tris = vertices[vertex].Triangles.Where(x => triangles[x].level == tmp).ToList();

			foreach (var i in tris)
			{
				Split(triangles[i], level);
			}
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

		public List<int> GetNeighbors(int v)
		{
			List<int> ret = new List<int>();

			foreach (var i in vertices[v].Triangles)
			{
				if (!(triangles[i].a == v || triangles[i].b == v || triangles[i].c == v)) throw new Exception("oops1");
				if (!(vertices[triangles[i].a].Triangles.Contains(i))) throw new Exception("oops2");
				if (!(vertices[triangles[i].b].Triangles.Contains(i))) throw new Exception("oops3");
				if (!(vertices[triangles[i].c].Triangles.Contains(i))) throw new Exception("oops4");

				if (triangles[i].Children != null)
					continue;

				if (triangles[i].a != v && !ret.Contains(triangles[i].a))
					ret.Add(triangles[i].a);

				if (triangles[i].b != v && !ret.Contains(triangles[i].b))
					ret.Add(triangles[i].b);

				if (triangles[i].c != v && !ret.Contains(triangles[i].c))
					ret.Add(triangles[i].c);
			}

			return ret;
		}

		public Icosphere(int level)
		{
			Level = level;
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
	}
}
