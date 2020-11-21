using System;
using System.Collections.Generic;
using System.Text;

namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class TypeDumpModel
    {
        public string QualifiedName { get; }
        public string FullName { get; }
        public string Name { get; }

        public TypeDumpModel(string qualifiedName, string fullName, string name)
        {
            QualifiedName = qualifiedName;
            FullName = fullName;
            Name = name;
        }
    }
}
