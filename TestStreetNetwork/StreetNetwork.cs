using System;
using System.Linq;
using System.Collections.Generic;
using Cairo;

namespace ProceduralCities
{
	class StreetNode
	{
		public readonly double x;
		public readonly double y;

		public StreetNode(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public override string ToString()
		{
			return String.Format("StreetNode({0:F3}, {1:F3})", x, y);
		}
	}

	class StreetSegment
	{
		public StreetNode node1;
		public StreetNode node2;
		public int generation;
		public bool stop;
		public int parent;

		public override string ToString()
		{
			return String.Format("StreetSegment({0}-{1})", node1, node2);
		}
	}

	class StreetNetwork
	{
		List<StreetSegment> segments = new List<StreetSegment>();
		SortedDictionary<int, Queue<StreetSegment>> candidates = new SortedDictionary<int, Queue<StreetSegment>>();

		public static double Distance(StreetSegment s, StreetNode n)
		{
			StreetNode n1 = s.node1;
			StreetNode n2 = s.node2;
			double lambda = ((n.x - n1.x) * (n2.x - n1.x) + (n.y - n1.y) * (n2.y - n1.y)) / ((n2.x - n1.x) * (n2.x - n1.x) + (n2.y - n1.y) * (n2.y - n1.y));

			if (lambda < 0)
				lambda = 0;
			else if (lambda > 1)
				lambda = 1;

			return Distance(n, n1.x + lambda * (n2.x - n1.x), n1.y + lambda * (n2.y - n1.y));
		}

		public static double Angle(StreetSegment s1, StreetSegment s2)
		{
			StreetNode n1 = s1.node1;
			StreetNode n2 = s1.node2;
			StreetNode n3 = s2.node1;
			StreetNode n4 = s2.node2;
			double x1 = n1.x - n2.x;
			double x2 = n3.x - n4.x;
			double y1 = n1.y - n2.y;
			double y2 = n3.y - n4.y;
			double dot = (x1 * x2 + y1 * y2) / Math.Sqrt((x1 * x1 + y1 * y1) * (x2 * x2 + y2 * y2));
			return Math.Acos(Math.Abs(dot));
		}

		public static double Distance(StreetNode n1, double x, double y)
		{
			return Math.Sqrt((n1.x - x) * (n1.x - x) + (n1.y - y) * (n1.y - y));
		}

		public static double Distance(StreetNode n1, StreetNode n2)
		{
			return Math.Sqrt((n1.x - n2.x) * (n1.x - n2.x) + (n1.y - n2.y) * (n1.y - n2.y));
		}

		public bool Intersects(StreetSegment s1, StreetSegment s2, out double lambda, out double mu)
		{
			// s1: [n1 n2]
			// s2: [n3 n4]
			// intersection = n1 + lambda * (n2 - n1) = n3 + mu * (n4 - n3)
			// lambda * (n2.x - n1.x) + mu * (n3.x - n4.x) = n3.x - n1.x
			// lambda * (n2.y - n1.y) + mu * (n3.y - n4.y) = n3.y - n1.y

			StreetNode n1 = s1.node1;
			StreetNode n2 = s1.node2;
			StreetNode n3 = s2.node1;
			StreetNode n4 = s2.node2;

			double det = (n2.x - n1.x) * (n3.y - n4.y) - (n2.y - n1.y) * (n3.x - n4.x);
			lambda = ((n3.x - n1.x) * (n3.y - n4.y) - (n3.y - n1.y) * (n3.x - n4.x)) / det;
			mu = ((n2.x - n1.x) * (n3.y - n1.y) - (n2.y - n1.y) * (n3.x - n1.x)) / det;

			return (lambda >= 0 && lambda <= 1 && mu >= 0 && mu <= 1);
		}

		public StreetNode FindClosestNode(double x, double y)
		{
			StreetNode best = null;
			double bestDistance = double.MaxValue;

			foreach(StreetSegment i in segments)
			{
				double distance = Distance(i.node1, x, y);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i.node1;
				}

				distance = Distance(i.node2, x, y);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i.node2;
				}
			}

			return best;
		}

		public StreetNode FindClosestNode(StreetNode node)
		{
			return FindClosestNode(node.x, node.y);
		}

		StreetNode FindOrCreateNode(double x, double y, double tol)
		{
			StreetNode node = FindClosestNode(x, y);

			if (node != null && Distance(node, x, y) < tol)
			{
				return node;
			}
			else
			{
				return new StreetNode(x, y);
			}
		}

