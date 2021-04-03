using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.Utils;
using MarineLang.VirtualMachines.Attributes;
using MarineLang.VirtualMachines.CSharpFunctionCallResolver;

namespace MarineLang.VirtualMachines
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
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).Reverse().ToArray();
            vm.Push(methodInfo.Invoke(null, args));
        }

        public override string ToString()
        {
            return typeof(CSharpFuncCallIL).Name + " '" + methodInfo.Name + "' " + argCount;
        }
    }

    public struct InstanceCSharpFuncCallIL : IMarineIL
    {
        public readonly string funcName;
        public readonly int argCount;
        public readonly ILDebugInfo iLDebugInfo;

        public InstanceCSharpFuncCallIL(string funcName, int argCount, ILDebugInfo iLDebugInfo = null)
        {
            this.funcName = funcName;
            this.argCount = argCount;
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).Reverse().ToArray();
            var instance = vm.Pop();

            var funcNameClone = funcName;
            var classType = instance.GetType();
            var types = args.Select(arg => arg.GetType()).ToArray();
            var methodInfos =
                classType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(e => e.Name == funcNameClone)
                    .ToArray();
            var methodInfo = MethodInfoResolver.Select(methodInfos, types);

            var args2 = args.Concat(Enumerable.Repeat(Type.Missing, methodInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodInfo))
                vm.Push(methodInfo.Invoke(instance, args2));
            else
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"({funcName})",
                        ErrorCode.RuntimeMemberAccessPrivate,
                        iLDebugInfo.position
                    )
                );
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFuncCallIL).Name + " '" + funcName + "' " + argCount;
        }
    }

    public struct InstanceCSharpFieldLoadIL : IMarineIL
    {
        public readonly string fieldName;
        public readonly ILDebugInfo iLDebugInfo;


        public InstanceCSharpFieldLoadIL(string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.fieldName = fieldName;
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var instance = vm.Pop();
            var instanceType = instance.GetType();
            var fieldInfo = instanceType.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo))
                    vm.Push(fieldInfo.GetValue(instance));
                else
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"{fieldName}",
                            ErrorCode.RuntimeMemberAccessPrivate,
                            iLDebugInfo.position
                        )
                    );
            }
            else
            {
                PropertyInfo propertyInfo = instanceType.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"{fieldName}",
                            ErrorCode.RuntimeMemberNotFound,
                            iLDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    vm.Push(propertyInfo.GetValue(instance));
                else
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"({fieldName})",
                            ErrorCode.RuntimeMemberAccessPrivate,
                            iLDebugInfo.position
                        )
                    );
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFieldLoadIL).Name + " '" + fieldName;
        }
    }

    public struct InstanceCSharpIndexerLoadIL : IMarineIL
    {
        public readonly ILDebugInfo iLDebugInfo;

        public InstanceCSharpIndexerLoadIL(ILDebugInfo iLDebugInfo = null)
        {
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var indexValue = vm.Pop();
            var instance = vm.Pop();
            var instanceType = instance.GetType();

            if (instanceType.IsArray)
                vm.Push((instance as IList)[(int) indexValue]);
            else
            {
                PropertyInfo propertyInfo =
                    instanceType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            string.Empty,
                            ErrorCode.RuntimeIndexerNotFound,
                            iLDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    vm.Push(propertyInfo.GetValue(instance, new object[] {indexValue}));
                else
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            "(Indexer)",
                            ErrorCode.RuntimeMemberAccessPrivate
                        )
                    );
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpIndexerLoadIL).Name;
        }
    }

    public struct InstanceCSharpIndexerStoreIL : IMarineIL
    {
        public readonly ILDebugInfo iLDebugInfo;

        public InstanceCSharpIndexerStoreIL(ILDebugInfo iLDebugInfo = null)
        {
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var storeValue = vm.Pop();
            var indexValue = vm.Pop();
            var instance = vm.Pop();
            var instanceType = instance.GetType();

            if (instanceType.IsArray)
                (instance as IList)[(int) indexValue] = storeValue;
            else
            {
                PropertyInfo propertyInfo =
                    instanceType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            string.Empty,
                            ErrorCode.RuntimeIndexerNotFound,
                            iLDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    propertyInfo.SetValue(instance, storeValue, new object[] {indexValue});
                else
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            "(Indexer)",
                            ErrorCode.RuntimeMemberAccessPrivate
                        )
                    );
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpIndexerStoreIL).Name;
        }
    }

    public struct InstanceCSharpFieldStoreIL : IMarineIL
    {
        public readonly string fieldName;
        public readonly ILDebugInfo iLDebugInfo;

        public InstanceCSharpFieldStoreIL(string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.fieldName = fieldName;
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var value = vm.Pop();
            var instance = vm.Pop();
            var instanceType = instance.GetType();
            var fieldInfo = instanceType.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo))
                    fieldInfo.SetValue(instance, value);
                else
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"({fieldName})",
                            ErrorCode.RuntimeMemberAccessPrivate,
                            iLDebugInfo.position
                        )
                    );
            }
            else
            {
                PropertyInfo propertyInfo = instanceType.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"{fieldName}",
                            ErrorCode.RuntimeMemberNotFound,
                            iLDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    propertyInfo.SetValue(instance, value);
                else
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"({fieldName})",
                            ErrorCode.RuntimeMemberAccessPrivate,
                            iLDebugInfo.position
                        )
                    );
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFieldStoreIL).Name + " '" + fieldName;
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
            vm.stackBaseCount =
                vm.GetStackCurrent() - argCount - VirtualMachineConstants.CALL_RESTORE_STACK_FRAME;
            vm.callNestCount++;
        }

        public override string ToString()
        {
            return typeof(MarineFuncCallIL).Name + " '" + funcName + "' " + argCount;
        }
    }

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

    public struct GetIterCurrentL : IMarineIL
    {
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
            var stackBaseCount = (int) vm.Load(vm.stackBaseCount + argCount + 1);
            vm.nextILIndex =
                (int) vm.Load(vm.stackBaseCount + argCount + VirtualMachineConstants.CALL_RESTORE_STACK_FRAME) - 1;
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
            var condValue = (bool) vm.Pop();
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
        }

        public override string ToString()
        {
            return typeof(YieldIL).Name;
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
                        case int v: return v + (int) rightValue;
                        case float v: return v + (float) rightValue;
                        case string v: return v + rightValue;
                    }

                    break;
                case TokenType.MinusOp:
                    switch (leftValue)
                    {
                        case int v: return v - (int) rightValue;
                        case float v: return v - (float) rightValue;
                    }

                    break;
                case TokenType.MulOp:
                    switch (leftValue)
                    {
                        case int v: return v * (int) rightValue;
                        case float v: return v * (float) rightValue;
                    }

                    break;
                case TokenType.DivOp:
                    switch (leftValue)
                    {
                        case int v: return v / (int) rightValue;
                        case float v: return v / (float) rightValue;
                    }

                    break;
                case TokenType.ModOp:
                    switch (leftValue)
                    {
                        case int v: return v % (int) rightValue;
                    }

                    break;
                case TokenType.EqualOp:
                    return leftValue.Equals(rightValue);
                case TokenType.NotEqualOp:
                    return !leftValue.Equals(rightValue);
                case TokenType.OrOp:
                    return (bool) leftValue || (bool) rightValue;
                case TokenType.AndOp:
                    return (bool) leftValue && (bool) rightValue;
                case TokenType.GreaterOp:
                    switch (leftValue)
                    {
                        case int v: return v > (int) rightValue;
                        case float v: return v > (float) rightValue;
                    }

                    break;
                case TokenType.GreaterEqualOp:
                    switch (leftValue)
                    {
                        case int v: return v >= (int) rightValue;
                        case float v: return v >= (float) rightValue;
                    }

                    break;

                case TokenType.LessOp:
                    switch (leftValue)
                    {
                        case int v: return v < (int) rightValue;
                        case float v: return v < (float) rightValue;
                    }

                    break;

                case TokenType.LessEqualOp:
                    switch (leftValue)
                    {
                        case int v: return v <= (int) rightValue;
                        case float v: return v <= (float) rightValue;
                    }

                    break;
            }

            return null;
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
                    return !((bool) value);
            }

            return null;
        }
    }
}