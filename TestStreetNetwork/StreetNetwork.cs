using System;
using System.Collections.Generic;
using Cairo;

namespace ProceduralCities
{
	class TestStreetNetwork
	{
		public static void Main(string[] args)
		{
			var net = new StreetNetwork();
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

	struct StreetNode
	{
		public double x;
		public double y;
		public override string ToString()
		{
			return String.Format("StreetNode({0:F3}, {1:F3})", x, y);
		}

		public static double Distance(StreetNode n1, double x, double y)
		{
			return Math.Sqrt((n1.x - x) * (n1.x - x) + (n1.y - y) * (n1.y - y));
		}

		public static double Distance(StreetNode n1, StreetNode n2)
		{
			return Math.Sqrt((n1.x - n2.x) * (n1.x - n2.x) + (n1.y - n2.y) * (n1.y - n2.y));
		}
	}

	struct StreetSegment
	{
		public int node1;
		public int node2;
		public int generation;
	}

	class StreetNetwork
	{
		List<StreetNode> nodes = new List<StreetNode>();
		List<StreetSegment> streets = new List<StreetSegment>();

		public double Distance(StreetSegment s, StreetNode n)
		{
			StreetNode n1 = nodes[s.node1];
			StreetNode n2 = nodes[s.node2];
			double lambda = ((n.x - n1.x) * (n2.x - n1.x) + (n.y - n1.y) * (n2.y - n1.y)) / ((n2.x - n1.x) * (n2.x - n1.x) + (n2.y - n1.y) * (n2.y - n1.y));

			if (lambda < 0)
				lambda = 0;
			else if (lambda > 1)
				lambda = 1;

			return StreetNode.Distance(n, n1.x + lambda * (n2.x - n1.x), n1.y + lambda * (n2.y - n1.y));
		}

		public int FindClosestNode(double x, double y)
		{
			int best = -1;
			double bestDistance = double.MaxValue;

			for (int i = 0; i < nodes.Count; i++)
			{
				double distance = Distance(nodes[i], x, y);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i;
				}
			}

			return best;
		}

		public int FindClosestNode(StreetNode node)
		{
			return FindClosestNode(node.x, node.y);
		}

		double scale = 0.003;
		int width = 800;
		int height = 800;

		public StreetNetwork()
		{
		}

//		public void Add

		public void BuildRoads()
		{
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

		/*public void DrawRoadTensor(string filename)
		{
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

					ctx.SetSourceColor(new Color(0, 0, 0));
					ctx.LineWidth = 2;
					for (int i = 20; i < width; i += 40)
					{
						for (int j = 20; j < height; j += 40)
						{
							double x, y;
							GetTensor(i * scale, j * scale, out x, out y);
							double theta = Math.Atan2(y, x) / 2;

							ctx.MoveTo(i, j);
							ctx.RelLineTo(20 * Math.Cos(theta), 20 * Math.Sin(theta));
							ctx.RelLineTo(5 * Math.Cos(theta + 3 * Math.PI / 4), 5 * Math.Sin(theta + 3 * Math.PI / 4));
							ctx.Stroke();

							ctx.MoveTo(i + 20 * Math.Cos(theta), j + 20 * Math.Sin(theta));
							ctx.RelLineTo(5 * Math.Cos(theta - 3 * Math.PI / 4), 5 * Math.Sin(theta - 3 * Math.PI / 4));
							ctx.Stroke();
						}
					}
				}
				surface.WriteToPng(filename);
			}
		}*/

		/*public void DrawRoadNetwork(string filename)
		{
			Console.WriteLine("{0} nodes, {1} segments", nodes.Count, streets.Count);
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

					ctx.SetSourceColor(new Color(1, 1, 0));
					ctx.LineWidth = 2;
					foreach (Street s in streets)
					{
						if (!s.major)
						{
							StreetNode n1 = nodes[s.node1];
							StreetNode n2 = nodes[s.node2];
							ctx.MoveTo(n1.x / scale, n1.y / scale);
							ctx.LineTo(n2.x / scale, n2.y / scale);
						}
					}
					ctx.Stroke();

					ctx.SetSourceColor(new Color(1, 0, 0));
					ctx.LineWidth = 3;
					foreach (Street s in streets)
					{
						if (s.major)
						{
							StreetNode n1 = nodes[s.node1];
							StreetNode n2 = nodes[s.node2];
							ctx.MoveTo(n1.x / scale, n1.y / scale);
							ctx.LineTo(n2.x / scale, n2.y / scale);
						}
					}
					ctx.Stroke();
				}
				surface.WriteToPng(filename);
			}
		}*/

		/*public StreetNode NextStreetNode(StreetNode node, bool major)
		{
			double x1, y1;

			double Tx, Ty;
			GetTensor(node.x, node.y, out Tx, out Ty);
			double theta = Math.Atan2(Ty, Tx) / 2 + (major ? 0 : Math.PI / 2);
			double dx1 = Math.Cos(theta);
			double dy1 = Math.Sin(theta);

			x1 = node.x + dx1;
			y1 = node.y + dy1;

			GetTensor(x1, y1, out Tx, out Ty);
			theta = Math.Atan2(Ty, Tx) / 2 + (major ? 0 : Math.PI / 2);
			double dx2 = Math.Cos(theta);
			double dy2 = Math.Sin(theta);

			StreetNode ret = new StreetNode();
			double dx = (dx1 + dx2) / 2;
			double dy = (dy1 + dy2) / 2;
			double d = Math.Sqrt(dx * dx + dy * dy);


			ret.x = node.x + 0.05*dx/d;
			ret.y = node.y + 0.05*dy/d;

			return ret;
		}*/

		/*public void BuildRoad(double x0, double y0, bool major)
		{
			int idx = FindClosestNode(x0, y0);

			if (idx == -1 || Distance(nodes[idx], x0, y0) > 0.01)
			{
				idx = nodes.Count;
				StreetNode node = new StreetNode();
				node.x = x0;
				node.y = y0;
				nodes.Add(node);
				Console.WriteLine("Starting road at new node {0}", node);
			}
			else
			{
				Console.WriteLine("Starting road at existing node {0}", nodes[idx]);
			}

			BuildRoad(idx, major);
		}*/

		/*public void BuildRoad(int initialNode, bool major)
		{
			Street s;
			while (true)
			{
				StreetNode next = NextStreetNode(nodes[initialNode], major);
				if (next.x < 0 || next.x > width * scale)
					return;
				
				if (next.y < 0 || next.y > height * scale)
					return;

				int closestNode = FindClosestNode(next);
				if (closestNode >= 0 && Distance(next, nodes[closestNode]) < 0.04)
				{
					s = new Street();
					s.node1 = initialNode;
					s.node2 = closestNode;
					s.major = major;
					streets.Add(s);
					Console.WriteLine("Finished on existing node {0}", next);
					return;
				}

				int newNodeIdx = nodes.Count;
				nodes.Add(next);
				Console.WriteLine("Added new node {0}", next);

				s = new Street();
				s.node1 = initialNode;
				s.node2 = newNodeIdx;
				s.major = major;
				streets.Add(s);

				initialNode = newNodeIdx;
			}
		}*/
	}
}
