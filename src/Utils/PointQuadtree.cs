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

		public bool IsDirty { get; private set; }

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

				var quadrant = GetQuadrant(point.X, point.Y, scout.Point);

				parent = scout;
				scout = scout[quadrant];
			}

			if (parent != null)
			{
				var quadrant = GetQuadrant(point.X, point.Y, parent.Point);
				parent[quadrant] = new Node(item, point);
			}
			else
			{
				_root = new Node(item, point);
			}

			IsDirty = true;
		}

		public int Count(Envelope extent, Predicate<T> filter)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));

			return Query(_root, extent, filter, null);
		}

		public int Query(Envelope extent, Predicate<T> filter, ICollection<T> results)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));

			return Query(_root, extent, filter, node => results?.Add(node.Payload));
		}

		public int Query(Point center, double maxdist, int maxitems, Predicate<T> filter, ICollection<T> results)
		{
			if (center == null)
				throw new ArgumentNullException(nameof(center));
			if (maxdist <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxdist), "must be greater than zero");

			if (maxitems <= 0) return 0;

			NearestPoints nearest = new NearestPointsXY(center, maxdist, maxitems);

			var extent = new Envelope(center.X - maxdist, center.Y - maxdist,
			                          center.X + maxdist, center.Y + maxdist);

			Query(_root, extent, filter, node => nearest.AddNode(node));

			return nearest.Collect(results);
		}

		/// <summary>
		/// Same as Query(center, radius, ...) but assuming coordinates
		/// are latitude/longitude in decimal degrees on a spherical Earth.
		/// </summary>
		/// <param name="center">Center point (X=longitude, Y=latitude)</param>
		/// <param name="distanceMeters">Maximum distance from center in meters</param>
		/// <param name="maxitems">Maximum number of items to search for</param>
		/// <param name="filter">Optional filtering predicate</param>
		/// <param name="results">Collection of results, can be null</param>
		/// <returns>Number of items found, at most <paramref name="maxitems"/></returns>
		public int QueryGeo(Point center, double distanceMeters, int maxitems, Predicate<T> filter, ICollection<T> results)
		{
			if (center == null)
				throw new ArgumentNullException(nameof(center));
			if (distanceMeters <= 0)
				throw new ArgumentOutOfRangeException(nameof(distanceMeters), "must be greater than zero");

			if (maxitems <= 0) return 0;

			NearestPoints nearest = new NearestPointsGeo(center, distanceMeters, maxitems);

			Geodesy.PointRadiusBox(center.Y, center.X, distanceMeters,
				out var west, out var south, out var east, out var north);

			if (west > east)
			{
				var extentW = new Envelope(-180.0, south, east, north);
				Query(_root, extentW, filter, node => nearest.AddNode(node));

				var extentE = new Envelope(west, south, 180.0, north);
				Query(_root, extentE, filter, node => nearest.AddNode(node));
			}
			else
			{
				var extent = new Envelope(west, south, east, north, true);

				Query(_root, extent, filter, node => nearest.AddNode(node));
			}

			return nearest.Collect(results);
		}

		public void Query(Envelope extent, Action<T, Point> collector)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));
			if (collector == null)
				throw new ArgumentNullException(nameof(collector));

			Query(_root, extent, null, WrapCollector(collector));
		}

		private static Action<Node> WrapCollector(Action<T, Point> collector)
		{
			return node => collector(node.Payload, node.Point);
		}

		public void Build()
		{
			// Not really a "build" but named so for consistency with InvertedIndex.
			// We could, however, try to balance the Quadtree... but that's hard.

			Analyze(_root);

			IsDirty = false;
		}

		public void Clear()
		{
			_root = null; // GC will reclaim all nodes
			_boundingBox = null;
			_itemCount = _meanDepth = _maxDepth = 0;
			IsDirty = false; // all empty is clean
		}

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
			if (IsDirty)
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
				_meanDepth = depthSum / _itemCount;
			}
		}

		private static void Analyze(Stack<KeyValuePair<Node,int>> stack, ref int nodeCount, ref int depthSum, ref int maxDepth, ref Envelope bbox)
		{
			while (stack.Count > 0)
			{
				var entry = stack.Pop();

				var node = entry.Key;
				var depth = entry.Value;

				Debug.Assert(node != null, "Node on stack is null");

				nodeCount += 1;
				depthSum += depth;

				if (depth > maxDepth)
				{
					maxDepth = depth;
				}

				bbox = bbox.Expand(node.Point);

				var ne = node[Quadrant.NE];
				if (ne != null) stack.Push(new KeyValuePair<Node, int>(ne, depth + 1));

				var nw = node[Quadrant.NW];
				if (nw != null) stack.Push(new KeyValuePair<Node, int>(nw, depth + 1));

				var sw = node[Quadrant.SW];
				if (sw != null) stack.Push(new KeyValuePair<Node, int>(sw, depth + 1));

				var se = node[Quadrant.SE];
				if (se != null) stack.Push(new KeyValuePair<Node, int>(se, depth + 1));
			}
		}

		#region Recursive Query

		//private static int Query(Node node, ICollection<T> results, Envelope extent)
		//{
		//    // node may be null
		//    // results may be null (to count only)
		//    Assert.ArgumentNotNull(extent, "extent");

		//    int count = 0;

		//    if (node != null)
		//    {
		//        if (extent.Contains(node.Point))
		//        {
		//            count += 1;

		//            if (results != null)
		//            {
		//                results.Add(node.Payload);
		//            }
		//        }

		//        // Upper left corner NW of node?
		//        if (GetQuadrant(extent.XMin, extent.YMax, node.Point) == Quadrant.NW)
		//        {
		//            count += Query(node[Quadrant.NW], results, extent);
		//        }

		//        // Upper right corner NE of node?
		//        if (GetQuadrant(extent.XMax, extent.YMax, node.Point) == Quadrant.NE)
		//        {
		//            count += Query(node[Quadrant.NE], results, extent);
		//        }

		//        // Lower left corner SW of node?
		//        if (GetQuadrant(extent.XMin, extent.YMin, node.Point) == Quadrant.SW)
		//        {
		//            count += Query(node[Quadrant.SW], results, extent);
		//        }

		//        // Lower right corner SE of node?
		//        if (GetQuadrant(extent.XMax, extent.YMin, node.Point) == Quadrant.SE)
		//        {
		//            count += Query(node[Quadrant.SE], results, extent);
		//        }
		//    }

		//    return count;
		//}

		#endregion

		private int Query(Node root, Envelope extent, Predicate<T> filter, Action<Node> collector)
		{
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));

			if (root == null)
			{
				return 0;
			}

			int capacity = IsDirty ? 8 : Math.Max(_meanDepth, 8);
			var stack = new Stack<Node>(capacity);

			stack.Push(root);

			return Query(stack, extent, filter ?? AcceptAll, collector);
		}

		private static int Query(Stack<Node> stack, Envelope extent, Predicate<T> filter, Action<Node> collector)
		{
			if (stack == null)
				throw new ArgumentNullException(nameof(stack));
			if (extent == null)
				throw new ArgumentNullException(nameof(extent));
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			int count = 0;

			while (stack.Count > 0)
			{
				var node = stack.Pop();
				var nodePoint = node.Point;

				if (extent.Contains(nodePoint) && filter(node.Payload))
				{
					count += 1;

					collector?.Invoke(node);
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

		private static bool AcceptAll(T item)
		{
			return true;
		}

		#endregion

		#region Nested type: Quadrant

		private enum Quadrant
		{
			NE = 0,
			NW = 1,
			SW = 2,
			SE = 3
		}

		#endregion

		#region Nested type: Node

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
				get => _nodes[(int) quadrant];
				set => _nodes[(int) quadrant] = value;
			}
		}

		#endregion

		#region Nested type: NearestPoints

		private abstract class NearestPoints : PriorityQueue<Node>
		{
			protected NearestPoints(int capacity) : base(capacity) { }

			public abstract void AddNode(Node node);

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

					if (results is IList<T> list)
					{
						// Nearest first:
						int startIndex = list.Count - count;
						ListUtils.Reverse(list, startIndex, count);
					}
				}

				return count;
			}
		}

		private class NearestPointsXY : NearestPoints
		{
			private readonly Point _center;
			private readonly double _maxDistSquared;

			public NearestPointsXY(Point center, double maxdist, int capacity)
				: base(capacity)
			{
				_center = center;
				_maxDistSquared = maxdist * maxdist;
			}

			protected override bool Priority(Node a, Node b)
			{
				double da = GetDistanceSquared(_center, a.Point);
				double db = GetDistanceSquared(_center, b.Point);
				return da >= db;
			}

			public override void AddNode(Node node)
			{
				double dist = GetDistanceSquared(_center, node.Point);
				if (dist <= _maxDistSquared)
				{
					AddWithOverflow(node);
				}
				// else: inside search box but outside circle!
			}

			private static double GetDistanceSquared(Point a, Point b)
			{
				double dx = a.X - b.X;
				double dy = a.Y - b.Y;

				return dx * dx + dy * dy;
			}
		}

		private class NearestPointsGeo : NearestPoints
		{
			private readonly Point _center;
			private readonly double _maxH;

			public NearestPointsGeo(Point center, double distanceMeters, int capacity)
				: base(capacity)
			{
				_center = center;
				_maxH = Geodesy.HaversineSortKey(distanceMeters);
			}

			protected override bool Priority(Node a, Node b)
			{
				var pa = a.Point;
				var pb = b.Point;

				var ha = Geodesy.HaversineSortKey(_center.Y, _center.X, pa.Y, pa.X);
				var hb = Geodesy.HaversineSortKey(_center.Y, _center.X, pb.Y, pb.X);

				return ha >= hb;
			}

			public override void AddNode(Node node)
			{
				var p = node.Point;
				double h = Geodesy.HaversineSortKey(_center.Y, _center.X, p.Y, p.X);
				if (h <= _maxH)
				{
					AddWithOverflow(node);
				}
				// else: inside search box but outside circle!
			}
		}

		#endregion
	}
}
