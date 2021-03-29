

namespace Sylphe.IR
{
	/// <summary>
	/// The essence of an inverted index: given any term,
	/// yield all document IDs that contain this term.
	/// </summary>
	public interface IInvertedIndex
	{
		bool AllowAll { get; }
		DocSetIterator All();
		DocSetIterator Get(string term);
	}
}
