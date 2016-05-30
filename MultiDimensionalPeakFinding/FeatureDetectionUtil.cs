using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiDimensionalPeakFinding.PeakDetection;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding
{
	public class FeatureDetectionUtil
	{
		private readonly ConcurrentStack<UimfUtil> m_uimfUtilStack;
		private readonly SavitzkyGolaySmoother m_smoother;
		private readonly ParallelOptions m_parallelOptions;

		/// <summary>
		/// Creates an object that can be used for finding 3-D XIC Features while taking advantage of multithreading.
		/// </summary>
		/// <param name="uimfFileLocation">The location of the UIMF file.</param>
		/// <param name="numPointsToSmooth">The number of points to use for Savitzky Golay smoothing. Defaults to 11.</param>
		/// <param name="degreeOfParallelism">Desired degree of parallelism. Use 0 if you want to use all cores available. Defaults to all cores.</param>
		public FeatureDetectionUtil(string uimfFileLocation, int numPointsToSmooth = 11, int degreeOfParallelism = 0)
		{
			if (degreeOfParallelism <= 0) degreeOfParallelism = Environment.ProcessorCount;

			m_uimfUtilStack = new ConcurrentStack<UimfUtil>();
			for(var i = 0; i < degreeOfParallelism; i++)
			{
				var uimfUtil = new UimfUtil(uimfFileLocation);
				m_uimfUtilStack.Push(uimfUtil);
			}

			m_smoother = new SavitzkyGolaySmoother(numPointsToSmooth, 2);

			m_parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = degreeOfParallelism};
		}

		public IDictionary<double, IEnumerable<FeatureBlob>> GetFeatures(IEnumerable<double> targetMzList, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			var resultDictionary = new Dictionary<double, IEnumerable<FeatureBlob>>();

			Parallel.ForEach(targetMzList, m_parallelOptions, targetMz =>
			{
				// Grab a UIMF Util object from the stack
			    UimfUtil uimfUtil;
			    m_uimfUtilStack.TryPop(out uimfUtil);

				// Do Feature Finding
				var intensityBlock = uimfUtil.GetXic(targetMz, tolerance, frameType, toleranceType);
				var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
				m_smoother.Smooth(ref pointList);
				var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetMz, featureList);

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}

		public IDictionary<int, IEnumerable<FeatureBlob>> GetFeatures(IEnumerable<int> targetBinList, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			var resultDictionary = new Dictionary<int, IEnumerable<FeatureBlob>>();

			Parallel.ForEach(targetBinList, m_parallelOptions, targetBin =>
			{
				// Grab a UIMF Util object from the stack
				UimfUtil uimfUtil;
				m_uimfUtilStack.TryPop(out uimfUtil);

				var targetMz = uimfUtil.GetMzFromBin(targetBin);

				// Do Feature Finding
				var intensityBlock = uimfUtil.GetXic(targetMz, tolerance, frameType, toleranceType);
				var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
				m_smoother.Smooth(ref pointList);
				var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetBin, featureList);

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}

		public IDictionary<double, IEnumerable<FeatureBlobStatistics>> GetFeatureStatistics(IEnumerable<double> targetMzList, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			var resultDictionary = new Dictionary<double, IEnumerable<FeatureBlobStatistics>>();

			Parallel.ForEach(targetMzList, m_parallelOptions, targetMz =>
			{
				// Grab a UIMF Util object from the stack
				UimfUtil uimfUtil;
				m_uimfUtilStack.TryPop(out uimfUtil);

				// Do Feature Finding
				var intensityBlock = uimfUtil.GetXic(targetMz, tolerance, frameType, toleranceType);
				var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
				m_smoother.Smooth(ref pointList);
				var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetMz, featureList.Select(featureBlob => featureBlob.CalculateStatistics()).ToArray());

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}

		public IDictionary<int, IEnumerable<FeatureBlobStatistics>> GetFeatureStatistics(IEnumerable<int> targetBinList, double tolerance, DataReader.FrameType frameType, DataReader.ToleranceType toleranceType)
		{
			var resultDictionary = new Dictionary<int, IEnumerable<FeatureBlobStatistics>>();

			Parallel.ForEach(targetBinList, m_parallelOptions, targetBin =>
			{
				// Grab a UIMF Util object from the stack
				UimfUtil uimfUtil;
				m_uimfUtilStack.TryPop(out uimfUtil);

				var targetMz = uimfUtil.GetMzFromBin(targetBin);

				// Do Feature Finding
				var intensityBlock = uimfUtil.GetXic(targetMz, tolerance, frameType, toleranceType);
				var pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
				m_smoother.Smooth(ref pointList);
				var featureList = FeatureDetection.DoWatershedAlgorithm(pointList);

				// Add result to dictionary
				resultDictionary.Add(targetBin, featureList.Select(featureBlob => featureBlob.CalculateStatistics()));

				// Push the UIMF Util object back onto the stack when we are done with it
				m_uimfUtilStack.Push(uimfUtil);
			});

			return resultDictionary;
		}
	}
}
