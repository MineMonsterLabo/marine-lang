﻿namespace MarineLang.VirtualMachines.MarineILs
{
    public struct NoOpIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
        }

        public override string ToString()
        {
            return typeof(NoOpIL).Name;
        }
    }

    public struct RetIL : IMarineIL
    {
        public readonly int argCount;

        public RetIL(int argCount)
        {
            this.argCount = argCount;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            if (vm.callNestCount == 0)
            {
                vm.endFlag = true;
                return;
            }

            vm.callNestCount--;
            var retValue = vm.Pop();
            var stackBaseCount = (int)vm.Load(vm.stackBaseCount + argCount + 1);
            vm.nextILIndex =
                (int)vm.Load(vm.stackBaseCount + argCount + VirtualMachineConstants.CALL_RESTORE_STACK_FRAME) - 1;
            vm.SetStackCurrent(vm.stackBaseCount);
            vm.stackBaseCount = stackBaseCount;
            vm.Push(retValue);
        }

        public override string ToString()
        {
            return typeof(RetIL).Name + " " + argCount;
        }
    }

    public struct JumpFalseIL : IMarineIL
    {
        public readonly int nextILIndex;

        public JumpFalseIL(int nextILIndex)
        {
            this.nextILIndex = nextILIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var condValue = (bool)vm.Pop();
            if (condValue == false) vm.nextILIndex = nextILIndex - 1;
        }

        public override string ToString()
        {
            return typeof(JumpFalseIL).Name + " " + nextILIndex;
        }
    }

    /// <summary>
    /// スタックの状態を変えずに、スタックの最後に積まれた値がFalseならJumpをする命令
    /// </summary>
    public struct JumpFalseNoPopIL : IMarineIL
    {
        public readonly int nextILIndex;

        public JumpFalseNoPopIL(int nextILIndex)
        {
            this.nextILIndex = nextILIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var condValue = (bool)vm.CurrentValue;
            if (condValue == false) vm.nextILIndex = nextILIndex - 1;
        }

        public override string ToString()
        {
            return typeof(JumpFalseNoPopIL).Name + " " + nextILIndex;
        }
    }

    /// <summary>
    /// スタックの状態を変えずに、スタックの最後に積まれた値がTrueならJumpをする命令
    /// </summary>
    public struct JumpTrueNoPopIL : IMarineIL
    {
        public readonly int nextILIndex;

        public JumpTrueNoPopIL(int nextILIndex)
        {
            this.nextILIndex = nextILIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var condValue = (bool)vm.CurrentValue;
            if (condValue) vm.nextILIndex = nextILIndex - 1;
        }

        public override string ToString()
        {
            return typeof(JumpTrueNoPopIL).Name + " " + nextILIndex;
        }
    }

    public struct JumpIL : IMarineIL
    {
        public readonly int nextILIndex;

        public JumpIL(int nextILIndex)
        {
            this.nextILIndex = nextILIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.nextILIndex = nextILIndex - 1;
        }

        public override string ToString()
        {
            return typeof(JumpIL).Name + " " + nextILIndex;
        }
    }

    public struct BreakIL : IMarineIL
    {
        public readonly BreakIndex breakIndex;

        public BreakIL(BreakIndex breakIndex)
        {
            this.breakIndex = breakIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.nextILIndex = breakIndex.Index - 1;
        }

        public override string ToString()
        {
            return typeof(BreakIL).Name + " " + breakIndex.Index;
        }
    }

    public struct StoreValueIL : IMarineIL
    {
        public readonly StackIndex stackIndex;
        public readonly object value;

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Store(value, stackIndex.GetIndex(vm.stackBaseCount));
        }
    }

    public struct PushValueIL : IMarineIL
    {
        public readonly object value;

        public PushValueIL(object value)
        {
            this.value = value;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Push(value);
        }

        public override string ToString()
        {
            return typeof(PushValueIL).Name + " " + value;
        }
    }

    public struct StoreIL : IMarineIL
    {
        public readonly StackIndex stackIndex;

        public StoreIL(in StackIndex stackIndex)
        {
            this.stackIndex = stackIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var storeValue = vm.Pop();
            vm.Store(storeValue, stackIndex.GetIndex(vm.stackBaseCount));
        }

        public override string ToString()
        {
            return typeof(StoreIL).Name + " " + stackIndex;
        }
    }

    public struct LoadIL : IMarineIL
    {
        public readonly StackIndex stackIndex;

        public LoadIL(in StackIndex stackIndex)
        {
            this.stackIndex = stackIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var loadValue = vm.Load(stackIndex.GetIndex(vm.stackBaseCount));
            vm.Push(loadValue);
        }

        public override string ToString()
        {
            return typeof(LoadIL).Name + " " + stackIndex;
        }
    }

    public struct PopIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Pop();
        }

        public override string ToString()
        {
            return typeof(PopIL).Name;
        }
    }

    public struct CreateArrayIL : IMarineIL
    {
        public readonly int initSize;
        public readonly int size;

        public CreateArrayIL(int initSize, int? size)
        {
            this.initSize = initSize;
            this.size = size ?? initSize;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var arr = new object[size];
            for (var i = initSize - 1; i >= 0; i--)
                arr[i] = vm.Pop();
            vm.Push(arr);
        }

        public override string ToString()
        {
            return typeof(CreateArrayIL).Name + " " + size;
        }
    }

    public struct StackAllocIL : IMarineIL
    {
        public readonly int size;

        public StackAllocIL(int size)
        {
            this.size = size;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.SetStackCurrent(vm.GetStackCurrent() + size);
        }

        public override string ToString()
        {
            return typeof(StackAllocIL).Name + " " + size;
        }
    }

    public struct YieldIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            vm.yieldFlag = true;
            vm.yieldCurrentRegister = vm.Pop();
        }

        public override string ToString()
        {
            return typeof(YieldIL).Name;
        }
    }
}
