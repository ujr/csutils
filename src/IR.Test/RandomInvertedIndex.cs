using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sylphe.IR.Test
{
	/// <summary>
	/// For each term, return a random (but always the same) doc id sequence.
	/// Each sequence is a random but strictly ascending selection of (at most)
	/// K integers from the range 0 to N-1.
	/// </summary>
	public class RandomInvertedIndex : IInvertedIndex
	{
		private readonly IDictionary<string, int> _termSeeds;
		private readonly IDictionary<string, IList<int>> _termDocs;

		public RandomInvertedIndex(int k, int n, bool allowAll = false)
		{
			if (k <= 0) throw new ArgumentException("must be positive", nameof(k));
			if (n <= 0) throw new ArgumentException("must be positive", nameof(n));
			if (k > n) throw new ArgumentException("ensure k <= n");

			K = k;
			N = n;

			AllowAll = allowAll;

			_termSeeds = new Dictionary<string, int>();
			_termDocs = new Dictionary<string, IList<int>>();
		}

		public int K { get; }
		public int N { get; }

		public bool AllowAll { get; }
		public bool EnableCache { get; set; }

		public DocSetIterator All()
		{
			if (!AllowAll)
				throw new NotSupportedException("The All query is not allowed");
			return new EnumerationIterator(Enumerable.Range(0, N));
		}

		public DocSetIterator Get(string term)
		{
			if (!_termSeeds.TryGetValue(term, out var seed))
			{
				seed = 1 + _termSeeds.Count;
				_termSeeds.Add(term, seed);
			}

			if (EnableCache)
			{
				if (!_termDocs.TryGetValue(term, out var docs))
				{
					docs = AscendingSample(K, N, seed).ToList();
					_termDocs.Add(term, docs);
				}

				return new ListIterator(docs);
			}

			return new EnumerationIterator(AscendingSample(K, N, seed));
		}

		// Random selection of (at most) k numbers from 0..n-1 in ascending order
		private static IEnumerable<int> AscendingSample(int k, int n, int seed)
		{
			Debug.Assert(k <= n);
			var random = new Random(seed);
			for (var i = 0; i < n; i++)
			{
				if (random.Next(n - i) < k)
				{
					k -= 1;
					yield return i;
				}
			}
		}
	}
}
