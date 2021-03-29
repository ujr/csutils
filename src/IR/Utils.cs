using System.Collections.Generic;

namespace Sylphe.IR
{
	public static class Utils
	{
		public static IEnumerable<int> Intersect(IEnumerable<int> p1, IEnumerable<int> p2)
		{
			using (var e1 = p1.GetEnumerator())
			using (var e2 = p2.GetEnumerator())
			{
				bool has1 = e1.MoveNext();
				bool has2 = e2.MoveNext();

				while (has1 && has2)
				{
					int doc1 = e1.Current;
					int doc2 = e2.Current;

					if (doc1 < doc2)
					{
						has1 = e1.MoveNext();
						continue;
					}

					if (doc2 < doc1)
					{
						has2 = e2.MoveNext();
						continue;
					}

					// doc1 == doc2
					yield return doc1;
					has1 = e1.MoveNext();
					has2 = e2.MoveNext();
				}
			}
		}

		public static IEnumerable<int> Union(IEnumerable<int> p1, IEnumerable<int> p2)
		{
			using (var e1 = p1.GetEnumerator())
			using (var e2 = p2.GetEnumerator())
			{
				bool has1 = e1.MoveNext();
				bool has2 = e2.MoveNext();

				while (has1 && has2)
				{
					int doc1 = e1.Current;
					int doc2 = e2.Current;

					if (doc1 < doc2)
					{
						has1 = e1.MoveNext();
						yield return doc1;
						continue;
					}

					if (doc2 < doc1)
					{
						has2 = e2.MoveNext();
						yield return doc2;
						continue;
					}

					has1 = e1.MoveNext();
					has2 = e2.MoveNext();
					yield return doc1;
				}

				while (has1)
				{
					yield return e1.Current;
					has1 = e1.MoveNext();
				}

				while (has2)
				{
					yield return e2.Current;
					has2 = e2.MoveNext();
				}
			}
		}
	}
}
