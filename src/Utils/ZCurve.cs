using System;
using System.Collections.Generic;
using System.Linq;

namespace Sylphe.Utils
{
	/// <summary>
	/// Morton (aka Z order) interlacing of two dimensions.
	/// See https://en.wikipedia.org/wiki/Z-order_curve
	/// Conventions in the methods here:
	/// interlaced values are named z or q, and of type ulong;
	/// coordinates are named x and y, and of type uint
	/// </summary>
	public static class ZCurve
	{
		#region Encoding and decoding

		public static ulong Encode(uint x, uint y)
		{
			// Interlace the low 32 bits of x and y to produce the 64bit Morton code:
			// The comments show how the value changes in each step (the lowest few bits)

			ulong xx = x & 0x00000000FFFFFFFF;
			xx = (xx ^ (xx << 16)) & 0x0000FFFF0000FFFF; // ... fedc ba98 7654 3210
			xx = (xx ^ (xx << 8)) & 0x00FF00FF00FF00FF;  // ... ---- ---- 7654 3210
			xx = (xx ^ (xx << 4)) & 0x0F0F0F0F0F0F0F0F;  // ... ---- 7654 ---- 3210
			xx = (xx ^ (xx << 2)) & 0x3333333333333333;  // ... --76 --54 --32 --10
			xx = (xx ^ (xx << 1)) & 0x5555555555555555;  // ... -7-6 -5-4 -3-2 -1-0

			ulong yy = y & 0x00000000FFFFFFFF;
			yy = (yy ^ (yy << 16)) & 0x0000FFFF0000FFFF;
			yy = (yy ^ (yy << 8)) & 0x00FF00FF00FF00FF;
			yy = (yy ^ (yy << 4)) & 0x0F0F0F0F0F0F0F0F;
			yy = (yy ^ (yy << 2)) & 0x3333333333333333;
			yy = (yy ^ (yy << 1)) & 0x5555555555555555;

			return (yy << 1) | xx; // the code's MSB is from y, the LSB from x
		}

		public static uint DecodeX(ulong z)
		{
			z = z & 0x5555555555555555;
			z = (z ^ (z >> 1)) & 0x3333333333333333;
			z = (z ^ (z >> 2)) & 0x0F0F0F0F0F0F0F0F;
			z = (z ^ (z >> 4)) & 0x00FF00FF00FF00FF;
			z = (z ^ (z >> 8)) & 0x0000FFFF0000FFFF;
			z = (z ^ (z >> 16)) & 0x00000000FFFFFFFF;
			return (uint) z;
		}

		public static uint DecodeY(ulong z)
		{
			z = (z >> 1) & 0x5555555555555555;
			z = (z ^ (z >> 1)) & 0x3333333333333333;
			z = (z ^ (z >> 2)) & 0x0F0F0F0F0F0F0F0F;
			z = (z ^ (z >> 4)) & 0x00FF00FF00FF00FF;
			z = (z ^ (z >> 8)) & 0x0000FFFF0000FFFF;
			z = (z ^ (z >> 16)) & 0x00000000FFFFFFFF;
			return (uint) z;
		}

		#endregion

		#region Search box comparisons

		/// <summary>
		/// Return true iff <paramref name="z"/> is beyond
		/// <paramref name="zmax"/>, that is, if <paramref name="z"/>
		/// is outside the axes-aligned rectangle defined by Z values
		/// 0 (zero) and <paramref name="zmax"/>.
		/// </summary>
		public static bool IsBeyond(ulong z, ulong zmax)
		{
			// Return true iff x(z) > x(zmax) or y(z) > y(zmax)
			// Do not decode x and y, just mask off every other bit
			const ulong xmask = 0x5555555555555555UL;
			const ulong ymask = 0xAAAAAAAAAAAAAAAAUL;
			return (z & xmask) > (zmax & xmask) || (z & ymask) > (zmax & ymask);
		}

		/// <summary>
		/// Return true iff <paramref name="z"/> is within (or on the
		/// boundary of) the axes-aligned rectangle defined by Z values
		/// <paramref name="qlo"/> and <paramref name="qhi"/>.
		/// </summary>
		public static bool IsInsideBox(ulong z, ulong qlo, ulong qhi)
		{
			/// Do not decode x and y, just mask off every other bit
			const ulong xmask = 0x5555555555555555UL;
			const ulong ymask = 0xAAAAAAAAAAAAAAAAUL;

			ulong zx = z & xmask;
			ulong zy = z & ymask;

			return (qlo & xmask) <= zx && zx <= (qhi & xmask) &&
				   (qlo & ymask) <= zy && zy <= (qhi & ymask);
		}

