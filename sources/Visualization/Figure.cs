using System;
using System.Collections.Generic;
using System.Drawing;
using UMapx.Core;

namespace UMapx.Visualization
{
    /// <summary>
    /// Renders Cartesian plots, scalar fields, complex functions, and 3-D surfaces.
    /// </summary>
    [Serializable]
    public partial class Figure
    {
        #region Private data
        private int _xscale = 10, _yscale = 10;
        private float _xmin = -5, _xmax = 5;
        private float _ymin = -5, _ymax = 5;
        private float _scaling = 0.65f;
        private readonly FigureStyle _style;
        private readonly List<PlotSeries> _plotSeries = new List<PlotSeries>();
        private Bitmap _imagePane;
        #endregion

        #region Figure constructor
        /// <summary>
        /// Initializes the figure.
        /// </summary>
        /// <param name="style">Figure style.</param>
        public Figure(FigureStyle style)
        {
            _style = style;
        }
        #endregion

        #region Figure properties
        /// <summary>
        /// Gets or sets X label.
        /// </summary>
        public string LabelX { get; set; } = "Label X";
        /// <summary>
        /// Gets or sets Y label.
        /// </summary>
        public string LabelY { get; set; } = "Label Y";
        /// <summary>
        /// Gets or sets the figure title.
        /// </summary>
        public string Title { get; set; } = "Title";
        /// <summary>
        /// Gets or sets the grid.
        /// </summary>
        public Grid Grid { get; set; } = new Grid();
        /// <summary>
        /// Gets or sets the legend.
        /// </summary>
        public Legend Legend { get; set; } = new Legend();
        /// <summary>
        /// Gets or sets X range [min, max].
        /// </summary>
        public RangeFloat RangeX
        {
            get
            {
                return new RangeFloat(_xmin, _xmax);
            }
            set
            {
                if (value.Min == value.Max)
                    throw new ArgumentOutOfRangeException("Start and end points cannot be the same");

                if (Maths.IsSingular(value.Min) || Maths.IsSingular(value.Max))
                    throw new ArgumentOutOfRangeException("Start of end points cannot be singular");

                _xmin = value.Min; _xmax = value.Max;
            }
        }
        /// <summary>
        /// Gets or sets Y range [min, max].
        /// </summary>
        public RangeFloat RangeY
        {
            get
            {
                return new RangeFloat(_ymin, _ymax);
            }
            set
            {
                if (value.Min == value.Max)
                    throw new ArgumentOutOfRangeException("Start and end points cannot be the same");

                if (Maths.IsSingular(value.Min) || Maths.IsSingular(value.Max))
                    throw new ArgumentOutOfRangeException("Start of end points cannot be singular");

                _ymin = value.Min; _ymax = value.Max;
            }
        }
        /// <summary>
        /// Gets or sets the scale of a range of digital elevations along the X and Y axes.
        /// </summary>
        public PointInt Marks
        {
            get
            {
                return new PointInt(_xscale, _yscale);
            }
            set
            {
                if (value.X < 1 || value.Y < 1)
                    throw new ArgumentOutOfRangeException("The range of marks cannot be less than 1");

                _xscale = value.X; _yscale = value.Y;
            }
        }
        /// <summary>
        /// Gets or sets the preferred canvas fraction in (0, 1]. Space for labels is reserved automatically.
        /// </summary>
        public float Scaling
        {
            get 
            { 
                return _scaling; 
            }
            set 
            {
                if (value <= 0 || value > 1)
                    throw new ArgumentOutOfRangeException("Scale factor cannot be less than 0 or more than 1");

                _scaling = value; 
            }
        }
        /// <summary>
        /// Gets or sets whether axes follow the finite data bounds.
        /// Constant axes receive a finite margin so coordinate mapping remains defined.
        /// </summary>
        public bool AutoRange { get; set; } = true;
        #endregion

        #region Figure methods
        /// <summary>
        /// Draw figure to bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap.</param>
        public void To(Bitmap bitmap)
        {
            using var graphics = Graphics.FromImage(bitmap);
            this.To(graphics);
        }
        /// <summary>
        /// Draw figure to graphics object.
        /// </summary>
        /// <param name="graphics">Graphics.</param>
        public void To(Graphics graphics) => RenderFigure(graphics);
        /// <summary>
        /// Show image at the figure.
        /// </summary>
        /// <param name="bitmap">Bitmap.</param>
        public void Image(Bitmap bitmap)
        {
            _scientificMode = ScientificMode.None;
            _imagePane = bitmap;
        }
        /// <summary>
        /// Add the plot series to the figure.
        /// </summary>
        /// <param name="plotSeries">Plot series.</param>
        public void Plot(PlotSeries plotSeries)
        {
            if (plotSeries.X.Length != plotSeries.Y.Length)
                throw new ArgumentException("Vectors must be of the same length");

            _plotSeries.Add(plotSeries);
            _scientificMode = ScientificMode.None;
        }
        /// <summary>
        /// Remove all plot series from the figure. 
        /// </summary>
        public void Clear()
        {
            _plotSeries.Clear();
            _imagePane = null;
            _xmin = -5; _xmax = 5;
            _ymin = -5; _ymax = 5;
            ClearScientific();
        }
        #endregion

    }
}
