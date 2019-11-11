using System;
using System.IO;
using System.Text;
using Xunit;

namespace Sylphe.Json.Test
{
	public class JsonWriterTest
	{
		[Fact]
		public void JsonWriter_CanSerialize()
		{
			var buffer = new StringBuilder();
			var writer = new StringWriter(buffer);
			var jsonWriter = new JsonWriter(writer);

			jsonWriter.WriteStartObject();
			jsonWriter.WriteProperty("ok", true);
			jsonWriter.WriteProperty("info", "ok");
			jsonWriter.WritePropertyName("locations");
			jsonWriter.WriteStartArray();
			jsonWriter.WriteStartObject();
			jsonWriter.WriteProperty("id", 1906);
			jsonWriter.WriteProperty("name", "Alte Gasse, 40489 Düsseldorf");
			jsonWriter.WritePropertyName("pt");
			jsonWriter.WriteStartArray();
			jsonWriter.WriteValue(2555054.82);
			jsonWriter.WriteValue(5689233);
			jsonWriter.WriteEndArray();
			jsonWriter.WriteEndObject();
			jsonWriter.WriteEndArray();
			jsonWriter.WriteProperty("nasty", "Hello \b\f\n\r\t\0");
			jsonWriter.WriteEndObject();

			jsonWriter.Close();

			const string expected = @"{""ok"":true,""info"":""ok"",""locations"":[{""id"":1906,""name"":""Alte Gasse, 40489 Düsseldorf"",""pt"":[2555054.82,5689233]}],""nasty"":""Hello \b\f\n\r\t\u0000""}";
			string actual = buffer.ToString();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void JsonWriter_CanInt64()
		{
			var buffer = new StringBuilder();
			var writer = new StringWriter(buffer);
			var jsonWriter = new JsonWriter(writer);

			jsonWriter.WriteStartArray();
			jsonWriter.WriteValue(int.MaxValue);
			jsonWriter.WriteValue(long.MaxValue);
			jsonWriter.WriteValue((double) int.MaxValue);
			// Note: (double) long.MaxValue will loose precision,
			// but I've seen differences between runtime environments
			jsonWriter.WriteEndArray();

			jsonWriter.Close();

			const string expected = @"[2147483647,9223372036854775807,2147483647]";
			string actual = buffer.ToString();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void JsonWriter_CanNanAndInf()
		{
			var buffer = new StringBuilder();
			var writer = new StringWriter(buffer);
			var jsonWriter = new JsonWriter(writer);

			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyName("notANumber");
			jsonWriter.WriteValue(double.NaN);
			jsonWriter.WriteProperty("positiveInfinity", double.PositiveInfinity);
			jsonWriter.WriteProperty("negativeInfinity", double.NegativeInfinity);
			jsonWriter.WriteEndObject();

			const string expected = @"{""notANumber"":null,""positiveInfinity"":null,""negativeInfinity"":null}";
			string actual = buffer.ToString();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void JsonWriter_ProperContentType()
		{
			var buffer = new StringBuilder();
			var writer = new StringWriter(buffer);

			var json = new JsonWriter(writer);
			Assert.Equal("application/json", json.ContentType);

			var jsonp = new JsonWriter(writer, false, "callback");
			Assert.Equal("application/javascript", jsonp.ContentType);
		}

		[Fact]
		public void JsonWriter_Encoding_Retained()
		{
			var encoding = new UTF8Encoding(false);

			var buffer = new MemoryStream();
			var writer = new StreamWriter(buffer, encoding);

			var json = new JsonWriter(writer);
			Assert.Same(encoding, json.Encoding);
		}

		[Fact]
		public void JsonWriter_LifecycleTest()
		{
			var buffer = new MemoryStream();
			var writer = new StreamWriter(buffer);
			var json = new JsonWriter(writer);
			json.WriteNull();
			json.Flush();
			json.Close();
			Assert.Throws<ObjectDisposedException>(json.WriteNull);
			Assert.Throws<ObjectDisposedException>(json.Close);
			json.Dispose(); // by contract: can Dispose() repeatedly
		}
	}
}
