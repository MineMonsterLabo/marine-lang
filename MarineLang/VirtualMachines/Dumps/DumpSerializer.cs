using MarineLang.VirtualMachines.Dumps.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MarineLang.VirtualMachines.Dumps
{
    public class DumpSerializer
    {
        public string Serialize(IDictionary<string, object> globalVariableDict)
        {
            JObject dumps = new JObject();
            foreach (KeyValuePair<string, object> pair in globalVariableDict)
            {
                dumps[pair.Key] = JObject.FromObject(SerializeClass(pair.Value));
            }

            return dumps.ToString(Formatting.Indented);
        }

        private ClassDumpModel SerializeClass(object value)
        {
            var type = value.GetType();
            return new ClassDumpModel(SerializeType(type), SerializeMembers(type.GetMembers()));
        }

        private MemberDumpModel[] SerializeMembers(MemberInfo[] members)
        {
            List<MemberDumpModel> memberList = new List<MemberDumpModel>();
            foreach (MemberInfo member in members)
            {
                switch (member)
                {
                    case FieldInfo fieldInfo:
                        memberList.Add(SerializeField(fieldInfo));
                        break;

                    case PropertyInfo propertyInfo:
                        memberList.Add(SerializeProperty(propertyInfo));
                        break;

                    case MethodInfo methodInfo:
                        if (methodInfo.IsSpecialName)
                            continue;

                        memberList.Add(SerializeMethod(methodInfo));
                        break;

                    default:
                        continue;
                }
            }

            return memberList.ToArray();
        }

        private FieldDumpModel SerializeField(FieldInfo fieldInfo)
        {
            return new FieldDumpModel(fieldInfo.Name, SerializeType(fieldInfo.FieldType), fieldInfo.IsInitOnly, fieldInfo.IsStatic);
        }

        private PropertyDumpModel SerializeProperty(PropertyInfo propertyInfo)
        {
            bool isIndexer = propertyInfo.GetIndexParameters().Length > 0;
            bool isStatic = propertyInfo.GetAccessors().Any(e => e.IsStatic);
            if (isIndexer)
                return new PropertyDumpModel(propertyInfo.Name, SerializeType(propertyInfo.PropertyType), propertyInfo.CanRead, propertyInfo.CanWrite, SerializeParameters(propertyInfo.GetIndexParameters()), isStatic);
            else
                return new PropertyDumpModel(propertyInfo.Name, SerializeType(propertyInfo.PropertyType), propertyInfo.CanRead, propertyInfo.CanWrite, isStatic);
        }

        private MethodDumpModel SerializeMethod(MethodInfo methodInfo)
        {
            return new MethodDumpModel(methodInfo.Name, SerializeType(methodInfo.ReturnType), SerializeParameters(methodInfo.GetParameters()), methodInfo.IsStatic);
        }

        private ParameterDumpModel[] SerializeParameters(ParameterInfo[] parameters)
        {
            List<ParameterDumpModel> parameterList = new List<ParameterDumpModel>();
            foreach (ParameterInfo parameter in parameters)
            {
                bool isRef = parameter.ParameterType.IsByRef && !parameter.IsOut;
                if (parameter.IsOptional)
                    parameterList.Add(new ParameterDumpModel(parameter.Name, SerializeType(parameter.ParameterType), parameter.IsIn, parameter.IsOut, isRef));
                else
                    parameterList.Add(new ParameterDumpModel(parameter.Name, SerializeType(parameter.ParameterType), parameter.IsIn, parameter.IsOut, isRef, parameter.RawDefaultValue));
            }

            return parameterList.ToArray();
        }

        private TypeDumpModel SerializeType(Type type)
        {
            return new TypeDumpModel(type.AssemblyQualifiedName, type.FullName, type.Name);
        }
    }
}
