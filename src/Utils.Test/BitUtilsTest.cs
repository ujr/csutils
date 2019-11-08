using System;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class BitUtilsTest
	{
		[Fact]
		public void BitShiftTest()
		{
			// In C# (and in Java), the x<<n and x>>n operations shift
			// by n&0x1F (if x is 32 bits) or n&0x3F (if x is 64 bits),
			// that is, the high order bits are ignored.

			Assert.Equal(24, 3 << 3);
			Assert.Equal(8, 1 << 0x00DEAD03);

			Assert.Equal(125, 500 >> 2);
			Assert.Equal(125, 500 >> 0x00DEAD02);

			// C# does logical right shift for unsigned operands
			// and arithmetic right shift for signed operands.
			// Java has an unsigned right shift (>>>) operator;
			// in C#, cast (unchecked) to uint or ulong, then shift.

			Assert.Equal(8, 32 >> 2);
			Assert.Equal(1, 32 >> 5);
			Assert.Equal(0, 32 >> 31);
			Assert.Equal(32, 32 >> 32); // 32 = 0010 0000, ie low 5 bits zero

			Assert.Equal(-8, -32 >> 2);
			Assert.Equal(-1, -32 >> 31);
			Assert.Equal(-32, -32 >> 32);

			Assert.Equal(0xffffffe0, unchecked((uint) -32));
			Assert.Equal(0x3ffffff8, unchecked((int) ((uint) -32 >> 2)));
		}

		[Fact]
		public void LogicalRightShiftTest()
		{
			Assert.Equal(-1, BitUtils.LogicalShiftRight(-1, 0));
			Assert.Equal(int.MaxValue, BitUtils.LogicalShiftRight(-1, 1));
			Assert.Equal(int.MaxValue/2, BitUtils.LogicalShiftRight(-1, 2));

			Assert.Equal(-1L, BitUtils.LogicalShiftRight(-1L, 0));
			Assert.Equal(long.MaxValue, BitUtils.LogicalShiftRight(-1L, 1));
			Assert.Equal(long.MaxValue/2, BitUtils.LogicalShiftRight(-1L, 2));
		}

		[Fact]
		public void PopulationCountTest()
		{
			Assert.Equal(31, BitUtils.PopulationCount(int.MaxValue));
			Assert.Equal(32, BitUtils.PopulationCount(uint.MaxValue));
			Assert.Equal(63, BitUtils.PopulationCount(long.MaxValue));
			Assert.Equal(64, BitUtils.PopulationCount(ulong.MaxValue));
		}

		[Fact]
		public void LeadingZeroCountTest()
		{
			Assert.Equal(32, BitUtils.LeadingZeroCount(0));
			Assert.Equal(64, BitUtils.LeadingZeroCount(0L));

			Assert.Equal(0, BitUtils.LeadingZeroCount(-1));
			Assert.Equal(16, BitUtils.LeadingZeroCount(65535U));
			Assert.Equal(48, BitUtils.LeadingZeroCount(65535UL));
			Assert.Equal(31, BitUtils.LeadingZeroCount(1UL + uint.MaxValue));
			Assert.Equal(0, BitUtils.LeadingZeroCount(-1L));

			Assert.Equal(8, BitUtils.LeadingZeroCount(0x00BABE00));
			Assert.Equal(24, BitUtils.LeadingZeroCount(0x000000BABE000000));
		}

		[Fact]
		public void TrailingZeroCountTest()
		{
			Assert.Equal(32, BitUtils.TrailingZeroCount(0));
			Assert.Equal(64, BitUtils.TrailingZeroCount(0L));

			Assert.Equal(0, BitUtils.TrailingZeroCount(1U));
			Assert.Equal(0, BitUtils.TrailingZeroCount(1UL));
			Assert.Equal(1, BitUtils.TrailingZeroCount(2U));
			Assert.Equal(1, BitUtils.TrailingZeroCount(2UL));

			Assert.Equal(9, BitUtils.TrailingZeroCount(0x00BABE00));
			Assert.Equal(25, BitUtils.TrailingZeroCount(0x000000BABE000000));
		}

		[Fact]
		public void IsPowerOfTwoTest()
		{
			Assert.True(BitUtils.IsPowerOfTwo(0));
			Assert.True(BitUtils.IsPowerOfTwo(1));
			Assert.True(BitUtils.IsPowerOfTwo(2));
			Assert.False(BitUtils.IsPowerOfTwo(3));
			Assert.True(BitUtils.IsPowerOfTwo(4));

			Assert.False(BitUtils.IsPowerOfTwo(-1));
			Assert.False(BitUtils.IsPowerOfTwo(-2));
			Assert.False(BitUtils.IsPowerOfTwo(-4));

			Assert.False(BitUtils.IsPowerOfTwo(uint.MaxValue));
			Assert.True(BitUtils.IsPowerOfTwo(uint.MaxValue - 0x7FFFFFFF));

			Assert.False(BitUtils.IsPowerOfTwo((long) int.MaxValue));
			Assert.True(BitUtils.IsPowerOfTwo(1L + int.MaxValue));

			Assert.False(BitUtils.IsPowerOfTwo(ulong.MaxValue));
			Assert.True(BitUtils.IsPowerOfTwo(ulong.MaxValue - 0x7FFFFFFFFFFFFFFF));
		}

		[Fact]
		public void PowerOfTwoFloorCeiling32Test()
		{
			const int value = 200;
			int ceiling = BitUtils.PowerOfTwoCeiling(value);
			Console.WriteLine(@"PowerOfTwoCeiling({0}) = {1}", value, ceiling);

			Assert.Equal(0, BitUtils.PowerOfTwoFloor(0));
			Assert.Equal(0, BitUtils.PowerOfTwoCeiling(0));

			Assert.Equal(1, BitUtils.PowerOfTwoFloor(1));
			Assert.Equal(1, BitUtils.PowerOfTwoCeiling(1));

			Assert.Equal(2, BitUtils.PowerOfTwoFloor(2));
			Assert.Equal(2, BitUtils.PowerOfTwoCeiling(2));

			Assert.Equal(2, BitUtils.PowerOfTwoFloor(3));
			Assert.Equal(4, BitUtils.PowerOfTwoCeiling(3));

			Assert.Equal(4, BitUtils.PowerOfTwoFloor(4));
			Assert.Equal(4, BitUtils.PowerOfTwoCeiling(4));

			Assert.Equal(4, BitUtils.PowerOfTwoFloor(5));
			Assert.Equal(8, BitUtils.PowerOfTwoCeiling(5));

			// Careful to use UInt32 literals in tests below:

			Assert.Equal(1U << 30, BitUtils.PowerOfTwoFloor((1U << 31) - 1)); // 2**31 - 1
			Assert.Equal(1U << 31, BitUtils.PowerOfTwoCeiling((1U << 31) - 1));

			Assert.Equal(1U << 31, BitUtils.PowerOfTwoFloor(1U << 31)); // 2**31
			Assert.Equal(1U << 31, BitUtils.PowerOfTwoCeiling(1U << 31));

			Assert.Equal(1U << 31, BitUtils.PowerOfTwoFloor((1U << 31) + 1)); // 2**31 + 1
			Assert.Equal(0U, BitUtils.PowerOfTwoCeiling((1U << 31) + 1));

			Assert.Equal(1U<<31, BitUtils.PowerOfTwoFloor(uint.MaxValue)); // 2**32 - 1
			Assert.Equal(0U, BitUtils.PowerOfTwoCeiling(uint.MaxValue)); // (mod 2**32)
		}

		[Fact]
		public void PowerOfTwoFloorCeiling64Test()
		{
			const long value = 0xc9a1d677466b7ba;
			long floor = BitUtils.PowerOfTwoFloor(value);
			Console.WriteLine(@"PowerOfTwoFloor(0x{0:X}) = 0x{1:X}", value, floor);

			Assert.Equal(0, BitUtils.PowerOfTwoFloor(0L));
			Assert.Equal(0, BitUtils.PowerOfTwoCeiling(0L));

			Assert.Equal(4, BitUtils.PowerOfTwoFloor(5L));
			Assert.Equal(8, BitUtils.PowerOfTwoCeiling(5L));

			// Careful! Use the UL literals, otherwise AreEquals converts to decimal:

			Assert.Equal(1UL<<62, BitUtils.PowerOfTwoFloor((1UL << 63) - 1)); // 2**63-1
			Assert.Equal(1UL<<63, BitUtils.PowerOfTwoCeiling((1UL << 63) - 1));

			Assert.Equal(1UL<<63, BitUtils.PowerOfTwoFloor(1UL << 63)); // 2**63
			Assert.Equal(1UL<<63, BitUtils.PowerOfTwoCeiling(1UL << 63));

			Assert.Equal(1UL<<63, BitUtils.PowerOfTwoFloor((1UL << 63) + 1)); // 2**63+1
			Assert.Equal(0UL, BitUtils.PowerOfTwoCeiling((1UL << 63) + 1));

			Assert.Equal(1UL<<63, BitUtils.PowerOfTwoFloor(ulong.MaxValue)); // 2**64 - 1
			Assert.Equal(0UL, BitUtils.PowerOfTwoCeiling(ulong.MaxValue)); // (mod 2**64)
		}

		[Fact]
		public void ToStringTest()
		{
			string s = BitUtils.ToString(-1, true); // tight
            Assert.Equal("11111111111111111111111111111111", s);

			s = BitUtils.ToString(-1);
			Assert.Equal("11111111 11111111 11111111 11111111", s);

			s = BitUtils.ToString(0);
			Assert.Equal("00000000 00000000 00000000 00000000", s);

			s = BitUtils.ToString(-256);
			Assert.Equal("11111111 11111111 11111111 00000000", s);

			s = BitUtils.ToString(0xDEADBEEFU);
			Assert.Equal("11011110 10101101 10111110 11101111", s);

			s = BitUtils.ToString(long.MaxValue);
			Assert.Equal("01111111 11111111 11111111 11111111 11111111 11111111 11111111 11111111", s);

			s = BitUtils.ToString(long.MinValue);
			Assert.Equal("10000000 00000000 00000000 00000000 00000000 00000000 00000000 00000000", s);

			s = BitUtils.ToString(0x1CEDC0FFEEUL);
			Assert.Equal("00000000 00000000 00000000 00011100 11101101 11000000 11111111 11101110", s);
		}
	}
}
