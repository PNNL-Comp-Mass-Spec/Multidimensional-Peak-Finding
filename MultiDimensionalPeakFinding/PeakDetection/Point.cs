using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class Point : IComparable<Point>
	{
		public int ScanLc { get; private set; }
		public int ScanIms { get; private set; }
		public double Intensity { get; private set; }

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

		public Point(int scanLc, int scanIms, double intensity)
		{
			ScanLc = scanLc;
			ScanIms = scanIms;
			Intensity = intensity;
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
			return other.ScanLc == ScanLc && other.ScanIms == ScanIms;
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
				return (ScanLc*397) ^ ScanIms;
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
			if (this.ScanLc != other.ScanLc) return this.ScanLc.CompareTo(other.ScanLc);
			return this.ScanIms.CompareTo(other.ScanIms);
		}
	}
}
