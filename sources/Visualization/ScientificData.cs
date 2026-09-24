using System;
using UMapx.Core;

namespace UMapx.Visualization
{
    internal static class ScientificData
    {
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        internal static void ValidateGrid(float[] x, float[] y, Array values)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (x.Length < 2 || y.Length < 2 || values.Rank != 2 ||
                values.GetLength(0) != y.Length || values.GetLength(1) != x.Length)
                throw new ArgumentException("Values must have shape [y.Length, x.Length], with at least two samples per axis.");
            ValidateCoordinates(x); ValidateCoordinates(y);
        }

        private static void ValidateCoordinates(float[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (!Finite(values[i]) || (i > 0 && values[i] <= values[i - 1]))
                    throw new ArgumentException("Grid coordinates must be finite and strictly increasing.");
        }

        internal static float[] Coordinates(RangeFloat range, int count)
        {
            ValidateRange(range);
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
            var result = new float[count];
            for (int i = 0; i < count; i++)
                result[i] = (float)(range.Min + ((double)range.Max - range.Min) * i / (count - 1));
            ValidateCoordinates(result);
            return result;
        }

        internal static float[] Indices(int count)
        {
            var result = new float[count];
            for (int i = 0; i < count; i++) result[i] = i;
            return result;
        }

        internal static void ValidateRange(RangeFloat range)
        {
            if (!Finite(range.Min) || !Finite(range.Max) || range.Min >= range.Max)
                throw new ArgumentOutOfRangeException(nameof(range), "A finite, increasing range is required.");
        }

        internal static RangeFloat Expand(double min, double max)
        {
            if (!Finite(min) || !Finite(max)) return new RangeFloat(-1, 1);
            if (min == max)
            {
                double margin = Math.Max(0.5, Math.Abs(min) * 0.05);
                min = Math.Max(-float.MaxValue, min - margin);
                max = Math.Min(float.MaxValue, max + margin);
            }
            return new RangeFloat((float)min, (float)max);
        }

        internal static double Normalize(double value, RangeFloat range) =>
            (value - range.Min) / ((double)range.Max - range.Min);

        internal static double Component(Complex32 value, ComplexComponent component)
        {
            if (!Finite(value.Real) || !Finite(value.Imag)) return double.NaN;
            switch (component)
            {
                case ComplexComponent.Real: return value.Real;
                case ComplexComponent.Imaginary: return value.Imag;
                case ComplexComponent.Magnitude:
                    return Math.Sqrt((double)value.Real * value.Real + (double)value.Imag * value.Imag);
                case ComplexComponent.Phase: return Math.Atan2(value.Imag, value.Real);
                default: throw new ArgumentOutOfRangeException(nameof(component));
            }
        }

        internal static void ValidateComponent(ComplexComponent component)
        {
            if (!Enum.IsDefined(typeof(ComplexComponent), component))
                throw new ArgumentOutOfRangeException(nameof(component));
        }

        internal static float ToFloat(double value) => Math.Abs(value) > float.MaxValue ? float.NaN : (float)value;
    }
}
