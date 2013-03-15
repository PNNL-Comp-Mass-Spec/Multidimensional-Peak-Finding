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
		public FeatureBlob FeatureBlob { get; set; }

		public Point(int scanLcIndex, int scanLcOffset, int scanImsIndex, int scanImsOffset, double intensity)
		{
			this.ScanLcIndex = scanLcIndex;
			this.ScanLcOffset = scanLcOffset;
			this.ScanImsIndex = scanImsIndex;
			this.ScanImsOffset = scanImsOffset;
			this.Intensity = intensity;
			this.IsBackground = false;
		}

		public HigherNeighborResult FindMoreIntenseNeighbors(out FeatureBlob feature)
		{
			FeatureBlob savedFeature = null;
			feature = null;

			if (this.North != null && this.North.Intensity > this.Intensity)
			{
				if(this.North.IsBackground) return HigherNeighborResult.Background;
				savedFeature = this.North.FeatureBlob;
			}
			if (this.South != null && this.South.Intensity > this.Intensity)
			{
				if (this.South.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if(savedFeature != this.South.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.South.FeatureBlob;
				}
			}
			if (this.East != null && this.East.Intensity > this.Intensity)
			{
				if (this.East.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if (savedFeature != this.East.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.East.FeatureBlob;
				}
			}
			if (this.West != null && this.West.Intensity > this.Intensity)
			{
				if (this.West.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if (savedFeature != this.West.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.West.FeatureBlob;
				}
			}
			if (this.NorthEast != null && this.NorthEast.Intensity > this.Intensity)
			{
				if (this.NorthEast.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if (savedFeature != this.NorthEast.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.NorthEast.FeatureBlob;
				}
			}
			if (this.NorthWest != null && this.NorthWest.Intensity > this.Intensity)
			{
				if (this.NorthWest.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if (savedFeature != this.NorthWest.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.NorthWest.FeatureBlob;
				}
			}
			if (this.SouthEast != null && this.SouthEast.Intensity > this.Intensity)
			{
				if (this.SouthEast.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if (savedFeature != this.SouthEast.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.SouthEast.FeatureBlob;
				}
			}
			if (this.SouthWest != null && this.SouthWest.Intensity > this.Intensity)
			{
				if (this.SouthWest.IsBackground) return HigherNeighborResult.Background;
				if (savedFeature != null)
				{
					if (savedFeature != this.SouthWest.FeatureBlob) return HigherNeighborResult.MultipleFeatures;
				}
				else
				{
					savedFeature = this.SouthWest.FeatureBlob;
				}
			}

			if (savedFeature == null)
			{
				return HigherNeighborResult.None;
			}
		
			feature = savedFeature;
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
			if (this.ScanLcIndex != other.ScanLcIndex) return this.ScanLcIndex.CompareTo(other.ScanLcIndex);
			return this.ScanImsIndex.CompareTo(other.ScanImsIndex);
		}
	}
}
