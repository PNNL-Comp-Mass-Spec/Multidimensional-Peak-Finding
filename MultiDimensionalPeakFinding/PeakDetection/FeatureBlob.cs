using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PNNLOmics.Algorithms.Solvers.LevenburgMarquadt;
using PNNLOmics.Algorithms.Solvers.LevenburgMarquadt.BasisFunctions;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class FeatureBlob
	{
		private FeatureBlobStatistics m_statistics;

		public int Id { get; private set; }
		public List<Point> PointList { get; private set; }

		public FeatureBlobStatistics Statistics
		{
			get
			{
				if (m_statistics == null) CalculateStatistics();
				return m_statistics;
			} 
			private set { m_statistics = value; }
		}

		public FeatureBlob(int id)
		{
			this.Id = id;
			this.PointList = new List<Point>();
			this.Statistics = null;
		}

		public double[,] ToArray()
		{
			if (this.Statistics == null) this.CalculateStatistics();

			int scanImsMin = this.Statistics.ScanImsMin;
			int scanImsMax = this.Statistics.ScanImsMax;
			int scanLcMin = this.Statistics.ScanLcMin;
			int scanLcMax = this.Statistics.ScanLcMax;

			double[,] returnArray = new double[scanLcMax - scanLcMin + 1, scanImsMax - scanImsMin + 1];

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
			int scanLcMin = int.MaxValue;
			int scanLcMax = 0;
			int scanImsMin = int.MaxValue;
			int scanImsMax = 0;
			int scanLcRep = 0;
			int scanImsRep = 0;
		    Point apex = null;

			foreach (Point point in this.PointList)
			{
				int scanIms = point.ScanIms;
				int scanLc = point.ScanLc;
				double intensity = point.Intensity;

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
			}

			FeatureBlobStatistics statistics = new FeatureBlobStatistics(scanLcMin, scanLcMax, scanLcRep, scanImsMin, scanImsMax, scanImsRep, maxIntensity, sumIntensities, this.PointList.Count);
            statistics.ComputePeakProfile(apex);
			this.Statistics = statistics;

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
