using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class ListUtilsTest
	{
		private readonly ITestOutputHelper _output;

		public ListUtilsTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void SwapTest()
		{
			IList<int> list = new[] { 1, 2, 3 };

			ListUtils.Swap(list, 0, 1);
			ListUtils.Swap(list, 0, 2);

			list.Swap(1, 1); // as extension

			Assert.Equal(MakeArray(3, 1, 2), list);
		}

		[Fact]
		public void BinarySearchEmptyTest()
		{
			int[] empty = {};

			Assert.Equal(0, ~empty.BinarySearch(5));
			Assert.Equal(0, ~empty.BinarySearch(-2));
		}

		[Fact]
		public void BinarySearchTest()
		{
			int[] array = {2, 4, 7, 11, 12};

			Assert.Equal(0, ~array.BinarySearch(1));
			Assert.Equal(0, array.BinarySearch(2));
			Assert.Equal(1, ~array.BinarySearch(3));
			Assert.Equal(1, array.BinarySearch(4));
			Assert.Equal(2, ~array.BinarySearch(5));
			Assert.Equal(2, ~array.BinarySearch(6));
			Assert.Equal(2, array.BinarySearch(7));
			Assert.Equal(3, ~array.BinarySearch(8));
			Assert.Equal(3, ~array.BinarySearch(9));
			Assert.Equal(3, ~array.BinarySearch(10));
			Assert.Equal(3, array.BinarySearch(11));
			Assert.Equal(4, array.BinarySearch(12));
			Assert.Equal(5, ~array.BinarySearch(13));

			Assert.Equal(2, array.BinarySearch(2, 3, 7));
			Assert.Equal(3, ~array.BinarySearch(2, 3, 9));
		}

		[Fact]
		public void RotateTest()
		{
			int[] array = { 1, 2, 3, 4, 5 };
			// Rotate left 3 steps:
			int[] expected1 = { 4, 5, 1, 2, 3 };
			// Rotate right 1 step:
			int[] expected2 = { 3, 4, 5, 1, 2 };
			// Rotate left 4 steps:
			int[] expected3 = { 2, 3, 4, 5, 1 };
			// Rotate right 2 steps:
			int[] expected4 = { 5, 1, 2, 3, 4 };
			// Rotate subarray [1..3] left 1 step:
			int[] expected5 = { 5, 2, 3, 1, 4 };
			// Rotate subarray [3..4] right 4 steps:
			int[] expected6 = { 5, 2, 3, 1, 4 };

			array.Rotate(3);
			Assert.Equal(expected1, array);

			array.Rotate(-1);
			Assert.Equal(expected2, array);

			array.Rotate(9);
			Assert.Equal(expected3, array);

			array.Rotate(-12);
			Assert.Equal(expected4, array);

			// Rotate array[1..3], leave array[0] and array[4]:
			array.Rotate(1, 1, 3);
			Assert.Equal(expected5, array);

			array.Rotate(4, 3, 2);
			Assert.Equal(expected6, array);
		}

		[Fact]
		public void RotateEmptyTest()
		{
			var empty = new int[0];

			empty.Rotate(0);
			empty.Rotate(1);
			empty.Rotate(-5);
		}

		[Fact]
		public void ReverseEmptyTest()
		{
			var empty = (IList<int>) new List<int>();

			empty.Reverse();
			empty.Reverse(0, 0);

			Assert.Empty(empty);
		}

		[Fact]
		public void ReverseTest()
		{
			IList<int> list0 = new int[0];
			ListUtils.Reverse(list0, 0, 0);
			Assert.Equal(0, list0.Count);

			IList<int> list1 = new[] {1};
			ListUtils.Reverse(list1, 0, 1);
			Assert.Equal(MakeArray(1), list1);

			IList<int> list2 = new[] {1, 2};
			ListUtils.Reverse(list2);
			ListUtils.Reverse(list2, 0, 1);
			ListUtils.Reverse(list2, 1, 1);
			Assert.Equal(MakeArray(2, 1), list2);

			IList<int> list3 = new[] {1, 2, 3};
			ListUtils.Reverse(list3, 0, 2);
			ListUtils.Reverse(list3, 1, 2);
			ListUtils.Reverse(list3);
			Assert.Equal(MakeArray(1, 3, 2), list3);
		}

		[Fact]
		public void IndexedSortEmptyTest()
		{
			var empty = new List<int>();

			ListUtils.Sort(empty, CompareIndexed);

			Assert.Empty(empty);
		}

		[Fact]
		public void IndexedSortTest()
		{
			var one = new[] { 77 };
			var two = new[] { 4, 2 };
			var three = new[] { 2, 3, 1 };
			var four = new[] { 4, 3, 2, 1 };
			var five = new[] { 4, 5, 3, 7, 1 };
			var ties = new[] {6, 3, 7, 3, 3, 5, 6};

			ListUtils.Sort(one, CompareIndexed);
			Assert.Equal(MakeArray(77), one);

			ListUtils.Sort(two, CompareIndexed);
			Assert.Equal(MakeArray(2, 4), two);
			ListUtils.Sort(two, CompareIndexed); // sorted
			Assert.Equal(MakeArray(2, 4), two);

			ListUtils.Sort(three, CompareIndexed);
			Assert.Equal(MakeArray(1, 2, 3), three);
			ListUtils.Sort(three, CompareIndexed); // sorted
			Assert.Equal(MakeArray(1, 2, 3), three);

			ListUtils.Sort(four, CompareIndexed);
			Assert.Equal(MakeArray(1, 2, 3, 4), four);
			ListUtils.Sort(four, CompareIndexed); // sorted
			Assert.Equal(MakeArray(1, 2, 3, 4), four);

			ListUtils.Sort(five, CompareIndexed);
			Assert.Equal(MakeArray(1, 3, 4, 5, 7), five);
			ListUtils.Sort(five, CompareIndexed); // sorted
			Assert.Equal(MakeArray(1, 3, 4, 5, 7), five);

			ListUtils.Sort(ties, CompareIndexed);
			Assert.Equal(MakeArray(3, 3, 3, 5, 6, 6, 7), ties);
			ListUtils.Sort(ties, CompareIndexed); // sorted
			Assert.Equal(MakeArray(3, 3, 3, 5, 6, 6, 7), ties);
		}

		private static int CompareIndexed(IList<int> list, int i1, int i2)
		{
			int x1 = list[i1];
			int x2 = list[i2];
			return Math.Sign(x1 - x2);
		}

		[Fact]
		public void ElementSortEmptyTest()
		{
			var empty = new List<int>();

			ListUtils.Sort(empty, CompareElement);

			Assert.Empty(empty);
		}

		[Fact]
		public void ElementSortTest()
		{
			var one = new[] { 77 };
			var two = new[] { 4, 2 };
			var three = new[] { 2, 3, 1 };
			var four = new[] { 4, 3, 2, 1 };
			var five = new[] { 4, 5, 3, 7, 1 };
			var ties = new[] { 6, 3, 7, 3, 3, 5, 6 };

			ListUtils.Sort(one, CompareElement);
			Assert.Equal(MakeArray(77), one);

			ListUtils.Sort(two, CompareElement);
			Assert.Equal(MakeArray(2, 4), two);
			ListUtils.Sort(two, CompareElement); // sorted
			Assert.Equal(MakeArray(2, 4), two);

			ListUtils.Sort(three, CompareElement);
			Assert.Equal(MakeArray(1, 2, 3), three);
			ListUtils.Sort(three, CompareElement); // sorted
			Assert.Equal(MakeArray(1, 2, 3), three);

			ListUtils.Sort(four, CompareElement);
			Assert.Equal(MakeArray(1, 2, 3, 4), four);
			ListUtils.Sort(four, CompareElement); // sorted
			Assert.Equal(MakeArray(1, 2, 3, 4), four);

			ListUtils.Sort(five, CompareElement);
			Assert.Equal(MakeArray(1, 3, 4, 5, 7), five);
			ListUtils.Sort(five, CompareElement); // sorted
			Assert.Equal(MakeArray(1, 3, 4, 5, 7), five);

			ListUtils.Sort(ties, CompareElement);
			Assert.Equal(MakeArray(3, 3, 3, 5, 6, 6, 7), ties);
			ListUtils.Sort(ties, CompareElement); // sorted
			Assert.Equal(MakeArray(3, 3, 3, 5, 6, 6, 7), ties);
		}

		private static int CompareElement(int x1, int x2)
		{
			return Math.Sign(x1 - x2);
		}

		[Fact]
		public void SortSublistTest()
		{
			var list = new[] { 5, 3, 7, 2, 9, 3 };

			ListUtils.Sort(list, 0, 0, CompareIndexed);
			Assert.Equal(MakeArray(5, 3, 7, 2, 9, 3), list);
			ListUtils.Sort(list, 6, 0, CompareIndexed);
			Assert.Equal(MakeArray(5, 3, 7, 2, 9, 3), list);

			ListUtils.Sort(list, 4, 2, CompareIndexed);
			Assert.Equal(MakeArray(5, 3, 7, 2, 3, 9), list);
			ListUtils.Sort(list, 4, 2, CompareIndexed);
			Assert.Equal(MakeArray(5, 3, 7, 2, 3, 9), list);

			ListUtils.Sort(list, 0, 3, CompareElement);
			Assert.Equal(MakeArray(3, 5, 7, 2, 3, 9), list);
			ListUtils.Sort(list, 0, 3, CompareIndexed);
			Assert.Equal(MakeArray(3, 5, 7, 2, 3, 9), list);

			ListUtils.Sort(list, 0, list.Length, CompareElement);
			Assert.Equal(MakeArray(2, 3, 3, 5, 7, 9), list);
			ListUtils.Sort(list, 0, list.Length, CompareElement);
			Assert.Equal(MakeArray(2, 3, 3, 5, 7, 9), list);
		}

		[Fact]
		public void SortPerformanceTest()
		{
			const int repeatCount = 1000;
			const int listSize = 1000;
			var maxTime = TimeSpan.FromSeconds(1.0);

			var random = new Random();
			IList<int> source = new List<int>(listSize);
			for (int i = 0; i < listSize; i++)
			{
				source.Add(random.Next(0, listSize));
			}

			var temp = new int[source.Count];

			DateTime startTime = DateTime.Now;
			for (int i = 0; i < repeatCount; i++)
			{
				source.CopyTo(temp, 0);
				ListUtils.Sort(temp, CompareIndexed);
			}
			TimeSpan indexedTime = DateTime.Now - startTime;
			_output.WriteLine(@"Sorting {0} times {1} ints using CompareIndexed: {2}",
							  repeatCount, temp.Length, indexedTime);

			startTime = DateTime.Now;
			for (int i = 0; i < repeatCount; i++)
			{
				source.CopyTo(temp, 0);
				ListUtils.Sort(temp, CompareElement);
			}
			TimeSpan elementTime = DateTime.Now - startTime;
			_output.WriteLine(@"Sorting {0} times {1} ints using CompareElement: {2}",
							  repeatCount, temp.Length, elementTime);

			Assert.True(indexedTime < maxTime, "sort too slow");
			Assert.True(elementTime < maxTime, "sort too slow");
		}

		private static T[] MakeArray<T>(params T[] args)
		{
			return args;
		}
	}
}
