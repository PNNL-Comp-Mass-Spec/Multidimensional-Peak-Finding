using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MultiDimensionalPeakFinding.PeakDetection;
using PNNLOmics.Generic;

namespace MultiDimensionalPeakFinding.PeakCorrelation
{
	public class FeatureCorrelator
	{
		public static double CorrelateFeatures(FeatureBlob referenceFeature, FeatureBlob featureToTest)
		{
			double refMaxIntensity = 0;
			int refScanLcMin = int.MaxValue;
			int refScanLcMax = 0;
			int refScanImsMin = int.MaxValue;
			int refScanImsMax = 0;
			int refScanLcRep = 0;
			int refScanImsRep = 0;

			foreach (Point point in referenceFeature.PointList)
			{
				int scanIms = point.ScanIms;
				int scanLc = point.ScanLc;
				double intensity = point.Intensity;

				if (scanIms < refScanImsMin) refScanImsMin = scanIms;
				if (scanIms > refScanImsMax) refScanImsMax = scanIms;
				if (scanLc < refScanLcMin) refScanLcMin = scanLc;
				if (scanLc > refScanLcMax) refScanLcMax = scanLc;

				if(intensity > refMaxIntensity)
				{
					refMaxIntensity = intensity;
					refScanLcRep = scanLc;
					refScanImsRep = scanIms;
				}
			}

			double testMaxIntensity = 0;
			int testScanLcMin = int.MaxValue;
			int testScanLcMax = 0;
			int testScanImsMin = int.MaxValue;
			int testScanImsMax = 0;
			int testScanLcRep = 0;
			int testScanImsRep = 0;

			foreach (Point point in featureToTest.PointList)
			{
				int scanIms = point.ScanIms;
				int scanLc = point.ScanLc;
				double intensity = point.Intensity;

				if (scanIms < testScanImsMin) testScanImsMin = scanIms;
				if (scanIms > testScanImsMax) testScanImsMax = scanIms;
				if (scanLc < testScanLcMin) testScanLcMin = scanLc;
				if (scanLc > testScanLcMax) testScanLcMax = scanLc;

				if (intensity > testMaxIntensity)
				{
					testMaxIntensity = intensity;
					testScanLcRep = scanLc;
					testScanImsRep = scanIms;
				}
			}

			Console.WriteLine("RefLcRep = " + refScanLcRep + "\tTestLcRep = " + testScanLcRep + "\tRefImsRep = " + refScanImsRep + "\tTestImsRep = " + testScanImsRep + "\tRefLc = " + refScanLcMin + " - " + refScanLcMax + "\tTestLc = " + testScanLcMin + " - " + testScanLcMax + "\tRefIms = " + refScanImsMin + " - " + refScanImsMax + "\tTestIms = " + testScanImsMin + " - " + testScanImsMax + "\tRefIntensity = " + refMaxIntensity + "\tTestIntensity = " + testMaxIntensity);

			return 0;
		}

		public static double CorrelateFeaturesLinearRegression(FeatureBlob referencesFeature, FeatureBlob featureToTest)
		{
			AnonymousComparer<Point> pointComparer = new AnonymousComparer<Point>((x, y) => x.ScanLc != y.ScanLc ? x.ScanLc.CompareTo(y.ScanLc) : x.ScanIms.CompareTo(y.ScanIms));

			int numPoints = referencesFeature.PointList.Count;

			double[,] inputData = new double[numPoints, 2];
			List<Point> testPointList = featureToTest.PointList;
			testPointList.Sort(pointComparer);

			int index = 0;
			double sumOfTestValues = 0;

			// Get the corresponding reference:test intensity values
			foreach (Point referencePoint in referencesFeature.PointList)
			{
				inputData[index, 0] = referencePoint.Intensity;

				int binarySearchResult = testPointList.BinarySearch(referencePoint, pointComparer);
				if (binarySearchResult < 0)
				{
					inputData[index, 1] = 0;
				}
				else
				{
					double intensity = testPointList[binarySearchResult].Intensity;
					inputData[index, 1] = intensity;
					sumOfTestValues += intensity;
				}

				index++;
			}

			int numIndependentVariables = 1;
			int info;
			alglib.linearmodel linearModel;
			alglib.lrreport regressionReport;
			alglib.lrbuild(inputData, numPoints, numIndependentVariables, out info, out linearModel, out regressionReport);

			double[] regressionLineInfo;
			alglib.lrunpack(linearModel, out regressionLineInfo, out numIndependentVariables);

			double slope = regressionLineInfo[0];
			double intercept = regressionLineInfo[1];
			double rSquared = 0;

			double averageOfTestValues = sumOfTestValues / numPoints;
			double sumOfSquaredMeanResiduals = 0;
			double sumOfSquaredResiduals = 0;

			for(int i = 0; i < numPoints; i++)
			{
				double referenceValue = inputData[i, 0];
				double testValue = inputData[i, 1];

				double calculatedTestValue = alglib.lrprocess(linearModel, new double[] { referenceValue });
				
				double residual = testValue - calculatedTestValue;
				sumOfSquaredResiduals += (residual * residual);

				double meanResidual = testValue - averageOfTestValues;
				sumOfSquaredMeanResiduals += (meanResidual * meanResidual);
			}

			if(sumOfSquaredMeanResiduals > 0)
			{
				rSquared = 1 - (sumOfSquaredResiduals / sumOfSquaredMeanResiduals);
			}

			return rSquared;
		}

