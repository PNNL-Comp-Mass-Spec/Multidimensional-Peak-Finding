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

			TextWriter textWriter = new StreamWriter("output.csv");

			// Get the corresponding reference:test intensity values
			foreach (Point referencePoint in referencesFeature.PointList)
			{
				string values = referencePoint.Intensity + ",";
				string output = "LC = " + referencePoint.ScanLc + "\tIMS = " + referencePoint.ScanIms + "\tRef = " + referencePoint.Intensity + "\tTest = ";
				inputData[index, 0] = referencePoint.Intensity;

				int binarySearchResult = testPointList.BinarySearch(referencePoint, pointComparer);
				if (binarySearchResult < 0)
				{
					inputData[index, 1] = 0;
					output += "0";
					values += "0";
				}
				else
				{
					double intensity = testPointList[binarySearchResult].Intensity;
					inputData[index, 1] = intensity;
					sumOfTestValues += intensity;
					output += intensity.ToString();
					values += intensity.ToString();
				}

				Console.WriteLine(output);
				textWriter.WriteLine(values);

				index++;
			}

			textWriter.Close();

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
	}
}
