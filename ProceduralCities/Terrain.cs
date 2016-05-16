using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralCities
{
	public class Terrain : IPlanetData
	{
		readonly CelestialBody Body;

		public Terrain(CelestialBody body)
		{
			Body = body;

			// FIXME: don't hardcode values
			BiomeList = body.BiomeMap.Attributes.Select(x => new Planet.Biome(
				x.name,
				x.name == "Ice Caps" ? 0.0 : x.name == "Water" ? 0.0 : x.name == "Deserts" ? 0.1 : 1.0
			)).ToArray();

			Radius = body.Radius;
		}

		public PairIntDouble[] GetTerrainAndBiome(Coordinates[] coords)
		{
			DebugUtils.Assert(!ThreadDispatcher.IsMainThread);
			PairIntDouble[] ret = new PairIntDouble[coords.Length];

			ThreadDispatcher.QueueToMainThreadSync(() =>
			{
				for(int i = 0; i < coords.Length; i++)
				{
					double lon = coords[i].Longitude;
					double lat = coords[i].Latitude;

					double alt = Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(lat * 180 / Math.PI, lon * 180 / Math.PI)) - Body.Radius;
					int biome = -1;

					if (Body.BiomeMap)
					{
						var attr = Body.BiomeMap.GetAtt(lat, lon);
						for (int k = 0, n = Body.BiomeMap.Attributes.Length; k < n; k++)
						{
							if (attr == Body.BiomeMap.Attributes[k])
							{
								biome = k;
								break;
							}
						}
					}

					ret[i] = new PairIntDouble(biome, alt);
				}
			});


			return ret;
		}

		public double[] GetTerrain(Coordinates[] coords)
		{
			return GetTerrainAndBiome(coords).Select(x => x.item2).ToArray();
		}

		public Planet.Biome[] BiomeList { get; private set; }
		public double Radius { get; private set; }
	}
}
