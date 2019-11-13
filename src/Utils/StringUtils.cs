using System;
using System.Collections.Generic;
using System.Text;

namespace Sylphe.Utils
{
	public static class StringUtils
	{
		/// <summary>
		/// Remove leading and trailing white space from <paramref name="s"/>.
		/// If <paramref name="s"/> is <c>null</c>, return <c>null</c>.
		/// <seealso cref="TrimCanonical"/>
		/// </summary>
		/// <param name="s">The string to trim; can be <c>null</c></param>
		public static string Trim(this string s)
		{
			return s?.Trim();
		}

		/// <summary>
		/// Remove leading and trailing white space from <paramref name="s"/>.
		/// Return the resulting strimg, or <c>null</c> if it is null or empty.
		/// </summary>
		/// <remarks>
		/// Useful for bringing user input text into a canonical form.
		/// </remarks>
		/// <param name="s">The string to trim; can be <c>null</c></param>
		public static string TrimCanonical(string s)
		{
			if (s == null) return null;
			s = s.Trim();
			return s.Length > 0 ? s : null;
		}

		/// <summary>
		/// Remove leading and trailing white space (or the given
		/// <paramref name="trimChars"/>) from <paramref name="sb"/>.
		/// If <paramref name="sb"/> is <c>null</c>, return <c>null</c>.
		/// </summary>
		/// <param name="sb">The <see cref="StringBuilder"/> to trim; can be <c>null</c></param>
		/// <param name="trimChars">The caracters to trim; defaults to white space if <c>null</c></param>
		public static StringBuilder Trim(this StringBuilder sb, string trimChars = null)
		{
			if (sb == null)
			{
				return null;
			}

			const string whiteSpace = " \t\n\v\f\r\u0085\u00A0"; // SP HT LF VT FF CR NextLine NoBreakSpace

			trimChars = trimChars ?? whiteSpace;

			int length = sb.Length;
			int start, end;

			for (start = 0; start < length; start++)
			{
				if (trimChars.IndexOf(sb[start]) < 0)
					break;
			}

			for (end = length - 1; end >= start; end--)
			{
				if (trimChars.IndexOf(sb[end]) < 0)
					break;
			}

			if (end < length - 1)
			{
				sb.Remove(end + 1, length - end - 1);
			}

			if (start > 0)
			{
				sb.Remove(0, start);
			}

			return sb;
		}

		/// <summary>
		/// Format and join all <paramref name="items"/> into a string.
		/// Separate items by the given <paramref name="separator"/>.
		/// </summary>
		/// <param name="separator">The separator string; can be null or empty</param>
		/// <param name="items">The items to join; can be null or empty</param>
		public static string Join<T>(string separator, IEnumerable<T> items)
		{
			if (items == null)
			{
				return string.Empty;
			}

			if (separator == null)
			{
				separator = string.Empty;
			}

			var sb = new StringBuilder();

			foreach (var item in items)
			{
				if (sb.Length > 0)
				{
					sb.Append(separator);
				}

				sb.Append(item);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Return the index of the first non-white-space char at or
		/// after <paramref name="startIndex"/> in <paramref name="text"/>.
		/// Return <paramref name="text"/>.Length if all white space.
		/// </summary>
		public static int SkipWhiteSpace(string text, int startIndex)
		{
			int index = startIndex;

			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1;
			}

			return index;
		}

		/// <summary>
		/// Get the length of the common prefix of the two given strings.
		/// If <paramref name="maxLength"/> is non-negative, compare at
		/// most that many characters.
		/// </summary>
		public static int CommonPrefixLength(string a, string b, int maxLength = -1)
		{
			if (a == null || b == null) return 0;

			int limit = Math.Min(a.Length, b.Length);
			if (maxLength >= 0) limit = Math.Min(maxLength, limit);

			int index;
			for (index = 0; index < limit; index++)
			{
				if (a[index] != b[index]) break;
			}

			return index; // index of first differing char is length of common prefix
		}

		/// <summary>
		/// Get the next larger string such that the given <paramref name="key"/>
		/// is not a prefix of the result! Use ordinal comparison. If there is
		/// no such string, return <c>null</c>.
		/// </summary>
		/// <example>
		/// For example, the next larger key after "abc" is "abd" and the next
		/// larger key after "az" is "b" (here 'z' stands for char.MaxValue).
		/// <para/>
		/// Note that the next larger string after "z" is "za" and therefore "z"
		/// is a prefix of all strings larger than "z" (here again 'z' stands for
		/// char.MaxValue, and 'a' stands for char.MinValue).
		/// </example>
		public static string NextKeyOrdinal(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return null; // empty string is prefix to all strings: can't get next
			}

			char[] chars = key.ToCharArray();

			int index = chars.Length - 1;
			for (; index >= 0; index--)
			{
				if (chars[index] < char.MaxValue)
				{
					chars[index]++;
					break;
				}
			}

			return index >= 0 ? new string(chars, 0, index + 1) : null;
		}

		/// <summary>
		/// The same as <see cref="string.CompareOrdinal"/>, but compare
		/// only up to the first occurrence of the given stop char. For
		/// example, if stop = '#', "foo" and "foo#bar" compare equal.
		/// </summary>
		public static int CompareOrdinal(string x, string y, char stop)
		{
			int xlen = x.Length;
			int ylen = y.Length;
			int min = xlen < ylen ? xlen : ylen;

			for (int i = 0; i < min; i++)
			{
				char xi = x[i];
				char yi = y[i];

				if (xi == stop)
				{
					return yi == stop ? 0 : -1;
				}

				if (yi == stop)
				{
					return xi == stop ? 0 : +1;
				}

				if (xi < yi) return -1; // x < y
				if (xi > yi) return +1; // x > y

				// xi == yi, check chars at next position
			}

			// the first min chars are the same

			if (xlen < ylen)
			{
				// x is exhausted, y has more chars:
				// Let x="foo", then there are two cases for y:
				//  1. y="foo#" => consider x==y
				//  2. y="fooX" => consider x < y
				return y[min] == stop ? 0 : -1;
			}

			if (xlen > ylen)
			{
				// y is exhausted, x has more chars:
				return x[min] == stop ? 0 : +1;
			}

			return 0; // x == y
		}

