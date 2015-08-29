using System;
using System.Linq;
using System.Collections.Generic;

namespace ProceduralCities
{
	public class Pathfinding
	{
		public struct Node
		{
			public int origin;
			public double distance;
			public bool visited;
		}

		Planet planet;
		public readonly Node[] Nodes;

		IEnumerable<int> GetNeighbors(int index)
		{
			for (int i = 0; i < 6; i++)
			{
				if (planet.Edges[index, i] == -1)
					yield break;
				yield return planet.Edges[index, i];
			}
		}

		public Pathfinding(Planet planet, IEnumerable<int> origins)
		{
			this.planet = planet;
			Nodes = new Node[planet.Vertices.Count];

			// Unvisited nodes at the border
			var unvisited = new SortedDictionary<Pair<double, int>, int>();

			for (int i = 0, n = planet.Vertices.Count; i < n; i++)
			{
				Nodes[i].distance = double.MaxValue;
				Nodes[i].visited = false;
			}

			foreach (int i in origins)
			{
				Nodes[i].distance = 0;
				Nodes[i].origin = i;
				unvisited.Add(new Pair<double, int>(0, i), 0);
			}

			while (unvisited.Count > 0)
			{
				Pair<double, int> current = unvisited.First().Key;
				unvisited.Remove(current);

				double currentDistance = current.item1;
				int currentIdx = current.item2;

				// Update distances
				foreach (int j in GetNeighbors(currentIdx))
				{
					if (Nodes[j].visited)
						continue;

					if (planet.Vertices[j].TerrainHeight < 0)
						continue;

					double distance = currentDistance + Coordinates.Distance(planet.Vertices[j].coord, planet.Vertices[currentIdx].coord);
					if (Nodes[j].distance > distance)
					{
						unvisited.Remove(new Pair<double, int>(Nodes[j].distance, j));
						Nodes[j].distance = distance;
						Nodes[j].origin = currentIdx;
						unvisited.Add(new Pair<double, int>(Nodes[j].distance, j), 0);
					}
				}

				Nodes[currentIdx].visited = true;
			}
		}
	}
}

