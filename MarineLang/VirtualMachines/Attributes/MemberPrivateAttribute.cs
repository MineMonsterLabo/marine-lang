using System;

namespace MarineLang.VirtualMachines.Attributes
{
    [AttributeUsage(AttributeTargetConstants.MemberTarget, Inherited = false)]
    public sealed class MemberPrivateAttribute : Attribute
    {
    }
}