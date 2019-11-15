using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class StringUtilsTest
	{
		[Fact]
		public void CanTrim()
		{
			Assert.Null(StringUtils.Trim(null));
			Assert.Empty(StringUtils.Trim(string.Empty));
			Assert.Equal("foo bar", StringUtils.Trim("foo bar"));
			Assert.Equal("foo bar", StringUtils.Trim("  foo bar  "));
		}

		[Fact]
		public void CanTrimCanonical()
		{
			Assert.Null(StringUtils.TrimCanonical(null));
			Assert.Null(StringUtils.TrimCanonical(string.Empty));
			Assert.Null(StringUtils.TrimCanonical("\t \n \r"));
			Assert.Equal("foo", StringUtils.TrimCanonical("foo"));
			Assert.Equal("foo bar", StringUtils.TrimCanonical("  foo bar  "));
		}

		[Fact]
		public void CanTrimStringBuilder()
		{
			var sb = new StringBuilder();

			var self = sb.Trim();
			Assert.Same(sb, self);
			Assert.Equal(0, sb.Length);

			sb.Clear().Append(" \t foo \r\n bar \n\r\n");
			Assert.Equal("foo \r\n bar", sb.Trim().ToString());

			sb.Clear().Append(" \t \n \v \f \r ");
			Assert.Empty(sb.Trim().ToString());

			Assert.Null(StringUtils.Trim((StringBuilder) null));
		}

		[Fact]
		public void CanJoin()
		{
			Assert.Equal(string.Empty, StringUtils.Join<string>(null, null));
			Assert.Equal("abcd", StringUtils.Join(null, Seq("ab", "cd")));
			Assert.Equal("abcd", StringUtils.Join(string.Empty, Seq("ab", "cd")));
			Assert.Equal("ab*cd", StringUtils.Join("*", Seq("ab", "cd")));
			Assert.Equal(string.Empty, StringUtils.Join("*", new string[0]));
			Assert.Equal("abc", StringUtils.Join("*", Seq("abc")));
			Assert.Equal("abc*def*ghi", StringUtils.Join("*", Seq("abc", "def", "ghi")));
		}

		[Fact]
		public void CanCommonPrefixLength()
		{
			Assert.Equal(0, StringUtils.CommonPrefixLength(null, null));
			Assert.Equal(0, StringUtils.CommonPrefixLength("a", "b"));
			Assert.Equal(3, StringUtils.CommonPrefixLength("define", "default"));
			Assert.Equal(2, StringUtils.CommonPrefixLength("define", "default", 2));
		}

		[Fact]
		public void CanNextKeyOrdinal()
		{
			Assert.Null(StringUtils.NextKeyOrdinal(null));

			// empty is prefix of all strings, thus no next key

			Assert.Null(StringUtils.NextKeyOrdinal(string.Empty));

			Assert.Equal("abd", StringUtils.NextKeyOrdinal("abc"));

			// "abz" => "ac" (where z stands for char.MaxValue)

			string abz = new string(new[] {'a', 'b', char.MaxValue});
			Assert.Equal("ac", StringUtils.NextKeyOrdinal(abz));

			// next string after "z" is "za" but then "z" is a common prefix
			// (here "z" stands for char.MaxValue and "a" for char.MinValue)

			string z = new string(char.MaxValue, 1);
			Assert.Null(StringUtils.NextKeyOrdinal(z));
		}

		[Fact]
		public void CanCompareOrdinal()
		{
			int r = StringUtils.CompareOrdinal(string.Empty, string.Empty, '#');
			Assert.True(r == 0);

			r = StringUtils.CompareOrdinal(string.Empty, "a", '#');
			Assert.True(r < 0);

			r = StringUtils.CompareOrdinal("aaa", string.Empty, '#');
			Assert.True(r > 0);

			r = StringUtils.CompareOrdinal("a", "b", 'a');
			Assert.True(r < 0); // this essentially compares empty against "b"

			r = StringUtils.CompareOrdinal("foo", "foo#bar", '#');
			Assert.True(r == 0);

			r = StringUtils.CompareOrdinal("foo#bar", "foo#baz", '#');
			Assert.True(r == 0);

			r = StringUtils.CompareOrdinal("foo#bar", "foo#baz", '*');
			Assert.True(r < 0);
		}

		#region Private test utils

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}

		#endregion
	}
}
