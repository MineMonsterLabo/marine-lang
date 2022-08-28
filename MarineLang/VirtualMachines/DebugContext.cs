using MarineLang.Models;

namespace MarineLang.VirtualMachines
{
    public class DebugContext
    {
        public string FuncName { get; }
        public string ProgramUnitName { get; }
        public RangePosition RangePosition { get; }

        public DebugContext(string programUnitName, string funcName, RangePosition rangePosition)
        {
            FuncName = funcName;
            ProgramUnitName = programUnitName;
            RangePosition = rangePosition;
        }

        public override string ToString()
        {
            return $"{FuncName} in \"{ProgramUnitName}\" ({RangePosition})";
        }
    }
}
