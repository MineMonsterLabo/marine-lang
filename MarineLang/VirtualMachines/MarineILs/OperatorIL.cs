using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.VirtualMachines.CSharpFunctionCallResolver;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines.MarineILs
{

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

        public string GetMethodName(TokenType opKind)
        {
            switch (opKind)
            {
                case TokenType.PlusOp:
                    return "op_Addition";
                case TokenType.MinusOp:
                    return "op_Subtraction";
                case TokenType.MulOp:
                    return "op_Multiply";
                case TokenType.DivOp:
                    return "op_Division";
                case TokenType.ModOp:
                    return "op_Modulus";
                case TokenType.EqualOp:
                    return "op_Equality";
                case TokenType.NotEqualOp:
                    return "op_Inequality";
                case TokenType.GreaterOp:
                    return "op_GreaterThan";
                case TokenType.GreaterEqualOp:
                    return "op_GreaterThanOrEqual";
                case TokenType.LessOp:
                    return "op_LessThan";
                case TokenType.LessEqualOp:
                    return "op_LessThanOrEqual";
            }
            return string.Empty;
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
                    return leftValue?.Equals(rightValue) ?? null == rightValue;
                case TokenType.NotEqualOp:
                    return (!leftValue?.Equals(rightValue)) ?? null != rightValue; ;
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

            var opMethodName = GetMethodName(opKind);
            var leftValueType = leftValue.GetType();

            var methodInfos =
             leftValueType
              .GetMethods(BindingFlags.Public | BindingFlags.Static)
              .Where(e => e.Name == opMethodName)
              .ToArray();

            var methodInfo =
                MethodBaseResolver.Select(
                    methodInfos,
                    new[] { leftValueType, rightValue.GetType() }
                );

            if (methodInfo == null)
            {
                this.ThrowRuntimeError($"演算子{opKind}:", ErrorCode.RuntimeOperatorNotFound);
            }

            return methodInfo.Invoke(null, new[] { leftValue, rightValue });
        }
    }

    public struct UnaryOpIL : IMarineIL
    {
        public readonly TokenType opKind;

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