using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines
{
    public class VirtualMachineStepEventArgs
    {
        public int Index { get; }
        public IMarineIL MarineIL { get; }

        public VirtualMachineStepState State { get; }

        public VirtualMachineStepEventArgs(int index, IMarineIL il)
        {
            Index = index;
            MarineIL = il;

            State = VirtualMachineStepState.Step;
        }

        public VirtualMachineStepEventArgs(int index, VirtualMachineStepState state)
        {
            Index = index;

            State = state;
        }
    }

    public enum VirtualMachineStepState
    {
        Step,
        Yield,
        End
    }
}