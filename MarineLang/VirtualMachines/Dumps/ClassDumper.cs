using System.Linq;
using System.Reflection;
using MarineLang.VirtualMachines.Attributes;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps
{
    public class ClassDumper
    {
        private object _value;

        public ClassDumper(object value)
        {
            _value = value;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            JObject typeObj = new JObject();
            typeObj["qualified_type"] = _value.GetType().AssemblyQualifiedName;
            typeObj["full_name"] = _value.GetType().FullName;
            typeObj["name"] = _value.GetType().Name;

            jObject["type"] = typeObj;

            JArray memberArray = new JArray();
            var members = _value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(ClassAccessibilityChecker.CheckMember);
            foreach (MemberInfo memberInfo in members)
            {
                switch (memberInfo)
                {
                    case FieldInfo fieldInfo:
                        JObject field = new JObject();
                        field["kind"] = "field";
                        field["name"] = fieldInfo.Name;
                        field["is_readonly"] = fieldInfo.IsInitOnly;

                        JObject fieldType = new JObject();
                        fieldType["qualified_type"] = fieldInfo.FieldType.AssemblyQualifiedName;
                        fieldType["full_name"] = fieldInfo.FieldType.FullName;
                        fieldType["name"] = fieldInfo.FieldType.Name;

                        field["type"] = fieldType;
                        memberArray.Add(field);
                        break;

                    case PropertyInfo propertyInfo:
                        JObject property = new JObject();
                        property["kind"] = "property";
                        property["name"] = propertyInfo.Name;
                        property["can_read"] = propertyInfo.CanRead;
                        property["can_write"] = propertyInfo.CanWrite;

                        bool isIndexer = propertyInfo.GetIndexParameters().Length > 0;
                        property["is_indexer"] = isIndexer;

                        JObject propertyType = new JObject();
                        propertyType["qualified_type"] = propertyInfo.PropertyType.AssemblyQualifiedName;
                        propertyType["full_name"] = propertyInfo.PropertyType.FullName;
                        propertyType["name"] = propertyInfo.PropertyType.Name;

                        property["type"] = propertyType;

                        if (isIndexer)
                        {
                            JArray indexerParameters = new JArray();
                            foreach (ParameterInfo parameter in propertyInfo.GetIndexParameters())
                            {
                                JObject param = new JObject();
                                param["name"] = parameter.Name;
                                param["is_in"] = parameter.IsIn;
                                param["is_out"] = parameter.IsOut;
                                param["is_ref"] = parameter.ParameterType.IsByRef && !parameter.IsOut;
                                param["is_optional"] = parameter.IsOptional;
                                if (parameter.IsOptional)
                                    param["default_value"] = new JValue(parameter.RawDefaultValue);

                                JObject paramType = new JObject();
                                paramType["qualified_type"] = parameter.ParameterType.AssemblyQualifiedName;
                                paramType["full_name"] = parameter.ParameterType.FullName;
                                paramType["name"] = parameter.ParameterType.Name;

                                param["type"] = paramType;
                                indexerParameters.Add(param);
                            }

                            property["index_parameters"] = indexerParameters;
                        }

                        memberArray.Add(property);
                        break;

                    case MethodInfo methodInfo:
                        // get_Name など、特殊な名前の余計なメソッドを飛ばす
                        if (methodInfo.IsSpecialName)
                            continue;

                        JObject method = new JObject();
                        method["kind"] = "method";
                        method["name"] = methodInfo.Name;

                        JObject methodRetType = new JObject();
                        methodRetType["qualified_type"] = methodInfo.ReturnType.AssemblyQualifiedName;
                        methodRetType["full_name"] = methodInfo.ReturnType.FullName;
                        methodRetType["name"] = methodInfo.ReturnType.Name;

                        method["ret_type"] = methodRetType;

                        JArray parameters = new JArray();
                        foreach (ParameterInfo parameter in methodInfo.GetParameters())
                        {
                            JObject param = new JObject();
                            param["name"] = parameter.Name;
                            param["is_in"] = parameter.IsIn;
                            param["is_out"] = parameter.IsOut;
                            param["is_ref"] = parameter.ParameterType.IsByRef && !parameter.IsOut;
                            param["is_optional"] = parameter.IsOptional;
                            if (parameter.IsOptional)
                                param["default_value"] = new JValue(parameter.RawDefaultValue);

                            JObject paramType = new JObject();
                            paramType["qualified_type"] = parameter.ParameterType.AssemblyQualifiedName;
                            paramType["full_name"] = parameter.ParameterType.FullName;
                            paramType["name"] = parameter.ParameterType.Name;

                            param["type"] = paramType;
                            parameters.Add(param);
                        }

                        method["parameters"] = parameters;
                        memberArray.Add(method);
                        break;
                }
            }

            jObject["members"] = memberArray;
            return jObject;
        }
    }
}