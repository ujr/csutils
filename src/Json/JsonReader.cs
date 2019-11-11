using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sylphe.Json
{
	public enum JsonType
	{
		None = 0,
		Null,
		False,
		True,
		Number,
		String,
		Array,
		Object,
		Closed,
	}

	/// <summary>
	/// A low-level reader for JSON data
	/// </summary>
	public class JsonReader
	{
		private readonly string _text;
		private int _index; // into _text
		private readonly StringBuilder _buffer;
		private readonly Stack<StackEntry> _stack;
		private State _state;

		// TODO Nice-to-have: work on a TextReader (forward-only)

		public JsonReader(string text, int index = 0)
		{
			_text = text;
			_index = index;
			_buffer = new StringBuilder();
			_stack = new Stack<StackEntry>();
			_state = State.WantValue;

			Position = 0;

			Type = JsonType.None;
			Value = null;
			Index = 0;
			Label = null;
		}

		public int Position { get; private set; }

		public JsonType Type { get; private set; }
		public object Value { get; private set; }
		public int Index { get; private set; }
		public string Label { get; private set; }

		public int Depth => _stack.Count;
		public JsonType Context => _stack.Count > 0 ? _stack.Peek().Context : JsonType.None;

		public bool Read()
		{
			again:
			object value;
			TokenType token = Advance(out value);

			if (_state == State.ValueComplete)
			{
				switch (token)
				{
					case TokenType.End:
						Expect(Depth == 0, Position, "Unexpected end of input at depth {0}", Depth);
						Type = JsonType.None;
						Value = null;
						return false;

					case TokenType.Comma:
						Expect(Depth > 0, Position, "Unexpected comma outside array or object");
						Index += 1;
						Label = null;
						_state = IsObject ? State.WantLabel : State.WantValue;
						goto again;

					case TokenType.BracketClose:
						Expect(IsArray, Position, "Unmatched closing bracket");
						var aentry = _stack.Pop();
						Index = aentry.Index;
						Label = aentry.Label;
						Type = JsonType.Closed;
						Value = null;
						_state = State.ValueComplete;
						return true;

					case TokenType.BraceClose:
						Expect(IsObject, Position, "Unmatched closing brace");
						var oentry = _stack.Pop();
						Index = oentry.Index;
						Label = oentry.Label;
						Type = JsonType.Closed;
						Value = null;
						_state = State.ValueComplete;
						return true;
				}

				throw SyntaxError(Position, "Expected a comma, or a closing bracket or brace, but got: {0}", token);
			}

			if (_state == State.WantLabel)
			{
				switch (token)
				{
					case TokenType.End:
						throw SyntaxError(Position, "Unexpected end of JSON text");

					case TokenType.String:
					case TokenType.Name:
						var label = value;
						token = Advance(out value);
						Expect(token == TokenType.Colon, Position, "Expected a colon but got {0}", token);
						Label = Convert.ToString(label);
						_state = State.WantValue;
						goto again;

					case TokenType.BraceClose:
						// Closing brace in state WantLabel => empty object
						Expect(IsObject, Position, "Unmatched closing brace");
						Expect(Index == 0, Position, "Expected a member but got a closing brace");
						var oentry = _stack.Pop();
						Type = JsonType.Closed;
						Value = null;
						Index = oentry.Index;
						Label = oentry.Label;
						_state = State.ValueComplete;
						return true;
				}

				throw SyntaxError(Position, "Unexpected token: {0}", token);
			}

			if (_state == State.WantValue)
			{
				if (IsArray && Index == 0)
				{
					// An array's label sticks for first array item:
					// explicitly clear:
					Label = null;
				}

				switch (token)
				{
					case TokenType.End:
						throw SyntaxError(Position, "Unexpected end of JSON text");

					case TokenType.Null:
						Type = JsonType.Null;
						Value = null;
						_state = State.ValueComplete;
						return true;
					case TokenType.False:
						Type = JsonType.False;
						Value = false;
						_state = State.ValueComplete;
						return true;
					case TokenType.True:
						Type = JsonType.True;
						Value = true;
						_state = State.ValueComplete;
						return true;

					case TokenType.Number:
						Type = JsonType.Number;
						Value = value;
						_state = State.ValueComplete;
						return true;

					case TokenType.String:
						Type = JsonType.String;
						Value = value;
						_state = State.ValueComplete;
						return true;

					case TokenType.BracketOpen:
						_stack.Push(new StackEntry(JsonType.Array, Index, Label));
						Type = JsonType.Array;
						Value = null;
						Index = 0;
						_state = State.WantValue;
						return true;

					case TokenType.BracketClose:
						// Closing bracket in state WantValue => empty array
						Expect(IsArray, Position, "Unmatched closing bracket");
						Expect(Index == 0, Position, "Expected a value but got a closing bracket");
						var aentry = _stack.Pop();
						Type = JsonType.Closed;
						Value = null;
						Index = aentry.Index;
						Label = aentry.Label;
						_state = State.ValueComplete;
						return true;

					case TokenType.BraceOpen:
						_stack.Push(new StackEntry(JsonType.Object, Index, Label));
						Type = JsonType.Object;
						Value = null;
						Index = 0;
						_state = State.WantLabel;
						return true;
				}

				throw SyntaxError(Position, "Unexpected token: {0}", token);
			}

			throw new Exception($"Unknown state: {_state}");
		}

		public override string ToString()
		{
			return $"Type={Type} Value={Value} Depth={Depth}";
		}

		private TokenType Advance(out object value)
		{
			again:
			Position = _index;
			var token = ScanToken(_text, ref _index, out value, _buffer);
			if (token == TokenType.White) goto again;
			return token;
		}

		private static void Expect(bool condition, int position, string format, params object[] args)
		{
			if (!condition)
			{
				throw SyntaxError(position, format, args);
			}
		}

		private bool IsArray => _stack.Count > 0 && _stack.Peek().Context == JsonType.Array;

		private bool IsObject => _stack.Count > 0 && _stack.Peek().Context == JsonType.Object;

		#region Lexical scanner

		private enum TokenType
		{
			End = 0,
			White,
			Null,
			False,
			True,
			Name,
			String,
			Number,
			Comma,
			Colon,
			BracketOpen,
			BracketClose,
			BraceOpen,
			BraceClose
		}

		private static TokenType ScanToken(string text, ref int index, out object value, StringBuilder buffer)
		{
			if (index >= text.Length)
			{
				value = null;
				return TokenType.End;
			}

			char cc = text[index];

			if (char.IsWhiteSpace(cc))
			{
				int length = ScanWhite(text, index);
				index += length;
				value = null;
				return TokenType.White;
			}

			if (IsInitialNameChar(cc))
			{
				int length = ScanName(text, index);
				string s = text.Substring(index, length);
				index += length;
				switch (s)
				{
					case "null":
						value = null;
						return TokenType.Null;
					case "false":
						value = false;
						return TokenType.False;
					case "true":
						value = true;
						return TokenType.True;
				}
				value = s;
				return TokenType.Name;
			}

			if (cc == '"')
			{
				buffer.Clear();
				int length = ScanString(text, index, buffer);
				index += length;
				value = buffer.ToString();
				return TokenType.String;
			}

			if (cc == '-' || char.IsDigit(cc))
			{
				int length = ScanNumber(text, index);
				string s = text.Substring(index, length);

				double number;
				if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
				{
					throw SyntaxError(index, "Invalid number: '{0}'", s);
				}
				index += length;
				value = number;
				return TokenType.Number;
			}

			switch (cc)
			{
				case ',':
					index += 1;
					value = null;
					return TokenType.Comma;
				case ':':
					index += 1;
					value = null;
					return TokenType.Colon;
				case '[':
					index += 1;
					value = null;
					return TokenType.BracketOpen;
				case ']':
					index += 1;
					value = null;
					return TokenType.BracketClose;
				case '{':
					index += 1;
					value = null;
					return TokenType.BraceOpen;
				case '}':
					index += 1;
					value = null;
					return TokenType.BraceClose;
			}

			throw SyntaxError(index, "Invalid input character: U+{0:X4}", (int)cc);
		}

		/// <summary>
		/// Scan a run of white space from <paramref name="text"/>
		/// at <paramref name="index"/> and return the number of
		/// characters scanned.
		/// </summary>
		/// <remarks>
		/// If no white space is found at the given position,
		/// zero will be returned.
		/// <para/>
		/// This method is useful to skip over optional white space:
		/// <code>
		/// int index = ...;
		/// string text = "...";
		/// index += ScanWhite(text, index);</code>
		/// </remarks>
		private static int ScanWhite(string text, int index)
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
		/// Scan a name token from <paramref name="text"/> at <paramref name="index"/>
		/// and return the number of characters scanned.
		/// </summary>
		/// <remarks>
		/// If no name token is found at the given position, zero will be returned
		/// (not an exception thrown).</remarks>
		private static int ScanName(string text, int index)
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
			return char.IsLetterOrDigit(c) || c == '_' || c == '$' || c == '#';
		}

		/// <summary>
		/// Scan a string token from <paramref name="text"/> at <paramref name="index"/>
		/// and return the number of characters scanned. The value of the string token
		/// (with all escapes expanded) is appended to <paramref name="buffer"/>
		/// (<paramref name="buffer"/> is NOT cleared).
		/// </summary>
		/// <remarks>
		/// If no valid string token found at the given position,
		/// an exception will be thrown.
		/// </remarks>
		private static int ScanString(string text, int index, StringBuilder buffer)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0 || index >= text.Length)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			// don't clear buffer, append to it

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
				throw SyntaxError(index, "Incomplete \\u escape (expect 4 hex digits)");
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
						throw SyntaxError(index+i, "Invalid \\u escape (expect 4 hex digits)");
				}
			}

			buffer.Append(char.ConvertFromUtf32(u));
			return 4;
		}

		private static int ScanNumber(string text, int index)
		{
			char cc;
			int anchor = index;

			if (index < text.Length && ((cc = text[index]) == '-' || cc == '+'))
			{
				index += 1; // optional - or + sign (handled by parser/eval)
			}

			while (index < text.Length && char.IsDigit(text, index))
			{
				index += 1;
			}

			if (index < text.Length && text[index] == '.')
			{
				index += 1;

				while (index < text.Length && char.IsDigit(text, index))
				{
					index += 1;
				}
			}

			if (index < text.Length && ((cc = text[index]) == 'e' || cc == 'E'))
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

			if (index < text.Length && IsInitialNameChar(text[index]))
			{
				throw SyntaxError(anchor, "Unterminated numeric token");
			}

			return index - anchor;
		}

		#endregion

		private static Exception SyntaxError(int position, string format, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendFormat(format, args);
			sb.AppendFormat(" (at position {0})", position);
			return new JsonException(sb.ToString());
		}

		#region Nested types

		private enum State
		{
			WantValue,
			WantLabel,
			ValueComplete
		}

		private struct StackEntry
		{
			public readonly JsonType Context;
			public readonly int Index;
			public readonly string Label;

			public StackEntry(JsonType type, int index, string label)
			{
				Context = type;
				Index = index;
				Label = label;
			}

			public override string ToString()
			{
				return $"Type={Context} Index={Index} Label={Label}";
			}
		}

		#endregion
	}
}
