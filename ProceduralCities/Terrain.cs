using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralCities
{
	public class Terrain : PlanetData
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

			for(int i = 0; i < coords.Length; i += 1000)
			{
				int copy = i;
				ThreadDispatcher.QueueToMainThread(() =>
				{
					for(int j = copy; j < copy + 1000 && j < coords.Length; j++)
					{
						Coordinates c = coords[j];
						double alt = Body.pqsController.GetSurfaceHeight(Body.GetRelSurfaceNVector(c.Latitude * 180 / Math.PI, c.Longitude * 180 / Math.PI)) - Body.Radius;
						int biome = -1;

						if (Body.BiomeMap)
						{
							var attr = Body.BiomeMap.GetAtt(c.Latitude, c.Longitude);
							for (int k = 0, n = Body.BiomeMap.Attributes.Length; k < n; k++)
							{
								if (attr == Body.BiomeMap.Attributes[k])
								{
									biome = k;
									break;
								}
							}
						}

						ret[j] = new PairIntDouble(biome, alt);
					}
				});
			}

			ThreadDispatcher.QueueToMainThreadSync(() => {});

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
