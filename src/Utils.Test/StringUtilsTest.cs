using System;
using System.Collections.Generic;
using System.Linq;
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
		public void CanStringBuilderTrim()
		{
			var sb = new StringBuilder();

			var self = sb.Trim();
			Assert.Same(sb, self);
			Assert.Equal(0, sb.Length);

			sb.Clear().Append(" \t foo \r\n bar \n\r\n");
			Assert.Equal("foo \r\n bar", sb.Trim().ToString());

			sb.Clear().Append(" \t \n \v \f \r ");
			Assert.Empty(sb.Trim().ToString());

			Assert.Null(((StringBuilder) null).Trim());
		}

		[Fact]
		public void CanStringBuilderEndsWith()
		{
			var sb = new StringBuilder();

			Assert.True(sb.EndsWith(null));
			Assert.True(sb.EndsWith(string.Empty));
			Assert.False(sb.EndsWith("foo"));

			sb.Append("FooBar");

			Assert.True(sb.EndsWith(""));
			Assert.True(sb.EndsWith("Bar"));
			Assert.False(sb.EndsWith("bar"));
			Assert.True(sb.EndsWith("bar", StringComparison.OrdinalIgnoreCase));
			Assert.True(sb.EndsWith(sb.ToString()));
		}

		[Fact]
		public void CanStringBuilderReverse()
		{
			var sb = new StringBuilder();
			sb.Append("Hallelujah");

			var o = sb.Reverse(4, 3);
			Assert.Equal("Hallulejah", sb.ToString());
			Assert.Same(sb, o);

			sb.Reverse(sb.Length, 0); // border case
			Assert.Equal("Hallulejah", sb.ToString());

			sb.Clear();
			sb.Reverse(0, 0);
			Assert.Equal(0, sb.Length);
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
		public void CanRemoveDiacritics()
		{
			Assert.Null(StringUtils.RemoveDiacritics(null));
			Assert.Empty(string.Empty.RemoveDiacritics());

			Assert.Equal("aouAOUß", "äöüÄÖÜß".RemoveDiacritics());
			Assert.Equal("aaceeei", "àâçéèêï".RemoveDiacritics());

			Assert.Equal("łŁ", "łŁ".RemoveDiacritics()); // sic
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
			Assert.True(StringUtils.CompareOrdinal(string.Empty, string.Empty, '#') == 0);
			Assert.True(StringUtils.CompareOrdinal(string.Empty, "a", '#') < 0);
			Assert.True(StringUtils.CompareOrdinal("aaa", string.Empty, '#') > 0);
			Assert.True(StringUtils.CompareOrdinal("a", "b", 'a') < 0); // this essentially compares empty against "b"
			Assert.True(StringUtils.CompareOrdinal("foo", "foo#bar", '#') == 0);
			Assert.True(StringUtils.CompareOrdinal("foo#bar", "foo#baz", '#') == 0);
			Assert.True(StringUtils.CompareOrdinal("foo#bar", "foo#baz", '*') < 0);
		}

		[Fact]
		public void CanCompareLogical()
		{
			Assert.True(StringUtils.CompareLogical(null, null) == 0);
			Assert.True(StringUtils.CompareLogical(null, string.Empty) < 0);
			Assert.True(StringUtils.CompareLogical("abc", null) > 0);
			Assert.True(StringUtils.CompareLogical(string.Empty, string.Empty) == 0);
			Assert.True(StringUtils.CompareLogical(string.Empty, "abc") < 0);

			Assert.True(StringUtils.CompareLogical("abc", "ABC") > 0); // case sensitive (by default)
			Assert.True(StringUtils.CompareLogical("abc", "ABC", StringComparison.OrdinalIgnoreCase) == 0);

			Assert.True(StringUtils.CompareLogical("123", "123") == 0);
			Assert.True(StringUtils.CompareLogical("123", "124") < 0);
			Assert.True(StringUtils.CompareLogical("123", "122") > 0);

			Assert.True(StringUtils.CompareLogical("", "0") < 0);
			Assert.True(StringUtils.CompareLogical("xx", "12") > 0); // numeric before text
			Assert.True(StringUtils.CompareLogical("12", "xx") < 0);

			Assert.True(StringUtils.CompareLogical("aa1", "aa2") < 0);
			Assert.True(StringUtils.CompareLogical("aa11", "aa2") > 0);
			Assert.True(StringUtils.CompareLogical("aa11", "aa2x") > 0);
			Assert.True(StringUtils.CompareLogical("aa11", "aa12") < 0);

			Assert.True(StringUtils.CompareLogical("aa11b21.txt", "aa11b21.txt") == 0);
			Assert.True(StringUtils.CompareLogical("aa11b3.txt", "aa11b21.txt") < 0);

			Assert.True(StringUtils.CompareLogical("ab12c3def456z", "ab12c3def456z") == 0);
			Assert.True(StringUtils.CompareLogical("ab12c3def456z", "ab12c3def456") > 0);
			Assert.True(StringUtils.CompareLogical("ab12c3def456z", "ab12c3def456z!") < 0);
			Assert.True(StringUtils.CompareLogical("ab12c3def456z", "ab12c3def99z") > 0);

			// Leading zeros:
			Assert.Equal(0, StringUtils.CompareLogical("foo7", "foo7"));
			Assert.True(StringUtils.CompareLogical("foo7", "foo07") < 0);
			Assert.Equal(0, StringUtils.CompareLogical("foo07", "foo07"));
		}

		[Fact]
		public void CanSortLogical()
		{
			var names = new[] {"1", "01", "11", "10", "2", "_1", "a1b2", "a1b1", "a11b2", "a2b11", "a2b2", "42", "4.2", "end"};

			var comparer = new StringUtils.LogicalStringComparer();
			var ordered = names.OrderBy(s => s, comparer).ToList();

			Assert.True(ordered.SequenceEqual(Seq("1", "01", "2", "4.2", "10", "11", "42", "a1b1", "a1b2", "a2b2", "a2b11", "a11b2", "end", "_1")));

			var names2 = new[] {"abc", "Abc", "aBC", "ABC"};

			comparer = new StringUtils.LogicalStringComparer(); // default is ignore case
			ordered = names2.OrderBy(s => s, comparer).ToList();
			Assert.True(ordered.SequenceEqual(Seq("abc", "Abc", "aBC", "ABC")));

			comparer = new StringUtils.LogicalStringComparer(StringComparison.Ordinal);
			ordered = names2.OrderBy(s => s, comparer).ToList();
			Assert.True(ordered.SequenceEqual(Seq("ABC", "Abc", "aBC", "abc")));
		}

		#region Private test utils

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}

		#endregion
	}
}
