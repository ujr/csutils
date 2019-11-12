using System;
using System.Diagnostics;
using System.Linq;

namespace Sylphe.Utils
{
	/// <summary>
	/// A bit set of a fixed size, suitable for sparse data.
	/// Bits are identified by integers in 0..Length-1.
	/// Bit space is divided into chunks of 4096 bits,
	/// which is 64 words of 64 bits. Only words with at
	/// least one non-zero bit are stored in the chunk array.
	/// <para/>
	/// Min size is 1, max size is 2^31-1=2,147,483,647.
	/// </summary>
	/// <remarks>
	/// C# does arithmetic shift (ie, sign extension) on ints,
	/// which is fine because negative length/index does not occur.
	/// C# does long shifts mod 64, that is, only the 6 least significant
	/// bits of the shift amount are used -- this feature is relied upon!
	/// </remarks>
	public class SparseBitSet
	{
		private readonly int _length;
		private readonly ulong[] _catalog;
		private readonly ulong[][] _chunks;

		public SparseBitSet(int length)
		{
			if (length < 1)
				throw new ArgumentOutOfRangeException(nameof(length), @"need at least length one");

			_length = length;

			// Thanks to the assertion above (length > 0),
			// arithmetic shift is fine (sign extension will not occur)
			int chunkCount = length >> 12; // div 4096
			if ((chunkCount << 12) < length)
				chunkCount += 1; // ceil

			_catalog = new ulong[chunkCount];
			_chunks = new ulong[chunkCount][];
		}

		public int Length => _length;

		public int Cardinality
		{
			get
			{
				return _chunks.Where(chunk => chunk != null)
					.SelectMany(chunk => chunk)
					.Sum(BitUtils.PopulationCount);
			}
		}

		public bool Get(int i)
		{
			if (i < 0 || i >= _length)
				throw new ArgumentOutOfRangeException();

			int chunkIndex = i >> 12; // i div 4096
			int wordNum = i >> 6; // i div 64 is word within chunk
			ulong flags = _catalog[chunkIndex];

			// First check in catalog:
			// if flags[i/64 mod 64] is zero, then the ith bit in the set is zero
			if ((flags & (1UL << wordNum)) == 0)
			{
				return false;
			}

			// The catalog says that bit (i/64 mod 64) in chunk i/4096 is set.
			// Look at word k in _chunks[chunkIndex] where k is the number of
			// bits set in flags below bit (i/64 mod 64). Why? Because a chunk
			// array stores only words with at lest one non-zero bit and for
			// any such word the corresponding bit in the _catalog is set.
			int offset = BitUtils.PopulationCount(flags & ((1UL << wordNum) - 1));
			ulong word = _chunks[chunkIndex][offset];
			return (word & (1UL << i)) != 0;
		}

		public SparseBitSet Set(int i)
		{
			if (i < 0 || i >= _length)
				throw new ArgumentOutOfRangeException();

			int chunkIndex = i >> 12; // div 4096
			int wordNum = i >> 6; // div 64
			ulong flags = _catalog[chunkIndex];

			// Three cases:
			// 1. word in chunk is non-zero: just set the bit
			// 2. word is zero in non-zero chunk: insert a word into the chunk array
			// 3. entire chunk is zero: insert a new chunk

			if ((flags & (1UL << wordNum)) != 0)
			{
				// Case 1: chunk exists, word exists: set the bit
				int offset = BitUtils.PopulationCount(flags & ((1UL << wordNum) - 1));
				_chunks[chunkIndex][offset] |= (1UL << i);
			}
			else if (flags != 0)
			{
				// Case 2: word is zero in non-zero chunk: insert word
				InsertWord(chunkIndex, wordNum, i, flags);
			}
			else
			{
				// Case 3: entire chunk is zero: insert a new chunk
				Debug.Assert(_chunks[chunkIndex] == null);
				_catalog[chunkIndex] = (1UL << wordNum);
				var newChunk = new ulong[1];
				newChunk[0] = (1UL << i);
				_chunks[chunkIndex] = newChunk;
			}

			return this;
		}

		public SparseBitSet Clear(int i)
		{
			if (i < 0 || i >= _length)
				throw new ArgumentOutOfRangeException();

			int chunkIndex = i >> 12; // div 4096
			int wordNum = i >> 6; // div 64
			ulong flags = _catalog[chunkIndex];

			if ((flags & (1UL << wordNum)) != 0)
			{
				// The word's offset within its chunk:
				int offset = BitUtils.PopulationCount(flags & ((1UL << wordNum) - 1));

				ulong word = _chunks[chunkIndex][offset];
				word &= ~(1UL << i); // clear the bit

				if (word == 0)
				{
					// All bits are gone, remove the entire word:
					RemoveWord(chunkIndex, wordNum, flags, offset);
				}
				else
				{
					// Some bits remain:
					_chunks[chunkIndex][offset] = word;
				}
			}

			return this;
		}

