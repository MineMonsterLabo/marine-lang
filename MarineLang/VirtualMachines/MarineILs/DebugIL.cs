using System;
using System.Collections.Generic;
using System.Text;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct PushDebugContextIL : IMarineIL
    {
        public readonly DebugContext debugContext;

        public PushDebugContextIL(DebugContext debugContext)
        {
            this.debugContext = debugContext;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.PushDebugContext(debugContext);
        }

        public override string ToString()
        {
            return typeof(PushDebugContextIL).Name;
        }
    }

    public struct PopDebugContextIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            vm.PopDebugContext();
        }

        public override string ToString()
        {
            return typeof(PopDebugContextIL).Name;
        }
    }

}
