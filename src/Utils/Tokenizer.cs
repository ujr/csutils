using System;
using System.Globalization;
using System.Text;

namespace Sylphe.Utils
{
	/// <summary>
	/// Separate a text string into individual tokens (lexical scanning).
	/// Token types: Name, String, Number, Operator, End (of input).
	/// Operators always use ordinal, names the given string comparison.
	/// </summary>
	public class Tokenizer
	{
		private readonly string _text;
		private readonly StringBuilder _buffer;

		private int _index;
		private Token _currentToken;

		private const string DefaultExtraNameChars = "$_";

		public Tokenizer(string text, int index = 0)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			_text = text;
			_index = index;
			_buffer = new StringBuilder();
			_currentToken = Token.None;

			AllowDecimals = true;
			AllowExponent = true;
			ExtraNameChars = DefaultExtraNameChars;
			AllowDoubleQuotedString = true;
			AllowSingleQuotedString = true;
			NameComparison = StringComparison.Ordinal;
		}

		public bool AllowDecimals;

		public bool AllowExponent;

		public string ExtraNameChars;

		public bool AllowDoubleQuotedString;

		public bool AllowSingleQuotedString;

		public StringComparison NameComparison;

		public bool Advance()
		{
			_currentToken = NextToken(_text, ref _index);
			return _currentToken.Type != TokenType.End;
		}

		public int Index => _index;

		public object CurrentValue => _currentToken.Value;

		public void ExpectName()
		{
			if (_currentToken.Type != TokenType.Name)
			{
				throw SyntaxError(_currentToken.Index, "Expected a name, got {0}", _currentToken);
			}
		}

		public void ExpectOperator(string op)
		{
			if (_currentToken.Type != TokenType.Operator ||
			    !string.Equals(_currentToken.Value as string, op, StringComparison.Ordinal))
			{
				throw SyntaxError(_currentToken.Index, "Expected {0}, got {1}", op, _currentToken);
			}
		}

		public bool IsName()
		{
			return _currentToken.Type == TokenType.Name;
		}

		public bool IsName(string name)
		{
			if (_currentToken.Type != TokenType.Name) return false;
			var value = (string) _currentToken.Value;
			return string.Equals(value, name, NameComparison);
		}

		public bool IsString()
		{
			return _currentToken.Type == TokenType.String;
		}

		public bool IsNumber()
		{
			return _currentToken.Type == TokenType.Number;
		}

		public bool IsOperator(string op)
		{
			if (_currentToken.Type != TokenType.Operator) return false;
			var value = (string) _currentToken.Value;
			return string.Equals(value, op, StringComparison.Ordinal);
		}

		public bool IsOperator(string op1, string op2)
		{
			if (_currentToken.Type != TokenType.Operator) return false;
			var value = (string) _currentToken.Value;
			return string.Equals(value, op1, StringComparison.Ordinal) ||
			       string.Equals(value, op2, StringComparison.Ordinal);
		}

		public bool IsOperator(string op1, string op2, string op3)
		{
			if (_currentToken.Type != TokenType.Operator) return false;
			var value = (string) _currentToken.Value;
			return string.Equals(value, op1, StringComparison.Ordinal) ||
			       string.Equals(value, op2, StringComparison.Ordinal) ||
			       string.Equals(value, op3, StringComparison.Ordinal);
		}

		public bool IsEnd => _currentToken.Type == TokenType.End;

		private Token NextToken(string text, ref int index)
		{
			// Name:     trim, Hello, null
			// Number:   123, 0.345, 1.567, 7.89e2
			// String:   "You said: \"Hi!\"" or 'Rock''n''Roll'
			// Operator: = && ? : { } != <= ( ) etc.

			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1; // skip white space
			}

			if (index >= text.Length)
			{
				return Token.End(index);
			}

			char cc = text[index];

			if (IsInitialNameChar(cc, ExtraNameChars))
			{
				int length = ScanName(text, index);
				int anchor = index;
				index += length;
				return Token.Name(text.Substring(anchor, length), anchor);
			}

			if (cc == '"' && AllowDoubleQuotedString)
			{
				int anchor = index;
				index += ScanCString(text, index, _buffer.Clear());
				return Token.String(_buffer.ToString(), anchor);
			}

			if (cc == '\'' && AllowSingleQuotedString)
			{
				int anchor = index;
				index += ScanSqlString(text, index, _buffer.Clear());
				return Token.String(_buffer.ToString(), anchor);
			}

			if (char.IsDigit(cc))
			{
				// Unary minus or plus is considered an operator,
				// not part of the number token.
				int length = ScanNumber(text, index, AllowDecimals, AllowExponent);
				int anchor = index;
				index += length;
				var s = text.Substring(anchor, length);
				double value = double.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);

