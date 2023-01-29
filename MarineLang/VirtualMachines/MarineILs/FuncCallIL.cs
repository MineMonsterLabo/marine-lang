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
        public readonly MethodInfo[] methodInfos;
        public readonly string funcName;
        public readonly int argCount;
        public readonly Type[] genericTypes;

        public StaticCSharpFuncCallIL(Type type, MethodInfo[] methodInfos, string funcName, int argCount, Type[] genericTypes = null)
        {
            this.type = type;
            this.funcName = funcName;
            this.argCount = argCount;
            this.methodInfos = methodInfos;
            this.genericTypes = genericTypes;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).Reverse().ToArray();

            var types = args.Select(arg => arg.GetType()).ToArray();

            var methodInfo = MethodResolver.Select(methodInfos, types, genericTypes);
            if (methodInfo == null)
                this.ThrowRuntimeError(funcName, ErrorCode.RuntimeMemberNotFound);
            methodInfo = MethodResolver.ResolveGenericMethod(methodInfo, types, genericTypes);

            var args2 =
                args.Concat(Enumerable.Repeat(Type.Missing, methodInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodInfo) == false)
                this.ThrowRuntimeError(funcName, ErrorCode.RuntimeMemberAccessPrivate);

            vm.Push(methodInfo.Invoke(null, args2));
        }

        public override string ToString()
        {
            return typeof(StaticCSharpFuncCallIL).Name + " '" + type.FullName + "." + funcName + "' " + argCount;
        }
    }

    public struct StaticCSharpConstructorCallIL : IMarineIL
    {
        public readonly Type type;
        public readonly ConstructorInfo[] constructorInfos;
        public readonly string funcName;
        public readonly int argCount;

        public StaticCSharpConstructorCallIL(Type type, ConstructorInfo[] constructorInfos, string funcName, int argCount)
        {
            this.type = type;
            this.funcName = funcName;
            this.argCount = argCount;
            this.constructorInfos = constructorInfos;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var args = Enumerable.Range(0, argCount).Select(_ => vm.Pop()).Reverse().ToArray();

            var types = args.Select(arg => arg.GetType()).ToArray();

            var constructorInfo = MethodResolver.Select(constructorInfos, types);
            if (constructorInfo == null)
                this.ThrowRuntimeError(funcName, ErrorCode.RuntimeMemberNotFound);

            var args2 =
                args.Concat(Enumerable.Repeat(Type.Missing, constructorInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(constructorInfo) == false)
                this.ThrowRuntimeError(funcName, ErrorCode.RuntimeMemberAccessPrivate);

            vm.Push(constructorInfo.Invoke(args2));
        }

        public override string ToString()
        {
            return typeof(StaticCSharpConstructorCallIL).Name + " '" + type.FullName + "." + funcName + "' " + argCount;
        }
    }

    public struct InstanceCSharpFuncCallIL : IMarineIL
    {
        public readonly string funcName;
        public readonly int argCount;
        public readonly Type[] genericTypes;

        public InstanceCSharpFuncCallIL(string funcName, int argCount, Type[] genericTypes = null)
        {
            this.funcName = funcName;
            this.argCount = argCount;
            this.genericTypes = genericTypes;
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
            var methodInfo = MethodResolver.Select(methodInfos, types, genericTypes);
            if (methodInfo == null)
                this.ThrowRuntimeError(funcName, ErrorCode.RuntimeMemberNotFound);
            methodInfo = MethodResolver.ResolveGenericMethod(methodInfo, types, genericTypes);

            var args2 = args.Concat(Enumerable.Repeat(Type.Missing, methodInfo.GetParameters().Length - args.Length))
                .ToArray();

            if (ClassAccessibilityChecker.CheckMember(methodInfo) == false)
                this.ThrowRuntimeError(funcName, ErrorCode.RuntimeMemberAccessPrivate);

            vm.Push(methodInfo.Invoke(instance, args2));
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFuncCallIL).Name + " " + funcName + "," + argCount;
        }
    }

    public struct MarineFuncCallIL : IMarineIL
    {
        public readonly string funcName;
        public readonly int argCount;
        public readonly FuncILIndex funcILIndex;

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
            return typeof(MarineFuncCallIL).Name + " " + funcName + "," + argCount;
        }
    }
}
