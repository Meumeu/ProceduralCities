using System;
using System.IO;

namespace ProceduralCities
{
	public class PlanetExporter
	{
		readonly CelestialBody Body;
		public PlanetExporter(CelestialBody body)
		{
			Body = body;
		}

		double TerrainHeight(double lat, double lon)
		{
			return Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(lat, lon)) - Body.Radius;
		}

		byte Biome(double lat, double lon)
		{
			var attr = Body.BiomeMap.GetAtt(lat * Math.PI / 180, lon * Math.PI / 180);
			for (int k = 0, n = Body.BiomeMap.Attributes.Length; k < n; k++)
			{
				if (attr == Body.BiomeMap.Attributes[k])
				{
					return (byte)k;
				}
			}
			return 255;
		}

		public void Export(string filename, int width, int height)
		{
			using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
			{
				writer.Write(width);
				writer.Write(height);

				for (int i = 0; i < height; i++)
				{
					double lat = ((double)i / (double)height) * 180 - 90;
					for (int j = 0; j < width; j++)
					{
						double lon = ((double)j / (double)width) * 360 - 180;
						writer.Write((float)TerrainHeight(lat, lon));
					}
				}
				
				writer.Write(width);
				writer.Write(height);
				for (int i = 0; i < height; i++)
				{
					double lat = ((double)i / (double)height) * 180 - 90;
					for (int j = 0; j < width; j++)
					{
						double lon = ((double)j / (double)width) * 360 - 180;
						writer.Write(Biome(lat, lon));
					}
				}

				for(byte i = 0; i < Body.BiomeMap.Attributes.Length; i++)
				{
					writer.Write(Body.BiomeMap.Attributes[i].name);
				}
			}
		}
	}
}

