using System;
using System.Collections.Generic;

namespace Sylphe.Utils
{
    public static partial class Algorithms
    {
		/// <summary>
		/// An implementation of Jeffrey Vitter's Reservoir Sampling:
		/// Given a (large) sequence of <paramref name="items"/>,
		/// select a random sample of a given size <paramref name="k"/>.
		/// <para/>
		/// The algorithm works using a "reservoir list" of size <paramref name="k"/>
		/// and fills it with the first <paramref name="k"/> items from the sequence.
		/// Subsequent items in the sequence replace items in the reservoir,
		/// but with decreasing probability.
		/// </summary>
		/// <param name="items">The sequence to sample (required)</param>
		/// <param name="k">How many elements to sample (at least one)</param>
		/// <param name="random">A source of random numbers (optional)</param>
		/// <returns>The sample, a list of <paramref name="k"/> items</returns>
		/// <remarks>
		/// This implementation also works if the sample size is larger
		/// than the length of the sequence. In this case, all items of
		/// the sequence will be in the sample.
		/// </remarks>
		public static IList<T> ReservoirSample<T>(this IEnumerable<T> items, int k, Random random = null)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			if (k < 1)
				throw new ArgumentException("Need sample size at least 1", nameof(k));

			if (random == null)
            {
				random = new Random();
            }

			int itemCount = 0;
			var reservoir = new List<T>(k);

			foreach (var item in items)
			{
				itemCount += 1;

				if (itemCount <= k)
				{
					reservoir.Add(item);
				}
				else
				{
					int r = random.Next(itemCount);
					if (r < k)
					{
						reservoir[r] = item;
					}
				}
			}

			return reservoir;
		}
    }
}