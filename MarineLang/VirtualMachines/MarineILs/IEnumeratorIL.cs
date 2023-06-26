using System.Collections;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct MoveNextIL : IMarineIL
    {
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

    public struct GetIterCurrentIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            var instance = vm.Pop();
            vm.Push((instance as IEnumerator).Current);
        }

        public override string ToString()
        {
            return typeof(GetIterCurrentIL).Name;
        }
    }
}
