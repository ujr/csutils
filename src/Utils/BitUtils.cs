namespace Sylphe.Utils
{
	/// <summary>
	/// Methods for bit shifting, bit counting, etc., exploiting the
	/// binary nature of the computer. Many algorithms here are taken
	/// and/or adapted from "Hacker's Delight" by Henry S. Warren, Jr.
	/// The C# Language Reference defines the bit shifting operators.
	/// </summary>
	public static class BitUtils
	{
		#region Unsigned (aka logical) right shift

		// C# does not have Java's unsigned right shift (>>>) operator.
		// Instead, C# does unsigned right shift on uint and ulong, but
		// signed right shift (sign extension) on int and long.

		// C# as well as Java use only the low order 5 (int) or 6 (long)
		// bits of the shift count: x << n and x >> n shift by n&31 or n&63
		// bits to the left or right. This is different from C, where shifting
		// by a value beyond the word size is explicitly undefined.

		public static int LogicalShiftRight(int x, int n)
		{
			return unchecked((int) ((uint) x >> n));
		}

		public static long LogicalShiftRight(long x, int n)
		{
			return unchecked((long) ((ulong) x >> n));
		}

		#endregion

		#region Population count

		// Population count algorithm for 32 bits is from Hacker's Delight.
		// The variation for 64 bits is an adaptation of this algorithm.
		// Population count is the number of one bits in the argument.

		public static int PopulationCount(int value)
		{
			return PopulationCount(unchecked((uint) value));
		}

		public static int PopulationCount(uint x)
		{
			x = x - ((x >> 1) & 0x55555555);
			x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
			x = (x + (x >> 4)) & 0x0F0F0F0F;
			x = x + (x >> 8);
			x = x + (x >> 16);
			return (int) (x & 0x0000003F);
		}

		public static int PopulationCount(long value)
		{
			return PopulationCount(unchecked((ulong) value));
		}

		public static int PopulationCount(ulong x)
		{
			x = x - ((x >> 1) & 0x5555555555555555L);
			x = (x & 0x3333333333333333L) + ((x >> 2) & 0x3333333333333333L);
			x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FL;
			x = x + (x >> 8);
			x = x + (x >> 16);
			x = x + (x >> 32);
			return ((int) x) & 0x7F;
		}

		#endregion

		#region Leading and trailing zeros

		// Number of leading and trailing zeros:
		// Easy using PopulationCount, but other
		// (slightly more efficient) procedures exist.

		public static int LeadingZeroCount(int x)
		{
			return LeadingZeroCount(unchecked((uint) x));
		}

		public static int LeadingZeroCount(uint x)
		{
			x = x | (x >> 1);
			x = x | (x >> 2);
			x = x | (x >> 4);
			x = x | (x >> 8);
			x = x | (x >> 16);
			return PopulationCount(~x);
		}

		public static int LeadingZeroCount(long x)
		{
			return LeadingZeroCount(unchecked((ulong) x));
		}

		public static int LeadingZeroCount(ulong x)
		{
			x = x | (x >> 1);
			x = x | (x >> 2);
			x = x | (x >> 4);
			x = x | (x >> 8);
			x = x | (x >> 16);
			x = x | (x >> 32);
			return PopulationCount(~x);
		}

		public static int TrailingZeroCount(int x)
		{
			return TrailingZeroCount(unchecked((uint) x));
		}

		public static int TrailingZeroCount(uint x)
		{
			x = x | (x << 1);
			x = x | (x << 2);
			x = x | (x << 4);
			x = x | (x << 8);
			x = x | (x << 16);
			return PopulationCount(~x);
		}

		public static int TrailingZeroCount(long x)
		{
			return TrailingZeroCount(unchecked((ulong) x));
		}

		public static int TrailingZeroCount(ulong x)
		{
			x = x | (x << 1);
			x = x | (x << 2);
			x = x | (x << 4);
			x = x | (x << 8);
			x = x | (x << 16);
			x = x | (x << 32);
			return PopulationCount(~x);
		}

		#endregion

		#region Power of two: test, floor, ceiling

		// A power of 2 has a population count of 1.
		// The methods here test if an integer is a power of two,
		// and round up (ceiling) or down (floor) to a power of two.

		public static bool IsPowerOfTwo(int x)
		{
			return (x & (x - 1)) == 0;
		}

		public static bool IsPowerOfTwo(uint x)
		{
			return (x & (x - 1)) == 0;
		}

		public static bool IsPowerOfTwo(long x)
		{
			return (x & (x - 1)) == 0;
		}

		public static bool IsPowerOfTwo(ulong x)
		{
			return (x & (x - 1)) == 0;
		}

		public static int PowerOfTwoCeiling(int x)
		{
			return unchecked((int) PowerOfTwoCeiling((uint) x));
		}

		public static uint PowerOfTwoCeiling(uint x)
		{
			x -= 1;
			x |= (x >> 1);
			x |= (x >> 2);
			x |= (x >> 4);
			x |= (x >> 8);
			x |= (x >> 16);
			return x + 1;
		}

		public static long PowerOfTwoCeiling(long x)
		{
			return unchecked((long) PowerOfTwoCeiling((ulong) x));
		}

		public static ulong PowerOfTwoCeiling(ulong x)
		{
			x -= 1;
			x |= (x >> 1);
			x |= (x >> 2);
			x |= (x >> 4);
			x |= (x >> 8);
			x |= (x >> 16);
			x |= (x >> 32);
			return x + 1;
		}

		public static int PowerOfTwoFloor(int x)
		{
			return unchecked((int) PowerOfTwoFloor((uint) x));
		}

		public static uint PowerOfTwoFloor(uint x)
		{
			x |= (x >> 1);
			x |= (x >> 2);
			x |= (x >> 4);
			x |= (x >> 8);
			x |= (x >> 16);
			return x - (x >> 1);
		}

		public static long PowerOfTwoFloor(long x)
		{
			return unchecked((long) PowerOfTwoFloor((ulong) x));
		}

		public static ulong PowerOfTwoFloor(ulong x)
		{
			x |= (x >> 1);
			x |= (x >> 2);
			x |= (x >> 4);
			x |= (x >> 8);
			x |= (x >> 16);
			x |= (x >> 32);
			return x - (x >> 1);
		}

		#endregion

		public static string ToString(int x, bool tight = false)
		{
			return ToString(unchecked((uint) x), tight);
		}

		public static string ToString(uint x, bool tight = false)
		{
			int len = 32 + (tight ? 0 : 3);
			var buf = new char[len];
			int bufpos = 0, bitpos = 31;

			while (bitpos >= 0)
			{
				uint mask = 1U << bitpos;
				buf[bufpos++] = (x & mask) == mask ? '1' : '0';

				if (!tight && (bitpos & 7) == 0 && bitpos > 0)
					buf[bufpos++] = ' ';

				bitpos -= 1;
			}

			return new string(buf, 0, buf.Length);
		}

		public static string ToString(long x, bool tight = false)
		{
			return ToString(unchecked((ulong) x), tight);
		}

		public static string ToString(ulong x, bool tight = false)
		{
			int len = 64 + (tight ? 0 : 7);
			var buf = new char[len];
			int bufpos = 0, bitpos = 63;

			while (bitpos >= 0)
			{
				ulong mask = 1UL << bitpos;
				buf[bufpos++] = (x & mask) == mask ? '1' : '0';

				if (!tight && (bitpos & 7) == 0 && bitpos > 0)
					buf[bufpos++] = ' ';

				bitpos -= 1;
			}

			return new string(buf, 0, buf.Length);
		}
	}
}
