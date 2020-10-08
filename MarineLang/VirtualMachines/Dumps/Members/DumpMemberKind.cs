using System;

namespace MarineLang.VirtualMachines.Dumps.Members
{
    public enum DumpMemberKind
    {
        Field,
        Property,
        Method
    }

    public static class DumpKindExtensions
    {
        internal static DumpMemberKind ToDumpKind(this string kindName)
        {
            switch (kindName)
            {
                case "field":
                    return DumpMemberKind.Field;

                case "property":
                    return DumpMemberKind.Property;

                case "method":
                    return DumpMemberKind.Method;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}