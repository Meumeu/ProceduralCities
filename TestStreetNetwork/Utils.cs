using System;

namespace ProceduralCities
{
	public class Utils
	{
		public Utils()
		{
		}
	}

	public class Palette
	{
		byte[] _data;
		int _nbColors;

		public Palette(byte[] data)
		{
			_data = data;
			_nbColors = _data.Length / 3;
		}

		public static Palette HeightMap()
		{
			return new Palette(new byte[] {
				176, 243, 190,
				224, 251, 178,
				184, 222, 118,
				39, 165, 42,
				52, 136, 60,
				156, 164, 41,
				248, 176, 4,
				192, 74, 2,
				135, 8, 0,
				116, 24, 5,
				108, 42, 10,
				125, 74, 43,
				156, 129, 112,
				181, 181, 181,
				218, 216, 218
			});
		}

		public static Palette PopulationMap()
		{
			return new Palette(new byte[] {
				128, 255, 128,
				128, 255, 255,
				128, 128, 255,
				255, 128, 128
			});
		}

		public void Plot(byte[] img, int index, double value)
		{
			value = (value < 0 ? 0 : value > 1 ? 1 : value) * _nbColors;
			int idx = (int)Math.Floor(value);
			if (idx >= _nbColors - 1)
				idx = _nbColors - 2;
			
			double lambda = value - idx;
			idx *= 3;

			img[index + 2] = (byte)(_data[idx] * (1-lambda) + _data[idx + 3] * lambda);
			img[index + 1] = (byte)(_data[idx+1] * (1-lambda) + _data[idx + 4] * lambda);
			img[index + 0] = (byte)(_data[idx+2] * (1-lambda) + _data[idx + 5] * lambda);
		}
	}
}

