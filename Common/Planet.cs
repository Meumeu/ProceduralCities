﻿using System;
using System.Collections.Generic;

namespace ProceduralCities
{
	public abstract class Planet
	{
		public struct Vertex
		{
			public Coordinates coord;
			public int Biome;
			public double TerrainHeight;

			// Dijkstra stuff
			public int origin;
			public int cost;

			// City generation stuff
			public double score;

			public Vertex(Coordinates c)
			{
				coord = c;
				Biome = 0;
				TerrainHeight = 0;
				origin = 0;
				cost = 0;
				score = 0;
			}
		}

		public class Biome
		{
			public string name;
			public double desirability;
		}

		public List<Vertex> Vertices;
		public List<Biome> Biomes;
		public int[,] Edges;

		#region Icosphere building
		// http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
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
		List<Triangle> Triangles;

		int AddVertex(Coordinates v)
		{
			Vertices.Add(new Vertex(v));
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

		protected void BuildIcosphere(int level)
		{
			System.Diagnostics.Debug.Assert(Vertices == null);
			System.Diagnostics.Debug.Assert(Triangles == null);
			var watch = System.Diagnostics.Stopwatch.StartNew();

			Vertices = new List<Vertex>();
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

			int nb_edges = 0;
			for (int i = 0; i < Vertices.Count; i++)
			{
				for (int j = 0; j < 6; j++)
					if (Edges[i, j] >= 0)
						nb_edges++;
			}

			Console.WriteLine("{0} triangles, {1} vertices, {2} edges, elapsed: {3}", Triangles.Count, Vertices.Count, nb_edges / 2, watch.Elapsed);
		}
		#endregion

		#region Dijkstra
		protected void Dijkstra()
		{
			
		}
		#endregion


		#region Build major cities
		#endregion

		#region Build major roads
		#endregion

		#region To be implemented by derived classes
		protected abstract void FillTerrainAndBiome();
		#endregion
	}
}