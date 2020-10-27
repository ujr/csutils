using System;

namespace Sylphe.Utils
{
	/// <summary>
	/// Utility functions related to "geodetic" stuff
	/// </summary>
	public static class Geodesy
	{
		/// <summary>
		/// The "mean readius" (2a+b)/3 in meters, assuming the WGS84
		/// ellipsoid; see https://en.wikipedia.org/wiki/Earth_radius
		/// </summary>
		public const double EarthMeanRadiusMeters = 6371008.7714;

		/// <summary>
		/// Clip the given latitude (in degrees) to
		/// the range -90 to +90, both ends inclusive.
		/// </summary>
		public static double ClipLatitude(double degrees)
		{
			const double minLatDeg = -90.0;
			const double maxLatDeg = +90.0;

			if (degrees < minLatDeg) return minLatDeg;
			if (degrees > maxLatDeg) return maxLatDeg;

			return degrees;
		}

		/// <summary>
		/// Reduce the given longitude (in degrees) to
		/// the range -180 (inclusive) to 180 (exclusive).
		/// </summary>
		public static double WrapLongitude(double degrees)
		{
			if (-180 <= degrees && degrees <= 180)
			{
				return degrees;
			}

			double fullturns = Math.Floor((degrees + 180) / 360); // round up

			return degrees - fullturns * 360;
		}

		/// <summary>
		/// Reduce latitude to [-90,90] and longitude to [-180,180).
		/// Input latitudes outside [-90,90] cross the pole and
		/// continue on the other side of the world, thus needing
		/// longitude to be adjusted as well!
		/// </summary>
		public static void WrapLatLon(ref double lat, ref double lon)
		{
			// Originally from:
			// https://gist.github.com/missinglink/d0a085188a8eab2ca66db385bb7c023a

			var reduced = lat % 90;

			// If exactly at a pole, longitude is undefined; in this case
			// it would be better to NOT flip longitude; the code below
			// yields lon=-135 for lat=90, lon=45.

			int quadrant = (int) Math.Floor(Math.Abs(lat) / 90) % 4;
			var pole = lat > 0 ? 90 : -90;

			switch (quadrant)
			{
				case 0:
					lat = reduced;
					break;
				case 1:
					lat = pole - reduced;
					lon += 180;
					break;
				case 2:
					lat = -reduced;
					lon += 180;
					break;
				case 3:
					lat = -pole + reduced;
					break;
			}

			if (lon > 180 || lon < -180)
			{
				lon -= Math.Floor((lon + 180) / 360) * 360;
			}
		}

		public static double Radians(double degrees)
		{
			const double radPerDeg = Math.PI / 180.0;
			return degrees * radPerDeg;
		}

		public static double Degrees(double radians)
		{
			const double degPerRad = 180.0 / Math.PI;
			return radians * degPerRad;
		}

		public static void ToDegMinSec(double degrees, out int deg, out int min, out double sec)
		{
			deg = (int) Math.Floor(degrees);
			min = (int) Math.Floor((degrees - deg) * 60);
			sec = ((degrees - deg) * 60 - min) * 60;
		}

		public static double ToDegrees(double deg, double min, double sec)
		{
			return deg + min / 60 + sec / 3600;
		}

		/// <summary>
		/// A rough approximation to distance on the sphere, only for small distances:
		/// use Pythagoras on the equirectangular projection with standard parallel
		/// at mid latitude of the two points.
		/// Return the square of the distance on the unit sphere.
		/// To get the actual distance on Earth, take the square root
		/// and multiply by <see cref="EarthMeanRadiusMeters"/>.
		/// </summary>
		public static double EquirectangularSquared(double lat1, double lon1, double lat2, double lon2)
		{
			const double radPerDeg = Math.PI/180.0;

			double ϕ1 = lat1 * radPerDeg;
			double ϕ2 = lat2 * radPerDeg;

			double dϕ = ϕ1 - ϕ2;
			double dλ = (lon1 - lon2) * radPerDeg;

			dλ *= Math.Cos((ϕ1 + ϕ2) / 2);

			return dϕ * dϕ + dλ * dλ;
		}

		public static double EquirectangularMeters(double sortKey)
		{
			return EarthMeanRadiusMeters * Math.Sqrt(sortKey);
		}

		public static double EquirectangularMeters(double lat1, double lon1, double lat2, double lon2)
		{
			double dd = EquirectangularSquared(lat1, lon1, lat2, lon2);
			return EquirectangularMeters(dd);
		}

		// The haversine formula assumes a spherical earth.
		// For more accuracy, use Vincenty's method, which
		// assumes an ellipsoidal earth. To start with, see
		// https://en.wikipedia.org/wiki/Great-circle_distance
		// The idea to split the computation into two parts,
		// the "sort key" and the real distance, is from Lucene.

		/// <summary>
		/// Use the haversine formula to compute the great circle
		/// distance in meters between the two points given by their
		/// latitudes and longitudes in degrees (-90..90 and -180..180).
		/// </summary>
		public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
		{
			return HaversineMeters(HaversineSortKey(lat1, lon1, lat2, lon2));
		}