		void SplitSegment(StreetSegment s, double lambda, double tol)
		{
			StreetNode middle = FindOrCreateNode(
				                    s.node1.x + lambda * (s.node2.x - s.node1.x),
				                    s.node1.y + lambda * (s.node2.y - s.node1.y),
				                    tol);
			StreetSegment newseg = new StreetSegment();
			newseg.node1 = middle;
			newseg.node2 = s.node2;
			newseg.generation = s.generation;
			segments.Add(newseg);

			s.node2 = middle;
		}

		// Modify the segment and returns true if the segment should be kept
		bool LocalConstraints(ref StreetSegment segment)
		{
			if (segment.generation > 4)
				return false;

			// Stop the segment if it intersects another segment
			double intersecting_lambda = 0;
			double intersecting_mu = 1;
			int intersecting_segment = -1;
			for(int i = 0, n = segments.Count; i < n; i++)
			{
				double lambda, mu;
				if (Intersects(segments[i], segment, out lambda, out mu))
				{
					if (mu < intersecting_mu && mu > 0.0001)
					{
						intersecting_segment = i;
						intersecting_lambda = lambda;
						intersecting_mu = mu;
						segment.stop = true;
					}
				}
			}

			StreetNode closest = FindClosestNode(segment.node2);
			if (Distance(segment.node2, closest) < 5)
			{
				segment.node2 = closest;
				segment.stop = true;
			}

			var ok = Distance(segment.node1, segment.node2) > 5
				&& Distance(segment.node1, new StreetNode(0,0)) < 1000
				&& Distance(segment.node2, new StreetNode(0,0)) < 1000;

			for (int i = 0, n = segments.Count; i < n; i++)
			{
				if ((Distance(segments[i], segment.node1) < 0.1 && Distance(segments[i], segment.node2) < 0.1) && Angle(segments[i], segment) < 0.1 && i != segment.parent)
					ok = false;
			}

			if (intersecting_segment >= 0)
			{
				if (Angle(segment, segments[intersecting_segment]) < Math.PI / 6)
					ok = false;
			}

			// If the current segment intersects another segment, split it
			if (intersecting_segment >= 0 && ok)
			{
				SplitSegment(segments[intersecting_segment], intersecting_lambda, 0.1);
				segment.node2 = FindOrCreateNode(
					segment.node1.x + (segment.node2.x - segment.node1.x) * intersecting_mu,
					segment.node1.y + (segment.node2.y - segment.node1.y) * intersecting_mu,
					0.1);
			}

			return ok;
		}

		Random rnd = new Random();
		// Add candidate segments
		void GlobalGoals(int idx, int t)
		{
			StreetSegment segment = segments[idx];
			if (segment.stop)
				return;

			double x = segment.node2.x;
			double y = segment.node2.y;
			double theta = (rnd.NextDouble() - 0.5) * 0.4;
			double dx = (x - segment.node1.x) * Math.Cos(theta) - (y - segment.node1.y) * Math.Sin(theta);
			double dy = (x - segment.node1.x) * Math.Sin(theta) + (y - segment.node1.y) * Math.Cos(theta);

			// Continue the current road
			StreetSegment seg = new StreetSegment();
			seg.node1 = segment.node2;
			seg.node2 = FindOrCreateNode(x + dx, y + dy, 0.1);
			seg.generation = segment.generation;
			seg.stop = false;
			seg.parent = idx;
			PushCandidate(t + 1, seg);

			// Branch secondary roads
			seg = new StreetSegment();
			seg.node1 = segment.node2;
			seg.node2 = FindOrCreateNode(x + dy, y - dx, 0.1);
			seg.generation = segment.generation + 1;
			seg.stop = false;
			seg.parent = idx;
			PushCandidate(t + 1, seg);

			seg = new StreetSegment();
			seg.node1 = segment.node2;
			seg.node2 = FindOrCreateNode(x - dy, y + dx, 0.1);
			seg.generation = segment.generation + 1;
			seg.stop = false;
			seg.parent = idx;
			PushCandidate(t + 1, seg);
		}

		double scale = 0.3;
		int width = 800;
		int height = 800;

		public StreetNetwork()
		{
		}

		bool PopCandidate(out StreetSegment segment, out int t)
		{
			while (candidates.Count > 0)
			{
				var tmp = candidates.First(); // TODO: vérifier si O(log n) ?
				if (tmp.Value.Count > 0)
				{
					t = tmp.Key;
					segment = tmp.Value.Dequeue();
					return true;
				}

				candidates.Remove(tmp.Key);
			}

			t = 0;
			segment = null;
			return false;
		}

		void PushCandidate(int t, StreetSegment segment)
		{
			if (!candidates.ContainsKey(t))
				candidates.Add(t, new Queue<StreetSegment>());

			candidates[t].Enqueue(segment);
		}

