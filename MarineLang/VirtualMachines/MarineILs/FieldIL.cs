using MarineLang.Models.Errors;
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
        public readonly ILDebugInfo iLDebugInfo;

        public StaticCSharpFieldLoadIL(Type type, string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.type = type;
            this.fieldName = fieldName;
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            if (type == null)
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"{fieldName}",
                        ErrorCode.Unknown,
                        iLDebugInfo.position
                    )
                );

            var fieldInfo = type.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo))
                    vm.Push(fieldInfo.GetValue(null));
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
                PropertyInfo propertyInfo = type.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"{fieldName}",
                            ErrorCode.RuntimeMemberNotFound,
                            iLDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    vm.Push(propertyInfo.GetValue(type));
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
            return typeof(StaticCSharpFieldLoadIL).Name + " '" + type.FullName + "." + fieldName + "'";
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

    public struct StaticCSharpFieldStoreIL : IMarineIL
    {
        public readonly Type type;
        public readonly string fieldName;
        public readonly ILDebugInfo iLDebugInfo;

        public StaticCSharpFieldStoreIL(Type type, string fieldName, ILDebugInfo iLDebugInfo = null)
        {
            this.type = type;
            this.fieldName = fieldName;
            this.iLDebugInfo = iLDebugInfo;
        }

        public void Run(LowLevelVirtualMachine vm)
        {
            var value = vm.Pop();
            if (type == null)
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(
                        $"{fieldName}",
                        ErrorCode.Unknown,
                        iLDebugInfo.position
                    )
                );

            var fieldInfo = type.GetField(NameUtil.GetLowerCamelName(fieldName),
                BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                if (ClassAccessibilityChecker.CheckMember(fieldInfo))
                    fieldInfo.SetValue(type, value);
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
                PropertyInfo propertyInfo = type.GetProperty(NameUtil.GetUpperCamelName(fieldName),
                    BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            $"{fieldName}",
                            ErrorCode.RuntimeMemberNotFound,
                            iLDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    propertyInfo.SetValue(type, value);
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
            return typeof(StaticCSharpFieldStoreIL).Name + " '" + type.FullName + "." + fieldName + "'";
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
}
