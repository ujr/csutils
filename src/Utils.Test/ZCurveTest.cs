using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class ZCurveTest
	{
		private readonly ITestOutputHelper _output;

		public ZCurveTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void EncodeDecodeTest()
		{
			const uint ux = 0xFFFFFFFF;
			const uint uy = 0x5555AAAA;

			ulong uz = ZCurve.Encode(ux, uy);
			Assert.Equal(0x77777777DDDDDDDDUL, uz);
			Assert.Equal(ux, ZCurve.DecodeX(uz));
			Assert.Equal(uy, ZCurve.DecodeY(uz));

			Assert.Equal(0U, ZCurve.DecodeX(ZCurve.Encode(0, 0)));
			Assert.Equal(0U, ZCurve.DecodeY(ZCurve.Encode(0, 0)));
			Assert.Equal(123456789U, ZCurve.DecodeX(ZCurve.Encode(123456789, 999999)));
			Assert.Equal(999999U, ZCurve.DecodeY(ZCurve.Encode(12345689, 999999)));

			Assert.Equal(ulong.MaxValue, ZCurve.Encode(uint.MaxValue, uint.MaxValue));
		}

		[Fact]
		public void WarrensBitShuffling()
		{
			// Just for the record, this method is straight from
			// Warren's book Hacker's Delight (1st ed, 2003, page 107).
			// It interleaves bits of a single word, whereas ZCurve.Encode
			// interleaves the bits of two words.

			uint t, x = 0xDEADBEEF; // 1101 1110 1010 1101  1011 1110 1110 1111

			t = (x ^ (x >> 8)) & 0x0000FF00; x = x ^ t ^ (t << 8);
			t = (x ^ (x >> 4)) & 0x00F000F0; x = x ^ t ^ (t << 4);
			t = (x ^ (x >> 2)) & 0x0C0C0C0C; x = x ^ t ^ (t << 2);
			t = (x ^ (x >> 1)) & 0x22222222; x = x ^ t ^ (t << 1);

			Assert.Equal(0xE7FCDCF7, x); // 1110 0111 1111 1100 1101 1100 1111 0111

			t = (x ^ (x >> 1)) & 0x22222222; x = x ^ t ^ (t << 1);
			t = (x ^ (x >> 2)) & 0x0C0C0C0C; x = x ^ t ^ (t << 2);
			t = (x ^ (x >> 4)) & 0x00F000F0; x = x ^ t ^ (t << 4);
			t = (x ^ (x >> 8)) & 0x0000FF00; x = x ^ t ^ (t << 8);

			Assert.Equal(0xDEADBEEF, x);
		}

		[Fact]
		public void IsBeyondTest()
		{
			Assert.False(ZCurve.IsBeyond(0, 0));
			Assert.False(ZCurve.IsBeyond(255UL, 255UL));

			Assert.True(ZCurve.IsBeyond(44, 51));
			Assert.True(ZCurve.IsBeyond(22, 51));
		}

		[Fact]
		public void IsInsideBoxTest()
		{
			ulong min = ZCurve.Encode(1U, 1U); // 3
			ulong max = ZCurve.Encode(5U, 4U); // 49

			Assert.False(ZCurve.IsInsideBox(0, min, max));
			Assert.True(ZCurve.IsInsideBox(3, min, max));
			Assert.True(ZCurve.IsInsideBox(19, min, max));
			Assert.False(ZCurve.IsInsideBox(20, min, max));
			Assert.False(ZCurve.IsInsideBox(8, min, max));
			Assert.True(ZCurve.IsInsideBox(15, min, max));
			Assert.True(ZCurve.IsInsideBox(49, min, max));
			Assert.False(ZCurve.IsInsideBox(50, min, max));

			min = ZCurve.Encode(int.MaxValue - 1, int.MaxValue - 1);
			max = ZCurve.Encode(int.MaxValue, int.MaxValue);
			Assert.False(ZCurve.IsInsideBox(ZCurve.Encode(int.MaxValue - 2, int.MaxValue - 2), min, max));
			Assert.True(ZCurve.IsInsideBox(ZCurve.Encode(int.MaxValue - 1, int.MaxValue - 1), min, max));
			Assert.True(ZCurve.IsInsideBox(ZCurve.Encode(int.MaxValue, int.MaxValue), min, max));
			Assert.False(ZCurve.IsInsideBox(ZCurve.Encode(1U + int.MaxValue, 1U + int.MaxValue), min, max));
		}

		[Fact]
		public void NearestAboveTest()
		{
			Assert.Equal(1UL, ZCurve.NearestAbove(0, 0));
			Assert.Equal(2UL, ZCurve.NearestAbove(0, 1));
			Assert.Equal(4UL, ZCurve.NearestAbove(0, 2));
			Assert.Equal(8UL, ZCurve.NearestAbove(0, 3));
			Assert.Equal(1UL << 31, ZCurve.NearestAbove(0, 31));
			Assert.Equal(1UL << 63, ZCurve.NearestAbove(0, 63));

			Assert.Equal(3UL, ZCurve.NearestAbove(3, 0));
			Assert.Equal(3UL, ZCurve.NearestAbove(3, 1));
			Assert.Equal(6UL, ZCurve.NearestAbove(3, 2));
			Assert.Equal(9UL, ZCurve.NearestAbove(3, 3));
			Assert.Equal(18UL, ZCurve.NearestAbove(3, 4));
			Assert.Equal(66UL, ZCurve.NearestAbove(3, 6));
			Assert.Equal(129UL, ZCurve.NearestAbove(3, 7));
			Assert.Equal((1UL << 31) + 1, ZCurve.NearestAbove(3, 31));
			Assert.Equal((1UL << 63) + 1, ZCurve.NearestAbove(3, 63));

			// TODO More
		}

		[Fact]
		public void NearestBelowTest()
		{
			Assert.Equal(0UL, ZCurve.NearestBelow(0, 0));
			Assert.Equal(0UL, ZCurve.NearestBelow(0, 1));
			Assert.Equal(1UL, ZCurve.NearestBelow(0, 2));
			Assert.Equal(2UL, ZCurve.NearestBelow(0, 3));
			Assert.Equal(0x2AAAAAAAUL, ZCurve.NearestBelow(0, 31));
			Assert.Equal(0x2AAAAAAAAAAAAAAAUL, ZCurve.NearestBelow(0, 63));

			// 55 = Z(7,5) // 0111b 0101b => 0011 0111b
			Assert.Equal(54UL, ZCurve.NearestBelow(55, 0));
			Assert.Equal(53UL, ZCurve.NearestBelow(55, 1));
			Assert.Equal(51UL, ZCurve.NearestBelow(55, 2));
			Assert.Equal(55UL, ZCurve.NearestBelow(55, 3));
			Assert.Equal(39UL, ZCurve.NearestBelow(55, 4));
			Assert.Equal(31UL, ZCurve.NearestBelow(55, 5));
			Assert.Equal(55UL, ZCurve.NearestBelow(55, 6));
			Assert.Equal(63UL, ZCurve.NearestBelow(55, 7));
			Assert.Equal(ZCurve.Encode(7, 0x7FFF), ZCurve.NearestBelow(55, 31));
			Assert.Equal(ZCurve.Encode(7, int.MaxValue), ZCurve.NearestBelow(55, 63));

			// TODO More
		}

		[Fact]
		public void NextInsideBoxTest()
		{
			Assert.Equal(27UL, ZCurve.NextInsideBox(0, 27, 102));
			Assert.Equal(74UL, ZCurve.NextInsideBox(58, 27, 102));
			Assert.Equal(74UL, ZCurve.NextInsideBox(67, 27, 102));
			Assert.Equal(96UL, ZCurve.NextInsideBox(79, 27, 102));
			Assert.Equal(74UL, ZCurve.NextInsideBox(61, 27, 102));

			// What if z is within box(qlo,qhi)? Drill down, cutting
			// off parts of the box, until a single cell remains.

			Assert.Equal(6UL, ZCurve.NextInsideBox(3, 3, 49));
			Assert.Equal(11UL, ZCurve.NextInsideBox(9, 3, 49));
			Assert.Equal(14UL, ZCurve.NextInsideBox(13, 3, 49));
			Assert.Equal(19UL, ZCurve.NextInsideBox(18, 3, 49));
			Assert.Equal(49UL, ZCurve.NextInsideBox(48, 3, 49));

			Assert.Equal(0UL, ZCurve.NextInsideBox(49, 3, 49));
			Assert.Equal(0UL, ZCurve.NextInsideBox(54, 54, 54));

			// What if z > qhi? Only keys 000 or 100 or 101 can occur,
			// drilling down (possibly cutting off lower parts of the
			// query box) until the box is below and z is above the
			// dividing line (key 100) and we return the result so far,
			// which is zero (result's initial value).

			Assert.Equal(0UL, ZCurve.NextInsideBox(50, 3, 49, 5)); // x within, y beyond
			Assert.Equal(0UL, ZCurve.NextInsideBox(51, 3, 49, 5)); // x within, y beyond
			Assert.Equal(0UL, ZCurve.NextInsideBox(52, 3, 49, 5)); // x beyond, y within
			Assert.Equal(0UL, ZCurve.NextInsideBox(53, 3, 49, 5)); // x beyond, y within
			Assert.Equal(0UL, ZCurve.NextInsideBox(54, 3, 49, 5)); // both x and y beyond

			// Enumerate cells within query box qlo..qhi:

			var result = new List<ulong>();
			var expected = new ulong[] { 3, 6, 7, 9, 11, 12, 13, 14, 15, 18, 19, 24, 25, 26, 27, 33, 36, 37, 48, 49 };

			ulong qlo = ZCurve.Encode(1U, 1U); // 3
			ulong qhi = ZCurve.Encode(5U, 4U); // 49
			ulong cur = qlo;

			while (true)
			{
				result.Add(cur);
				cur += 1;
				if (cur > qhi) break;
				const int starti = 5;
				if (!ZCurve.IsInsideBox(cur, qlo, qhi))
				{
					cur = ZCurve.NextInsideBox(cur, qlo, qhi, starti);
				}
			}

			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(0, 32, 0, ulong.MaxValue)]
		[InlineData(1, 0, 1, 1)]
		[InlineData(2, 0, 2, 2)]
		[InlineData(3, 0, 3, 3)]
		[InlineData(4, 1, 4, 7)]
		[InlineData(5, 0, 5, 5)]
		[InlineData(48, 2, 48, 63)]
		[InlineData(192, 3, 192, 255)]
		[InlineData(256, 4, 256, 511)]
		[InlineData(512, 4, 0x200, 0x2FF)]
		[InlineData(768, 4, 0x300, 0x3FF)]
		[InlineData(1024, 5, 0x400, 0x7FF)]
		[InlineData(0x8000, 7, 0x8000, 0xBFFF)]
		[InlineData(long.MaxValue - 15, 2, long.MaxValue - 15, long.MaxValue)]
		public void BlockPropertyTest(ulong z, int level, ulong first, ulong last)
		{
			int i = ZCurve.LargestLevel(z);
			ulong f = ZCurve.FirstInBlock(z, i);
			ulong l = ZCurve.LastInBlock(z, i);
			ulong n = ZCurve.CellsPerBlock(i);

			Assert.Equal(level, i);
			Assert.Equal(first, f);
			Assert.Equal(last, l);

			_output.WriteLine($"z={z} i={i} first={f:X8} last={l:X8} cells_in_block={n}");
		}

		[Fact]
		public void FirstInBlockTest()
		{
			// At level 0, each block contains one cell:
			Assert.Equal(0UL, ZCurve.FirstInBlock(0, 0));
			Assert.Equal(1UL, ZCurve.FirstInBlock(1, 0));
			Assert.Equal(2UL, ZCurve.FirstInBlock(2, 0));
			Assert.Equal(3UL, ZCurve.FirstInBlock(3, 0));
			Assert.Equal(4UL, ZCurve.FirstInBlock(4, 0));
			Assert.Equal(1234567890UL, ZCurve.FirstInBlock(1234567890, 0));
			Assert.Equal((ulong)(long.MaxValue), ZCurve.FirstInBlock(long.MaxValue, 0));

			Assert.Equal(0UL, ZCurve.FirstInBlock(0, 1));
			Assert.Equal(0UL, ZCurve.FirstInBlock(1, 1));
			Assert.Equal(0UL, ZCurve.FirstInBlock(2, 1));
			Assert.Equal(0UL, ZCurve.FirstInBlock(3, 1));
			Assert.Equal(4UL, ZCurve.FirstInBlock(4, 1));
			Assert.Equal(0x499602d0UL, ZCurve.FirstInBlock(0x499602d2, 1));
			Assert.Equal((ulong)(long.MaxValue - 3), ZCurve.FirstInBlock(long.MaxValue, 1));

			Assert.Equal(0UL, ZCurve.FirstInBlock(0, 20));
			Assert.Equal(0UL, ZCurve.FirstInBlock(1, 20));
			Assert.Equal(0UL, ZCurve.FirstInBlock(2, 20));
			Assert.Equal(0UL, ZCurve.FirstInBlock(3, 20));
			Assert.Equal(0UL, ZCurve.FirstInBlock(4, 20));
			Assert.Equal(0UL, ZCurve.FirstInBlock(0x499602d2, 20));
			Assert.Equal(0x7fffff0000000000UL, ZCurve.FirstInBlock(long.MaxValue, 20));

			Assert.Equal(0x4000000000000000UL, ZCurve.FirstInBlock(long.MaxValue, 31));
			Assert.Equal(0UL, ZCurve.FirstInBlock(long.MaxValue, 32));
		}

		[Fact]
		public void LastInBlockTest()
		{
			Assert.Equal(0UL, ZCurve.LastInBlock(0, 0));
			Assert.Equal(1UL, ZCurve.LastInBlock(1, 0));
			Assert.Equal(2UL, ZCurve.LastInBlock(2, 0));
			Assert.Equal(3UL, ZCurve.LastInBlock(3, 0));
			Assert.Equal(4UL, ZCurve.LastInBlock(4, 0));
			Assert.Equal(256UL, ZCurve.LastInBlock(256, 0));
			Assert.Equal(ulong.MaxValue, ZCurve.LastInBlock(ulong.MaxValue, 0));

			Assert.Equal(3UL, ZCurve.LastInBlock(0, 1));
			Assert.Equal(15UL, ZCurve.LastInBlock(0, 2));
			Assert.Equal(63UL, ZCurve.LastInBlock(0, 3));
			Assert.Equal(255UL, ZCurve.LastInBlock(0, 4));
			Assert.Equal(65535UL, ZCurve.LastInBlock(0, 8));
			Assert.Equal(0x3FFFFFFFFFFFFFFFUL, ZCurve.LastInBlock(0, 31));
			Assert.Equal(ulong.MaxValue, ZCurve.LastInBlock(0, 32));

			Assert.Equal(15UL, ZCurve.LastInBlock(1, 2));

			Assert.Equal(0x3FFFFFFFFFFFFFFFUL, ZCurve.LastInBlock(0, 31));
			Assert.Equal(0x7FFFFFFFFFFFFFFFUL, ZCurve.LastInBlock(1L << 62, 31));
			Assert.Equal(0xFFFFFFFFFFFFFFFFUL, ZCurve.LastInBlock(0, 32));
		}

		[Fact]
		public void CellsPerBlockTest()
		{
			Assert.Equal(1UL, ZCurve.CellsPerBlock(0));
			Assert.Equal(4UL, ZCurve.CellsPerBlock(1));
			Assert.Equal(16UL, ZCurve.CellsPerBlock(2));
			Assert.Equal(64UL, ZCurve.CellsPerBlock(3));
			Assert.Equal(256UL, ZCurve.CellsPerBlock(4));
			Assert.Equal(1024UL, ZCurve.CellsPerBlock(5));
			Assert.Equal(0x0400000000000000UL, ZCurve.CellsPerBlock(29));
			Assert.Equal(0x1000000000000000UL, ZCurve.CellsPerBlock(30));
			Assert.Equal(0x4000000000000000UL, ZCurve.CellsPerBlock(31));
			Assert.Equal(0UL, ZCurve.CellsPerBlock(32)); // wrap around
		}

		[Fact]
		public void LargestLevel1Test()
		{
			Assert.Equal(32, ZCurve.LargestLevel(0));
			Assert.Equal(0, ZCurve.LargestLevel(1));
			Assert.Equal(0, ZCurve.LargestLevel(2));
			Assert.Equal(0, ZCurve.LargestLevel(3));
			Assert.Equal(1, ZCurve.LargestLevel(4));
			Assert.Equal(2, ZCurve.LargestLevel(16));
			Assert.Equal(3, ZCurve.LargestLevel(64));
			Assert.Equal(4, ZCurve.LargestLevel(256));
			Assert.Equal(0, ZCurve.LargestLevel(long.MaxValue));
			Assert.Equal(30, ZCurve.LargestLevel(1UL << 60)); // 0x100...
			Assert.Equal(30, ZCurve.LargestLevel(2UL << 60)); // 0x200...
			Assert.Equal(30, ZCurve.LargestLevel(3UL << 60)); // 0x300...
			Assert.Equal(31, ZCurve.LargestLevel(1UL << 62)); // 0x400...
			Assert.Equal(31, ZCurve.LargestLevel(2UL << 62)); // 0x800...
			Assert.Equal(31, ZCurve.LargestLevel(3UL << 62)); // 0xC00...
			Assert.Equal(0, ZCurve.LargestLevel(ulong.MaxValue)); // 0xFF...FF
		}

		[Fact]
		public void LargestLevel2Test()
		{
			Assert.Equal(0, ZCurve.LargestLevel(48, 48));

			Assert.Equal(2, ZCurve.LargestLevel(0, 49));
			Assert.Equal(0, ZCurve.LargestLevel(1, 49));
			Assert.Equal(0, ZCurve.LargestLevel(2, 49));
			Assert.Equal(0, ZCurve.LargestLevel(3, 49));
			Assert.Equal(1, ZCurve.LargestLevel(4, 49));
			Assert.Equal(0, ZCurve.LargestLevel(5, 49));

			Assert.Equal(3, ZCurve.LargestLevel(0, 192));
			Assert.Equal(0, ZCurve.LargestLevel(3, 192));
			Assert.Equal(1, ZCurve.LargestLevel(12, 192));
			Assert.Equal(0, ZCurve.LargestLevel(15, 192));
			Assert.Equal(2, ZCurve.LargestLevel(48, 192));
			Assert.Equal(0, ZCurve.LargestLevel(51, 192));
			Assert.Equal(1, ZCurve.LargestLevel(60, 192));
			Assert.Equal(0, ZCurve.LargestLevel(63, 192));
			Assert.Equal(0, ZCurve.LargestLevel(192, 192));

			// bad args (z>zmax) give level 0:
			Assert.Equal(0, ZCurve.LargestLevel(60, 48));
			Assert.Equal(0, ZCurve.LargestLevel(195, 192));
		}

		[Fact]
		public void LargestLevel3Test()
		{
			var levels12 = new[] { 1, 2 };

			Assert.Equal(1, ZCurve.LargestLevel(48, 48, levels12));
			Assert.Equal(1, ZCurve.LargestLevel(60, 48, levels12));

			Assert.Equal(2, ZCurve.LargestLevel(0, 49, levels12));
			Assert.Equal(1, ZCurve.LargestLevel(1, 49, levels12));
			Assert.Equal(1, ZCurve.LargestLevel(2, 49, levels12));
			Assert.Equal(1, ZCurve.LargestLevel(3, 49, levels12));
			Assert.Equal(1, ZCurve.LargestLevel(4, 49, levels12));
			Assert.Equal(1, ZCurve.LargestLevel(5, 49, levels12));

			Assert.Equal(1, ZCurve.LargestLevel(48, 49, levels12));
			Assert.Equal(2, ZCurve.LargestLevel(48, 63, levels12));

			var levels24 = new[] { 2, 4 };

			Assert.Equal(4, ZCurve.LargestLevel(0, 768, levels24));
			Assert.Equal(2, ZCurve.LargestLevel(128, 768, levels24));
			Assert.Equal(2, ZCurve.LargestLevel(31, 31, levels24));
		}

		[Fact]
		public void QueryBlocksTrial()
		{
			// Expect shortest ordered sequence of blocks covering query box

			// Query box:
			ulong qlo = ZCurve.Encode(1U, 1U); // 3
			ulong qhi = ZCurve.Encode(5U, 4U); // 49

			// Expected result: 3 6 7 9 11 (12..15) 18 19 (24..27) 33 36 37 48 49
			var starts = new ulong[] { 3, 6, 7, 9, 11, 12, 18, 19, 24, 33, 36, 37, 48, 49 };
			var levels = new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0 };
			var expected = starts.Zip(levels, (s, l) => new ZCurve.Block(s, l)).ToList();
			var actual = new List<ZCurve.Block>();

			ulong first = ZCurve.FirstInBlock(qlo, 0);
			ulong last = ZCurve.FirstInBlock(qhi, 0);

			for (ulong cursor = first; ; )
			{
				int level = ZCurve.LargestLevel(cursor, last);
				ulong increment = ZCurve.CellsPerBlock(level);

				actual.Add(new ZCurve.Block(cursor, level));

				cursor += increment;

				if (cursor > last) break;

				if (!ZCurve.IsInsideBox(cursor, first, last))
					cursor = ZCurve.NextInsideBox(cursor, first, last);
			}

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void GetQueryBlocks_General()
		{
			var blocks1 = ZCurve.GetQueryBlocks(1, 1, 5, 4); //3..49
			var expected1 = Blocks(3, 0, 6, 0, 7, 0, 9, 0, 11, 0, 12, 1, 18, 0, 19, 0, 24, 1, 33, 0, 36, 0, 37, 0, 48, 0, 49, 0);
			Assert.Equal(expected1, blocks1);

			var blocks2 = ZCurve.GetQueryBlocks(4, 3, 8, 7); //26..106
			var expected2 = Blocks(26, 0, 27, 0, 30, 0, 31, 0, 48, 2, 74, 0, 96, 0, 98, 0, 104, 0, 106, 0);
			Assert.Equal(expected2, blocks2);

			var blocks3 = ZCurve.GetQueryBlocks(0, 0, 15, 15);
			var expected3 = Blocks(0, 4);
			Assert.Equal(expected3, blocks3);

			var blocks4 = ZCurve.GetQueryBlocks(0, 0, 15, 15, new[]{1, 3});
			var expected4 = Blocks(0, 3, 64, 3, 128, 3, 192, 3);
			Assert.Equal(expected4, blocks4);

			var blocks5 = ZCurve.GetQueryBlocks(7, 3, 13, 7, new[]{1, 2, 3}); // 31..123
			var expected5 = Blocks(28, 1, 52, 1, 60, 1, 72, 1, 76, 1, 88, 1, 96, 2, 112, 1, 120, 1);
			Assert.Equal(expected5, blocks5);
		}

		[Fact]
		public void GetQueryBlocks_WithinBlock()
		{
			var q = ZCurve.Encode(7, 10); // 157

			var blocks1 = ZCurve.GetQueryBlocks(q, q, new[]{0, 1, 2, 3});
			Assert.Equal(Blocks(157, 0), blocks1);

			var blocks2 = ZCurve.GetQueryBlocks(q, q, new[]{1, 2, 3});
			Assert.Equal(Blocks(156, 1), blocks2);

			var blocks3 = ZCurve.GetQueryBlocks(q, q, new[]{2, 3});
			Assert.Equal(Blocks(144, 2), blocks3);

			var blocks4 = ZCurve.GetQueryBlocks(q, q, new[]{3});
			Assert.Equal(Blocks(128, 3), blocks4);
		}

		[Fact]
		public void GetQueryBlocks_AcrossBlock()
		{
			var qlo = ZCurve.Encode(7, 7); // 63
			var qhi = ZCurve.Encode(8, 8); // 192

			var blocks1 = ZCurve.GetQueryBlocks(qlo, qhi); // all levels
			var expected1 = Blocks(63, 0, 106, 0, 149, 0, 192, 0);
			Assert.Equal(expected1, blocks1);

			var blocks2 = ZCurve.GetQueryBlocks(qlo, qhi, new[]{1, 2});
			var expected2 = Blocks(60, 1, 104, 1, 148, 1, 192, 1);
			Assert.Equal(expected2, blocks2);

			var blocks3 = ZCurve.GetQueryBlocks(qlo, qhi, new[]{2});
			var expected3 = Blocks(48, 2, 96, 2, 144, 2, 192, 2);
			Assert.Equal(expected3, blocks3);
		}

		[Fact]
		public void GetQueryBlocks_SingleCell()
		{
			var q1 = ZCurve.Encode(6, 4); // 52
			var blocks1 = ZCurve.GetQueryBlocks(q1, q1); // single cell, all levels
			var expected1 = Blocks(52, 0);
			Assert.Equal(expected1, blocks1);

			var q2 = ZCurve.Encode(11, 11); // 207
			var blocks2 = ZCurve.GetQueryBlocks(q2, q2); // single cell, all levels
			var expected2 = Blocks(207, 0);
			Assert.Equal(expected2, blocks2);

			var q3 = ZCurve.Encode(5, 1); // 19
			var blocks3 = ZCurve.GetQueryBlocks(q3, q3, new[]{2}); // single cell, specific levels
			var expected3 = Blocks(16, 2);
			Assert.Equal(expected3, blocks3);

			var q4 = ZCurve.Encode(uint.MaxValue, uint.MaxValue); // ulong.MaxValue
			var blocks4 = ZCurve.GetQueryBlocks(q4, q4, new[]{3});
			var expected4 = Blocks(ulong.MaxValue - 63, 3);
			Assert.Equal(expected4, blocks4);
		}

		private static IEnumerable<ZCurve.Block> Blocks(params ulong[] values)
		{
			Assert.True(values.Length % 2 == 0, "Need even number of values");

			for (int i = 0; i < values.Length; i += 2)
			{
				yield return new ZCurve.Block(values[i], (int) values[i+1]);
			}
		}
	}
}