		/// <summary>
		/// Split a string at a given delimiter character into individual fields.
		/// White space around each delimiter is removed, but empty fields are not.
		/// </summary>
		/// <param name="result">Where to collect results; may be null</param>
		/// <param name="text">The text to parse; may be null</param>
		/// <param name="startIndex">The start position within text</param>
		/// <param name="count">The number of characters of text to consider</param>
		/// <param name="delimiter">The character that separates fields</param>
		/// <returns>The number of fields found</returns>
		/// <example>
		/// Let text="foo: bar :baz", startIndex=3, count=9, delimiter=':',
		/// then ParseFieldList() finds these fields in order: "", "bar", "ba";
		/// notice the empty field at the beginning!
		/// </example>
		/// <remarks>
		/// This method avoids String.Split() and String.Trim() for efficiency.
		/// <para/>
		/// The parameters <paramref name="startIndex"/> and <paramref name="count"/>
		/// specify a substring of <paramref name="text"/>.
		/// </remarks>
		public static int ParseFieldList(ICollection<string> result, string text, char delimiter, int startIndex = 0, int count = -1)
		{
			if (text == null) return 0;

			if (count < 0) count = text.Length;
			int index = Math.Max(0, startIndex);
			int limit = Math.Min(startIndex + count, text.Length);

			count = 0; // nasty semantic re-definition of count...

			while (index < limit)
			{
				// Skip leading white space:
				while (index < limit && char.IsWhiteSpace(text, index)) ++index;
				int start = index;

				while (index < limit && text[index] != delimiter) ++index;
				int length = index - start;

				// Backskip trailing white space:
				while (length > 0 && char.IsWhiteSpace(text, start + length - 1)) --length;

				count += 1;
				if (result != null)
				{
					result.Add(text.Substring(start, length));
				}

				if (index < limit) index += 1; // skip over the colon
			}

			// We want "foo:" to be two fields: "foo" and "" (empty)
			if (index > startIndex && text[index-1] == delimiter)
			{
				count += 1;
				if (result != null)
				{
					result.Add(string.Empty);
				}
			}

			return count;
		}

		/// <summary>
		/// Parse a "page list" specification (as is known from typical
		/// print dialogs) and yield the corresponding list of integers.
		/// Numbers must be separated by commas, and a dash indicates
		/// a range of numbers; white space is not allowed. For example,
		/// "1,5-7,12,29-26" expands into 1, 5, 6, 7, 12, 29, 28, 27.
		/// </summary>
		/// <param name="spec">The "page" list spec to parse; negative numbers are not allowed</param>
		/// <param name="list">The list to hold the result; may be null</param>
		/// <param name="max">Add at most max numbers to list (but count beyond max)</param>
		/// <returns>The number of items in the list</returns>
		// TODO Allow white space anywhere except within numbers
		public static int ParsePageList(string spec, IList<int> list, int max)
		{
			if (string.IsNullOrEmpty(spec)) return 0;
			// list may be null => just count

			int count = 0;
			int start = -1;
			int number = 0;
			bool pending = false;

			for (int i = 0; i < spec.Length; i++)
			{
				char c = spec[i];

				switch (c)
				{
					#region Accumulate number

					case '0':
						pending = true;
						number *= 10;
						break;
					case '1':
						pending = true;
						number *= 10;
						number += 1;
						break;
					case '2':
						pending = true;
						number *= 10;
						number += 2;
						break;
					case '3':
						pending = true;
						number *= 10;
						number += 3;
						break;
					case '4':
						pending = true;
						number *= 10;
						number += 4;
						break;
					case '5':
						pending = true;
						number *= 10;
						number += 5;
						break;
					case '6':
						pending = true;
						number *= 10;
						number += 6;
						break;
					case '7':
						pending = true;
						number *= 10;
						number += 7;
						break;
					case '8':
						pending = true;
						number *= 10;
						number += 8;
						break;
					case '9':
						pending = true;
						number *= 10;
						number += 9;
						break;

					#endregion

					case ',':
						if (!pending)
						{
							throw new ArgumentException("Invalid syntax: unexpected comma");
						}

						count += EmitPageRange(list, start, number, max - count);
						start = -1;
						number = 0;
						pending = false;
						break;

					case '-':
						if (!pending)
						{
							throw new ArgumentException("Invalid syntax: unexpected dash");
						}

						start = number;
						number = 0;
						pending = false; // sic
						break;

					default:
						throw char.IsWhiteSpace(c)
							? new ArgumentException("Invalid white space")
							: new ArgumentException($"Invalid character '{c}'");
				}
			}

			if (!pending)
			{
				throw new ArgumentException("Invalid syntax");
			}

			count += EmitPageRange(list, start, number, max - count);

			return count;
		}

		private static int EmitPageRange(IList<int> list, int first, int last, int max)
		{
			if (first < 0) first = last;

			int count = 1 + Math.Abs(last - first);

			if (list != null)
			{
				if (first <= last)
				{
					for (int i = first; i <= last && max > 0; i++, max--)
					{
						list.Add(i);
					}
				}
				else
				{
					for (int i = first; i >= last && max > 0; i--, max--)
					{
						list.Add(i);
					}
				}
			}

			return count;
		}
	}
}
