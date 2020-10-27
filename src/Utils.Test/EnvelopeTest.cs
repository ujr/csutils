using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class EnvelopeTest
	{
		private readonly ITestOutputHelper _output;

		public EnvelopeTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void CanCreate()
		{
			var p = new Point(1, 2);
			var q = new Point(3, 4);

			Assert.Equal(new Envelope(1, 2, 1, 2), new Envelope(p));
			Assert.Equal(new Envelope(1, 2, 1, 2), new Envelope(1, 2));
			Assert.Equal(new Envelope(1, 2, 3, 4), new Envelope(p, q));
			Assert.Equal(new Envelope(1, 2, 3, 4), new Envelope(q, p));
			Assert.Equal(new Envelope(1, 2, 3, 4), new Envelope(3, 2, 1, 4));
			Assert.Equal(new Envelope(3, 2, 1, 4, true), new Envelope(3, 2, 1, 4, true));
			Assert.Equal(new Envelope(1, 2, 3, 4), new Envelope(new Envelope(1, 2, 3, 4)));
		}

		[Fact]
		public void CanCreateBox()
		{
			var box1 = Envelope.Create(Enumerable.Empty<Point>());
			Assert.True(box1.IsEmpty);

			var box2 = Envelope.Create(Enumerable.Repeat(new Point(1, 2), 1));
			Assert.False(box2.IsEmpty);
			Assert.Equal(new Envelope(1, 2, 1, 2), box2);

			var pts = RandomPoints(10).ToList();
			var box3 = Envelope.Create(pts);
			var expected = new Envelope(
				pts.Min(p => p.X), pts.Min(p => p.Y),
				pts.Max(p => p.X), pts.Max(p => p.Y));
			Assert.Equal(expected, box3);
		}

		[Fact]
		public void CanEmpty()
		{
			var pt = new Envelope(1, 1, 1, 1);
			Assert.False(pt.IsEmpty); // closed interval: not empty

			Assert.True(Envelope.Empty.IsEmpty);
		}

		[Fact]
		public void CanEquals()
		{
			var a = new Envelope(10, 10, 20, 20);
			var b = new Envelope(10, 10, 20, 20);
			var c = new Envelope(11, 11, 22, 22);

			Assert.True(a.Equals(b));
			Assert.False(b.Equals(c));

			Assert.False(c.Equals(Envelope.Empty));
			Assert.False(Envelope.Empty.Equals(a));
			Assert.True(Envelope.Empty.Equals(Envelope.Empty));
		}

		[Fact]
		public void CanContains()
		{
			var punctual = new Envelope(10, 10, 10, 10);
			Assert.True(punctual.Contains(new Point(10, 10)));

			var envelope = new Envelope(10, 10, 20, 20);
			Assert.True(envelope.Contains(new Point(15, 15))); // inside
			Assert.True(envelope.Contains(new Point(15, 10))); // boundary
			Assert.False(envelope.Contains(new Point(20, 20.0001))); // outside

			Assert.True(envelope.Contains(14, 16));
		}

		[Fact]
		public void CanIntersect()
		{
			var a = new Envelope(10, 10, 20, 20);
			var b = new Envelope(15, 15, 25, 25);
			var c = new Envelope(0, 0, 5, 5);

			Assert.Equal(new Envelope(15, 15, 20, 20), a.Intersect(b));
			Assert.Equal(new Envelope(15, 15, 20, 20), b.Intersect(a));

			Assert.True(a.Intersect(c).IsEmpty);

			Assert.True(a.Intersects(b));
			Assert.True(b.Intersects(a));
			Assert.False(b.Intersects(c));
			Assert.False(c.Intersects(b));
		}

		[Fact]
		public void CanExpand()
		{
			var e0 = new Envelope(10, 10, 20, 20);

			var e1 = e0.Expand(15, 15);
			Assert.Equal(e0, e1);

			var e2 = e1.Expand(20, 20);
			Assert.Equal(e0, e2);

			var e3 = e2.Expand(25, 25);
			Assert.Equal(new Envelope(10, 10, 25, 25), e3);

			var e4 = e3.Expand(new Point(30, 30));
			Assert.Equal(new Envelope(10, 10, 30, 30), e4);

			var e5 = e4.Expand(new Envelope(0, 0, 5, 5));
			Assert.Equal(new Envelope(0, 0, 30, 30), e5);

			Assert.Equal(new Envelope(5, 5), Envelope.Empty.Expand(5, 5));
			Assert.Equal(new Envelope(5, 5), new Envelope(5, 5).Expand(Envelope.Empty));
		}

		[Fact]
		public void CanExpandFactor()
		{
			var e0 = new Envelope(10, 10, 20, 20);

			var e1 = e0.Expand(0.0);
			Assert.Equal(new Envelope(10, 10, 20, 20), e1);

			var e2 = e1.Expand(1.0);
			Assert.Equal(new Envelope(5, 5, 25, 25), e2);

			var e3 = e2.Expand(-0.5);
			Assert.Equal(new Envelope(10, 10, 20, 20), e3);
		}

		[Fact(Skip = "Performance trial")]
		public void EnvelopeCopySpeedComparison()
		{
			const int count = 1000 * 1000;
			var points = RandomPoints(count).ToList();

			// This is a typical pattern to get the bounding box of some points:

			var timer = Stopwatch.StartNew();
			Envelope bbox1 = null;

			foreach (var point in points)
			{
				bbox1 = bbox1 == null ? new Envelope(point) : bbox1.Expand(point);
			}

			var elapsed1 = timer.ElapsedMilliseconds;

			// Using a special factory method is about 5 times faster:

			timer.Restart();
			var bbox2 = Envelope.Create(points);
			var elapsed2 = timer.ElapsedMilliseconds;

			_output.WriteLine(@"BBox of {0:N0} points: ", count);
			_output.WriteLine(@"  with bbox=bbox.Expand(pt) {0} ms", elapsed1);
			_output.WriteLine(@"  with Envelope.Create(pts) {0} ms", elapsed2);
			_output.WriteLine(@"The latter is {0:N0}% of the former", 100.0*elapsed2/elapsed1);

			// Of course, the two results should be the same:
			Assert.Equal(bbox2, bbox1);
		}

		private static IEnumerable<Point> RandomPoints(int count)
		{
			var random = new Random();

			const double dx = 360.0;
			const double dy = 180.0;

			for (int i = 0; i < count; i++)
			{
				double x = random.NextDouble() * dx - dx / 2;
				double y = random.NextDouble() * dy - dy / 2;
				yield return new Point(x, y);
			}
		}
	}
}
