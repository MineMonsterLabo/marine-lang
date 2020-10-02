using System;

namespace MarineLang.VirtualMachines.Attributes
{
    static internal class AttributeTargetConstants
    {
        public const AttributeTargets ClassTarget =
            AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface;

        public const AttributeTargets MemberTarget =
            AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property;
    }
}