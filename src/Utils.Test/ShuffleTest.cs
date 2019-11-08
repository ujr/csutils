using System;
using System.Collections.Generic;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class ShuffleTest
	{
		[Fact]
		public void Shuffle_Empty_Noop()
		{
			var list = new int[0]; // empty

			Algorithms.Shuffle(list);

			Assert.Empty(list);
		}

		[Fact]
		public void Shuffle_Singleton_Noop()
		{
			var list = new List<int> { 42 };

			list.Shuffle();

			Assert.Equal(new int[]{42}, list);
		}

		[Fact]
		public void Shuffle_Preserve_Items()
		{
			var list = new int[] {1, 2, 3};

			list.Shuffle();

			Assert.Contains(1, list);
			Assert.Contains(2, list);
			Assert.Contains(3, list);
		}

		[Fact]
		public void Shuffle_Null_Throws()
		{
			IList<int> list = null;

			Assert.Throws<ArgumentNullException>(() => Algorithms.Shuffle(list));
		}
	}
}
