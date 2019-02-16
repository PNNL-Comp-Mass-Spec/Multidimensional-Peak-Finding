using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;

namespace MultiDimensionalPeakFinding.PeakDetection
{
    public class FeatureDetection
    {
        public static IEnumerable<FeatureBlob> DoWatershedAlgorithm (IEnumerable<Point> pointList)
        {
            // First make sure we are ordered by intensity
            pointList = pointList.Where(x => x.Intensity > 0).OrderByDescending(x => x.Intensity);

            var featureList = new List<FeatureBlob>();
            var featureIndex = 0;

            foreach (var point in pointList)
            {
                var higherNeighborResult = point.FindMoreIntenseNeighbors(out var moreIntenseFeature);

                // If no higher features are found, then seed a new Feature
                if (higherNeighborResult == HigherNeighborResult.None)
                {
                    // Local maximum and will be the seed of a new blob
                    var newFeature = new FeatureBlob(featureIndex);
                    newFeature.PointList.Add(point);
                    point.FeatureBlob = newFeature;
                    featureList.Add(newFeature);
                    featureIndex++;

                    continue;
                }

                // Background or Multiple Features means that this Point will be Background
                if (higherNeighborResult == HigherNeighborResult.Background)
                {
                    point.IsBackground = true;
                    continue;
                }

                // If we get here, then we know the only option is that a single Feature was returned
                moreIntenseFeature.PointList.Add(point);
                point.FeatureBlob = moreIntenseFeature;
            }

            return featureList;
        }

        public static IEnumerable<FeatureBlob> FilterFeatureList(IList<FeatureBlob> featureList, double filterLevel)
        {
            if (!featureList.Any()) return featureList;

            var meanOfMaxIntensities = featureList.Average(x => x.PointList.First().Intensity);
            var gammaDistribution = new Gamma(1, 1 / meanOfMaxIntensities);

            var filteredFeatureList = new List<FeatureBlob>();

            // First filter based on intensity p-value
            foreach (var featureBlob in featureList)
            {
                var value = gammaDistribution.CumulativeDistribution(featureBlob.PointList.First().Intensity);
                if (value < filterLevel) break;

                filteredFeatureList.Add(featureBlob);
            }

            // Then filter based on number of points
            return filteredFeatureList.Where(x => x.PointList.Count >= 1);
        }
    }
}
