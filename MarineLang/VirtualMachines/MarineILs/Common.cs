using MarineLang.Models;

namespace MarineLang.VirtualMachines.MarineILs
{
    public class ILDebugInfo
    {
        public readonly Position position;

        public ILDebugInfo(Position position)
        {
            this.position = position;
        }
    }

    public interface IMarineIL
    {
        void Run(LowLevelVirtualMachine vm);
    }
}