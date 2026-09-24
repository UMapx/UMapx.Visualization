using System;

namespace UMapx.Visualization
{
    /// <summary>Orthographic camera settings for surface figures.</summary>
    [Serializable]
    public sealed class View3D
    {
        /// <summary>Horizontal viewing angle in degrees.</summary>
        public float Azimuth { get; set; } = -37.5f;
        /// <summary>Elevation in degrees, between -90 and 90.</summary>
        public float Elevation { get; set; } = 30;
        /// <summary>Relative height of the normalized plotting box.</summary>
        public float HeightRatio { get; set; } = 0.75f;
        /// <summary>Number of intervals on each displayed 3-D axis.</summary>
        public int TickCount { get; set; } = 4;

        internal void Validate()
        {
            if (!ScientificData.Finite(Azimuth)) throw new ArgumentOutOfRangeException(nameof(Azimuth));
            if (!ScientificData.Finite(Elevation) || Elevation < -90 || Elevation > 90)
                throw new ArgumentOutOfRangeException(nameof(Elevation));
            if (!ScientificData.Finite(HeightRatio) || HeightRatio <= 0 || HeightRatio > 10)
                throw new ArgumentOutOfRangeException(nameof(HeightRatio));
            if (TickCount < 1 || TickCount > 100) throw new ArgumentOutOfRangeException(nameof(TickCount));
        }
    }
}
