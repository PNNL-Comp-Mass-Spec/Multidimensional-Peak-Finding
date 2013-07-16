using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class FeatureBlobStatistics
	{
        public ushort ScanLcStart { get; private set; }
        public byte ScanLcLength { get; private set; }
        public byte ScanLcRepOffset { get; private set; }
        public ushort ScanImsStart { get; private set; }
        public byte ScanImsLength { get; private set; }
        public byte ScanImsRepOffset { get; private set; }
        public float IntensityMax { get; private set; }
        public float SumIntensities { get; private set; }
        public ushort NumPoints { get; private set; }

        // Size: ScanLcMax-ScanLcMin+1
        public float[] LcApexPeakProfile { get; set; }

        // Size: ScanImsMax-ScanImsMin+1
        public float[] ImsApexPeakProfile { get; set; }

		public FeatureBlobStatistics(int scanLcMin, int scanLcMax, int scanLcRep, int scanImsMin, int scanImsMax, int scanImsRep, double intensityMax, double sumIntensities, int numPoints)
		{
            ScanLcStart = (ushort)scanLcMin;

            var scanLcLength = scanLcMax - scanLcMin + 1;
            if (scanLcLength > byte.MaxValue) ScanLcLength = byte.MaxValue;
            else ScanLcLength = (byte)(scanLcLength);

            var scanRepOffset = scanLcRep - scanLcMin;
            if (scanRepOffset > byte.MaxValue) ScanLcRepOffset = byte.MaxValue;
            else ScanLcRepOffset = (byte)(scanRepOffset);

            ScanImsStart = (ushort)scanImsMin;
            var imsLength = scanImsMax - scanImsMin + 1;
            if (imsLength > byte.MaxValue) ScanImsLength = byte.MaxValue;
            else ScanImsLength = (byte)(imsLength);

            var imsRepOffset = scanImsRep - scanImsMin;
            if (imsRepOffset > byte.MaxValue) ScanImsRepOffset = byte.MaxValue;
            else ScanImsRepOffset = (byte)(imsRepOffset);

            IntensityMax = (float)intensityMax;
            SumIntensities = (float)sumIntensities;
            NumPoints = (ushort)numPoints;
		}

        public int ScanLcMin
        {
			get { return ScanLcStart; }
        }

        public int ScanLcMax
        {
			get { return ScanLcStart + ScanLcLength - 1; }
        }

        public int ScanLcRep
        {
			get { return ScanLcStart + ScanLcRepOffset; }
        }

        public int ScanImsMin
        {
			get { return ScanImsStart; }
        }

        public int ScanImsMax
        {
			get { return ScanImsStart + ScanImsLength - 1; }
        }

        public int ScanImsRep
        {
			get { return ScanImsStart + ScanImsRepOffset; }
        }

        public void ComputePeakProfile(Point apex)
        {
            if (apex == null)
                return;

            LcApexPeakProfile = new float[ScanLcLength];

            Point curPoint = apex;
            int index;
            while(curPoint != null && (index = curPoint.ScanLc - ScanLcStart) >= 0)
            {
                LcApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.South;
            }

            curPoint = apex.North;
            while (curPoint != null && (index = curPoint.ScanLc - ScanLcStart) < LcApexPeakProfile.Length)
            {
                LcApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.North;
            } 

            ImsApexPeakProfile = new float[ScanImsLength];

            curPoint = apex;
            while (curPoint != null && (index = curPoint.ScanIms - ScanImsStart) >= 0)
            {
                ImsApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.West;
            }

            curPoint = apex.East;
            while (curPoint != null && (index = curPoint.ScanIms - ScanImsStart) < ImsApexPeakProfile.Length)
            {
                ImsApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.East;
            }
        }
	}
}
