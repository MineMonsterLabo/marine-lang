using MarineLang.Models;

namespace MarineLang.VirtualMachines.MarineILs
{

    public struct BinaryOpIL : IMarineIL
    {
        public readonly TokenType opKind;
        public ILDebugInfo ILDebugInfo => null;

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
                case TokenType.ModOp:
                    switch (leftValue)
                    {
                        case int v: return v % (int)rightValue;
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

    public struct UnaryOpIL : IMarineIL
    {
        public readonly TokenType opKind;
        public ILDebugInfo ILDebugInfo => null;

        public UnaryOpIL(TokenType opKind)
        {
            this.opKind = opKind;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Push(GetResult(vm));
        }

        public override string ToString()
        {
            return typeof(UnaryOpIL).Name + " " + opKind;
        }


        private object GetResult(LowLevelVirtualMachine vm)
        {
            var value = vm.Pop();
            switch (opKind)
            {
                case TokenType.MinusOp:
                    switch (value)
                    {
                        case int v: return -v;
                        case float v: return -v;
                    }

                    break;
                case TokenType.NotOp:
                    return !((bool)value);
            }

            return null;
        }
    }
}