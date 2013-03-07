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
	}
}
