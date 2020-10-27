using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class PointQuadtreeTest
	{
		private readonly ITestOutputHelper _output;

		public PointQuadtreeTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void CanBuild()
		{
			var qt = GetSampleDataset();

			string serializedQuadtree = GetSerializedQuadtree(qt);

			Assert.Equal(
				"(Chicago (Toronto ...(Buffalo ....))(Denver ....)(Omaha ....)(Mobile (Atlanta ....)..(Miami ....)))",
				serializedQuadtree);

			Assert.Equal(8, qt.ItemCount);
			Assert.Equal(1, qt.MeanDepth);
			Assert.Equal(2, qt.MaxDepth);
		}

		[Fact]
		public void CanCount()
		{
			var qt = GetSampleDataset();

			var extent = new Envelope(50, 0, 100, 50); // SE quadrant
			Predicate<Place> filter = place => place.Tag.StartsWith("M");

			int count = qt.Count(extent, null);
			Assert.Equal(3, count); // Atlanta, Miami, Mobile

			count = qt.Count(extent, filter);
			Assert.Equal(2, count); // Miami, Mobile
		}

		[Fact]
		public void CanQueryBox()
		{
			var qt = GetSampleDataset();

			var r1 = new List<Place>();
			int c1 = qt.Query(new Envelope(0, 0, 100, 100), null, r1);
			Assert.Equal(8, c1);
			Assert.Equal(r1.OrderBy(p => p.Tag), Seq(Atlanta, Buffalo, Chicago, Denver, Miami, Mobile, Omaha, Toronto));

			// Point query (extent with Width==Height==0)
			var r2 = new List<Place>();
			int c2 = qt.Query(new Envelope(82, 65, 82, 65), null, r2);
			Assert.Equal(1, c2);
			Assert.Equal(r2.OrderBy(p => p.Tag), Seq(Buffalo));

			// Box query (rectangular extent)
			var r3 = new List<Place>();
			int c3 = qt.Query(new Envelope(50, 0, 100, 42), null, r3);
			Assert.Equal(3, c3);
			Assert.Equal(r3.OrderBy(p => p.Tag), Seq(Atlanta, Miami, Mobile));

			// Box query with borderline case
			var r4 = new List<Place>();
			int c4 = qt.Query(new Envelope(35, 0, 100, 42), null, r4);
			Assert.Equal(4, c4);
			Assert.Equal(r4.OrderBy(p => p.Tag), Seq(Atlanta, Chicago, Miami, Mobile));
		}

		[Fact]
		public void CanQueryBoxFiltered()
		{
			var qt = GetSampleDataset();

			var all = new Envelope(0, 0, 100, 100);
			var lrq = new Envelope(50, 0, 100, 50); // SE quadrant
			Predicate<Place> filter = place => place.Tag.Length == 7;

			var r1 = new List<Place>();
			int c1 = qt.Query(all, filter, r1);
			Assert.Equal(4, c1);
			Assert.Equal(r1.OrderBy(p => p.Tag), Seq(Atlanta, Buffalo, Chicago, Toronto));

			var r2 = new List<Place>();
			int c2 = qt.Query(lrq, filter, r2);
			Assert.Equal(1, c2);
			Assert.Equal(r2.OrderBy(p => p.Tag), Seq(Atlanta));
			// Spatial criterion excludes: Buffalo, Chicago, Toronto
		}

		[Fact]
		public void CanQueryNear()
		{
			var qt = GetSampleDataset();

			var r1 = new List<Place>();
			int c1 = qt.Query(new Point(50, 50), 100, 1, null, r1);
			Assert.Equal(1, c1);
			Assert.Equal(r1, Seq(Chicago));

			var r2 = new List<Place>();
			int c2 = qt.Query(new Point(50, 50), 100, 2, null, r2);
			Assert.Equal(2, c2);
			Assert.Equal(r2, Seq(Chicago, Omaha));

			var r3 = new List<Place>();
			int c3 = qt.Query(Atlanta.Point, 100, 3, null, r3);
			Assert.Equal(3, c3);
			Assert.Equal(r3, Seq(Atlanta, Miami, Mobile));
		}

		[Fact]
		public void CanQueryNearFiltered()
		{
			PointQuadtree<Place> qt = GetSampleDataset();
			Predicate<Place> filter = place => place.Tag.Length != 7;
			var center = new Point(50, 50);

			var r1 = new List<Place>();
			int c1 = qt.Query(center, 100, 1, filter, r1);
			Assert.Equal(1, c1);
			Assert.Equal(r1, Seq(Omaha));
			// Chicago is nearest, but excluded by filter; Omaha is next

			var r2 = new List<Place>();
			int c2 = qt.Query(center, 100, 2, filter, r2);
			Assert.Equal(2, c2);
			Assert.Equal(r2, Seq(Omaha, Mobile));

			var r3 = new List<Place>();
			int c3 = qt.Query(Atlanta.Point, 100, 3, filter, r3);
			Assert.Equal(3, c3);
			Assert.Equal(r3, Seq(Miami, Mobile, Omaha));
			// Atlanta is at the query point, but ruled out by filter
		}

		[Fact]
		public void CanQueryNearGeo()
		{
			var qt = GetGlobalDataset();

			var paris = new Point(2.347, 48.854); // close to Notre Dame
			var gambell = new Point(-171.728, 63.780); // on St Lawrence Island
			const double km = 1000;

			var r1 = new List<Place>();
			qt.QueryGeo(paris, 500, 10, null, r1);
			Assert.Equal(Seq(NotreDameTourNord, NotreDameTourSud), r1);

			var r2 = new List<Place>();
			qt.QueryGeo(paris, 5000, 10, null, r2);
			Assert.Equal(Seq(NotreDameTourNord, NotreDameTourSud, EiffelTower), r2);

			var r3 = new List<Place>();
			qt.QueryGeo(gambell, 400 * km, 10, null, r3);
			Assert.Equal(Seq(StMatthewIsland), r3);

			var r4 = new List<Place>();
			qt.QueryGeo(gambell, 600 * km, 10, null, r4); // across 180 meridian, but not across pole
			Assert.Equal(Seq(StMatthewIsland, Anadyr), r4);

			var r5 = new List<Place>();
			qt.QueryGeo(gambell, 3000 * km, 10, null, r5); // across pole, Longyearbyen within box, but 4000km away
			Assert.Equal(Seq(StMatthewIsland, Anadyr), r5);

			var r6 = new List<Place>();
			qt.QueryGeo(new Point(180.0, 75.0), 3000 * km, 10, null, r6); // now within box and 3000km
			Assert.Equal(Seq(Anadyr, StMatthewIsland, Longyearbyen), r6);
		}

		[Fact]
		public void CanQueryWithCollector()
		{
			var qt = GetSampleDataset();
			var extent = new Envelope(50, 0, 100, 42);

			var list = new List<Place>();
			void Collector(Place place, Point point) => list.Add(place);

			qt.Query(extent, Collector);

			Assert.Equal(Seq(Atlanta, Miami, Mobile), list.OrderBy(p => p.Tag));
		}

		[Fact]
		public void CanBorderlineInsertion()
		{
			var qt = GetSampleDataset();

			// Memphis is (in the test data) exactly south of Chicago.
			// Following the convention that a quadrant's lower and left
			// boundaries are closed, but that its upper and right boundaries
			// are open, Memphis must go to Chicago's SE quadrant!
			qt.Add(Memphis, Memphis.Point);

			string serializedQuadtree = GetSerializedQuadtree(qt);

			Assert.Equal(
				"(Chicago (Toronto ...(Buffalo ....))(Denver ....)(Omaha ....)(Mobile (Atlanta ....)(Memphis ....).(Miami ....)))",
				serializedQuadtree);
		}

		[Fact]
		public void CanHandleCoincidentPoints()
		{
			var qt = new PointQuadtree<Place>();

			qt.Add(Chicago, Chicago.Point);
			qt.Add(Chicago, Chicago.Point); // again

			string serializedQuadtree = GetSerializedQuadtree(qt);
			Assert.Equal("(Chicago (Chicago ....)...)", serializedQuadtree);

			var r1 = new List<Place>();
			int c1 = qt.Query(new Envelope(0, 0, 100, 100), null, r1);
			Assert.Equal(2, c1);
			Assert.Equal(r1.OrderBy(p => p.Tag), Seq(Chicago, Chicago));

			var r2 = new List<Place>();
			int c2 = qt.Query(new Envelope(Chicago.Point, Chicago.Point), null, r2);
			Assert.Equal(2, c2);
			Assert.Equal(r2.OrderBy(p => p.Tag), Seq(Chicago, Chicago));

			qt.Add(Mobile, Mobile.Point);
			qt.Add(Mobile, Mobile.Point); // again

			var r3 = new List<Place>();
			int c3 = qt.Query(new Envelope(Mobile.Point, Mobile.Point), null, r3);
			Assert.Equal(2, c3);
			Assert.Equal(r3.OrderBy(p => p.Tag), Seq(Mobile, Mobile));

			var r4 = new List<Place>();
			int c4 = qt.Query(new Envelope(Chicago.Point, Mobile.Point), null, r4);
			Assert.Equal(4, c4);
			Assert.Equal(r4.OrderBy(p => p.Tag), Seq(Chicago, Chicago, Mobile, Mobile));

			serializedQuadtree = GetSerializedQuadtree(qt);
			Assert.Equal("(Chicago (Chicago ....)..(Mobile (Mobile ....)...))", serializedQuadtree);

			qt.Add(Toronto, Toronto.Point);
			qt.Add(Buffalo, Buffalo.Point);
			qt.Add(Denver, Denver.Point);
			qt.Add(Omaha, Omaha.Point);
			qt.Add(Atlanta, Atlanta.Point);
			qt.Add(Miami, Miami.Point);
			qt.Add(Memphis, Memphis.Point);
			qt.Add(Memphis, Memphis.Point); // again

			serializedQuadtree = GetSerializedQuadtree(qt);
			Assert.Equal(
				"(Chicago (Chicago (Toronto ...(Buffalo ....))...)(Denver ....)(Omaha ....)(Mobile (Mobile (Atlanta ....)...)(Memphis (Memphis ....)...).(Miami ....)))",
				serializedQuadtree);

			var r5 = new List<Place>();
			int c5 = qt.Query(new Envelope(30, 5, 60, 25), null, r5);
			Assert.Equal(4, c5);
			Assert.Equal(r5.OrderBy(p => p.Tag), Seq(Memphis, Memphis, Mobile, Mobile));

			ShowStats(qt);
		}

		[Fact]
		public void CanReplaceCoincidentPoints()
		{
			var qt = GetSampleDataset();
			qt.Build();
			int count = qt.ItemCount;

			qt.ReplaceCoincident = true;

			var foo = new Place(Chicago.Point.X, Chicago.Point.Y, "Foo");
			var bar = new Place(Omaha.Point.X, Omaha.Point.Y, "Bar");

			qt.Add(foo, foo.Point); // should replace Chicago
			qt.Add(bar, bar.Point); // should replace Omaha

			qt.Build();
			Assert.Equal(count, qt.ItemCount);
			var results = new List<Place>();
			qt.Query(qt.BoundingBox, null, results);
			Assert.Contains(foo, results);
			Assert.DoesNotContain(Chicago, results);
			Assert.Contains(bar, results);
			Assert.DoesNotContain(Omaha, results);
		}

		[Fact]
		public void CanBoundingBox()
		{
			var qt = new PointQuadtree<int>();

			Assert.False(qt.IsDirty);
			Assert.Equal(0, qt.ItemCount);
			Assert.Null(qt.BoundingBox);

			qt.Add(33, new Point(3, 3));
			qt.Build();

			Assert.NotNull(qt.BoundingBox);
			Assert.Equal(new Envelope(3, 3, 3, 3), qt.BoundingBox);

			qt.Add(55, new Point(5, 5));
			qt.Build();

			Assert.NotNull(qt.BoundingBox);
			Assert.Equal(new Envelope(3, 3, 5, 5), qt.BoundingBox);

			qt.Clear();

			Assert.False(qt.IsDirty);
			Assert.Null(qt.BoundingBox);
		}

		[Fact]
		public void CanMaintainDirtyFlag()
		{
			var qt = new PointQuadtree<int>();

			// New empty inverted index isn't dirty:
			Assert.False(qt.IsDirty);

			qt.Add(0, new Point(0, 0));

			// After adding an item, it's dirty:
			Assert.True(qt.IsDirty);

			qt.Build();

			// After building, the index is clean:
			Assert.False(qt.IsDirty);

			qt.Add(1, new Point(1, 1));

			// After adding items, index is dirty:
			Assert.True(qt.IsDirty);

			qt.Clear();

			// After clearing, index is empty and thus clean:
			Assert.False(qt.IsDirty);
		}

		[Fact]
		public void LargeQuadtreeTest()
		{
			const int numPoints = 1000000;
			var random = new Random();
			var timer = new Stopwatch();

			// Build quadtree with numPoints points having
			// random x and y coordinates in the real interval:
			timer.Start();
			var qt = new PointQuadtree<int>();
			for (int id = 0; id < numPoints; id++)
			{
				double x = 100 * random.NextDouble();
				double y = 100 * random.NextDouble();

				qt.Add(id, new Point(x, y));
			}
			timer.Stop();
			_output.WriteLine("Built QT with {0:N0} random points in {1}",
							  numPoints, timer.Elapsed);

			timer.Reset();

			timer.Start();
			var r1 = new List<int>();
			int c1 = qt.Query(new Envelope(0, 0, 100, 100), null, r1);
			Assert.Equal(numPoints, c1);
			Assert.Equal(numPoints, r1.Count);
			timer.Stop();
			_output.WriteLine("Queried world rectangle in {0}", timer.Elapsed);

			timer.Reset();

			timer.Start();
			int c2 = qt.Count(new Envelope(0, 0, 100, 100), null);
			Assert.Equal(numPoints, c2);
			timer.Stop();
			_output.WriteLine("Counted world rectangle in {0}", timer.Elapsed);

			timer.Reset();

			timer.Start();
			var r3 = new List<int>();
			int c3 = qt.Query(new Envelope(0, 0, 50, 50), null, r3);
			Assert.True(r3.Count == c3);
			timer.Stop();
			_output.WriteLine("Queried SW quadrant ({0} points) in {1}",
							  c3, timer.Elapsed);

			timer.Reset();

			timer.Start();
			int c4 = qt.Count(new Envelope(0, 0, 50, 50), null);
			timer.Stop();
			_output.WriteLine("Counted SW quadrant ({0} points) in {1}",
							  c4, timer.Elapsed);

			ShowStats(qt);
		}

		#region Test data

		// Places for sample dataset: taken from Hanan & Samet's book
		private static readonly Place Chicago = new Place(35, 42, "Chicago");
		private static readonly Place Mobile = new Place(52, 10, "Mobile");
		private static readonly Place Toronto = new Place(62, 77, "Toronto");
		private static readonly Place Buffalo = new Place(82, 65, "Buffalo");
		private static readonly Place Denver = new Place(5, 45, "Denver");
		private static readonly Place Omaha = new Place(27, 35, "Omaha");
		private static readonly Place Atlanta = new Place(85, 15, "Atlanta");
		private static readonly Place Miami = new Place(90, 5, "Miami");

		private static readonly Place Memphis = new Place(35, 20, "Memphis");

		private static PointQuadtree<Place> GetSampleDataset()
		{
			var quadtree = new PointQuadtree<Place>();

			quadtree.Add(Chicago, Chicago.Point);
			quadtree.Add(Mobile, Mobile.Point);
			quadtree.Add(Toronto, Toronto.Point);
			quadtree.Add(Buffalo, Buffalo.Point);
			quadtree.Add(Denver, Denver.Point);
			quadtree.Add(Omaha, Omaha.Point);
			quadtree.Add(Atlanta, Atlanta.Point);
			quadtree.Add(Miami, Miami.Point);
			// don't add Memphis

			quadtree.Build();

			return quadtree;
		}

		// Places around the globe (hand digitized)
		private static readonly Place FernsehturmBerlin = new Place(13.409422, 52.520824, "Fernsehturm, Berlin");
		private static readonly Place ElizabethTowerLondon = new Place(-0.124554, 51.500695, "Elizabeth Tower, London");
		private static readonly Place NotreDameTourNord = new Place(2.349272, 48.853402, "Tour nord du Notre Dame, Paris");
		private static readonly Place NotreDameTourSud = new Place(2.349035, 48.853087, "Tour sud du Notre Dame, Paris");
		private static readonly Place EiffelTower = new Place(2.294525, 48.858270, "Tour Eiffel, Paris");
		private static readonly Place EsriRedlands = new Place(-117.195700, 34.056495, "Esri, Redlands");
		private static readonly Place LadyLiberty = new Place(-74.044540, 40.689247, "Statue of Liberty, New York");
		private static readonly Place SydneyOpera = new Place(151.214366, -33.857499, "Sydney Opera House, Sydney");
		private static readonly Place Anadyr = new Place(177.516667, 64.733333, "Anadyr");
		private static readonly Place Josefstrasse21 = new Place(8.533677, 47.380877, "Josefstrasse 21, 8005 Zürich");
		private static readonly Place Josefstrasse211 = new Place(8.521943, 47.387117, "Josefstrasse 211, 8005 Zürich");
		private static readonly Place Josefstrasse212 = new Place(8.523117, 47.387069, "Josefstrasse 212, 8005 Zürich");
		private static readonly Place Josefstrasse214 = new Place(8.523265, 47.387221, "Josefstrasse 214, 8005 Zürich");
		private static readonly Place Josefstrasse218 = new Place(8.522689, 47.387523, "Josefstrasse 218, 8005 Zürich");
		private static readonly Place Josefstrasse216 = new Place(8.5225, 47.387354, "Josefstrasse 216, 8005 Zürich");
		private static readonly Place KigaliAirport = new Place(30.134993, -1.963173, "Kigali Airport, Kigali");
		private static readonly Place StMatthewIsland = new Place(-172.72, 60.4086, "St. Matthew Island");
		private static readonly Place Longyearbyen = new Place(16.6497, 78.2194, "Longyearbyen, Spitsbergen");

		private static PointQuadtree<Place> GetGlobalDataset()
		{
			var places = new[]
			{
				FernsehturmBerlin, ElizabethTowerLondon, NotreDameTourNord, NotreDameTourSud,
				EiffelTower, EsriRedlands, LadyLiberty, SydneyOpera, Anadyr, Josefstrasse21,
				Josefstrasse211, Josefstrasse212, Josefstrasse214, Josefstrasse216,
				Josefstrasse218, KigaliAirport, StMatthewIsland, Longyearbyen
			};

			var quadtree = new PointQuadtree<Place>();

			foreach (var place in places)
			{
				quadtree.Add(place, place.Point);
			}

			quadtree.Build();

			return quadtree;
		}

		#endregion

		#region Test utils

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}

		private static string GetSerializedQuadtree(PointQuadtree<Place> qt)
		{
			var buffer = new StringBuilder();
			using (var writer = new StringWriter(buffer))
			{
				qt.Dump(writer);
			}
			return buffer.ToString();
		}

		private void ShowStats<T>(PointQuadtree<T> qt)
		{
			if (qt.IsDirty) qt.Build();

			_output.WriteLine($"Items: {qt.ItemCount}, Depth: {qt.MeanDepth} mean {qt.MaxDepth} max");
		}

		#endregion

		private class Place : IComparable<Place>
		{
			public string Tag { get; }
			public Point Point { get; }

			public Place(double x, double y, string tag)
			{
				if (tag == null)
					throw new ArgumentNullException(nameof(tag));

				Tag = tag;
				Point = new Point(x, y);
			}

			public int CompareTo(Place other)
			{
				if (ReferenceEquals(other, null))
				{
					return 1; // null sorts first
				}

				return string.Compare(Tag, other.Tag);
			}

			public override string ToString()
			{
				return Tag;
			}
		}
	}
}
