using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Sylphe.Cdb.Test
{
	public class CdbTest
	{
		[Fact]
		public void CanHash()
		{
			var empty = new byte[0];
			Assert.Equal(5381U, Cdb.Hash(empty));
			var key = Encoding.UTF8.GetBytes("abc");
			var hash = Cdb.Hash(key);
			Assert.Equal(193409669U, hash);
		}

		[Fact]
		public void CanCreateAndQuery()
		{
			var r1 = CreateRecord("key", "data");
			var r2 = CreateRecord("foo", "Bar");
			var r3 = CreateRecord("foo", "Quux"); // same key
			var r4 = CreateRecord("", ""); // empty key and data

			string filePath = Path.GetTempFileName(); // create empty file

			CreateCdb(filePath, r1, r2, r3, r4);

			var cdb = Cdb.Open(filePath);

			Assert.Equal(r1.Data, cdb.Find(r1.Key));

			cdb.FindStart();
			var d23 = new[]{cdb.FindNext(r2.Key), cdb.FindNext(r2.Key)};
			Assert.Null(cdb.FindNext(r2.Key));
			Assert.Contains(r2.Data, d23);
			Assert.Contains(r3.Data, d23);

			Assert.Equal(r4.Data, cdb.Find(r4.Key));

			Assert.Null(cdb.Find(Encoding.UTF8.GetBytes("NoSuchKey")));

			cdb.Close();

			File.Delete(filePath);
		}

		[Fact]
		public void CanFindAll()
		{
			int count = 10000;
			var array = GenerateRandomPairs(count).Distinct(new RecordKeyEquality()).ToArray();

			string filePath = Path.GetTempFileName(); // create empty file

			CreateCdb(filePath, array);

			using (var cdb = Cdb.Open(filePath))
			{
				foreach (var pair in array)
				{
					var data = cdb.Find(pair.Key);

					Assert.NotNull(data);
					Assert.Equal(pair.Data, data);
				}
			}
		}

		[Fact]
		public void CanCreateAndGet()
		{
			var r1 = CreateRecord("key", "data");
			var r2 = CreateRecord("foo", "Bar");
			var r3 = CreateRecord("foo", "Quux"); // same key
			var r4 = CreateRecord("", ""); // empty key and data

			string filePath = Path.GetTempFileName(); // create empty file

			CreateCdb(filePath, r1, r2, r3, r4);

			Assert.Equal(r1.Data, Cdb.Get(filePath, r1.Key));

			var list = new List<byte[]>();
			list.Add(Cdb.Get(filePath, r2.Key));
			list.Add(Cdb.Get(filePath, r2.Key, 1));
			Assert.Null(Cdb.Get(filePath, r2.Key, 2));
			Assert.Null(Cdb.Get(filePath, r2.Key, 99));
			Assert.Contains(r2.Data, list);
			Assert.Contains(r3.Data, list);

			Assert.Equal(r4.Data, Cdb.Get(filePath, r4.Key));

			Assert.Null(Cdb.Get(filePath, Encoding.UTF8.GetBytes("NoSuchKey")));

			File.Delete(filePath);
		}

		[Fact]
		public void CanCreateAndDump()
		{
			int count = 5;//100;
			var array = GenerateRandomPairs(count).ToArray();

			string filePath = Path.GetTempFileName(); // create empty file

			CreateCdb(filePath, array);

			var records = Cdb.Dump(filePath);

			Assert.Equal(array.Select(RecordToString).OrderBy(s => s),
						 records.Select(RecordToString).OrderBy(s => s));

			File.Delete(filePath);
		}

		[Fact]
		public void CanMakeFromText()
		{
			const string text = "+3,4:key->data\n"
			+ "+3,3:foo->bar\n"
			+ "+3,4:foo->Quux\n"
			+ "+0,0:->\n";

			var reader = new StringReader(text);
			string filePath = Path.GetTempFileName(); // create empty file

			Cdb.Make(reader, filePath);

			var records = Cdb.Dump(filePath).ToList();

			var comparer = new RecordKeyEquality();
			Assert.Contains(CreateRecord("key", "data"), records, comparer);
			Assert.Contains(CreateRecord("foo", "Bar"), records, comparer);
			Assert.Contains(CreateRecord("foo", "Quux"), records, comparer);
			Assert.Contains(CreateRecord("", ""), records, comparer);

			File.Delete(filePath);
		}

		#region Test utilities

		private static string RecordToString(Cdb.Record record)
		{
			var s = Encoding.UTF8.GetString(record.Key);
			var t = Encoding.UTF8.GetString(record.Data);
			return string.Concat(s, ":", t);
		}

		private static IEnumerable<Cdb.Record> GenerateRandomPairs(int count)
		{
			for (int i = 0; i < count; i++)
			{
				// random strings for key and value
				var key = Path.GetRandomFileName().Replace(".", "");
				var data = Path.GetRandomFileName().Replace(".", "");
				yield return CreateRecord(key, data);
			}
		}

		private static Cdb.Record CreateRecord(string key, string data)
		{
			var k = Encoding.UTF8.GetBytes(key);
			var d = Encoding.UTF8.GetBytes(data);
			return new Cdb.Record(k, d);
		}

		private static void CreateCdb(string cdbFilePath, params Cdb.Record[] records)
		{
			Cdb.Make(records, cdbFilePath);
		}

		private class RecordKeyEquality : IEqualityComparer<Cdb.Record>
		{
			public bool Equals(Cdb.Record x, Cdb.Record y)
			{
				if (x == null && y == null) return true;
				if (x == null || y == null) return false;
				if (ReferenceEquals(x, y)) return true;
				if (x.Key == null && y.Key == null) return true;
				if (x.Key == null || y.Key == null) return false;
				if (ReferenceEquals(x.Key, y.Key)) return true;
				if (x.Key.Length != y.Key.Length) return false;
				for (int i = 0; i < x.Key.Length; i++)
					if (x.Key[i] != y.Key[i]) return false;
				return true;
			}

			public int GetHashCode(Cdb.Record record)
			{
				return record.Key.GetHashCode();
			}
		}

		#endregion
	}
}