		/// <summary>
		/// Returns the next set bit at or after <paramref name="i"/>
		/// or -1 if no such bit; note that set.Set(i).NextSetBit(i) == i
		///</summary>
		public int NextSetBit(int i)
		{
			int chunkIndex = i >> 12; // div 4096
			ulong flags = _catalog[chunkIndex];
			int wordNum = i >> 6; // div 64
			ulong[] chunk = _chunks[chunkIndex];

			int offset = BitUtils.PopulationCount(flags & ((1UL << wordNum) - 1));
			if ((flags & (1UL << wordNum)) != 0)
			{
				// At least one bit is set in the current word; bit i MAY be
				// amongst them; right shift by i (mod 64) to see if there
				// are any bits at or after i:
				ulong bits = chunk[offset] >> i;
				if (bits != 0)
				{
					// Ok, bits at or after i; NTZ(bits) is how many positions
					// after i they occur (if bit i is set; NTZ(bits)==0)
					int ntz = BitUtils.TrailingZeroCount(bits);
					return i + ntz;
				}
				offset += 1; // next word in chunk array
			}

			// Shift away the stuff we already looked at (>>1 for "my" word):
			flags = flags >> wordNum >> 1;
			if (flags > 0)
			{
				// The current chunk still contains some set bits:
				// Advance wordNum past the current word and intermediate all-zero words:
				wordNum += 1;
				wordNum += BitUtils.TrailingZeroCount(flags);
				ulong word = chunk[offset];
				return (wordNum << 6) | BitUtils.TrailingZeroCount(word);
			}

			// No more bits in the current chunk; search in following chunk(s):
			while (++chunkIndex < _catalog.Length)
			{
				flags = _catalog[chunkIndex];
				if (flags != 0)
				{
					wordNum = BitUtils.TrailingZeroCount(flags);
					int bitNum = BitUtils.TrailingZeroCount(_chunks[chunkIndex][0]);
					return (chunkIndex << 12) | (wordNum << 6) | bitNum;
				}
			}

			// i was past the last set bit:
			return -1;
		}

		private void InsertWord(int chunkIndex, int wordNum, int i, ulong flags)
		{
			_catalog[chunkIndex] |= 1UL << wordNum; // mark word as non-zero

			int offset = BitUtils.PopulationCount(flags & ((1UL << wordNum) - 1));
			ulong[] chunk = _chunks[chunkIndex];

			Debug.Assert(chunk.Length > 0);

			if (chunk[chunk.Length - 1] == 0)
			{
				// Chunk array has (at least one) empty slot:
				// copy words within array, no need to realloc.
				Array.Copy(chunk, offset, chunk, offset + 1, chunk.Length - offset - 1);
				chunk[offset] = 1UL << i;
			}
			else
			{
				// Enlarge chunk array to accomodate the new word:
				int newSize = Oversize(chunk.Length + 1);
				var newChunk = new ulong[newSize];
				Array.Copy(chunk, 0, newChunk, 0, offset);
				newChunk[offset] = 1UL << i;
				Array.Copy(chunk, offset, newChunk, offset + 1, chunk.Length - offset);
				_chunks[chunkIndex] = newChunk;
			}
		}

		private static int Oversize(int size)
		{
			int extra = size >> 1;
			int newSize = size + extra;
			return (newSize > 50) ? 64 : newSize;
		}

		private void RemoveWord(int chunkIndex, int wordNum, ulong flags, int offset)
		{
			// Clear word's flag in catalog:
			flags &= ~(1UL << wordNum);
			_catalog[chunkIndex] = flags;

			if (flags == 0)
			{
				// The chunk is now all-zero: release the array of words
				_chunks[chunkIndex] = null;
			}
			else
			{
				// Move words after the now-empty word down in the chunk array;
				// do not shrink the chunk array; the empty slots at the end
				// may be reused by later Set(i) operations.
				int length = BitUtils.PopulationCount(flags);
				ulong[] chunk = _chunks[chunkIndex];
				Array.Copy(chunk, offset + 1, chunk, offset, length - offset);
				chunk[length] = 0UL; // empty slot at end of chunk array
			}
		}
	}
}