		/// <summary>
		/// Find the least value along the Z curve that is greater than
		/// <paramref name="z"/> and within the axes-aligned rectangle
		/// defined by <paramref name="qlo"/> and <paramref name="qhi"/>.
		/// <para/>
		/// Also works if <paramref name="z"/> is within this rectangle,
		/// unless <paramref name="z"/> equals <paramref name="qhi"/>, in
		/// which case <c>0</c> (zero) is returned. In practice, however,
		/// you should should simply increment <paramref name="z"/> and
		/// test if it's inside using <see cref="IsInsideBox"/>; only if
		/// it is outside, call this method to find the next Z inside.
		/// <para/>
		/// This method implements the algorithm described by Tropf and Herzog
		/// in "Multidimensional Range Search in Dynamically Balanced Trees",
		/// Angewandte Informatik (Applied Informatics), 2/1981.
		/// </summary>
		public static ulong NextInsideBox(ulong z, ulong qlo, ulong qhi, int starti = 63)
		{
			// Procedure: "binary search" for the quadrant containing z
			// while cutting off from the query box the parts that cannot
			// contain the least z' > z. This works because the Z curve
			// fully fills a quadrant before entering the next quadrant.

			int i = starti;
			ulong result = 0;

			while (i >= 0)
			{
				// Split Z plane into halves along x (if i even) or y (if i odd) axis;
				// then see where the points z, qlo, qhi are relative to the split line:
				ulong key = ((z >> i) & 1) << 2 | (((qlo >> i) & 1) << 1) | ((qhi >> i) & 1);

				switch (key)
				{
					case 0: // all in "lower" half; continue search in lower half
						break;
					case 1: // z in lower half, cut off upper part of query box
						qhi = NearestBelow(qhi, i);
						// but result might be least z in cut-off part
						result = NearestAbove(qlo, i);
						break;
					case 2: // cannot occur because qlo <= qhi
						throw new ArgumentException("Ensure qlo <= qhi");
					case 3: // query box beyond split line: result is upper left corner of query box
						return qlo;

					case 4: // query box beyond split line: no result (or last case 1 result)
						return result;
					case 5: // z in upper half, cut off lower part of query box
						qlo = NearestAbove(qlo, i);
						break;
					case 6: // cannot occur because qlo <= qhi
						throw new ArgumentException("Ensure qlo <= qhi");
					case 7: // all in "upper" half; continue search in upper half
						break;

					default:
						throw new Exception("Bug: key must be in 0..7");
				}

				i -= 1;
			}

			return result;
		}

		public static ulong NearestAbove(ulong z, int i)
		{
			// Find the nearest z at or above the ith Z plane bisector; i=63
			// cuts into upper/lower half, i=62 cuts into left/right quadrant, etc.
			// Procedure: set z[i] and clear z[j] for j < i, j = 2k, k in N.

			//       i         bit position
			// ...00010000000  omask (the bit to set)
			// ...11110101010  amask (the bits to clear)
			// ...00001010101  inverted -- this is ...1010... shifted right (zero fill!)

			// Caution: unsigned (aka logical) shift right on line below!
			ulong amask = ~(0x2AAAAAAAAAAAAAAAUL >> (63 - i)); // ...11110101
			ulong omask = 1UL << i;                            // ...00100000

			return (z & amask) | omask;
		}

		public static ulong NearestBelow(ulong z, int i)
		{
			// Find the nearest z below the ith Z plane bisector; i=63 cuts
			// into upper/lower half, i=62 cuts into left/right quadrant, etc.
			// Procedure: clear z[i] and set z[j] for j < i, j = 2k, k in N.

			//       i                 i
			// ... 1101 1111    ... 1110 1111    clear z[i]
			// ... 0000 1010    ... 0000 0101    set z[j], j < i, ...

			ulong amask = ~(1UL << i);                      // ...11011111
			ulong omask = 0x2AAAAAAAAAAAAAAAUL >> (63 - i); // ...00001010
			// Caution: unsigned (aka logical) shift right on line above!

			return (z & amask) | omask;
		}

		#endregion

		#region Block (level) routines

		/// <summary>
		/// Blocks covering a single cell
		/// </summary>
		public const int MinLevel = 0;

		/// <summary>
		/// The one block covering all cells
		/// </summary>
		public const int MaxLevel = 32;

		public static ulong FirstInBlock(ulong z, int level)
		{
			// Valid levels are between 0 and 32, inclusive.
			// Shift twice by level, not once by 2*level; reason:
			// C# shifts longs mod 64, that is, x<<64 == x<<0 == x
			return z & ~(((1UL << level) << level) - 1UL);
		}

		public static ulong LastInBlock(ulong z, int level)
		{
			// Valid levels are between 0 and 32, inclusive.
			// Shift twice by level, not once by 2*level; reason:
			// C# shifts longs mod 64, that is, x<<64 == x<<0 == x
			return z | (((1UL << level) << level) - 1UL);
		}

		public static ulong CellsPerBlock(int level)
		{
			// Level 0 => 1, 1=>4, 2=>16, ... n => 2**(2n) = 1UL<<(n<<1)
			// About shifting twice: see comments elsewhere
			return (1UL << level) << level;
		}

