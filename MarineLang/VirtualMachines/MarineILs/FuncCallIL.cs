using MarineLang.Models.Errors;
using MarineLang.VirtualMachines.Attributes;
using MarineLang.VirtualMachines.CSharpFunctionCallResolver;
using System;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct CSharpFuncCallIL : IMarineIL
    {
        public readonly MethodInfo methodInfo;
        public readonly int argCount;
        public ILDebugInfo ILDebugInfo => null;

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

    public struct StaticCSharpFuncCallIL : IMarineIL
    {
        public readonly Type type;
        public readonly string funcName;
        public readonly int argCount;
        public ILDebugInfo ILDebugInfo { get; }

        public StaticCSharpFuncCallIL(Type type, string funcName, int argCount, ILDebugInfo iLDebugInfo = null)
        {
            this.type = type;
            this.funcName = funcName;
            this.argCount = argCount;
            ILDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).Reverse().ToArray();
            if (type == null)
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"{funcName}",
                        ErrorCode.Unknown,
                        ILDebugInfo.position
                    )
                );

            var funcNameClone = funcName;
            var types = args.Select(arg => arg.GetType()).ToArray();
            var methodInfos =
                type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(e => e.Name == funcNameClone)
                    .ToArray();
            var methodInfo = MethodInfoResolver.Select(methodInfos, types);
            if (methodInfo == null)
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"{funcName}",
                        ErrorCode.RuntimeMemberNotFound,
                        ILDebugInfo.position
                    )
                );

            var args2 = args.Concat(Enumerable.Repeat(Type.Missing, methodInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodInfo))
                vm.Push(methodInfo.Invoke(null, args2));
            else
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"({funcName})",
                        ErrorCode.RuntimeMemberAccessPrivate,
                        ILDebugInfo.position
                    )
                );
        }

        public override string ToString()
        {
            return typeof(StaticCSharpFuncCallIL).Name + " '" + type.FullName + "." + funcName + "' " + argCount;
        }
    }

    public struct InstanceCSharpFuncCallIL : IMarineIL
    {
        public readonly string funcName;
        public readonly int argCount;
        public ILDebugInfo ILDebugInfo { get; }

        public InstanceCSharpFuncCallIL(string funcName, int argCount, ILDebugInfo iLDebugInfo = null)
        {
            this.funcName = funcName;
            this.argCount = argCount;
            ILDebugInfo = iLDebugInfo;
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
            if (methodInfo == null)
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"{funcName}",
                        ErrorCode.RuntimeMemberNotFound,
                        ILDebugInfo.position
                    )
                );

            var args2 = args.Concat(Enumerable.Repeat(Type.Missing, methodInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodInfo))
                vm.Push(methodInfo.Invoke(instance, args2));
            else
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"({funcName})",
                        ErrorCode.RuntimeMemberAccessPrivate,
                        ILDebugInfo.position
                    )
                );
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFuncCallIL).Name + " '" + funcName + "' " + argCount;
        }
    }

    public struct MarineFuncCallIL : IMarineIL
    {
        int nextILIndex;
        public readonly string funcName;
        public readonly int argCount;
        public ILDebugInfo ILDebugInfo => null;

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
}
