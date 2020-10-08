using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps.Members
{
    public class MethodDumper : MemberDumper
    {
        private MethodInfo _methodInfo;

        public TypeDumper RetType { get; }

        public IReadOnlyCollection<ParameterDumper> Parameters { get; }

        internal MethodDumper(MethodInfo methodInfo) : base(methodInfo)
        {
            _methodInfo = methodInfo;
        }

        public MethodDumper(JObject member) : base(member)
        {
            RetType = new TypeDumper(member["ret_type"] as JObject);

            Parameters = (member["parameters"] as JArray).Select(e => new ParameterDumper(e as JObject)).ToArray();
        }

        internal override JObject ToJObject()
        {
            // get_Name など、特殊な名前の余計なメソッドを飛ばす
            if (_methodInfo.IsSpecialName)
                return null;

            JObject method = new JObject();
            method["kind"] = "method";
            method["name"] = _methodInfo.Name;

            method["ret_type"] = new TypeDumper(_methodInfo.ReturnType).ToJObject();

            JArray parameters = new JArray();
            foreach (ParameterInfo parameter in _methodInfo.GetParameters())
            {
                parameters.Add(new ParameterDumper(parameter).ToJObject());
            }

            method["parameters"] = parameters;
            return method;
        }
    }
}