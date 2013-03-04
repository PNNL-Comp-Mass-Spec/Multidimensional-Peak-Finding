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

		public double[,] GetXic(double targetMz, double ppmTolerance, DataReader.FrameType frameType)
		{
			return m_uimfReader.GetXic(targetMz, ppmTolerance, frameType);
		}
	}
}
