using Xunit;

namespace Sylphe.Utils.Test
{
	public class EnvelopeTest
	{
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
	}
}
