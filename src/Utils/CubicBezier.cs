using System;

namespace Sylphe.Utils
{
	public static class CubicBezier
	{
		/// <summary>
		/// Degree elevation from quadratic to cubic: given a quadratic
		/// (2nd order) Bezier curve, find the cubic (3rd order) Bezier
		/// curve that has the same shape. Let the quadratic be defined
		/// by points Q0 Q1 Q2, and the cubic by points P0 P1 P2 P3.
		/// Evidently, the first and last points must be the same, thus
		/// P0 = Q0 and P3 = Q2. This function computes the intermediate
		/// points P1 and P2, given the three points of the quadratic curve.
		/// See https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Degree_elevation
		/// </summary>
		public static void FromQuadratic(Pair q0, Pair q1, Pair q2, out Pair p1, out Pair p2)
		{
			const double c1 = 1.0 / 3.0;
			const double c2 = 2.0 / 3.0;
			p1 = c1 * q0 + c2 * q1;
			p2 = c2 * q1 + c1 * q2;
		}

		/// <summary>
		/// Return the control points of a cubic Bezier curve that approximates
		/// the unit quarter circle in the first quadrant. Various approximation
		/// schemes exist, some have P0 and P3 *not* exactly at (1,0) and (0,1),
		/// therefore all four points are returned. The present implementation,
		/// however, returns p0=(1,0) p1=(1,c) p2=(c,1) p3=(0,1) where c=0.551915
		/// as described in https://spencermortensen.com/articles/bezier-circle/;
		/// see https://en.wikipedia.org/wiki/Composite_B%C3%A9zier_curve for the
		/// "standard" approximation (c=0.552285). Orientation is counter-clockwise.
		///  </summary>
		/// <remarks>
		/// Rotate, scale, and shift the resulting points into size and place.
		/// </remarks>
		public static void QuarterCircle(out Pair p0, out Pair p1, out Pair p2, out Pair p3)
		{
			const double c = 0.551915; // for good circle approximation
			const double radius = 1.0;

			p0 = new Pair(radius  , 0       );
			p1 = new Pair(radius  , radius*c);
			p2 = new Pair(radius*c, radius  );
			p3 = new Pair(0       , radius  );
		}

		/// <summary>
		/// Compute the points of four cubic bezier curves that approximate
		/// a full circle of given radius centered at given cx,cy. The array
		/// <paramref name="points"/> must have room for at least 12 points
		/// starting at the given <paramref name="startIndex"/>. Orientation
		/// is counter-clockwise. The four Bezier curves use points 0,1,2,3;
		/// 3,4,5,6; 6,7,8,9; 9,10,11,0.
		/// See <see cref="QuarterCircle"/> for explanation and references.
		/// </summary>
		public static void FullCircle(Pair[] points, double radius,
		                              double cx, double cy, int startIndex = 0)
		{
			if (points is null)
				throw new ArgumentNullException(nameof(points));
			if (points.Length < startIndex + 16)
				throw new ArgumentException($"{nameof(points)} array is too short");
			if (radius <= 0)
				throw new ArgumentOutOfRangeException(nameof(radius), "must be positive number");

			const double c = 0.551915; // for good circle approximation

			points[startIndex + 0] = new Pair(radius, 0).Shifted(cx, cy);
			points[startIndex + 1] = new Pair(radius, c*radius).Shifted(cx, cy);
			points[startIndex + 2] = new Pair( c*radius, radius).Shifted(cx, cy);
			points[startIndex + 3] = new Pair(0, radius).Shifted(cx, cy);
			points[startIndex + 4] = new Pair(-c*radius, radius).Shifted(cx, cy);
			points[startIndex + 5] = new Pair(-radius, c*radius).Shifted(cx, cy);
			points[startIndex + 6] = new Pair(-radius, 0).Shifted(cx, cy);
			points[startIndex + 7] = new Pair(-radius, -c*radius).Shifted(cx, cy);
			points[startIndex + 8] = new Pair(-c*radius, -radius).Shifted(cx, cy);
			points[startIndex + 9] = new Pair(0, -radius).Shifted(cx, cy);
			points[startIndex + 10] = new Pair(c*radius, -radius).Shifted(cx, cy);
			points[startIndex + 11] = new Pair(radius, -c*radius).Shifted(cx, cy);
		}
	}
}
