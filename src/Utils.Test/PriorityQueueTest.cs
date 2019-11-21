using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// xUnit.net issues warnings to encourage specific collection related
// assertions (e.g. Assert.Contains and Assert.Empty). Disable them:
// here we want to specifically test pq.Count and pq.Contains().
#pragma warning disable xUnit2013
#pragma warning disable xUnit2017

namespace Sylphe.Utils.Test
{
	public class PriorityQueueTest
	{
		private readonly ITestOutputHelper _output;

		public PriorityQueueTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void CanCreateSubclass()
		{
			var pq1 = new TestPriorityQueue<char>(); //  unbounded
			Assert.Equal(TestPriorityQueue<char>.Unbounded, pq1.Capacity);
			Assert.Equal(0, pq1.Count);

			var pq2 = new TestPriorityQueue<char>(5); // fixed capacity
			Assert.Equal(5, pq2.Capacity);
			Assert.Equal(0, pq2.Count);
		}

		[Fact]
		public void CanFixedCapacity0()
		{
			const int capacity = 0;
			var pq = new TestPriorityQueue<char>(capacity);

			Assert.Equal(0, pq.Capacity);
			Assert.Equal(0, pq.Count);

			Assert.Throws<InvalidOperationException>(() => pq.Top()); // queue empty
			Assert.Throws<InvalidOperationException>(() => pq.Pop()); // queue empty

			// With capacity zero, all additions immediately overflow:
			Assert.Equal('z', pq.AddWithOverflow('z'));
			Assert.Equal('a', pq.AddWithOverflow('a'));
			Assert.Throws<InvalidOperationException>(() => pq.Add('b')); // queue full
		}

		[Fact]
		public void CanFixedCapacity6()
		{
			// Capacity 6: incomplete binary tree
			const int capacity = 6;
			var pq = new TestPriorityQueue<char>(capacity);

			Assert.Equal(6, pq.Capacity);
			Assert.Equal(0, pq.Count);

			pq.Add('e');
			Assert.Equal('e', pq.Top());
			pq.Add('b');
			Assert.Equal('b', pq.Top());
			pq.Add('c');
			Assert.Equal('b', pq.Top());
			pq.Add('a');
			Assert.Equal('a', pq.Top());
			pq.Add('f');
			Assert.Equal('a', pq.Top());
			pq.Add('d');
			Assert.Equal('a', pq.Top());

			Assert.Equal(6, pq.Count);

			Assert.Equal('a', pq.AddWithOverflow('x'));
			Assert.Equal('a', pq.AddWithOverflow('a'));

			Assert.Equal(6, pq.Count);

			Assert.Equal('b', pq.Top());
			Assert.Equal('b', pq.Pop());
			Assert.Equal('c', pq.Pop());
			Assert.Equal('d', pq.Pop());
			Assert.Equal('e', pq.Pop());
			Assert.Equal('f', pq.Pop());
			Assert.Equal('x', pq.Pop());

			Assert.Equal(0, pq.Count);

			Assert.Throws<InvalidOperationException>(() => pq.Pop()); // queueu empty
		}

		[Fact]
		public void CanFixedCapacity7()
		{
			// Capacity 7: complete binary tree
			const int capacity = 7;
			var pq = new TestPriorityQueue<char>(capacity);

			Assert.Equal(7, pq.Capacity);
			Assert.Equal(0, pq.Count);

			pq.Add('e');
			Assert.Equal('e', pq.Top());
			pq.Add('g');
			Assert.Equal('e', pq.Top());
			pq.Add('b');
			Assert.Equal('b', pq.Top());
			pq.Add('c');
			Assert.Equal('b', pq.Top());
			pq.Add('a');
			Assert.Equal('a', pq.Top());
			pq.Add('f');
			Assert.Equal('a', pq.Top());
			pq.Add('d');
			Assert.Equal('a', pq.Top());

			Assert.Equal(7, pq.Count);
			Assert.Equal('a', pq.Top());

			Assert.Equal('a', pq.AddWithOverflow('x')); // 'a' drops out
			Assert.Equal('a', pq.AddWithOverflow('a')); // 'a' never gets in...

			Assert.Equal(7, pq.Count);

			Assert.Equal('b', pq.Top());
			Assert.Equal('b', pq.Pop());
			Assert.Equal('c', pq.Pop());
			Assert.Equal('d', pq.Pop());
			Assert.Equal('e', pq.Pop());
			Assert.Equal('f', pq.Pop());
			Assert.Equal('g', pq.Pop());
			Assert.Equal('x', pq.Top());
			Assert.Equal('x', pq.Pop());

			Assert.Equal(0, pq.Count);

			Assert.Throws<InvalidOperationException>(() => pq.Pop()); // 	queue empty
		}

