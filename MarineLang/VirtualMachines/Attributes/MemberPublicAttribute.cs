using System;

namespace MarineLang.VirtualMachines.Attributes
{
    [AttributeUsage(AttributeTargetConstants.MEMBER_TARGET, Inherited = false)]
    public sealed class MemberPublicAttribute : Attribute
    {
    }
}