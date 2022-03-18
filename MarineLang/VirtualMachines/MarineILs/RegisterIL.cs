namespace MarineLang.VirtualMachines.MarineILs
{
    public struct PushYieldCurrentRegisterIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Push(vm.yieldCurrentRegister);
        }

        public override string ToString()
        {
            return typeof(PushYieldCurrentRegisterIL).Name;
        }
    }
}
