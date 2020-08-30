using System;
using System.Collections.Generic;
using System.Text;

namespace MarineLang.VirtualMachines
{
    public struct StackIndex
    {
        readonly bool isAbsolute;
        readonly int index;

        public StackIndex(int index, bool isAbsolute)
        {
            this.index = index;
            this.isAbsolute = isAbsolute;
        }

        public int GetIndex(int stackBaseCount)
        {
            return isAbsolute ? index : stackBaseCount + index;
        }
    }
}
