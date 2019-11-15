using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sylphe.Utils
{
	/// <summary>
	/// Utilities for parsing text strings, mostly on the lexical level.
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
			int index = ScanWhite(text, Math.Max(0, startIndex));
			int limit = Math.Min(startIndex + count, text.Length);

			while (index < limit)
			{
				int a, b;
				int n = ScanNumber(text, index, out a);
				if (n < 1)
					throw new FormatException("Number expected");
				index += n;
				index += ScanWhite(text, index);
				if (index < limit && text[index] == '-')
				{
					index += 1;
					index += ScanWhite(text, index);
					n = ScanNumber(text, index, out b);
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
				index += ScanWhite(text, index);
				if (index < limit && text[index] == ',')
				{
					index += 1;
					index += ScanWhite(text, index);
				}
			}
		}

		/// <summary>
		/// Scan a run of white space from <paramref name="text"/> starting
		/// at <paramref name="index"/> and return the number chars scanned.
		/// If no white space is found, zero will be returned.
		/// </summary>
		/// <remarks>
		/// This method is useful to skip over optional white space:
		/// <code>
		/// int index = ...;
		/// string text = "...";
		/// index += ScanWhite(text, index);</code>
		/// </remarks>
		public static int ScanWhite(string text, int index)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			int anchor = index;

			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1;
			}

			return index - anchor;
		}

		/// <summary>
		/// Scan a name token from <paramref name="text"/> starting
		/// at <paramref name="index"/> and return the number of chars
		/// scanned. If no name token is found, zero will be returned.
		/// A name token is any sequence of letters, underscores, and
		/// digits, not starting with a digit.
		/// </summary>
		public static int ScanName(string text, int index)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			int anchor = index;

			if (index >= text.Length || !IsInitialNameChar(text[index]))
			{
				return 0;
			}

			index += 1;
			while (index < text.Length && IsSequentNameChar(text[index]))
			{
				index += 1;
			}

			return index - anchor;
		}

		private static bool IsInitialNameChar(char c)
		{
			return char.IsLetter(c) || c == '_';
		}

		private static bool IsSequentNameChar(char c)
		{
			const string extra = "$#";
			return char.IsLetterOrDigit(c) || c == '_' || extra.IndexOf(c) >= 0;
		}

		/// <summary>
		/// Scan a number token from <paramref name="text"/> starting
		/// at <paramref name="index"/> and return the number of chars
		/// scanned. If no number token is found, zero will be returned.
		/// The text scanned is not converted to a numeric type.
		/// </summary>
		public static int ScanNumber(string text, int index,
			bool allowDecimal = false, bool allowExponent = false)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			char cc;
			int anchor = index;

			while (index < text.Length && char.IsDigit(text, index))
			{
				index += 1;
			}

			if (allowDecimal && index < text.Length && text[index] == '.')
			{
				index += 1;
				while (index < text.Length && char.IsDigit(text, index))
				{
					index += 1;
				}
			}

			if (allowExponent && index < text.Length && ((cc = text[index]) == 'e' || cc == 'E'))
			{
				index += 1;

				if (index < text.Length && ((cc = text[index]) == '-' || cc == '+'))
				{
					index += 1;
				}

				while (index < text.Length && char.IsDigit(text, index))
				{
					index += 1;
				}
			}

			return index - anchor;
		}

		/// <summary>
		/// Scan an integer from <paramref name="text"/> starting
		/// at <paramref name="index"/> and return the number of
		/// chars scanned. Convert the text scanned to Int32.
		/// </summary>
		public static int ScanNumber(string text, int index, out int value)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			int anchor = index;

			value = 0;
			while (index < text.Length && char.IsDigit(text, index))
			{
				char cc = text[index];

				if ((uint) value > (0x7FFFFFFF / 10))
					throw new OverflowException();

				value *= 10;
				value += (cc - '0');

				if (value < 0)
					throw new OverflowException();

				index += 1;
			}

			return index - anchor; // #chars scanned
		}

		/// <summary>
		/// Scan a string token from <paramref name="text"/> starting
		/// at <paramref name="index"/>, append the value of the string
		/// (with all escapes expanded) <paramref name="buffer"/>, and
		/// return the number of characters scanned.
		/// If no string is found, zero will be returned.
		/// Single-quoted strings use SQL escape conventions.
		/// Double-quoted strings use C# escape conventions.
		/// If the string is malformed, an exception will be thrown.
		/// </summary>
		public static int ScanString(string text, int index, StringBuilder buffer)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (index >= text.Length) return 0;

			if (buffer == null)
				buffer = new StringBuilder();
			// else: don't clear buffer, append to it

			switch (text[index])
			{
				case '\'':
					return ScanSqlString(text, index, buffer);
				case '"':
					return ScanCString(text, index, buffer);
			}

			return 0; // no string found
		}

		private static int ScanSqlString(string text, int index, StringBuilder buffer)
		{
			char quote = text[index];
			int anchor = index++; // skip opening apostrophe

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc == quote)
				{
					if (index < text.Length && text[index] == quote)
					{
						buffer.Append(quote); // un-escape
						index += 1; // skip 2nd apostrophe
					}
					else
					{
						return index - anchor;
					}
				}
				else
				{
					buffer.Append(cc);
				}
			}

			throw SyntaxError(anchor, "Unterminated string");
		}

		private static int ScanCString(string text, int index, StringBuilder buffer)
		{
			char quote = text[index];
			int anchor = index++; // skip opening quote

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc < ' ')
				{
					throw SyntaxError(index - 1, "Control character in string");
				}

				if (cc == quote)
				{
					return index - anchor;
				}

				if (cc == '\\')
				{
					if (index >= text.Length)
					{
						break;
					}

					switch (cc = text[index++])
					{
						case '"':
						case '\'':
						case '\\':
						case '/':
							buffer.Append(cc);
							break;
						case 'a':
							buffer.Append('\a');
							break;
						case 'b':
							buffer.Append('\b');
							break;
						case 'f':
							buffer.Append('\f');
							break;
						case 'n':
							buffer.Append('\n');
							break;
						case 'r':
							buffer.Append('\r');
							break;
						case 't':
							buffer.Append('\t');
							break;
						case 'v':
							buffer.Append('\v');
							break;
						case 'u':
							index += ScanHex4(text, index, buffer);
							break;
						default:
							throw SyntaxError(index, "Unknown escape '\\{0}' in string", cc);
					}
				}
				else
				{
					buffer.Append(cc);
				}
			}

			throw SyntaxError(anchor, "Unterminated string");
		}

		private static int ScanHex4(string text, int index, StringBuilder buffer)
		{
			if (index + 4 >= text.Length)
			{
				throw SyntaxError(index, "Incomplete \\u escape in string");
			}

			int u = 0;
			for (int i = 0; i < 4; i++)
			{
				u *= 16;
				switch (text[index + i])
				{
					case '0': u += 0; break;
					case '1': u += 1; break;
					case '2': u += 2; break;
					case '3': u += 3; break;
					case '4': u += 4; break;
					case '5': u += 5; break;
					case '6': u += 6; break;
					case '7': u += 7; break;
					case '8': u += 8; break;
					case '9': u += 9; break;
					case 'a': case 'A': u += 10; break;
					case 'b': case 'B': u += 11; break;
					case 'c': case 'C': u += 12; break;
					case 'd': case 'D': u += 13; break;
					case 'e': case 'E': u += 14; break;
					case 'f': case 'F': u += 15; break;
					default:
						throw SyntaxError(index+i, "Incomplete \\u escape in string");
				}
			}

			buffer.Append(char.ConvertFromUtf32(u));
			return 4;
		}

		public static void FormatValue(object value, StringBuilder result)
		{
			if (value == null)
			{
				result.Append("null");
				return;
			}

			if (value is bool)
			{
				result.Append(((bool)value) ? "true" : "false");
				return;
			}

			var s = value as string;
			if (s != null)
			{
				FormatString(s, result);
				return;
			}

			result.AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
		}

		public static void FormatString(string value, StringBuilder result)
		{
			int len = value.Length;

			result.Append('"');

			for (int j = 0; j < len; j++)
			{
				char c = value[j];

				const string escapes = "\"\"\\\\\bb\ff\nn\rr\tt";
				int k = escapes.IndexOf(c);

				if (k >= 0 && k % 2 == 0)
				{
					result.Append('\\');
					result.Append(escapes[k + 1]);
				}
				else if (char.IsControl(c))
				{
					result.AppendFormat("\\u{0:x4}", (int)c);
				}
				else
				{
					result.Append(c);
				}
			}

			result.Append('"');
		}

		public static Exception SyntaxError(int position, string format, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendFormat(format, args);
			sb.AppendFormat(" (at position {0})", position);
			return new FormatException(sb.ToString());
			// TODO Custom SyntaxException? FormatException is for string.Format and related stuff
		}
	}
}
