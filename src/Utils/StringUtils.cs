using System;
using System.Collections.Generic;
using System.Globalization;
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
		/// Remove diacritical marks from the given <paramref name="text"/>
		/// and return the result. French accents and German umlauts work well,
		/// but surprises exist; e.g. "łŁ" remains unchanged.
		/// </summary>
		/// <remarks>
		/// About removing diacritical marks, see
		/// https://docs.microsoft.com/en-us/dotnet/api/system.string.normalize and
		/// http://stackoverflow.com/questions/3769457/how-can-i-remove-accents-on-a-string
		/// </remarks>
		public static string RemoveDiacritics(this string text, StringBuilder buffer = null)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			text = text.Normalize(NormalizationForm.FormD);

			if (buffer == null)
			{
				buffer = new StringBuilder();
			}
			else
			{
				buffer.Clear();
			}

			foreach (char c in text)
			{
				var category = char.GetUnicodeCategory(c);
				if (category != UnicodeCategory.NonSpacingMark)
				{
					buffer.Append(c);
				}
			}

			return buffer.ToString();
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
		/// The same as <see cref="string.CompareOrdinal(string,string)"/>,
		/// but compare only up to the first occurrence of the given stop char.
		/// For example, if stop = '#', "foo" and "foo#bar" compare equal.
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
	}
}
