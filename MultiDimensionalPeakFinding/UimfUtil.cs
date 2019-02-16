using System;
using System.Collections.Generic;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding
{
    public class UimfUtil
    {
        public DataReader UimfReader { get; }

        public UimfUtil(string fileLocation)
        {
            UimfReader = new DataReader(fileLocation);
        }

        public double[,] GetXicAsArray(double targetMz, double tolerance, UIMFData.FrameType frameType, DataReader.ToleranceType toleranceType)
        {
            return UimfReader.GetXicAsArray(targetMz, tolerance, frameType, toleranceType);
        }

        public double[,] GetXicAsArray(double targetMz, double tolerance, UIMFData.FrameType frameType, int scanLcMin, int scanLcMax, int scanImsMin, int scanImsMax, DataReader.ToleranceType toleranceType)
        {
            return UimfReader.GetXicAsArray(targetMz, tolerance, scanLcMin, scanLcMax, scanImsMin, scanImsMax, frameType, toleranceType);
        }

        public double[,] GetXicAsArray(int targetBin, UIMFData.FrameType frameType)
        {
            return UimfReader.GetXicAsArray(targetBin, frameType);
        }

        public List<IntensityPoint> GetXic(double targetMz, double tolerance, UIMFData.FrameType frameType, DataReader.ToleranceType toleranceType)
        {
            return UimfReader.GetXic(targetMz, tolerance, frameType, toleranceType);
        }

        public List<IntensityPoint> GetXic(double targetMz, double tolerance, UIMFData.FrameType frameType, int scanLcMin, int scanLcMax, int scanImsMin, int scanImsMax, DataReader.ToleranceType toleranceType)
        {
            return UimfReader.GetXic(targetMz, tolerance, scanLcMin, scanLcMax, scanImsMin, scanImsMax, frameType, toleranceType);
        }

        public List<IntensityPoint> GetXic(int targetBin, UIMFData.FrameType frameType)
        {
            return UimfReader.GetXic(targetBin, frameType);
        }

        public int GetNumberOfBins()
        {
            return UimfReader.GetGlobalParams().Bins;
        }

        public double GetMzFromBin(int bin)
        {
            var globalParameters = UimfReader.GetGlobalParams();
            var frameParameters = UimfReader.GetFrameParams(1);
            return UIMFData.ConvertBinToMZ(frameParameters.CalibrationSlope, frameParameters.CalibrationIntercept, globalParameters.BinWidth, globalParameters.TOFCorrectionTime, bin);
        }

        public int GetBinFromMz(double mz)
        {
            var globalParameters = UimfReader.GetGlobalParams();
            var frameParameters = UimfReader.GetFrameParams(1);
            return (int)Math.Round(UIMFData.GetBinClosestToMZ(frameParameters.CalibrationSlope, frameParameters.CalibrationIntercept, globalParameters.BinWidth, globalParameters.TOFCorrectionTime, mz));
        }

        public bool DoesContainBinCentricData()
        {
            return UimfReader.DoesContainBinCentricData();
        }

        /// <summary>
        /// Returns the saturation level (maximum intensity value) for a single unit of measurement
        /// Instead use GetSaturationLevel(int detectorBits)
        /// </summary>
        /// <returns>saturation level</returns>
        [Obsolete("This assumes the detector is 8 bits; newer detectors used in 2014 are 12 bits")]
        public int GetSaturationLevel()
        {
            return UimfReader.GetSaturationLevel(8);
        }

         /// <summary>
        /// Returns the saturation level (maximum intensity value) for a single unit of measurement
        /// </summary>
        /// <param name="detectorBits">Number of bits used by the detector (usually 8 or 12)</param>
        /// <returns>saturation level</returns>
        public int GetSaturationLevel(int detectorBits)
        {
            return UimfReader.GetSaturationLevel(detectorBits);
        }

    }
}