				return Token.Number(value, anchor);
			}

			// Multi-character operators: what's the best way to scan them?
			// Options that come to mind: (1) treat them all as special cases,
			// (2) if cc in a set of "prefix" chars then scan while cc in a
			// set of "suffix" chars, which usually accepts many more operators
			// than are desired, (3) Knuth's METAFONT has the concept where each
			// multi-character operator is composed of a characters from a few
			// disjoint character classes, (4) scan along the branches of a
			// trie of the operators, but that's a bit non-trivial.

			// Here: approach (1) for the following set of multi-character ops:
			// && ** ++ -- .. // << == >> ?? || -> => <= >= += -= *= /= %= &= |= ^= != <>
			// (this is _approximately_ the C# operators and may easily be changed)

			const string dups = "&*+-./<=>?|"; // && ** ++ -- .. // << == >> ?? ||
			if (dups.IndexOf(cc) >= 0 && IsChar(text, index+1, cc))
			{
				int anchor = index;
				index += 2;
				return Token.Operator(text.Substring(anchor, index - anchor), anchor);
			}

			const string assgns = "+-*/%&|^<>!"; // += -= *= /= %= &= |= ^= <= >= !=
			if (assgns.IndexOf(cc) >= 0 && IsChar(text, index+1, '='))
			{
				int anchor = index;
				index += 2;
				return Token.Operator(text.Substring(anchor, index - anchor), anchor);
			}

			if ((cc == '-' || cc == '=' || cc == '<') && IsChar(text, index+1, '>'))
			{
				int anchor = index;
				index += 2;
				return Token.Operator(text.Substring(anchor, index - anchor), anchor);
			}

			if (!char.IsControl(cc))
			{
				int anchor = index;
				index += 1;
				return Token.Operator(text.Substring(anchor, 1), anchor);
			}

			throw SyntaxError(index, "Invalid input character: U+{0:X4}", (int) cc);
		}

		private static bool IsChar(string text, int index, char cc)
		{
			return index < text.Length && text[index] == cc;
		}

		#region Lexical scanning

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
		public static int ScanName(string text, int index, string extra = null)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			extra = extra ?? DefaultExtraNameChars;

			int anchor = index;

			if (index >= text.Length || !IsInitialNameChar(text[index], extra))
			{
				return 0;
			}

			index += 1;
			while (index < text.Length && IsSequentNameChar(text[index], extra))
			{
				index += 1;
			}

			return index - anchor;
		}

		private static bool IsInitialNameChar(char c, string extra)
		{
			return char.IsLetter(c) || extra.IndexOf(c) >= 0;
		}

		private static bool IsSequentNameChar(char c, string extra)
		{
			return char.IsLetterOrDigit(c) || extra.IndexOf(c) >= 0;
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

		#endregion

		#region Formatting literals

		/// <summary>
		/// Append a string representation of <paramref name="value"/>
		/// to <paramref name="result"/>. Strings are formatted as
		/// C like strings, <c>null</c> as "null", boolean values
		/// as "true" or "false", and numbers using invariant culture.
		/// </summary>
		public static void FormatValue(object value, StringBuilder result)
		{
			if (value == null)
			{
				result.Append("null");
				return;
			}

			if (value is bool b)
			{
				result.Append(b ? "true" : "false");
				return;
			}

			if (value is string s)
			{
				FormatString(s, result);
				return;
			}

			result.AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
		}

		private static void FormatString(string value, StringBuilder result)
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

		#endregion

		public static Exception SyntaxError(int position, string format, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendFormat(format, args);
			sb.AppendFormat(" (at position {0})", position);
			return new FormatException(sb.ToString());
		}

		#region Nested types: Token and TokenType

		private readonly struct Token
		{
			public readonly int Index;
			public readonly TokenType Type;
			public readonly object Value;

			private Token(int index, TokenType type, object value)
			{
				Index = index;
				Type = type;
				Value = value;
			}

			public static Token Name(string value, int index)
			{
				return new Token(index, TokenType.Name, value);
			}

			public static Token String(string value, int index)
			{
				return new Token(index, TokenType.String, value);
			}

			public static Token Number(double value, int index)
			{
				return new Token(index, TokenType.Number, value);
			}

			public static Token Operator(string value, int index)
			{
				return new Token(index, TokenType.Operator, value);
			}

			public static Token End(int index)
			{
				return new Token(index, TokenType.End, null);
			}

			public static Token None => new Token(0, TokenType.None, null);
		}

		private enum TokenType
		{
			None = 0, Name, String, Number, Operator, End
		}

		#endregion
	}
}
