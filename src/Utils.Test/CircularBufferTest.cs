using System;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class CircularBufferTest
	{
		[Fact]
		public void CanCreate()
		{
			var b3 = new CircularBuffer<int>(3);
			Assert.Equal(3, b3.Capacity);
			Assert.Equal(0, b3.Count);

			var b1 = new CircularBuffer<int>(1);
			Assert.Equal(1, b1.Capacity);
			Assert.Equal(0, b1.Count);

			Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(-1));
		}

		[Fact]
		public void CanAdd()
		{
			const int capacity = 3;
			var b = new CircularBuffer<int>(capacity);
			b.Add(1);
			b.Add(2);
			b.Add(3);
			b.Add(4);
			b.Add(5);
			b.Add(6);
			b.Add(7);
			Assert.Equal(7, b.Count);
			Assert.Equal(capacity, b.Capacity);

			b.Clear();
			Assert.Equal(0, b.Count);
			Assert.Equal(capacity, b.Capacity);
		}

		[Fact]
		public void CanToArray()
		{
			var b = new CircularBuffer<int>(3);
			Assert.Empty(b.ToArray());

			b.Add(1);
			Assert.Collection(b.ToArray(), IsInt(1));

			b.Add(2);
			Assert.Collection(b.ToArray(), IsInt(1), IsInt(2));

			b.Add(3);
			Assert.Collection(b.ToArray(), IsInt(1), IsInt(2), IsInt(3));

			b.Add(4);
			Assert.Collection(b.ToArray(), IsInt(2), IsInt(3), IsInt(4));
		}

		[Fact]
		public void CanSizeOne()
		{
			var b = new CircularBuffer<int>(1);
			b.Add(1);
			b.Add(2);
			b.Add(3);
			Assert.Equal(1, b.Capacity);
			Assert.Equal(3, b.Count);
			Assert.Collection(b.ToArray(), IsInt(3));
		}

		private static Action<int> IsInt(int val)
		{
			return i => Assert.Equal(val, i);
		}
	}
}
