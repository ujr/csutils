using System;
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
		public void CanSkipWhiteSpace()
		{
			int r = StringUtils.SkipWhiteSpace("", 0);
			Assert.Equal(0, r);

			r = StringUtils.SkipWhiteSpace("foo", 0);
			Assert.Equal(0, r);

			r = StringUtils.SkipWhiteSpace(" bar ", 0);
			Assert.Equal(1, r);

			r = StringUtils.SkipWhiteSpace(" bar ", 1);
			Assert.Equal(1, r);

			r = StringUtils.SkipWhiteSpace(" bar ", 4);
			Assert.Equal(5, r);

			r = StringUtils.SkipWhiteSpace("foo ", 99);
			Assert.Equal(99, r);
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

		[Fact]
		public void CanParseFieldList()
		{
			var list = new List<string>();
			int r = StringUtils.ParseFieldList(list, "a:b:c", ':');
			Assert.Equal(3, r);
			Assert.Equal(Seq("a", "b", "c"), list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, "a:b:c", ':', 1, 0);
			Assert.Equal(0, r);
			Assert.Empty(list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, "a:b:c", ':', 1, 1);
			Assert.Equal(2, r);
			Assert.Equal(Seq(string.Empty, string.Empty), list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, "a:b:c", ':', 2, 1);
			Assert.Equal(1, r);
			Assert.Equal(Seq("b"), list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, "foo: bar :baz", ':', 3, 9);
			Assert.Equal(3, r);
			Assert.Equal(Seq("", "bar", "ba"), list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, "", ':', 0, 0);
			Assert.Equal(0, r);
			Assert.Empty(list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, ":::", ':', 0, 3);
			Assert.Equal(4, r);
			Assert.Equal(Seq("", "", "", ""), list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, ": ", ':', 0, 99);
			Assert.Equal(2, r);
			Assert.Equal(Seq("", ""), list);

			list.Clear();
			r = StringUtils.ParseFieldList(list, " : foo : ", ':', 2, 5);
			Assert.Equal(1, r);
			Assert.Equal(Seq("foo"), list);

			// The result list may be empty => just count fields
			r = StringUtils.ParseFieldList(null, "foo: bar: baz", ':', 0, 99);
			Assert.Equal(3, r);
		}

		[Fact]
		public void CanParsePageList()
		{
			var list = new List<int>();
			int r = StringUtils.ParsePageList(string.Empty, list, 10);
			Assert.Equal(0, r);
			Assert.Empty(list);

			list.Clear();
			r = StringUtils.ParsePageList("1", list, 10);
			Assert.Equal(1, r);
			Assert.Equal(Seq(1), list);

			list.Clear();
			r = StringUtils.ParsePageList("1,2,3", list, 10);
			Assert.Equal(3, r);
			Assert.Equal(Seq(1, 2, 3), list);

			list.Clear();
			r = StringUtils.ParsePageList("1-2", list, 10);
			Assert.Equal(2, r);
			Assert.Equal(Seq(1, 2), list);

			list.Clear();
			r = StringUtils.ParsePageList("2-1", list, 10);
			Assert.Equal(2, r);
			Assert.Equal(Seq(2, 1), list);

			list.Clear();
			r = StringUtils.ParsePageList("1-3,4,7-5,987654321,0", list, 8);
			Assert.Equal(9, r);
			// The last "0" is counted in r, but not added to list:
			Assert.Equal(Seq(1, 2, 3, 4, 7, 6, 5, 987654321), list);

			// The list arg is optional; if null, just count:
			r = StringUtils.ParsePageList("4-2,3-5", null, 0);
			Assert.Equal(6, r);
		}

		[Fact]
		public void CanParsePageListThrowOnBadSyntax()
		{
			Assert.Throws<ArgumentException>(() => StringUtils.ParsePageList("1,", null, 0));
			Assert.Throws<ArgumentException>(() => StringUtils.ParsePageList("1,3-", null, 0));
			Assert.Throws<ArgumentException>(() => StringUtils.ParsePageList("1,-2", null, 0));
		}

		#region Private test utils

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}

		#endregion
	}
}
