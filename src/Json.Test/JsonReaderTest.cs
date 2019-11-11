using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Json.Test
{
	public class JsonReaderTest
	{
		private readonly ITestOutputHelper _output;

		public JsonReaderTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void CanReadPrimitives()
		{
			var r1 = new JsonReader("null");
			AssertRead(r1, 0, JsonType.Null, null, 0, null, JsonType.None);
			AssertReadEnd(r1);

			var r2 = new JsonReader("\"abc\\tdef\\u0021\"");
			AssertRead(r2, 0, JsonType.String, "abc\tdef!", 0, null, JsonType.None);
			AssertReadEnd(r2);

			var r3 = new JsonReader("-123");
			AssertRead(r3, 0, JsonType.Number, -123, 0, null, JsonType.None);
			AssertReadEnd(r3);

			var r4 = new JsonReader("1.2e3");
			AssertRead(r4, 0, JsonType.Number, 1.2e3, 0, null, JsonType.None);
			AssertReadEnd(r4);

			var r5 = new JsonReader("true");
			AssertRead(r5, 0, JsonType.True, true, 0, null, JsonType.None);
			AssertReadEnd(r5);

			var r6 = new JsonReader("false");
			AssertRead(r6, 0, JsonType.False, false, 0, null, JsonType.None);
			AssertReadEnd(r6);
		}

		[Fact]
		public void CanReadArrays()
		{
			var r1 = new JsonReader("[]");
			AssertRead(r1, 1, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r1, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r1);

			var r2 = new JsonReader("  [  ]  ");
			AssertRead(r2, 1, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r2, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r2);

			var r3 = new JsonReader("[99]");
			AssertRead(r3, 1, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r3, 1, JsonType.Number, 99, 0, null, JsonType.Array);
			AssertRead(r3, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r3);

			var r4 = new JsonReader("[1,\"two\", null, false, true]");
			AssertRead(r4, 1, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r4, 1, JsonType.Number, 1, 0, null, JsonType.Array);
			AssertRead(r4, 1, JsonType.String, "two", 1, null, JsonType.Array);
			AssertRead(r4, 1, JsonType.Null, null, 2, null, JsonType.Array);
			AssertRead(r4, 1, JsonType.False, false, 3, null, JsonType.Array);
			AssertRead(r4, 1, JsonType.True, true, 4, null, JsonType.Array);
			AssertRead(r4, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r4);

			var r5 = new JsonReader("[1,[2,[[4]]]]");
			AssertRead(r5, 1, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r5, 1, JsonType.Number, 1, 0, null, JsonType.Array);
			AssertRead(r5, 2, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r5, 2, JsonType.Number, 2, 0, null, JsonType.Array);
			AssertRead(r5, 3, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r5, 4, JsonType.Array, null, 0, null, JsonType.Array);
			AssertRead(r5, 4, JsonType.Number, 4, 0, null, JsonType.Array);
			AssertRead(r5, 3, JsonType.Closed, null, 0, null, JsonType.Array);
			AssertRead(r5, 2, JsonType.Closed, null, 1, null, JsonType.Array);
			AssertRead(r5, 1, JsonType.Closed, null, 1, null, JsonType.Array);
			AssertRead(r5, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r5);
		}

		[Fact]
		public void CanReadObjects()
		{
			var r1 = new JsonReader("{}");
			AssertRead(r1, 1, JsonType.Object, null, 0, null, JsonType.Object);
			AssertRead(r1, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r1);

			var r2 = new JsonReader("{\"num\":123}");
			AssertRead(r2, 1, JsonType.Object, null, 0, null, JsonType.Object);
			AssertRead(r2, 1, JsonType.Number, 123, 0, "num", JsonType.Object);
			AssertRead(r2, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r2);

			var r3 = new JsonReader("{\"foo\":\"bar\"}");
			AssertRead(r3, 1, JsonType.Object, null, 0, null, JsonType.Object);
			AssertRead(r3, 1, JsonType.String, "bar", 0, "foo", JsonType.Object);
			AssertRead(r3, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r3);

			var r4 = new JsonReader("{\"foo\":\"bar\",\"num\":123}");
			AssertRead(r4, 1, JsonType.Object, null, 0, null, JsonType.Object);
			AssertRead(r4, 1, JsonType.String, "bar", 0, "foo", JsonType.Object);
			AssertRead(r4, 1, JsonType.Number, 123, 1, "num", JsonType.Object);
			AssertRead(r4, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r4);

			var r5 = new JsonReader("{\"foo\":{\"bar\":{\"baz\":{\"quux\":[42]}}}}");
			AssertRead(r5, 1, JsonType.Object, null, 0, null, JsonType.Object);
			AssertRead(r5, 2, JsonType.Object, null, 0, "foo", JsonType.Object);
			AssertRead(r5, 3, JsonType.Object, null, 0, "bar", JsonType.Object);
			AssertRead(r5, 4, JsonType.Object, null, 0, "baz", JsonType.Object);
			AssertRead(r5, 5, JsonType.Array, null, 0, "quux", JsonType.Array);
			AssertRead(r5, 5, JsonType.Number, 42, 0, null, JsonType.Array);
			AssertRead(r5, 4, JsonType.Closed, null, 0, "quux", JsonType.Object);
			AssertRead(r5, 3, JsonType.Closed, null, 0, "baz", JsonType.Object);
			AssertRead(r5, 2, JsonType.Closed, null, 0, "bar", JsonType.Object);
			AssertRead(r5, 1, JsonType.Closed, null, 0, "foo", JsonType.Object);
			AssertRead(r5, 0, JsonType.Closed, null, 0, null, JsonType.None);
			AssertReadEnd(r5);
		}

		[Fact]
		public void CanCatchErrors()
		{
			Assert.Throws<JsonException>(() => new JsonReader("\"\\u99 incomplete\"").Read());
			Assert.Throws<JsonException>(() => new JsonReader("\"\\u999x invalid\"").Read());
			Assert.Throws<JsonException>(() => new JsonReader("\"unterminated").Read());
			Assert.Throws<JsonException>(() => new JsonReader("\"unknown escape \\x\"").Read());
			Assert.Throws<JsonException>(() => new JsonReader("\"control char\tin string\"").Read());

			Assert.Throws<JsonException>(() => new JsonReader("123x").Read());

			Assert.Throws<JsonException>(() => ReadAll("[],"));
			Assert.Throws<JsonException>(() => ReadAll("]"));
			Assert.Throws<JsonException>(() => ReadAll("{}}"));
			Assert.Throws<JsonException>(() => ReadAll("[{"));
			Assert.Throws<JsonException>(() => ReadAll("{\"a\":[}"));
		}

		[Fact(Skip="debugging")]
		public void DumpSamples()
		{
			JsonReadDump("\"abc\\tdef\"");

			JsonReadDump("[]");
			JsonReadDump("  [  ]  ");
			JsonReadDump("[99]");
			JsonReadDump("[1,\"two\", null, false, true]");
			JsonReadDump("[1,[2,[[4]]]]");

			JsonReadDump("{}");
			JsonReadDump("{\"num\":123}");
			JsonReadDump("{\"foo\":\"bar\"}");
			JsonReadDump("{\"foo\":\"bar\",\"num\":123}");
			JsonReadDump("{\"foo\":{\"bar\":{\"baz\":{\"quux\":[42]}}}}");

			JsonReadDump("{\"loc\":[12,23],\"sref\":{\"wkid\":4326}}");
		}

		[Fact(Skip="experimental")]
		public void JsonParseTrial()
		{
			const string json =
				@"{""foo"":{""bar"":22,""baz"":333}, " +
				@"""quux"":[{""text"":""42"",""pt"":{""x"":123.4,""y"":567.8}}]}";
			//const string json = @"{""foo"":1, ""bar"":2, ""baz"":3}";
			//const string json = @"[1, false, ""zero"", {}]";

			// $.foo.bar, $.foo.baz, $.quux[0].text, $.quux[0].pt.x, $.quux[0].pt.y

			var reader = new JsonReader(json);
			var stack = new Stack<object>();

			while (reader.Read())
			{
				if (reader.Type == JsonType.Array || reader.Type == JsonType.Object)
				{
					if (reader.Depth == 1) stack.Push("$");
					else stack.Push((object) reader.Label ?? reader.Index);
				}
				else if (reader.Type == JsonType.Closed)
				{
					stack.Pop();
				}
				else
				{
					stack.Push((object) reader.Label ?? reader.Index);
					DumpStack(stack, $" = {reader.Value}");
					stack.Pop();
				}
			}
		}

		#region Utils

		private void DumpStack<T>(Stack<T> stack, string suffix)
		{
			var sb = new StringBuilder("$");
			foreach (var item in stack.Reverse().Skip(1))
			{
				if (item is string)
					sb.Append($@".{item}");
				else if (item is int)
					sb.Append($@"[{item}]");
			}
			sb.Append(suffix);
			_output.WriteLine(sb.ToString());
		}

		private void JsonReadDump(string json)
		{
			_output.WriteLine(@">>> {0}$", json);
			var reader = new JsonReader(json);

			while (reader.Read())
			{
				_output.WriteLine(@"Type={0,-8} Value={1,-6} Index={2,-3} Label={3,-6} Depth={4,-2} Context={5}",
					reader.Type, reader.Value, reader.Index, reader.Label, reader.Depth, reader.Context);
			}
			_output.WriteLine("");
		}

		private static void ReadAll(string json)
		{
			var reader = new JsonReader(json);
			while (reader.Read()) { }
		}

		private static void AssertRead(
			JsonReader reader, int depth, JsonType type, object value,
			int index, string label, JsonType context)
		{
			Assert.True(reader.Read()); // too little JSON input

			if (reader.Value is double && value is int)
			{
				value = (double) (int) value;
			}

			Assert.Equal(type, reader.Type);
			Assert.Equal(value, reader.Value);

			Assert.Equal(index, reader.Index);
			Assert.Equal(label, reader.Label);
			Assert.Equal(depth, reader.Depth);
			Assert.Equal(context, reader.Context);
		}

		private static void AssertReadEnd(JsonReader reader)
		{
			Assert.False(reader.Read()); // too much JSON input
		}

		#endregion
	}
}
