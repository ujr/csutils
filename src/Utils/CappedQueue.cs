using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sylphe.Utils
{
	/// <summary>
	/// A queue (first-in-last-out) of limited capacity.
	/// Adding an item to a full queue drops out the oldest item.
	/// Implementation based on an array; <see cref="Enqueue(T)"/>
	/// and <see cref="Dequeue()"/> are O(1) operations.
	/// </summary>
	/// <typeparam name="T">Type of queue items</typeparam>
	[Serializable]
	public class CappedQueue<T> : ICollection<T>, ICloneable
	{
		private int _tail;
		private int _head;
		private int _size;
		private T[] _array;
		private int _version;

		public CappedQueue(int capacity)
		{
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException(
					nameof(capacity), capacity, "Capacity must be at least one");

			_head = 0;
			_tail = 0;
			_size = 0;
			_array = new T[capacity];
			_version = 0;
		}

		public T Dequeue()
		{
			if (_size == 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			T head = _array[_head];

			_array[_head] = default(T);
			_head = (_head + 1) % _array.Length;
			_size -= 1; // one less in queue
			_version += 1;

			return head;
		}

		public void Enqueue(T item)
		{
			if (_size == _array.Length)
			{
				Dequeue(); // drop head to make room for new item
			}

			_array[_tail] = item;
			_tail = (_tail + 1) % _array.Length;
			_size += 1; // one more in queue
			_version += 1;
		}

		public T Peek()
		{
			if (_size == 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			return _array[_head];
		}

		public T GetElement(int index)
		{
			return _array[(_head + index) % _array.Length];
		}

		public int Capacity
		{
			get { return _array.Length; }
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException(
						nameof(value), value, "Capacity must be at least one");
				}

				SetCapacity(value);
			}
		}

		private void SetCapacity(int capacity)
		{
			if (_size < 1)
			{
				_head = 0;
				_tail = 0;
				_size = 0;
				_array = new T[capacity];
				_version += 1;
			}
			else if (capacity > _array.Length)
			{
				var array = new T[capacity];

				// Capacity enlarged: copy queue to beginning of new array

				if (_head < _tail)
				{
					Array.Copy(_array, _head, array, 0, _size);
				}
				else
				{
					Array.Copy(_array, _head, array, 0, _array.Length - _head);
					Array.Copy(_array, 0, array, _array.Length - _head, _tail);
				}

				_head = 0;
				_tail = (_size == capacity) ? 0 : _size;
				// _size didn't change!
				_array = array;
				_version += 1;
				return;
			}
			else if (capacity < _array.Length)
			{
				var array = new T[capacity];

				// Capacity shrunk: drop head items, keep tail items

				int offset = _array.Length - capacity;
				for (int i = 0; i < capacity; i++)
				{
					array[i] = GetElement(i + offset);
				}

				_head = 0;
				_tail = 0;
				_size = capacity;
				_array = array;
				_version += 1;
				return;
			}
			// else: no change in capacity, nothing to do
		}

		#region ICollection

		public void Add(T item)
		{
			Enqueue(item);
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException(); // todo can be done in O(n)
		}

		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}

		public int IndexOf(T item)
		{
			var comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < _size; i++)
			{
				T candidate = GetElement(i);

				// Good: comparer treats null and null as equal
				if (comparer.Equals(item, candidate))
				{
					return i;
				}
			}

			return -1; // not found
		}

		public void Clear()
		{
			_head = 0;
			_tail = 0;
			_size = 0;
			Array.Clear(_array, 0, _array.Length);
			_version += 1;
		}

		public int Count => _size;

		public bool IsReadOnly => false;

		public void CopyTo(T[] array, int index)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (index < 0 || index > array.Length)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (array.Length - index < _size)
				throw new ArgumentException("Array is too small", nameof(array));

			if (_size > 0)
			{
				if (_head < _tail)
				{
					Array.Copy(_array, _head, array, index + 0, _array.Length - _head);
				}
				else
				{
					Array.Copy(_array, _head, array, index + 0, _array.Length - _head);
					Array.Copy(_array, 0, array, index + _array.Length - _head, _tail);
				}
			}
		}

		public T[] ToArray()
		{
			var result = new T[_size];

			CopyTo(result, 0);

			return result;
		}

		#region IEnumerable

		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICloneable

		/// <summary>Creates a shallow copy.</summary>
		public object Clone()
		{
			var clone = new CappedQueue<T>(_array.Length);

			clone._head = _head;
			clone._tail = _tail;
			clone._size = _size;
			Array.Copy(_array, 0, clone._array, 0, _array.Length);
			clone._version = _version;

			return clone;
		}

		#endregion

		public override string ToString()
		{
			return $"Count = {Count}";
		}

		#endregion

		#region Nested type: Enumerator

		[Serializable, StructLayout(LayoutKind.Sequential)]
		private struct Enumerator : IEnumerator<T>
		{
			private int _index;
			private T _current;
			private int _version;
			private readonly CappedQueue<T> _queue;

			internal Enumerator(CappedQueue<T> queue)
			{
				_index = 0;
				_current = default(T);
				_version = queue._version;
				_queue = queue;
			}

			public void Reset()
			{
				_index = 0;
				_version = _queue._version;
				_current = default(T);
			}

			public bool MoveNext()
			{
				if (_version != _queue._version)
				{
					throw new InvalidOperationException("Collection changed while enumerating");
				}

				if (_index < _queue._size)
				{
					_current = _queue.GetElement(_index);
					_index += 1;
					return true;
				}

				_current = default(T);
				return false;
			}

			object IEnumerator.Current => Current;

			public T Current => _current;

			public void Dispose()
			{
				// Position _index at the end,
				// so MoveNext would return false:

				_index = _queue._size;
				_current = default(T);
			}
		}

		#endregion
	}
}
