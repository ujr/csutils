using System;
using System.Collections.Generic;
using System.Linq;

namespace Sylphe.Utils
{
	public static partial class Algorithms
	{
		// There are two broad classes of clustering algorithms:
		// "assignment" and "hierarchical". Assignment algorithms
		// create a given number of clusters, whereas hierarchical
		// algorithms cluster until some condition is met.

		// EXPERIMENTAL CODE

		#region Lloyd's K-means clustering

		// Motivated by a short presentation of the algorithm in the paper:
		// LINQits: Big Data on Little Clients,
		// by Eric S. Chung, John D. Davis, and Jaewon Lee,
		// presented at ISCA 2013 in Tel Aviv.

		public class Point
		{
			public readonly double X;
			public readonly double Y;

			public Point(double x, double y)
			{
				X = x;
				Y = y;
			}

			public static Point operator +(Point a, Point b)
			{
				return new Point(a.X + b.X, a.Y + b.Y);
			}

			public static Point operator /(Point p, double s)
			{
				return new Point(p.X / s, p.Y / s);
			}

			public static string ToString(Point p, int decimals)
			{
				return string.Format("{0} {1}", Math.Round(p.X, decimals), Math.Round(p.Y, decimals));
			}
		}

		/// <summary>
		/// Compute a key that represents the center nearest to the point.
		/// </summary>
		public static int NearestCenter(Point point, IEnumerable<Point> centers)
		{
			int minIndex = 0, curIndex = 0;
			double minDist = double.MaxValue;

			foreach (var center in centers)
			{
				double dx = point.X - center.X;
				double dy = point.Y - center.Y;
				double curDist = dx * dx + dy * dy;

				if (curDist < minDist)
				{
					minDist = curDist;
					minIndex = curIndex;
				}

				curIndex += 1;
			}

			return minIndex;
		}

		/// <summary>
		/// Compute one iteration of Lloyds k-means clustering:
		/// assign each point in <paramref name="points"/> to its nearest center
		/// in <paramref name="centers"/>, then return new centers in the middle
		/// of the points assigned to them. To get started select k points as
		/// initial cluster centers; then invoke this method repeatedly (just a few
		/// times or until no points are reassigned to a different cluster center).
		/// </summary>
		public static IEnumerable<Point> LloydStep(IEnumerable<Point> points, IEnumerable<Point> centers)
		{
			return points
				.GroupBy(p => NearestCenter(p, centers))
				.Select(g => g.Aggregate((p1, p2) => p1 + p2) / g.Count());
		}

		/// <summary>
		/// K-means clustering assigns N data points to K clusters.
		/// This method computes <paramref name="n"/> iterations
		/// of Lloyd's k-means clustering. Each iteration improves the
		/// result.
		/// </summary>
		public static IEnumerable<Point> LloydClustering(IEnumerable<Point> points, int k, int n = 0)
		{
			if (points == null)
				throw new ArgumentNullException(nameof(points));
			if (k <= 0)
				return Enumerable.Empty<Point>();

			// TODO initial centers should be spread over all input points
			var centers = ReservoirSample(points, k);

			// TODO iterate until stable (no more re-assignments)
			for (int i = 0; i < n; i++)
			{
				centers = LloydStep(points, centers).ToList();
			}

			return centers;
		}

		#endregion

		#region Hierarchical clustering

		/// <summary>
		/// Perform a hierarchical clustering of the given <paramref name="data"/>
		/// points until no more clusters are closer than <paramref name="maxdist"/>.
		/// </summary>
		/// <remarks>
		/// This method runs in O(N) space and O(N**3) time where N is the number
		/// of data points. The <paramref name="distanceFunc"/> is called O(N**3)
		/// times. The <paramref name="mergeFunc"/> is called at most N times.
		/// <para/>
		/// If <paramref name="maxdist"/> is larger than the maximum distance
		/// between any two data points, the result is one cluster comprising
		/// all data points.
		/// <para/>
		/// If points (or clusters) A and B have exactly the same distance as B and C,
		/// only one pair is merged into a cluster, and it is undefined which pair.
		/// Depending on <paramref name="maxdist"/>, the remaining point/cluster
		/// may or may not be merged with the other two.
		/// For example, if dist(A,B)=dist(B,C) but less than <paramref name="maxdist"/>,
		/// they either be clustered (AB)(C) or (A)(BC); if dist(AB,C) is less than
		/// <paramref name="maxdist"/>, the final result is (ABC), as is the case
		/// if dist(A,BC) is less than <paramref name="maxdist"/>.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="data">The list of data points</param>
		/// <param name="maxdist">Stop clustering when nearest pair is further apart</param>
		/// <param name="distanceFunc">Function to compute distance between two points</param>
		/// <param name="mergeFunc">Function to merge (cluster) two points</param>
		/// <returns>The list of clustered data points</returns>
		public static IEnumerable<T> Cluster<T>(
			IEnumerable<T> data, double maxdist,
			Func<T, T, double> distanceFunc,
			Func<T, T, T> mergeFunc) where T : class
		{
			var points = data.ToList();

			int n = points.Count;
			if (n < 2)
			{
				return points;
			}

			int M = n - 1; // max number of merge ops

			for (int m = 0; m < M; m++)
			{
				int i = 0, j = 0;
				double mindist = double.MaxValue;

				for (int k = 0; k < n; k++)
				{
					for (int l = k + 1; l < n; l++)
					{
						double dist = distanceFunc(points[k], points[l]);

						if (dist < mindist)
						{
							i = k;
							j = l;
							mindist = dist;
						}
					}
				}

				if (mindist > maxdist)
				{
					break;
				}

				int u = Math.Min(i, j);
				int v = Math.Max(i, j);

				// Create the cluster in place of the point at the least index:
				points[u] = mergeFunc(points[i], points[j]);
				// Clear the point at the larger index and swap it with the last:
				points[v] = points[n - 1];
				points[n - 1] = null;
				// This decreases the length of the list of points by one:
				n -= 1;
			}

			return points.Where(p => p != null);
		}

		#endregion
	}
}
