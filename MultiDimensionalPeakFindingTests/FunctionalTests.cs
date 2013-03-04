using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakDetection;
using NUnit.Framework;
using UIMFLibrary;

namespace MultiDimensionalPeakFindingTests
{
	public class FunctionalTests
	{
		[Test]
		public void TestGetXic()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] intensityBlock = uimfUtil.GetXic(targetMz, ppmTolerance, DataReader.FrameType.MS1);
		}

		[Test]
		public void Test3DSmooth()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] intensityBlock = uimfUtil.GetXic(targetMz, ppmTolerance, DataReader.FrameType.MS1);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref intensityBlock);
		}

		[Test]
		public void TestBuildWaterShedMap()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] intensityBlock = uimfUtil.GetXic(targetMz, ppmTolerance, DataReader.FrameType.MS1);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref intensityBlock);

			WaterShedMapUtil.BuildWatershedMap(intensityBlock);
		}

		[Test]
		public void TestDoWaterShedAlgorithm()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] intensityBlock = uimfUtil.GetXic(targetMz, ppmTolerance, DataReader.FrameType.MS1);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref intensityBlock);

			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
			IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);
		}

		[Test]
		public void TestDoWaterShedAlgorithmOutput()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw.csv");

			double[,] intensityBlock = uimfUtil.GetXic(targetMz, ppmTolerance, DataReader.FrameType.MS1);

			int boundX = intensityBlock.GetUpperBound(0);
			int boundY = intensityBlock.GetUpperBound(1);

			for (int i = 0; i < boundX; i++)
			{
				StringBuilder row = new StringBuilder();
				for (int j = 0; j < boundY; j++)
				{
					row.Append(intensityBlock[i, j] + ",");
				}
				unsmoothedWriter.WriteLine(row.ToString());
			}

			unsmoothedWriter.Close();

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref intensityBlock);

			TextWriter smoothedWriter = new StreamWriter("smoothedRaw.csv");
			for (int i = 0; i < boundX; i++)
			{
				StringBuilder row = new StringBuilder();
				for (int j = 0; j < boundY; j++)
				{
					row.Append(intensityBlock[i, j] + ",");
				}
				smoothedWriter.WriteLine(row.ToString());
			}

			smoothedWriter.Close();

			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
			IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

			Console.WriteLine(featureList.Count());

			featureList = featureList.OrderByDescending(x => x.PointList.Count);

			foreach (FeatureBlob featureBlob in featureList)
			{
				Point mostIntensePoint = featureBlob.PointList.OrderByDescending(x => x.Intensity).First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
			}
		}
	}
}
