using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sylphe.Utils
{
	public static class Variants
	{
		/// <summary>
		/// Generate variants of an input string based on markup:
		/// the vertical bar | separates variants, brackets [...]
		/// limit the scope of variants, and brackets with only one
		/// variant (i.e., no vertical bar) denote an optional part.
		/// Empty variants are not emitted. Brackets can be nested,
		/// but should be balanced. Invalid markup may result in
		/// unexpected output, but never raises an error.
		/// </summary>
		/// <example>
		/// "foo|bar" expands into the two strings "foo" and "bar"
		/// "ba[r|z]" expands into the two strings "bar" and "baz"
		/// "qu[u]x" expands into "quux" and "qux"
		/// "foo|ba[r|z[aar]]|quux" expands into "foo", "bar", "bazaar", "baz", "quux"
		/// "[foo|]" expands to "foo" (the empty string is not emitted)
		/// </example>
		public static IEnumerable<string> Expand(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return Enumerable.Empty<string>();
			}

			int len = text.Length;
			var down = new int[len];
			var side = new int[len];

			int pipe = Parse(text, down, side);

			var acc = new StringBuilder();
			var stk = new Stack<Entry>();

			if (pipe > 0) // 1st top-level pipe
			{
				stk.Push(new Entry(pipe+1, 0));
			}

			stk.Push(new Entry(0, 0));

			return Traverse(text, down, side, stk, acc);
		}

		/// <summary>
		/// Fill the <paramref name="down"/> and <paramref name="side"/>
		/// arrays with indices into <paramref name="text"/>.
		/// </summary>
		private static int Parse(string text, int[] down, IList<int> side)
		{
			//   posn: 0 1 2 3 4 5 6 7 8 9 A B
			//   text: a [ b [ c ] | d | e ] $
			//   down: : B : 6 : : B : B : :     one after closing bracket
			//   down: 1 : 3 : 5 6 : 8 : A B     down[i] > i for all i
			//   side: - 6 - - - - 8 - - - -     index of next pipe

			int len = text.Length;

			var stk = new Stack<int>();
			int firstTopLevelPipe = -1;

			for (int i = 0; i < len; i++)
			{
				switch(text[i])
				{
					case '[':
						stk.Push(i); // remember bracket position
						break;
					case '|':
						if (stk.Count > 0)
						{
							int j = stk.Peek();
							side[j] = i;
						}
						else
						{
							firstTopLevelPipe = i; // next[-1] = i
						}
						stk.Push(i); // remember pipe position
						break;
					case ']':
						while (stk.Count > 0)
						{
							int j = stk.Pop();
							down[j] = i+1;
							if (text[j] == '[') break;
						}
						down[i] = i+1;
						break;
					default:
						down[i] = i+1;
						break;
				}
			}

			// Implicitly close unmatched opening brackets:
			while (stk.Count > 0)
			{
				int j = stk.Pop();
				down[j] = len;
			}

			return firstTopLevelPipe;
		}

		/// <summary>
		/// Traverse the DAG defined by the <paramref name="down"/>
		/// and <paramref name="side"/> indices, yield variant strings.
		/// </summary>
		private static IEnumerable<string> Traverse(
			string text, int[] down, int[] side, Stack<Entry> stk, StringBuilder acc)
		{
			int len = text.Length;

			while (stk.Count > 0)
			{
				var entry = stk.Pop();
				int index = entry.Index;
				int reset = entry.Reset;

				acc.Length = reset; // truncate

				while (index < len)
				{
					switch (text[index])
					{
						case '[':
							reset = acc.Length;
							if (side[index] > 0) // two or more variants
							{
								stk.Push(new Entry(side[index]+1, reset));
								index += 1;
							}
							else // optional part: treat "[x]" as "[|x]"
							{
								stk.Push(new Entry(index+1, reset));
								index = down[index];
							}
							break;
						case '|':
							if (side[index] > 0)
							{
								stk.Push(new Entry(side[index]+1, reset));
								stk.Exchange(); // ensure variant ordering
							}
							index = down[index];
							break;
						case ']':
							index += 1;
							break;
						default:
							acc.Append(text[index]);
							index += 1;
							break;
					}
				}

				if (acc.Length > 0)
				{
					yield return acc.ToString();
				}
			}
		}

		/// <summary>
		/// Exchange the top two stack entries.
		/// </summary>
		private static void Exchange<T>(this Stack<T> stack)
		{
			var a = stack.Pop();
			var b = stack.Pop();
			stack.Push(a);
			stack.Push(b);
		}

		private struct Entry
		{
			public readonly int Index;
			public readonly int Reset;

			public Entry(int index, int reset)
			{
				Index = index;
				Reset = reset;
			}
		}
	}
}
