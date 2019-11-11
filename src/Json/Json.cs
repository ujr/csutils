using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sylphe.Json
{
	/// <summary>
	/// Minimalistic JSON serialization and hydratisation (de-serialization).
	/// Serialization makes use of reflection, hydratisation uses dynamics.
	/// This is work in progress; for production, use a full-fledged library.
	/// </summary>
	public static class Json
	{
		public static string Serialize<T>(T instance)
		{
			var buffer = new StringBuilder();

			using (var writer = new JsonWriter(new StringWriter(buffer)))
			{
				Serialize(instance, writer);
			}

			return buffer.ToString();
		}

		public static dynamic Hydrate(string json)
		{
			var reader = new JsonReader(json);

			if (!reader.Read())
			{
				throw new JsonException("No JSON input");
			}

			dynamic result = HydrateValue(reader);

			if (reader.Read())
			{
				throw new JsonException("Spurious text after JSON value");
			}

			return result;
		}

		public static T Hydrate<T>(string json)
		{
			throw new NotImplementedException();
		}

		public static object Hydrate(string json, Type type)
		{
			throw new NotImplementedException();
		}

		public static object Undefined { get; } = new JsonUndefined();

		public static bool PropagateUndefined { get; set; }

		#region Serialization

		private static void Serialize(object o, JsonWriter writer)
		{
			if (o == null)
			{
				writer.WriteNull();
				return;
			}

			if (o is bool)
			{
				writer.WriteValue((bool) o);
				return;
			}

			if (o is double || o is float)
			{
				// do not cast: can only unbox to orig type
				writer.WriteValue(Convert.ToDouble(o));
				return;
			}

			if (o is int || o is uint || o is short || o is ushort || o is long || o is byte || o is sbyte)
			{
				// do not cast: (long) o would fail if o is int (unboxing only to orig type)
				// ulong could overflow long - handled below via decimal
				writer.WriteValue(Convert.ToInt64(o));
				return;
			}

			if (o is ulong || o is decimal)
			{
				writer.WriteValue(Convert.ToDecimal(o));
				return;
			}

			if (o is DateTime)
			{
				var dt = (DateTime) o;
				writer.WriteValue(dt.ToString("o")); // ISO 8601
				return;
			}

			if (o is string)
			{
				writer.WriteValue((string) o);
				return;
			}

			if (o is JsonArray)
			{
				writer.WriteStartArray();
				foreach (var item in (JsonArray) o)
				{
					Serialize(item, writer);
				}
				writer.WriteEndArray();
				return;
			}

			if (o is JsonObject)
			{
				writer.WriteStartObject();
				foreach (var pair in (JsonObject) o)
				{
					writer.WritePropertyName(pair.Key);
					Serialize(pair.Value, writer);
				}
				writer.WriteEndObject();
				return;
			}

			if (o is Array)
			{
				var a = (Array) o;
				writer.WriteStartArray();
				for (int i = 0; i < a.Length; i++)
				{
					object item = a.GetValue(i);
					Serialize(item, writer);
				}
				writer.WriteEndArray();
				return;
			}

			if (IsUnboundGenericType(typeof(List<>), o))
			{
				// System.Collections.Generic.List<> implements non-generic IEnumerable:
				writer.WriteStartArray();
				foreach (var item in (IEnumerable) o)
				{
					Serialize(item, writer);
				}
				writer.WriteEndArray();
				return;
			}

			if (IsUnboundGenericType(typeof(Dictionary<,>), o))
			{
				writer.WriteStartObject();

				// TODO review - at least cache the GetMethod() result

				Type[] genericArguments = o.GetType().GetGenericArguments();
				Type valueType = genericArguments[1];

				var methodInfo = typeof(Json).GetMethod("SerializeObjectItems", BindingFlags.NonPublic | BindingFlags.Static);
				methodInfo.MakeGenericMethod(valueType).Invoke(null, new[] {o, writer});

				writer.WriteEndObject();
				return;
			}

			if (o is IDynamicMetaObjectProvider)
			{
				// Note: o.GetType().GetProperties() yields no properties for dynamic objects!
				// Use dyn.GetMetaObject(Expression.Constant(dyn)).GetDynamicMemberNames())
				// where dyn is o as IDynamicMetaObjectProvider; but then how to get the values?
				throw new NotImplementedException("Serialization of dynamic types is not yet implemented");
			}

			// TODO need special stuff for anonymous types?
			// TODO code below will not work with dynamic types!

			var type = o.GetType();
			var anonymous = type.IsAnonymousType();
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance); // todo cached?
			writer.WriteStartObject();
			foreach (var property in properties)
			{
				if (property.CanRead && (property.CanWrite || anonymous))
				{
					object value = property.GetValue(o);
					writer.WritePropertyName(property.Name);
					Serialize(value, writer);
				}
			}
			writer.WriteEndObject();
		}

		// used implicitly (via reflection)
		private static void SerializeObjectItems<T>(IDictionary<string, T> dict, JsonWriter writer)
		{
			foreach (var pair in dict)
			{
				writer.WritePropertyName(Convert.ToString(pair.Key));
				Serialize(pair.Value, writer);
			}
		}

		#endregion

		#region Hydratization

		private static dynamic HydrateValue(JsonReader reader)
		{
			switch (reader.Type)
			{
				case JsonType.None:
					throw new JsonException("Unexpected end of JSON input");

				case JsonType.Null:
					return null;

				case JsonType.False:
					return false;

				case JsonType.True:
					return true;

				case JsonType.Number:
				case JsonType.String:
					return reader.Value;

				case JsonType.Array:
					return HydrateArray(reader);

				case JsonType.Object:
					return HydrateObject(reader);

				case JsonType.Closed:
					throw new JsonException("Invalid JSON input");

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static /*dynamic[]*/ JsonArray HydrateArray(JsonReader reader)
		{
			if (reader.Type != JsonType.Array)
				throw new ArgumentException($"unexpected {nameof(reader)}.Type");

			//var result = new List<dynamic>();
			var result = new JsonArray();

			while (reader.Read() && reader.Type != JsonType.Closed)
			{
				dynamic item = HydrateValue(reader);

				result.AddItem(item);
			}

			if (reader.Type != JsonType.Closed)
				throw new JsonException("Array not closed?");

			//return result.ToArray(); // make it a true array
			return result;
		}

		private static JsonObject HydrateObject(JsonReader reader)
		{
			if (reader.Type != JsonType.Object)
				throw new ArgumentException($"unexpected {nameof(reader)}.Type");

			var result = new JsonObject();

			while (reader.Read() && reader.Type != JsonType.Closed)
			{
				string name = reader.Label;
				dynamic item = HydrateValue(reader);

				result.AddItem(name, item);
			}

			if (reader.Type != JsonType.Closed)
				throw new JsonException("Object not closed?");

			return result;
		}

		#endregion

		#region Dynamic objects for JSON arrays and objects

		private class JsonUndefined : DynamicObject
		{
			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				if (PropagateUndefined)
				{
					result = this;
					return true;
				}

				throw new InvalidOperationException("Cannot index undefined");
			}

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				if (PropagateUndefined)
				{
					result = this;
					return true;
				}

				throw new InvalidOperationException($"Cannot get property {binder.Name} of undefined");
			}

			public override string ToString()
			{
				return "Json.Undefined";
			}
		}

		private class JsonArray : DynamicObject, IList<object>
		{
			private readonly IList<dynamic> _items;

			internal JsonArray()
			{
				_items = new List<dynamic>();
			}

			internal void AddItem(object value)
			{
				_items.Add(value);
			}

			public int Count => _items.Count;

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				if (string.Equals(binder.Name, "Count") || string.Equals(binder.Name, "Length"))
				{
					result = _items.Count;
					return true;
				}

				result = Undefined;
				return true;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				int index = (int) indexes[0];
				if (0 <= index && index < _items.Count)
				{
					result = _items[index];
					return true;
				}

				result = Undefined;
				return true;
			}

			#region IEnumerable implementation

			public IEnumerator<dynamic> GetEnumerator()
			{
				return _items.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _items.GetEnumerator();
			}

			#endregion

			#region ICollection implementation

			public void CopyTo(dynamic[] array, int index)
			{
				_items.CopyTo(array, index);
			}

			#endregion

			#region IList implementation

			public bool IsReadOnly => true;

			public object this[int index]
			{
				get
				{
					if (0 <= index && index < _items.Count)
					{
						return _items[index];
					}

					return Undefined;
				}
				set { throw new NotSupportedException("Collection is read-only"); }
			}

			public void Add(dynamic value)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			public bool Contains(dynamic value)
			{
				return _items.Contains(value);
			}

			public void Clear()
			{
				throw new NotSupportedException("Collection is read-only");
			}

			public int IndexOf(dynamic value)
			{
				return _items.IndexOf(value);
			}

			public void Insert(int index, object dynamic)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			public bool Remove(dynamic value)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			public void RemoveAt(int index)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			#endregion
		}

		private class JsonObject : DynamicObject, IEnumerable<KeyValuePair<string, object>>
		{
			private readonly IDictionary<string, object> _items;

			internal JsonObject()
			{
				_items = new Dictionary<string, object>();
			}

			internal void AddItem(string name, object value)
			{
				_items.Add(name, value);
			}

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				object value;
				if (_items.TryGetValue(binder.Name, out value))
				{
					result = value;
					return true;
				}

				result = Undefined;
				return true;
			}

			#region IEnumerable implementation

			public IEnumerator<KeyValuePair<string, dynamic>> GetEnumerator()
			{
				return _items.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _items.GetEnumerator();
			}

			#endregion
		}

		#endregion

		#region Reflection utils

		public static bool IsAnonymousType(this Type type)
		{
			// https://stackoverflow.com/questions/1650681/determining-whether-a-type-is-an-anonymous-type
			// It seems there are only heuristics. Here's one:
			bool isCompilerGenerated = Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false);
			return isCompilerGenerated && type.Name.Contains("__AnonymousType");
		}

		/// <summary>
		/// Test if an object is of an unbound generic type, for example:
		/// <code>
		/// var ex = new WebFaultException&lt;string&gt;("oops", HttpStatusCode.BadRequest);
		/// bool b = IsUnboundGenericType(typeof(WebFaultException&lt;&gt;), ex); // true
		/// </code>
		/// </summary>
		/// <remarks>
		/// This presently fails for closed generic types:
		/// <code>
		/// IsGenericType(typeof(WebFaultException&lt;string&gt;), new WebFaultException&lt;string&gt;("oops", HttpStatusCode.BadRequest))
		/// </code>
		/// would be false with our logic, but is detected and throws an exception.
		/// </remarks>
		public static bool IsUnboundGenericType(Type genericType, object instance)
		{
			var actualType = instance?.GetType();

			if (genericType.IsGenericType && !genericType.IsGenericTypeDefinition)
			{
				throw new NotImplementedException("Closed generic type not implemented; use 'obj is Foo<Bar>' instead");
			}

			while (actualType != null)
			{
				var current = actualType.IsGenericType ? actualType.GetGenericTypeDefinition() : actualType;

				if (current == genericType)
				{
					return true;
				}

				actualType = actualType.BaseType; // walk up the inheritance tree
			}

			return false;
		}

		#endregion
	}
}
