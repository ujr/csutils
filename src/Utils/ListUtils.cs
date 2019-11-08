using System;
using System.Collections.Generic;

namespace Sylphe.Utils
{
	/// <summary>
	/// Use the utility methods here if you have an
	/// <see cref="IList{T}"/> that is neither a
	/// <see cref="List{T}"/> nor an <see cref="Array"/>.
	/// </summary>
	public static class ListUtils
	{
		/// <summary>
		/// Search for <paramref name="item"/> in <paramref name="list"/>
		/// using the given (or default) <paramref name="comparer"/>.
		/// See <see cref="BinarySearch{T}(IList{T}, int, int, T, IComparer{T})"/>.
		/// </summary>
		public static int BinarySearch<T>(this IList<T> list, T item, IComparer<T> comparer = null)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			return BinarySearch(list, 0, list.Count, item, comparer);
		}

		/// <summary>
		/// Search for the given <paramref name="item"/> in the sublist of
		/// <paramref name="list"/> defined by <paramref name="index"/> and
		/// <paramref name="count"/>. Compare items using the given
		/// <paramref name="comparer"/> or, if <paramref name="comparer"/>
		/// is <c>null</c>, the item type's default comparer.
		/// <para/>
		/// Assume the sublist is sorted according to the given (or default) comparer.
		/// <para/>
		/// If the <paramref name="item"/> is found, return its index.
		/// If it is not found, return a negative integer, whose bitwise complement
		/// is the index where <paramref name="item"/> would have to be inserted
		/// in order to maintain the list's ordering.
		/// </summary>
		public static int BinarySearch<T>(this IList<T> list, int index, int count, T item, IComparer<T> comparer = null)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			if (index < 0 || count < 0 || index + count > list.Count)
				throw new ArgumentException("index and/or count out of range");

			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}

			int lo = index;
			int hi = index + count - 1;

			while (lo <= hi)
			{
				int mid = lo + ((hi - lo) >> 1);
				int order = comparer.Compare(list[mid], item);
				if (order == 0) return mid;
				if (order < 0)
				{
					lo = mid + 1;
				}
				else
				{
					hi = mid - 1;
				}
			}

			return ~lo;
		}

		public static void Reverse<T>(this IList<T> list)
		{
			Reverse(list, 0, list.Count);
		}

		public static void Reverse<T>(this IList<T> list, int index, int count)
		{
			for (int lo = index, hi = index + count - 1; lo < hi; lo++, hi--)
			{
				var temp = list[lo];
				list[lo] = list[hi];
				list[hi] = temp;
			}
		}

		public static void Rotate<T>(this IList<T> list, int steps)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			Rotate(list, steps, 0, list.Count);
		}

		public static void Rotate<T>(this IList<T> list, int steps, int index, int count)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			if (index < 0 || index > list.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (count < 0 || (index + count) > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0 || steps == 0)
			{
				return; // nothing to do
			}

			steps = steps%count;

			if (steps < 0)
			{
				steps += count;
			}

			Reverse(list, index, steps);
			Reverse(list, index + steps, count - steps);
			Reverse(list, index, count);
		}

        public static void Sort<T>(this IList<T> list, Func<IList<T>, int, int, int> compare)
        {
            QuickSort(0, list.Count - 1, list, compare);
        }

		public static void Sort<T>(this IList<T> list, int startIndex, int count, Func<IList<T>, int, int, int> compare)
		{
			QuickSort(startIndex, startIndex + count - 1, list, compare);
		}

        public static void Sort<T>(this IList<T> list, Func<T, T, int> compare)
        {
            QuickSort(0, list.Count - 1, list, compare);
        }

		public static void Sort<T>(this IList<T> list, int startIndex, int count, Func<T, T, int> compare)
		{
			QuickSort(startIndex, startIndex + count - 1, list, compare);
		}

		public static void Swap<T>(this IList<T> list, int i1, int i2)
		{
			if (i1 == i2) return;

			T temp = list[i1];
			list[i1] = list[i2];
			list[i2] = temp;
		}

		#region Non-public methods

		private static void QuickSort<T>(int left, int right, IList<T> list, Func<IList<T>, int, int, int> compare)
		{
			if (left < right)
			{
				// Partition list[left..right] using list[right] as pivot:
				int i = left, j = right - 1;
				for (; ; )
				{
					while (compare(list, i, right) <= 0 && i < right) ++i;
					while (compare(list, j, right) >= 0 && j > i) --j;
					if (i >= j) break;
					Swap(list, i, j);
				}

				Swap(list, i, right); // move the pivot into place

				QuickSort(left, i - 1, list, compare);
				QuickSort(i + 1, right, list, compare);
			}
		}

		private static void QuickSort<T>(int left, int right, IList<T> list, Func<T,T,int> compare)
		{
			if (left < right)
			{
				// Partition list[left..right] using list[right] as pivot:
				T pivot = list[right];
				int i = left, j = right - 1;
				for (; ; )
				{
					while (compare(list[i], pivot) <= 0 && i < right) ++i;
					while (compare(list[j], pivot) >= 0 && j > i) --j;
					if (i >= j) break;
					Swap(list, i, j);
				}

				Swap(list, i, right); // move the pivot into place

				QuickSort(left, i - 1, list, compare);
				QuickSort(i + 1, right, list, compare);
			}
		}

		#endregion
	}
}
