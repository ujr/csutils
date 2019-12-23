/* Ported from http://cr.yp.to/cdb.html, public domain. */

using System;
using System.Collections.Generic;
using System.IO;

namespace Sylphe.Cdb
{
	/// <summary>
	/// Creating, querying, and dumping constant database (CDB) files.
	/// </summary>
	public static class Cdb
	{
		public static byte[] Get(string cdbFilePath, byte[] key, int skip = 0)
		{
			using (var cdb = new CdbFile(cdbFilePath))
			{
				cdb.FindStart();
				while (skip-- > 0 && cdb.FindNext(key) != null);
				return cdb.FindNext(key);
			}
		}

		public static CdbFile Open(string cdbFilePath)
		{
			return new CdbFile(cdbFilePath);
		}

		public static CdbMake Make(string cdbFilePath)
		{
			return new CdbMake(cdbFilePath);
		}

		/// <summary>
		/// Make a CDB file from the given input text file.
		/// </summary>
		/// <param name="inputFilePath">The input file path.</param>
		/// <param name="cdbFilePath">The target CDB file path.</param>
		public static void Make(string inputFilePath, string cdbFilePath)
		{
			using (TextReader input = File.OpenText(inputFilePath))
			{
				Make(input, cdbFilePath);
			}
		}

		/// <summary>
		/// Make a CDB file from the given input text reader.
		/// Each record in the input is formatted as follows:
		/// "+" klen "," dlen ":" key "->" data "\n"
		/// </summary>
		/// <param name="input">The input, in &quot;CDB Make Format&quot;.</param>
		/// <param name="cdbFilePath">The target CDB file path.</param>
		public static void Make(TextReader input, string cdbFilePath)
		{
			Make(ParseInput(input), cdbFilePath);
		}

		/// <summary>
		/// Make a CDB file from the given records.
		/// </summary>
		/// <param name="records"/>The key/data records to add.</param>
		/// <param name="cdbFilePath"/>The target CDB file path.</param>
		public static void Make(IEnumerable<Record> records, string cdbFilePath)
		{
			using (var maker = new CdbMake(cdbFilePath))
			{
				foreach (var record in records)
				{
					maker.Add(record.Key, record.Data);
				}
			}
		}

		/// <summary>
		/// Enumerate all records in the given CDB in an undefined order.
		/// </summary>
		/// <param name="cdbFilePath">Path to the CDB file.</param>
		public static IEnumerable<Record> Dump(string cdbFilePath)
		{
			Stream cdbFile = File.OpenRead(cdbFilePath);

			try
			{
				// Hint: do not simply return GetEntries(cdbFile) because
				// we must not close until after enumeration is complete!

				foreach (var record in Dump(cdbFile))
				{
					yield return record;
				}
			}
			finally
			{
				cdbFile.Close();
			}
		}

		/// <summary>
		/// Enumerate all records in the given CDB in an undefined order.
		/// The file stream must be readable but not necessarily seekable.
		/// </summary>
		/// <param name="cdbFilePath">The CDB file stream.</param>
		public static IEnumerable<Record> Dump(Stream cdbFile)
		{
			// Read the end-of-data value
			UInt32 eod = ReadInt32(cdbFile);

			// Skip the rest of the heads table
			var dummy = new byte[2048 - 4];
			ReadBytes(cdbFile, dummy, dummy.Length);

			UInt32 pos = 2048;

			while (pos < eod)
			{
				// Format is: key length, data length, key, data

				UInt32 klen = ReadInt32(cdbFile);
				pos += 4;

				UInt32 dlen = ReadInt32(cdbFile);
				pos += 4;

				var key = new byte[klen];
				ReadBytes(cdbFile, key, (int) klen);
				pos += klen;

				var data = new byte[dlen];
				ReadBytes(cdbFile, data, (int) dlen);
				pos += dlen;

				yield return new Cdb.Record(key, data);
			}
		}

