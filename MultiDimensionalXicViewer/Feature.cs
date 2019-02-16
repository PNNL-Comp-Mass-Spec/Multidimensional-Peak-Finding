using System.Drawing;
using MultiDimensionalPeakFinding.PeakDetection;
using Point = System.Drawing.Point;

namespace MultiDimensionalXicViewer
{
    public class Feature
    {
        public ushort ScanLcStart { get; }   // 100
        public byte ScanLcLength { get; }    // 5 -> 100-104
        public byte ScanLcRepOffset { get; } // to highest point
        public ushort ScanImsStart { get; }
        public byte ScanImsLength { get; }
        public byte ScanImsRepOffset { get; }
        public float IntensityMax { get; }
        public float SumIntensities { get; }
        public ushort NumPoints { get; }
        public float[] LcApexPeakProfile { get; }  //ScanLcLength
        public float[] ImsApexPeakProfile { get; } //ScanImsLength

        public Feature(FeatureBlobStatistics featureBlobStatistics)
        {
            ScanLcStart = featureBlobStatistics.ScanLcStart;
            ScanLcLength = featureBlobStatistics.ScanLcLength;
            ScanLcRepOffset = featureBlobStatistics.ScanLcRepOffset;
            ScanImsStart = featureBlobStatistics.ScanImsStart;
            ScanImsLength = featureBlobStatistics.ScanImsLength;
            ScanImsRepOffset = featureBlobStatistics.ScanImsRepOffset;
            IntensityMax = featureBlobStatistics.IntensityMax;
            SumIntensities = featureBlobStatistics.SumIntensities;
            NumPoints = featureBlobStatistics.NumPoints;
            LcApexPeakProfile = featureBlobStatistics.LcApexPeakProfile;
            ImsApexPeakProfile = featureBlobStatistics.ImsApexPeakProfile;
        }

        public Rectangle GetBoundary()
        {
            return new Rectangle(ScanLcStart, ScanImsStart, ScanLcLength, ScanImsLength);
        }

        public Point GetHighestPoint()
        {
            return new Point(ScanLcStart+ScanLcRepOffset, ScanImsStart+ScanImsRepOffset);
        }

        public override string ToString()
        {
            return string.Format(
                "LC: [{0},{1}], IMS: [{2},{3}], Apex: [{4},{5}] SumIntensities: {6}, NumPoints: {7}",
                ScanLcStart,
                (ScanLcStart + ScanLcLength - 1),
                ScanImsStart,
                ScanImsStart + ScanImsLength - 1,
                ScanLcStart + ScanLcRepOffset,
                ScanImsStart + ScanImsRepOffset,
                SumIntensities,
                NumPoints
                );
        }
    }
}
