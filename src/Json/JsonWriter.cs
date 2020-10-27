using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sylphe.Json
{
	/// <summary>
	/// Wraps around a <see cref="TextWriter"/> to generate
	/// tight but syntactically correct JSON or JSON-P.
	/// </summary>
	public sealed class JsonWriter : IDisposable
	{
		private TextWriter _writer;
		private readonly bool _closeOutput;
		private readonly Stack<WriterState> _stack;
		private readonly CultureInfo _invariant;
		private readonly string _jsonpFunctionName;
		private bool _gotPropertyName;

		/// <param name="writer">The underlying text writer (required)</param>
		/// <param name="closeOutput">whether to close the underlying
		/// text writer (optional; default: false)</param>
		/// <param name="jsonpFunctionName">request JSON-P using the given
		/// function name (if null or empty, plain JSON is written)</param>
		public JsonWriter(TextWriter writer, bool closeOutput = false, string jsonpFunctionName = null)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			_writer = writer;
			_closeOutput = closeOutput;
			_jsonpFunctionName = jsonpFunctionName;
			_invariant = CultureInfo.InvariantCulture;
			_stack = new Stack<WriterState>();
			_stack.Push(WriterState.Initial);
			_gotPropertyName = false;

			// Remember the encoding so it's available even after
			// disposing the writer - a nice convenience for our
			// customers. Notice that TextWriter.Encoding is never null.
			Encoding = writer.Encoding;

			if (!string.IsNullOrEmpty(jsonpFunctionName))
			{
				_writer.Write(jsonpFunctionName);
				_writer.Write("(");
			}
		}

		/// <summary>
		/// The Content Type: this is "application/json" for plain JSON (by RFC 4627),
		/// or "application/javascript" for JSON with Padding (by RFC 4329).
		/// </summary>
		public string ContentType
		{
			get
			{
				const string mimeTypeJson = "application/json"; // RFC 4627
				const string mimeTypeJsonP = "application/javascript"; // RFC 4329

				return string.IsNullOrEmpty(_jsonpFunctionName)
					? mimeTypeJson
					: mimeTypeJsonP;
			}
		}

		/// <summary>
		/// The encoding used by the underlying stream writer.
		/// For convenience, this is available even after disposing.
		/// </summary>
		public Encoding Encoding { get; }

		public void WriteNull()
		{
			CheckDisposed();
			WriteSeparator();
			_writer.Write("null");
		}

		public void WriteValue(bool value)
		{
			CheckDisposed();
			WriteSeparator();
			_writer.Write(value ? "true" : "false");
		}

		public void WriteValue(long value)
		{
			CheckDisposed();
			WriteSeparator();
			string json = value.ToString(_invariant);
			_writer.Write(json);
		}

		public void WriteValue(double value, int significantDigits = 0)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonNumber(value, significantDigits);
		}

		public void WriteValue(decimal value)
		{
			CheckDisposed();
			WriteSeparator();
			string json = value.ToString(_invariant);
			_writer.Write(json);
		}

		public void WriteValue(IEnumerable<char> value)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonString(value, true);
		}

		public void WriteStartArray()
		{
			CheckDisposed();
			WriteSeparator();
			_writer.Write('[');
			Push(WriterState.InArray1);
		}

		public void WriteEndArray()
		{
			CheckDisposed();
			var state = Pop();
			if (state != WriterState.InArray1 && state != WriterState.InArrayN)
			{
				_stack.Clear();
				throw new InvalidOperationException("Not writing an array");
			}
			_writer.Write(']');
		}

		public void WriteStartObject()
		{
			CheckDisposed();
			WriteSeparator();
			_writer.Write('{');
			Push(WriterState.InObject1);
		}

		public void WritePropertyName(string name)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonString(name, true);
			_writer.Write(':');
			_gotPropertyName = true;
		}

		public void WriteProperty(string name, bool value)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonString(name, true);
			_writer.Write(':');
			_writer.Write(value ? "true" : "false");
		}

		public void WriteProperty(string name, long value)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonString(name, true);
			_writer.Write(':');
			_writer.Write(value.ToString(_invariant));
		}

		public void WriteProperty(string name, double value, int significantDigits = 0)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonString(name, true);
			_writer.Write(':');
			WriteJsonNumber(value, significantDigits);
		}

		public void WriteProperty(string name, IEnumerable<char> value)
		{
			CheckDisposed();
			WriteSeparator();
			WriteJsonString(name, true);
			_writer.Write(':');
			WriteJsonString(value, true);
		}

		public void WriteEndObject()
		{
			CheckDisposed();
			var state = Pop();
			if (state != WriterState.InObject1 && state != WriterState.InObjectN)
			{
				_stack.Clear();
				throw new InvalidOperationException("Not writing an object");
			}
			_writer.Write('}');
		}

		public void Flush()
		{
			CheckDisposed();

			_writer.Flush();
		}

		public void Close()
		{
			CheckDisposed();

			if (!string.IsNullOrEmpty(_jsonpFunctionName))
			{
				_writer.Write(");");
			}

			_stack.Clear();

			_writer.Flush(); // always flush!

			if (_closeOutput)
			{
				_writer.Close();
			}

			_writer = null;
		}

		#region Non-public methods

		private void CheckDisposed()
		{
			if (_writer == null)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		private void WriteSeparator()
		{
			if (_gotPropertyName)
			{
				_gotPropertyName = false;
			}
			else
			{
				switch (State)
				{
					case WriterState.Initial:
						break;
					case WriterState.InArray1:
						Pop();
						Push(WriterState.InArrayN);
						break;
					case WriterState.InObject1:
						Pop();
						Push(WriterState.InObjectN);
						break;
					case WriterState.InArrayN:
					case WriterState.InObjectN:
						_writer.Write(',');
						break;
					default:
						throw new InvalidOperationException("Invalid writer state");
				}
			}
		}

		private void WriteJsonNumber(double value, int significantDigits = 0)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				_writer.Write("null"); // see http://www.json.org/json.ppt
			}
			else if (0 < significantDigits && significantDigits < 17)
			{
				var format = Formats[significantDigits];
				_writer.Write(value.ToString(format, _invariant));
			}
			else
			{
				_writer.Write(value.ToString(_invariant));
			}
		}

		private static readonly string[] Formats = {
			"g0", "g1", "g2", "g3", "g4", "g5", "g6", "g7", "g8", "g9", "g10", "g11", "g12", "g13", "g14", "g15", "g16"
		};

		private void WriteJsonString(IEnumerable<char> value, bool includeDelimiters)
		{
			if (value == null)
			{
				_writer.Write("null");
				return;
			}

			if (includeDelimiters)
			{
				_writer.Write('"');
			}

			foreach (var c in value)
			{
				// See www.json.org:
				// Any unicode char except " or \ or control character

				if (char.IsControl(c))
				{
					switch (c)
					{
						case '\b':
							_writer.Write("\\b");
							break;
						case '\f':
							_writer.Write("\\f");
							break;
						case '\n':
							_writer.Write("\\n");
							break;
						case '\r':
							_writer.Write("\\r");
							break;
						case '\t':
							_writer.Write("\\t");
							break;
						default:
							_writer.Write("\\u");
							_writer.Write(HexChar((c >> 12) & 0xf));
							_writer.Write(HexChar((c >> 8) & 0xf));
							_writer.Write(HexChar((c >> 4) & 0xf));
							_writer.Write(HexChar(c & 0xf));
							break;
					}

					continue;
				}

				if (c == '"' || c == '\\')
				{
					_writer.Write('\\');
				}

				_writer.Write(c);
			}

			if (includeDelimiters)
			{
				_writer.Write('"');
			}
		}

		private static char HexChar(int value)
		{
			if (0 <= value && value < 16)
			{
				return "0123456789abcdef"[value];
			}

			throw new ArgumentException("value out of range");
		}

		private void Push(WriterState state)
		{
			_stack.Push(state);
		}

		private WriterState Pop()
		{
			if (_stack.Count > 0)
			{
				return _stack.Pop();
			}

			return WriterState.Error;
		}

		private WriterState State
		{
			get
			{
				if (_stack.Count > 0)
				{
					return _stack.Peek();
				}

				return WriterState.Error;
			}
		}

		#endregion

		#region Implementation of IDisposable

		public void Dispose()
		{
			// By contract, Dispose() can be called any number of times.
			// But Close can only be called once on an open stream/writer.
			if (_writer != null)
			{
				Close();
			}
		}

		#endregion

		#region Nested type: WriterState

		private enum WriterState
		{
			Initial,
			InArray1,
			InArrayN,
			InObject1,
			InObjectN,
			Error
		}

		#endregion
	}
}
