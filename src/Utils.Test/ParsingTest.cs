using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Sylphe.Utils.Test
{
	public class ParsingTest
	{
		[Fact]
		public void CanParseFieldList()
		{
			var r0 = Parsing.ParseFieldList(null, ':');
			Assert.Empty(r0);

			var r1 = Parsing.ParseFieldList("a:b:c", ':');
			Assert.Equal(Seq("a", "b", "c"), r1);

			var r2 = Parsing.ParseFieldList("a:b:c", ':', 1, 0);
			Assert.Equal(Seq(""), r2);

			var r3 = Parsing.ParseFieldList("a:b:c", ':', 1, 1);
			Assert.Equal(Seq("", ""), r3);

			var r4 = Parsing.ParseFieldList("a:b:c", ':', 2, 1);
			Assert.Equal(Seq("b"), r4);

			var r5 = Parsing.ParseFieldList("foo: bar :baz", ':', 3, 9);
			Assert.Equal(Seq("", "bar", "ba"), r5);

			var r6 = Parsing.ParseFieldList("", ':');
			Assert.Equal(Seq(""), r6);

			var r7 = Parsing.ParseFieldList(":::", ':');
			Assert.Equal(Seq("", "", "", ""), r7);

			var r8 = Parsing.ParseFieldList(": ", ':');
			Assert.Equal(Seq("", ""), r8);

			var r9 = Parsing.ParseFieldList(" : foo : ", ':', 2, 5);
			Assert.Equal(Seq("foo"), r9);
		}

		[Fact]
		public void CanParsePageList()
		{
			Assert.Empty(Parsing.ParsePageList(null));
			Assert.Empty(Parsing.ParsePageList(string.Empty));
			Assert.Empty(Parsing.ParsePageList("  "));

			var r1 = Parsing.ParsePageList("1,5-7,12,14,29-27");
			Assert.Equal(Seq(1, 5, 6, 7, 12, 14, 29, 28, 27), r1);

			// white space is ignored
			var r2 = Parsing.ParsePageList(" 1 , 5 - 7 , 12 , 14 , 29 - 27 ");
			Assert.Equal(Seq(1, 5, 6, 7, 12, 14, 29, 28, 27), r2);

			// comma as separator is optional
			var r3 = Parsing.ParsePageList("1 5-7 12 14 29-27");
			Assert.Equal(Seq(1, 5, 6, 7, 12, 14, 29, 28, 27), r3);

			// a single number is valid input
			var r4 = Parsing.ParsePageList("99");
			Assert.Equal(Seq(99), r4);

			// the "singleton range"
			var r5 = Parsing.ParsePageList("5-5");
			Assert.Equal(Seq(5), r5);

			// numbers up to int.MaxValue are fine (beware of wrap-around)
			var r6 = Parsing.ParsePageList("2147483647-2147480647");
			var e6 = Enumerable.Range(int.MaxValue - 3000, 3001).Reverse();
			Assert.Equal(e6, r6);
			var r7 = Parsing.ParsePageList("2147480647-2147483647");
			var e7 = Enumerable.Range(int.MaxValue - 3000, 3001);
			Assert.Equal(e7, r7);
			// and down to zero
			var r8 = Parsing.ParsePageList("5-0");
			Assert.Equal(Seq(5, 4, 3, 2, 1, 0), r8);

			// detect syntax errors (ToList to force enumeration)
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("-3").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("1,,2").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("1--2").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("3-").ToList());
			Assert.Throws<FormatException>(() => Parsing.ParsePageList("1,-2").ToList());

			// detect numeric overflow (ToList to force enumeration)
			Assert.Throws<OverflowException>(() => Parsing.ParsePageList("2147483648").ToList());
			Assert.Throws<OverflowException>(() => Parsing.ParsePageList("21474836400").ToList());
		}

		[Fact]
		public void CanScanWhite()
		{
			Assert.Equal(0, Parsing.ScanWhite("", 0));
			Assert.Equal(0, Parsing.ScanWhite("", 99));
			Assert.Equal(0, Parsing.ScanWhite("foo bar", 0));
			Assert.Equal(1, Parsing.ScanWhite("foo bar", 3));
			Assert.Equal(4, Parsing.ScanWhite("\t\r\n baz", 0));

			Assert.Throws<ArgumentNullException>(() => Parsing.ScanWhite(null, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => Parsing.ScanWhite("foo", -1));
		}

		[Fact]
		public void CanScanName()
		{
			Assert.Equal(0, Parsing.ScanName("", 0));
			Assert.Equal(0, Parsing.ScanName("", 99));
			Assert.Equal(0, Parsing.ScanName(" foo13 ", 0));
			Assert.Equal(5, Parsing.ScanName(" foo13 ", 1));
			Assert.Equal(0, Parsing.ScanName(" foo13 ", 4));

			Assert.Throws<ArgumentNullException>(() => Parsing.ScanName(null, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => Parsing.ScanName("foo", -1));
		}

		[Fact]
		public void CanScanNumber()
		{
			Assert.Equal(3, Parsing.ScanNumber("123", 0));

			Assert.Equal(4, Parsing.ScanNumber("9876.54", 0));
			Assert.Equal(7, Parsing.ScanNumber("9876.54", 0, true));

			Assert.Equal(1, Parsing.ScanNumber("0.278e-05", 0));
			Assert.Equal(5, Parsing.ScanNumber("0.278e-05", 0, true));
			Assert.Equal(9, Parsing.ScanNumber("0.278e-05", 0, true, true));

			Assert.Equal(1, Parsing.ScanNumber("5*5", 0));

			Assert.Equal(0, Parsing.ScanNumber("-32", 0, true, true)); // sic
			Assert.Equal(2, Parsing.ScanNumber("-32", 1, true, true));

			Assert.Throws<ArgumentNullException>(() => Parsing.ScanNumber(null, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => Parsing.ScanNumber("foo", -1));
		}

		[Fact]
		public void CanScanNumberInt32()
		{
			int value;

			Assert.Equal(0, Parsing.ScanNumber("", 0, out value));

			Assert.Equal(2, Parsing.ScanNumber("99", 0, out value));
			Assert.Equal(99, value);

			Assert.Equal(1, Parsing.ScanNumber("99", 1, out value));
			Assert.Equal(9, value);

			Assert.Equal(0, Parsing.ScanNumber("-12", 0, out value));

			Assert.Equal(10, Parsing.ScanNumber("2147483647$", 0, out value));
			Assert.Equal(int.MaxValue, value);

			Assert.Throws<OverflowException>(() => Parsing.ScanNumber("2147483648", 0, out value));
			Assert.Throws<OverflowException>(() => Parsing.ScanNumber("10000000000", 0, out value));

			Assert.Throws<ArgumentNullException>(() => Parsing.ScanNumber(null, 0, out value));
			Assert.Throws<ArgumentOutOfRangeException>(() => Parsing.ScanNumber("123", -1, out value));
		}

		[Fact]
		public void CanScanString()
		{
			var buffer = new StringBuilder();

			buffer.Clear();
			Assert.Equal(5, Parsing.ScanString(@"""foo""bar", 0, buffer));
			Assert.Equal("foo", buffer.ToString());

			buffer.Clear();
			Assert.Equal(6, Parsing.ScanString(@"3'j''s'3", 1, buffer));
			Assert.Equal("j's", buffer.ToString());

			buffer.Clear();
			Assert.Equal(9, Parsing.ScanString(@"""a\nb\tc""", 0, buffer));
			Assert.Equal("a\nb\tc", buffer.ToString());

			buffer.Clear();
			Assert.Equal(8, Parsing.ScanString(@"""\u0021""", 0, buffer));
			Assert.Equal("!", buffer.ToString());

			var ex1 = Assert.Throws<FormatException>(() => Parsing.ScanString("'foo", 0, buffer));
			Assert.StartsWith("Unterminated string", ex1.Message);

			var ex2 = Assert.Throws<FormatException>(() => Parsing.ScanString("\"foo", 0, buffer));
			Assert.StartsWith("Unterminated string", ex2.Message);

			var ex3 = Assert.Throws<FormatException>(() => Parsing.ScanString("\"foo\n\"", 0, buffer));
			Assert.StartsWith("Control character in string", ex3.Message);

			var ex4 = Assert.Throws<FormatException>(() => Parsing.ScanString("\"\\x\"", 0, buffer));
			Assert.StartsWith("Unknown escape '\\x' in string", ex4.Message);

			var ex5 = Assert.Throws<FormatException>(() => Parsing.ScanString("\"\\u012x\"", 0, buffer));
			Assert.StartsWith("Incomplete \\u escape in string", ex5.Message);
		}

		[Fact]
		public void CanFormatString()
		{
			var buffer = new StringBuilder();

			buffer.Clear();
			Parsing.FormatString(string.Empty, buffer);
			Assert.Equal("\"\"", buffer.ToString());

			buffer.Clear();
			Parsing.FormatString("He's said: \"Hello!\"", buffer);
			Assert.Equal("\"He's said: \\\"Hello!\\\"\"", buffer.ToString());

			buffer.Clear();
			Parsing.FormatString("She repl'd: \"\n#\t\'\a\bc\u0000", buffer);
			Assert.Equal("\"She repl'd: \\\"\\n#\\t'\\u0007\\bc\\u0000\"", buffer.ToString());
		}

		#region Private test utils

		private static IEnumerable<T> Seq<T>(params T[] args)
		{
			return args;
		}

		#endregion
	}
}