		[Fact]
		public void CanCopeWithTies()
		{
			var pq = new TestPriorityQueue<char>(5);

			pq.Add('x');
			pq.Add('y');

			Assert.Equal(2, pq.Count);

			pq.Add('x'); // again

			Assert.Equal(3, pq.Count);

			Assert.Equal('x', pq.Pop());
			Assert.Equal('x', pq.Pop()); // again!
			Assert.Equal('y', pq.Pop());

			Assert.Equal(0, pq.Count);
		}

		[Fact]
		public void CanUnboundedCapacity()
		{
			const int count = 1000; // will trigger reallocations

			var items = Enumerable.Range(1, count).ToList();
			Shuffle(items, new Random(1234));

			var pq = new TestPriorityQueue<int>();

			foreach (var i in items)
			{
				pq.Add(i);
			}

			Assert.Equal(count, pq.Count);

			var popped = new List<int>();
			while (pq.Count > 0)
			{
				popped.Add(pq.Pop());
			}

			var expected = Enumerable.Range(1, count);
			Assert.Equal(expected, popped);
		}

		[Fact]
		public void CannotPopEmpty()
		{
			var pq = new TestPriorityQueue<char>();

			Assert.Throws<InvalidOperationException>(() => pq.Pop()); // queue empty

			pq.Add('a');
			pq.Pop();

			Assert.Throws<InvalidOperationException>(() => pq.Pop()); // queue empty
		}

		[Fact]
		public void CannotTopEmpty()
		{
			var pq = new TestPriorityQueue<char>();

			Assert.Throws<InvalidOperationException>(() => pq.Top()); // queue empty

			pq.Add('a');
			pq.Pop();

			Assert.Throws<InvalidOperationException>(() => pq.Top()); // queue empty
		}

		[Fact]
		public void CannotAddFull()
		{
			const int capacity = 5;
			var pq = new TestPriorityQueue<char>(capacity);

			pq.Add('a');
			pq.Add('b');
			pq.Add('c');
			pq.Add('d');
			pq.Add('e');

			Assert.Equal(capacity, pq.Count); // queue is now full

			Assert.Throws<InvalidOperationException>(() => pq.Add('x')); // queue full
		}

		[Fact]
		public void CanEnumerateFixedQueue()
		{
			var pq1 = new TestPriorityQueue<char>(5); // fixed capacity

			pq1.AddWithOverflow('f');
			pq1.AddWithOverflow('u');
			pq1.AddWithOverflow('b');
			pq1.AddWithOverflow('a');
			pq1.AddWithOverflow('r');

			var enumerator = pq1.GetEnumerator();

			var list1 = Iterate(enumerator);
			Assert.Equal("abfru", new string(list1.OrderBy(c => c).ToArray()));

			pq1.Clear();
			pq1.Add('f');
			pq1.Add('o');
			pq1.Add('o');

			enumerator.Reset();

			var list2 = Iterate(enumerator);
			Assert.Equal("foo", new string(list2.OrderBy(c => c).ToArray()));

			enumerator.Dispose();
		}

		[Fact]
		public void CanEnumerateGrowingQueue()
		{
			var pq = new TestPriorityQueue<char>(); // unbounded

			pq.Add('f');
			pq.Add('u');
			pq.Add('b');
			pq.Add('a');
			pq.Add('r');

			var enumerator = pq.GetEnumerator();

			var list1 = Iterate(enumerator);
			Assert.Equal("abfru", new string(list1.OrderBy(c => c).ToArray()));

			list1.Clear();
			pq.Pop(); // modify collection
			enumerator.Reset();

			var list2 = Iterate(enumerator);
			Assert.Equal("bfru", new string(list2.OrderBy(c => c).ToArray()));

			enumerator.Dispose();
		}