		public static double CorrelateFeaturesUsingLc(FeatureBlob referenceFeature, FeatureBlob featureToTest)
		{
			FeatureBlobStatistics referenceStatistics = referenceFeature.Statistics;
			FeatureBlobStatistics testStatistics = featureToTest.Statistics;

			int referenceScanLcMin = referenceStatistics.ScanLcMin;
			int referenceScanLcMax = referenceStatistics.ScanLcMax;
			int testScanLcMin = testStatistics.ScanLcMin;
			int testScanLcMax = testStatistics.ScanLcMax;

			// If these features do not overlap, then just return 0
			if (testScanLcMin > referenceScanLcMax || testScanLcMax < referenceScanLcMin) return 0;

			int scanLcOffset = referenceScanLcMin - testScanLcMin;

			double[] referenceLcProfile = Array.ConvertAll(referenceStatistics.LcApexPeakProfile, x => (double)x);
			double[] testLcProfile = new double[referenceScanLcMax - referenceScanLcMin + 1];
			float[] testLcProfileAsFloat = testStatistics.LcApexPeakProfile;

			int numPointsInTestLcProfile = testLcProfileAsFloat.Length;

			for (int i = 0; i < referenceLcProfile.Length; i++)
			{
				int testLcProfileIndex = i + scanLcOffset;
				if (testLcProfileIndex < 0) continue;
				if (testLcProfileIndex >= numPointsInTestLcProfile) break;

				testLcProfile[i] = testLcProfileAsFloat[testLcProfileIndex];
			}

			double slope, intercept, rSquared;
			GetLinearRegression(referenceLcProfile, testLcProfile, out slope, out intercept, out rSquared);

			return rSquared;
		}

		public static double CorrelateFeaturesUsingIms(FeatureBlob referenceFeature, FeatureBlob featureToTest)
		{
			FeatureBlobStatistics referenceStatistics = referenceFeature.Statistics;
			FeatureBlobStatistics testStatistics = featureToTest.Statistics;

			int referenceScanImsMin = referenceStatistics.ScanImsMin;
			int referenceScanImsMax = referenceStatistics.ScanImsMax;
			int testScanImsMin = testStatistics.ScanImsMin;
			int testScanImsMax = testStatistics.ScanImsMax;

			// If these features do not overlap, then just return 0
			if (testScanImsMin > referenceScanImsMax || testScanImsMax < referenceScanImsMin) return 0;

			int scanImsOffset = referenceScanImsMin - testScanImsMin;

			double[] referenceImsProfile = Array.ConvertAll(referenceStatistics.ImsApexPeakProfile, x => (double)x);
			double[] testImsProfile = new double[referenceScanImsMax - referenceScanImsMin + 1];
			float[] testImsProfileAsFloat = testStatistics.ImsApexPeakProfile;

			int numPointsInTestImsProfile = testImsProfileAsFloat.Length;

			for (int i = 0; i < referenceImsProfile.Length; i++)
			{
				int testImsProfileIndex = i + scanImsOffset;
				if (testImsProfileIndex < 0) continue;
				if (testImsProfileIndex >= numPointsInTestImsProfile) break;

				testImsProfile[i] = testImsProfileAsFloat[testImsProfileIndex];
			}

			double slope, intercept, rSquared;
			GetLinearRegression(referenceImsProfile, testImsProfile, out slope, out intercept, out rSquared);

			return rSquared;
		}

		private static void GetLinearRegression(double[] xvals, double[] yvals, out double slope, out double intercept, out double rsquaredVal)
		{
			double[,] inputData = new double[xvals.Length, 2];
			double sumOfYValues = 0;

			for (int i = 0; i < xvals.Length; i++)
			{
				double xValue = xvals[i];
				double yValue = yvals[i];

				inputData[i, 0] = xValue;
				inputData[i, 1] = yValue;

				sumOfYValues += yValue;
			}

			int numIndependentVariables = 1;
			int numPoints = yvals.Length;

			alglib.linearmodel linearModel;
			int info;
			alglib.lrreport regressionReport;
			alglib.lrbuild(inputData, numPoints, numIndependentVariables, out info, out linearModel, out regressionReport);

			double[] regressionLineInfo;

			try
			{
				alglib.lrunpack(linearModel, out regressionLineInfo, out numIndependentVariables);

			}
			catch (Exception ex)
			{
				slope = -99999999;
				intercept = -9999999;
				rsquaredVal = -9999999;
				return;
			}

			slope = regressionLineInfo[0];
			intercept = regressionLineInfo[1];

			double averageY = sumOfYValues / numPoints;
			double sumOfSquaredMeanResiduals = 0;
			double sumOfSquaredResiduals = 0;

			for (int i = 0; i < xvals.Length; i++)
			{
				double xValue = xvals[i];
				double yValue = yvals[i];

				double calculatedYValue = alglib.lrprocess(linearModel, new double[] { xValue });

				double residual = yValue - calculatedYValue;
				sumOfSquaredResiduals += (residual * residual);

				double meanResidual = yValue - averageY;
				sumOfSquaredMeanResiduals += (meanResidual * meanResidual);
			}

			//check for sum=0 
			if (sumOfSquaredMeanResiduals == 0)
			{
				rsquaredVal = 0;
			}
			else
			{
				rsquaredVal = 1 - (sumOfSquaredResiduals / sumOfSquaredMeanResiduals);
			}
		}
	}
}
