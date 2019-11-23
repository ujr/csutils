using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class SparseBitSetTest
	{
		private readonly ITestOutputHelper _output;

		public SparseBitSetTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Theory]
		[InlineData(100*1000*1000, 2.0)]
		[InlineData(int.MaxValue, 25.0)]
		public void PerformanceTest(int length, double maxAllowedSecs)
		{
			const int seed = 1234;
			var startTime = DateTime.Now;
			int count = length / 100;
			var largeSet = new SparseBitSet(length);

			var random = new Random(seed);
			for (int k = 0; k < count; k++)
			{
				int i = random.Next(length);
				largeSet.Set(i);
			}

			random = new Random(seed);
			for (int k = 0; k < count; k++)
			{
				int i = random.Next(length);
				Assert.True(largeSet.Get(i));
			}

			random = new Random(seed);
			for (int k = 0; k < count; k++)
			{
				int i = random.Next(length);
				largeSet.Clear(i);
			}

			var elapsed = DateTime.Now - startTime;

			_output.WriteLine(@"size={0:N0} count={1:N0} set/get/clear elapsed={2}",
				length, count, elapsed);

			Assert.Equal(0, largeSet.Cardinality);
			Assert.True(elapsed < TimeSpan.FromSeconds(maxAllowedSecs), "Too slow");
		}

		[Fact]
		public void NextSetBitTest()
		{
			var set = new SparseBitSet(10000); // 3 chunks, bits 0..9999
			set.Set(0).Set(1).Set(2).Set(10).Set(100); // all in 1st chunk
			set.Set(5000).Set(5001).Set(5100); // all in 2nd chunk
			Assert.Equal(8, set.Cardinality);
			Assert.Equal(0, set.NextSetBit(0));
			Assert.Equal(1, set.NextSetBit(1));
			Assert.Equal(2, set.NextSetBit(2));
			Assert.Equal(10, set.NextSetBit(3));
			Assert.Equal(10, set.NextSetBit(10));
			Assert.Equal(100, set.NextSetBit(11));
			Assert.Equal(100, set.NextSetBit(100));
			Assert.Equal(5000, set.NextSetBit(101));
			Assert.Equal(5000, set.NextSetBit(5000));
			Assert.Equal(5001, set.NextSetBit(5001));
			Assert.Equal(5100, set.NextSetBit(5002));
			Assert.Equal(5100, set.NextSetBit(5100));
			Assert.Equal(-1, set.NextSetBit(5101)); // no next set bit
		}

		[Fact]
		public void SingleChunkTest()
		{
			var singleChunkSet = new SparseBitSet(4096);
			Assert.Equal(0, singleChunkSet.Cardinality);
			singleChunkSet.Set(2345).Set(64);
			Assert.Equal(2, singleChunkSet.Cardinality);
			Assert.True(singleChunkSet.Get(64));
			Assert.False(singleChunkSet.Get(65));
			Assert.True(singleChunkSet.Get(2345));
			Assert.False(singleChunkSet.Get(2346));
			Assert.Equal(64, singleChunkSet.NextSetBit(0));
			Assert.Equal(2345, singleChunkSet.NextSetBit(64 + 1));
			Assert.Equal(-1, singleChunkSet.NextSetBit(2345 + 1));
		}

		[Fact]
		public void SingletonTest()
		{
			var singletonSet = new SparseBitSet(1);
			Assert.False(singletonSet.Get(0));
			Assert.Equal(0, singletonSet.Cardinality);
			singletonSet.Set(0).Clear(0).Set(0);
			Assert.True(singletonSet.Get(0));
			Assert.Equal(1, singletonSet.Cardinality);
			Assert.Equal(1, singletonSet.Length);
			Assert.Equal(0, singletonSet.NextSetBit(0));
			Assert.Equal(-1, singletonSet.NextSetBit(1));
		}

		[Fact]
		public void IterationTest()
		{
			const int bitSetSize = 1000;
			const int sampleSize = bitSetSize/10;

			var set = new SparseBitSet(bitSetSize);

			// Generate random but unique bit numbers:
			var bitnums = Enumerable.Range(0, set.Length).ReservoirSample(sampleSize);

			foreach (int i in bitnums)
			{
				set.Set(i);
			}

			Assert.Equal(100, set.Cardinality);

			// Retrieve ids in ascending order:
			var actual = new List<int>();
			int id = -1;
			while ((id = set.NextSetBit(id+1)) >= 0)
			{
				actual.Add(id);
			}

			var expected = bitnums.ToList();
			expected.Sort((a,b) => a.CompareTo(b));
			Assert.Equal(expected, actual);
		}
	}
}
