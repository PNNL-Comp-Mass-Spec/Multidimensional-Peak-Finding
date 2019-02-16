namespace MultiDimensionalPeakFinding.PeakDetection
{
    public class FeatureBlobStatistics
    {
        public ushort ScanLcStart { get; }
        public byte ScanLcLength { get; }
        public byte ScanLcRepOffset { get; }
        public ushort ScanImsStart { get; }
        public byte ScanImsLength { get; }
        public byte ScanImsRepOffset { get; }
        public float IntensityMax { get; }
        public float SumIntensities { get; }
        public ushort NumPoints { get; }
        public bool IsSaturated { get; }

        // Size: ScanLcMax - ScanLcMin + 1
        public float[] LcApexPeakProfile { get; set; }

        // Size: ScanImsMax - ScanImsMin + 1
        public float[] ImsApexPeakProfile { get; set; }

        public FeatureBlobStatistics(int scanLcMin, int scanLcMax, int scanLcRep, int scanImsMin, int scanImsMax, int scanImsRep, double intensityMax, double sumIntensities, int numPoints, bool isSaturated)
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
            IsSaturated = isSaturated;
        }

        public int ScanLcMin => ScanLcStart;

        public int ScanLcMax => ScanLcStart + ScanLcLength - 1;

        public int ScanLcRep => ScanLcStart + ScanLcRepOffset;

        public int ScanImsMin => ScanImsStart;

        public int ScanImsMax => ScanImsStart + ScanImsLength - 1;

        public int ScanImsRep => ScanImsStart + ScanImsRepOffset;

        public void ComputePeakProfile(Point apex)
        {
            if (apex == null)
                return;

            LcApexPeakProfile = new float[ScanLcLength];

            var curPoint = apex;
            int index;
            while(curPoint != null && curPoint != curPoint.South && (index = curPoint.ScanLc - ScanLcStart) >= 0)
            {
                LcApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.South;
            }

            curPoint = apex.North;
            while (curPoint != null && curPoint != curPoint.North && (index = curPoint.ScanLc - ScanLcStart) < LcApexPeakProfile.Length)
            {
                LcApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.North;
            }

            ImsApexPeakProfile = new float[ScanImsLength];

            curPoint = apex;
            while (curPoint != null && curPoint != curPoint.West && (index = curPoint.ScanIms - ScanImsStart) >= 0)
            {
                ImsApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.West;
            }

            curPoint = apex.East;
            while (curPoint != null && curPoint != curPoint.East && (index = curPoint.ScanIms - ScanImsStart) < ImsApexPeakProfile.Length)
            {
                ImsApexPeakProfile[index] = (float)curPoint.Intensity;
                curPoint = curPoint.East;
            }
        }
    }
}
