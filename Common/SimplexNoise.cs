using System;

namespace ProceduralCities
{
	class SimplexNoise
	{
		Byte[] permutation_table = new Byte[256];
		double[,] gradient_table = new double[8,3];
		double _gain;
		double _lacunarity;
		double _frequency;
		double _amplitude;

		public SimplexNoise(int seed, double gain, double lacunarity, double frequency, double amplitude)
		{
			_gain = gain;
			_lacunarity = lacunarity;
			_frequency = frequency;
			_amplitude = amplitude;

			Random rnd = new Random(seed);

			for (int i = 0; i < 256; i++)
			{
				permutation_table[i] = (Byte)i;
			}

			for (int i = 0; i < 256; i++)
			{
				int j = rnd.Next(i, 255);
				Byte tmp = permutation_table[i];
				permutation_table[i] = permutation_table[j];
				permutation_table[j] = tmp;
			}


			//set up the gradient table
			for (int i = 0; i < 8; ++i)
			{
				for (int j = 0, k = 1; j < 3; ++j, k <<= 1)
				{
					gradient_table[i,j] = ((i & k) != 0) ? -1 : 1;
				}
			}
		}

		public double Generate(double x, double y, double z, int octaves)
		{
			double value = 0;
			double current_amplitude = _amplitude;

			x *= _frequency;
			y *= _frequency;
			z *= _frequency;

			for (int i = 0; i < octaves; i++)
			{
				value += GenerateOneOctave(x, y, z);
				x *= _lacunarity;
				y *= _lacunarity;
				z *= _lacunarity;
				current_amplitude *= _gain;
			}

			return value;
		}

		double general_skew = 1.0f / 3.0f;
		double general_unskew = 1.0f / 6.0f;

		// See https://code.google.com/p/fractalterraingeneration/source/browse/branches/simplex/SimplexNoise.cpp
		double GenerateOneOctave(double x, double y, double z)
		{
			// 4 corners, each with an x,y, and z coordinate. Note: only corners[0] contains original values;
			// the other three are offset values from corners[0].
			int[,] corners = new int[4, 3];
			// the distances to each of the four corners
			double[,] distances = new double[4, 3];

			//first, get the bottom corner in skewed space
			double specific_skew = general_skew * (x + y + z);
			corners[0, 0] = myfloor(x + specific_skew);
			corners[0, 1] = myfloor(y + specific_skew);
			corners[0, 2] = myfloor(z + specific_skew);

			//next, get the distance vectors to the bottom corner
			double specific_unskew = (corners[0, 0] + corners[0, 1] + corners[0, 2]) * general_unskew;
			distances[0, 0] = x - corners[0, 0] + specific_unskew;
			distances[0, 1] = y - corners[0, 1] + specific_unskew;
			distances[0, 2] = z - corners[0, 2] + specific_unskew;

			//find the coordinates for the two middle corners
			if (distances[0, 0] < distances[0, 1]) // y > x
			{
				if (distances[0, 1] < distances[0, 2]) // if z > y > x
				{
					corners[1, 0] = 0;
					corners[1, 1] = 0;
					corners[1, 2] = 1;

					corners[2, 0] = 0;
					corners[2, 1] = 1;
					corners[2, 2] = 1;
				}
				else if (distances[0, 0] < distances[0, 2]) // if y > z > x
				{
					corners[1, 0] = 0;
					corners[1, 1] = 1;
					corners[1, 2] = 0;

					corners[2, 0] = 0;
					corners[2, 1] = 1;
					corners[2, 2] = 1;
				}
				else // y > x > z
				{
					corners[1, 0] = 0;
					corners[1, 1] = 1;
					corners[1, 2] = 0;

					corners[2, 0] = 1;
					corners[2, 1] = 1;
					corners[2, 2] = 0;
				}
			}
			else // x > y
			{
				if (distances[0, 0] < distances[0, 2]) // z > x > y
				{
					corners[1, 0] = 0;
					corners[1, 1] = 0;
					corners[1, 2] = 1;

					corners[2, 0] = 1;
					corners[2, 1] = 0;
					corners[2, 2] = 1;
				}
				else if (distances[0, 1] < distances[0, 2]) // x > z > y
				{
					corners[1, 0] = 1;
					corners[1, 1] = 0;
					corners[1, 2] = 0;

					corners[2, 0] = 1;
					corners[2, 1] = 0;
					corners[2, 2] = 1;
				}
				else // x > y > z
				{
					corners[1, 0] = 1;
					corners[1, 1] = 0;
					corners[1, 2] = 0;

					corners[2, 0] = 1;
					corners[2, 1] = 1;
					corners[2, 2] = 0;
				}
			}

			// get the top corner
			corners[3, 0] = 1;
			corners[3, 1] = 1;
			corners[3, 2] = 1;

			// get the distances
			for (int i = 1; i <= 3; ++i)
			{
				for (int j = 0; j < 3; ++j)
				{
					distances[i, j] = distances[0, j] - corners[i, j] + general_unskew * i;
				}
			}

			//get the gradients indices
			int[] gradient_index = new int[4];

			gradient_index[0] = permutation_table[(corners[0,0] + permutation_table[(corners[0,1] + permutation_table[corners[0,2] & 255]) & 255]) & 255] & 7;
			for (int i = 1; i < 4; ++i)
				gradient_index[i] = permutation_table[(corners[0,0] + corners[i,0] + permutation_table[(corners[0,1] + corners[i,1] + permutation_table[(corners[0,2] + corners[i,2]) & 255]) & 255]) & 255] & 7;

			//sum the contributions from each corner, found using radial attenuation
			double final_sum = 0.0f;
			for (int i = 0; i < 4; ++i)
			{
				final_sum += radial_attenuation(distances, i, gradient_index[i]);
			}

			return (32.0f * final_sum);
		}

		double radial_attenuation(double[,] distances, int i, int gradient_index)
		{
			double test_product = 0.6f - distances[i,0] * distances[i,0] - distances[i,1] * distances[i,1] - distances[i,2] * distances[i,2];

			if (test_product < 0.0f)
				return (0.0f);

			double dot_product = distances[i,0] * gradient_table[gradient_index,0] + distances[i,1] * gradient_table[gradient_index,1] + distances[i,2] * gradient_table[gradient_index,2];

			test_product *= test_product; //square it

			return (test_product * test_product * dot_product);
		}

		int myfloor(double value)
		{
			return value >= 0 ? (int)value : (int)value - 1;
		}
	}
}