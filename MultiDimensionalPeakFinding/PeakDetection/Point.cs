using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
    public class Point : IComparable<Point>
    {
        public int ScanLc
        {
            get { return this.ScanLcIndex + this.ScanLcOffset; }
        }

        public int ScanIms
        {
            get { return this.ScanImsIndex + this.ScanImsOffset; }
        }

        public int ScanLcIndex { get; private set; }
        public int ScanLcOffset { get; private set; }
        public int ScanImsIndex { get; private set; }
        public int ScanImsOffset { get; private set; }

        public double Intensity { get; set; }

        public Point North { get; set; }
        public Point South { get; set; }
        public Point East { get; set; }
        public Point West { get; set; }
        public Point NorthEast { get; set; }
        public Point NorthWest { get; set; }
        public Point SouthEast { get; set; }
        public Point SouthWest { get; set; }

        public bool IsBackground { get; set; }
        public bool IsSaturated { get; set; }
        public FeatureBlob FeatureBlob { get; set; }

        public Point(int scanLcIndex, int scanLcOffset, int scanImsIndex, int scanImsOffset, double intensity, bool isSaturated = false)
        {
            ScanLcIndex = scanLcIndex;
            ScanLcOffset = scanLcOffset;
            ScanImsIndex = scanImsIndex;
            ScanImsOffset = scanImsOffset;
            Intensity = intensity;
            IsBackground = false;
            IsSaturated = isSaturated;
        }

        public HigherNeighborResult FindMoreIntenseNeighbors(out FeatureBlob feature)
        {
            int featureCount = 0;
            FeatureBlob savedFeature = null;
            feature = null;

            if (North != null && North.Intensity >= Intensity)
            {
                if(North.IsBackground) return HigherNeighborResult.Background;
                savedFeature = North.FeatureBlob;
            }
            if (South != null && South.Intensity >= Intensity)
            {
                if (South.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (South.FeatureBlob != null && savedFeature != South.FeatureBlob)
                    {
                        if (South.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = South.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = South.FeatureBlob;
                }
            }
            if (East != null && East.Intensity >= Intensity)
            {
                if (East.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (East.FeatureBlob != null && savedFeature != East.FeatureBlob)
                    {
                        if (East.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = East.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = East.FeatureBlob;
                }
            }
            if (West != null && West.Intensity >= Intensity)
            {
                if (West.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (West.FeatureBlob != null && savedFeature != West.FeatureBlob)
                    {
                        if (West.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = West.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = West.FeatureBlob;
                }
            }
            if (NorthEast != null && NorthEast.Intensity >= Intensity)
            {
                if (NorthEast.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (NorthEast.FeatureBlob != null && savedFeature != NorthEast.FeatureBlob)
                    {
                        if (NorthEast.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = NorthEast.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = NorthEast.FeatureBlob;
                }
            }
            if (NorthWest != null && NorthWest.Intensity >= Intensity)
            {
                if (NorthWest.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (NorthWest.FeatureBlob != null && savedFeature != NorthWest.FeatureBlob)
                    {
                        if (NorthWest.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = NorthWest.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = NorthWest.FeatureBlob;
                }
            }
            if (SouthEast != null && SouthEast.Intensity >= Intensity)
            {
                if (SouthEast.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (SouthEast.FeatureBlob != null && savedFeature != SouthEast.FeatureBlob)
                    {
                        if (SouthEast.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = SouthEast.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = SouthEast.FeatureBlob;
                }
            }
            if (SouthWest != null && SouthWest.Intensity >= Intensity)
            {
                if (SouthWest.IsBackground) return HigherNeighborResult.Background;
                if (savedFeature != null)
                {
                    if (SouthWest.FeatureBlob != null && savedFeature != SouthWest.FeatureBlob)
                    {
                        if (SouthWest.FeatureBlob.PointList.Count > savedFeature.PointList.Count)
                        {
                            savedFeature = SouthWest.FeatureBlob;
                            featureCount++;
                        }
                        //return HigherNeighborResult.MultipleFeatures;
                    }
                }
                else
                {
                    savedFeature = SouthWest.FeatureBlob;
                }
            }

            if (savedFeature == null)
            {
                return HigherNeighborResult.None;
            }

            feature = savedFeature;

            if(featureCount > 1)
            {
                return HigherNeighborResult.MultipleFeatures;
            }

            return HigherNeighborResult.OneFeature;
        }

        public bool Equals(Point other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.ScanLcIndex == ScanLcIndex && other.ScanImsIndex == ScanImsIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Point)) return false;
            return Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ScanLcIndex * 397) ^ ScanImsIndex;
            }
        }

        public static bool operator ==(Point left, Point right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(Point other)
        {
            if (ScanLcIndex != other.ScanLcIndex) return ScanLcIndex.CompareTo(other.ScanLcIndex);
            return ScanImsIndex.CompareTo(other.ScanImsIndex);
        }

        public override string ToString()
        {
            return string.Format("ScanLcIndex: {0}, ScanImsIndex: {1}, Intensity: {2}", ScanLcIndex, ScanImsIndex, Intensity);
        }
    }
}
