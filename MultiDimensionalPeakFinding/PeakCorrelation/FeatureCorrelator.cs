using System;
using System.Collections.Generic;
using MultiDimensionalPeakFinding.PeakDetection;

namespace MultiDimensionalPeakFinding.PeakCorrelation
{
    public class FeatureCorrelator
    {
        public static double CorrelateFeatures(FeatureBlob referenceFeature, FeatureBlob featureToTest)
        {
            double refMaxIntensity = 0;
            var refScanLcMin = int.MaxValue;
            var refScanLcMax = 0;
            var refScanImsMin = int.MaxValue;
            var refScanImsMax = 0;
            var refScanLcRep = 0;
            var refScanImsRep = 0;

            foreach (var point in referenceFeature.PointList)
            {
                var scanIms = point.ScanIms;
                var scanLc = point.ScanLc;
                var intensity = point.Intensity;

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
            var testScanLcMin = int.MaxValue;
            var testScanLcMax = 0;
            var testScanImsMin = int.MaxValue;
            var testScanImsMax = 0;
            var testScanLcRep = 0;
            var testScanImsRep = 0;

            foreach (var point in featureToTest.PointList)
            {
                var scanIms = point.ScanIms;
                var scanLc = point.ScanLc;
                var intensity = point.Intensity;

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
            var pointComparer = new AnonymousComparer<Point>((x, y) => x.ScanLc != y.ScanLc ? x.ScanLc.CompareTo(y.ScanLc) : x.ScanIms.CompareTo(y.ScanIms));

            var numPoints = referencesFeature.PointList.Count;

            var inputData = new double[numPoints, 2];
            var testPointList = featureToTest.PointList;
            testPointList.Sort(pointComparer);

            var index = 0;
            double sumOfTestValues = 0;

            // Get the corresponding reference:test intensity values
            foreach (var referencePoint in referencesFeature.PointList)
            {
                inputData[index, 0] = referencePoint.Intensity;

                var binarySearchResult = testPointList.BinarySearch(referencePoint, pointComparer);
                if (binarySearchResult < 0)
                {
                    inputData[index, 1] = 0;
                }
                else
                {
                    var intensity = testPointList[binarySearchResult].Intensity;
                    inputData[index, 1] = intensity;
                    sumOfTestValues += intensity;
                }

                index++;
            }

            var numIndependentVariables = 1;
            int info;
            alglib.linearmodel linearModel;
            alglib.lrreport regressionReport;
            alglib.lrbuild(inputData, numPoints, numIndependentVariables, out info, out linearModel, out regressionReport);

            double[] regressionLineInfo;
            alglib.lrunpack(linearModel, out regressionLineInfo, out numIndependentVariables);

            var slope = regressionLineInfo[0];
            var intercept = regressionLineInfo[1];
            double rSquared = 0;

            var averageOfTestValues = sumOfTestValues / numPoints;
            double sumOfSquaredMeanResiduals = 0;
            double sumOfSquaredResiduals = 0;

            for(var i = 0; i < numPoints; i++)
            {
                var referenceValue = inputData[i, 0];
                var testValue = inputData[i, 1];

                var calculatedTestValue = alglib.lrprocess(linearModel, new double[] { referenceValue });

                var residual = testValue - calculatedTestValue;
                sumOfSquaredResiduals += (residual * residual);

                var meanResidual = testValue - averageOfTestValues;
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
            var referenceStatistics = referenceFeature.Statistics;
            var testStatistics = featureToTest.Statistics;

            var referenceScanLcMin = referenceStatistics.ScanLcMin;
            var referenceScanLcMax = referenceStatistics.ScanLcMax;
            var testScanLcMin = testStatistics.ScanLcMin;
            var testScanLcMax = testStatistics.ScanLcMax;

            // If these features do not overlap, then just return 0
            if (testScanLcMin > referenceScanLcMax || testScanLcMax < referenceScanLcMin) return 0;

            var scanLcOffset = referenceScanLcMin - testScanLcMin;

            var referenceLcProfile = Array.ConvertAll(referenceStatistics.LcApexPeakProfile, x => (double)x);
            var testLcProfile = new double[referenceScanLcMax - referenceScanLcMin + 1];
            var testLcProfileAsFloat = testStatistics.LcApexPeakProfile;

            var numPointsInTestLcProfile = testLcProfileAsFloat.Length;

            for (var i = 0; i < referenceLcProfile.Length; i++)
            {
                var testLcProfileIndex = i + scanLcOffset;
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
            var referenceStatistics = referenceFeature.Statistics;
            var testStatistics = featureToTest.Statistics;

            var referenceScanImsMin = referenceStatistics.ScanImsMin;
            var referenceScanImsMax = referenceStatistics.ScanImsMax;
            var testScanImsMin = testStatistics.ScanImsMin;
            var testScanImsMax = testStatistics.ScanImsMax;

            // If these features do not overlap, then just return 0
            if (testScanImsMin > referenceScanImsMax || testScanImsMax < referenceScanImsMin) return 0;

            var scanImsOffset = referenceScanImsMin - testScanImsMin;

            var referenceImsProfile = Array.ConvertAll(referenceStatistics.ImsApexPeakProfile, x => (double)x);
            var testImsProfile = new double[referenceScanImsMax - referenceScanImsMin + 1];
            var testImsProfileAsFloat = testStatistics.ImsApexPeakProfile;

            var numPointsInTestImsProfile = testImsProfileAsFloat.Length;

            for (var i = 0; i < referenceImsProfile.Length; i++)
            {
                var testImsProfileIndex = i + scanImsOffset;
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
            var inputData = new double[xvals.Length, 2];
            double sumOfYValues = 0;

            for (var i = 0; i < xvals.Length; i++)
            {
                var xValue = xvals[i];
                var yValue = yvals[i];

                inputData[i, 0] = xValue;
                inputData[i, 1] = yValue;

                sumOfYValues += yValue;
            }

            var numIndependentVariables = 1;
            var numPoints = yvals.Length;

            alglib.linearmodel linearModel;
            int info;
            alglib.lrreport regressionReport;
            alglib.lrbuild(inputData, numPoints, numIndependentVariables, out info, out linearModel, out regressionReport);

            double[] regressionLineInfo;

            try
            {
                alglib.lrunpack(linearModel, out regressionLineInfo, out numIndependentVariables);

            }
            catch (Exception)
            {
                slope = -99999999;
                intercept = -9999999;
                rsquaredVal = -9999999;
                return;
            }

            slope = regressionLineInfo[0];
            intercept = regressionLineInfo[1];

            var averageY = sumOfYValues / numPoints;
            double sumOfSquaredMeanResiduals = 0;
            double sumOfSquaredResiduals = 0;

            for (var i = 0; i < xvals.Length; i++)
            {
                var xValue = xvals[i];
                var yValue = yvals[i];

                var calculatedYValue = alglib.lrprocess(linearModel, new double[] { xValue });

                var residual = yValue - calculatedYValue;
                sumOfSquaredResiduals += (residual * residual);

                var meanResidual = yValue - averageY;
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
