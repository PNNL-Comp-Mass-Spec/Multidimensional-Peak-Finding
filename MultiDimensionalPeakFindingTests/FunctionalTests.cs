using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakCorrelation;
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

			double targetMz = 582.32181703760114;
			double ppmTolerance = 25;

			double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
		}

		[Test]
		public void Test3DSmooth()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

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

			double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref intensityBlock);

			WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
		}

		[Test]
		public void TestDoWaterShedAlgorithm()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 582.32181703760114;
			double ppmTolerance = 25;

			double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(11, 2);
			smoother.Smooth(ref intensityBlock);

			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
			IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);
			foreach (FeatureBlob featureBlob in featureList)
			{
				Point mostIntensePoint = featureBlob.PointList.First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
			}
			Console.WriteLine("******************************************************");

			List<IntensityPoint> intensityPointList = uimfUtil.GetXic(targetMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			IEnumerable<Point> newPointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
			smoother.Smooth(ref newPointList);
			IEnumerable<FeatureBlob> newFeatureList = FeatureDetection.DoWatershedAlgorithm(newPointList);
			foreach (FeatureBlob featureBlob in newFeatureList)
			{
				Point mostIntensePoint = featureBlob.PointList.First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
			}
		}

		[Test]
		public void TestDoWaterShedAlgorithmOutput()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double targetMz = 643.27094937;
			double ppmTolerance = 50;

			TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw.csv");

			double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

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

			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
			IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

			Console.WriteLine(featureList.Count());

			featureList = featureList.OrderByDescending(x => x.PointList.Count);

			TextWriter intensityWriter = new StreamWriter("intensities.csv");

			foreach (FeatureBlob featureBlob in featureList)
			{
				Point mostIntensePoint = featureBlob.PointList.First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
				intensityWriter.WriteLine(mostIntensePoint.Intensity.ToString());
			}
		}

		[Test]
		public void TestDoWaterShedAlgorithmPrecursorAndFragments()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double parentMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] parentIntensityBlock = uimfUtil.GetXicAsArray(parentMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref parentIntensityBlock);

			IEnumerable<Point> parentPointList = WaterShedMapUtil.BuildWatershedMap(parentIntensityBlock, 0, 0);
			FeatureBlob parentFeature = FeatureDetection.DoWatershedAlgorithm(parentPointList).First();

			FeatureBlobStatistics statistics = parentFeature.Statistics;
			int scanLcMin = statistics.ScanLcMin;
			int scanLcMax = statistics.ScanLcMax;
			int scanImsMin = statistics.ScanImsMin;
			int scanImsMax = statistics.ScanImsMax;

			using (TextReader fragmentReader = new StreamReader(@"..\..\..\testFiles\OneFragment.csv"))
			{
				string line = "";
				while ((line = fragmentReader.ReadLine()) != null)
				{
					string mzString = line.Trim();
					double targetMz = double.Parse(mzString);

					TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw" + targetMz + ".csv");

					double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS2, scanLcMin, scanLcMax, scanImsMin, scanImsMax, DataReader.ToleranceType.PPM);

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

					smoother.Smooth(ref intensityBlock);

					TextWriter smoothedWriter = new StreamWriter("smoothedRaw" + targetMz + ".csv");
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

					IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, scanLcMin, scanImsMin);
					IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

					Console.WriteLine("******************************************************");
					Console.WriteLine("targetMz = " + targetMz);

					foreach (FeatureBlob featureBlob in featureList)
					{
						Point mostIntensePoint = featureBlob.PointList.OrderByDescending(x => x.Intensity).First();
						Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
					}
				}
			}
		}

		[Test]
		public void TestFragmentCorrelation()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);

			double parentMz = 643.27094937;
			double ppmTolerance = 50;

			double[,] parentIntensityBlock = uimfUtil.GetXicAsArray(parentMz, ppmTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);
			smoother.Smooth(ref parentIntensityBlock);

			IEnumerable<Point> parentPointList = WaterShedMapUtil.BuildWatershedMap(parentIntensityBlock, 0, 0);
			FeatureBlob parentFeature = FeatureDetection.DoWatershedAlgorithm(parentPointList).First();

			using (TextReader fragmentReader = new StreamReader(@"..\..\..\testFiles\fragments.csv"))
			//using (TextReader fragmentReader = new StreamReader(@"..\..\..\testFiles\OneFragment.csv"))
			{
				string line = "";
				while ((line = fragmentReader.ReadLine()) != null)
				{
					string mzString = line.Trim();
					double targetMz = double.Parse(mzString);

					TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw" + targetMz + ".csv");

					double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, DataReader.FrameType.MS2, DataReader.ToleranceType.PPM);

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

					smoother.Smooth(ref intensityBlock);

					TextWriter smoothedWriter = new StreamWriter("smoothedRaw" + targetMz + ".csv");
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

					IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
					IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

					featureList = featureList.Where(x => x.PointList.Count > 50).OrderByDescending(x => x.PointList.Count);

					Console.WriteLine("******************************************************");
					Console.WriteLine("targetMz = " + targetMz);

					foreach (FeatureBlob featureBlob in featureList)
					{
						double rSquared = FeatureCorrelator.CorrelateFeatures(parentFeature, featureBlob); 
						//Point mostIntensePoint = featureBlob.PointList.OrderByDescending(x => x.Intensity).First();
						//Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity + "\tRSquared = " + rSquared);
					}
				}
			}
		}

		[Test]
		public void TestDoWaterShedAlgorithmByBin()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);
			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);

			int bin = 73009;
			List<IntensityPoint> intensityPointList = uimfUtil.GetXic(bin, DataReader.FrameType.MS1);

			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);

			IEnumerable<FeatureBlob> preSmoothedFeatureList = FeatureDetection.DoWatershedAlgorithm(pointList);
			Console.WriteLine(DateTime.Now + "\tBefore Smoothing:\tNumPoints = " + pointList.Count() + "\tNumFeatures = " + preSmoothedFeatureList.Count());
			foreach (FeatureBlob featureBlob in preSmoothedFeatureList)
			{
				Point mostIntensePoint = featureBlob.PointList.First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
			}
			Console.WriteLine("******************************************************");

			IEnumerable<Point> newPointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
			smoother.Smooth(ref newPointList);
			IEnumerable<FeatureBlob> smoothedFeatureList = FeatureDetection.DoWatershedAlgorithm(newPointList);
			Console.WriteLine(DateTime.Now + "\tAfter Smoothing:\tNumPoints = " + newPointList.Count() + "\tNumFeatures = " + smoothedFeatureList.Count());
			foreach (FeatureBlob featureBlob in smoothedFeatureList)
			{
				Point mostIntensePoint = featureBlob.PointList.First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
			}
		}

		[Test]
		public void TestDoWaterShedAlgorithmAllBins()
		{
			string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
			UimfUtil uimfUtil = new UimfUtil(fileLocation);
			int numberOfBins = uimfUtil.GetNumberOfBins();

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);

			for (int i = 73009; i <= 84000; i++)
			{
				double mz = uimfUtil.GetMzFromBin(i);
				List<IntensityPoint> intensityPointList = uimfUtil.GetXic(mz, 25, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
				//List<IntensityPoint> intensityPointList = uimfUtil.GetXic(i, DataReader.FrameType.MS1);

				//SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(9, 2);
				//smoother.Smooth(ref intensityBlock);

				//int boundX = intensityBlock.GetUpperBound(0);
				//int boundY = intensityBlock.GetUpperBound(1);

				//TextWriter smoothedWriter = new StreamWriter("smoothedRaw.csv");
				//for (int j = 0; j < boundX; j++)
				//{
				//    StringBuilder row = new StringBuilder();
				//    for (int k = 0; k < boundY; k++)
				//    {
				//        row.Append(intensityBlock[j, k] + ",");
				//    }
				//    smoothedWriter.WriteLine(row.ToString());
				//}

				//smoothedWriter.Close();

				IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
				smoother.Smooth(ref pointList);
				IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				Console.WriteLine(DateTime.Now + "\tBin = " + i + "\tNumPoints = " + pointList.Count() + "\tNumFeatures = " + featureList.Count());
			}
		}

		[Test]
		public void TestFakeSaturatedPoints()
		{
			List<IntensityPoint> intensityPointList = new List<IntensityPoint>();

			for(int i = 0; i < 5; i++)
			{
				for(int j = 0; j < 5; j++)
				{
					IntensityPoint point = new IntensityPoint(i, j, 8925);
					intensityPointList.Add(point);
				}
			}

			IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
			IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

			Console.WriteLine(DateTime.Now + "\tNumPoints = " + pointList.Count() + "\tNumFeatures = " + featureList.Count());
			foreach (FeatureBlob featureBlob in featureList)
			{
				Point mostIntensePoint = featureBlob.PointList.First();
				Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
			}
		}

        [Test]
        public void TestComputingApexProfiles()
        {
            string fileLocation = @"..\..\..\testFiles\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            UimfUtil uimfUtil = new UimfUtil(fileLocation);
            SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(5, 2);

            double targetMz = 964.40334;
            double tolerance = 20;

            List<IntensityPoint> intensityPointList = uimfUtil.GetXic(targetMz, tolerance, DataReader.FrameType.MS1,
                                                                      DataReader.ToleranceType.PPM);
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            smoother.Smooth(ref pointList);
            IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);
            IEnumerable<FeatureBlobStatistics> featureBlobStatList = featureBlobs.Select(featureBlob => featureBlob.Statistics).ToList();
            foreach(FeatureBlobStatistics f in featureBlobStatList)
            {
                Console.WriteLine(
                    "LC: [{0},{1}], IMS: [{2},{3}], Apex: [{4},{5}] SumIntensities: {6}, NumPoints: {7}",
                    f.ScanLcMin,
                    f.ScanLcMax,
                    f.ScanImsMin,
                    f.ScanImsMax,
                    f.ScanLcRep,
                    f.ScanImsRep,
                    f.SumIntensities,
                    f.NumPoints
                    );
                Console.WriteLine("LC Apex profile");
                Console.WriteLine(string.Join(",", f.LcApexPeakProfile));
                Console.WriteLine("IMS Apex profile");
                Console.WriteLine(string.Join(",", f.ImsApexPeakProfile));
            }
        }
	}
}
