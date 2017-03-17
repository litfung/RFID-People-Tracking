using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace PeopleTrackingGui
{
    class MatrixUtils
    {

        public static Matrix<double> NormalizeMatrix(Matrix<double> matrix) {

            double max = findMax(matrix);
            double min = findMin(matrix);

            if (max == min)
            {
                return matrix;
            }
            else {
                return (matrix - min) / (max - min);
            }

            
        }

        public static double findMax(Matrix<double> matrix) {

            double max = matrix[0,0];
            for (int i = 0; i < matrix.RowCount; i++) {

                for (int j = 0; j < matrix.ColumnCount; j++) {
                    if (matrix[i, j] > max) {
                        max = matrix[i, j];
                    }

                }
            }

            return max;
        }

        public static double findMin(Matrix<double> matrix) {
            double min = matrix[0, 0];
            for (int i = 0; i < matrix.RowCount; i++)
            {

                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    if (matrix[i, j] < min)
                    {
                        min = matrix[i, j];
                    }

                }
            }

            return min;
        }

    }
}
