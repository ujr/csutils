using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class ParsingTest
	{
		[Fact]
		public void CanParseFieldList()
		{
			var r0 = Parsing.ParseFieldList(null, ':');
			Assert.Empty(r0);

			var r1 = Parsing.ParseFieldList("a:b:c", ':');
			Assert.Equal(Seq("a", "b", "c"), r1);

			var r2 = Parsing.ParseFieldList("a:b:c", ':', 1, 0);
			Assert.Equal(Seq(""), r2);

			var r3 = Parsing.ParseFieldList("a:b:c", ':', 1, 1);
			Assert.Equal(Seq("", ""), r3);

			var r4 = Parsing.ParseFieldList("a:b:c", ':', 2, 1);
			Assert.Equal(Seq("b"), r4);

			var r5 = Parsing.ParseFieldList("foo: bar :baz", ':', 3, 9);
			Assert.Equal(Seq("", "bar", "ba"), r5);

			var r6 = Parsing.ParseFieldList("", ':');
			Assert.Equal(Seq(""), r6);

			var r7 = Parsing.ParseFieldList(":::", ':');
			Assert.Equal(Seq("", "", "", ""), r7);

			var r8 = Parsing.ParseFieldList(": ", ':');
			Assert.Equal(Seq("", ""), r8);

			var r9 = Parsing.ParseFieldList(" : foo : ", ':', 2, 5);
			Assert.Equal(Seq("foo"), r9);
		}

		[Fact]
		public void CanParsePageList()
		{
			Assert.Empty(Parsing.ParsePageList(null));
			Assert.Empty(Parsing.ParsePageList(string.Empty));
			Assert.Empty(Parsing.ParsePageList("  "));

			var r1 = Parsing.ParsePageList("1,5-7,12,14,29-27");
			Assert.Equal(Seq(1, 5, 6, 7, 12, 14, 29, 28, 27), r1);

			// white space is ignored
			var r2 = Parsing.ParsePageList(" 1 , 5 - 7 , 12 , 14 , 29 - 27 ");
			Assert.Equal(Seq(1, 5, 6, 7, 12, 14, 29, 28, 27), r2);

			// comma as separator is optional
			var r3 = Parsing.ParsePageList("1 5-7 12 14 29-27");
			Assert.Equal(Seq(1, 5, 6, 7, 12, 14, 29, 28, 27), r3);

			// a single number is valid input
			var r4 = Parsing.ParsePageList("99");
			Assert.Equal(Seq(99), r4);

			// the "singleton range"
			var r5 = Parsing.ParsePageList("5-5");
			Assert.Equal(Seq(5), r5);

			// numbers up to int.MaxValue are fine (beware of wrap-around)
			var r6 = Parsing.ParsePageList("2147483647-2147480647");
			var e6 = Enumerable.Range(int.MaxValue - 3000, 3001).Reverse();
			Assert.Equal(e6, r6);
			var r7 = Parsing.ParsePageList("2147480647-2147483647");
			var e7 = Enumerable.Range(int.MaxValue - 3000, 3001);
			Assert.Equal(e7, r7);
			// and down to zero
			var r8 = Parsing.ParsePageList("5-0");
			Assert.Equal(Seq(5, 4, 3, 2, 1, 0), r8);

			// detect syntax errors (ToList to force enumeration)
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("-3").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("1,,2").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("1--2").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("3-").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("1,-2").ToList());

			// detect numeric overflow (ToList to force enumeration)
			Assert.Throws<OverflowException>(() => Parsing.ParsePageList("2147483648").ToList());
			Assert.Throws<OverflowException>(() => Parsing.ParsePageList("21474836400").ToList());
		}

		#region Private test utils

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}

		#endregion
	}
}
