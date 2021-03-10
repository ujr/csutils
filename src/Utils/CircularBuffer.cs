using System;

namespace Sylphe.Utils
{
	/// <remarks>
	/// This is a simplified version of <see cref="CappedQueue{T}"/>,
	/// supporting only <see cref="Add"/> and <see cref="ToArray"/>.
	/// </remarks>
	public class CircularBuffer<T>
	{
		private int _index;
		private long _count;
		private readonly T[] _array;

		public CircularBuffer(int capacity)
		{
			if (capacity < 1)
			{
				throw new ArgumentOutOfRangeException(
					nameof(capacity), @"Capacity must be at least 1");
			}

			_index = 0;
			_count = 0;
			_array = new T[capacity];
		}

		public void Add(T item)
		{
			_array[_index] = item;
			_index = (_index + 1) % _array.Length;
			_count += 1;
		}

		public T[] ToArray()
		{
			int length = (int) Math.Min(_count, _array.Length);

			var result = new T[length];

			bool full = length == _array.Length;

			if (full)
			{
				int tail = length - _index;

				Array.Copy(_array, 0, result, tail, _index);
				Array.Copy(_array, _index, result, 0, tail);
			}
			else
			{
				Array.Copy(_array, 0, result, 0, length);
			}

			return result;
		}

		public void Clear()
		{
			_index = 0;
			_count = 0;

			// Release all references to allow GC:
			Array.Clear(_array, 0, _array.Length);
		}

		public int Capacity => _array.Length;

		/// <summary>
		/// The number of items added, including those overwritten.
		/// <see cref="Count"/> may be larger than <see cref="Capacity"/>.
		/// </summary>
		public long Count => _count;
	}
}
