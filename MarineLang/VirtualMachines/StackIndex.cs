
namespace MarineLang.VirtualMachines
{
    public struct StackIndex
    {
        public readonly bool isAbsolute;
        public readonly int index;

        public StackIndex(int index, bool isAbsolute)
        {
            this.index = index;
            this.isAbsolute = isAbsolute;
        }

        public int GetIndex(int stackBaseCount)
        {
            return isAbsolute ? index : stackBaseCount + index;
        }
    }
}
