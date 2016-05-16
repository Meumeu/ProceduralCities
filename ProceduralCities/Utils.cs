using System;
using UnityEngine;

namespace ProceduralCities
{
	static class Utils
	{
		public class EditableString
		{
			string _prompt;
			string _value;

			public EditableString(string prompt, string value = "")
			{
				_prompt = prompt;
				_value = value;
			}

			public void Draw()
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(_prompt);
				_value = GUILayout.TextField(_value);
				GUILayout.EndHorizontal();
			}

			public static implicit operator string(EditableString instance)
			{
				return instance._value;
			}

			public void Set(string value)
			{
				_value = value;
			}
		}

		public class EditableInt : EditableString
		{
			public EditableInt(string prompt, int value = 0) : base(prompt, value.ToString())
			{
			}

			public static implicit operator int(EditableInt instance)
			{
				int value;
				if (int.TryParse(instance, out value))
					return value;

				return 0;
			}

			public void Set(int value)
			{
				Set(value.ToString());
			}
		}

		public class EditableDouble : EditableString
		{
			public EditableDouble(string prompt, double value = 0) : base(prompt, value.ToString())
			{
			}

			public static implicit operator double(EditableDouble instance)
			{
				double value;
				if (double.TryParse(instance, out value))
					return value;

				return 0;
			}

			public void Set(double value)
			{
				Set(value.ToString());
			}
		}

		// Color palette from SCANsat
		/* Wikipedia color scheme licensed under Creative Commons Attribution-Share Alike 3.0 Unported license
		 * Mars color scheme by PZmaps - http://commons.wikimedia.org/wiki/User:PZmaps
		 * */
		static Func<byte, byte, byte, GradientColorKey> RGB = (r, g, b) => new GradientColorKey(new Color(r / 255.0f, g / 255.0f, b / 255.0f), 0.0f);
		public static Texture2D TextureFromArrayHeight(double[,] data, double minValue, double maxValue)
		{
			var colours = new GradientColorKey[] {
				RGB(176, 243, 190),
				RGB(224, 251, 178),
				RGB(184, 222, 118),
				RGB(39, 165, 42),
				RGB(52, 136, 60),
				RGB(156, 164, 41),
				RGB(248, 176, 4),
				RGB(192, 74, 2),
				RGB(135, 8, 0),
				RGB(116, 24, 5),
				RGB(108, 42, 10),
				RGB(125, 74, 43),
				RGB(156, 129, 112),
				RGB(181, 181, 181),
				RGB(218, 216, 218)
			};

			colours[0].time = 0;
			colours[1].time = 0.001f;

			for (int i = 0; i < colours.Length; i++)
			{
				colours[i].time = (float)i / (float)(colours.Length - 1);
			}

			int width = data.GetLength(0);
			int height = data.GetLength(1);

			Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					float value = Mathf.Clamp01((float)((data[x, y] - minValue) / (maxValue - minValue)));
					if (data[x, y] <= 0)
						tex.SetPixel(x, y, new Color(0, 0, 1));
					else
						for(int i = 1; i < colours.Length; i++)
						{
							if (value < colours[i].time)
							{
								float lambda = (value - colours[i - 1].time) / (colours[i].time - colours[i - 1].time);
								tex.SetPixel(x, y, Color.Lerp(colours[i - 1].color, colours[i].color, lambda));
								break;
							}
						}
				}
			}

			tex.Apply();
			return tex;
		}
	
		public static void Log(string format, params object[] args)
		{
			Debug.Log("[ProceduralCities] " + String.Format(format, args).Replace("\n", "\n[ProceduralCities] "));
		}
	}
}
	