﻿using System;
using System.Linq;
using System.Diagnostics;
using Cairo;

namespace ProceduralCities
{
	class MainProgram
	{
		static UInt32 LerpColor(double lambda, UInt32 color0, UInt32 color1)
		{
			if (lambda < 0)
				lambda = 0;
			else if (lambda > 1)
				lambda = 1;
			
			uint r0 = color0 >> 24;
			uint g0 = (color0 >> 16) & 0xff;
			uint b0 = color0 & 0xff;

			uint r1 = color1 >> 24;
			uint g1 = (color1 >> 16) & 0xff;
			uint b1 = color1 & 0xff;

			uint r = (uint)((1 - lambda) * r0 + lambda * r1);
			uint g = (uint)((1 - lambda) * g0 + lambda * g1);
			uint b = (uint)((1 - lambda) * b0 + lambda * b1);

			Debug.Assert(r <= 255);
			Debug.Assert(g <= 255);
			Debug.Assert(b <= 255);

			return (r << 16) | (g << 8) | b;
		}

		static UInt32 TerrainColor(double alt, UInt32[] AltitudePalette)
		{
			if (alt < 0)
				return 0x0978AB;

			int idx = (int)Math.Floor(Math.Max(0, Math.Min(alt / 300, 18)));

			return LerpColor((alt - idx * 300.0) / 300.0, AltitudePalette[idx], AltitudePalette[idx + 1]);
		}

		static ImageSurface CreateMap(out byte[] data, TestPlanet p, int width = 4096, int height = 2048)
		{
			data = new byte[width * height * 4];
			UInt32[] AltitudePalette = new UInt32[] {
				0xA7DFD2,
				0xACD0A5,
				0x94BF8B,
				0xA8C68F,
				0xBDCC96,
				0xD1D7AB,
				0xE1E4B5,
				0xEFEBC0,
				0xE8E1B6,
				0xDED6A3,
				0xD3CA9D,
				0xCAB982,
				0xC3A76B,
				0xB9985A,
				0xAA8753,
				0xAC9A7C,
				0xBAAE9A,
				0xCAC3B8,
				0xE0DED8,
				0xF5F4F2
			};

			for (int i = 0, index = 0; i < height; i++)
			{
				double lat = ((double)i / (double)height) * Math.PI - Math.PI / 2;
				for (int j = 0; j < width; j++, index += 4)
				{
					double lon = ((double)j / (double)width) * 2 * Math.PI - Math.PI;
					double grad_x, grad_y;

					double alt = p.GetTerrainHeight(-lat, lon);
					// p.GetTerrainGradient(-lat, lon, out grad_x, out grad_y);

					UInt32 c = TerrainColor(alt, AltitudePalette);
					data[index + 0] = (byte)(c & 0xff);
					data[index + 1] = (byte)((c >> 8) & 0xff);
					data[index + 2] = (byte)((c >> 16) & 0xff);
				}
			}

			return new ImageSurface(data, Format.RGB24, width, height, width * 4);
		}

		static void PrintMaps(TestPlanet p, string filename)
		{
			byte[] data;
			using (var surface = CreateMap(out data, p))
			{
				surface.WriteToPng(filename);
			}
		}

		static void DrawEdge(Context ctx, Coordinates v1, Coordinates v2, int w, int h, bool arrow)
		{
			double x1 = (v1.Longitude + Math.PI) * w / (2 * Math.PI);
			double y1 = (-v1.Latitude + Math.PI / 2) * h / Math.PI;
			double x2 = (v2.Longitude + Math.PI) * w / (2 * Math.PI);
			double y2 = (-v2.Latitude + Math.PI / 2) * h / Math.PI;

			double dx = x2 - x1;
			double dy = y2 - y1;
			double d = Math.Sqrt(dx * dx + dy * dy);

			if (Math.Abs(dx) < w / 2)
			{
				ctx.MoveTo(x1, y1);
				ctx.LineTo(x2, y2);

				if (arrow && d > 0)
				{
					ctx.MoveTo(x2 - dx / d * 5 + dy / d * 5, y2 - dx / d * 5 - dy / d * 5);
					ctx.LineTo(x2, y2);
					ctx.LineTo(x2 - dx / d * 5 - dy / d * 5, y2 + dx / d * 5 - dy / d * 5);
				}
				ctx.Stroke();
			}
		}

