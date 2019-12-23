/* Ported from http://cr.yp.to/cdb.html, public domain. */

using System;
using System.IO;

namespace Sylphe.Cdb
{
	public class CdbFile : IDisposable
	{
		private readonly object _syncLock = new object();

		private Stream _cdbFile;

		// The first 2048 bytes of a CDB file are an array
		// of 256 subtable heads, which are (pos,len) pairs.
		// Bernstein's Unix implementation mmap()s this array,
		// but here we read it into this array.
		private readonly UInt32[] _heads;

		private UInt32 _khash; // hash value of current key
		private UInt32 _hslots; // #slots in hashtab for current key
		private UInt32 _hpos; // byte offset of hashtab for current key
		private UInt32 _kpos; // byte offset of current key in slot
		private int _loop; // num of hash slots searched under current key

		/// <summary>
		/// Open a constant database from the given file.
		/// </summary>
		/// <param name="filePath">Path to the CDB file.</param>
		public CdbFile(string filePath)
		{
			_cdbFile = File.OpenRead(filePath);

			_heads = new UInt32[256*2];

			var bytes = new byte[2048];
			CdbRead(bytes, bytes.Length);

			for (int i = 0, offset = 0; i < 256; i++)
			{
				UInt32 pos = Cdb.UnpackInt32(bytes, offset);
				offset += 4;
				UInt32 len = Cdb.UnpackInt32(bytes, offset);
				offset += 4;

				_heads[i << 1] = pos;
				_heads[(i << 1) + 1] = len;
			}

			_loop = 0;
		}

		public long Length
		{
			get { return _cdbFile == null ? 0 : _cdbFile.Length; }
		}

		public void Close()
		{
			if (_cdbFile == null)
				throw new InvalidOperationException("Already closed");

			_cdbFile.Close();
			_cdbFile = null;
		}

		public void Dispose()
		{
			if (_cdbFile != null)
			{
				Close();
			}
		}

		/// <summary>
		/// Find the first record stored under the given <paramref name="key"/>.
		/// </summary>
		/// <returns>The data stored under the given key,
		/// or <c>null</c> if there is no such key.</returns>
		public byte[] Find(byte[] key)
		{
			lock (_syncLock)
			{
				FindStart();
				return FindNext(key);
			}
		}

		/// <summary>
		/// Prepare for subsequent calls to <see cref="FindNext"/>.
		/// </summary>
		public void FindStart()
		{
			lock (_syncLock)
			{
				_loop = 0;
			}
		}

		/// <summary>
		/// Find the next record stored under the given <paramref name="key"/>.
		/// </summary>
		/// <returns>The next record stored under the given key,
		/// or <c>null</c> if no more records can be found.</returns>
		public byte[] FindNext(byte[] key)
		{
			// We change object state in here, so nobody must intervene!
			lock (_syncLock)
			{
				if (_cdbFile == null)
				{
					throw new ObjectDisposedException(GetType().Name);
				}

				// Initialize: locate hash table and entry in it:
				if (_loop == 0)
				{
					UInt32 u = Cdb.Hash(key);

					// Get hash table len&pos for this key:
					UInt32 slot = u & 255;
					_hslots = _heads[(slot << 1) + 1];
					if (_hslots == 0) return null;
					_hpos = _heads[slot << 1];

					// Remember this key's hash:
					_khash = u;

					// Locate the slot in the table at _hpos:
					u >>= 8;
					u %= _hslots;
					u <<= 3;
					_kpos = _hpos + u;
				}

				// Search: iterate all hash slots for the given key:
				var bytes = new byte[8];
				while (_loop < _hslots)
				{
					// Read the entry for this key from the hash slot
					CdbRead(bytes, 8, _kpos);
					UInt32 h = Cdb.UnpackInt32(bytes, 0);
					UInt32 pos = Cdb.UnpackInt32(bytes, 4);
					if (pos == 0) break;

					// Advance loop counter and key position.
					// Wrap key position if at end of hash table.
					_loop += 1;
					_kpos += 8;
					if (_kpos == (_hpos + (_hslots << 3)))
					{
						_kpos = _hpos;
					}

					// Different hash? Probe next slot...
					if (h != _khash) continue;

					// Jump to the record and see if key matches:
					CdbRead(bytes, 8, pos);
					UInt32 klen = Cdb.UnpackInt32(bytes, 0);
					if (klen != key.Length) continue;
					if (!CdbMatch(key)) continue;
					UInt32 dlen = Cdb.UnpackInt32(bytes, 4);

					// Keys match: fetch the data
					var data = new byte[dlen];
					CdbRead(data, data.Length);
					return data;
				}

				return null; // No such key
			}
		}

		#region Utilities

		private void CdbRead(byte[] buffer, int count)
		{
			if (count < _cdbFile.Read(buffer, 0, count))
			{
				throw new FormatException("Invalid CDB file format");
			}
		}

		private void CdbRead(byte[] buffer, int count, long position)
		{
			_cdbFile.Seek(position, SeekOrigin.Begin);

			CdbRead(buffer, count);
		}

		private bool CdbMatch(byte[] bytes)
		{
			int count = bytes.Length;

			var other = new byte[count];

			CdbRead(other, count);

			for (int i = 0; i < count; i++)
			{
				if (bytes[i] != other[i])
				{
					return false;
				}
			}

			return true;
		}

		#endregion
	}
}
