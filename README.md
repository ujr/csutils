
C# Utils
========

A small collection of C# utility methods and classes.  
Intended for selective copy/paste rather than assembly reference.

The code here is set up as a few .NET Core projects with unit tests, 
so that it can be verified by building it and running the test suite.

>   `dotnet build`  
>   `dotnet test`  


Utilities
---------

 - Utils/BitUtils.cs - bit query and manipulation methods
 - Utils/ListUtils.cs - some `List<T>` methods but for `IList<T>`
 - Utils/Reservoir.cs - reservoir sampling (*k* random items from an `IEnumerable`)
 - Utils/Shuffle.cs - shuffling an `IList<T>` (rearrange in random order)
 - Utils/SparseBitSet.cs - fixed size bit set, suitable for sparse data

 - Json/JsonWriter.cs - simple API to write syntactically correct JSON
 - Json/JsonReader.cs - a low-level reader for JSON data
 - Json/JsonException.cs - used by JsonReader (but not by JsonWriter)
 - Json/Json.cs - minimalistic JSON serialization and hydratisation (dynamics)
