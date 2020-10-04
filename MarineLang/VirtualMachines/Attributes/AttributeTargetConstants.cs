using System;

namespace MarineLang.VirtualMachines.Attributes
{
    static internal class AttributeTargetConstants
    {
        public const AttributeTargets CLASS_TARGET =
            AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface;

        public const AttributeTargets MEMBER_TARGET =
            AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property;
    }
}