		/// <summary>
		/// The largest block level starting at <paramref name="z"/>.
		/// </summary>
		/// <param name="z">An address on the Z curve</param>
		/// <returns>The level, 0..32</returns>
		public static int LargestLevel(ulong z)
		{
			// Each block contains 4 immediate sub blocks; therefore:
			// level = number of trailing 00's (pairs of zeros)
			return BitUtils.TrailingZeroCount(z)/2;
		}

		/// <summary>
		/// The largest block level starting at <paramref name="z"/>
		/// but not extending beyond <paramref name="zmax"/>.
		/// </summary>
		/// <param name="z">An address on the Z curve,
		///  must not be beyond <paramref name="zmax"/></param>
		/// <param name="zmax">The upper bound of the query box</param>
		/// <returns>The level, 0..32</returns>
		public static int LargestLevel(ulong z, ulong zmax)
		{
			// The largest level of a block starting at z
			// but not extending beyond zmax
			// TODO Make more efficient -- don't test each level

			int level = LargestLevel(z);
			ulong last = LastInBlock(z, level);

			while (level > 0 && IsBeyond(last, zmax))
			{
				level -= 1;
				last = LastInBlock(z, level);
			}

			return level;
		}

		/// <summary>
		/// The largest block level from the given <paramref name="levels"/>
		/// that starts at <paramref name="z"/> and does not extend beyond
		/// <paramref name="zmax"/>.
		/// </summary>
		/// <param name="z">An address on the Z curve,
		///  must not be beyond <paramref name="zmax"/></param>
		/// <param name="zmax">The upper bound of the query box</param>
		/// <param name="levels">Available levels, must be strictly increasing</param>
		/// <returns>One of the given <paramref name="levels"/></returns>
		public static int LargestLevel(ulong z, ulong zmax, params int[] levels)
		{
			// Same as LargestLevel(z,zmax) but look only amongst
			// the given levels. Expect levels to be strictly increasing,
			// like this: [L0, L1 > L0, L2 > L1, L3 > L2, ...]

			int level = LargestLevel(z); // level in 0..32
			int index = levels.BinarySearch(level);
			if (index < 0)
			{
				index = ~index - 1;
				if (index < 0)
					index = 0;
				// index in 0..len(levels)-1
				level = levels[index];
			}

			ulong last = LastInBlock(z, level);

			while (index > 0 && IsBeyond(last, zmax))
			{
				index -= 1;
				level = levels[index];
				last = LastInBlock(z, level);
			}

			return level;
		}

		public struct Block
		{
			/// <summary>
			/// The block's starting Z address, any ulong value.
			/// </summary>
			public readonly ulong Start;

			/// <summary>
			/// The block's level: between 0 (a single cell)
			/// and 32 (inclusive; one block covering all cells).
			/// </summary>
			public readonly int Level;

			public Block(ulong start, int level)
			{
				if (level < MinLevel || level > MaxLevel)
					throw new ArgumentOutOfRangeException(nameof(level), "Must be between MinLevel and MaxLevel (inclusive)");
				Start = start;
				Level = level;
			}

			public override string ToString()
			{
				return $"{Start} at level {Level}";
			}
		}

		#endregion

		/// <summary>
		/// Call <see cref="GetQueryBlocks(ulong, ulong, IEnumerable{int})"/>
		/// with the Z values that result from interleaving the two coordinate
		/// pairs xmin,ymin and xmax,ymax.
		/// </summary>
		public static IEnumerable<Block> GetQueryBlocks(
			uint xmin, uint ymin, uint xmax, uint ymax,
			IEnumerable<int> indexedLevels = null)
		{
			ulong qlo = Encode(xmin, ymin);
			ulong qhi = Encode(xmax, ymax);

			return GetQueryBlocks(qlo, qhi, indexedLevels);
		}

		/// <summary>
		/// Yields the shortest ordered sequence of blocks (at the given
		/// <paramref name="indexedLevels"/> or any level if omitted)
		/// covering the whole box defined by <paramref name="qlo"/> and
		/// <paramref name="qhi"/> (the least and greatest Z value).
		/// </summary>
		/// <returns>An enumeration of blocks, that is, pairs of
		/// starting address and level.</returns>
		public static IEnumerable<Block> GetQueryBlocks(ulong qlo, ulong qhi, IEnumerable<int> indexedLevels = null)
		{
			if (indexedLevels == null)
			{
				indexedLevels = Enumerable.Range(0, 33);
			}

			int leastIndexedLevel = indexedLevels.Min();
			int shift = leastIndexedLevel*2;

			ulong first = qlo >> shift;
			ulong last = qhi >> shift;
			var levels = indexedLevels.Select(level => level - leastIndexedLevel).ToArray();

			for (ulong cursor = first;;)
			{
				int level = LargestLevel(cursor, last, levels);
				ulong increment = CellsPerBlock(level);

				yield return new Block(cursor << shift, level + leastIndexedLevel);

				cursor += increment;

				if (cursor > last) break;

				if (!IsInsideBox(cursor, first, last))
				{
					cursor = NextInsideBox(cursor, first, last, 63 - shift);
				}
			}
		}
	}
}