		public static void Main(string[] args)
		{
			TestPlanet p = new TestPlanet("Kerbin.dat");

			Console.WriteLine("Printing map");
			//PrintMaps(p, "kerbin.png");

			//int w = 4096, h = 2048;
			int w = 8000, h = 4000;
			byte[] data;
			using (var surface = CreateMap(out data, p, w, h))
			{
				using (var ctx = new Context(surface))
				{
					ctx.SetSourceRGBA(0.5, 0.7, 0, 0.5);

					for(int i = 0, n = p.Vertices.Count; i < n; i++)
					{
						double x = (p.Vertices[i].coord.Longitude + Math.PI) * w / (2 * Math.PI);
						double y = (-p.Vertices[i].coord.Latitude + Math.PI / 2) * h / Math.PI;

						ctx.Arc(x, y, 1, 0, 2 * Math.PI);
						ctx.Fill();

						if (p.Vertices[i].NextVertex != -1) //(p.PathToNearestCity.Nodes[i].visited)
						{
							Planet.Vertex org = p.Vertices[p.Vertices[i].NextVertex];
							DrawEdge(ctx, p.Vertices[i].coord, org.coord, w, h, true);
						}
//
//						if (p.PathToOcean.Nodes[i].visited)
//						{
//							Planet.Vertex org = p.Vertices[p.PathToOcean.Nodes[i].origin];
//							DrawEdge(ctx, p.Vertices[i].coord, org.coord, w, h);
//						}
//
//						/*for (int j = 0; j < 6; j++)
//						{
//							if (p.Edges[i, j] == -1)
//								break;
//
//							DrawEdge(ctx, p.Vertices[i].coord, p.Vertices[p.Edges[i, j]].coord, w, h);
//						}*/
					}

					ctx.SetSourceRGBA(0.8, 0, 0, 0.5);
					foreach(var i in p.Roads)
					{
						Coordinates? last = null;
						//foreach (Coordinates j in i.Select(x => p.Vertices[x].coord))
						var road = new Bezier(i.Select(x => p.Vertices[x].coord).ToList(), 600000);
						foreach (Coordinates j in road.Rasterize(1000))
						{
							if (last.HasValue)
								DrawEdge(ctx, last.Value, j, w, h, false);

							last = j;

//							double x = (j.Longitude + Math.PI) * w / (2 * Math.PI);
//							double y = (-j.Latitude + Math.PI / 2) * h / Math.PI;
//
//							ctx.Arc(x, y, 1, 0, 2 * Math.PI);
//							ctx.Fill();
						}
					}

					ctx.SetSourceColor(new Color(0, 0, 0));
					foreach (Planet.City i in p.Cities)
					{
						Coordinates pos = p.Vertices[i.Position].coord;
						double x = (pos.Longitude + Math.PI) * w / (2 * Math.PI);
						double y = (-pos.Latitude + Math.PI / 2) * h / Math.PI;

						ctx.Arc(x, y, 3, 0, 2 * Math.PI);
						ctx.Fill();
					}
				}

				surface.WriteToPng("kerbin-cities.png");
			}

			/*var vertices = new Icosphere(8).GetCoordinates().ToList();
			var noise = new SimplexNoise(123, 0.5, 2, 1, 1);
			Console.WriteLine("vertices: {0}", vertices.Count);

			double[] simplexNoise = new double[vertices.Count];
			double sumX = 0;
			double sumX2 = 0;
			for(int i = 0; i < vertices.Count; i++)
			{
				simplexNoise[i] = noise.Generate(vertices[i].x, vertices[i].y, vertices[i].z, 6);
				sumX += simplexNoise[i];
				sumX2 += simplexNoise[i] * simplexNoise[i];
			}

			double meanX = sumX / vertices.Count;
			double meanX2 = sumX2 / vertices.Count;
			double variance = meanX2 - meanX * meanX;
			double std = Math.Sqrt(variance);
			Console.WriteLine("Mean: {0}, std: {1}", meanX, std);

			int[] nb = new int[200];

			for (int i = 0; i < vertices.Count; i++)
			{
				int idx = (int)((simplexNoise[i] - meanX) / std * 25 + 100);

				if (idx >= 0 && idx < 200)
					nb[idx]++;
			}

			for (int i = 0; i < 200; i++)
			{
				Console.WriteLine(nb[i]);
			}*/
		}
	}
}
