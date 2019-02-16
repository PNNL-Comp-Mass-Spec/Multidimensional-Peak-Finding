using System;
using System.Collections.Generic;
using System.Globalization;
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
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var targetMz = 582.32181703760114;
            double ppmTolerance = 25;

            var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            Console.WriteLine(intensityBlock.Length);

            Assert.AreEqual(270000, intensityBlock.Length);
        }

        [Test]
        public void Test3DSmooth()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var targetMz = 643.27094937;
            double ppmTolerance = 50;

            var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var smoother = new SavitzkyGolaySmoother(5, 2);
            smoother.Smooth(ref intensityBlock);

            Console.WriteLine(intensityBlock.Length);

            Assert.AreEqual(270000, intensityBlock.Length);
            Assert.AreEqual(intensityBlock[112, 120], 2.32898, 0.0001);
            Assert.AreEqual(intensityBlock[183, 122], 48.82041, 0.0001);

        }

        [Test]
        public void TestBuildWaterShedMap()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var targetMz = 643.27094937;
            double ppmTolerance = 50;

            var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var smoother = new SavitzkyGolaySmoother(5, 2);
            smoother.Smooth(ref intensityBlock);

            var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0).ToList();

            Console.WriteLine(intensityBlock.Length);


            Assert.AreEqual(270000, intensityBlock.Length);
            Assert.AreEqual(12969, pointList.Count);

            Assert.AreEqual(pointList[1000].Intensity, 14.844082, 0.0001);
            Assert.AreEqual(pointList[5000].Intensity, 5.760, 0.0001);
        }

        [Test]
        public void TestDoWaterShedAlgorithm()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var targetMz = 582.32181703760114;
            double ppmTolerance = 25;

            var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var smoother = new SavitzkyGolaySmoother(11, 2);
            smoother.Smooth(ref intensityBlock);

            var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
            var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);
            foreach (var featureBlob in featureList)
            {
                var mostIntensePoint = featureBlob.PointList.First();
                Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
            }
            Console.WriteLine("******************************************************");

            var intensityPointList = uimfUtil.GetXic(targetMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var newPointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            smoother.Smooth(ref newPointList);
            var newFeatureList = FeatureDetection.DoWatershedAlgorithm(newPointList);
            foreach (var featureBlob in newFeatureList)
            {
                var mostIntensePoint = featureBlob.PointList.First();
                Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
            }
        }

        [Test]
        public void TestDoWaterShedAlgorithmOutput()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var targetMz = 643.27094937;
            double ppmTolerance = 50;

            TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw.csv");

            var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var boundX = intensityBlock.GetUpperBound(0);
            var boundY = intensityBlock.GetUpperBound(1);

            for (var i = 0; i < boundX; i++)
            {
                var row = new StringBuilder();
                for (var j = 0; j < boundY; j++)
                {
                    row.Append(intensityBlock[i, j] + ",");
                }
                unsmoothedWriter.WriteLine(row.ToString());
            }

            unsmoothedWriter.Close();

            var smoother = new SavitzkyGolaySmoother(5, 2);
            smoother.Smooth(ref intensityBlock);

            TextWriter smoothedWriter = new StreamWriter("smoothedRaw.csv");
            for (var i = 0; i < boundX; i++)
            {
                var row = new StringBuilder();
                for (var j = 0; j < boundY; j++)
                {
                    row.Append(intensityBlock[i, j] + ",");
                }
                smoothedWriter.WriteLine(row.ToString());
            }

            smoothedWriter.Close();

            var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
            var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

            Console.WriteLine(featureList.Count());

            featureList = featureList.OrderByDescending(x => x.PointList.Count);

            TextWriter intensityWriter = new StreamWriter("intensities.csv");

            foreach (var featureBlob in featureList)
            {
                var mostIntensePoint = featureBlob.PointList.First();
                Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
                intensityWriter.WriteLine(mostIntensePoint.Intensity.ToString(CultureInfo.InvariantCulture));
            }
        }

        [Test]
        public void TestDoWaterShedAlgorithmPrecursorAndFragments()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var parentMz = 643.27094937;
            double ppmTolerance = 50;

            var parentIntensityBlock = uimfUtil.GetXicAsArray(parentMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var smoother = new SavitzkyGolaySmoother(5, 2);
            smoother.Smooth(ref parentIntensityBlock);

            // ReSharper disable RedundantArgumentDefaultValue
            var parentPointList = WaterShedMapUtil.BuildWatershedMap(parentIntensityBlock, 0, 0);
            // ReSharper restore RedundantArgumentDefaultValue

            var parentFeature = FeatureDetection.DoWatershedAlgorithm(parentPointList).First();

            var statistics = parentFeature.Statistics;
            var scanLcMin = statistics.ScanLcMin;
            var scanLcMax = statistics.ScanLcMax;
            var scanImsMin = statistics.ScanImsMin;
            var scanImsMax = statistics.ScanImsMax;

            using (var fragmentReader = new StreamReader(@"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\testFiles\OneFragment.csv"))
            {
                while (!fragmentReader.EndOfStream)
                {
                    var dataLine = fragmentReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var mzString = dataLine.Trim();
                    var targetMz = double.Parse(mzString);

                    TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw" + targetMz + ".csv");

                    var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS2, scanLcMin, scanLcMax, scanImsMin, scanImsMax, DataReader.ToleranceType.PPM);

                    var boundX = intensityBlock.GetUpperBound(0);
                    var boundY = intensityBlock.GetUpperBound(1);

                    for (var i = 0; i < boundX; i++)
                    {
                        var row = new StringBuilder();
                        for (var j = 0; j < boundY; j++)
                        {
                            row.Append(intensityBlock[i, j] + ",");
                        }
                        unsmoothedWriter.WriteLine(row.ToString());
                    }

                    unsmoothedWriter.Close();

                    smoother.Smooth(ref intensityBlock);

                    TextWriter smoothedWriter = new StreamWriter("smoothedRaw" + targetMz + ".csv");
                    for (var i = 0; i < boundX; i++)
                    {
                        var row = new StringBuilder();
                        for (var j = 0; j < boundY; j++)
                        {
                            row.Append(intensityBlock[i, j] + ",");
                        }
                        smoothedWriter.WriteLine(row.ToString());
                    }

                    smoothedWriter.Close();

                    var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, scanLcMin, scanImsMin).ToList();
                    var featureList = FeatureDetection.DoWatershedAlgorithm(pointList).ToList();

                    Assert.AreEqual(430, pointList.Count);
                    Assert.AreEqual(37, featureList.Count);

                    Console.WriteLine("******************************************************");
                    Console.WriteLine("targetMz = " + targetMz);

                    for (var i = 0; i < featureList.Count; i++)
                    {
                        var featureBlob = featureList[i];
                        var mostIntensePoint = featureBlob.PointList.OrderByDescending(x => x.Intensity).First();
                        Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
                        if (i != 1)
                            continue;

                        // Num Points = 34	LC = 138	IMS = 122	Intensity = 79.5697959183673
                        Assert.AreEqual(34, featureBlob.PointList.Count);
                        Assert.AreEqual(138, mostIntensePoint.ScanLc);
                        Assert.AreEqual(122, mostIntensePoint.ScanIms);
                        Assert.AreEqual(79.569796, mostIntensePoint.Intensity, 0.0001);
                    }
                }
            }
        }

        [Test]
        public void TestFragmentCorrelation()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            var parentMz = 643.27094937;
            double ppmTolerance = 50;

            var parentIntensityBlock = uimfUtil.GetXicAsArray(parentMz, ppmTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            var smoother = new SavitzkyGolaySmoother(5, 2);
            smoother.Smooth(ref parentIntensityBlock);

            // ReSharper disable RedundantArgumentDefaultValue
            var parentPointList = WaterShedMapUtil.BuildWatershedMap(parentIntensityBlock, 0, 0);
            // ReSharper restore RedundantArgumentDefaultValue

            var parentFeature = FeatureDetection.DoWatershedAlgorithm(parentPointList).First();

            using (var fragmentReader = new StreamReader(@"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\testFiles\fragments.csv"))
            {
                while (!fragmentReader.EndOfStream)
                {
                    var dataLine = fragmentReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var mzString = dataLine.Trim();
                    var targetMz = double.Parse(mzString);

                    TextWriter unsmoothedWriter = new StreamWriter("unsmoothedRaw" + targetMz + ".csv");

                    var intensityBlock = uimfUtil.GetXicAsArray(targetMz, ppmTolerance, UIMFData.FrameType.MS2, DataReader.ToleranceType.PPM);

                    var boundX = intensityBlock.GetUpperBound(0);
                    var boundY = intensityBlock.GetUpperBound(1);

                    for (var i = 0; i < boundX; i++)
                    {
                        var row = new StringBuilder();
                        for (var j = 0; j < boundY; j++)
                        {
                            row.Append(intensityBlock[i, j] + ",");
                        }
                        unsmoothedWriter.WriteLine(row.ToString());
                    }

                    unsmoothedWriter.Close();

                    smoother.Smooth(ref intensityBlock);

                    TextWriter smoothedWriter = new StreamWriter("smoothedRaw" + targetMz + ".csv");
                    for (var i = 0; i < boundX; i++)
                    {
                        var row = new StringBuilder();
                        for (var j = 0; j < boundY; j++)
                        {
                            row.Append(intensityBlock[i, j] + ",");
                        }
                        smoothedWriter.WriteLine(row.ToString());
                    }

                    smoothedWriter.Close();

                    // ReSharper disable RedundantArgumentDefaultValue
                    var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
                    // ReSharper restore RedundantArgumentDefaultValue

                    var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

                    featureList = featureList.Where(x => x.PointList.Count > 50).OrderByDescending(x => x.PointList.Count);

                    Console.WriteLine("******************************************************");
                    Console.WriteLine("targetMz = " + targetMz);

                    foreach (var featureBlob in featureList)
                    {

                        // ReSharper disable once UnusedVariable
                        var rSquared = FeatureCorrelator.CorrelateFeatures(parentFeature, featureBlob);

                        //Point mostIntensePoint = featureBlob.PointList.OrderByDescending(x => x.Intensity).First();
                        //Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity + "\tRSquared = " + rSquared);
                    }
                }
            }
        }

        [Test]
        public void TestDoWaterShedAlgorithmByBin()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);
            var smoother = new SavitzkyGolaySmoother(5, 2);

            var bin = 73009;
            var intensityPointList = uimfUtil.GetXic(bin, UIMFData.FrameType.MS1);

            var pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);

            var preSmoothedFeatureList = FeatureDetection.DoWatershedAlgorithm(pointList);
            Console.WriteLine(DateTime.Now + "\tBefore Smoothing:\tNumPoints = " + pointList.Count() + "\tNumFeatures = " + preSmoothedFeatureList.Count());
            foreach (var featureBlob in preSmoothedFeatureList)
            {
                var mostIntensePoint = featureBlob.PointList.First();
                Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
            }
            Console.WriteLine("******************************************************");

            var newPointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            smoother.Smooth(ref newPointList);
            var smoothedFeatureList = FeatureDetection.DoWatershedAlgorithm(newPointList);
            Console.WriteLine(DateTime.Now + "\tAfter Smoothing:\tNumPoints = " + newPointList.Count() + "\tNumFeatures = " + smoothedFeatureList.Count());
            foreach (var featureBlob in smoothedFeatureList)
            {
                var mostIntensePoint = featureBlob.PointList.First();
                Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
            }
        }

        [Test]
        public void TestDoWaterShedAlgorithmAllBins()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);

            // ReSharper disable once UnusedVariable
            var numberOfBins = uimfUtil.GetNumberOfBins();

            var smoother = new SavitzkyGolaySmoother(5, 2);

            for (var i = 73009; i <= 84000; i++)
            {
                var mz = uimfUtil.GetMzFromBin(i);
                var intensityPointList = uimfUtil.GetXic(mz, 25, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);
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

                var pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
                smoother.Smooth(ref pointList);
                var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

                Console.WriteLine(DateTime.Now + "\tBin = " + i + "\tNumPoints = " + pointList.Count() + "\tNumFeatures = " + featureList.Count());
            }
        }

        [Test]
        public void TestFakeSaturatedPoints()
        {
            var intensityPointList = new List<IntensityPoint>();

            for(var i = 0; i < 5; i++)
            {
                for(var j = 0; j < 5; j++)
                {
                    var point = new IntensityPoint(i, j, 8925);
                    intensityPointList.Add(point);
                }
            }

            var pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

            Console.WriteLine(DateTime.Now + "\tNumPoints = " + pointList.Count() + "\tNumFeatures = " + featureList.Count());
            foreach (var featureBlob in featureList)
            {
                var mostIntensePoint = featureBlob.PointList.First();
                Console.WriteLine("Num Points = " + featureBlob.PointList.Count + "\tLC = " + mostIntensePoint.ScanLc + "\tIMS = " + mostIntensePoint.ScanIms + "\tIntensity = " + mostIntensePoint.Intensity);
            }
        }

        [Test]
        public void TestComputingApexProfiles()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(fileLocation);
            var smoother = new SavitzkyGolaySmoother(5, 2);

            var targetMz = 964.40334;
            double tolerance = 20;

            var intensityPointList = uimfUtil.GetXic(targetMz, tolerance, UIMFData.FrameType.MS1,
                                                                      DataReader.ToleranceType.PPM);
            var pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            smoother.Smooth(ref pointList);
            var featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);
            IEnumerable<FeatureBlobStatistics> featureBlobStatList = featureBlobs.Select(featureBlob => featureBlob.Statistics).ToList();
            foreach(var f in featureBlobStatList)
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

        [Test]
        public void TestParallelFeatureFinding()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var targetMzList = new List<double> { 582.3218, 964.40334, 643.27094937 };

            var featureUtil = new FeatureDetectionUtil(fileLocation, 11, 4);
            var targetDictionary = featureUtil.GetFeatures(targetMzList, 30, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            foreach (var kvp in targetDictionary)
            {
                Console.WriteLine(kvp.Key + "\t" + kvp.Value.Count());
            }
        }

        [Test]
        public void TestParallelFeatureFindingUsingBins()
        {
            var fileLocation = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            //List<int> targetBinList = new List<int> { 20000, 30000, 40000, 50000, 60000, 70000, 80000, 90000, 10000 };
            var targetBinList = new List<int>();

            for(var i = 10000; i < 100000; i += 1000)
            {
                targetBinList.Add(i);
            }

            var featureUtil = new FeatureDetectionUtil(fileLocation, 11, 4);
            var targetDictionary = featureUtil.GetFeatures(targetBinList, 30, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);

            //foreach (var kvp in targetDictionary)
            //{
            //    Console.WriteLine(kvp.Key + "\t" + kvp.Value.Count());
            //}
        }

        // Added by Sangtae
        [Test]
        public void TestParallelFeatureDetection()
        {
            const string uimfFilePath = @"\\proto-2\UnitTest_Files\MultidimensionalFeatureFinding\BSA_10ugml_IMS6_TOF03_CID_27Aug12_Frodo_Collision_Energy_Collapsed.UIMF";
            var uimfUtil = new UimfUtil(uimfFilePath);
            var featureDetectionUtil = new FeatureDetectionUtil(uimfFilePath, 11, 4);
            var minTargetBin = uimfUtil.GetBinFromMz(500.0);
            var maxTargetBin = uimfUtil.GetBinFromMz(600.0);
            var targetMzList = Enumerable.Range(minTargetBin, maxTargetBin - minTargetBin + 1).Select(uimfUtil.GetMzFromBin).ToList();
            featureDetectionUtil.GetFeatureStatistics(targetMzList, 15, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);
        }
    }
}
