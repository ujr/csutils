
C# Utils
========

A small collection of C# utility methods and classes.  
Intended for selective copy/paste rather than assembly reference.

The code here is set up as a few .NET Core projects with unit tests,
so that it can be built and verified by running the test suite.

    git clone https://github.com/ujr/csutils
    cd csutils/src
    dotnet restore
    dotnet build
    dotnet test


The Code
--------

Utils

 - [BitUtils.cs](src/Utils/BitUtils.cs) - bit query and manipulation methods
 - [CappedQueue.cs](src/Utils/CappedQueue.cs) - a queue (first-in-first-out) of limited capacity
 - [ListUtils.cs](src/Utils/ListUtils.cs) - some `List<T>` methods but for `IList<T>`
 - [Parsing.cs](src/Utils/Parsing.cs) - parsing text strings (see also: Tokenizer)
 - [Point](src/Utils.Point.cs) and [Envelope](src/Utils/Envelope.cs) - immutable (x,y) and (x0,y0,x1,y1)
 - [PriorityQueue.cs](src/Utils/PriorityQueue.cs) - base class for a heap-based priority queue
 - [ReadOnlySublist.cs](src/Utils/ReadOnlySublist.cs) - read-only view on a subrange of an `IList<T>`
 - [Reservoir.cs](src/Utils/Reservoir.cs) - reservoir sampling (*k* random items from an `IEnumerable`)
 - [Shuffle.cs](src/Utils/Shuffle.cs) - shuffling an `IList<T>` (rearrange in random order)
 - [SparseBitSet.cs](src/Utils/SparseBitSet.cs) - fixed size bit set, suitable for sparse data
 - [StringUtils.cs](src/Utils/StringUtils.cs) - utilities for strings, some for rare use cases
 - [Tokenizer.cs](src/Utils/Tokenizer.cs) - separate a text into Name/Number/String/Operator tokens
 - [Variants.cs](src/Utils/Variants.cs) - expand variant notation, e.g. `ba[r|z]` to bar and baz
 - [ZCurve.cs](src/Utils/ZCurve.cs) - interlacing two dimensions into Morton order

JSON

 - [JsonWriter.cs](src/Json/JsonWriter.cs) - simple API to write syntactically correct JSON
 - [JsonReader.cs](src/Json/JsonReader.cs) - a low-level reader for JSON data
 - [JsonException.cs](src/Json/JsonException.cs) - used by JsonReader (but not by JsonWriter)
 - [Json.cs](src/Json/Json.cs) - minimalistic JSON serialization and hydratisation (dynamics)


Documentation
-------------

Where available, read the XML comments in the code,
and see the unit tests.

There are some [general notes](/doc/Notes.md) about tools and concepts,
and details about some of the utility classes:

 - [BitUtils.md](/doc/BitUtils.md)
 - [SparseBitSet.pdf](/doc/SparseBitSet.pdf)
 - [ZCurve.pdf](/doc/ZCurve.pdf)
