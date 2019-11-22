using System;
using System.Collections.Generic;
using Xunit;

// xUnit.net issues warnings to encourage specific collection related
// assertions (e.g. Assert.Contains and Assert.Empty). Disable them:
// here we want to specifically test queue.Count and queue.Contains().
#pragma warning disable xUnit2013
#pragma warning disable xUnit2017

namespace Sylphe.Utils.Test
{
	public class CappedQueueTest
	{
		[Fact]
		public void CanCreate()
		{
			var q = new CappedQueue<int>(1);
			Assert.Equal(1, q.Capacity);
			Assert.Equal(0, q.Count);

			Assert.Throws<ArgumentOutOfRangeException>(() => new CappedQueue<int>(0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new CappedQueue<int>(-1));
		}

		[Fact]
		public void CanEnqueue()
		{
			var q1 = new CappedQueue<int>(1);

			q1.Enqueue(1);
			Assert.Equal(1, q1.Count);
			Assert.Equal(1, q1.GetElement(0));

			q1.Enqueue(2);
			Assert.Equal(1, q1.Count);
			Assert.Equal(2, q1.GetElement(0));

			var q2 = new CappedQueue<int>(2);

			q2.Enqueue(1);
			q2.Enqueue(2);
			q2.Enqueue(3);

			Assert.Equal(2, q2.Count);
			Assert.Equal(2, q2.GetElement(0));
			Assert.Equal(3, q2.GetElement(1));
		}

		[Fact]
		public void CanEnumerate()
		{
			var q = new CappedQueue<int>(3);

			Assert.Empty(q);

			q.Enqueue(1);

			Assert.Single(q, 1);

			q.Enqueue(2);

			Assert.Equal(Seq(1, 2), q);

			q.Enqueue(3);

			Assert.Equal(Seq(1, 2, 3), q);

			q.Enqueue(4);

			Assert.Equal(Seq(2, 3, 4), q);
		}

		[Fact]
		public void CanContains()
		{
			var q = new CappedQueue<int>(3);

			q.Enqueue(1);
			q.Enqueue(2);
			q.Enqueue(3);
			q.Enqueue(4);

			Assert.False(q.Contains(1));
			Assert.True(q.Contains(2));
			Assert.True(q.Contains(3));
			Assert.True(q.Contains(4));
			Assert.False(q.Contains(5));

			var qq = new CappedQueue<string>(3);

			qq.Enqueue("one");
			qq.Enqueue("two");
			qq.Enqueue(null);

			Assert.True(qq.Contains("one"));
			Assert.True(qq.Contains("two"));
			Assert.True(qq.Contains(null));
			Assert.False(qq.Contains("three"));
		}

		[Fact]
		public void CanSetCapacity()
		{
			var q = new CappedQueue<int>(1);

			q.Capacity = 2; // enlarge empty queue

			Assert.Equal(0, q.Count);
			Assert.Equal(2, q.Capacity);

			q.Enqueue(1);
			q.Enqueue(2);
			q.Enqueue(3);

			Assert.Equal(2, q.GetElement(0));
			Assert.Equal(3, q.GetElement(1));

			q.Capacity = 3; // enlarge

			Assert.Equal(3, q.Capacity);
			Assert.Equal(2, q.Count);

			Assert.Equal(2, q.GetElement(0));
			Assert.Equal(3, q.GetElement(1));

			q.Enqueue(4);

			Assert.Equal(3, q.Count);
			Assert.Equal(2, q.GetElement(0));
			Assert.Equal(3, q.GetElement(1));
			Assert.Equal(4, q.GetElement(2));

			q.Capacity = 2; // shrink

			Assert.Equal(2, q.Capacity);
			Assert.Equal(2, q.Count);

			Assert.Equal(3, q.GetElement(0));
			Assert.Equal(4, q.GetElement(1));
		}

		[Fact]
		public void CanClear()
		{
			var q = new CappedQueue<int>(3);

			q.Enqueue(1);
			q.Enqueue(2);
			q.Enqueue(3);
			q.Enqueue(4);

			q.Clear();
			q.Clear(); // idempotent

			Assert.Equal(0, q.Count);
			Assert.Equal(3, q.Capacity);
		}

		[Fact]
		public void CanCopyTo()
		{
			var q = new CappedQueue<int>(3);

			q.Enqueue(1);
			q.Enqueue(2);
			q.Enqueue(3);

			var array = new int[5];
			q.CopyTo(array, 2);
			const int zero = default(int);
			Assert.Equal(Seq(zero, zero, 1, 2, 3), array);
		}

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}
	}
}
