using System;

namespace MarineLang.VirtualMachines.Attributes
{
    [AttributeUsage(AttributeTargetConstants.CLASS_TARGET, Inherited = false)]
    public sealed class DefaultMemberAllPrivateAttribute : Attribute
    {
    }
}