using System;

namespace ProceduralCities
{
	[Serializable]
	public struct Coordinates
	{
		public readonly double x, y, z, Latitude, Longitude;
		public static readonly Coordinates KSC = new Coordinates(-0.001788962483527778, -1.301584137981534);

		public Coordinates(double x, double y, double z)
		{
			double r = Math.Sqrt(x * x + y * y + z * z);
			this.x = x / r;
			this.y = y / r;
			this.z = z / r;

			Latitude = Math.Asin(this.y);
			Longitude = Math.Atan2(this.z, this.x);

			if (double.IsNaN(Longitude))
				Longitude = 0;
		}

		public Coordinates(double latitude, double longitude)
		{
			while (longitude > Math.PI)
				longitude -= 2 * Math.PI;

			while (longitude < -Math.PI)
				longitude += 2 * Math.PI;

			this.Latitude = latitude;
			this.Longitude = longitude;
			x = Math.Cos(longitude) * Math.Cos(latitude);
			y = Math.Sin(latitude);
			z = Math.Sin(longitude) * Math.Cos(latitude);
		}

		public Coordinates(System.IO.BinaryReader reader)
		{
			Latitude = reader.ReadDouble();
			Longitude = reader.ReadDouble();
			x = reader.ReadDouble();
			y = reader.ReadDouble();
			z = reader.ReadDouble();
		}

		public void Write(System.IO.BinaryWriter writer)
		{
			writer.Write(Latitude);
			writer.Write(Longitude);
			writer.Write(x);
			writer.Write(y);
			writer.Write(z);
		}

		public override string ToString()
		{
			char hemisphereNS = Latitude > 0 ? 'N' : 'S';
			double latDeg = Math.Abs(Latitude * 180 / Math.PI);
			double latMin = (latDeg - Math.Floor(latDeg)) * 60;
			double latSec = (latMin - Math.Floor(latMin)) * 60;

			char hemisphereEW = Longitude > 0 ? 'E' : 'W';
			double lonDeg = Math.Abs(Longitude * 180 / Math.PI);
			double lonMin = (lonDeg - Math.Floor(lonDeg)) * 60;
			double lonSec = (lonMin - Math.Floor(lonMin)) * 60;

			return String.Format("{0}°{1}'{2}\"{3} {4}°{5}'{6}\"{7}",
				Math.Floor(latDeg), Math.Floor(latMin), Math.Floor(latSec), hemisphereNS,
				Math.Floor(lonDeg), Math.Floor(lonMin), Math.Floor(lonSec), hemisphereEW
			);
		}

		public static double Distance(Coordinates u, Coordinates v)
		{
			return Math.Acos(Math.Min(1, u.x * v.x + u.y * v.y + u.z * v.z));
		}

		public static Coordinates LinearCombination(double x, Coordinates u, double y, Coordinates v)
		{
			return new Coordinates(
				x * u.x + y * v.x,
				x * u.y + y * v.y,
				x * u.z + y * v.z
			);
		}

		public static Coordinates LinearCombination(double x, Coordinates u, double y, Coordinates v, double z, Coordinates w)
		{
			return new Coordinates(
				x * u.x + y * v.x + z * w.x,
				x * u.y + y * v.y + z * w.y,
				x * u.z + y * v.z + z * w.z
			);
		}
	}
}

