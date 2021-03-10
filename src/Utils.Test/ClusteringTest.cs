using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Sylphe.Utils.Test
{
	public class ClusteringTest
	{
		private readonly ITestOutputHelper _output;

		public ClusteringTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void LloydClusteringTrial()
		{
			var points = FromCoords(
				49, 51,
				49, 69,
				49, 89,
				163, 49,
				163, 67,
				163, 84,
				79, 123,
				78, 132,
				95, 155,
				115, 156,
				133, 122,
				134, 141);

			var centers = Algorithms.LloydClustering(points, 3, 10).ToList();

			_output.WriteLine("Clusters: {0}", string.Join(", ", centers.Select(p => Algorithms.Point.ToString(p, 1))));

			Assert.Equal(3, centers.Count);
		}

		[Fact]
		public void HierarchicalClusteringTrial()
		{
			var points = FromCoords(
				49, 51,
				49, 69,
				49, 89,
				163, 49,
				163, 67,
				163, 84,
				79, 123,
				78, 132,
				95, 155,
				115, 156,
				133, 122,
				134, 141);

			const double maxDist = 50*50; // in DistanceFunc units
			var clusters = Algorithms.Cluster(points, maxDist, DistanceFunc, MergeFunc);

			_output.WriteLine("Clusters: {0}", string.Join(", ", clusters.Select(p => Algorithms.Point.ToString(p, 1))));
		}

		private double DistanceFunc(Algorithms.Point a, Algorithms.Point b)
		{
			double dx = a.X - b.X;
			double dy = a.Y - b.Y;
			return dx*dx + dy*dy;
		}

		private Algorithms.Point MergeFunc(Algorithms.Point a, Algorithms.Point b)
		{
			return (a + b) / 2;
		}

		private static IEnumerable<Algorithms.Point> FromCoords(params double[] coords)
		{
			if (coords.Length % 2 != 0)
				throw new ArgumentException("Number of coords must be even");
			for (int i = 0; i < coords.Length; )
			{
				double x = coords[i++];
				double y = coords[i++];
				yield return new Algorithms.Point(x, y);
			}
		}
	}
}
