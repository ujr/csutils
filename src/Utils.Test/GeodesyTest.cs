using System;
using System.Collections.Generic;
using Xunit;

namespace Sylphe.Utils.Test
{
	// Note: with xUnit.net, in Assert.Equal(expected, actual, precision)
	// the precision argument is the number of decimal digits (0..15).

	public class GeodesyTest
	{
		[Fact]
		public void ClipLatitudeTest()
		{
			Assert.Equal(89.999999, Geodesy.ClipLatitude(89.999999));
			Assert.Equal(90.0, Geodesy.ClipLatitude(90.001));
			Assert.Equal(-89.999999, Geodesy.ClipLatitude(-89.999999));
			Assert.Equal(-90.0, Geodesy.ClipLatitude(-90.001));
		}

		[Fact]
		public void WrapLongitudeTest()
		{
			Assert.Equal(180.0, Geodesy.WrapLongitude(180.0));
			Assert.Equal(179.999999, Geodesy.WrapLongitude(179.999999));
			Assert.Equal(-179.999999, Geodesy.WrapLongitude(-179.999999));
			Assert.Equal(-180.0, Geodesy.WrapLongitude(-180.0));

			Assert.Equal(-170.0, Geodesy.WrapLongitude(190.0));
			Assert.Equal(179.999, Geodesy.WrapLongitude(-540.001), 7);
			Assert.Equal(89.0, Geodesy.WrapLongitude(-631.0));
		}

		[Fact]
		public void WrapLatLonTest()
		{
			double lat = 0;
			double lon = 0;
			Geodesy.WrapLatLon(ref lat, ref lon);
			Assert.Equal(0.0, lat);
			Assert.Equal(0.0, lon);

			lat = 45;
			lon = 60;
			Geodesy.WrapLatLon(ref lat, ref lon);
			Assert.Equal(45.0, lat);
			Assert.Equal(60.0, lon);

			lat = 91;
			lon = 8;
			Geodesy.WrapLatLon(ref lat, ref lon);
			Assert.Equal(89.0, lat);
			Assert.Equal(-172.0, lon);

			lat = -181;
			lon = 12;
			Geodesy.WrapLatLon(ref lat, ref lon);
			Assert.Equal(1.0, lat);
			Assert.Equal(-168.0, lon);

			lat = 271;
			lon = 736;
			Geodesy.WrapLatLon(ref lat, ref lon);
			Assert.Equal(-89.0, lat);
			Assert.Equal(16.0, lon);
		}

		[Fact]
		public void RadiansTest()
		{
			const int precision = 9; // decimal digits
			Assert.Equal(Math.PI, Geodesy.Radians(180), precision);
			Assert.Equal(-2.0 * Math.PI, Geodesy.Radians(-360), precision);
		}

		[Fact]
		public void DegreesTest()
		{
			const int precision = 9; // decimal digits
			Assert.Equal(180.0, Geodesy.Degrees(Math.PI), precision);
			Assert.Equal(-360.0, Geodesy.Degrees(-2.0 * Math.PI), precision);
		}

		[Fact]
		public void DegMinSecTest()
		{
			double dec = Geodesy.ToDegrees(47, 20, 35);
			Geodesy.ToDegMinSec(dec, out var deg, out var min, out var sec);

			const int precision = 9; // decimal digits
			Assert.Equal(47.0, deg, precision);
			Assert.Equal(20.0, min, precision);
			Assert.Equal(35.0, sec, precision);
		}

		[Fact]
		public void EquirectangularDistanceTest()
		{
			// This is an approximation for short distances only!
			// So here's a short distance:

			const double lat1 = 47.394232;
			const double lon1 = 8.527658;
			const double lat2 = 47.387515;
			const double lon2 = 8.522760;

			double s12 = Geodesy.EquirectangularSquared(lat1, lon1, lat2, lon2);
			double d12 = Geodesy.EquirectangularMeters(s12);

			Assert.Equal(833.0, d12, Delta(833.0 * 0.01));

			Assert.Equal(833.0, Geodesy.EquirectangularMeters(lat1, lon1, lat2, lon2), Delta(833.0 * 0.1));

			const double lat5 = 48.137457;
			const double lon5 = 11.575532;
			const double lat6 = 48.137020;
			const double lon6 = 11.575331;

			double s56 = Geodesy.EquirectangularSquared(lat5, lon5, lat6, lon6);
			double d56 = Geodesy.EquirectangularMeters(s56);

			Assert.Equal(51.0, d56, Delta(51.0 * 0.01));
		}

		[Fact]
		public void HaversineDistanceTest()
		{
			// Short distance: within Zurich

			const double lat1 = 47.394232;
			const double lon1 = 8.527658;
			const double lat2 = 47.387515;
			const double lon2 = 8.522760;

			double d12 = Geodesy.HaversineMeters(lat1, lon1, lat2, lon2);
			Assert.Equal(833.0, d12, Delta(0.1));

			// Long distance: Esri Redlands .. Statue of Liberty New York

			const double lat3 = 34.056495;
			const double lon3 = -117.195699;
			const double lat4 = 40.689246;
			const double lon4 = -74.044540;

			double d34 = Geodesy.HaversineMeters(lat3, lon3, lat4, lon4);
			Assert.Equal(3848590.0, d34, Delta(4400));

			// Very short distance: across Marienplatz München

			const double lat5 = 48.137457;
			const double lon5 = 11.575532;
			const double lat6 = 48.137020;
			const double lon6 = 11.575331;

			double d56 = Geodesy.HaversineMeters(lat5, lon5, lat6, lon6);
			Assert.Equal(51.0, d56, Delta(0.2));

			// Note: perfect accuracy is not necessary for our purposes,
			// because we compare haversine distances against each other,
			// not against real-world distances.
		}

