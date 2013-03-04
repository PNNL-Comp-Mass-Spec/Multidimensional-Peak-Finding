using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionalPeakFinding.PeakDetection
{
	public class WaterShedMapUtil
	{
		public static IEnumerable<Point> BuildWatershedMap(double[,] inputData)
		{
			int boundX = inputData.GetUpperBound(0);
			int boundY = inputData.GetUpperBound(1);

			Point[,] pointArray = new Point[boundX, boundY];
			List<Point> pointList = new List<Point>(boundX * boundY);

			// First create a Point at 0,0
			double intensity = inputData[0, 0];
			Point basePoint = new Point(0,0, intensity);
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
					LookForNeighbors(currentPoint, pointArray, inputData, boundX, boundY);
				}
			}

			return pointList;
		}

		private static void LookForNeighbors(Point point, Point[,] pointArray, double[,] inputData, int boundX, int boundY)
		{
			int xValue = point.ScanLc;
			int yValue = point.ScanIms;

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
					Point northPoint = new Point(xValue, northYValue, intensity);
					pointArray[xValue, northYValue] = northPoint;

					point.North = northPoint;
					northPoint.South = point;
				}

				if(canMoveEast)
				{
					// You always have to create a new NE point
					double intensity = inputData[eastXValue, northYValue];
					Point northEastPoint = new Point(eastXValue, northYValue, intensity);
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
					Point eastPoint = new Point(eastXValue, yValue, intensity);
					pointArray[eastXValue, yValue] = eastPoint;

					point.East = eastPoint;
					eastPoint.West = point;
				}
			}
		}
	}
}
