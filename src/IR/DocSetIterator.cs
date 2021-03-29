using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sylphe.IR
{
	public abstract class DocSetIterator
	{
		protected static readonly int Uninitialized = -1;
		protected static readonly int NoMoreDocs = int.MaxValue;

		protected DocSetIterator(string tag = null)
		{
			Tag = tag ?? string.Empty;
			Doc = Uninitialized;
		}

		private string Tag { get; }

		/// <summary>ID of the current doc or NoMoreDocs</summary>
		/// <remarks>
		/// Making this a field (not a poroperty) at least doubles performance
		/// in Debug builds, but leaves it roughly the same in Release builds;
		/// presumably, release build optimizations eliminate the method call.
		/// </remarks>
		public int Doc { get; protected set; }

		/// <summary>Advance to and return the next doc ID or NoMoreDocs</summary>
		public abstract int NextDoc();

		/// <summary>
		/// Advance to and return the next doc ID after the current and
		/// greater than or equal to the given <paramref name="target"/>.
		/// </summary>
		public virtual int Advance(int target)
		{
			int doc;

			while ((doc = NextDoc()) < target) { }

			return Doc = doc;
		}

		public IEnumerable<int> GetAll()
		{
			int doc;
			while ((doc = NextDoc()) != NoMoreDocs)
			{
				yield return doc;
			}
		}

		public override string ToString()
		{
			return $"Doc = {Doc}, Tag = {Tag}";
		}
	}

	public class ListIterator : DocSetIterator
	{
		private readonly int[] _list;
		private readonly int _count;
		private int _index;

		public ListIterator(IEnumerable<int> sortedList, string tag = null)
			: base(tag)
		{
			_list = sortedList?.ToArray() ?? throw new ArgumentNullException();
			_count = _list.Length;
			_index = 0;
		}

		public override int NextDoc()
		{
			if (_index < _count)
			{
				return Doc = _list[_index++];
			}

			return Doc = NoMoreDocs;
		}
	}

	public class EnumerationIterator : DocSetIterator
	{
		private IEnumerator<int> _enumerator;

		public EnumerationIterator(IEnumerable<int> sortedEnum, string tag = null)
			: base(tag)
		{
			if (sortedEnum == null)
				throw new ArgumentNullException(nameof(sortedEnum));
			_enumerator = sortedEnum.GetEnumerator();
		}

		public override int NextDoc()
		{
			if (_enumerator == null)
			{
				return NoMoreDocs;
			}

			if (_enumerator.MoveNext())
			{
				return Doc = _enumerator.Current;
			}

			_enumerator = null;
			return Doc = NoMoreDocs;
		}
	}

	public class EmptyIterator : DocSetIterator
	{
		public EmptyIterator(string tag = null) : base(tag)
		{
			Doc = NoMoreDocs;
		}

		public override int NextDoc()
		{
			return NoMoreDocs;
		}
	}

	public class ButNotIterator : DocSetIterator
	{
		private DocSetIterator _candidates;
		private DocSetIterator _exclusions;

		public ButNotIterator(DocSetIterator candidates, DocSetIterator exclusions)
		{
			_candidates = candidates; // can be null
			_exclusions = exclusions; // can be null
		}

		public override int NextDoc()
		{
			if (_candidates == null)
			{
				return Doc = NoMoreDocs;
			}

			Doc = _candidates.NextDoc();

			if (Doc == NoMoreDocs)
			{
				_candidates = null; // exhausted
				return Doc;
			}

			if (_exclusions == null)
			{
				return Doc; // nothing more to subtract
			}

			return Doc = NextNonExcluded();
		}

		public override int Advance(int target)
		{
			if (_candidates == null)
			{
				return Doc = NoMoreDocs;
			}

			if (_exclusions == null)
			{
				return Doc = _candidates.Advance(target);
			}

			if (_candidates.Advance(target) == NoMoreDocs)
			{
				_candidates = null;
				return Doc = NoMoreDocs;
			}

			return Doc = NextNonExcluded();
		}

		private int NextNonExcluded()
		{
			Debug.Assert(_candidates != null);
			Debug.Assert(_exclusions != null);

			var candidate = _candidates.Doc;
			var excluded = _exclusions.Doc;

			do
			{
				if (candidate < excluded)
				{
					return candidate; // candidate before next exclusion
				}

				if (candidate > excluded)
				{
					excluded = _exclusions.Advance(candidate);
					if (excluded == NoMoreDocs)
					{
						_exclusions = null; // no more exclusions
						return candidate;
					}

					if (candidate < excluded)
					{
						return candidate; // candidate before next exclusion
					}
				}
				// else: candidate == excluded, try next candidate
			}
			while ((candidate = _candidates.NextDoc()) != NoMoreDocs);

			_candidates = null; // no more candidates
			return NoMoreDocs;
		}
	}

	public class ConjunctionIterator : DocSetIterator
	{
		private readonly DocSetIterator[] _iterators;

		public ConjunctionIterator(params DocSetIterator[] iterators)
		{
			if (iterators == null)
				throw new ArgumentNullException();
			if (iterators.Length < 1)
				throw new ArgumentException("Need at least one iterators");

			foreach (var scorer in iterators)
			{
				// prime each sub-iterator
				var doc = scorer.NextDoc();

				if (doc == NoMoreDocs)
				{
					// if any iterator is empty,
					// then the conjunction is empty
					Doc = NoMoreDocs;
					return;
				}
			}

			// Order iterators by first doc, ascending
			Array.Sort(iterators, (a, b) => a.Doc - b.Doc);

			// The invariant will be that all iterators are
			// always positioned at the same commen doc,
			// which will eventually be NoMoreDocs for all.
			// The call to StepToCommonDoc() will establish
			// and maintain this invariant.

			if (StepToCommonDoc(iterators) == NoMoreDocs)
			{
				// no common doc among all scorers
				Doc = NoMoreDocs;
				return;
			}

			_iterators = iterators;
			Doc = Uninitialized;
		}

		private static int StepToCommonDoc(IReadOnlyList<DocSetIterator> iterators)
		{
			var index = 0;
			var doc = iterators[iterators.Count - 1].Doc;

			DocSetIterator iterator;
			while ((iterator = iterators[index]).Doc < doc)
			{
				doc = iterator.Advance(doc);
				index += 1;
				if (index >= iterators.Count) index = 0;
			}

			return doc;
		}

		public override int NextDoc()
		{
			if (Doc == NoMoreDocs)
			{
				return NoMoreDocs;
			}

			if (Doc < 0)
			{
				return Doc = _iterators[_iterators.Length - 1].Doc;
			}

			_iterators[_iterators.Length - 1].NextDoc();

			return Doc = StepToCommonDoc(_iterators);
		}

		public override int Advance(int target)
		{
			if (Doc == NoMoreDocs)
			{
				return NoMoreDocs;
			}

			if (_iterators[_iterators.Length - 1].Doc < target)
			{
				_iterators[_iterators.Length - 1].Advance(target);
			}

			return Doc = StepToCommonDoc(_iterators);
		}
	}

	public class DisjunctionIterator : DocSetIterator
	{
		private readonly DocSetIterator[] _heap;
		private int _heapSize;

		// Maintain a heap of active iterators such that
		// the iterator with the min doc is at heap's root

		public DisjunctionIterator(params DocSetIterator[] iterators)
		{
			if (iterators == null)
				throw new ArgumentNullException();

			// slot at index 0 remains unused
			_heap = new DocSetIterator[iterators.Length + 1];

			// prime all iterators and build the heap
			var n = 0;
			foreach (var iterator in iterators)
			{
				var doc = iterator.NextDoc();
				if (doc != NoMoreDocs)
				{
					_heap[++n] = iterator;
				}
			}

			// a sorted array has the heap property
			Array.Sort(_heap, 1, n, new IteratorComparer());

			_heapSize = n;

			Doc = n > 0 ? Uninitialized : NoMoreDocs;
		}

		public override int NextDoc()
		{
			if (_heapSize == 0)
			{
				return NoMoreDocs; // all lists exhausted
			}

			while (_heap[1].Doc == Doc)
			{
				var doc = _heap[1].NextDoc();

				if (doc == NoMoreDocs)
				{
					_heap[1] = _heap[_heapSize];
					_heapSize -= 1;

					if (_heapSize == 0)
					{
						// last doc list exhausted
						return Doc = NoMoreDocs;
					}
				}

				DownHeap(1);
			}

			return Doc = _heap[1].Doc;
		}

		public override int Advance(int target)
		{
			if (_heapSize == 0)
			{
				return NoMoreDocs; // all lists exhausted
			}

			while (_heap[1].Doc < target)
			{
				var doc = _heap[1].Advance(target);

				if (doc == NoMoreDocs)
				{
					_heap[1] = _heap[_heapSize];
					_heapSize -= 1; // remove exhausted list

					if (_heapSize == 0)
					{
						// last doc list exhausted
						return Doc = NoMoreDocs;
					}
				}

				DownHeap(1);
			}

			return Doc = _heap[1].Doc;
		}

		/// <summary>
		/// Restore the heap property after heap[k] changed its priority.
		/// If the root changed its priority (the usual case), k is 1.
		/// </summary>
		private void DownHeap(int k)
		{
			// Inside the while loop, there are two calls to the comparer.
			// There is an alternative implementation with only one call
			// inside the loop and an UpHeap operation after the loop.

			var item = _heap[k]; // save top item
			var itemDoc = item.Doc;
			var limit = _heapSize >> 1; // div 2

			while (k <= limit)
			{
				// Pick smaller child:
				var child = k << 1; // times 2, left child
				if (child < _heapSize && _heap[child + 1].Doc < _heap[child].Doc)
				{
					child++; // right child
				}

				if (itemDoc < _heap[child].Doc) break;

				_heap[k] = _heap[child]; // shift child up

				k = child;
			}

			_heap[k] = item; // restore saved item
		}

		private class IteratorComparer : IComparer<DocSetIterator>
		{
			public int Compare(DocSetIterator x, DocSetIterator y)
			{
				if (x == null && y == null) return 0;
				if (x == null) return -1;
				if (y == null) return +1;
				return Comparer<int>.Default.Compare(x.Doc, y.Doc);
			}
		}
	}
}
