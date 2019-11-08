using System;
using System.Collections.Generic;

namespace Sylphe.Utils
{
    public static partial class Algorithms
    {
        /// <summary>
        /// Shuffle the given <paramref name="list"/> into a random order.
        /// Optionally, <paramref name="random"/> is the randomness to use.
        /// </summary>
		public static void Shuffle<T>(this IList<T> list, Random random = null)
		{
			// This is essentially Fisher-Yates shuffle:
			//int n = list.Count;
			//while (n > 1)
			//{
			//	int k = random.Next(n); // 0 <= k < n
			//	n -= 1;
			//	var temp = list[k];
			//	list[k] = list[n];
			//	list[n] = temp;
			//}
			// The code below attempts to spare one assigment from
			// the loop at the price of more code outside the loop.

            if (list == null)
                throw new ArgumentNullException(nameof(list));

			int count = list.Count;
			if (count < 2) return;

            if (random == null)
            {
                random = new Random();
            }

			int r, last = count - 1;
			T x = list[last]; // remember last item

			while (last > 0)
			{
				r = random.Next(last); // 0 <= r < last

				list[last] = list[r];
				list[r] = list[--last];
			}

			// Restore last item at a random place:

			r = random.Next(count);
			list[0] = list[r];
			list[r] = x;

			// The last three lines are short for:
			// list[0] = x; Swap(list, 0, random.Next(count));
		}
    }
}