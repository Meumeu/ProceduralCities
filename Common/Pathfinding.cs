using System;
using System.Linq;
using System.Collections.Generic;

namespace ProceduralCities
{
	struct Queue
	{
		class Item
		{
			public readonly double score;
			public readonly int vertex;
			public Item next;
			public Item(double score, int vertex, Item next)
			{
				this.score = score;
				this.vertex = vertex;
				this.next = next;
			}
		}

		Item list;

		void InsertAfter(Item prev, double score, int vertex)
		{
			if (prev == null)
				list = new Item(score, vertex, list);
			else
				prev.next = new Item(score, vertex, prev.next);
		}

		public void Add(double score, int vertex)
		{
			Item prev = null;
			for (var item = list; item != null; prev = item, item = item.next)
			{
				if (item.score > score || (item.score == score && item.vertex > vertex))
				{
					InsertAfter(prev, score, vertex);
					return;
				}
			}
			InsertAfter(prev, score, vertex);
		}

		public void ChangeScore(double oldScore, double newScore, int vertex)
		{
			Add(newScore, vertex);
		}

		public int GetBest()
		{
			return list.vertex;
		}

		public int RemoveBest()
		{
			var res = list.vertex;
			list = list.next;
			return res;
		}

		public bool Empty { get { return list == null; }}
	}

	public class Pathfinding
	{
		public struct Node
		{
			public int next;
			public double f_score;
			public double g_score;
			public bool visited;
		}

		Planet planet;
		public readonly Node[] Nodes;

		IEnumerable<int> GetNeighbors(int index)
		{
			for (int i = 0, n = planet.Edges.GetLength(1); i < n && planet.Edges[index, i] >= 0; i++)
			{
				yield return planet.Edges[index, i];
			}
		}

		double EdgeCost(int node1, int node2)
		{
			for (int i = 0, n = planet.Edges.GetLength(1); i < n && planet.Edges[node1, i] >= 0; i++)
			{
				if (planet.Edges[node1, i] == node2)
					return planet.EdgeCost[node1, i];
			}

			throw new InvalidOperationException("unreachable");
		}

		double Heuristic(int node1, int node2, double radius)
		{
			return Coordinates.Distance(planet.Vertices[node1].coord, planet.Vertices[node2].coord) * radius;
		}

		double Distance(int node1, int node2, double radius, double TerrainCost)
		{
			return Coordinates.Distance(planet.Vertices[node1].coord, planet.Vertices[node2].coord) * radius + EdgeCost(node1, node2) * TerrainCost;
		}

		static Int64 stat_nbAstar = 0;
		static Int64 stat_timeAstar = 0;

		public static string Stats()
		{
			return string.Format("Pathfinder stats: number of calls: {0}, average time: {1} ms", stat_nbAstar, (((double)stat_timeAstar) / ((double)stat_nbAstar)));
		}

		void AStar(int target, int origin, double TerrainCost)
		{
			double radius = planet.Radius;

			// Unvisited nodes at the border
			var open_set = new Queue();

			for (int i = 0, n = planet.Vertices.Count; i < n; i++)
			{
				Nodes[i].g_score = double.MaxValue;
				Nodes[i].visited = false;
			}

			Nodes[target].f_score = Heuristic(target, origin, radius);
			Nodes[target].g_score = 0;
			Nodes[target].next = target;
			open_set.Add(Nodes[target].f_score, target);

			while (!open_set.Empty)
			{
				int currentIdx = open_set.RemoveBest();

				// Update distances
				foreach (int j in GetNeighbors(currentIdx))
				{
					if (Nodes[j].visited)
						continue;

					if (planet.Vertices[j].TerrainHeight < 0)
						continue;

					double edgeDistance = Distance(j, currentIdx, radius, TerrainCost);
					double distance = Nodes[currentIdx].g_score + edgeDistance;
					if (Nodes[j].g_score > distance)
					{
						double oldScore = Nodes[j].f_score;
						Nodes[j].f_score = distance + Heuristic(j, origin, radius);
						Nodes[j].g_score = distance;
						Nodes[j].next = currentIdx;
						open_set.ChangeScore(oldScore, Nodes[j].f_score, j);
					}
				}

				Nodes[currentIdx].visited = true;

				if (currentIdx == origin)
					return;
			}
		}

		void Dijkstra(IEnumerable<int> targets, double TerrainCost)
		{
			double radius = planet.Radius;

			// Unvisited nodes at the border
			var unvisited = new Queue();

			for (int i = 0, n = planet.Vertices.Count; i < n; i++)
			{
				Nodes[i].g_score = double.MaxValue;
				Nodes[i].visited = false;
			}

			foreach (int i in targets)
			{
				Nodes[i].g_score = 0;
				Nodes[i].next = i;
				unvisited.Add(0, i);
			}

			while (!unvisited.Empty)
			{
				int currentIdx = unvisited.RemoveBest();

				double currentDistance = Nodes[currentIdx].g_score;


				// Update distances
				foreach (int j in GetNeighbors(currentIdx))
				{
					if (Nodes[j].visited)
						continue;

					if (planet.Vertices[j].TerrainHeight < 0)
						continue;

					double edgeDistance = Distance(j, currentIdx, radius, TerrainCost);
					double distance = currentDistance + edgeDistance;
					if (Nodes[j].g_score > distance)
					{
						double oldScore = Nodes[j].g_score;
						Nodes[j].g_score = distance;
						Nodes[j].next = currentIdx;
						unvisited.ChangeScore(oldScore, Nodes[j].g_score, j);
					}
				}

				Nodes[currentIdx].visited = true;
			}
		}

		public Pathfinding(Planet planet, IEnumerable<int> targets, int origin = -1)
		{
			this.planet = planet;
			Nodes = new Node[planet.Vertices.Count];

			Dijkstra(targets, 0);
		}

		public Pathfinding(Planet planet, int target, int origin)
		{
			this.planet = planet;
			Nodes = new Node[planet.Vertices.Count];
			var watch = System.Diagnostics.Stopwatch.StartNew();
			AStar(target, origin, 1000);
			stat_nbAstar++;
			stat_timeAstar += watch.ElapsedMilliseconds;
		}

		public IEnumerable<int> GetPath(int from)
		{
			while (Nodes[from].next != from)
			{
				yield return from;
				from = Nodes[from].next;
			}
			yield return from;
		}

		public Pathfinding(System.IO.BinaryReader reader)
		{
			int n = reader.ReadInt32();
			Nodes = new Node[n];
			for (int i = 0; i < n; i++)
			{
				Nodes[i].next = reader.ReadInt32();
				Nodes[i].f_score = reader.ReadDouble();
				Nodes[i].g_score = reader.ReadDouble();
				Nodes[i].visited = reader.ReadBoolean();
			}
		}

		public void Write(System.IO.BinaryWriter writer)
		{
			writer.Write(Nodes.Count());
			for (int i = 0, n = Nodes.Count(); i < n; i++)
			{
				writer.Write(Nodes[i].next);
				writer.Write(Nodes[i].f_score);
				writer.Write(Nodes[i].g_score);
				writer.Write(Nodes[i].visited);
			}
		}
	}
}