		/// <summary>
		/// Compute and return the hash value for the given key.
		/// Used internally but public for your convenience.
		/// </summary>
		public static UInt32 Hash(byte[] key)
		{
			const UInt32 initialHash = 5381;

			UInt32 hash = initialHash;

			for (int i = 0; i < key.Length; i++)
			{
				hash += hash << 5;
				hash ^= key[i];
			}

			return hash;
		}

		public sealed class Record
		{
			public Record(byte[] key, byte[] data)
			{
				Key = key;
				Data = data;
			}

			public byte[] Key { get; }
			public byte[] Data { get; }
		}

		#region Private utilities

		private static IEnumerable<Record> ParseInput(TextReader input)
		{
			for (;;)
			{
				// Line format is: "+" klen "," dlen ":" key "->" data "\n"

				int ch = input.Read();
				if (ch < 0) break; // end of input
				if (IsLineEnd(ch, input)) break;
				if (ch != '+')
				{
					throw SyntaxError();
				}

				int klen = 0;
				for (;;)
				{
					ch = input.Read();
					if (ch == ',') break;
					if (ch < '0' || ch > '9')
						throw SyntaxError();
					if (klen > 429496720)
						throw SyntaxError("key length is too big");
					klen = klen*10 + (ch - '0');
				}

				int dlen = 0;
				for (;;)
				{
					ch = input.Read();
					if (ch == ':') break;
					if (ch < '0' || ch > '9')
						throw SyntaxError();
					if (dlen > 429496720)
						throw SyntaxError("data length is too big");
					dlen = dlen*10 + (ch - '0');
				}

				var key = new byte[klen];
				for (int i = 0; i < klen; i++)
				{
					if ((ch = input.Read()) == -1)
					{
						throw SyntaxError("unexpected end-of-file");
					}

					key[i] = (byte) (ch & 255);
				}

				if (input.Read() != '-')
				{
					throw SyntaxError();
				}

				if (input.Read() != '>')
				{
					throw SyntaxError();
				}

				var data = new byte[dlen];
				for (int i = 0; i < dlen; i++)
				{
					if ((ch = input.Read()) == -1)
					{
						throw SyntaxError("input file is truncated");
					}

					data[i] = (byte) (ch & 255);
				}

				ch = input.Read();
				if (!IsLineEnd(ch, input))
				{
					throw SyntaxError();
				}

				yield return new Record(key, data);
			}
		}

		private static bool IsLineEnd(int ch, TextReader input)
		{
			if (ch == '\n')
			{
				return true; // Unix line end: LF
			}

			if (ch == '\r' && input.Peek() == '\n')
			{
				input.Read(); // consume the LF
				return true; // Windows line end: CRLF
			}

			return false;
		}

		private static FormatException SyntaxError(string message = null)
		{
			const string prefix = "Invalid CDB make input format";

			message = message == null
				? prefix
				: string.Concat(prefix, ": ", message);

			return new FormatException(message);
		}

		/// <remarks>CDB uses Little Endian</remarks>
		public static void PackInt32(UInt32 value, byte[] bytes, int offset)
		{
			bytes[offset++] = (byte) (value & 255);
			value >>= 8;
			bytes[offset++] = (byte) (value & 255);
			value >>= 8;
			bytes[offset++] = (byte) (value & 255);
			value >>= 8;
			bytes[offset] = (byte) (value & 255);
		}

		/// <remarks>CDB uses Little Endian</remarks>
		public static UInt32 UnpackInt32(byte[] bytes, int offset)
		{
			UInt32 result;

			result = bytes[offset + 3];
			result <<= 8;
			result += bytes[offset + 2];
			result <<= 8;
			result += bytes[offset + 1];
			result <<= 8;
			result += bytes[offset];

			return result;
		}

		private static UInt32 ReadInt32(Stream stream)
		{
			var bytes = new byte[4];
			ReadBytes(stream, bytes, 4);
			return UnpackInt32(bytes, 0);
		}

		private static void ReadBytes(Stream file, byte[] buffer, int count)
		{
			if (file.Read(buffer, 0, count) != count)
			{
				throw new FormatException("Invalid CDB file format");
			}
		}

		#endregion
	}
}
