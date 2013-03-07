using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class FeatureBlobStatistics
	{
		public int ScanLcMin { get; private set; }
		public int ScanLcMax { get; private set; }
		public int ScanLcRep { get; private set; }
		public int ScanImsMin { get; private set; }
		public int ScanImsMax { get; private set; }
		public int ScanImsRep { get; private set; }
		public double IntensityMax { get; private set; }

		public FeatureBlobStatistics(int scanLcMin, int scanLcMax, int scanLcRep, int scanImsMin, int scanImsMax, int scanImsRep, double intensityMax)
		{
			ScanLcMin = scanLcMin;
			ScanLcMax = scanLcMax;
			ScanLcRep = scanLcRep;
			ScanImsMin = scanImsMin;
			ScanImsMax = scanImsMax;
			ScanImsRep = scanImsRep;
			IntensityMax = intensityMax;
		}
	}
}
