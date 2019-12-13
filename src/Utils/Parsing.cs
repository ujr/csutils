using System;
using System.Collections.Generic;

namespace Sylphe.Utils
{
	/// <summary>
	/// Utilities for parsing text strings.
	/// </summary>
	public static class Parsing
	{
		/// <summary>
		/// Split a string at a given delimiter character into individual fields.
		/// White space around each delimiter is removed, but empty fields are not.
		/// For example, the input ":foo : bar" splits into "", "foo", and "bar".
		/// </summary>
		/// <param name="text">The text to parse; may be null</param>
		/// <param name="delimiter">The character that separates fields</param>
		/// <param name="startIndex">The start position within text</param>
		/// <param name="count">The number of characters of text to consider</param>
		/// <remarks>
		/// This method avoids String.Split() and String.Trim() for efficiency.
		/// </remarks>
		public static IEnumerable<string> ParseFieldList(string text, char delimiter, int startIndex = 0, int count = -1)
		{
			if (text == null) yield break;

			if (count < 0) count = text.Length;
			int index = Math.Max(0, startIndex);
			int limit = Math.Min(startIndex + count, text.Length);

			while (index <= limit)
			{
				// Skip leading white space:
				while (index < limit && char.IsWhiteSpace(text, index)) ++index;
				int start = index;

				// Look for delimiter:
				while (index < limit && text[index] != delimiter) ++index;
				int length = index - start;

				// Backskip trailing white space:
				while (length > 0 && char.IsWhiteSpace(text, start + length - 1)) --length;

				yield return text.Substring(start, length);

				index += 1; // skip delim (or eos)
			}
		}

		/// <summary>
		/// Parse a "page list" specification (as is known from typical
		/// print dialogs) and yield the corresponding list of integers.
		/// Numbers may be separated by commas; a dash indicates a range
		/// of numbers; negative numbers are not allowed; white space is
		/// allowed and ignored. For example, the input "1,5-7,12,29-27"
		/// yields the integer sequence 1, 5, 6, 7, 12, 29, 28, 27.
		/// </summary>
		public static IEnumerable<int> ParsePageList(string text, int startIndex = 0, int count = -1)
		{
			if (text == null) yield break;

			if (count < 0) count = text.Length;
			int index = Tokenizer.ScanWhite(text, Math.Max(0, startIndex));
			int limit = Math.Min(startIndex + count, text.Length);

			while (index < limit)
			{
				int a, b;
				int n = Tokenizer.ScanNumber(text, index, out a);
				if (n < 1)
					throw new FormatException("Number expected");
				index += n;
				index += Tokenizer.ScanWhite(text, index);
				if (index < limit && text[index] == '-')
				{
					index += 1;
					index += Tokenizer.ScanWhite(text, index);
					n = Tokenizer.ScanNumber(text, index, out b);
					if (n < 1)
						throw new FormatException("Number expected");
					index += n;
					if (a < b)
					{
						// prevent wrap-around in i++ when b==int.MaxValue:
						// keep i < b and emit b separately
						for (int i = a; i < b; i++) yield return i;
						yield return b;
					}
					else
					{
						// here we know b >= 0, hence no danger, but
						// keep the pattern (if we changed int to uint)
						for (int i = a; i > b; i--) yield return i;
						yield return b;
					}
				}
				else
				{
					yield return a;
				}
				index += Tokenizer.ScanWhite(text, index);
				if (index < limit && text[index] == ',')
				{
					index += 1;
					index += Tokenizer.ScanWhite(text, index);
				}
			}
		}
	}
}
