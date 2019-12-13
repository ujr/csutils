using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class TokenizerTest
	{
		[Fact]
		public void CanScanWhite()
		{
			Assert.Equal(0, Tokenizer.ScanWhite("", 0));
			Assert.Equal(0, Tokenizer.ScanWhite("", 99));
			Assert.Equal(0, Tokenizer.ScanWhite("foo bar", 0));
			Assert.Equal(1, Tokenizer.ScanWhite("foo bar", 3));
			Assert.Equal(4, Tokenizer.ScanWhite("\t\r\n baz", 0));

			Assert.Throws<ArgumentNullException>(() => Tokenizer.ScanWhite(null, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => Tokenizer.ScanWhite("foo", -1));
		}

		[Fact]
		public void CanScanName()
		{
			Assert.Equal(0, Tokenizer.ScanName("", 0));
			Assert.Equal(0, Tokenizer.ScanName("", 99));
			Assert.Equal(0, Tokenizer.ScanName(" foo13 ", 0));
			Assert.Equal(5, Tokenizer.ScanName(" foo13 ", 1));
			Assert.Equal(0, Tokenizer.ScanName(" foo13 ", 4));

			// By default, $ and _ are name chars:
			Assert.Equal(2, Tokenizer.ScanName("$_", 0));
			Assert.Equal(2, Tokenizer.ScanName("_$", 0));
			Assert.Equal(0, Tokenizer.ScanName("_foo", 0, ""));

			Assert.Throws<ArgumentNullException>(() => Tokenizer.ScanName(null, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => Tokenizer.ScanName("foo", -1));
		}

		[Fact]
		public void CanScanNumber()
		{
			Assert.Equal(3, Tokenizer.ScanNumber("123", 0));

			Assert.Equal(4, Tokenizer.ScanNumber("9876.54", 0));
			Assert.Equal(7, Tokenizer.ScanNumber("9876.54", 0, true));

			Assert.Equal(1, Tokenizer.ScanNumber("0.278e-05", 0));
			Assert.Equal(5, Tokenizer.ScanNumber("0.278e-05", 0, true));
			Assert.Equal(9, Tokenizer.ScanNumber("0.278e-05", 0, true, true));

			Assert.Equal(1, Tokenizer.ScanNumber("5*5", 0));

			Assert.Equal(0, Tokenizer.ScanNumber("-32", 0, true, true)); // sic
			Assert.Equal(2, Tokenizer.ScanNumber("-32", 1, true, true));

			Assert.Throws<ArgumentNullException>(() => Tokenizer.ScanNumber(null, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => Tokenizer.ScanNumber("foo", -1));
		}

		[Fact]
		public void CanScanNumberInt32()
		{
			int value;

			Assert.Equal(0, Tokenizer.ScanNumber("", 0, out value));

			Assert.Equal(2, Tokenizer.ScanNumber("99", 0, out value));
			Assert.Equal(99, value);

			Assert.Equal(1, Tokenizer.ScanNumber("99", 1, out value));
			Assert.Equal(9, value);

			Assert.Equal(0, Tokenizer.ScanNumber("-12", 0, out value));

			Assert.Equal(10, Tokenizer.ScanNumber("2147483647$", 0, out value));
			Assert.Equal(int.MaxValue, value);

			Assert.Throws<OverflowException>(() => Tokenizer.ScanNumber("2147483648", 0, out value));
			Assert.Throws<OverflowException>(() => Tokenizer.ScanNumber("10000000000", 0, out value));

			Assert.Throws<ArgumentNullException>(() => Tokenizer.ScanNumber(null, 0, out value));
			Assert.Throws<ArgumentOutOfRangeException>(() => Tokenizer.ScanNumber("123", -1, out value));
		}

		[Fact]
		public void CanScanString()
		{
			var buffer = new StringBuilder();

			Assert.Equal(5, Tokenizer.ScanString(@"""foo""bar", 0, buffer.Clear()));
			Assert.Equal("foo", buffer.ToString());

			Assert.Equal(6, Tokenizer.ScanString(@"3'j''s'3", 1, buffer.Clear()));
			Assert.Equal("j's", buffer.ToString());

			Assert.Equal(9, Tokenizer.ScanString(@"""a\nb\tc""", 0, buffer.Clear()));
			Assert.Equal("a\nb\tc", buffer.ToString());

			Assert.Equal(8, Tokenizer.ScanString(@"""\u0021""", 0, buffer.Clear()));
			Assert.Equal("!", buffer.ToString());

			var ex1 = Assert.Throws<FormatException>(() => Tokenizer.ScanString("'foo", 0, buffer));
			Assert.StartsWith("Unterminated string", ex1.Message);

			var ex2 = Assert.Throws<FormatException>(() => Tokenizer.ScanString("\"foo", 0, buffer));
			Assert.StartsWith("Unterminated string", ex2.Message);

			var ex3 = Assert.Throws<FormatException>(() => Tokenizer.ScanString("\"foo\n\"", 0, buffer));
			Assert.StartsWith("Control character in string", ex3.Message);

			var ex4 = Assert.Throws<FormatException>(() => Tokenizer.ScanString("\"\\x\"", 0, buffer));
			Assert.StartsWith("Unknown escape '\\x' in string", ex4.Message);

			var ex5 = Assert.Throws<FormatException>(() => Tokenizer.ScanString("\"\\u012x\"", 0, buffer));
			Assert.StartsWith("Incomplete \\u escape in string", ex5.Message);
		}

		[Fact]
		public void CanTokenize()
		{
			const string text = "\tf(123*-foo,   'it''s'+\"\\ntime\")\n";
			var tokenizer = new Tokenizer(text);

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsName("f"));
			Assert.Equal("f", tokenizer.CurrentValue);

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsOperator("("));

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsNumber());
			Assert.Equal(123.0, tokenizer.CurrentValue);

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsOperator("*"));
			Assert.Equal("*", tokenizer.CurrentValue);

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsOperator("+", "-"));

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsName("foo"));

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsOperator(","));

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsString());
			Assert.Equal("it's", tokenizer.CurrentValue);

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsOperator("+"));

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsString());
			Assert.Equal("\ntime", tokenizer.CurrentValue);

			Assert.True(tokenizer.Advance());
			Assert.True(tokenizer.IsOperator(")"));

			Assert.False(tokenizer.Advance());
			Assert.True(tokenizer.IsEnd);

			Assert.Equal(text.Length, tokenizer.Index);

			// Idempotence at end of input:
			Assert.False(tokenizer.Advance());
			Assert.True(tokenizer.IsEnd);
		}

		[Fact]
		public void CanTokenizeEmpty()
		{
			var tokenizer = new Tokenizer(string.Empty);
			Assert.False(tokenizer.Advance());
			Assert.True(tokenizer.IsEnd);
			Assert.Null(tokenizer.CurrentValue);
		}

		[Fact]
		public void CanTokenizeBlank()
		{
			var tokenizer = new Tokenizer(" \t ");
			Assert.False(tokenizer.Advance());
			Assert.True(tokenizer.IsEnd);
			Assert.Null(tokenizer.CurrentValue);
		}

		[Fact]
		public void CanTokenizeOperators()
		{
			const string text = "!!=>+++==>==<???+&&***<>";
			var tokenizer = new Tokenizer(text);

			var list = new List<string>();
			while (tokenizer.Advance())
			{
				list.Add((string) tokenizer.CurrentValue);
			}

			Assert.Equal(new[]{"!", "!=", ">", "++", "+=", "=>", "==", "<", "??", "?", "+", "&&", "**", "*", "<>"}, list);
		}

		[Fact]
		public void CanFormatString()
		{
			var buffer = new StringBuilder();

			Tokenizer.FormatValue(string.Empty, buffer.Clear());
			Assert.Equal("\"\"", buffer.ToString());

			Tokenizer.FormatValue("He's said: \"Hello!\"", buffer.Clear());
			Assert.Equal("\"He's said: \\\"Hello!\\\"\"", buffer.ToString());

			Tokenizer.FormatValue("She repl'd: \"\n#\t\'\a\bc\u0000", buffer.Clear());
			Assert.Equal("\"She repl'd: \\\"\\n#\\t'\\u0007\\bc\\u0000\"", buffer.ToString());
		}

		[Fact]
		public void CanFormatValue()
		{
			var buffer = new StringBuilder();

			Tokenizer.FormatValue(null, buffer);
			Tokenizer.FormatValue(0, buffer);
			Tokenizer.FormatValue(false, buffer);
			Tokenizer.FormatValue(-1, buffer);
			Tokenizer.FormatValue(true, buffer);
			Tokenizer.FormatValue("hello", buffer);
			Tokenizer.FormatValue(-1.25, buffer);

			Assert.Equal("null0false-1true\"hello\"-1.25", buffer.ToString());
		}

		[Fact]
		public void CanSyntaxError()
		{
			var ex = Tokenizer.SyntaxError(123, "Oops: {0}", "testing");
			Assert.IsType<FormatException>(ex);
			Assert.True(ex.Message.Contains("123"), "expect position in message");
		}
	}
}
