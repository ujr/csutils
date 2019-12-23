
Constant Database (CDB)
=======================

A constant database (CDB) is an on-disk associative array, mapping
byte strings (keys) to byte strings (data). It was designed and
implemented as a C language API by D. J. Bernstein in 1996, and
released into the public domain in 2009. 
See [cr.yp.to/cdb.html](http://cr.yp.to/cdb.html) for the original
software and documentation,
and [cr.yp.to/distributors.html](https://cr.yp.to/distributors.html)
for the public domain dedication.


Structure of a CDB
------------------

A CDB is a single file on disk, consisting of a fixed-size
hash table at the beginning, followed by the key/data records,
followed by up to 256 secondary hash tables.

The fixed-size hash table has 256 (offset, length) pairs
where the *length* is the number of entries in the
secondary table pointed to by *offset*. If *length* is
zero the corresponding secondary table does not exist.

Records are stored sequentially with no padding. Each
record is a tuple (key length, data length, key, data).

Slots in the secondary tables are (hash, offset) pairs.
If *offset* is zero, the slot is empty and marks the
end for lookup probing.

Keys and data are opaque byte strings.
Offsets are in bytes from the beginning of the file.
All offsets and lengths (and hash values) are 32-bit
integers, so there is an effective 4GB file size limit.

A record is located as follows:

  1. Compute the key's hash value `h`.
  2. Let `i=h%256` the index into the fixed-size table,
     which yields the offset `p` and the table length `n`
     of the relevant secondary table.
  3. Probe the slot at index `j=(h/256)%n` in the secondary
     table starting at offset `p`. If the hash values
     and the keys agree, the record is found, otherwise
     probe the next slot until the record is found or
     an empty slot is reached.

The CDB structure and hash function are specified in
<http://cr.yp.to/cdb/cdb.txt>. Detailed illustrations can be found
at <http://www.unixuser.org/~euske/doc/cdbinternals/>.


The C# Port
-----------

The present C# code is a straightforward port of Bernstein's
original C code. The API is slightly different, in particular,
the Find method returns the data directly as a new byte array.
The static Cdb class provides convenient access to most
functionality, and mimics the original *cdbget*, *cdbmake*,
and *cdbdump* command line tools.

  * The `CdbFile` class is for querying CDB files.
  * The `CdbMake` class is for creating CDB files.
  * The `Cdb` class is a static accessor to both and the central entry point.


Usage Examples
--------------

Creating a constant database:

```C#
var maker =  Cdb.Make("path/to/my.tmp");
byte[] key = GetKey();
byte[] data = GetData();
maker.Add(key, data);
maker.Close(); // or maker.Dispose()

Move("path/to/my.tmp", "path/to/my.cdb");
```

Alternatively, the data is provided in a file as a sequence
of lines with the format `+keylen,datalen:key->data\n`, for
example `+3,4:key->data\n`.

```C#
File.AppendAllText("input.txt", "+3,4:key->data\n+3,3:Foo->Bar\n");

Cdb.Make("input.txt", "path/to/my.tmp");

Move("path/to/my.tmp", "path/to/my.cdb");
```

Usually, a CDB is created into a temporary file, which is
then (atomically) moved into place (overwriting an earlier
CDB file). This logic is indicated by the `Move` call above
and is not part of the C# port.

Querying a constant database once:

```C#
byte[] key = GetKey();
byte[] data = Cdb.Get("path/to/my.cdb", key);
if (data == null) // no such key
// else do something with data
```

Querying a constant database multiple times:

```C#
var cdb = Cdb.Open("path/to/my.cdb");
byte[] key = GetKey();
byte[] data = cdb.Find(key);
if (data == null) // no such key
// else do something with data
// perform further queries on cdb
cdb.Close(); // or cdb.Dispose()
```

To retrieve all entries with a given key:

```C#
var cdb = Cdb.Open("path/to/my.cdb");
byte[] key = GetKey();
byte[] data;
cdb.FindStart();
while ((data = FindNext(key)) != null)
    // another record with this same key
```

Note that in this case the `key` must not change.
Whenever there is another key, first call `FindStart`.
(The API could be changed such that the key is passed
to FindStart, not to FindNext, but then CdbFile must
keep a copy of the key internally.)

Getting all entries in a CDB:

```C#
foreach (var record in Cdb.Dump("path/to/my.cdb"))
{
    byte[] key = record.Key;
    byte[] data = record.Data;
    // do something with key and/or data
}
```
