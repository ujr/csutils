using System;
using System.Collections.Generic;

namespace Sylphe.Utils
{
	/// <summary>
	/// Immutable representation of an envelope (bounding box).
	/// </summary>
	public sealed class Envelope : IEquatable<Envelope>
	{
		#region Constructors

		private Envelope() : this(0, 0, -1, -1, true) {}

		public Envelope(double x, double y) : this(x, y, x, y) {}

		public Envelope(double x0, double y0, double x1, double y1, bool verbatim = false)
		{
			if (verbatim)
			{
				XMin = x0;
				YMin = y0;
				XMax = x1;
				YMax = y1;
			}
			else
			{
				XMin = Math.Min(x0, x1);
				YMin = Math.Min(y0, y1);
				XMax = Math.Max(x0, x1);
				YMax = Math.Max(y0, y1);
			}
		}

		public Envelope(Point p) : this(p, p) {}

		public Envelope(Point p0, Point p1)
		{
			if (p0 == null)
				throw new ArgumentNullException(nameof(p0));
			if (p1 == null)
				throw new ArgumentNullException(nameof(p1));

			XMin = Math.Min(p0.X, p1.X);
			YMin = Math.Min(p0.Y, p1.Y);
			XMax = Math.Max(p0.X, p1.X);
			YMax = Math.Max(p0.Y, p1.Y);
		}

		public Envelope(Envelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException(nameof(envelope));

			XMin = envelope.XMin;
			YMin = envelope.YMin;
			XMax = envelope.XMax;
			YMax = envelope.YMax;
		}

		/// <summary>
		/// Create an envelope that is the bounding box around the
		/// given sequence of points (empty if no points).
		/// This is typically at least 5 times faster than repeated
		/// <c>bbox = bbox.Expand(point)</c> calls.
		/// </summary>
		public static Envelope Create(IEnumerable<Point> points)
		{
			if (points == null) return Empty;

			double xmin = double.MaxValue, ymin = double.MaxValue;
			double xmax = double.MinValue, ymax = double.MinValue;
			long count = 0;

			foreach (var point in points)
			{
				if (point == null) continue;

				if (point.X < xmin) xmin = point.X;
				if (point.X > xmax) xmax = point.X;

				if (point.Y < ymin) ymin = point.Y;
				if (point.Y > ymax) ymax = point.Y;

				count += 1;
			}

			return count > 0 ? new Envelope(xmin, ymin, xmax, ymax) : Empty;
		}

		public static Envelope Empty { get; } = new Envelope();

		#endregion

		public double XMin { get; }
		public double YMin { get; }
		public double XMax { get; }
		public double YMax { get; }

		public double Width => IsEmpty ? 0 : XMax - XMin;
		public double Height => IsEmpty ? 0 : YMax - YMin;

		/// <remarks>
		/// Envelope represents a closed interval, that is, [x,x] and [y,y]
		/// is considered to contain the the point (x,y) and only this point.
		/// </remarks>
		public bool IsEmpty => XMin > XMax || YMin > YMax;

		#region Containment

		public bool Contains(double x, double y)
		{
			if (IsEmpty) return false;
			return XMin <= x && x <= XMax &&
			       YMin <= y && y <= YMax;
		}

		public bool Contains(Point point)
		{
			if (point == null)
				throw new ArgumentNullException(nameof(point));

			return Contains(point.X, point.Y);
		}

		public bool Contains(Envelope other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			if (IsEmpty) return false;
			if (other.IsEmpty) return true;

			return XMin <= other.XMin && other.XMax <= XMax &&
			       YMin <= other.YMin && other.YMax <= YMax;
		}

		#endregion

		#region Intersection

		public bool Intersects(Envelope other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			if (IsEmpty || other.IsEmpty) return false;
			return XMin < other.XMax && other.XMin < XMax &&
			       YMin < other.YMax && other.YMin < YMax;
		}

		public Envelope Intersect(Envelope other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			if (IsEmpty || other.IsEmpty) return new Envelope(Empty);

			double x0 = Math.Max(XMin, other.XMin);
			double y0 = Math.Max(YMin, other.YMin);
			double x1 = Math.Min(XMax, other.XMax);
			double y1 = Math.Min(YMax, other.YMax);

			return new Envelope(x0, y0, x1, y1, true);
		}

		#endregion

		#region Expansion

		/// <summary>
		/// Return a new envelope that is the least envelope
		/// that contains this envelope and the point (x,y).
		/// </summary>
		public Envelope Expand(double x, double y)
		{
			if (IsEmpty)
			{
				return new Envelope(x, y);
			}

			double x0 = Math.Min(x, XMin);
			double y0 = Math.Min(y, YMin);
			double x1 = Math.Max(x, XMax);
			double y1 = Math.Max(y, YMax);

			return new Envelope(x0, y0, x1, y1, true);
		}

		public Envelope Expand(Point point)
		{
			if (point == null)
				throw new ArgumentNullException(nameof(point));

			return Expand(point.X, point.Y);
		}

		public Envelope Expand(Envelope other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			if (IsEmpty) return new Envelope(other);
			if (other.IsEmpty) return new Envelope(this);

			double x0 = Math.Min(XMin, other.XMin);
			double y0 = Math.Min(YMin, other.YMin);
			double x1 = Math.Max(XMax, other.XMax);
			double y1 = Math.Max(YMax, other.YMax);

			return new Envelope(x0, y0, x1, y1, true);
		}

		/// <summary>
		/// Return a new envelope that is a copy of this envelope
		/// expanded (grown or shrunk) by <paramref name="factor"/>.
		/// The center point remains unchanged.
		/// </summary>
		public Envelope Expand(double factor)
		{
			if (IsEmpty || double.IsNaN(factor))
			{
				return new Envelope(this);
			}

			double dx = Width * factor / 2;
			double dy = Height * factor / 2;

			return new Envelope(XMin - dx, YMin - dy, XMax + dx, YMax + dy, true);
		}

		#endregion

		#region Equality

		public bool Equals(Envelope other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;

			if (IsEmpty)
			{
				return other.IsEmpty;
			}

			return other.XMin == XMin && other.YMin == YMin &&
			       other.XMax == XMax && other.YMax == YMax;
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(Envelope)) return false;
			return Equals((Envelope) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = XMin.GetHashCode();
				result = (result * 397) ^ YMin.GetHashCode();
				result = (result * 397) ^ XMax.GetHashCode();
				result = (result * 397) ^ YMax.GetHashCode();
				return result;
			}
		}

		#endregion

		public override string ToString()
		{
			return $"XMin = {XMin}, YMin = {YMin}, XMax = {XMax}, YMax = {YMax}";
		}
	}
}
