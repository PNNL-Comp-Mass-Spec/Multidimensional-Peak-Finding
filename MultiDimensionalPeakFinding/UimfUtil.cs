using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding
{
	public class UimfUtil
	{
		private DataReader m_uimfReader;

		public UimfUtil(string fileLocation)
		{
			m_uimfReader = new DataReader(fileLocation);
		}

		public double[,] GetXic(double targetMz, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			return m_uimfReader.GetXic(targetMz, tolerance, frameType, toleranceType);
		}

		public double[,] GetXic(double targetMz, double tolerance, DataReader.FrameType frameType, int scanLcMin, int scanLcMax, int scanImsMin, int scanImsMax, DataReader.ToleranceType toleranceType)
		{
			return m_uimfReader.GetXic(targetMz, tolerance, scanLcMin, scanLcMax, scanImsMin, scanImsMax, frameType, toleranceType);
		}

		public double[,] GetXic(int targetBin, DataReader.FrameType frameType)
		{
			return m_uimfReader.GetXic(targetBin, frameType);
		}

		public int GetNumberOfBins()
		{
			return m_uimfReader.GetGlobalParameters().Bins;
		}

		public double GetMzFromBin(int bin)
		{
			GlobalParameters globalParameters = m_uimfReader.GetGlobalParameters();
			FrameParameters frameParameters = m_uimfReader.GetFrameParameters(1);
			return DataReader.ConvertBinToMZ(frameParameters.CalibrationSlope, frameParameters.CalibrationIntercept, globalParameters.BinWidth, globalParameters.TOFCorrectionTime, bin);
		}

		public int GetBinFromMz(double mz)
		{
			GlobalParameters globalParameters = m_uimfReader.GetGlobalParameters();
			FrameParameters frameParameters = m_uimfReader.GetFrameParameters(1);
			return (int)Math.Round(DataReader.GetBinClosestToMZ(frameParameters.CalibrationSlope, frameParameters.CalibrationIntercept, globalParameters.BinWidth, globalParameters.TOFCorrectionTime, mz));
		}
	}
}
