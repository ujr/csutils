using System;
using System.Linq;
using Xunit;

namespace Sylphe.Utils.Test
{
    public class ReservoirTest
    {
        [Fact]
        public void ReservoirSample_Null_Throws()
        {
            int[] sequence = null;
            const int sampleSize = 5;

            Assert.Throws<ArgumentNullException>(() => Algorithms.ReservoirSample(sequence, sampleSize));
        }

        [Fact]
        public void ReservoirSample_Empty_ReturnsEmpty()
        {
            var sequence = new int[0]; // empty
            const int sampleSize = 5;

            var sample = sequence.ReservoirSample(sampleSize);

            Assert.Empty(sample);
        }

        [Fact]
        public void ReservoirSample_SampleIsSubset()
        {
            var sequence = Enumerable.Range(0, 20);
            const int sampleSize = 5;

            var sample = sequence.ReservoirSample(sampleSize);

            Action<int> fromSequence = x => sequence.Contains(x);
            Assert.Collection(sample, fromSequence, fromSequence, fromSequence, fromSequence, fromSequence);
        }

        [Fact]
        public void ReservoirSample_SampleLargerThanSequence_ReturnsSequence()
        {
            var sequence = Enumerable.Range(5, 3); // 5, 6, 7
            const int sampleSize = 5;

            var sample = sequence.ReservoirSample(sampleSize);

            Assert.Equal(3, sample.Count);
            Assert.Contains(5, sample);
            Assert.Contains(6, sample);
            Assert.Contains(7, sample);
        }
    }
}
