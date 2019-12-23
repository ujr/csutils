/* Ported from http://cr.yp.to/cdb.html, public domain. */

using System;
using System.Collections.Generic;
using System.IO;

namespace Sylphe.Cdb
{
	public sealed class CdbMake : IDisposable
	{
		private Stream _file; // the CDB file to be (must be seekable)
		private UInt32 _pos; // current key's byte offset into the CDB file
		private readonly IList<HashPos> _hashInfo; // hash and offset of records
		private readonly UInt32[] _hashSize; // number of slots in each hash table

		/// <summary>
		/// Start building a constant database into the given file.
		/// Call <see cref="Add"/> any number of times before calling
		/// <see cref="Close"/> (or <see cref="Dispose"/>).
		/// </summary>
		/// <param name="cdbFilePath">The CDB file to create.</param>
		public CdbMake(string cdbFilePath)
		{
			_hashInfo = new List<HashPos>();
			_hashSize = new UInt32[256];

			// Initially, each of the 256 hash tables is empty:
			for (int i = 0; i < 256; i++)
			{
				_hashSize[i] = 0;
			}

			// Truncate the file, if it exists, create it, if not:
			_file = new FileStream(cdbFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
			_file.SetLength(0); // Truncate! (in case the file already existed)

			// Seek to end of fixed-size hash table:
			_pos = 2048;
			_file.Seek(_pos, SeekOrigin.Begin);
		}

		/// <summary>
		/// Add a key/data pair to the database.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="data">The data.</param>
		public void Add(byte[] key, byte[] data)
		{
			if (_file == null)
				throw new ObjectDisposedException(GetType().Name);

			var bytes = new byte[8];
			Cdb.PackInt32((UInt32) key.Length, bytes, 0);
			Cdb.PackInt32((UInt32) data.Length, bytes, 4);

			// Write the record: keylen, datalen, key, data.
			_file.Write(bytes, 0, bytes.Length);
			_file.Write(key, 0, key.Length);
			_file.Write(data, 0, data.Length);

			// Add hash and offset to the in-memory list:
			UInt32 hash = Cdb.Hash(key);
			_hashInfo.Add(new HashPos(hash, _pos));
			_hashSize[hash & 255] += 1; // one more entry

			// Update the file position pointer:
			AdvancePos(8); // key and data length
			AdvancePos((UInt32) key.Length);
			AdvancePos((UInt32) data.Length);
		}

		/// <summary>
		/// Finish creation of this CDB and close the file.
		/// </summary>
		public void Close()
		{
			Finish();

			_file.Close();
			_file = null;
		}

		public void Dispose()
		{
			if (_file != null)
			{
				Close();
			}
		}

		private void Finish()
		{
			// Find the start of each hash table
			var tableStart = new UInt32[256];
			UInt32 start = 0;
			for (int i = 0; i < 256; i++)
			{
				start += _hashSize[i];
				tableStart[i] = start;
			}

			// Create a new hash info table in order by hash table
			var table = new HashPos[_hashInfo.Count];
			foreach (var hp in _hashInfo)
			{
				table[--tableStart[hp.Hash & 255]] = hp;
			}

			var eightBytes = new byte[8];
			var fixedTable = new byte[2048];

			// Append each of the hash tables to the end of the file.
			// Along the way, build the fixed table (to be written later).
			for (int i = 0; i < 256; i++)
			{
				UInt32 len = _hashSize[i]*2; // len of i-th hash table

				// Remember pos and len of i-th hash table in fixed table:
				Cdb.PackInt32(_pos, fixedTable, i*8);
				Cdb.PackInt32(len, fixedTable, i*8 + 4);

				// Build the i-th hash table:
				UInt32 tableIndex = tableStart[i];
				var hashTable = new HashPos[len];
				for (uint u = 0; u < _hashSize[i]; u++)
				{
					HashPos hp = table[tableIndex++];

					// Locate a free entry in the hash table:
					UInt32 where = (hp.Hash >> 8)%len;
					while (hashTable[where].Pos != 0)
					{
						if (++where == len) where = 0; // wrap around
					}

					hashTable[where] = hp; // and store the (hash,pos) pair
				}

				// Append to the end of the CDB file:
				for (uint u = 0; u < len; u++)
				{
					Cdb.PackInt32(hashTable[u].Hash, eightBytes, 0);
					Cdb.PackInt32(hashTable[u].Pos, eightBytes, 4);

					_file.Write(eightBytes, 0, 8);
					AdvancePos(8);
				}
			}

			// Rewind to the start of the file and write the fixed-size table:
			_file.Seek(0, SeekOrigin.Begin);
			_file.Write(fixedTable, 0, fixedTable.Length);
		}

		/// <summary>
		/// Advance the file pointer by the given count.
		/// Throw an exception if the pointer grows beyond 4GB.
		/// </summary>
		/// <param name="count">The byte count.</param>
		private void AdvancePos(UInt32 count)
		{
			UInt32 newpos = _pos + count;

			if (newpos < count)
			{
				throw new IOException("CDB file grows too big; limit is 4GB");
			}

			_pos = newpos;
		}

		private struct HashPos
		{
			public UInt32 Hash { get; }
			public UInt32 Pos { get; }

			public HashPos(UInt32 hash, UInt32 pos)
			{
				Hash = hash;
				Pos = pos;
			}
		}
	}
}
