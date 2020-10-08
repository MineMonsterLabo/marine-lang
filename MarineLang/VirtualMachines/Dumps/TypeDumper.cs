using System;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps
{
    public class TypeDumper
    {
        private Type _type;

        public string QualifiedName { get; }
        public string FullName { get; }
        public string Name { get; }

        internal TypeDumper(Type type)
        {
            _type = type;
        }

        public TypeDumper(JObject type)
        {
            QualifiedName = type.Value<string>("qualified_type");
            FullName = type.Value<string>("full_name");
            Name = type.Value<string>("name");
        }

        internal JObject ToJObject()
        {
            JObject typeObj = new JObject();
            typeObj["qualified_type"] = _type.AssemblyQualifiedName;
            typeObj["full_name"] = _type.FullName;
            typeObj["name"] = _type.Name;

            return typeObj;
        }
    }
}