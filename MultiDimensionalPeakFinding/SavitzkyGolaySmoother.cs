using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MultiDimensionalPeakFinding.PeakDetection;

namespace MultiDimensionalPeakFinding
{
    public class SavitzkyGolaySmoother
    {
        private readonly int m_numPointsForSmoothing;
        private readonly Matrix<double> m_smoothingFiltersConjugateTranspose;

        public SavitzkyGolaySmoother(int pointsForSmoothing, int polynomialOrder)
        {
            if (pointsForSmoothing < 3) throw new ArgumentOutOfRangeException("pointsForSmoothing", "SavitzkyGolay pointsForSmoothing must be an odd number 3 or higher");
            if (pointsForSmoothing % 2 == 0) throw new ArgumentOutOfRangeException("pointsForSmoothing", "SavitzkyGolay pointsForSmoothing must be an odd number 3 or higher");

            m_numPointsForSmoothing = pointsForSmoothing;
            var smoothingFilters = CalculateSmoothingFilters(polynomialOrder, pointsForSmoothing);
            m_smoothingFiltersConjugateTranspose = smoothingFilters.ConjugateTranspose();
        }

        public void Smooth(ref IEnumerable<Point> pointList)
        {
            var m = (m_numPointsForSmoothing - 1) / 2;
            var conjTransposeColumnResult = m_smoothingFiltersConjugateTranspose.Column(m);

            foreach (var point in pointList)
            {
                var summedValue = (conjTransposeColumnResult[m] * point.Intensity) * 2.0;

                // Smooth Left
                var currentPoint = point;
                for(var i = m - 1; i >= 0; i--)
                {
                    var nextPoint = currentPoint.West;
                    if (nextPoint == null) break;
                    summedValue += (conjTransposeColumnResult[i] * nextPoint.Intensity);
                    currentPoint = nextPoint;
                }

                // Smooth Right
                currentPoint = point;
                for (var i = m + 1; i < m_numPointsForSmoothing; i++)
                {
                    var nextPoint = currentPoint.East;
                    if (nextPoint == null) break;
                    summedValue += (conjTransposeColumnResult[i] * nextPoint.Intensity);
                    currentPoint = nextPoint;
                }

                // Smooth Down
                currentPoint = point;
                for (var i = m - 1; i >= 0; i--)
                {
                    var nextPoint = currentPoint.South;
                    if (nextPoint == null) break;
                    summedValue += (conjTransposeColumnResult[i] * nextPoint.Intensity);
                    currentPoint = nextPoint;
                }

                // Smooth Up
                currentPoint = point;
                for (var i = m + 1; i < m_numPointsForSmoothing; i++)
                {
                    var nextPoint = currentPoint.North;
                    if (nextPoint == null) break;
                    summedValue += (conjTransposeColumnResult[i] * nextPoint.Intensity);
                    currentPoint = nextPoint;
                }

                point.Intensity = summedValue;
            }
        }

        public void Smooth(ref double[,] inputValues)
        {
            // TODO: Using the matrix works, but does a lot of data accesses. Can improve by working out all the data access myself? I might be able to cut down on number of data accesses, but not sure.
            var inputMatrix = DenseMatrix.OfArray(inputValues);

            for (var i = 0; i < inputMatrix.RowCount; i++)
            {
                inputMatrix.SetRow(i, Smooth(inputMatrix.Row(i).ToArray()));
            }

            for (var i = 0; i < inputMatrix.ColumnCount; i++)
            {
                inputMatrix.SetColumn(i, Smooth(inputMatrix.Column(i).ToArray()));
            }

            inputValues = inputMatrix.ToArray();
        }

        public double[] Smooth(double[] inputValues)
        {
            // No need to smooth if all values are 0
            if (IsEmpty(inputValues)) return inputValues;

            var m = (m_numPointsForSmoothing - 1) / 2;
            var colCount = inputValues.Length;
            var returnYValues = new double[colCount];

            var conjTransposeMatrix = m_smoothingFiltersConjugateTranspose;

            for (var i = 0; i <= m; i++)
            {
                var conjTransposeColumn = conjTransposeMatrix.Column(i);

                double multiplicationResult = 0;
                for (var z = 0; z < m_numPointsForSmoothing; z++)
                {
                    multiplicationResult += (conjTransposeColumn[z] * inputValues[z]);
                }

                returnYValues[i] = multiplicationResult;
            }

            var conjTransposeColumnResult = conjTransposeMatrix.Column(m);

            for (var i = m + 1; i < colCount - m - 1; i++)
            {
                double multiplicationResult = 0;
                for (var z = 0; z < m_numPointsForSmoothing; z++)
                {
                    multiplicationResult += (conjTransposeColumnResult[z] * inputValues[i - m + z]);
                }
                returnYValues[i] = multiplicationResult;
            }

            for (var i = 0; i <= m; i++)
            {
                var conjTransposeColumn = conjTransposeMatrix.Column(m + i);

                double multiplicationResult = 0;
                for (var z = 0; z < m_numPointsForSmoothing; z++)
                {
                    multiplicationResult += (conjTransposeColumn[z] * inputValues[colCount - m_numPointsForSmoothing + z]);
                }
                returnYValues[colCount - m - 1 + i] = multiplicationResult;
            }

            return returnYValues;
        }

        private DenseMatrix CalculateSmoothingFilters(int polynomialOrder, int filterLength)
        {
            var m = (filterLength - 1) / 2;
            var denseMatrix = new DenseMatrix(filterLength, polynomialOrder + 1);

            for (var i = -m; i <= m; i++)
            {
                for (var j = 0; j <= polynomialOrder; j++)
                {
                    denseMatrix[i + m, j] = Math.Pow(i, j);
                }
            }

            var sTranspose = (DenseMatrix)denseMatrix.ConjugateTranspose();
            var f = sTranspose * denseMatrix;
            var fInverse = (DenseMatrix)f.LU().Solve(DenseMatrix.CreateIdentity(f.ColumnCount));
            var smoothingFilters = denseMatrix * fInverse * sTranspose;

            return smoothingFilters;
        }

        private bool IsEmpty(double[] inputValues)
        {
            foreach (var inputValue in inputValues)
            {
                if (inputValue > 0) return false;
            }

            return true;
        }
    }
}
