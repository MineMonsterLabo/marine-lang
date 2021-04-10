﻿using MarineLang.Models.Errors;
using MarineLang.Utils;
using MarineLang.VirtualMachines.Attributes;
using System;
using System.Reflection;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct StaticCSharpFieldLoadIL : IMarineIL
    {
        public readonly Type type;
        public readonly string fieldName;
        public ILDebugInfo ILDebugInfo { get; }

        public StaticCSharpFieldLoadIL(Type type, string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.type = type;
            this.fieldName = fieldName;
            ILDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            if (type == null)
                this.ThrowRuntimeError($"{fieldName}", ErrorCode.Unknown);

            var fieldInfo = type.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo) == false)
                    this.ThrowRuntimeError($"{fieldName}", ErrorCode.RuntimeMemberAccessPrivate);

                vm.Push(fieldInfo.GetValue(null));
            }
            else
            {
                PropertyInfo propertyInfo = type.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo == null)
                    this.ThrowRuntimeError($"{fieldName}", ErrorCode.RuntimeMemberNotFound);

                if (ClassAccessibilityChecker.CheckMember(propertyInfo) == false)
                    this.ThrowRuntimeError($"{fieldName}", ErrorCode.RuntimeMemberAccessPrivate);

                vm.Push(propertyInfo.GetValue(type));
            }
        }

        public override string ToString()
        {
            return typeof(StaticCSharpFieldLoadIL).Name + " '" + type.FullName + "." + fieldName + "'";
        }
    }

    public struct InstanceCSharpFieldLoadIL : IMarineIL
    {
        public readonly string fieldName;
        public ILDebugInfo ILDebugInfo { get; }

        public InstanceCSharpFieldLoadIL(string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.fieldName = fieldName;
            ILDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var instance = vm.Pop();
            var instanceType = instance.GetType();
            var fieldInfo = instanceType.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo) == false)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberAccessPrivate);

                vm.Push(fieldInfo.GetValue(instance));
            }
            else
            {
                PropertyInfo propertyInfo = instanceType.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberNotFound);

                if (ClassAccessibilityChecker.CheckMember(propertyInfo) == false)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberAccessPrivate);

                vm.Push(propertyInfo.GetValue(instance));
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFieldLoadIL).Name + " '" + fieldName;
        }
    }

    public struct StaticCSharpFieldStoreIL : IMarineIL
    {
        public readonly Type type;
        public readonly string fieldName;
        public ILDebugInfo ILDebugInfo { get; }

        public StaticCSharpFieldStoreIL(Type type, string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.type = type;
            this.fieldName = fieldName;
            ILDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var value = vm.Pop();
            if (type == null)
                this.ThrowRuntimeError($"({fieldName})", ErrorCode.Unknown);

            var fieldInfo = type.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo) == false)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberAccessPrivate);

                fieldInfo.SetValue(type, value);
            }
            else
            {
                PropertyInfo propertyInfo = type.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo == null)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberNotFound);

                if (ClassAccessibilityChecker.CheckMember(propertyInfo) == false)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberAccessPrivate);

                propertyInfo.SetValue(type, value);
            }
        }

        public override string ToString()
        {
            return typeof(StaticCSharpFieldStoreIL).Name + " '" + type.FullName + "." + fieldName + "'";
        }
    }

    public struct InstanceCSharpFieldStoreIL : IMarineIL
    {
        public readonly string fieldName;
        public ILDebugInfo ILDebugInfo { get; }

        public InstanceCSharpFieldStoreIL(string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.fieldName = fieldName;
            ILDebugInfo = iLDebugInfo;
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
                if (ClassAccessibilityChecker.CheckMember(fieldInfo) == false)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberAccessPrivate);

                fieldInfo.SetValue(instance, value);
            }
            else
            {
                PropertyInfo propertyInfo = instanceType.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberNotFound);

                if (ClassAccessibilityChecker.CheckMember(propertyInfo) == false)
                    this.ThrowRuntimeError($"({fieldName})", ErrorCode.RuntimeMemberAccessPrivate);

                propertyInfo.SetValue(instance, value);
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpFieldStoreIL).Name + " '" + fieldName;
        }
    }
}
