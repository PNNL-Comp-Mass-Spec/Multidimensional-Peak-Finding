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

		public List<Point> FindMoreIntenseNeighbors()
		{
			List<Point> pointList = new List<Point>();

			if (this.North != null && this.North.Intensity > this.Intensity) pointList.Add(this.North);
			if (this.South != null && this.South.Intensity > this.Intensity) pointList.Add(this.South);
			if (this.East != null && this.East.Intensity > this.Intensity) pointList.Add(this.East);
			if (this.West != null && this.West.Intensity > this.Intensity) pointList.Add(this.West);
			if (this.NorthEast != null && this.NorthEast.Intensity > this.Intensity) pointList.Add(this.NorthEast);
			if (this.NorthWest != null && this.NorthWest.Intensity > this.Intensity) pointList.Add(this.NorthWest);
			if (this.SouthEast != null && this.SouthEast.Intensity > this.Intensity) pointList.Add(this.SouthEast);
			if (this.SouthWest != null && this.SouthWest.Intensity > this.Intensity) pointList.Add(this.SouthWest);

			return pointList;
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
