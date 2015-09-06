using System;

namespace ProceduralCities
{
	[Serializable]
	public struct Coordinates
	{
		public readonly double x, y, z, Latitude, Longitude;

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
			this.Latitude = latitude;
			this.Longitude = longitude;
			x = Math.Cos(longitude) * Math.Cos(latitude);
			y = Math.Sin(latitude);
			z = Math.Sin(longitude) * Math.Cos(latitude);
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
			return Math.Acos(u.x * v.x + u.y * v.y + u.z * v.z);
		}
	}
}

