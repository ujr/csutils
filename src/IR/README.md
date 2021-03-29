# Inverted Index Trials

Inspired by Lucene, but several orders of magnitude smaller
and purely in-memory. I've always had an implementation that
always materializes the doc ID lists, whereas this approach
only generates them on-demand / on-the-fly.

Boolean operations

- Conjunction: start with largest first doc; then keep all lists at common doc
- Disjunction: same as merge sort, i.e., use a heap, but omit duplicates
- Negation: only "but not", no true negation (which potentially yields all docs)

Inverted index

At the core, this is simply a mapping from terms to list of docs
containing the term.
