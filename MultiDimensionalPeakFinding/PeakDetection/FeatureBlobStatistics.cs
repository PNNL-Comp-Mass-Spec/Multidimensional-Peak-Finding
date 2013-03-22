using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class FeatureBlobStatistics
	{
        // All boundaries are inclusive
		public int ScanLcMin { get; private set; }
		public int ScanLcMax { get; private set; }
		public int ScanLcRep { get; private set; }
		public int ScanImsMin { get; private set; }
		public int ScanImsMax { get; private set; }
		public int ScanImsRep { get; private set; }
		public double IntensityMax { get; private set; }
        public double SumIntensities { get; private set; }
        public int NumPoints { get; private set; }

        // Size: ScanLcMax-ScanLcMin+1
        public float[] LcApexPeakProfile { get; set; }

        // Size: ScanImsMax-ScanImsMin+1
        public float[] ImsApexPeakProfile { get; set; }

		public FeatureBlobStatistics(int scanLcMin, int scanLcMax, int scanLcRep, int scanImsMin, int scanImsMax, int scanImsRep, double intensityMax, double sumIntensities, int numPoints)
		{
			ScanLcMin = scanLcMin;
			ScanLcMax = scanLcMax;
			ScanLcRep = scanLcRep;
			ScanImsMin = scanImsMin;
			ScanImsMax = scanImsMax;
			ScanImsRep = scanImsRep;
			IntensityMax = intensityMax;
		    SumIntensities = sumIntensities;
		    NumPoints = numPoints;
		}

        public void ComputePeakProfile(Point apex)
        {
            if (apex == null)
                return;

            LcApexPeakProfile = new float[ScanLcMax-ScanLcMin+1];

            Point curPoint = apex;
            int index;
            while(curPoint != null && (index = curPoint.ScanLc - ScanLcMin) >= 0)
            {
                LcApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.South;
            }

            curPoint = apex.North;
            while (curPoint != null && (index = curPoint.ScanLc - ScanLcMin) < LcApexPeakProfile.Length)
            {
                LcApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.North;
            } 

            ImsApexPeakProfile = new float[ScanImsMax-ScanImsMin+1];

            curPoint = apex;
            while (curPoint != null && (index = curPoint.ScanIms - ScanImsMin) >= 0)
            {
                ImsApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.West;
            }

            curPoint = apex.East;
            while (curPoint != null && (index = curPoint.ScanIms - ScanImsMin) < ImsApexPeakProfile.Length)
            {
                ImsApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.East;
            }
        }
	}
}
