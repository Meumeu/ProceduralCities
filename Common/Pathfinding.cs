using System;
using System.Linq;
using System.Collections.Generic;

namespace ProceduralCities
{
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

		IEnumerable<int> GetNeighbors(int index, int level = 0)
		{
			if (level == 0)
			{
				for (int i = 0; i < planet.Edges.GetLength(1); i++)
				{
					if (planet.Edges[index, i] == -1)
						yield break;
					yield return planet.Edges[index, i];
				}
			}
			else
			{
				for (int i = 0; i < planet.Edges.GetLength(1); i++)
				{
					if (planet.Edges[index, i] == -1)
						yield break;

					if (planet.Vertices[planet.Edges[index, i]].TerrainHeight < 0)
						continue;

					foreach(int j in GetNeighbors(planet.Edges[index, i], level - 1))
						yield return j;
				}
			}
		}

		double EdgeCost(int node1, int node2)
		{
			for (int i = 0, n = planet.Edges.GetLength(1); i < n && planet.Edges[node1, i] >= 0; i++)
			{
				if (planet.Edges[node1, i] == node2)
				{
					return planet.EdgeCost[node1, i];
				}
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

		void AStar(int target, int origin, double TerrainCost)
		{
			double radius = planet.Radius();

			// Unvisited nodes at the border
			var open_set = new SortedDictionary<Pair<double, int>, int>();

			for (int i = 0, n = planet.Vertices.Count; i < n; i++)
			{
				Nodes[i].g_score = double.MaxValue;
				Nodes[i].visited = false;
			}

			Nodes[target].f_score = Heuristic(target, origin, radius);
			Nodes[target].g_score = 0;
			Nodes[target].next = target;
			open_set.Add(new Pair<double, int>(Nodes[target].f_score, target), 0);

			while (open_set.Count > 0)
			{
				Pair<double, int> current = open_set.First().Key;
				open_set.Remove(current);

				int currentIdx = current.item2;

				// Update distances
				foreach (int j in GetNeighbors(currentIdx, 0))
				{
					if (Nodes[j].visited)
						continue;

					if (planet.Vertices[j].TerrainHeight < 0)
						continue;

					double edgeDistance = Distance(j, currentIdx, radius, TerrainCost);
					double distance = Nodes[currentIdx].g_score + edgeDistance;
					if (Nodes[j].g_score > distance)
					{
						open_set.Remove(new Pair<double, int>(Nodes[j].f_score, j));
						Nodes[j].f_score = distance + Heuristic(j, origin, radius);
						Nodes[j].g_score = distance;
						Nodes[j].next = currentIdx;
						open_set.Add(new Pair<double, int>(Nodes[j].f_score, j), 0);
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
			var unvisited = new SortedDictionary<Pair<double, int>, int>();

			for (int i = 0, n = planet.Vertices.Count; i < n; i++)
			{
				Nodes[i].g_score = double.MaxValue;
				Nodes[i].visited = false;
			}

			foreach (int i in targets)
			{
				Nodes[i].g_score = 0;
				Nodes[i].next = i;
				unvisited.Add(new Pair<double, int>(0, i), 0);
			}

			while (unvisited.Count > 0)
			{
				Pair<double, int> current = unvisited.First().Key;
				unvisited.Remove(current);

				double currentDistance = current.item1;
				int currentIdx = current.item2;

				// Update distances
				foreach (int j in GetNeighbors(currentIdx, 0))
				{
					if (Nodes[j].visited)
						continue;

					if (planet.Vertices[j].TerrainHeight < 0)
						continue;

					double edgeDistance = Distance(j, currentIdx, radius, TerrainCost);
					double distance = currentDistance + edgeDistance;
					if (Nodes[j].g_score > distance)
					{
						unvisited.Remove(new Pair<double, int>(Nodes[j].g_score, j));
						Nodes[j].g_score = distance;
						Nodes[j].next = currentIdx;
						unvisited.Add(new Pair<double, int>(Nodes[j].g_score, j), 0);
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
			AStar(target, origin, 1000);
//			Dijkstra(new[]{target}, 1000);
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

