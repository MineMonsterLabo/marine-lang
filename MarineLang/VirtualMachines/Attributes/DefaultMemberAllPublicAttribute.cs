using System;

namespace MarineLang.VirtualMachines.Attributes
{
    [AttributeUsage(AttributeTargetConstants.ClassTarget, Inherited = false)]
    public sealed class DefaultMemberAllPublicAttribute : Attribute
    {
    }
}