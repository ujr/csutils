using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sylphe.Utils
{
	public readonly struct Tags : IEnumerable<string>, IEquatable<Tags>
	{
		private const string Separator = ",";   // used when joining
		private const string Separators = ",;"; // used when splitting

		private readonly string _value;
		private string Value => _value ?? string.Empty;

		public Tags(string tags)
		{
			_value = tags ?? string.Empty;
		}

		public Tags(Tags tags) : this(tags.Value) { }

		public Tags(params string[] tags) : this(JoinTags(tags)) { }

		public static implicit operator string(Tags tags) => string.IsNullOrWhiteSpace(tags.Value) ? null : tags.Value;
		public static explicit operator Tags(string s) => new Tags(s);

		public Tags AddTag(string tag)
		{
			return new Tags(AddTag(Value, tag));
		}

		public Tags AddTags(params string[] wanted)
		{
			return new Tags(AddTags(Value, wanted));
		}

		public bool HasTag(string tag)
		{
			return HasTag(Value, tag);
		}

		public bool HasTags(params string[] wanted)
		{
			return HasTags(Value, wanted);
		}

		public bool Equals(Tags other)
		{
			return SameTags(Value, other.Value);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Tags other)) return false;
			return SameTags(Value, other.Value);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<string> GetEnumerator()
		{
			return SplitTags(Value).GetEnumerator();
		}

		public override string ToString()
		{
			return Value;
		}

		#region Static utils

		public static string AddTag(string tags, string tag)
		{
			if (string.IsNullOrEmpty(tag)) return tags;
			if (string.IsNullOrEmpty(tags)) return tag;
			if (HasTag(tags, tag)) return tags;
			return string.Concat(tags, Separator, tag);
		}

		public static string AddTags(string tags, params string[] wanted)
		{
			if (wanted == null || wanted.Length < 1)
				return tags;
			if (wanted.Length == 1)
				return AddTag(tags, wanted[0]);
			if (string.IsNullOrEmpty(tags))
				return string.Join(Separator, wanted);
			var existing = new HashSet<string>(SplitTags(tags));
			var extension = string.Join(Separator, wanted.Where(t => ! existing.Contains(t)));
			return extension.Length > 0 ? string.Concat(tags, Separator, extension) : tags;
		}

		public static bool HasTag(string tags, string tag)
		{
			if (tags == null) return false;
			if (string.IsNullOrEmpty(tag)) return false;

			int index = tags.IndexOf(tag, StringComparison.Ordinal);
			if (index < 0) return false;

			// There must be no alphanumeric at either end of the matched string!
			if (index > 0 && char.IsLetterOrDigit(tags, index - 1)) return false;
			int limit = index + tag.Length;
			if (limit < tags.Length && char.IsLetterOrDigit(tags, limit)) return false;

			return true;
		}

		public static bool HasTags(string tags, params string[] wanted)
		{
			if (tags == null) return false;
			if (wanted == null) return false;

			return wanted.All(tag => HasTag(tags, tag));
		}

		public static bool SameTags(string tagsA, string tagsB)
		{
			if (tagsA == null && tagsB == null) return true;
			if (tagsA == null || tagsB == null) return true;
			var setA = new HashSet<string>(SplitTags(tagsA));
			return setA.SetEquals(SplitTags(tagsB));
		}

		public static IEnumerable<string> SplitTags(string tags)
		{
			if (string.IsNullOrWhiteSpace(tags)) return Array.Empty<string>();
			var array = tags.Split(Separators.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			return array.Select(t => t.Trim()).Where(t => t.Length > 0);
		}

		public static string JoinTags(IEnumerable<string> tags)
		{
			return string.Join(Separator, tags.Select(t => t?.Trim()).Where(t => !string.IsNullOrEmpty(t)).Distinct());
		}

		#endregion
	}
}
