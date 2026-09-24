using System;
using UMapx.Core;

namespace UMapx.Visualization
{
    /// <summary>The real-valued component to display for complex data.</summary>
    public enum ComplexComponent
    {
        /// <summary>Real part.</summary>
        Real,
        /// <summary>Imaginary part.</summary>
        Imaginary,
        /// <summary>Absolute value.</summary>
        Magnitude,
        /// <summary>Phase angle in radians, from -pi to pi.</summary>
        Phase
    }

    /// <summary>A sampled complex function on the complex plane. Arrays are retained.</summary>
    [Serializable]
    public sealed class ComplexSeries
    {
        /// <summary>Creates samples indexed [imaginary coordinate, real coordinate].</summary>
        public ComplexSeries(float[] real, float[] imaginary, Complex32[,] values)
        {
            ScientificData.ValidateGrid(real, imaginary, values);
            Real = real; Imaginary = imaginary; Values = values;
        }

        /// <summary>Strictly increasing real coordinates of the domain.</summary>
        public float[] Real { get; }
        /// <summary>Strictly increasing imaginary coordinates of the domain.</summary>
        public float[] Imaginary { get; }
        /// <summary>Function values, indexed [imaginary coordinate, real coordinate].</summary>
        public Complex32[,] Values { get; }

        /// <summary>Samples a function on an evenly spaced complex grid. Function exceptions propagate.</summary>
        public static ComplexSeries Sample(Func<Complex32, Complex32> function, RangeFloat realRange,
            RangeFloat imaginaryRange, int columns = 161, int rows = 161)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
            var x = ScientificData.Coordinates(realRange, columns);
            var y = ScientificData.Coordinates(imaginaryRange, rows);
            var values = new Complex32[rows, columns];
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++) values[j, i] = function(new Complex32(x[i], y[j]));
            return new ComplexSeries(x, y, values);
        }

        /// <summary>Copies a component into a scalar field for Heatmap, Contour, or Surface.</summary>
        public SurfaceSeries ToField(ComplexComponent component)
        {
            ScientificData.ValidateComponent(component);
            var field = new SurfaceSeries((float[])Real.Clone(), (float[])Imaginary.Clone(), Extract(component));
            if (component == ComplexComponent.Phase)
            {
                field.Colormap = Colormap.Phase;
                field.ColorRange = new RangeFloat(-(float)Math.PI, (float)Math.PI);
            }
            return field;
        }

        /// <summary>Copies a height component and phase colors into a 3-D surface.</summary>
        public SurfaceSeries ToSurface(ComplexComponent height = ComplexComponent.Magnitude)
        {
            ScientificData.ValidateComponent(height);
            return new SurfaceSeries((float[])Real.Clone(), (float[])Imaginary.Clone(),
                Extract(height), Extract(ComplexComponent.Phase))
            {
                Colormap = Colormap.Phase,
                ColorRange = new RangeFloat(-(float)Math.PI, (float)Math.PI),
                Style = SurfaceStyle.Surface
            };
        }

        private float[,] Extract(ComplexComponent component)
        {
            ScientificData.ValidateGrid(Real, Imaginary, Values);
            var result = new float[Imaginary.Length, Real.Length];
            for (int j = 0; j < Imaginary.Length; j++)
                for (int i = 0; i < Real.Length; i++)
                    result[j, i] = ScientificData.ToFloat(ScientificData.Component(Values[j, i], component));
            return result;
        }
    }
}
