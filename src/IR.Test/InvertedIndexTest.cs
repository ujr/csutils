using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.IR.Test
{
	public class InvertedIndexTest
	{
		private readonly ITestOutputHelper _output;

		public InvertedIndexTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void CanParseQuery()
		{
			var q1 = Query.Parse(null);
			Assert.Equal("(NoDocs)", q1.ToString());

			var q2 = Query.Parse(string.Empty);
			Assert.Equal("(NoDocs)", q2.ToString());

			var q3 = Query.Parse("foo");
			Assert.Equal("foo", q3.ToString());

			var q4 = Query.Parse("foo.-bar");
			Assert.Equal("(AND foo (NOT bar))", q4.ToString());

			var q5 = Query.Parse(" foo , bar . baz ");
			Assert.Equal("(OR foo (AND bar baz))", q5.ToString());

			var q6 = Query.Parse("foo.-(bar,-baz)");
			Assert.Equal("(AND foo (NOT (OR bar (NOT baz))))", q6.ToString());

			var q7 = Query.Parse("-foo.bar,baz");
			Assert.Equal("(OR (AND (NOT foo) bar) baz)", q7.ToString());
		}

		[Fact]
		public void CanRewriteQueries()
		{
			var q1 = Query.Parse("(A.B).(C,D)").Rewrite();
			Assert.Equal("(AND A B (OR C D))", q1.ToString());

			var q2 = Query.Parse("-A.(-B)").Rewrite();
			Assert.Equal("(AND (NOT A) B)", q2.ToString());

			var q3 = Query.Parse("-(A,B).C").Rewrite();
			Assert.Equal("(AND (NOT A) (NOT B) C)", q3.ToString());

			var q4 = Query.Parse("- ( A , - ( B . C ) )").Rewrite();
			Assert.Equal("(AND (NOT A) B C)", q4.ToString());

			var q5 = Query.Parse("-(A.-(B,-C).D)").Rewrite();
			Assert.Equal("(OR (NOT A) B (NOT C) (NOT D))", q5.ToString());

			var q6 = Query.Parse("-(-A,B,-(C,D),E.-(F.(G,-H,(I.J))))").Rewrite();
			Assert.Equal("(AND A (NOT B) (OR C D) (OR (NOT E) (AND F (OR G (NOT H) (AND I J)))))", q6.ToString());

			var q7 = Query.Parse("A,B,(C,D,E),F.G.(H.(I.J))").Rewrite();
			Assert.Equal("(OR A B C D E (AND F G H I J))", q7.ToString());
		}

		[Fact]
		public void CanQueryDocs()
		{
			var index = new TestInvertedIndex {AllowAll = true};
			index.Add(0); // no terms!
			index.Add(1, "foo");
			index.Add(2, "foo", "bar");
			index.Add(3, "foo", "bar", "baz");
			index.Add(4, "foo", "bar", "baz", "quux");
			index.Add(5, "bar", "baz", "quux");
			index.Add(6, "baz", "quux");
			index.Add(7, "quux");
			index.Build();

			var q1 = Query.Parse("foo");
			var r1 = q1.Search(index).GetAll().ToList();
			Assert.True(r1.SequenceEqual(new[] {1, 2, 3, 4}));

			var q2 = Query.Parse("foo.bar.baz");
			var r2 = q2.Search(index).GetAll().ToList();
			Assert.True(r2.SequenceEqual(new[] {3, 4}));

			var q3 = Query.Parse("foo.(bar.baz)");
			var r3 = q3.Search(index).GetAll().ToList();
			Assert.True(r3.SequenceEqual(new[] {3, 4}));

			var q4 = Query.Parse("foo.bar.-quux");
			var r4 = q4.Search(index).GetAll().ToList();
			Assert.True(r4.SequenceEqual(new[] {2, 3}));

			var q5 = Query.Parse("foo,bar,baz");
			var r5 = q5.Search(index).GetAll().ToList();
			Assert.True(r5.SequenceEqual(new[] {1, 2, 3, 4, 5, 6}));

			var q6 = Query.Parse("-foo.-bar.-baz");
			var r6 = q6.Search(index).GetAll().ToList();
			Assert.True(r6.SequenceEqual(new[] {7}));

			var q7 = Query.Parse("foo,-bar");
			var r7 = q7.Search(index).GetAll().ToList();
			Assert.True(r7.SequenceEqual(new[] {1, 2, 3, 4, 6, 7}));

			index.AllowAll = false;
			Assert.Throws<NotSupportedException>(() => q6.Search(index));
			Assert.Throws<NotSupportedException>(() => q7.Search(index));

			// TODO need much more
		}

		[Fact]
		public void CanConjunctionIterator()
		{
			var l1 = new[] {1, 2, 3, 4, 5, 6, 7};
			var l2 = new[] {1, 3, 5, 7, 9};
			var l3 = new[] {3, 4, 5};

			// ReSharper disable ObjectCreationAsStatement
			Assert.Throws<ArgumentException>(() => new ConjunctionIterator());
			Assert.Throws<ArgumentNullException>(() => new ConjunctionIterator(null));
			// ReSharper restore ObjectCreationAsStatement

			var r1 = new ConjunctionIterator(new ListIterator(l1)).GetAll();
			Assert.True(l1.SequenceEqual(r1));

			var p1 = new ListIterator(l1, "P1");
			var p2 = new ListIterator(l2, "P2");
			var p3 = new ListIterator(l3, "P3");
			var r2 = new ConjunctionIterator(p3, p1, p2).GetAll().ToList();
			Assert.True(new[] {3, 5}.SequenceEqual(r2));

			p1 = new ListIterator(l1);
			p2 = new ListIterator(l2);
			var empty = new EmptyIterator();
			var r3 = new ConjunctionIterator(p1, p2, empty).GetAll().ToList();
			Assert.Empty(r3);
		}

		[Fact]
		public void CanDisjunctionIterator()
		{
			var l1 = new[] {1, 2, 3, 4, 5, 6, 7};
			var l2 = new[] {1, 3, 5, 7, 9};
			var l3 = new[] {3, 4, 5};

			var r1 = new DisjunctionIterator().GetAll().ToList();
			Assert.True(Enumerable.Empty<int>().SequenceEqual(r1));

			var p1 = new ListIterator(l1);
			var r2 = new DisjunctionIterator(p1).GetAll().ToList();
			Assert.True(l1.SequenceEqual(r2));

			var p2 = new ListIterator(l2, "P2");
			var p3 = new ListIterator(l3, "P3");
			var r3 = new DisjunctionIterator(p2, p3).GetAll().ToList();
			Assert.True(new[] {1, 3, 4, 5, 7, 9}.SequenceEqual(r3));

			p1 = new ListIterator(l1, "P1");
			p2 = new ListIterator(l2, "P2");
			p3 = new ListIterator(l3, "P3");
			var r4 = new DisjunctionIterator(p1, p2, p3).GetAll().ToList();
			Assert.True(new[] {1, 2, 3, 4, 5, 6, 7, 9}.SequenceEqual(r4));

			p1 = new ListIterator(l1);
			var empty = new EmptyIterator();
			var r5 = new DisjunctionIterator(p1, empty).GetAll().ToList();
			Assert.True(l1.SequenceEqual(r5));
		}

		[Fact]
		public void CanButNotIterator()
		{
			var l1 = new[] {1, 2, 3, 4, 5, 6, 7};
			var l2 = new[] {1, 3, 5, 7, 9};

			var p1 = new ListIterator(l1);
			var p2 = new ListIterator(l2);
			var r1 = new ButNotIterator(p1, p2).GetAll().ToList();
			Assert.True(new[] {2, 4, 6}.SequenceEqual(r1));

			p1 = new ListIterator(l1, "P1");
			p2 = new ListIterator(l2, "P2");
			var r2 = new ButNotIterator(p2, p1).GetAll().ToList();
			Assert.True(new[] {9}.SequenceEqual(r2));

			p1 = new ListIterator(l1);
			var r3 = new ButNotIterator(p1, null).GetAll().ToList();
			Assert.True(l1.SequenceEqual(r3));

			p2 = new ListIterator(l2);
			var r4 = new ButNotIterator(null, p2).GetAll().ToList();
			Assert.Empty(r4);

			var r5 = new ButNotIterator(null, null).GetAll().ToList();
			Assert.Empty(r5);

			p1 = new ListIterator(l1);
			var empty = new EmptyIterator();
			var r6 = new ButNotIterator(p1, empty).GetAll().ToList();
			Assert.True(l1.SequenceEqual(r6));

			empty = new EmptyIterator();
			p2 = new ListIterator(l2);
			var r7 = new ButNotIterator(empty, p2).GetAll().ToList();
			Assert.Empty(r7);
		}

		[Fact]
		public void CanEmptyIterator()
		{
			Assert.Empty(new EmptyIterator().GetAll());
		}

		[Fact]
		public void CanUnionEnumerable()
		{
			IEnumerable<int> odd = new[] {1, 3, 5, 7, 9};
			IEnumerable<int> even = new[] {2, 4, 6, 8};
			IEnumerable<int> more = new[] {1, 4, 5, 6, 8, 9};

			AssertSequence(Utils.Union(odd, even), 1, 2, 3, 4, 5, 6, 7, 8, 9);
			AssertSequence(Utils.Union(even, more), 1, 2, 4, 5, 6, 8, 9);
			AssertSequence(Utils.Union(more, more), more.ToArray());
			AssertSequence(Utils.Union(more, odd), 1, 3, 4, 5, 6, 7, 8, 9);
		}

		[Fact]
		public void CanIntersectEnumerable()
		{
			var odd = new[] {1, 3, 5, 7, 9};
			var even = new[] {2, 4, 6, 8};
			var more = new[] {1, 4, 5, 6, 8, 9};

			AssertSequence(Utils.Intersect(odd, more), 1, 5, 9);
			AssertSequence(Utils.Intersect(odd, even));
			AssertSequence(Utils.Intersect(even, more), 4, 6, 8);
			AssertSequence(Utils.Intersect(more, more), more);
		}

		[Fact]
		public void CanGoodPerformance()
		{
			// Assumption: AND t_i is O(max(docfreq(t_i))) i.e. k
			// Assumption: OR t_i is O(sum(docfreq(t_i))) i.e. m*k
			// Requirement: 10'000 docops per ms
			#if DEBUG
			const int docops = 7500;
			#else
			const int docops = 10000;
			#endif

			const int k = 1000000, n = 10000000;
			// yields random doc lists of at most k docs in range 0..n-1
			var index = GetRandomIndex(k, n, true, "foo", "bar", "baz");

			var q1 = Query.Parse("foo,bar,baz");
			var w1 = Stopwatch.StartNew();
			var r1 = q1.Search(index).GetAll().ToList();
			_output.WriteLine(
				$@"{w1.ElapsedMilliseconds} ms for {q1} with k={k:N0} and n={n:N0}; got |r|={r1.Count:N0}");
			Assert.True(w1.ElapsedMilliseconds < 3 * k / docops, "too slow");

			var q2 = Query.Parse("foo.bar.baz");
			var w2 = Stopwatch.StartNew();
			var r2 = q2.Search(index).GetAll().ToList();
			_output.WriteLine(
				$@"{w2.ElapsedMilliseconds} ms for {q2} with k={k:N0} and n={n:N0}; got |r|={r2.Count:N0}");
			Assert.True(w2.ElapsedMilliseconds < 1.2 * k / docops, "too slow");
		}

		[Fact/*(Skip = "Performance comparison")*/]
		public void SpeedComparison()
		{
			// Debug build:   A,B,C is 150% slower; A.B.C is 40% slower; summary: double time (140 65 vs 350 90)
			// Release build: A,B,C is  30% faster; A.B.C is 25% slower; summary: about same  (105 60 vs 148 48)
			const int k = 1000000, n = 10000000;
			var index = GetRandomIndex(k, n, true, "foo", "bar", "baz");

			var foo = index.Get("foo").GetAll().ToList();
			var bar = index.Get("bar").GetAll().ToList();
			var baz = index.Get("baz").GetAll().ToList();

			var w1 = Stopwatch.StartNew();
			var r1 = Utils.Union(Utils.Union(foo, bar), baz).ToList();
			_output.WriteLine($@"{w1.ElapsedMilliseconds} ms for A,B,C with k={k:N0} and n={n:N0}; got |r|={r1.Count:N0}");

			var w2 = Stopwatch.StartNew();
			var r2 = Utils.Intersect(Utils.Intersect(foo, bar), baz).ToList();
			_output.WriteLine($@"{w2.ElapsedMilliseconds} ms for A.B.C with k={k:N0} and n={n:N0}; got |r|={r2.Count:N0}");
		}

		private static IInvertedIndex GetRandomIndex(int k, int n, bool allowAll, params string[] termsToPrime)
		{
			var index = new RandomInvertedIndex(k, n, allowAll);

			// same term must always yield same docs
			Assert.True(index.Get("term").GetAll().SequenceEqual(index.Get("term").GetAll()));
			Assert.Equal(index.N, index.All().GetAll().Count());

			if (termsToPrime != null && termsToPrime.Length > 0)
			{
				index.EnableCache = true;

				foreach (var term in termsToPrime) index.Get(term);
			}

			return index;
		}

		private static void AssertSequence<T>(IEnumerable<T> actual, params T[] expected)
		{
			var list = actual.ToList();

			if (list.Count != expected.Length)
			{
				Assert.True(false, $"Different length: expected {expected.Length}, actual {list.Count}");
			}

			for (int i = 0; i < expected.Length; i++)
			{
				if (!Equals(list[i], expected[i]))
				{
					Assert.True(false, $"Items differ at index {i}: expected {expected[i]}, actual {list[i]}");
				}
			}
		}
	}
}
