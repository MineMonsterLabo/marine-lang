using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps.Members
{
    public class FieldDumper : MemberDumper
    {
        private FieldInfo _fieldInfo;

        public bool IsReadOnly { get; }

        public TypeDumper Type { get; }

        internal FieldDumper(FieldInfo fieldInfo) : base(fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public FieldDumper(JObject field) : base(field)
        {
            IsReadOnly = field.Value<bool>("is_readonly");
            Type = new TypeDumper(field["type"] as JObject);
        }

        internal override JObject ToJObject()
        {
            JObject field = new JObject();
            field["kind"] = "field";
            field["name"] = _fieldInfo.Name;
            field["is_readonly"] = _fieldInfo.IsInitOnly;

            TypeDumper typeDumper = new TypeDumper(_fieldInfo.FieldType);
            field["type"] = typeDumper.ToJObject();

            return field;
        }
    }
}