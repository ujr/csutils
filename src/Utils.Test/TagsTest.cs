using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Assert = Xunit.Assert;

namespace Sylphe.Utils.Test
{
	public class TagsTest
	{
		[Fact]
		public void CanAddTag()
		{
			Assert.Null(Tags.AddTag(null, null));
			Assert.Equal("foo", Tags.AddTag(null, "foo"));
			Assert.Equal("foo", Tags.AddTag(string.Empty, "foo"));
			Assert.Equal("foo", Tags.AddTag("foo", null));
			Assert.Equal("foo", Tags.AddTag("foo", string.Empty));
			Assert.Equal("foo,bar,new", Tags.AddTag("foo,bar", "new"));
			Assert.Equal("foo,bar", Tags.AddTag("foo,bar", "foo"));
		}

		[Fact]
		public void CanAddTags()
		{
			Assert.Null(Tags.AddTags(null, null));
			Assert.Equal("foo", Tags.AddTags(null, "foo"));
			Assert.Equal("foo,bar", Tags.AddTags(null, "foo", "bar"));
			Assert.Equal("foo,bar", Tags.AddTags("foo", "foo", "bar"));
			Assert.Equal("foo,bar", Tags.AddTags("foo,bar", "foo", "bar"));
			Assert.Equal("foo,bar,new", Tags.AddTags("foo,bar", "bar", "new"));
			Assert.Equal("foo,bar", Tags.AddTags("foo,bar", null));
			Assert.Equal("foo,bar", Tags.AddTags("foo,bar"));
		}

		[Fact]
		public void CanHasTag()
		{
			Assert.False(Tags.HasTag(null, null));
			Assert.False(Tags.HasTag(null, "foo"));
			Assert.False(Tags.HasTag("foo,,bar", null));
			Assert.False(Tags.HasTag("foo,,bar", string.Empty));
			Assert.True(Tags.HasTag("start,mid,end", "start"));
			Assert.True(Tags.HasTag("start,mid,end", "mid"));
			Assert.True(Tags.HasTag("start,mid,end", "end"));
			Assert.False(Tags.HasTag("start,mid,end", "NoSuchTag"));
			Assert.False(Tags.HasTag("start,mid,end", "art"));

			// HasTag is separator-agnostic:
			Assert.True(Tags.HasTag("start; mid, end", "mid"));
			Assert.True(Tags.HasTag("start  mid  end", "mid"));
			Assert.True(Tags.HasTag("start, mid; end", "mid"));
		}

		[Fact]
		public void CanHasTags()
		{
			Assert.False(Tags.HasTags(null, null));
			Assert.False(Tags.HasTags(null, ""));
			Assert.False(Tags.HasTags(null, "foo"));
			Assert.False(Tags.HasTags(null, "foo", "bar"));

			Assert.False(Tags.HasTags("foo", null));
			Assert.False(Tags.HasTags("foo", ""));
			Assert.True(Tags.HasTags("foo", "foo"));
			Assert.False(Tags.HasTags("foo", "foo", "bar"));

			Assert.False(Tags.HasTags("foo,bar", null));
			Assert.False(Tags.HasTags("foo,bar", ""));
			Assert.True(Tags.HasTags("foo,bar", "foo"));
			Assert.True(Tags.HasTags("foo,bar", "foo", "bar"));

			// HasTags is separator-agnostic:
			Assert.True(Tags.HasTags(" foo ; bar ; baz", "foo", "bar"));
			Assert.True(Tags.HasTags(" foo   bar   baz", "bar", "baz"));
		}

		[Fact]
		public void CanSplitTags()
		{
			AssertTags(Tags.SplitTags(null).ToArray());
			AssertTags(Tags.SplitTags(string.Empty).ToArray());
			AssertTags(Tags.SplitTags(" \t").ToArray());
			AssertTags(Tags.SplitTags("foo,bar;baz").ToArray(), "foo", "bar", "baz");
			AssertTags(Tags.SplitTags(" , foo ,, bar ;; baz ; ").ToArray(), "foo", "bar", "baz");
		}

		[Fact]
		public void CanJoinTags()
		{
			Assert.Equal("foo,bar,baz", Tags.JoinTags(new[] { "foo", "bar", "  baz  " }));
		}

		[Fact]
		public void CanSameTags()
		{
			Assert.True(Tags.SameTags(null, null));
			Assert.True(Tags.SameTags(null, string.Empty));
			Assert.True(Tags.SameTags(" \t ", null));
			Assert.True(Tags.SameTags("foo", "foo"));
			Assert.True(Tags.SameTags("foo,bar", " foo ; bar "));
			Assert.True(Tags.SameTags("foo,bar", "bar,foo"));
			Assert.True(Tags.SameTags("foo,bar,foo", "bar,foo,bar"));
			Assert.True(Tags.SameTags("foo,bar", "bar,bar;foo,foo"));
			Assert.True(Tags.SameTags("bar,bar;foo;foo", "foo,bar"));

			Assert.False(Tags.SameTags("foo", "   "));
			Assert.False(Tags.SameTags("foo,bar,baz", "foo,bar,BAZ"));
		}

		[Fact]
		public void CanBehavior()
		{
			var a = new Tags("foo, bar");

			Assert.Equal(new Tags("foo,bar"), a.AddTag("bar"));
			Assert.Equal(new Tags("foo,bar,baz"), a.AddTag("baz"));
			Assert.Equal(new Tags("foo,bar,baz,aar"), a.AddTags("baz", "aar"));

			Assert.True(a.HasTag("foo"));
			Assert.False(a.HasTag("quux"));
			Assert.True(a.HasTags("foo", "bar"));
			Assert.False(a.HasTags("bar", "quux"));

			Assert.Contains("foo", (IEnumerable<string>) a);
			Assert.Contains("bar", (IEnumerable<string>) a);
			Assert.DoesNotContain("quux", (IEnumerable<string>) a);

			Assert.NotEqual(new Tags("foo"), new Tags("foo,bar"));
			Assert.Equal(new Tags((string) null), new Tags(string.Empty));
			Assert.Equal(new Tags(string.Empty), new Tags((string) null));
			Assert.Equal(new Tags("foo,bar"), new Tags("bar,foo"));
		}

		[Fact]
		public void CanCast()
		{
			var t = new Tags("foo", "bar");
			Assert.Equal("foo,bar", t); // implicit cast Tags to String
			Assert.Equal(t, (Tags) "foo,bar"); // explicit cast String to Tags
		}

		[Fact]
		public void CanDefault()
		{
			Tags d = default;
			Assert.False(d.HasTag("foo"));
			Assert.False(d.HasTag(string.Empty));
			Assert.False(d.HasTag(null));
			Assert.Empty(d);
		}

		private static void AssertTags(string[] actual, params string[] expected)
		{
			if (actual == null && expected == null) return;
			Assert.NotNull(actual);
			Assert.NotNull(expected);
			Assert.Equal(expected.Length, actual.Length);
			for (int i = 0; i < actual.Length; i++)
			{
				Assert.Equal(expected[i], actual[i], StringComparer.Ordinal);
			}
		}
	}
}
