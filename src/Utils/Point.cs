using System;

namespace Sylphe.Utils
{
	/// <summary>
	/// Immutable representation of a point with X and Y coordinates.
	/// </summary>
	public sealed class Point : IEquatable<Point>
	{
		public Point(Point p) : this(p.X, p.Y) {}

		public Point(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; }

		public double Y { get; }

		public bool IsEmpty => double.IsNaN(X) || double.IsNaN(Y);

		public bool Equals(Point other)
		{
			if (ReferenceEquals(other, null)) return false;
			if (ReferenceEquals(other, this)) return true;

			if (IsEmpty && other.IsEmpty)
			{
				// Hint: NaN doesn't equal anything, including NaN
				return true;
			}

			return X == other.X && Y == other.Y;
		}

		public override string ToString()
		{
			return $"X = {X}, Y = {Y}";
		}

		public static Point Empty { get; } = new Point(double.NaN, double.NaN);
	}
}
