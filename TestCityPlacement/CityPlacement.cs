using System;
using System.Collections.Generic;

namespace ProceduralCities
{
	public class CityPlacement
	{
		public struct Position
		{
			public double x;
			public double y;
			public double z;
			public int index;

			public byte Biome;
			public double TerrainHeight;
			public double Latitude;
			public double Longitude;
			public double score;

			public Position(Planet.Vertex v, int index) : this(v.coord.x, v.coord.y, v.coord.z, index)
			{
			}

			public Position(double x, double y, double z, int index)
			{
				this.x = x;
				this.y = y;
				this.z = z;
				this.index = index;

				Latitude = Math.Asin(y);
				Longitude = Math.Atan2(z, x);

				Biome = 255;
				TerrainHeight = 0;
				score = 0;
			}
		}

		TestPlanet planet;
		public Position[] Cities;

		string CoordToString(double lat, double lon)
		{
			lat *= 180 / Math.PI;
			string hemisphere = lat > 0 ? "N" : "S";
			lat = Math.Abs(lat);
			int latDeg = (int)lat;
			int latMin = (int)((lat - latDeg) * 60);
			int latSec = (int)(((lat - latDeg) * 60 - latMin) * 60);
			string ret = string.Format("{0}°{1}'{2}''{3} ", latDeg, latMin, latSec, hemisphere);

			lon *= 180 / Math.PI;
			hemisphere = lon > 0 ? "E" : "W";
			lon = Math.Abs(lon);
			int lonDeg = (int)lon;
			int lonMin = (int)((lon - lonDeg) * 60);
			int lonSec = (int)(((lon - lonDeg) * 60 - lonMin) * 60);
			return ret + string.Format("{0}°{1}'{2}''{3}", lonDeg, lonMin, lonSec, hemisphere);
		}

		public string KerbalMapsUrl()
		{
			string url = "http://www.kerbalmaps.com/?";
			int i = 1;
			foreach (Position p in Cities)
			{
				double lat = p.Latitude * 180 / Math.PI;
				double lon = p.Longitude * 180 / Math.PI;
				url += string.Format(new System.Globalization.CultureInfo("en-US"), "loc={0},{1},City%20{2}%20{3}%20{4:F1}%20m&", lat, lon, i + 1, planet.Biomes[p.Biome].name.Replace(" ", "%20"), p.TerrainHeight);
				i++;
			}

			return url;
		}

		public CityPlacement(TestPlanet planet)
		{
			this.planet = planet;
			for (int i = 0, n = planet.Biomes.Count; i < n; i++)
			{
				switch (planet.Biomes[i].name)
				{
				case "Deserts":
					planet.Biomes[i].desirability = 0.1;
					break;
				case "Ice Caps":
				case "Water":
					planet.Biomes[i].desirability = 0.0;
					break;
				default:
					planet.Biomes[i].desirability = 1.0;
					break;
				}

				Console.WriteLine("Biome \"{0}\", desirability={1}", planet.Biomes[i].name, planet.Biomes[i].desirability);
			}

			List<Position> positions = new List<Position>();
			double sumScores = 0;

			for (int i = 0, n = planet.Vertices.Count; i < n; i++)
			{
				Position p = new Position(planet.Vertices[i], i);
				p.Biome = planet.GetBiomeId(p.Latitude, p.Longitude);
				p.TerrainHeight = this.planet.GetTerrainHeight(p.Latitude, p.Longitude);

				if (p.TerrainHeight < 0)
					continue;

				p.score = planet.Biomes[p.Biome].desirability;

				sumScores += p.score;

				positions.Add(p);
			}

			Console.WriteLine("{0} positions considered, {1} positions kept", planet.Vertices.Count, positions.Count);
			Console.WriteLine("Sum scores: {0}", sumScores);

			var rand = new System.Random(0);
			int nb = 50;
			Cities = new Position[nb];
			for (int i = 0; i < nb; i++)
			{
				double r = rand.NextDouble() * sumScores;
				int j = 0;
				while (r > positions[j].score)
				{
					r -= positions[j++].score;
				}

				Cities[i] = positions[j];
				Console.WriteLine("Founded city {0}, position: {1}, {2}, altitude: {3} m", i + 1, CoordToString(positions[j].Latitude, positions[j].Longitude), planet.Biomes[positions[j].Biome].name, positions[j].TerrainHeight);
			}

			Console.WriteLine(KerbalMapsUrl());
		}
	}
}

