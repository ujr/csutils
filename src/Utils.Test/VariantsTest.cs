using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class VariantsTest
	{
		private readonly ITestOutputHelper _output;

		public VariantsTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Theory]
		[InlineData("foo|bar", "foo", "bar")]
		[InlineData("ba[r|z]", "bar", "baz")]
		[InlineData("qu[u]x", "qux", "quux")]
		[InlineData("foo|ba[r|z[aar]]|qu[u]x", "foo", "bar", "baz", "bazaar", "qux", "quux")]
		[InlineData("It[[em|alic]iz|erat]e[d], please",
		"Itemize, please",
		"Itemized, please",
		"Italicize, please",
		"Italicized, please",
		"Iterate, please",
		"Iterated, please")]
		[InlineData("a[1|2|3]b[4|5]c", "a1b4c", "a1b5c", "a2b4c", "a2b5c", "a3b4c", "a3b5c")]
		[InlineData("J[oh[ann]] W[olfgang] [v[on] ]Goethe", "J W Goethe",
		"J W v Goethe",
		"J W von Goethe",
		"J Wolfgang Goethe",
		"J Wolfgang v Goethe",
		"J Wolfgang von Goethe",
		"Joh W Goethe",
		"Joh W v Goethe",
		"Joh W von Goethe",
		"Joh Wolfgang Goethe",
		"Joh Wolfgang v Goethe",
		"Joh Wolfgang von Goethe",
		"Johann W Goethe",
		"Johann W v Goethe",
		"Johann W von Goethe",
		"Johann Wolfgang Goethe",
		"Johann Wolfgang v Goethe",
		"Johann Wolfgang von Goethe")]
		public void CanExpandBasics(string input, params string[] expected)
		{
			var actual = Variants.Expand(input).ToList();
			_output.WriteLine("{0} => {1}", input, string.Join(", ", actual));
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("[[[foo]]]", "foo")]
		[InlineData("[|||]")]
		[InlineData("[a|b|]", "a", "b")]
		public void CanOmitEmptyVariants(string input, params string[] expected)
		{
			var actual = Variants.Expand(input).ToList();
			_output.WriteLine("{0} => {1}", input, string.Join(", ", actual));
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("x[]x", "xx", "xx")]
		[InlineData("xx[", "xx", "xx")]
		[InlineData("x[|]x", "xx", "xx")]
		[InlineData("x[[]]x", "xx", "xx", "xx")]
		public void CanEmptyBrackets(string input, params string[] expected)
		{
			var actual = Variants.Expand(input).ToList();
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void CanIgnoreUnmatchedClosingBrackets()
		{
			var actual = Variants.Expand("f]o|o]").ToList();
			var expected = new string[]{"fo", "o"};
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void CanImplicitlyCloseUnmatchedOpeningBrackets()
		{
			var actual = Variants.Expand("[f[o[o").ToList();
			var expected = new string[]{"f", "fo", "foo"};
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void CanNullAndEmpty()
		{
			Assert.Empty(Variants.Expand(null));
			Assert.Empty(Variants.Expand(string.Empty));
		}
	}
}
