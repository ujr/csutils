using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Sylphe.Csv.Test;

public class CsvTest
{
	[Fact]
	public void CsvReaderTest()
	{
		const string csv =
			"Hello,\"happy\",world\r\nHow, do\t, \" you \" ,do? \n \r\n 3 empty (\"\"\"\"\"\") fields: ,  ,, \"\" ";
		var reader = new CsvReader(new StringReader(csv));
		Assert.Equal(0, reader.Values.Count);

		Assert.True(reader.ReadRecord());
		Assert.Equal(3, reader.Values.Count);
		Assert.Equal("Hello", reader.Values[0]);
		Assert.Equal("happy", reader.Values[1]);
		Assert.Equal("world", reader.Values[2]);
		Assert.Equal(1, reader.RecordNumber);
		Assert.Equal(2, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(4, reader.Values.Count);
		Assert.Equal("How", reader.Values[0]);
		Assert.Equal("do", reader.Values[1]);
		Assert.Equal(" you ", reader.Values[2]);
		Assert.Equal("do?", reader.Values[3]);
		Assert.Equal(2, reader.RecordNumber);
		Assert.Equal(3, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(3, reader.Values.Count);
		Assert.Empty(string.Join("", reader.Values));
		Assert.Equal(3, reader.RecordNumber);
		Assert.Equal(4, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(4, reader.Values.Count);
		Assert.Equal("3 empty (\"\") fields:", reader.Values[0]);
		Assert.Equal(string.Empty, reader.Values[1]);
		Assert.Equal(string.Empty, reader.Values[2]);
		Assert.Equal(string.Empty, reader.Values[3]);
		Assert.Equal(4, reader.RecordNumber);
		Assert.Equal(4, reader.LineNumber); // no line terminator on last line

		Assert.False(reader.ReadRecord());
		Assert.Equal(0, reader.Values.Count);
		Assert.Equal(4, reader.RecordNumber);
		Assert.Equal(4, reader.LineNumber);

		reader.Dispose();

		Assert.Throws<ObjectDisposedException>(() => reader.ReadRecord());
	}

	[Fact]
	public void CsvReaderNewlineTest()
	{
		const string csv = "One \"\"\"1\"\"\" here \nTwo\rThree\r\nFour\n\rSix\r\n\"\n\r\n\r\"";
		var reader = new CsvReader(new StringReader(csv));

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("One \"1\" here", reader.Values[0]);
		Assert.Equal(1, reader.RecordNumber);
		Assert.Equal(2, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("Two", reader.Values[0]);
		Assert.Equal(2, reader.RecordNumber);
		Assert.Equal(3, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("Three", reader.Values[0]);
		Assert.Equal(3, reader.RecordNumber);
		Assert.Equal(4, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("Four", reader.Values[0]);
		Assert.Equal(4, reader.RecordNumber);
		Assert.Equal(5, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Empty(reader.Values[0]);
		Assert.Equal(5, reader.RecordNumber);
		Assert.Equal(6, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("Six", reader.Values[0]);
		Assert.Equal(6, reader.RecordNumber);
		Assert.Equal(7, reader.LineNumber);

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("\n\r\n\r", reader.Values[0]);
		Assert.Equal(7, reader.RecordNumber);
		Assert.Equal(10, reader.LineNumber);

		Assert.False(reader.ReadRecord());

		reader.Dispose();
	}

	[Fact]
	public void CsvReaderValueTest()
	{
		// Our reader allows fields to be partially quoted like this:
		// foo,The message was: """be tolerant in what you take""",bar

		const string csv =
			"Tight, Trimmed,\" Padded \", \" within quotes only \" , Mix: \"\"\"fine\"\"\" ingredients ";

		var reader = new CsvReader(new StringReader(csv));

		Assert.True(reader.ReadRecord());
		Assert.Equal(5, reader.Values.Count);
		Assert.Equal("Tight", reader.Values[0]);
		Assert.Equal("Trimmed", reader.Values[1]);
		Assert.Equal(" Padded ", reader.Values[2]);
		Assert.Equal(" within quotes only ", reader.Values[3]);
		Assert.Equal("Mix: \"fine\" ingredients", reader.Values[4]);

		Assert.False(reader.ReadRecord());

		reader.Dispose();
	}

	[Fact]
	public void CsvReaderEmptyFieldsTest()
	{
		// Empty fields are returned.
		// A record consisting only of an empty field is still returned.
		// A blank line is returned as one empty field.
		// There's no way to distinguish an empty line from a blank line.

		const string csv = " # Comment line \r\n" +
		                   ", ,  ,\"\", \"\", \" \" \r\n";

		var reader = new CsvReader(new StringReader(csv));

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.Values.Count);
		Assert.Equal("# Comment line", reader.Values[0]);

		Assert.True(reader.ReadRecord());
		Assert.Equal(6, reader.Values.Count);
		Assert.Empty(reader.Values[0]);
		Assert.Empty(reader.Values[1]);
		Assert.Empty(reader.Values[2]);
		Assert.Empty(reader.Values[3]);
		Assert.Empty(reader.Values[4]);
		Assert.Equal(" ", reader.Values[5]);

		Assert.False(reader.ReadRecord());

		reader.Dispose();
	}

	[Fact]
	public void CsvReaderPadEmptyTest()
	{
		const char sep = ';';
		const string csv = "a;b;c\nd;e\nf;\ng;h;i;j\nk";

		using (var reader = new CsvReader(new StringReader(csv), sep))
		{
			Assert.True(reader.ReadRecord());
			Assert.Equal(3, reader.Values.Count);
			Assert.True(Seq("a", "b", "c").SequenceEqual(reader.Values));

			Assert.True(reader.ReadRecord());
			Assert.Equal(3, reader.Values.Count);
			Assert.True(Seq("d", "e", "").SequenceEqual(reader.Values));

			Assert.True(reader.ReadRecord());
			Assert.Equal(3, reader.Values.Count);
			Assert.True(Seq("f", "", "").SequenceEqual(reader.Values));

			Assert.True(reader.ReadRecord());
			Assert.Equal(4, reader.Values.Count);
			Assert.True(Seq("g", "h", "i", "j").SequenceEqual(reader.Values));

			Assert.True(reader.ReadRecord());
			Assert.Equal(3, reader.Values.Count);
			Assert.True(Seq("k", "", "").SequenceEqual(reader.Values));

			Assert.False(reader.ReadRecord());
			Assert.Equal(0, reader.Values.Count);
		}
	}

	[Fact]
	public void CsvFileEndTest()
	{
		const char sep = ';';
		const string csv1 = "a;\r\na;\r\n"; // file ends with CR LF EOF
		const string csv2 = "a;\r\na;"; // file ends with just EOF

		// Both "files" must yield the same records and fields!

		Action<CsvReader> assertor =
			reader =>
			{
				Assert.True(reader.ReadRecord());
				Assert.Equal(2, reader.Values.Count);
				Assert.Equal(string.Empty, reader.Values[1]);

				Assert.True(reader.ReadRecord());
				Assert.Equal(2, reader.Values.Count);
				Assert.Equal(string.Empty, reader.Values[1]);

				Assert.False(reader.ReadRecord());
			};

		using (var reader = new CsvReader(new StringReader(csv1), sep))
		{
			assertor(reader);
		}

		using (var reader = new CsvReader(new StringReader(csv2), sep))
		{
			assertor(reader);
		}
	}

	[Fact]
	public void CsvReaderSkipBlankTest()
	{
		//                  1   2 3   4    5       6   7 8
		const string csv = "\r\n\n\r  \r\n,\r\n\"\"\r\n\n";
		var reader = new CsvReader(new StringReader(csv)) {SkipBlankLines = true};

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.RecordNumber);
		Assert.Equal(6, reader.LineNumber);
		Assert.Equal(2, reader.Values.Count);
		Assert.Empty(reader.Values[0]);
		Assert.Empty(reader.Values[1]);

		Assert.True(reader.ReadRecord());
		Assert.Equal(2, reader.RecordNumber);
		Assert.Equal(7, reader.LineNumber);
		Assert.Equal(2, reader.Values.Count);
		Assert.Empty(reader.Values[0]); // the only field is empty, but quoted
		Assert.Empty(reader.Values[1]); // pad field to match #fields of first row

		Assert.False(reader.ReadRecord());
		Assert.Equal(2, reader.RecordNumber);
		Assert.Equal(8, reader.LineNumber);
		Assert.Equal(0, reader.Values.Count);
	}

	[Fact]
	public void CsvReaderSkipCommentTest()
	{
		const string csv = "# Comment\r\n,#Not a comment\r\n  #Another";
		var reader = new CsvReader(new StringReader(csv)) {SkipCommentLines = true};

		Assert.True(reader.ReadRecord());
		Assert.Equal(1, reader.RecordNumber);
		Assert.Equal(3, reader.LineNumber);
		Assert.Equal(2, reader.Values.Count);
		Assert.Equal("", reader.Values[0]);
		Assert.Equal("#Not a comment", reader.Values[1]);

		Assert.False(reader.ReadRecord());
	}

	[Fact]
	public void CsvWriterTest()
	{
		var buffer = new StringBuilder();
		var writer = new CsvWriter(new StringWriter(buffer));

		writer.WriteRecord("One", "Two", "Three");
		writer.WriteRecord(); // blank line
		writer.WriteRecord("QuoteChar", new string(writer.QuoteChar, 1));
		writer.WriteRecord("FieldSeparator", new string(writer.FieldSeparator, 1));
		writer.WriteRecord("a\"b\"c", "line\nbreak", "line\rbreak", "line\r\nbreak");
		writer.WriteRecord(" leading", "trailing ", " blanks ");

		writer.Dispose();

		Assert.Throws<ObjectDisposedException>(() => writer.WriteRecord());

		string expected =
			"One,Two,Three\n" +
			"\n" +
			"QuoteChar,\"\"\"\"\n" +
			"FieldSeparator,\",\"\n" +
			"\"a\"\"b\"\"c\",\"line\nbreak\",\"line\rbreak\",\"line\r\nbreak\"\n" +
			"\" leading\",\"trailing \",\" blanks \"\n"
			.Replace("\n", Environment.NewLine);

		Assert.Equal(expected, buffer.ToString());
	}

	[Fact]
	public void CsvWriterEmptyTest()
	{
		var buffer = new StringBuilder();

		new CsvWriter(new StringWriter(buffer)).Dispose();

		Assert.Empty(buffer.ToString());

		buffer.Length = 0; // clear

		new CsvWriter(new StringWriter(buffer)).WriteRecord().Dispose();

		Assert.Equal(Environment.NewLine, buffer.ToString());

		buffer.Length = 0; // clear

		new CsvWriter(new StringWriter(buffer)).WriteRecord(string.Empty).Dispose();

		Assert.Equal(Environment.NewLine, buffer.ToString());
	}

	[Fact]
	public void CsvDefaultSettingsTest()
	{
		// The defaults of properties must not change
		// or client code may break!

		var reader = new CsvReader(TextReader.Null);

		Assert.Equal('"', reader.QuoteChar);
		Assert.Equal(',', reader.FieldSeparator);

		Assert.False(reader.SkipBlankLines);
		Assert.False(reader.SkipCommentLines);
		Assert.Equal('#', reader.CommentChar);

		var writer = new CsvWriter(TextWriter.Null);

		Assert.Equal('"', writer.QuoteChar);
		Assert.Equal(',', writer.FieldSeparator);

		Assert.False(writer.QuoteAllFields);
	}

	[Fact]
	public void CsvRoundtripTest()
	{
		const char sep = ';';

		var buffer = new StringBuilder();

		using (var writer = new CsvWriter(new StringWriter(buffer), sep))
		{
			writer.WriteRecord("One", "Two", "Three");
			writer.WriteRecord(); // blank line
			writer.WriteRecord("QuoteChar", new string(writer.QuoteChar, 1));
			writer.WriteRecord("FieldSeparator", new string(writer.FieldSeparator, 1));
			writer.WriteRecord("a\"b\"c", "line\nbreak", "line\rbreak", "line\r\nbreak");
			writer.WriteRecord(" leading", "trailing ", " blanks ");
		}

		string csv = buffer.ToString();

		using (var reader = new CsvReader(new StringReader(csv), sep))
		{
			Assert.True(reader.ReadRecord());
			Assert.Equal("One|Two|Three",
			                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

			Assert.True(reader.ReadRecord());
			Assert.Equal("||",
			                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

			Assert.True(reader.ReadRecord());
			Assert.Equal("QuoteChar|\"|",
			                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

			Assert.True(reader.ReadRecord());
			Assert.Equal($"FieldSeparator|{sep}|",
			                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

			Assert.True(reader.ReadRecord());
			Assert.Equal("a\"b\"c|line\nbreak|line\rbreak|line\r\nbreak",
			                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

			Assert.True(reader.ReadRecord());
			Assert.Equal(" leading|trailing | blanks ",
			                reader.Values.Aggregate((s, t) => string.Concat(s, "|", t)));

			Assert.False(reader.ReadRecord());
		}
	}

	private static IEnumerable<T> Seq<T>(params T[] args)
	{
		return args;
	}
}
