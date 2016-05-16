using System;

#if NOTKSP

public struct Vector3d
{
	public double x;
	public double y;
	public double z;

	public Vector3d(double x, double y, double z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static Vector3d Cross(Vector3d lhs, Vector3d rhs)
	{
		return new Vector3d(
			lhs.y * rhs.z - lhs.z * rhs.y,
			lhs.z * rhs.x - lhs.x * rhs.z,
			lhs.x * rhs.y - lhs.y * rhs.x);
	}

	public static double Dot(Vector3d lhs, Vector3d rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
	}

	public static Vector3d operator +(Vector3d a, Vector3d b)
	{
		return new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	public static Vector3d operator /(Vector3d a, double d)
	{
		return new Vector3d(a.x / d, a.y / d, a.z / d);
	}

	public static Vector3d operator *(Vector3d a, double d)
	{
		return new Vector3d(d * a.x, d * a.y, d * a.z);
	}

	public static Vector3d operator *(double d, Vector3d a)
	{
		return new Vector3d(d * a.x, d * a.y, d * a.z);
	}

	public static Vector3d operator -(Vector3d a, Vector3d b)
	{
		return new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
	}

	public static Vector3d operator -(Vector3d a)
	{
		return new Vector3d(-a.x, -a.y, -a.z);
	}

	public double magnitude
	{
		get
		{
			return Math.Sqrt(x * x + y * y + z * z);
		}
	}
	public Vector3d normalized
	{
		get
		{
			double tmp = 1 / magnitude;
			return this * tmp;
		}
	}

	public static Vector3d Exclude(Vector3d excludeThis, Vector3d fromThat)
	{
		Vector3d tmp = excludeThis.normalized;
		return fromThat - Dot(tmp, fromThat) * tmp;
	}

	public override string ToString()
	{
		return string.Format("[{0:F4}, {1:F4}, {2:F4}]", x, y, z);
	}

	public string ToString(string format)
	{
		return string.Format("{0}; {1}; {2}", x.ToString(format), y.ToString(format), z.ToString(format));
	}
}

#endif
