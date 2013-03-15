using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Distributions;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class FeatureDetection
	{
		public static IEnumerable<FeatureBlob> DoWatershedAlgorithm (IEnumerable<Point> pointList)
		{
			// First make sure we are ordered by intensity
			pointList = pointList.Where(x => x.Intensity > 0).OrderByDescending(x => x.Intensity);

			List<FeatureBlob> featureList = new List<FeatureBlob>();
			int featureIndex = 0;

			foreach (Point point in pointList)
			{
				FeatureBlob moreIntenseFeature = null;
				HigherNeighborResult higherNeighborResult = point.FindMoreIntenseNeighbors(out moreIntenseFeature);

				// If no higher features are found, then seed a new Feature
				if (higherNeighborResult == HigherNeighborResult.None)
				{
					// Local maximum and will be the seed of a new blob
					FeatureBlob newFeature = new FeatureBlob(featureIndex);
					newFeature.PointList.Add(point);
					point.FeatureBlob = newFeature;
					featureList.Add(newFeature);
					featureIndex++;

					continue;
				}

				// Background or Multiple Features means that this Point will be Background
				if (higherNeighborResult == HigherNeighborResult.Background || higherNeighborResult == HigherNeighborResult.MultipleFeatures)
				{
					point.IsBackground = true;
					continue;
				}

				// If we get here, then we know the only option is that a single Feature was returned
				moreIntenseFeature.PointList.Add(point);
				point.FeatureBlob = moreIntenseFeature;
			}

			return FilterFeatureList(featureList);
		}

		private static IEnumerable<FeatureBlob> FilterFeatureList(IEnumerable<FeatureBlob> featureList)
		{
			if (!featureList.Any()) return featureList;

			double meanOfMaxIntensities = featureList.Average(x => x.PointList.First().Intensity);
			Gamma gammaDistribution = new Gamma(1, 1 / meanOfMaxIntensities);

			List<FeatureBlob> filteredFeatureList = new List<FeatureBlob>();

			// Frst filter based on intensity p-value
			foreach (FeatureBlob featureBlob in featureList)
			{
				double value = gammaDistribution.CumulativeDistribution(featureBlob.PointList.First().Intensity);
				if (value < 0.995) break;

				filteredFeatureList.Add(featureBlob);
			}

			// Then filter based on number of points
			return filteredFeatureList.Where(x => x.PointList.Count >= 25);
		}
	}
}