		[Fact]
		public void CanEnumerateFixedEmptyQueue()
		{
			var pq = new TestPriorityQueue<char>(0); // fixed capacity
			var enumerator = pq.GetEnumerator();

			Assert.Empty(Iterate(enumerator));

			pq.AddWithOverflow('x');
			Assert.Equal(0, pq.Count); // still empty
			enumerator.Reset();

			Assert.Empty(Iterate(enumerator));

			enumerator.Dispose();
		}

		[Fact]
		public void CanEnumerateGrowingEmptyQueue()
		{
			var pq = new TestPriorityQueue<char>(); // unbounded
			var enumerator = pq.GetEnumerator();

			Assert.Empty(Iterate(enumerator));

			pq.Add('x');
			pq.Pop(); // empty again
			enumerator.Reset();

			Assert.Empty(Iterate(enumerator));

			enumerator.Dispose();
		}

		[Fact]
		public void CannotEnumerateChangingQueue()
		{
			const string items = "hello";
			var pq = new TestPriorityQueue<char>(items.Length + 1);

			foreach (var c in items)
			{
				pq.Add(c);
			}

			var iter = pq.GetEnumerator();

			Assert.True(iter.MoveNext());
			pq.Add('x'); // modify (will fill up capacity)
			Assert.Throws<InvalidOperationException>(() => iter.MoveNext());

			iter.Reset();

			Assert.True(iter.MoveNext());
			pq.AddWithOverflow('y'); // modify (will overflow)
			Assert.Throws<InvalidOperationException>(() => iter.MoveNext());

			iter.Reset();

			Assert.True(iter.MoveNext());
			pq.Pop(); // modify
			Assert.Throws<InvalidOperationException>(() => iter.MoveNext());

			iter.Reset();

			Assert.True(iter.MoveNext());
			pq.Clear(); // modify
			Assert.Throws<InvalidOperationException>(() => iter.MoveNext());

			iter.Dispose();
		}

		[Fact]
		public void CanCopyTo()
		{
			var pq = new TestPriorityQueue<char>();
			foreach (char c in "hello")
			{
				pq.Add(c);
			}

			var array = new char[pq.Count];
			pq.CopyTo(array, 0);

			// The items in the array are in "heap array order",
			// but that's an undocumented implementation detail;
			// officially the ordering is undefined, so sort:

			Array.Sort(array);
			Assert.Equal("ehllo".ToCharArray(), array);
		}

		[Fact]
		public void CanContains()
		{
			var pq = new TestPriorityQueue<char>();
			foreach (char c in "hello")
			{
				pq.Add(c);
			}

			Assert.True(pq.Contains('h'));
			Assert.True(pq.Contains('e'));
			Assert.True(pq.Contains('l'));
			Assert.True(pq.Contains('o'));
			Assert.False(pq.Contains('x'));
		}

