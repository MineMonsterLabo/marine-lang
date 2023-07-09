using System;

namespace MarineLang.VirtualMachines.BinaryImage
{
    [Flags]
    public enum ImageOptimization
    {
        None,

        NoDebug = 1,

        NoHeaderAndMeta = 2
        // Size = 4,
        // StripCode = 8
    }
}