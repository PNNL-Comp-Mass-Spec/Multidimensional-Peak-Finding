using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiDimensionalPeakFinding.PeakDetection;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding
{
	public class FeatureDetectionUtil
	{
		private ConcurrentStack<UimfUtil> m_uimfUtilStack;
		private SavitzkyGolaySmoother m_smoother;
		private ParallelOptions m_parallelOptions;

		/// <summary>
		/// Creates an object that can be used for findinf 3-D XIC Features while taking advantage of multithreading.
		/// </summary>
		/// <param name="uimfFileLocation">The location of the UIMF file.</param>
		/// <param name="numPointsToSmooth">The number of points to use for Savitzky Golay smoothing. Defaults to 11.</param>
		/// <param name="degreeOfParallelism">Desired degree of parallelism. Use 0 if you want to use all cores available. Defaults to all cores.</param>
		public FeatureDetectionUtil(string uimfFileLocation, int numPointsToSmooth = 11, int degreeOfParallelism = 0)
		{
			if (degreeOfParallelism <= 0) degreeOfParallelism = Environment.ProcessorCount;

			m_uimfUtilStack = new ConcurrentStack<UimfUtil>();
			for(int i = 0; i < degreeOfParallelism; i++)
			{
				UimfUtil uimfUtil = new UimfUtil(uimfFileLocation);
				m_uimfUtilStack.Push(uimfUtil);
			}

			m_smoother = new SavitzkyGolaySmoother(numPointsToSmooth, 2);

			m_parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = degreeOfParallelism};
		}

		public IDictionary<double, IEnumerable<FeatureBlob>> GetFeatures(IEnumerable<double> targetMzList, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			Dictionary<double, IEnumerable<FeatureBlob>> resultDictionary = new Dictionary<double, IEnumerable<FeatureBlob>>();

			Parallel.ForEach(targetMzList, m_parallelOptions, targetMz =>
			{
				// Grab a UIMF Util object from the stack
			    UimfUtil uimfUtil;
			    m_uimfUtilStack.TryPop(out uimfUtil);

				// Do Feature Finding
				var intensityBlock = uimfUtil.GetXic(targetMz, tolerance, frameType, toleranceType);
				IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
				m_smoother.Smooth(ref pointList);
				IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetMz, featureList);

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}

		public IDictionary<int, IEnumerable<FeatureBlob>> GetFeatures(IEnumerable<int> targetBinList, DataReader.FrameType frameType)
		{
			Dictionary<int, IEnumerable<FeatureBlob>> resultDictionary = new Dictionary<int, IEnumerable<FeatureBlob>>();

			Parallel.ForEach(targetBinList, m_parallelOptions, targetBin =>
			{
				// Grab a UIMF Util object from the stack
				UimfUtil uimfUtil;
				m_uimfUtilStack.TryPop(out uimfUtil);

				// Do Feature Finding
				double[,] intensityBlock = uimfUtil.GetXicAsArray(targetBin, frameType);
				m_smoother.Smooth(ref intensityBlock);
				IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
				IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetBin, featureList);

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}

		public IDictionary<double, IEnumerable<FeatureBlobStatistics>> GetFeatureStatistics(IEnumerable<double> targetMzList, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			Dictionary<double, IEnumerable<FeatureBlobStatistics>> resultDictionary = new Dictionary<double, IEnumerable<FeatureBlobStatistics>>();

			Parallel.ForEach(targetMzList, m_parallelOptions, targetMz =>
			{
				// Grab a UIMF Util object from the stack
				UimfUtil uimfUtil;
				m_uimfUtilStack.TryPop(out uimfUtil);

				// Do Feature Finding
				double[,] intensityBlock = uimfUtil.GetXicAsArray(targetMz, tolerance, frameType, toleranceType);
				m_smoother.Smooth(ref intensityBlock);
				IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
				IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetMz, featureList.Select(featureBlob => featureBlob.CalculateStatistics()).ToArray());

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}

		public IDictionary<int, IEnumerable<FeatureBlobStatistics>> GetFeatureStatistics(IEnumerable<int> targetBinList, DataReader.FrameType frameType)
		{
			Dictionary<int, IEnumerable<FeatureBlobStatistics>> resultDictionary = new Dictionary<int, IEnumerable<FeatureBlobStatistics>>();

			Parallel.ForEach(targetBinList, m_parallelOptions, targetBin =>
			{
				// Grab a UIMF Util object from the stack
				UimfUtil uimfUtil;
				m_uimfUtilStack.TryPop(out uimfUtil);

				// Do Feature Finding
				double[,] intensityBlock = uimfUtil.GetXicAsArray(targetBin, frameType);
				m_smoother.Smooth(ref intensityBlock);
				IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock, 0, 0);
				IEnumerable<FeatureBlob> featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetBin, featureList.Select(featureBlob => featureBlob.CalculateStatistics()));

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}
	}
}
