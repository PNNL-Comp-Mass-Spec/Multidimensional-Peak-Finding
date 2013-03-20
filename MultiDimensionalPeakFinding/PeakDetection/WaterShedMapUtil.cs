using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class WaterShedMapUtil
	{
		public static IEnumerable<Point> BuildWatershedMap(double[,] inputData, int scanLcMin = 0, int scanImsMin = 0)
		{
			int boundX = inputData.GetUpperBound(0);
			int boundY = inputData.GetUpperBound(1);

			Point[,] pointArray = new Point[boundX, boundY];
			List<Point> pointList = new List<Point>(boundX * boundY);

			// First create a Point at 0,0
			double intensity = inputData[0, 0];
			Point basePoint = new Point(0, scanLcMin, 0, scanImsMin, intensity);
			pointArray[0, 0] = basePoint;

			// Build the map Point by Point
			for(int i = 0; i < boundX; i++)
			{
				for(int j = 0; j < boundY; j++)
				{
					// Since we created the 0,0 point and other points are created as we build, all Points will already have been created by the time we reach it in our loop
					Point currentPoint = pointArray[i, j];

					// Only add to the list if above 0
					// TODO: Pass in a threshold?
					if(currentPoint.Intensity > 0) pointList.Add(currentPoint);

					// Build all Points around the current Point
					LookForNeighbors(currentPoint, pointArray, inputData, boundX, boundY, scanLcMin, scanImsMin);
				}
			}

			return pointList;
		}

		public static IEnumerable<Point> BuildWatershedMap(List<IntensityPoint> intensityPointList)
		{
			int numPoints = intensityPointList.Count;

			List<Point> pointList = new List<Point>(numPoints);
			pointList.AddRange(intensityPointList.Select(intensityPoint => new Point(intensityPoint.ScanLc, 0, intensityPoint.ScanIms, 0, intensityPoint.Intensity)));

			for (int i = 0; i < numPoints - 1; i++)
			{
				Point currentPoint = pointList[i];
				int currentScanLc = currentPoint.ScanLc;
				int currentScanIms = currentPoint.ScanIms;

				Point nextPoint = pointList[i + 1];
				if(nextPoint.ScanLc == currentScanLc && nextPoint.ScanIms == currentScanIms + 1)
				{
					currentPoint.East = nextPoint;
					nextPoint.West = currentPoint;
				}

				Point dummyPoint = new Point(currentScanLc + 1, 0, currentScanIms - 1, 0, 0);
				int binarySearchResult = pointList.BinarySearch(dummyPoint);
				binarySearchResult = binarySearchResult < 0 ? ~binarySearchResult : binarySearchResult;
				for(int j = binarySearchResult; j <= binarySearchResult + 2; j++)
				{
					// Get out if we reach the list boundary
					if (j >= numPoints) break;

					Point testPoint = pointList[j];

					// If LC Scan is too big, get out
					if (testPoint.ScanLc > currentScanLc + 1) break;

					int testScanIms = testPoint.ScanIms;

					// If IMS Scan is too big, get out
					if (testScanIms > currentScanIms + 1) break;

					if(testScanIms == currentScanIms - 1)
					{
						currentPoint.NorthWest = testPoint;
						testPoint.SouthEast = currentPoint;
					}
					else if (testScanIms == currentScanIms)
					{
						currentPoint.North = testPoint;
						testPoint.South = currentPoint;
					}
					else
					{
						currentPoint.NorthEast = testPoint;
						testPoint.SouthWest = currentPoint;
					}
				}
			}

			return pointList;
		}

		private static void LookForNeighbors(Point point, Point[,] pointArray, double[,] inputData, int boundX, int boundY, int xOffset, int yOffset)
		{
			int xValue = point.ScanLcIndex;
			int yValue = point.ScanImsIndex;

			int westXValue = xValue - 1;
			int eastXValue = xValue + 1;
			int southYValue = yValue - 1;
			int northYValue = yValue + 1;

			bool canMoveWest = westXValue >= 0;
			bool canMoveEast = eastXValue < boundX;
			bool canMoveSouth = southYValue >= 0;
			bool canMoveNorth = northYValue < boundY;

			// For any new Point, links to W, S, and SW, and SE are guaranteed to already be created

			// Create any points to the north
			if (canMoveNorth)
			{
				// If we can move NW, then we can grab the NW and N points by accessing the W point
				if(canMoveWest)
				{
					Point westPoint = point.West;

					Point northwestPoint = westPoint.North;
					point.NorthWest = northwestPoint;
					northwestPoint.SouthEast = point;

					Point northPoint = westPoint.NorthEast;
					point.North = northPoint;
					northPoint.South = point;
				}
				// Otherwise, we need to create a new N point
				else
				{
					double intensity = inputData[xValue, northYValue];
					Point northPoint = new Point(xValue, xOffset, northYValue, yOffset, intensity);
					pointArray[xValue, northYValue] = northPoint;

					point.North = northPoint;
					northPoint.South = point;
				}

				if(canMoveEast)
				{
					// You always have to create a new NE point
					double intensity = inputData[eastXValue, northYValue];
					Point northEastPoint = new Point(eastXValue, xOffset, northYValue, yOffset, intensity);
					pointArray[eastXValue, northYValue] = northEastPoint;

					point.NorthEast = northEastPoint;
					northEastPoint.SouthWest = point;
				}
			}

			// Create the E point
			if(canMoveEast)
			{
				// If we can move SE, then we can grab the E point by accessing the S point
				if(canMoveSouth)
				{
					Point eastPoint = point.South.NorthEast;
					point.East = eastPoint;
					eastPoint.West = point;
				}
				// Otherwise, we need to create a new E point
				else
				{
					double intensity = inputData[eastXValue, yValue];
					Point eastPoint = new Point(eastXValue, xOffset, yValue, yOffset, intensity);
					pointArray[eastXValue, yValue] = eastPoint;

					point.East = eastPoint;
					eastPoint.West = point;
				}
			}
		}
	}
}
