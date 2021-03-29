using System;
using System.Collections.Generic;
using System.Linq;

namespace Sylphe.IR.Test
{
	/// <summary>
	/// Simplistic implementation for testing the query stuff here
	/// </summary>
	public class TestInvertedIndex : IInvertedIndex
	{
		private List<Post> _posts;
		private IList<int> _allDocsSorted;
		private IDictionary<string, List<int>> _dict;

		public TestInvertedIndex()
		{
			_posts = new List<Post>();
			_dict = null;
		}

		public void Add(int doc, params string[] terms)
		{
			if (_posts == null)
				throw new InvalidOperationException("Already committed, cannot modify");

			foreach (var term in terms) _posts.Add(new Post(doc, term));
		}

		public void Build()
		{
			if (_posts == null)
				throw new InvalidOperationException("Already committed, cannot build again");

			// sort _posts by term, then by doc
			_dict = _posts.GroupBy(post => post.Term)
				.ToDictionary(group => group.Key, group => group.Select(post => post.Doc).OrderBy(id => id).ToList());

			_allDocsSorted = _posts.Select(post => post.Doc).OrderBy(id => id).Distinct().ToList();

			_posts = null;
		}

		public bool AllowAll { get; set; }

		public DocSetIterator All()
		{
			if (_allDocsSorted == null)
				throw new InvalidOperationException("Must first build");
			if (!AllowAll)
				throw new NotSupportedException("The All query is not allowed");

			return new ListIterator(_allDocsSorted, "all");
		}

		public DocSetIterator Get(string term)
		{
			if (_dict == null)
				throw new InvalidOperationException("Must first build");

			if (term != null && _dict.TryGetValue(term, out var list)) return new ListIterator(list, term);

			return new EmptyIterator(term);
		}

		private class Post
		{
			public readonly int Doc;
			public readonly string Term;

			public Post(int doc, string term)
			{
				Doc = doc;
				Term = term;
			}
		}
	}
}