		[Fact]
		public void HaversineDistanceAcross180()
		{
			const double lat1 = 60.0;
			const double lat2 = lat1;
			const double lon1 = 179.0;
			const double lon2 = -179.0;

			// expected distance on spherical Earth: dL * R * cos P
			double dLa = Geodesy.Radians(2.0);
			double phi = Geodesy.Radians(lat1);
			double e12 = dLa * Geodesy.EarthMeanRadiusMeters * Math.Cos(phi);
			double d12 = Geodesy.HaversineMeters(lat1, lon1, lat2, lon2);
			Assert.Equal(e12, d12, Delta(100));

			// Anadyr, 177 E
			const double lat3 = 64.0 + 44.0 / 60;
			const double lon3 = 177.0 + 31.0 / 60;
			// Egwekinot, 179 W
			const double lat4 = 66.0 + 20.0 / 60;
			const double lon4 = -179.0 - 7.0 / 60;

			double d34 = Geodesy.HaversineMeters(lat3, lon3, lat4, lon4);
			const double e34 = 235000;
			Assert.Equal(e34, d34, Delta(1000));
		}

		[Fact]
		public void HaversineDistanceAcrossPole()
		{
			const double lat1 = 45.0;
			const double lon1 = 90.0;
			const double lat2 = 45.0;
			const double lon2 = -90.0;

			double d12 = Geodesy.HaversineMeters(lat1, lon1, lat2, lon2);

			// On a spherical Earth, expect 1/4 of the circumpherence:
			double e12 = Geodesy.EarthMeanRadiusMeters * Math.PI / 2;
			Assert.Equal(e12, d12, Delta(100));
		}

		[Fact]
		public void HaversineSortKeyTest()
		{
			// Mathematically, HavMeters(HavSortKey(dist))==dist,
			// but we are faced with roundoff errors:

			const int precision = 9; // decimal digits

			double s1 = Geodesy.HaversineSortKey(1234);
			double d1 = Geodesy.HaversineMeters(s1);
			Assert.Equal(1234, d1, precision);

			double s2 = Geodesy.HaversineSortKey(50);
			double d2 = Geodesy.HaversineMeters(s2);
			Assert.Equal(50, d2, precision);

			double s3 = Geodesy.HaversineSortKey(10*1000*1000);
			double d3 = Geodesy.HaversineMeters(s3);
			Assert.Equal(10*1000*1000, d3, precision);

			// Odessa
			const double lat5 = 46.46834;
			const double lon5 = 30.74066;
			// Istanbul
			const double lat6 = 41.03699;
			const double lon6 = 28.98509;

			double s56 = Geodesy.HaversineSortKey(lat5, lon5, lat6, lon6);
			double d56 = Geodesy.HaversineMeters(s56);
			Assert.Equal(620100, d56, Delta(500));
		}

		[Fact]
		public void PointRadiusBoxTest()
		{
			double west, south, east, north;

			const double minuteMeters = Geodesy.EarthMeanRadiusMeters * Math.PI / 180.0;
			Geodesy.PointRadiusBox(0.0, 0.0, minuteMeters, out west, out south, out east, out north);
			Assert.Equal(-1.0, west, Delta(0.000001));
			Assert.Equal(-1.0, south, Delta(0.000001));
			Assert.Equal(1.0, east, Delta(0.000001));
			Assert.Equal(1.0, north, Delta(0.000001));

			// Bern
			Geodesy.PointRadiusBox(46.94798, 7.44743, 500, out west, out south, out east, out north);
			Assert.Equal(7.44085, west, Delta(0.00001));
			Assert.Equal(46.94348, south, Delta(0.00001));
			Assert.Equal(7.45401, east, Delta(0.00001));
			Assert.Equal(46.95247, north, Delta(0.00001));

			// This circle contains north pole:
			Geodesy.PointRadiusBox(80.0, 0.0, 1200*1000, out west, out south, out east, out north);
			Assert.Equal(-180.0, west);
			Assert.Equal(180.0, east);
			Assert.Equal(90.0, north);
			Assert.Equal(69, south, Delta(1));

			// 200km around Anadyr: across date line, but still far from north pole
			Geodesy.PointRadiusBox(64.727638, 177.517991, 200*1000, out west, out _, out east, out north);
			Assert.True(west > east); // across date line
			Assert.True(north < 90.0);

			// 11'000 km around Rwanda: both poles included => box is whole earth
			Geodesy.PointRadiusBox(-2.0, 30.0, 11*1000*1000, out west, out south, out east, out north);
			Assert.True(west <= -180 && south <= -90 && east >= 180 && north >= 90);
			Assert.False(west > east); // not across date line
		}

		private static DeltaComparer Delta(double delta)
		{
			return new DeltaComparer(delta);
		}

		private class DeltaComparer : IEqualityComparer<double>
		{
			private readonly double _delta;

			public DeltaComparer(double delta)
			{
				_delta = delta;
			}

			public bool Equals(double a, double b)
			{
				return Math.Abs(a-b) < _delta;
			}

			public int GetHashCode(double value)
			{
				return value.GetHashCode();
			}
		}
	}
}
