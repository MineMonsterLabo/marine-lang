namespace MarineLang.Models
{
    public struct Position
    {
        public readonly int index;
        public readonly int line;
        public readonly int column;

        public Position(int index, int line, int column)
        {
            this.index = index;
            this.line = line;
            this.column = column;
        }

        public override string ToString()
        {
            return $"行:{line} 列:{column} index:{index}";
        }
    }
}
