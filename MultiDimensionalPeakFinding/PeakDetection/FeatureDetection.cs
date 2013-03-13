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
			pointList = pointList.OrderByDescending(x => x.Intensity);

			List<FeatureBlob> featureList = new List<FeatureBlob>();
			int featureIndex = 0;

			foreach (Point point in pointList)
			{
				// Stop detecting features once we reach 0 intensity points
				if (point.Intensity <= 0) break;

				List<Point> moreIntenseNeighbors = point.FindMoreIntenseNeighbors();

				if(moreIntenseNeighbors.Count == 0)
				{
					// Local maximum and will be the seed of a new blob
					FeatureBlob newFeature = new FeatureBlob(featureIndex);
					newFeature.PointList.Add(point);
					point.FeatureBlob = newFeature;
					featureList.Add(newFeature);
					featureIndex++;

					continue;
				}

				HashSet<FeatureBlob> featureBlobsOfMoreIntenseNeighbors = new HashSet<FeatureBlob>();

				foreach (Point moreIntenseNeighbor in moreIntenseNeighbors)
				{
					// If it has at least one higher neighbor, which is background, then it cannot be part of any blob and must be background.
					if(moreIntenseNeighbor.IsBackground)
					{
						point.IsBackground = true;
						break;
					}

					if (moreIntenseNeighbor.FeatureBlob != null)
					{
						featureBlobsOfMoreIntenseNeighbors.Add(moreIntenseNeighbor.FeatureBlob);

						// If it has more than one higher neighbor and if those higher neighbors are parts of different blobs, then it cannot be a part of any blob, and must be background.
						if(featureBlobsOfMoreIntenseNeighbors.Count > 1)
						{
							point.IsBackground = true;
							break;
						}
					}
				}

				if(!point.IsBackground)
				{
					// It has one or more higher neighbors, which are all parts of the same blob. Then, it must also be a part of that blob.
					FeatureBlob feature = featureBlobsOfMoreIntenseNeighbors.First();
					feature.PointList.Add(point);
					point.FeatureBlob = feature;
				}
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