		[Fact]
		public void CanRemove()
		{
			var pq = new TestPriorityQueue<char>();
			foreach (char c in "abcdefghijklmnopqrstuvwxyz")
			{
				pq.Add(c);
			}

			Assert.False(pq.Remove('$')); // no such item
			Assert.Equal(26, pq.Count);

			// Last item: easy to remove
			Assert.True(pq.Remove('z'));
			Assert.Equal(25, pq.Count);
			Assert.Equal('a', pq.Top());
			Assert.False(pq.Contains('z'));

			// Remove a bottom row item:
			Assert.True(pq.Remove('w'));
			Assert.Equal(24, pq.Count);
			Assert.Equal('a', pq.Top());
			Assert.False(pq.Contains('w'));

			// Remove an inner item:
			Assert.True(pq.Remove('e'));
			Assert.Equal(23, pq.Count);
			Assert.Equal('a', pq.Top());
			Assert.False(pq.Contains('e'));

			// Remove the root item:
			Assert.True(pq.Remove('a'));
			Assert.Equal(22, pq.Count);
			Assert.Equal('b', pq.Top());
			Assert.False(pq.Contains('a'));

			// Remove returns false if not found:
			Assert.False(pq.Remove('z'));
			Assert.False(pq.Remove('w'));
			Assert.False(pq.Remove('e'));
			Assert.False(pq.Remove('a'));

			// Pop remaining items and verify order:
			Assert.Equal('b', pq.Pop());
			Assert.Equal('c', pq.Pop());
			Assert.Equal('d', pq.Pop());
			Assert.Equal('f', pq.Pop());
			Assert.Equal('g', pq.Pop());
			Assert.Equal('h', pq.Pop());
			Assert.Equal('i', pq.Pop());
			Assert.Equal('j', pq.Pop());
			Assert.Equal('k', pq.Pop());
			Assert.Equal('l', pq.Pop());
			Assert.Equal('m', pq.Pop());
			Assert.Equal('n', pq.Pop());
			Assert.Equal('o', pq.Pop());
			Assert.Equal('p', pq.Pop());
			Assert.Equal('q', pq.Pop());
			Assert.Equal('r', pq.Pop());
			Assert.Equal('s', pq.Pop());
			Assert.Equal('t', pq.Pop());
			Assert.Equal('u', pq.Pop());
			Assert.Equal('v', pq.Pop());
			Assert.Equal('x', pq.Pop());
			Assert.Equal('y', pq.Pop());

			Assert.Equal(0, pq.Count);
		}

		[Fact]
		public void PerformanceTest()
		{
			const int capacity = 100*1000;
			const int count = 100*capacity;
			var random = new Random(12345);

			var startTime1 = DateTime.Now;
			var pq1 = new TestPriorityQueue<int>(capacity);

			for (int i = 0; i < count; i++)
			{
				int value = random.Next();
				pq1.AddWithOverflow(value);
			}

			Assert.Equal(Math.Min(capacity, count), pq1.Count);

			while (pq1.Count > 0)
			{
				pq1.Pop();
			}

			var elapsed1 = DateTime.Now - startTime1;

			_output.WriteLine(@"Capacity={0:N0} Count={1:N0} AddWithOverflow/Pop Elapsed={2}",
				capacity, count, elapsed1);

			var startTime2 = DateTime.Now;
			var pq2 = new TestPriorityQueue<int>(); // unbounded

			for (int i = 0; i < count; i++)
			{
				int value = random.Next();
				pq2.Add(value);
			}

			Assert.Equal(count, pq2.Count);

			while (pq2.Count > 0)
			{
				pq2.Pop();
			}

			var elapsed2 = DateTime.Now - startTime2;

			_output.WriteLine(@"Capacity=unbounded Count={0:N0} Add/Pop Elapsed={1}",
				count, elapsed2);

			Assert.True(elapsed1 < TimeSpan.FromSeconds(1), "Too slow");
			Assert.True(elapsed2 < TimeSpan.FromSeconds(12), "Too slow");
		}

		#region Test Utilities

		private class TestPriorityQueue<T> : PriorityQueue<T>
		{
			private readonly IComparer<T> _comparer;

			public TestPriorityQueue(int capacity = Unbounded) : base(capacity)
			{
				_comparer = Comparer<T>.Default;
			}

			protected override bool Priority(T a, T b)
			{
				return _comparer.Compare(a, b) < 0;
			}
		}

		private static IList<T> Iterate<T>(IEnumerator<T> enumerator)
		{
			// Note: to test pq's enumerator, do NOT call pq.ToList(),
			// as this extension method may bypass the enumerator
			// (it seems to use ICollector.CopyTo(), if available).

			var list = new List<T>();

			Assert.Equal(default(T), enumerator.Current);

			while (enumerator.MoveNext())
			{
				list.Add(enumerator.Current);
			}

			// Want Current at default(T) after MoveNext returned false;
			// and MoveNext must not change its mind once it returned false:
			Assert.Equal(default(T), enumerator.Current);
			Assert.False(enumerator.MoveNext());

			return list;
		}

		private static void Shuffle<T>(IList<T> list, Random random)
		{
			// This is Fisher-Yates shuffle.
			int n = list.Count;
			while (n > 1)
			{
				int k = random.Next(n--); // 0 <= k < n
				var temp = list[k];
				list[k] = list[n];
				list[n] = temp;
			}
		}

		#endregion
	}
}
