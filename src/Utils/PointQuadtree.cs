using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Sylphe.Utils
{
	public sealed class PointQuadtree<T>
	{
		private Node _root;
		private int _itemCount;
		private int _meanDepth;
		private int _maxDepth;
		private Envelope _boundingBox;
		private bool _isDirty;

		/// <summary>
		/// By default, multiple items can be added at the same point.
		/// If <c>true</c>, items at the same point are replaced.
		/// </summary>
		public bool ReplaceCoincident { get; set; }

		public void Add(T item, Point point)
		{
			if (point == null)
				throw new ArgumentNullException(nameof(point));
			if (point.IsEmpty)
				throw new ArgumentException("must not be empty", nameof(point));

			Node parent = null;
			Node scout = _root;

			while (scout != null)
			{
				if (ReplaceCoincident && scout.Point.Equals(point))
				{
					scout.Payload = item;
					return;
				}

				Quadrant quadrant = GetQuadrant(point.X, point.Y, scout.Point);

				parent = scout;
				scout = scout[quadrant];
			}

			if (parent != null)
			{
				Quadrant quadrant = GetQuadrant(point.X, point.Y, parent.Point);
				parent[quadrant] = new Node(item, point);
			}
			else
			{
				_root = new Node(item, point);
			}

			_isDirty = true;
		}

		public int Count(Envelope extent, Predicate<T> filter)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));
			// filter may be null

			return Query(_root, extent, filter, null);
		}

		public int Query(Envelope extent, Predicate<T> filter, ICollection<T> results)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));
			// filter and results may be null

			return Query(_root, extent, filter, node => results?.Add(node.Payload));
		}

		public int Query(Point center, double maxdist, int maxitems, Predicate<T> filter, ICollection<T> results)
		{
			if (center == null)
				throw new ArgumentNullException(nameof(center));
			if (maxdist <= 0)
				throw new ArgumentOutOfRangeException("must be greater than zero", nameof(maxdist));
			// filter and results may be null

			if (maxitems <= 0) return 0;

			var nearest = new NearestQueue(center, maxdist, maxitems, filter);

			var extent = new Envelope(center.X - maxdist, center.Y - maxdist,
									  center.X + maxdist, center.Y + maxdist);

			Query(_root, extent, filter, node => nearest.AddNode(node));

			return nearest.Collect(results);
		}

		public void Query(Envelope extent, Action<T,Point> collector)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));
			if (collector == null)
				throw new ArgumentNullException(nameof(collector));

			Query(_root, extent, null, WrapCollector(collector));
		}

		private static Action<Node> WrapCollector(Action<T, Point> collector)
		{
			return (Node node) => collector(node.Payload, node.Point);
		}

		public void Build()
		{
			// Not really a "build" but named so for consistency with InvertedIndex.
			// We could, however, try to balance the Quadtree... but not today.

			Analyze(_root);

			_isDirty = false;
		}

		public void Clear()
		{
			_root = null; // GC will reclaim all nodes
			_boundingBox = null;
			_itemCount = _meanDepth = _maxDepth = 0;
			_isDirty = false; // all empty is clean
		}

		public bool IsDirty => _isDirty;

		public int ItemCount
		{
			get
			{
				AssertNotDirty();
				return _itemCount;
			}
		}

		public int MeanDepth
		{
			get
			{
				AssertNotDirty();
				return _meanDepth;
			}
		}

		public int MaxDepth
		{
			get
			{
				AssertNotDirty();
				return _maxDepth;
			}
		}

		public Envelope BoundingBox
		{
			get
			{
				AssertNotDirty();
				return _boundingBox;
			}
		}

		public void Dump(TextWriter writer)
		{
			Dump(_root, writer);
		}

		#region Non-public methods

		private void AssertNotDirty()
		{
			if (_isDirty)
			{
				throw new InvalidOperationException("Must Build() first");
			}
		}

		private void Analyze(Node root)
		{
			_itemCount = _meanDepth = _maxDepth = 0;
			_boundingBox = null;

			if (root != null)
			{
				var stack = new Stack<KeyValuePair<Node,int>>();

				stack.Push(new KeyValuePair<Node,int>(root, 0));
				int depthSum = 0;
				_boundingBox = new Envelope(root.Point);
				Analyze(stack, ref _itemCount, ref depthSum, ref _maxDepth, ref _boundingBox);
				_meanDepth = depthSum/_itemCount;
			}
		}

		private static void Analyze(Stack<KeyValuePair<Node,int>> stack, ref int nodeCount, ref int depthSum, ref int maxDepth, ref Envelope bbox)
		{
			while (stack.Count > 0)
			{
				var entry = stack.Pop();

				Node node = entry.Key;
				int depth = entry.Value;

				Debug.Assert(node != null, "Node on stack is null");

				nodeCount += 1;
				depthSum += depth;

				if (depth > maxDepth)
				{
					maxDepth = depth;
				}

				bbox = bbox.Expand(node.Point);

				Node ne = node[Quadrant.NE];
				if (ne != null) stack.Push(new KeyValuePair<Node, int>(ne, depth + 1));

				Node nw = node[Quadrant.NW];
				if (nw != null) stack.Push(new KeyValuePair<Node, int>(nw, depth + 1));

				Node sw = node[Quadrant.SW];
				if (sw != null) stack.Push(new KeyValuePair<Node, int>(sw, depth + 1));

				Node se = node[Quadrant.SE];
				if (se != null) stack.Push(new KeyValuePair<Node, int>(se, depth + 1));
			}
		}

		#region Recursive Query

		// private static int Query(Node node, Envelope extent, ICollection<T> results)
		// {
		// 	if (extent == null)
		// 		throw new ArgumentNullException(nameof(extent));
		// 	// node and results may be null

		// 	int count = 0;

		// 	if (node != null)
		// 	{
		// 		if (extent.Contains(node.Point))
		// 		{
		// 			count += 1;

		// 			if (results != null)
		// 			{
		// 				results.Add(node.Payload);
		// 			}
		// 		}

		// 		// Upper left corner NW of node?
		// 		if (GetQuadrant(extent.XMin, extent.YMax, node.Point) == Quadrant.NW)
		// 		{
		// 			count += Query(node[Quadrant.NW], extent, results);
		// 		}

		// 		// Upper right corner NE of node?
		// 		if (GetQuadrant(extent.XMax, extent.YMax, node.Point) == Quadrant.NE)
		// 		{
		// 			count += Query(node[Quadrant.NE], extent, results);
		// 		}

		// 		// Lower left corner SW of node?
		// 		if (GetQuadrant(extent.XMin, extent.YMin, node.Point) == Quadrant.SW)
		// 		{
		// 			count += Query(node[Quadrant.SW], extent, results);
		// 		}

		// 		// Lower right corner SE of node?
		// 		if (GetQuadrant(extent.XMax, extent.YMin, node.Point) == Quadrant.SE)
		// 		{
		// 			count += Query(node[Quadrant.SE], extent, results);
		// 		}
		// 	}

		// 	return count;
		// }

		#endregion

		private int Query(Node root, Envelope extent, Predicate<T> filter, Action<Node> collector)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));

			if (root == null)
			{
				return 0;
			}

			int capacity = _isDirty ? 8 : Math.Max(_meanDepth, 8);
			var stack = new Stack<Node>(capacity);

			stack.Push(root);

			return Query(stack, extent, filter, collector);
		}

		private int Query(Stack<Node> stack, Envelope extent, Predicate<T> filter, Action<Node> collector)
		{
			if (stack == null)
				throw new ArgumentNullException(nameof(stack));
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));

			int count = 0;

			while (stack.Count > 0)
			{
				Node node = stack.Pop();
				Point nodePoint = node.Point;

				if (extent.Contains(nodePoint) && (filter == null || filter(node.Payload)))
				{
					count += 1;

					if (collector != null)
					{
						collector(node);
					}
				}

				// Hint: the code below could be written using GetQuadrant(),
				// but for ultimate speed it's not. Still we follow the
				// convention that a quadrant's lower and left boundary is
				// closed, its upper and right boundary is open.

				bool east = nodePoint.X <= extent.XMax;
				bool north = nodePoint.Y <= extent.YMax;
				bool west = extent.XMin < nodePoint.X;
				bool south = extent.YMin < nodePoint.Y;

				if (east && north)
				{
					var sub = node[Quadrant.NE];
					if (sub != null) stack.Push(sub);
				}

				if (north && west)
				{
					var sub = node[Quadrant.NW];
					if (sub != null) stack.Push(sub);
				}

				if (west && south)
				{
					var sub = node[Quadrant.SW];
					if (sub != null) stack.Push(sub);
				}

				if (south && east)
				{
					var sub = node[Quadrant.SE];
					if (sub != null) stack.Push(sub);
				}
			}

			return count;
		}

		private static Quadrant GetQuadrant(double pointX, double pointY, Point reference)
		{
			// Convention: the lower and left boundary of the quadrants
			// around the reference point are closed, the upper and right
			// boundaries are open. For example, a point that is exactly
			// south of the reference point goes to the SE quadrant.

			Quadrant result;

			if (pointX < reference.X)
			{
				result = pointY < reference.Y ? Quadrant.SW : Quadrant.NW;
			}
			else
			{
				result = pointY < reference.Y ? Quadrant.SE : Quadrant.NE;
			}

			return result;
		}

		private static void Dump(Node node, TextWriter writer)
		{
			if (node != null)
			{
				writer.Write("({0} ", node.Payload);

				Dump(node[Quadrant.NE], writer);
				Dump(node[Quadrant.NW], writer);
				Dump(node[Quadrant.SW], writer);
				Dump(node[Quadrant.SE], writer);

				writer.Write(")");
			}
			else
			{
				writer.Write(".");
			}
		}

		#endregion

		private enum Quadrant
		{
			NE = 0,
			NW = 1,
			SW = 2,
			SE = 3
		}

		private class Node
		{
			private readonly Node[] _nodes;

			internal Node(T payload, Point point)
			{
				Point = point; // not null
				Payload = payload;
				_nodes = new Node[4];
			}

			internal Point Point { get; }

			internal T Payload { get; set; }

			internal Node this[Quadrant quadrant]
			{
				get { return _nodes[(int) quadrant]; }
				set { _nodes[(int) quadrant] = value; }
			}
		}

		private class NearestQueue : PriorityQueue<Node>
		{
			private readonly Point _center;
			private readonly double _maxDistSquared;
			private readonly Predicate<T> _filter;

			public NearestQueue(Point center, double maxdist, int capacity, Predicate<T> filter) : base(capacity)
			{
				_center = center;
				_maxDistSquared = maxdist * maxdist;
				_filter = filter ?? (item => true);
			}

			protected override bool Priority(Node a, Node b)
			{
				double da = GetDistanceSquared(_center, a.Point);
				double db = GetDistanceSquared(_center, b.Point);
				return da >= db;
			}

			public void AddNode(Node node)
			{
				double dist = GetDistanceSquared(_center, node.Point);
				if (dist <= _maxDistSquared && _filter(node.Payload))
				{
					base.AddWithOverflow(node);
				}
			}

			public int Collect(ICollection<T> results)
			{
				int count = Count;

				if (results != null)
				{
					while (Count > 0)
					{
						var node = Pop();
						results.Add(node.Payload);
					}

					var list = results as IList<T>;
					if (list != null)
					{
						// Nearest first:
						int startIndex = list.Count - count;
						ListUtils.Reverse(list, startIndex, count);
					}
				}

				return count;
			}

			private static double GetDistanceSquared(Point a, Point b)
			{
				double dx = a.X - b.X;
				double dy = a.Y - b.Y;

				return dx * dx + dy * dy;
			}
		}
	}
}
