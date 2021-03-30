# Inverted Index Trials

Inspired by Lucene, but several orders of magnitude smaller,
incomplete, no scoring, Boolean queries only, and all in-memory.
I've long since had an implementation that always materializes
the doc ID lists, whereas the approach here yields doc IDs only
on-demand / on-the-fly.

## Inverted index

At the core, an inverted index is a mapping from terms to list
of documents (or just doc IDs) containing the term. This is
abstracted by the `IInvertedIndex` interface, and the doc ID
lists are abstracted by the `DocSetIterator` abstract class.

By keeping doc ID lists strictly ordered by doc ID, merge
algorithms can be used to quickly compute set operations
(intersect, union) over those lists.

## Boolean operations

- Conjunction: start with largest first doc; then keep all lists at common doc
- Disjunction: same as merge sort, i.e., use a heap, but omit duplicates
- Negation: only "but not", no true negation (which potentially yields all docs)

Query syntax: following my old conventions, `.` is AND and `,` is OR.
The EBNF grammar has the details:

```text
Expression:   Disjunction
Disjunction:  Conjunction { , Conjunction }
Conjunction:  Factor { . Factor }
Factor:       [-] ( Literal | ( Expression ) )
Literal:      /[A-Za-z][A-Za-z0-9:]*/
```

## Usage

At the present stage, you must provide your own `IInvertedIndex`
implementation (see unit tests for a trivial example). Then use
the `Query` class as follows:

```cs
IInvertedIndex index = ...
var query = Query.Parse("foo.(bar,baz)");
var result = query.Search(index).GetAll();
foreach (int doc in result) ...
```

## Performance notes

It was observed that properties are much slower than fields
in Debug builds, whereas in Release builds they perform roughly
the same. Presumably, Release build optimizations replace the
method call with a direct field access.

Compared to my old implementation with materialized doc ID lists,
this implementation performs roughly at half the speed on Debug
builds, and at about the same speed on Release builds.

## Terminology

- IR = Information Retrieval
- Corups = collection = body of all documents
- Document = the basic unit to be retrieved
- Token = the units of a document to be indexed
- Term = normalized token
- Dictionary = mapping from terms to postings lists
- Postings List = list of document IDs per term

Information Retrieval is the field of research and technology
concerned with retrieving relevant documents from a corpus.
The dictionary and the postings list are the two central
data structures; together they may be referred to as an
*inverted index* because they work exactly like an index
in a book, which maps keywords to lists of pages in the book.
It is “inverted” in the sense that it is ordered by keyword,
not by page number, and in this sense any index is inverted.

## Literature

Manning, Raghavan, Schütze: *Introduction to Information Retrieval*,
Cambridge University Press, 2008. <https://nlp.stanford.edu/IR-book/>

D.R.Cutting, J.O.Pedersen: Space Optimizations for Total Ranking,
in proceedings of RIAO'97: Computer-Assisted Information Searching
on Internet, June 1997, pages 401-412.
<https://dl.acm.org/doi/10.5555/2856695.2856731>

The library of choice for real work is probably still Lucene
at <https://lucene.apache.org/>. It is written in Java.
A .NET port exists but was lagging several major versions behind,
though it seems that it was revived recently.
