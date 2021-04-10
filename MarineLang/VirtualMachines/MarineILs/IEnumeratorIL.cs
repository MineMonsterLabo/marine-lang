using System.Collections;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct MoveNextIL : IMarineIL
    {
        public ILDebugInfo ILDebugInfo => null;

        public void Run(LowLevelVirtualMachine vm)
        {
            var instance = vm.Pop();
            vm.Push((instance as IEnumerator).MoveNext());
        }

        public override string ToString()
        {
            return typeof(MoveNextIL).Name;
        }
    }

    public struct GetIterCurrentL : IMarineIL
    {
        public ILDebugInfo ILDebugInfo => null;

        public void Run(LowLevelVirtualMachine vm)
        {
            var instance = vm.Pop();
            vm.Push((instance as IEnumerator).Current);
        }

        public override string ToString()
        {
            return typeof(GetIterCurrentL).Name;
        }
    }
}
