using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sylphe.IR
{
	public abstract class Query
	{
		public bool Negated { get; private set; }

		public abstract Query Rewrite(bool negate = false);

		public abstract DocSetIterator Search(IInvertedIndex index, bool ignoreNegation = false);

		public static Query Parse(string text)
		{
			// Expression:   Disjunction
			// Disjunction:  Conjunction { , Conjunction }
			// Conjunction:  Factor { . Factor }
			// Factor:       [-] ( Literal | ( Expression ) )
			// Literal:      /[A-Za-z][A-Za-z0-9:]*/

			if (string.IsNullOrWhiteSpace(text)) return new NoDocsQuery();

			var index = 0;
			var query = ParseDisjunction(text, ref index);

			SkipWhite(text, ref index);
			if (index < text.Length)
				throw SyntaxError("Unexpected input after expression", index);

			return query;
		}

		#region Parser

		// ReSharper disable InconsistentNaming
		private const char OR = ',';
		private const char AND = '.';
		private const char NOT = '-';
		private const char LPAREN = '(';
		private const char RPAREN = ')';
		// ReSharper restore InconsistentNaming

		private static Query ParseDisjunction(string text, ref int index)
		{
			var first = ParseConjunction(text, ref index);
			SkipWhite(text, ref index);

			CompoundQuery compound = null;
			while (index < text.Length && text[index] == OR)
			{
				index += 1;

				if (compound == null)
					compound = CompoundQuery.Or(first);

				var query = ParseConjunction(text, ref index);
				SkipWhite(text, ref index);

				compound.AddClause(query);
			}

			return compound ?? first;
		}

		private static Query ParseConjunction(string text, ref int index)
		{
			var first = ParseFactor(text, ref index);
			SkipWhite(text, ref index);

			CompoundQuery compound = null;
			while (index < text.Length && text[index] == AND)
			{
				index += 1;

				if (compound == null)
					compound = CompoundQuery.And(first);

				var query = ParseFactor(text, ref index);
				SkipWhite(text, ref index);

				compound.AddClause(query);
			}

			return compound ?? first;
		}

		private static Query ParseFactor(string text, ref int index)
		{
			var negated = false;
			SkipWhite(text, ref index);

			if (index < text.Length && text[index] == NOT)
			{
				negated = true;
				index += 1;
				SkipWhite(text, ref index);
			}

			if (index < text.Length && text[index] == LPAREN)
			{
				index += 1; // skip the paren
				var query = ParseDisjunction(text, ref index);
				query.Negated = negated;
				SkipWhite(text, ref index);
				if (index >= text.Length)
					throw SyntaxError($"Expect '{RPAREN}' but got end-of-input", index);
				if (text[index] != RPAREN)
					throw SyntaxError($"Expect '{RPAREN}' but got '{text[index]}'", index);
				index += 1; // skip the paren
				return query;
			}

			return ParseLiteral(text, ref index, negated);
		}

		private static Query ParseLiteral(string text, ref int index, bool negate = false)
		{
			SkipWhite(text, ref index);

			if (index >= text.Length)
				throw SyntaxError("Expect a literal but got end-of-input", index);
			if (!char.IsLetter(text, index))
				throw SyntaxError($"Expect a literal but got '{text[index]}'", index);

			var anchor = index++;
			while (index < text.Length && (char.IsLetterOrDigit(text, index) || text[index] == ':')) index += 1;

			var literal = text.Substring(anchor, index - anchor);
			return new LiteralQuery(literal, negate);
		}

		private static void SkipWhite(string text, ref int index)
		{
			while (index < text.Length && char.IsWhiteSpace(text, index)) index += 1;
		}

		private static FormatException SyntaxError(string message, int position)
		{
			return new FormatException($"{message} (near position {position})");
		}

		#endregion

		private class NoDocsQuery : Query
		{
			public override Query Rewrite(bool negate = false)
			{
				return negate ? (Query) new AllDocsQuery() : this;
			}

			public override DocSetIterator Search(IInvertedIndex index, bool ignoreNegation = false)
			{
				if (ignoreNegation) return new EmptyIterator();
				return Negated ? index.All() : new EmptyIterator();
			}

			public override string ToString()
			{
				return "(NoDocs)";
			}
		}

		private class AllDocsQuery : Query
		{
			public override Query Rewrite(bool negate = false)
			{
				return negate ? (Query) new NoDocsQuery() : this;
			}

			public override DocSetIterator Search(IInvertedIndex index, bool ignoreNegation = false)
			{
				if (ignoreNegation) return index.All();
				return Negated ? new EmptyIterator() : index.All();
			}

			public override string ToString()
			{
				return "(AllDocs)";
			}
		}

		private class LiteralQuery : Query
		{
			public string Literal { get; }

			public LiteralQuery(string literal, bool negated = false)
			{
				Literal = literal ?? throw new ArgumentNullException(nameof(literal));
				Negated = negated;
			}

			public override Query Rewrite(bool negate = false)
			{
				return negate ? new LiteralQuery(Literal, !Negated) : this;
			}

			public override DocSetIterator Search(IInvertedIndex index, bool ignoreNegation = false)
			{
				var iterator = index.Get(Literal);

				if (Negated && !ignoreNegation)
				{
					if (!index.AllowAll)
						throw new NotSupportedException("Negative literal outside conjunction not supported");
					iterator = new ButNotIterator(index.All(), iterator);
				}

				return iterator;
			}

			public override string ToString()
			{
				return Negated ? $"(NOT {Literal})" : $"{Literal}";
			}
		}

		private enum BooleanBinOp
		{
			And,
			Or
		}

		public class CompoundQuery : Query
		{
			private BooleanBinOp Junctor { get; }
			private List<Query> Components { get; }

			public static CompoundQuery And(Query component, bool negated = false)
			{
				return new CompoundQuery(BooleanBinOp.And, component, negated);
			}

			public static CompoundQuery Or(Query component, bool negated = false)
			{
				return new CompoundQuery(BooleanBinOp.Or, component, negated);
			}

			private CompoundQuery(BooleanBinOp junctor, Query component, bool negated = false)
			{
				if (component == null)
					throw new ArgumentNullException(nameof(component));

				Junctor = junctor;
				Components = new List<Query>();
				Negated = negated;

				Components.Add(component);
			}

			private CompoundQuery(BooleanBinOp junctor, IEnumerable<Query> components, bool negated = false)
			{
				Junctor = junctor;
				Components = components?.ToList() ?? throw new ArgumentNullException(nameof(components));
				Negated = negated;
			}

			public void AddClause(Query query)
			{
				if (query == null)
					throw new ArgumentNullException();
				Components.Add(query);
			}

			public override Query Rewrite(bool negate = false)
			{
				if (Negated) negate = !negate;

				if (Components.Count == 1)
				{
					return Components[0].Rewrite(negate);
				}

				var components = new List<Query>();
				var junctor = negate ? Negate(Junctor) : Junctor;

				foreach (var component in Components)
				{
					var rewritten = component.Rewrite(negate);

					if (rewritten is CompoundQuery compound && compound.Junctor == junctor)
					{
						components.AddRange(compound.Components); // splice in
					}
					else if (rewritten is AllDocsQuery)
					{
						// X.T = X (omit) and X,T = T (return early)
						if (junctor == BooleanBinOp.Or)
						{
							return negate ? (Query) new NoDocsQuery() : new AllDocsQuery();
						}
					}
					else if (rewritten is NoDocsQuery)
					{
						// X,F = X (omit) and X.F = F (return early)
						if (junctor == BooleanBinOp.And)
						{
							return negate ? (Query) new AllDocsQuery() : new NoDocsQuery();
						}
					}
					else
					{
						components.Add(rewritten);
					}
				}

				// negation has been pushed down

				if (components.Count == 0)
				{
					if (junctor == BooleanBinOp.And)
						return new AllDocsQuery();
					if (junctor == BooleanBinOp.Or)
						return new NoDocsQuery();
					throw BadJunctor(junctor);
				}

				if (components.Count == 1) return components[0];

				return new CompoundQuery(junctor, components);

				// 1. if one component: extract, apply negated, rewrite, return
				// 2. if negated: swap and/or, negate each component
				// 3. rewrite each component
				// 4. splice in sub components with same junctor (associativity)
				// 5. remove neutral elements (X.T=X and X,F=X)
				// 6. simplify idempotency (X.X=X and X,X=X) TODO
				// 7. if no components remain: And => Verum, Or => Falsum
			}

			public override DocSetIterator Search(IInvertedIndex index, bool ignoreNegation = false)
			{
				switch (Junctor)
				{
					case BooleanBinOp.And:
						return CreateConjunctionIterator(index);
					case BooleanBinOp.Or:
						return CreateDisjunctionIterator(index);
					default:
						throw new NotSupportedException($"Unknown junctor '{Junctor}'");
				}
			}

			private DocSetIterator CreateConjunctionIterator(IInvertedIndex index)
			{
				var nrequired = Components.Count(c => !c.Negated);
				if (nrequired == 0)
				{
					if (!index.AllowAll)
						throw new NotSupportedException("All negative conjunction is not supported");
					return new ButNotIterator(index.All(),
						new DisjunctionIterator(Components.Where(c => c.Negated).Select(c => c.Search(index, true))
							.ToArray()));
				}

				var required = nrequired == 1
					? Components.Single(c => !c.Negated).Search(index)
					: new ConjunctionIterator(Components.Where(c => !c.Negated).Select(c => c.Search(index)).ToArray());

				var nexcluded = Components.Count(c => c.Negated);
				if (nexcluded == 0)
					return required;

				var excluded = nexcluded == 1
					? Components.Single(c => c.Negated).Search(index, true)
					: new DisjunctionIterator(Components.Where(c => c.Negated).Select(c => c.Search(index)).ToArray());

				return new ButNotIterator(required, excluded);
			}

			private DocSetIterator CreateDisjunctionIterator(IInvertedIndex index)
			{
				//if (Components.Count(c => c.Negated) > 0)
				//	throw new NotSupportedException("Disjunction with negative terms is not supported");

				var iterators = Components.Select(c => c.Search(index)).ToArray();
				return new DisjunctionIterator(iterators);
			}

			private static Exception BadJunctor(BooleanBinOp junctor)
			{
				return new InvalidOperationException($"Junctor '{junctor}' not supported");
			}

			private static BooleanBinOp Negate(BooleanBinOp junctor)
			{
				if (junctor == BooleanBinOp.And)
					return BooleanBinOp.Or;
				if (junctor == BooleanBinOp.Or)
					return BooleanBinOp.And;
				throw new InvalidOperationException($"Cannot negate BinOp '{junctor}'");
			}

			private string Format(bool negated, string junctor)
			{
				const string sep = " ";
				var sb = new StringBuilder();
				if (negated) sb.Append("(NOT ");
				sb.Append("(").Append(junctor ?? "*");
				foreach (var query in Components)
				{
					sb.Append(sep);
					sb.Append(query);
				}

				sb.Append(")");
				if (negated) sb.Append(")");
				return sb.ToString();
			}

			public override string ToString()
			{
				return Format(Negated, Junctor == BooleanBinOp.And ? "AND" : "OR");
			}
		}
	}
}