		/// <summary>
		/// See <see cref="HaversineSortKey(double,double,double,double)"/>.
		/// </summary>
		public static double HaversineMeters(double sortKey)
		{
			// Compute the "second half" of the haversine formula:
			// solve hav(d/R)=h/2 for d. The min protects against
			// instable results for nearly antipodal points.

			double sqrt = Math.Sqrt(sortKey*0.5);
			return 2.0 * EarthMeanRadiusMeters * Math.Asin(Math.Min(1.0, sqrt));
		}

		/// <summary>
		/// Compute the "first half" of the haversine distance formula.
		/// This is faster than computing the actual distance, but compares
		/// the same, that is, if d1 &lt; d2 then s1 &lt; s2 and vice versa,
		/// where d1 and d2 are actual distance, s1 and s2 are "sort keys".
		/// Use <see cref="HaversineMeters(double)"/> to convert a sort key
		/// into the actual distance.
		/// <para/>
		/// Latitudes and longitudes are in degrees (-90..90 and -180..180).
		/// </summary>
		public static double HaversineSortKey(double lat1, double lon1, double lat2, double lon2)
		{
			const double radPerDeg = Math.PI/180.0;

			double ϕ1 = lat1 * radPerDeg;
			double ϕ2 = lat2 * radPerDeg;

			double dϕ = ϕ1 - ϕ2;
			double dλ = (lon1 - lon2) * radPerDeg;

			// dλ: don't worry about sign and short/long way around world
			// since dλ appears only as cos(dλ) and cos(x)=cos(-x)=cos(360-x);
			// otherwise, would have to:
			//   let dlon = abs(lon1-lon2);
			//   if (dlon > 180) set dlon = 360 - dlon; // always short leg
			//   let dλ = dlon*π/180

			// Note: 1 - cos is known as the "versed sine" or "versin".
			// Note: hav(x) := (1-cos(x))/2, i.e., half the versed sine.

			double versin1 = 1 - Math.Cos(dϕ);
			double versin2 = 1 - Math.Cos(dλ);

			double h = versin1 + Math.Cos(ϕ1)*Math.Cos(ϕ2)*versin2;

			return h; // 2(hav(dϕ) + cos(ϕ1)*cos(ϕ2)*hav(dλ))
		}

		/// <summary>
		/// Find the sort key that corresponds to the given
		/// <paramref name="distanceMeters"/>. This is the
		/// inverse of <see cref="HaversineMeters(double)"/>.
		/// </summary>
		public static double HaversineSortKey(double distanceMeters)
		{
			// forward: distance d = 2 R asin(min(1,sqrt(h/2)))
			// inverse: h = 2*sin^2(d/(2R)) = 2*hav(d/(2R))

			double r = distanceMeters/EarthMeanRadiusMeters;
			double s = Math.Sin(r/2.0);
			return s*s*2.0;
		}

		/// <summary>
		/// Find the least lat/lon box that contains the circle with the given center and radius.
		/// There are two complications: (1) since meridians converge towards the
		/// poles, the box's eastern and western sides touch the circle not exactly
		/// east and west of its center, but somewhat towards the poles; (2) the
		/// circle may extend acros a pole, in which case the "box" takes the shape
		/// of a spherical cap.
		/// </summary>
		public static void PointRadiusBox(double lat, double lon, double radiusMeters,
			out double west, out double south, out double east, out double north)
		{
			if (!(radiusMeters >= 0))
				throw new ArgumentException("Radius must be non-negative", nameof(radiusMeters));

			const double minLatRad = -Math.PI/2;
			const double maxLatRad = Math.PI/2;
			const double minLonRad = -Math.PI;
			const double maxLonRad = Math.PI;

			const double degPerRad = 180.0 / Math.PI;
			const double radPerDeg = Math.PI / 180.0;

			double lam = lon * radPerDeg;
			double phi = lat * radPerDeg;
			double r = radiusMeters / EarthMeanRadiusMeters;

			south = phi - r;
			north = phi + r;

			if (south <= minLatRad || north >= maxLatRad)
			{
				// A pole is within radius of the center: the box extends
				// from -180..180 and up/down to the pole
				south = Math.Max(south, minLatRad);
				north = Math.Min(north, maxLatRad);
				west = minLonRad;
				east = maxLonRad;
			}
			else
			{
				// Circle is away from the poles: use spherical trigonometry
				// to determine the tangential meridians as follows:
				// The pole P, the circle's center C, and the tangent point Q (where
				// the meridian is tangent to the circle and at right angle to its
				// radius) form a spherical right triangle. Two sides (co-latitude
				// and radius) are known, the vertex anlge at P is desired.
				// Law of sines: sin(P)/sin(r/R)=sin(90°)/sin(co-latitude)
				// therefore: sin(P)=sin(r/R)/cos(centerLat°)
				// where r is the given radius and R the Earth's radius.

				double dlam = Math.Asin(Math.Sin(r)/Math.Cos(phi));
				west = lam - dlam;
				east = lam + dlam;

				// The tangential meridians now may be < -pi or > +pi:
				// if so, "rotate" them into the -pi..pi range (we may
				// still have W > E, which is fine and means our circle
				// extends across the date line):

				if (west < minLonRad)
				{
					west += Math.PI + Math.PI;
				}

				if (east > maxLonRad)
				{
					east -= Math.PI + Math.PI;
				}
			}

			west *= degPerRad;
			south *= degPerRad;
			east *= degPerRad;
			north *= degPerRad;
		}
	}
}
