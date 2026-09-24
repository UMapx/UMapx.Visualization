using System;

namespace UMapx.Visualization
{
    /// <summary>Color scale for surfaces, heatmaps, contours, and complex domain coloring.</summary>
    [Serializable]
    public sealed class Colorbar
    {
        /// <summary>Whether to show the scale. With multiple surfaces it describes the last surface.</summary>
        public bool Show { get; set; } = true;
        /// <summary>Optional scale title.</summary>
        public string Label { get; set; } = string.Empty;
    }
}
