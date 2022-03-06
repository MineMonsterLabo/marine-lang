using MarineLang.Models.Errors;
using MarineLang.VirtualMachines.Attributes;
using System.Collections;
using System.Reflection;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct InstanceCSharpIndexerLoadIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            var indexValue = vm.Pop();
            var instance = vm.Pop();
            var instanceType = instance.GetType();

            if (instanceType.IsArray)
                if (indexValue is int intIndex)
                    vm.Push((instance as IList)[intIndex]);
                else
                {
                    this.ThrowRuntimeError(string.Empty, ErrorCode.RuntimeIndexerNotFound);
                }
            else
            {
                PropertyInfo propertyInfo =
                    instanceType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    this.ThrowRuntimeError(string.Empty, ErrorCode.RuntimeIndexerNotFound);

                if (ClassAccessibilityChecker.CheckMember(propertyInfo) == false)
                    this.ThrowRuntimeError("(Indexer)", ErrorCode.RuntimeMemberAccessPrivate);

                vm.Push(propertyInfo.GetValue(instance, new object[] { indexValue }));
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpIndexerLoadIL).Name;
        }
    }

    public struct InstanceCSharpIndexerStoreIL : IMarineIL
    {
        public void Run(LowLevelVirtualMachine vm)
        {
            var storeValue = vm.Pop();
            var indexValue = vm.Pop();
            var instance = vm.Pop();
            var instanceType = instance.GetType();

            if (instanceType.IsArray)
                if (indexValue is int intIndex)
                    (instance as IList)[intIndex] = storeValue;
                else
                {
                    this.ThrowRuntimeError(string.Empty, ErrorCode.RuntimeIndexerNotFound);
                }
            else
            {
                PropertyInfo propertyInfo =
                    instanceType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    this.ThrowRuntimeError(string.Empty, ErrorCode.RuntimeIndexerNotFound);

                if (ClassAccessibilityChecker.CheckMember(propertyInfo) == false)
                    this.ThrowRuntimeError("(Indexer)", ErrorCode.RuntimeMemberAccessPrivate);

                propertyInfo.SetValue(instance, storeValue, new object[] { indexValue });
            }
        }

        public override string ToString()
        {
            return typeof(InstanceCSharpIndexerStoreIL).Name;
        }
    }
}
