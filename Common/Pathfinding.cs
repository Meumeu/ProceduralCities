using System;
using System.Linq;
using System.Collections.Generic;

namespace ProceduralCities
{
	struct Queue
	{
		SortedDictionary<Pair<double, int>, int> data;

		public void Add(double score, int vertex)
		{
			data.Add(new Pair<double, int>(score, vertex), 0);

			if (data.Count == 0 || data.First().Key.item1 >= score)
				Pathfinding.stat_insertionAtBeginning++;

			Pathfinding.stat_insertionCount++;
			Pathfinding.stat_openSetSizeSum += data.Count;
			Pathfinding.stat_openSetSize_nb++;
			Pathfinding.stat_openSetSizeMax = Math.Max(Pathfinding.stat_openSetSizeMax, data.Count);
		}

		public void ChangeScore(double oldScore, double newScore, int vertex)
		{
			data.Remove(new Pair<double, int>(oldScore, vertex));
			data.Add(new Pair<double, int>(newScore, vertex), 0);

			if (data.Count == 0 || data.First().Key.item1 >= newScore)
				Pathfinding.stat_insertionAtBeginning++;

			Pathfinding.stat_insertionCount++;
			Pathfinding.stat_openSetSizeSum += data.Count;
			Pathfinding.stat_openSetSize_nb++;
			Pathfinding.stat_openSetSizeMax = Math.Max(Pathfinding.stat_openSetSizeMax, data.Count);
		}

		public Pair<double, int> GetBest()
		{
			return data.First().Key;
		}

		public Pair<double, int> RemoveBest()
		{
			var ret = data.First().Key;
			data.Remove(ret);
			return ret;
		}

		public bool IsEmpty()
		{
			return data.Count == 0;
		}

		public Queue()
		{
			data = new SortedDictionary<Pair<double, int>, int>();
		}
	}

	[Serializable]
	public class Pathfinding
	{
		[Serializable]
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

		public static Int64 stat_openSetSizeSum = 0;
		public static Int64 stat_openSetSize_nb = 0;
		public static Int64 stat_openSetSizeMax = 0;
		public static Int64 stat_insertionCount = 0;
		public static Int64 stat_insertionAtBeginning = 0;
		public static Int64 stat_nbAstar = 0;
		public static Int64 stat_timeAstar = 0;

		public static void PrintStats()
		{
			Console.WriteLine("Average open set size: " + (((double)stat_openSetSizeSum) / ((double)stat_openSetSize_nb)));
			Console.WriteLine("Average time: " + (((double)stat_timeAstar) / ((double)stat_nbAstar)));
			Console.WriteLine("Max openset size: " + stat_openSetSizeMax);
			Console.WriteLine("Insertions at beginning: " + stat_insertionAtBeginning + " / " + stat_insertionCount);
		}

		void AStar(int target, int origin, double TerrainCost)
		{
			double radius = planet.Radius();

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

			while (!open_set.IsEmpty())
			{
				Pair<double, int> current = open_set.RemoveBest();

				int currentIdx = current.item2;

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
			double radius = planet.Radius();

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

			while (!unvisited.IsEmpty())
			{
				Pair<double, int> current = unvisited.RemoveBest();

				double currentDistance = current.item1;
				int currentIdx = current.item2;

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
	}
}

