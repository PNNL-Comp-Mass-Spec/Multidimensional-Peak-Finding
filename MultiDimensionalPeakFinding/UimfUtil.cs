using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding
{
	public class UimfUtil
	{
		public DataReader UimfReader { get; private set; }

		public UimfUtil(string fileLocation)
		{
			UimfReader = new DataReader(fileLocation);
		}

		public double[,] GetXicAsArray(double targetMz, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			return UimfReader.GetXicAsArray(targetMz, tolerance, frameType, toleranceType);
		}

		public double[,] GetXicAsArray(double targetMz, double tolerance, DataReader.FrameType frameType, int scanLcMin, int scanLcMax, int scanImsMin, int scanImsMax, DataReader.ToleranceType toleranceType)
		{
			return UimfReader.GetXicAsArray(targetMz, tolerance, scanLcMin, scanLcMax, scanImsMin, scanImsMax, frameType, toleranceType);
		}

		public double[,] GetXicAsArray(int targetBin, DataReader.FrameType frameType)
		{
			return UimfReader.GetXicAsArray(targetBin, frameType);
		}

		public List<IntensityPoint> GetXic(double targetMz, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			return UimfReader.GetXic(targetMz, tolerance, frameType, toleranceType);
		}

		public List<IntensityPoint> GetXic(double targetMz, double tolerance, DataReader.FrameType frameType, int scanLcMin, int scanLcMax, int scanImsMin, int scanImsMax, DataReader.ToleranceType toleranceType)
		{
			return UimfReader.GetXic(targetMz, tolerance, scanLcMin, scanLcMax, scanImsMin, scanImsMax, frameType, toleranceType);
		}

		public List<IntensityPoint> GetXic(int targetBin, DataReader.FrameType frameType)
		{
			return UimfReader.GetXic(targetBin, frameType);
		}

		public int GetNumberOfBins()
		{
			return UimfReader.GetGlobalParameters().Bins;
		}

		public double GetMzFromBin(int bin)
		{
			GlobalParameters globalParameters = UimfReader.GetGlobalParameters();
			FrameParameters frameParameters = UimfReader.GetFrameParameters(1);
			return DataReader.ConvertBinToMZ(frameParameters.CalibrationSlope, frameParameters.CalibrationIntercept, globalParameters.BinWidth, globalParameters.TOFCorrectionTime, bin);
		}

		public int GetBinFromMz(double mz)
		{
			GlobalParameters globalParameters = UimfReader.GetGlobalParameters();
			FrameParameters frameParameters = UimfReader.GetFrameParameters(1);
			return (int)Math.Round(DataReader.GetBinClosestToMZ(frameParameters.CalibrationSlope, frameParameters.CalibrationIntercept, globalParameters.BinWidth, globalParameters.TOFCorrectionTime, mz));
		}

		public bool DoesContainBinCentricData()
		{
			return UimfReader.DoesContainBinCentricData();
		}

		public int GetSaturationLevel()
		{
			return UimfReader.GetSaturationLevel();
		}
	}
}
