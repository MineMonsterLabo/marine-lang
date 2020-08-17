using MarineLang.Models;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines
{
    public interface IMarineIL
    {
        void Run(LowLevelVirtualMachine vm);
    }

    public struct CSharpFuncCallIL : IMarineIL
    {
        public readonly MethodInfo methodInfo;
        public readonly int argCount;

        public CSharpFuncCallIL(MethodInfo methodInfo, int argCount)
        {
            this.methodInfo = methodInfo;
            this.argCount = argCount;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).ToArray();
            vm.Push(methodInfo.Invoke(null, args));
        }
        public override string ToString()
        {
            return typeof(CSharpFuncCallIL).Name + " '" + methodInfo.Name + "' " + argCount;
        }
    }

    public struct MarineFuncCallIL : IMarineIL
    {
        int nextILIndex;
        public readonly string funcName;
        public readonly int argCount;

        public MarineFuncCallIL(string funcName, int argCount)
        {
            nextILIndex = -1;
            this.funcName = funcName;
            this.argCount = argCount;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            if (nextILIndex == -1)
                nextILIndex = vm.MarineFuncIndex(funcName);
            vm.Push(vm.stackBaseCount);
            vm.Push(vm.nextILIndex + 1);
            vm.nextILIndex = nextILIndex - 1;
            vm.stackBaseCount = vm.GetStackCurrent() - argCount - 2;
            vm.callNestCount++;
        }

        public override string ToString()
        {
            return typeof(MarineFuncCallIL).Name + " '" + funcName + "' " + argCount;
        }

    }

    public struct NoOpIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm) { }

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
            vm.nextILIndex = (int)vm.Load(vm.stackBaseCount + argCount + 2) - 1;
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

    public struct StoreValueIL : IMarineIL
    {
        public readonly int stackIndex;
        public readonly object value;
        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Store(value, vm.stackBaseCount + stackIndex);
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
        public readonly int stackIndex;

        public StoreIL(int stackIndex)
        {
            this.stackIndex = stackIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var storeValue = vm.Pop();
            vm.Store(storeValue, vm.stackBaseCount + stackIndex);
        }

        public override string ToString()
        {
            return typeof(StoreIL).Name + " " + stackIndex;
        }
    }

    public struct LoadIL : IMarineIL
    {
        public readonly int stackIndex;

        public LoadIL(int stackIndex)
        {
            this.stackIndex = stackIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var loadValue = vm.Load(vm.stackBaseCount + stackIndex);
            vm.Push(loadValue);
        }

        public override string ToString()
        {
            return typeof(LoadIL).Name + " " + stackIndex;
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
    public struct BinaryOpIL : IMarineIL
    {
        public readonly TokenType opKind;

        public BinaryOpIL(TokenType opKind)
        {
            this.opKind = opKind;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Push(GetResult(vm));
        }

        public override string ToString()
        {
            return typeof(BinaryOpIL).Name + " " + opKind;
        }


        private object GetResult(LowLevelVirtualMachine vm)
        {
            var rightValue = vm.Pop();
            var leftValue = vm.Pop();
            switch (opKind)
            {
                case TokenType.PlusOp:
                    switch (leftValue)
                    {
                        case int v: return v + (int)rightValue;
                        case float v: return v + (float)rightValue;
                        case string v: return v + rightValue;
                    }
                    break;
                case TokenType.MinusOp:
                    switch (leftValue)
                    {
                        case int v: return v - (int)rightValue;
                        case float v: return v - (float)rightValue;
                    }
                    break;
                case TokenType.MulOp:
                    switch (leftValue)
                    {
                        case int v: return v * (int)rightValue;
                        case float v: return v * (float)rightValue;
                    }
                    break;
                case TokenType.DivOp:
                    switch (leftValue)
                    {
                        case int v: return v / (int)rightValue;
                        case float v: return v / (float)rightValue;
                    }
                    break;
                case TokenType.EqualOp:
                    return leftValue.Equals(rightValue);
                case TokenType.NotEqualOp:
                    return !leftValue.Equals(rightValue);
                case TokenType.OrOp:
                    return (bool)leftValue || (bool)rightValue;
                case TokenType.AndOp:
                    return (bool)leftValue && (bool)rightValue;
                case TokenType.GreaterOp:
                    switch (leftValue)
                    {
                        case int v: return v > (int)rightValue;
                        case float v: return v > (float)rightValue;
                    }
                    break;
                case TokenType.GreaterEqualOp:
                    switch (leftValue)
                    {
                        case int v: return v >= (int)rightValue;
                        case float v: return v >= (float)rightValue;
                    }
                    break;

                case TokenType.LessOp:
                    switch (leftValue)
                    {
                        case int v: return v < (int)rightValue;
                        case float v: return v < (float)rightValue;
                    }
                    break;

                case TokenType.LessEqualOp:
                    switch (leftValue)
                    {
                        case int v: return v <= (int)rightValue;
                        case float v: return v <= (float)rightValue;
                    }
                    break;
            }
            return null;
        }
    }
}
