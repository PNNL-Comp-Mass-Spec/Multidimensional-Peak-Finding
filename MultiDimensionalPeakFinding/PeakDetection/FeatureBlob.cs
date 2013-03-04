using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class FeatureBlob
	{
		public int Id { get; private set; }
		public List<Point> PointList { get; private set; }

		public FeatureBlob(int id)
		{
			this.Id = id;
			this.PointList = new List<Point>();
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
