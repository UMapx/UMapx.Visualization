namespace UMapx.Visualization
{
    /// <summary>Controls whether a surface displays filled faces, grid edges, or both.</summary>
    public enum SurfaceStyle
    {
        /// <summary>Filled faces.</summary>
        Surface,
        /// <summary>Grid edges with hidden-line removal.</summary>
        Mesh,
        /// <summary>Filled faces and grid edges.</summary>
        SurfaceWithMesh
    }
}
