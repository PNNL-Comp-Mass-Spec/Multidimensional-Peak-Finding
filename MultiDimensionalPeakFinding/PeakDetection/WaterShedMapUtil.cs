using System.Collections.Generic;
using System.Linq;
using UIMFLibrary;

namespace MultiDimensionalPeakFinding.PeakDetection
{
    public class WaterShedMapUtil
    {
        public static IEnumerable<Point> BuildWatershedMap(double[,] inputData, int scanLcMin = 0, int scanImsMin = 0)
        {
            var boundX = inputData.GetUpperBound(0);
            var boundY = inputData.GetUpperBound(1);

            var pointArray = new Point[boundX, boundY];
            var pointList = new List<Point>(boundX * boundY);

            // First create a Point at 0,0
            var intensity = inputData[0, 0];
            var basePoint = new Point(0, scanLcMin, 0, scanImsMin, intensity);
            pointArray[0, 0] = basePoint;

            // Build the map Point by Point
            for(var i = 0; i < boundX; i++)
            {
                for(var j = 0; j < boundY; j++)
                {
                    // Since we created the 0,0 point and other points are created as we build, all Points will already have been created by the time we reach it in our loop
                    var currentPoint = pointArray[i, j];

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
            var numPoints = intensityPointList.Count;

            var pointList = new List<Point>(numPoints);
            pointList.AddRange(intensityPointList.Select(intensityPoint => new Point(intensityPoint.ScanLc, 0, intensityPoint.ScanIms, 0, intensityPoint.Intensity, intensityPoint.IsSaturated)));

            for (var i = 0; i < numPoints - 1; i++)
            {
                var currentPoint = pointList[i];
                var currentScanLc = currentPoint.ScanLc;
                var currentScanIms = currentPoint.ScanIms;

                var nextPoint = pointList[i + 1];
                if(nextPoint.ScanLc == currentScanLc && nextPoint.ScanIms == currentScanIms + 1)
                {
                    currentPoint.East = nextPoint;
                    nextPoint.West = currentPoint;
                }

                var dummyPoint = new Point(currentScanLc + 1, 0, currentScanIms - 1, 0, 0);
                var binarySearchResult = pointList.BinarySearch(dummyPoint);
                binarySearchResult = binarySearchResult < 0 ? ~binarySearchResult : binarySearchResult;
                for(var j = binarySearchResult; j <= binarySearchResult + 2; j++)
                {
                    // Get out if we reach the list boundary
                    if (j >= numPoints) break;

                    var testPoint = pointList[j];

                    // If LC Scan is too big, get out
                    if (testPoint.ScanLc > currentScanLc + 1) break;

                    var testScanIms = testPoint.ScanIms;

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
            var xValue = point.ScanLcIndex;
            var yValue = point.ScanImsIndex;

            var westXValue = xValue - 1;
            var eastXValue = xValue + 1;
            var southYValue = yValue - 1;
            var northYValue = yValue + 1;

            var canMoveWest = westXValue >= 0;
            var canMoveEast = eastXValue < boundX;
            var canMoveSouth = southYValue >= 0;
            var canMoveNorth = northYValue < boundY;

            // For any new Point, links to W, S, and SW, and SE are guaranteed to already be created

            // Create any points to the north
            if (canMoveNorth)
            {
                // If we can move NW, then we can grab the NW and N points by accessing the W point
                if(canMoveWest)
                {
                    var westPoint = point.West;

                    var northwestPoint = westPoint.North;
                    point.NorthWest = northwestPoint;
                    northwestPoint.SouthEast = point;

                    var northPoint = westPoint.NorthEast;
                    point.North = northPoint;
                    northPoint.South = point;
                }
                // Otherwise, we need to create a new N point
                else
                {
                    var intensity = inputData[xValue, northYValue];
                    var northPoint = new Point(xValue, xOffset, northYValue, yOffset, intensity);
                    pointArray[xValue, northYValue] = northPoint;

                    point.North = northPoint;
                    northPoint.South = point;
                }

                if(canMoveEast)
                {
                    // You always have to create a new NE point
                    var intensity = inputData[eastXValue, northYValue];
                    var northEastPoint = new Point(eastXValue, xOffset, northYValue, yOffset, intensity);
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
                    var eastPoint = point.South.NorthEast;
                    point.East = eastPoint;
                    eastPoint.West = point;
                }
                // Otherwise, we need to create a new E point
                else
                {
                    var intensity = inputData[eastXValue, yValue];
                    var eastPoint = new Point(eastXValue, xOffset, yValue, yOffset, intensity);
                    pointArray[eastXValue, yValue] = eastPoint;

                    point.East = eastPoint;
                    eastPoint.West = point;
                }
            }
        }
    }
}
