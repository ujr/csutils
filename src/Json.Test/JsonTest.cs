using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Json.Test
{
	public class JsonTest
	{
		private readonly ITestOutputHelper _output;

		public JsonTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void CanSerializePrimitives()
		{
			Assert.Equal("null", Json.Serialize<object>(null));
			Assert.Equal("false", Json.Serialize(false));
			Assert.Equal("true", Json.Serialize(true));
			Assert.Equal("123", Json.Serialize(123));
			Assert.Equal("-2147483648", Json.Serialize(int.MinValue));
			Assert.StartsWith("-3.14159265", Json.Serialize(-Math.PI));
			Assert.Equal("\"Hello, world!\"", Json.Serialize("Hello, world!"));
			Assert.Equal(@"""/\\\""\b\f\n\r\t\u0000/""", Json.Serialize("/\\\"\b\f\n\r\t\0/"));
			Assert.Equal("\"\"", Json.Serialize(string.Empty));
		}

		[Fact]
		public void CanSerializeArrays()
		{
			Assert.Equal("[]", Json.Serialize(new int[0]));
			Assert.Equal("[1,2,-3]", Json.Serialize(new[] {1, 2.0, -3}));
			Assert.Equal(@"[2.7,""Hi"",false]", Json.Serialize(new object[] {2.7, "Hi", false}));
			Assert.Equal(@"[[[""deep""]]]", Json.Serialize(new object[] {new object[] {new object[] {"deep"}}}));
		}

		[Fact]
		public void CanSerializeObjects()
		{
			Assert.Equal("{}", Json.Serialize(new {}));
			Assert.Equal(@"{""Foo"":1,""Bar"":2,""Baz"":3}", Json.Serialize(new {Foo = 1, Bar = 2, Baz = 3}));
			Assert.Equal(@"{""Foo"":{""Bar"":{""Baz"":""Quux""}}}", Json.Serialize(new {Foo = new {Bar = new {Baz = "Quux"}}}));
		}

		private class TestRoot
		{
			public string Foo { get; set; }
			public TestSub Sub { get; set; }
		}

		private class TestSub
		{
			public string Bar { get; set; }
			public TestPair[] Array { get; set; }
		}

		private class TestPair
		{
			public string Key { get; set; }
			public int Count { get; set; }
		}

		[Fact]
		public void CanSerializeCompound()
		{
			dynamic test1 = new {Foo = "Foo", Bar = new[] {1, 2, -3}, Nested = new {Baz = "Quux", Nix = (string) null}};
			const string expected1 = @"{""Foo"":""Foo"",""Bar"":[1,2,-3],""Nested"":{""Baz"":""Quux"",""Nix"":null}}";
			Assert.Equal(expected1, Json.Serialize(test1));

			var test2 = new TestRoot
			{
				Foo = "foo",
				Sub = new TestSub
				{
					Bar = "bar",
					Array = new[] {new TestPair {Key = "baz", Count = 42}}
				}
			};

			string expected2 = @"{'Foo':'foo','Sub':{'Bar':'bar','Array':[{'Key':'baz','Count':42}]}}".Replace('\'', '"');
			Assert.Equal(expected2, Json.Serialize(test2));
		}

		private class TypeWithCollectionProperties
		{
			public string[] Array { get; set; }

			public List<TestPair> List { get; set; }

			public Dictionary<string, TestPair> Dictionary { get; set; }
		}

		[Fact]
		public void CanSerializeCollectionProperties()
		{
			// The only collection types supported are: Array, List, Dictionary
			// (we need the exact types, not the interfaces IList or IDictionary).

			var root = new TypeWithCollectionProperties();
			root.Array = new[] {"foo", "quux"};
			root.List = root.Array.Select(s => new TestPair {Key = s, Count = s.Length}).ToList();
			root.Dictionary = root.List.ToDictionary(e => e.Key);
			Assert.True(root.Array.All(s => root.Dictionary.ContainsKey(s)));

			string json = Json.Serialize(root);
			const string expected =
				@"{""Array"":[""foo"",""quux""]," +
				@"""List"":[{""Key"":""foo"",""Count"":3},{""Key"":""quux"",""Count"":4}]," +
				@"""Dictionary"":{""foo"":{""Key"":""foo"",""Count"":3},""quux"":{""Key"":""quux"",""Count"":4}}}";
			Assert.Equal(expected, json);
		}

		[Fact]
		public void CanHydratePrimitives()
		{
			dynamic d1 = Json.Hydrate("null");
			Assert.Null(d1);
			dynamic d2 = Json.Hydrate("true");
			Assert.True(d2);
			dynamic d3 = Json.Hydrate("-123");
			Assert.Equal(-123, d3);
			dynamic d4 = Json.Hydrate("3.1415927");
			Assert.Equal(3.1415927, d4);
			dynamic d5 = Json.Hydrate("\"Hello\\nworld!\"");
			Assert.Equal("Hello\nworld!", d5);
		}

		[Fact]
		public void CanHydrateCompounds()
		{
			dynamic d1 = Json.Hydrate("[]");
			Assert.NotNull(d1);
			Assert.IsAssignableFrom<IDynamicMetaObjectProvider>(d1);
			Assert.Equal(0, d1.Length);

			dynamic d2 = Json.Hydrate("[42,\"foo\",null,[true]]");
			Assert.Equal(42, d2[0]);
			Assert.Equal("foo", d2[1]);
			Assert.Equal(true, d2[3][0]);

			dynamic d3 = Json.Hydrate("{}");
			Assert.NotNull(d3);
			Assert.IsAssignableFrom<IDynamicMetaObjectProvider>(d3);

			dynamic d5 = Json.Hydrate(@"{""Foo"":""Foo"",""Bar"":[1,{""Two"":2}],""Nested"":{""Baz"":""Quux"",""Nix"":null}}");
			Assert.Equal("Foo", d5.Foo);
			Assert.Equal(2, d5.Bar.Length);
			Assert.Equal(1, d5.Bar[0]);
			Assert.Equal(2, d5.Bar[1].Two);
			Assert.Equal("Quux", d5.Nested.Baz);
			Assert.Null(d5.Nested.Nix);
		}

		[Fact]
		public void CanEnumerateHydratedArrays()
		{
			const string json = @"[""one"", ""two"", ""three""]";
			var array = Json.Hydrate(json);

			var list = new List<string>();

			foreach (var item in array)
			{
				list.Add(item);

				_output.WriteLine(Json.Serialize(item));
			}

			Assert.Equal("one", list[0]);
			Assert.Equal("two", list[1]);
			Assert.Equal("three", list[2]);
		}

		[Fact]
		public void CanEnumerateHydratedObjects()
		{
			const string json = @"{""one"":""One"",""two"":2,""more"":[3,""infty""]}";
			dynamic hydrated = Json.Hydrate(json);

			var items = new List<KeyValuePair<string, dynamic>>();

			foreach (KeyValuePair<string, dynamic> item in hydrated)
			{
				items.Add(item);

				_output.WriteLine("{0}: {1}", item.Key, Json.Serialize(item.Value));
			}

			Assert.Equal("one", items[0].Key);
			Assert.Equal("One", items[0].Value);

			Assert.Equal("two", items[1].Key);
			Assert.Equal(2, items[1].Value);

			Assert.Equal("more", items[2].Key);
			Assert.Equal(3, items[2].Value[0]);
			Assert.Equal("infty", items[2].Value[1]);
		}

		[Fact]
		public void CanUndefinedPropertyAndIndex()
		{
			const string json = @"{""Foo"":""Foo"",""Array"":[""exists""]}";

			dynamic dyn = Json.Hydrate(json);

			Assert.Equal("Foo", dyn.Foo); // exists
			Assert.True(ReferenceEquals(Json.Undefined, dyn.Bar)); // no such property

			Assert.Equal("exists", dyn.Array[0]); // exists
			Assert.True(ReferenceEquals(Json.Undefined, dyn.Array[9])); // no such index
		}

		[Fact]
		public void CanUndefined()
		{
			dynamic undef = Json.Undefined;

			var ex1 = Assert.Throws<InvalidOperationException>(() => _output.WriteLine(undef.Quux));
			_output.WriteLine(@"Expected: {0}", ex1.Message);

			var ex2 = Assert.Throws<InvalidOperationException>(() => _output.WriteLine(undef[123]));
			_output.WriteLine(@"Expected: {0}", ex2.Message);

			Assert.Equal("Json.Undefined", Json.Undefined.ToString());
		}

		[Fact]
		public void CanPropagateUndefined()
		{
			Assert.False(Json.PropagateUndefined, "Expect default to be false");

			const string json = @"{""Foo"":""Foo"",""Array"":[""exists""]}";

			dynamic dyn = Json.Hydrate(json);

			Json.PropagateUndefined = true;

			Assert.True(ReferenceEquals(Json.Undefined, dyn.NoSuchProperty));
			Assert.True(ReferenceEquals(Json.Undefined, dyn.No.Such.Property));
			Assert.True(ReferenceEquals(Json.Undefined, dyn.Oops[0][1][2]));

			Json.PropagateUndefined = false;

			Assert.True(ReferenceEquals(Json.Undefined, dyn.NoSuchProperty));
			Assert.Throws<InvalidOperationException>(() => _output.WriteLine(dyn.No.Such.Property));
			Assert.Throws<InvalidOperationException>(() => _output.WriteLine(dyn.Oops[0][1][2]));
		}

		[Fact]
		public void CanHydrateHeavilyNestedStuff()
		{
			const string json = @"{""foo"":{""bar"":{""baz"":[[[""quux""]]]}}}";
			var obj = Json.Hydrate(json);

			Assert.Equal("quux", obj.foo.bar.baz[0][0][0]);
		}

		[Fact]
		public void CanRecognizeInvalidJson()
		{
			var ex1 = Assert.Throws<JsonException>(() => Json.Hydrate(string.Empty));
			_output.WriteLine(@"Expected: {0}", ex1.Message);

			var ex2 = Assert.Throws<JsonException>(() => Json.Hydrate("{\"foo\":"));
			_output.WriteLine(@"Expected: {0}", ex2.Message);

			var ex3 = Assert.Throws<JsonException>(() => Json.Hydrate("{}x"));
			_output.WriteLine(@"Expected: {0}", ex3.Message);

			var ex4 = Assert.Throws<JsonException>(() => Json.Hydrate("False"));
			_output.WriteLine(@"Expected: {0}", ex4.Message);
		}

		[Fact(Skip="Serialization of dynamic objects not yet implemented")]
		public void CanSerializeDynamics()
		{
			dynamic dyn1 = new ExpandoObject();
			dyn1.Foo = "foo";
			dyn1.Nested = new ExpandoObject();
			dyn1.Nested.Bar = "bar";
			dyn1.Nested.Baz = 123;
			dyn1.Array = new dynamic[] {"Quux"};
			string str1 = Json.Serialize(dyn1);
			Assert.Equal(@"{""Foo"":""foo"",""Nested"":{""Bar"":""bar"",""Baz"":123},""Array"":[""Quux""]}", str1);
		}
	}
}
