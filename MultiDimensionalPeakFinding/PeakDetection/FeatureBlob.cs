using System.Collections.Generic;

namespace MultiDimensionalPeakFinding.PeakDetection
{
    public class FeatureBlob
    {
        private FeatureBlobStatistics m_statistics;

        public int Id { get; }
        public List<Point> PointList { get; }

        public FeatureBlobStatistics Statistics
        {
            get
            {
                if (m_statistics == null) CalculateStatistics();
                return m_statistics;
            }
            private set => m_statistics = value;
        }

        public FeatureBlob(int id)
        {
            Id = id;
            PointList = new List<Point>();
            Statistics = null;
        }

        public double[,] ToArray()
        {
            if (Statistics == null) CalculateStatistics();

            var scanImsMin = Statistics.ScanImsMin;
            var scanImsMax = Statistics.ScanImsMax;
            var scanLcMin = Statistics.ScanLcMin;
            var scanLcMax = Statistics.ScanLcMax;

            var returnArray = new double[scanLcMax - scanLcMin + 1, scanImsMax - scanImsMin + 1];

            foreach (var point in PointList)
            {
                returnArray[point.ScanLcIndex, point.ScanImsIndex] = point.Intensity;
            }

            return returnArray;
        }

        public FeatureBlobStatistics CalculateStatistics()
        {
            double maxIntensity = 0;
            double sumIntensities = 0;
            var scanLcMin = int.MaxValue;
            var scanLcMax = 0;
            var scanImsMin = int.MaxValue;
            var scanImsMax = 0;
            var scanLcRep = 0;
            var scanImsRep = 0;
            Point apex = null;
            var isSaturated = false;

            foreach (var point in PointList)
            {
                var scanIms = point.ScanIms;
                var scanLc = point.ScanLc;
                var intensity = point.Intensity;

                if (scanIms < scanImsMin) scanImsMin = scanIms;
                if (scanIms > scanImsMax) scanImsMax = scanIms;
                if (scanLc < scanLcMin) scanLcMin = scanLc;
                if (scanLc > scanLcMax) scanLcMax = scanLc;

                sumIntensities += intensity;
                if (intensity > maxIntensity)
                {
                    maxIntensity = intensity;
                    scanLcRep = scanLc;
                    scanImsRep = scanIms;
                    apex = point;
                }

                if (point.IsSaturated) isSaturated = true;
            }

            var statistics = new FeatureBlobStatistics(scanLcMin, scanLcMax, scanLcRep, scanImsMin, scanImsMax, scanImsRep, maxIntensity, sumIntensities, PointList.Count, isSaturated);
            statistics.ComputePeakProfile(apex);
            Statistics = statistics;

            return statistics;
        }

        public bool Equals(FeatureBlob other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Id == Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FeatureBlob)) return false;
            return Equals((FeatureBlob) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(FeatureBlob left, FeatureBlob right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FeatureBlob left, FeatureBlob right)
        {
            return !Equals(left, right);
        }
    }
}
