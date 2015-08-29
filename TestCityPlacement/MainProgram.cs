using System;
using System.Diagnostics;
using Cairo;

namespace ProceduralCities
{
	class MainProgram
	{
		static void TestIcosphere()
		{
			/*double radius = 600;
			Icosphere sphere = new Icosphere(8);
			Console.WriteLine("{0} triangles", sphere.Triangles.Count);

			double area = 0;
			foreach(var tri in sphere.Triangles)
			{
				Icosphere.Vertex v1 = sphere.Vertices[tri.a];
				Icosphere.Vertex v2 = sphere.Vertices[tri.b];
				Icosphere.Vertex v3 = sphere.Vertices[tri.c];

				Icosphere.Vertex u = new Icosphere.Vertex(v2.x - v1.x, v2.y - v1.y, v2.z - v1.z);
				Icosphere.Vertex v = new Icosphere.Vertex(v3.x - v1.x, v3.y - v1.y, v3.z - v1.z);
				Icosphere.Vertex w = new Icosphere.Vertex(u.y * v.z - u.z * v.y, u.z * v.x - u.x * v.z, u.x * v.y - u.y * v.z);
				area += Math.Sqrt(w.x * w.x + w.y * w.y + w.z * w.z) / 2;
			}

			Console.WriteLine("Average area: {0}", area / sphere.Triangles.Count * radius * radius);
			Console.WriteLine("Total area: {0}", area);*/
		}

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

			return LerpColor(alt - idx * 300, AltitudePalette[idx], AltitudePalette[idx + 1]);
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
					p.GetTerrainGradient(-lat, lon, out grad_x, out grad_y);

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

		public static void Main(string[] args)
		{
			Console.WriteLine("TestIcosphere");
			TestIcosphere();

			TestPlanet p = new TestPlanet("Kerbin.dat");

			Console.WriteLine("Print map");
			//PrintMaps(p, "kerbin.png");

			double lat = -0.0485983; //double.Parse(args[0]);
			double lon = 285.2757538; //double.Parse(args[1]);
			Console.WriteLine("Coords: {0} {1}", lat, lon);
			Console.WriteLine("Biome: {0}", p.GetBiomeName(lat * Math.PI / 180, lon * Math.PI / 180));
			Console.WriteLine("Alt: {0}", p.GetTerrainHeight(lat * Math.PI / 180, lon * Math.PI / 180));

			
			Console.WriteLine("Building cities");
			int w = 4096, h = 2048;
			byte[] data;
			using (var surface = CreateMap(out data, p, w, h))
			{
				using (var ctx = new Context(surface))
				{
					var placement = new CityPlacement(p);

					ctx.SetSourceColor(new Color(0.5, 0.5, 0));
					foreach (Planet.Vertex pos in p.Vertices)
					{
						double x = (pos.coord.Longitude + Math.PI) * w / (2 * Math.PI);
						double y = (-pos.coord.Latitude + Math.PI / 2) * h / Math.PI;

						ctx.Arc(x, y, 1, 0, 2 * Math.PI);
						ctx.Fill();
					}

					ctx.SetSourceColor(new Color(0, 0, 0));
					foreach (CityPlacement.Position pos in placement.Cities)
					{
						double x = (pos.Longitude + Math.PI) * w / (2 * Math.PI);
						double y = (-pos.Latitude + Math.PI / 2) * h / Math.PI;

						ctx.Arc(x, y, 3, 0, 2 * Math.PI);
						ctx.Fill();
					}
				}

				surface.WriteToPng("kerbin-cities.png");
			}
		}
	}
}
