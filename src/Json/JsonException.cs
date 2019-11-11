using System;
using System.Runtime.Serialization;

namespace Sylphe.Json
{
	[Serializable]
	public class JsonException : Exception
	{
		public JsonException() {}

		public JsonException(string message)
			: base(message) {}

		public JsonException(string message, Exception inner)
			: base(message, inner) {}

		protected JsonException(SerializationInfo info, StreamingContext context)
			: base(info, context) {}
	}
}
