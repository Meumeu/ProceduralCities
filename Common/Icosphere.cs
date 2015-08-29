using System;
using System.Collections.Generic;

/*
namespace ProceduralCities
{
	// http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
	public class Icosphere
	{
		public struct Vertex
		{
			public double x, y, z;
			public Vertex(double _x, double _y, double _z)
			{
				x = _x;
				y = _y;
				z = _z;
			}
		}

		struct Triangle
		{
			public int a, b, c;
			public Triangle(int _a, int _b, int _c)
			{
				a = _a;
				b = _b;
				c = _c;
			}
		}

		readonly public List<Vertex> Vertices = new List<Vertex>();
		readonly List<Triangle> Triangles;
		Dictionary<UInt64, int> midpoint = new Dictionary<UInt64, int>();

		int AddVertex(Vertex v)
		{
			double invnorm = 1.0 / Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
			v.x *= invnorm;
			v.y *= invnorm;
			v.z *= invnorm;

			Vertices.Add(v);
			return Vertices.Count - 1;
		}

		int GetMidPoint(int v1, int v2)
		{
			UInt64 key = v1 > v2 ? ((UInt64)v1 << 32 | (UInt64)v2) : ((UInt64)v2 << 32 | (UInt64)v1);

			int ret;
			if (!midpoint.TryGetValue(key, out ret))
			{
				ret = AddVertex(new Vertex(Vertices[v1].x + Vertices[v2].x,
					Vertices[v1].y + Vertices[v2].y,
					Vertices[v1].z + Vertices[v2].z));
				midpoint.Add(key, ret);
			}

			return ret;
		}

		public Vertex this[int index]
		{
			get
			{
//				Vertex a = Vertices[Triangles[index].a];
//				Vertex b = Vertices[Triangles[index].b];
//				Vertex c = Vertices[Triangles[index].c];
//
//				Vertex v = new Vertex((a.x + b.x + c.x) / 3, (a.y + b.y + c.y) / 3, (a.z + b.z + c.z) / 3);
//				double invRadius = Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
//				v.x *= invRadius;
//				v.y *= invRadius;
//				v.z *= invRadius;
//
//				return v;
				return Vertices[index];
			}
		}

		public int Count
		{
			get
			{
				//return Triangles.Count;
				return Vertices.Count;
			}
		}

		public Icosphere(int level)
		{
			Triangles = new List<Triangle>();

			var t = (1.0 + Math.Sqrt(5.0)) / 2.0;

			AddVertex(new Vertex(-1,  t, 0));
			AddVertex(new Vertex( 1,  t, 0));
			AddVertex(new Vertex(-1, -t, 0));
			AddVertex(new Vertex( 1, -t, 0));

			AddVertex(new Vertex(0, -1,  t));
			AddVertex(new Vertex(0,  1,  t));
			AddVertex(new Vertex(0, -1, -t));
			AddVertex(new Vertex(0,  1, -t));

			AddVertex(new Vertex( t, 0, -1));
			AddVertex(new Vertex( t, 0,  1));
			AddVertex(new Vertex(-t, 0, -1));
			AddVertex(new Vertex(-t, 0,  1));

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
				var Triangles2 = new List<Triangle>();
				foreach (var tri in Triangles)
				{
					int a = GetMidPoint(tri.b, tri.c);
					int b = GetMidPoint(tri.a, tri.c);
					int c = GetMidPoint(tri.a, tri.b);

					Triangles2.Add(new Triangle(tri.a, b, c));
					Triangles2.Add(new Triangle(tri.b, a, c));
					Triangles2.Add(new Triangle(tri.c, a, b));
					Triangles2.Add(new Triangle(a, b, c));
				}
				Triangles = Triangles2;
			}
		}
	}
}

*/