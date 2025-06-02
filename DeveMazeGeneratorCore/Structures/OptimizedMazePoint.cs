namespace DeveMazeGeneratorCore.Structures
{
    /// <summary>
    /// Optimized MazePoint with packed coordinates for better cache efficiency
    /// </summary>
    public readonly struct OptimizedMazePoint
    {
        private readonly int _packed;

        public OptimizedMazePoint(int x, int y)
        {
            _packed = (y << 16) | (x & 0xFFFF);
        }

        public int X => _packed & 0xFFFF;
        public int Y => _packed >> 16;

        public override string ToString()
        {
            return $"OptimizedMazePoint(X: {X} Y: {Y})";
        }
    }
}