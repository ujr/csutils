using System;
using System.Collections;
using System.Collections.Generic;

namespace Sylphe.Utils
{
	public class ReadOnlySublist<T> : IList<T>
	{
		private readonly IList<T> _baseList;
		private readonly int _baseIndex;
		private readonly int _count;

		public ReadOnlySublist(IList<T> baseList, int baseIndex = 0, int count = -1)
		{
			if (baseList == null)
				throw new ArgumentNullException(nameof(baseList));
			if (baseIndex < 0 || baseIndex > baseList.Count)
				throw new ArgumentOutOfRangeException(nameof(baseIndex));
			if (count < 0)
				count = baseList.Count - baseIndex;
			if (baseIndex + count > baseList.Count)
				throw new ArgumentOutOfRangeException(nameof(count));

			_baseList = baseList;
			_baseIndex = baseIndex;
			_count = count;
		}

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator()
		{
			return new SublistEnumerator(_baseList, _baseIndex, _count);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICollection implementation

		public void Add(T item)
		{
			throw new InvalidOperationException("List is read-only");
		}

		public void Clear()
		{
			throw new InvalidOperationException("List is read-only");
		}

		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			for (int index = 0; index < _count; index++)
			{
				array[arrayIndex + index] = _baseList[_baseIndex + index];
			}
		}

		public bool Remove(T item)
		{
			throw new InvalidOperationException("List is read-only");
		}

		public int Count => _count;

		public bool IsReadOnly => true;

		#endregion

		#region IList implementation

		public int IndexOf(T item)
		{
			for (int i = 0; i < _count; i++)
			{
				if (Equals(item, _baseList[_baseIndex + i]))
				{
					return i;
				}
			}

			return -1; // not found
		}

		public void Insert(int index, T item)
		{
			throw new InvalidOperationException("List is read-only");
		}

		public void RemoveAt(int index)
		{
			throw new InvalidOperationException("List is read-only");
		}

		public T this[int index]
		{
			get { return _baseList[_baseIndex + index]; }
			set { throw new InvalidOperationException("List is read-only"); }
		}

		#endregion

		#region Nested type: SublistEnumerator

		private class SublistEnumerator : IEnumerator<T>
		{
			private readonly IList<T> _baseList;
			private readonly int _baseIndex;
			private readonly int _count;

			private int _index;
			private T _current;

			public SublistEnumerator(IList<T> baseList, int baseIndex, int count)
			{
				_baseList = baseList;
				_baseIndex = baseIndex;
				_count = count;
				_index = -1;
				_current = default(T);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				_index += 1;

				if (_index >= _count)
				{
					return false;
				}

				_current = _baseList[_baseIndex + _index];

				return true;
			}

			public void Reset()
			{
				_index = -1;
				_current = default(T);
			}

			public T Current => _current;

			object IEnumerator.Current => Current;
		}

		#endregion
	}
}
