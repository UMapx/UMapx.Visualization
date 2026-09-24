namespace UMapx.Visualization
{
    internal struct SurfaceVertex
    {
        internal double X, Y, Z, R, G, B;
        internal bool Valid;
        internal static SurfaceVertex Lerp(SurfaceVertex a, SurfaceVertex b, double t) => new SurfaceVertex
        {
            X = a.X + (b.X - a.X) * t, Y = a.Y + (b.Y - a.Y) * t,
            Z = a.Z + (b.Z - a.Z) * t, R = a.R + (b.R - a.R) * t,
            G = a.G + (b.G - a.G) * t, B = a.B + (b.B - a.B) * t, Valid = true
        };
    }
}
