using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps.Members
{
    public class ParameterDumper
    {
        private ParameterInfo _parameterInfo;

        public string Name { get; }

        public bool IsIn { get; }
        public bool IsOut { get; }
        public bool IsRef { get; }

        public bool IsOptional { get; }

        public object Value { get; }

        public TypeDumper Type { get; }

        internal ParameterDumper(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo;
        }

        public ParameterDumper(JObject parameter)
        {
            IsIn = parameter.Value<bool>("is_in");
            IsOut = parameter.Value<bool>("is_out");
            IsRef = parameter.Value<bool>("is_ref");

            IsOptional = parameter.Value<bool>("is_optional");

            if (IsOptional)
                Value = (parameter["default_value"] as JValue).Value;

            Type = new TypeDumper(parameter["type"] as JObject);
        }

        internal JObject ToJObject()
        {
            JObject param = new JObject();
            param["name"] = _parameterInfo.Name;
            param["is_in"] = _parameterInfo.IsIn;
            param["is_out"] = _parameterInfo.IsOut;
            param["is_ref"] = _parameterInfo.ParameterType.IsByRef && !_parameterInfo.IsOut;
            param["is_optional"] = _parameterInfo.IsOptional;
            if (_parameterInfo.IsOptional)
                param["default_value"] = new JValue(_parameterInfo.RawDefaultValue);

            param["type"] = new TypeDumper(_parameterInfo.ParameterType).ToJObject();

            return param;
        }
    }
}