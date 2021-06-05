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
        public readonly MethodBase[] methodBases;
        public readonly string funcName;
        public readonly int argCount;
        public ILDebugInfo ILDebugInfo { get; }

        public StaticCSharpFuncCallIL(Type type, MethodBase[] methodBases, string funcName, int argCount, ILDebugInfo iLDebugInfo = null)
        {
            this.type = type;
            this.funcName = funcName;
            this.argCount = argCount;
            this.methodBases = methodBases;
            ILDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).Reverse().ToArray();

            var funcNameClone = funcName;
            var types = args.Select(arg => arg.GetType()).ToArray();

            var methodBase = MethodBaseResolver.Select(methodBases, types);
            if (methodBase == null)
                this.ThrowRuntimeError($"{funcName}", ErrorCode.RuntimeMemberNotFound);

            var args2 = args.Concat(Enumerable.Repeat(Type.Missing, methodBase.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodBase) == false)
                this.ThrowRuntimeError($"({funcName})", ErrorCode.RuntimeMemberAccessPrivate);

            if (methodBase is ConstructorInfo constructorInfo)
                vm.Push(constructorInfo.Invoke(args2));
            else
                vm.Push(methodBase.Invoke(null, args2));
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
            var methodInfo = MethodBaseResolver.Select(methodInfos, types);
            if (methodInfo == null)
                this.ThrowRuntimeError($"{funcName}", ErrorCode.RuntimeMemberNotFound);

            var args2 = args.Concat(Enumerable.Repeat(Type.Missing, methodInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodInfo) == false)
                this.ThrowRuntimeError($"{funcName}", ErrorCode.RuntimeMemberAccessPrivate);

            vm.Push(methodInfo.Invoke(instance, args2));
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFuncCallIL).Name + " '" + funcName + "' " + argCount;
        }
    }

    public struct MarineFuncCallIL : IMarineIL
    {
        public readonly string funcName;
        public readonly int argCount;
        public readonly FuncILIndex funcILIndex;
        public ILDebugInfo ILDebugInfo => null;

        public MarineFuncCallIL(string funcName, FuncILIndex funcILIndex, int argCount)
        {
            this.funcName = funcName;
            this.argCount = argCount;
            this.funcILIndex = funcILIndex;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            vm.Push(vm.stackBaseCount);
            vm.Push(vm.nextILIndex + 1);
            vm.nextILIndex = funcILIndex.Index - 1;
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