		public void AddRoad(double x1, double y1, double x2, double y2)
		{
			StreetSegment seg = new StreetSegment();
			seg.node1 = FindOrCreateNode(x1, y1, 1);
			seg.node2 = FindOrCreateNode(x2, y2, 1);
			seg.generation = 0;
			seg.stop = false;
			seg.parent = -1;

			segments.Add(seg);
			GlobalGoals(segments.Count - 1, 0);
		}

		public void BuildRoads()
		{
			StreetSegment seg;
			int t;
			while (PopCandidate(out seg, out t) && segments.Count < 1000)
			{
				if (LocalConstraints(ref seg))
				{
					segments.Add(seg);
					GlobalGoals(segments.Count - 1, t);
				}
			}
		}

		/*public void DrawPopulation(string filename)
		{
			byte[] data = new byte[width * height * 4];
			Palette palette = Palette.PopulationMap();

			double minValue = double.MaxValue;
			double maxValue = double.MinValue;

			using (var surface = new ImageSurface(data, Format.RGB24, width, height, width * 4))
			{
				for (int i = 0, index = 0; i < width; i++)
				{
					for (int j = 0; j < height; j++, index += 4)
					{
						double value = GetPopulation(i * scale, j * scale) / 5;
						minValue = Math.Min(value, minValue);
						maxValue = Math.Max(value, maxValue);
						palette.Plot(data, index, value);
					}
				}

				surface.WriteToPng(filename);
				Console.WriteLine("Min value: {0}", minValue);
				Console.WriteLine("Max value: {0}", maxValue);
			}
		}*/

		/*public void DrawHeightMap(string filename)
		{
			byte[] data = new byte[width * height * 4];
			Palette palette = Palette.HeightMap();

			double minValue = double.MaxValue;
			double maxValue = double.MinValue;

			using (var surface = new ImageSurface(data, Format.RGB24, width, height, width * 4))
			{
				for (int i = 0, index = 0; i < width; i++)
				{
					for (int j = 0; j < height; j++, index += 4)
					{
						double value = GetHeight(i * scale, j * scale) / 8;
						minValue = Math.Min(value, minValue);
						maxValue = Math.Max(value, maxValue);
						if (value <= 0)
						{
							data[index] = 255;
							data[index + 1] = 0;
							data[index + 2] = 0;
						}
						else
						{
							palette.Plot(data, index, value);
						}
					}
				}

				surface.WriteToPng(filename);
				Console.WriteLine("Min value: {0}", minValue);
				Console.WriteLine("Max value: {0}", maxValue);
			}
		}*/

		public void DrawRoadNetwork(string filename)
		{
			Console.WriteLine("{0} segments", segments.Count);
			using (var surface = new ImageSurface(Format.RGB24, width, height))
			{
				using (var ctx = new Context(surface))
				{
					ctx.SetSourceColor(new Color(1, 1, 1));
					ctx.MoveTo(0, 0);
					ctx.LineTo(width, 0);
					ctx.LineTo(width, height);
					ctx.LineTo(0, height);
					ctx.Fill();

					ctx.LineWidth = 2;
					foreach (StreetSegment s in segments)
					{
						switch (s.generation)
						{
						case 0:
							ctx.SetSourceColor(new Color(1, 0, 0));
							break;

						case 1:
							ctx.SetSourceColor(new Color(0, 0, 1));
							break;

						case 2:
							ctx.SetSourceColor(new Color(0, 1, 0));
							break;

						default:
							ctx.SetSourceColor(new Color(1, 1, 0));
							break;

						}

						StreetNode n1 = s.node1;
						StreetNode n2 = s.node2;
						ctx.MoveTo(n1.x / scale + width / 2, n1.y / scale + height / 2);
						ctx.LineTo(n2.x / scale + width / 2, n2.y / scale + height / 2);
						ctx.Stroke();

						Console.WriteLine("{0};{1};{2};{3};{4}", n1.x, n1.y, n2.x, n2.y, s.generation);
					}
				}
				surface.WriteToPng(filename);
			}
		}
	}

	class TestStreetNetwork
	{
		public static void Main(string[] args)
		{
			var net = new StreetNetwork();
			net.AddRoad(0, 0, 0, 10);
			net.AddRoad(0, 0, 0, -10);
			net.AddRoad(0, 0, 10, 0);
			net.AddRoad(0, 0, -10, 0);
			net.BuildRoads();
			net.DrawRoadNetwork("Roads.png");
			//			net.DrawPopulation("Population.png");
			//			net.DrawHeightMap("Map.png");
			//			net.DrawRoadTensor("RoadTensor.png");

			//			for (double x = 0.1; x < 2.5; x += 0.2)
			//			{
			//				for (double y = 0.1; y < 2.5; y += 0.2)
			//				{
			//					net.BuildRoad(x, y, true);
			//				}
			//			}

			//			net.DrawRoadNetwork("Roads.png");
		}
	}
}
