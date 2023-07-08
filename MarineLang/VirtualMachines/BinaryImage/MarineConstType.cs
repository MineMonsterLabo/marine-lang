using System;
using System.IO;
using MineUtil;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public enum MarineConstType
    {
        Bool,
        Int,
        Float,
        Char,
        String,
        Enum,
        Null = 126,
        Unit = 127
    }
}