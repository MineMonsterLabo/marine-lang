using MarineLang.Models.Errors;
using MarineLang.VirtualMachines.Attributes;
using System.Collections;
using System.Reflection;

namespace MarineLang.VirtualMachines.MarineILs
{
    public struct InstanceCSharpIndexerLoadIL : IMarineIL
    {
        public ILDebugInfo ILDebugInfo { get; }


        public InstanceCSharpIndexerLoadIL(ILDebugInfo iLDebugInfo = null)
        {
            ILDebugInfo = iLDebugInfo;
        }

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
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            string.Empty,
                            ErrorCode.RuntimeIndexerNotFound,
                            ILDebugInfo.position
                        )
                    );
                }
            else
            {
                PropertyInfo propertyInfo =
                    instanceType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            string.Empty,
                            ErrorCode.RuntimeIndexerNotFound,
                            ILDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    vm.Push(propertyInfo.GetValue(instance, new object[] { indexValue }));
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
        public ILDebugInfo ILDebugInfo { get; }

        public InstanceCSharpIndexerStoreIL(ILDebugInfo iLDebugInfo = null)
        {
            ILDebugInfo = iLDebugInfo;
        }

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
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            string.Empty,
                            ErrorCode.RuntimeIndexerNotFound,
                            ILDebugInfo.position
                        )
                    );
                }
            else
            {
                PropertyInfo propertyInfo =
                    instanceType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new MarineRuntimeException(
                        new RuntimeErrorInfo(
                            string.Empty,
                            ErrorCode.RuntimeIndexerNotFound,
                            ILDebugInfo.position
                        )
                    );

                if (ClassAccessibilityChecker.CheckMember(propertyInfo))
                    propertyInfo.SetValue(instance, storeValue, new object[] { indexValue });
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
}
