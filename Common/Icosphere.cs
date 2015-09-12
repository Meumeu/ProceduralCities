using System;
using System.Linq;
using System.Collections.Generic;

// http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
namespace ProceduralCities
{
	
	/*[Serializable]
	public class IcosphereOld
	{
		struct Triangle
		{
			public int a, b, c;
			public Triangle(int a, int b, int c)
			{
				this.a = a;
				this.b = b;
				this.c = c;
			}
		}

		public List<Planet.Vertex> Vertices;
		public int[,] Edges;
		List<Triangle> Triangles;

		int AddVertex(Coordinates v)
		{
			Vertices.Add(new Planet.Vertex(v));
			return Vertices.Count - 1;
		}

		void AddEdge(int v1, int v2)
		{
			for (int i = 0; i < 6; i++)
			{
				if (Edges[v1, i] == v2)
					return;
				else if (Edges[v1, i] == -1)
				{
					Edges[v1, i] = v2;
					return;
				}
			}

			throw new InvalidOperationException("Unreachable");
		}

		int GetMidPoint(int v1, int v2, int[,] midpoints)
		{
			if (v1 > v2)
			{
				int tmp = v1;
				v1 = v2;
				v2 = tmp;
			}

			int ret, i;
			for (i = 0; i < 6; i++)
			{
				if (midpoints[v1, 2 * i] == v2)
					return midpoints[v1, 2 * i + 1];

				if (midpoints[v1, 2 * i] == -1)
				{
					ret = AddVertex(new Coordinates(
						Vertices[v1].coord.x + Vertices[v2].coord.x,
						Vertices[v1].coord.y + Vertices[v2].coord.y,
						Vertices[v1].coord.z + Vertices[v2].coord.z));

					midpoints[v1, 2 * i] = v2;
					midpoints[v1, 2 * i + 1] = ret;

					return ret;
				}
			}

			throw new InvalidOperationException("Unreachable");
		}

		public IcosphereOld(int level)
		{
			Vertices = new List<Planet.Vertex>();
			Triangles = new List<Triangle>();

			var t = (1.0 + Math.Sqrt(5.0)) / 2.0;

			AddVertex(new Coordinates(-1,  t, 0));
			AddVertex(new Coordinates( 1,  t, 0));
			AddVertex(new Coordinates(-1, -t, 0));
			AddVertex(new Coordinates( 1, -t, 0));

			AddVertex(new Coordinates(0, -1,  t));
			AddVertex(new Coordinates(0,  1,  t));
			AddVertex(new Coordinates(0, -1, -t));
			AddVertex(new Coordinates(0,  1, -t));

			AddVertex(new Coordinates( t, 0, -1));
			AddVertex(new Coordinates( t, 0,  1));
			AddVertex(new Coordinates(-t, 0, -1));
			AddVertex(new Coordinates(-t, 0,  1));

			Triangles.Add(new Triangle(0, 11, 5));
			Triangles.Add(new Triangle(0, 5, 1));
			Triangles.Add(new Triangle(0, 1, 7));
			Triangles.Add(new Triangle(0, 7, 10));
			Triangles.Add(new Triangle(0, 10, 11));

			// 5 adjacent faces
			Triangles.Add(new Triangle(1, 5, 9));
			Triangles.Add(new Triangle(5, 11, 4));
			Triangles.Add(new Triangle(11, 10, 2));
			Triangles.Add(new Triangle(10, 7, 6));
			Triangles.Add(new Triangle(7, 1, 8));

			// 5 faces around point 3
			Triangles.Add(new Triangle(3, 9, 4));
			Triangles.Add(new Triangle(3, 4, 2));
			Triangles.Add(new Triangle(3, 2, 6));
			Triangles.Add(new Triangle(3, 6, 8));
			Triangles.Add(new Triangle(3, 8, 9));

			// 5 adjacent faces
			Triangles.Add(new Triangle(4, 9, 5));
			Triangles.Add(new Triangle(2, 4, 11));
			Triangles.Add(new Triangle(6, 2, 10));
			Triangles.Add(new Triangle(8, 6, 7));
			Triangles.Add(new Triangle(9, 8, 1));

			for (int i = 0; i < level; i++)
			{
				int[,] midpoints = new int[Vertices.Count, 12];
				for (int j = 0; j < Vertices.Count; j++)
					for (int k = 0; k < 6; k++)
						midpoints[j, 2 * k] = -1;

				var Triangles2 = new List<Triangle>();
				foreach (var tri in Triangles)
				{
					int a = GetMidPoint(tri.b, tri.c, midpoints);
					int b = GetMidPoint(tri.a, tri.c, midpoints);
					int c = GetMidPoint(tri.a, tri.b, midpoints);

					Triangles2.Add(new Triangle(tri.a, b, c));
					Triangles2.Add(new Triangle(tri.b, a, c));
					Triangles2.Add(new Triangle(tri.c, a, b));
					Triangles2.Add(new Triangle(a, b, c));
				}
				Triangles = Triangles2;
			}

			Edges = new int[Vertices.Count, 6];
			for (int i = 0; i < Vertices.Count; i++)
				for (int j = 0; j < 6; j++)
					Edges[i, j] = -1;

			foreach (Triangle i in Triangles)
			{
				AddEdge(i.a, i.b);
				AddEdge(i.b, i.a);
				AddEdge(i.b, i.c);
				AddEdge(i.c, i.b);
				AddEdge(i.a, i.c);
				AddEdge(i.c, i.a);
			}

			Triangles = null;
		}
	}*/

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


			/*foreach (var i in vertices[a].Triangles)
			{
				if (triangles[i].a == a && triangles[i].b == b)
					v.Triangles.Add(i);
				if (triangles[i].a == a && triangles[i].c == b)
					v.Triangles.Add(i);
				if (triangles[i].b == a && triangles[i].c == b)
					v.Triangles.Add(i);
				if (triangles[i].a == b && triangles[i].b == a)
					v.Triangles.Add(i);
				if (triangles[i].a == b && triangles[i].c == a)
					v.Triangles.Add(i);
				if (triangles[i].b == b && triangles[i].c == a)
					v.Triangles.Add(i);
			}*/

			return index;
		}

		void Split(Triangle tri)
		{
			System.Diagnostics.Debug.Assert(tri.Children == null);

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
