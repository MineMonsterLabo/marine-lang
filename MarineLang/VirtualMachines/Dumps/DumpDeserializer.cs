using System;
using System.Collections.Generic;
using MarineLang.VirtualMachines.Dumps.Models;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps
{
    public class DumpDeserializer
    {
        public MarineDumpModel Deserialize(string json)
        {
            MarineDumpModel dumpModel = new MarineDumpModel();
            JObject jObject = JObject.Parse(json);
            JObject staticTypes = (JObject)jObject["StaticTypes"] ?? new JObject();
            JObject globalMethods = (JObject)jObject["GlobalMethods"] ?? new JObject();
            JObject globalVariables = (JObject)jObject["GlobalVariables"] ?? new JObject();
            JObject types = (JObject)jObject["Types"] ?? new JObject();

            foreach (var staticType in staticTypes)
            {
                dumpModel.StaticTypes[staticType.Key] = DeserializeTypeReference((JObject)staticType.Value);
            }

            foreach (var method in globalMethods)
            {
                dumpModel.GlobalMethods[method.Key] = DeserializeMethod((JObject)method.Value);
            }

            foreach (var variable in globalVariables)
            {
                dumpModel.GlobalVariables[variable.Key] = DeserializeTypeReference((JObject)variable.Value);
            }

            foreach (var type in types)
            {
                dumpModel.Types[type.Key] = DeserializeType((JObject)type.Value);
            }

            return dumpModel;
        }

        private TypeNameDumpModel DeserializeTypeReference(JObject jObject)
        {
            return new TypeNameDumpModel(jObject.Value<string>("QualifiedName"), jObject.Value<string>("FullName"),
                jObject.Value<string>("Name"));
        }

        private TypeDumpModel DeserializeType(JObject jObject)
        {
            TypeDumpModel dumpModel = new TypeDumpModel((TypeDumpKind)jObject.Value<int>("Kind"));
            JObject members = (JObject)jObject["Members"];
            foreach (var member in members)
            {
                List<MemberDumpModel> list = new List<MemberDumpModel>();
                JArray jArray = (JArray)member.Value ?? new JArray();
                foreach (var value in jArray)
                {
                    list.Add(DeserializeMember((JObject)value));
                }

                dumpModel.Members[member.Key] = list;
            }

            return dumpModel;
        }

        private MemberDumpModel DeserializeMember(JObject jObject)
        {
            var kind = (MemberDumpKind)jObject.Value<int>("Kind");
            switch (kind)
            {
                case MemberDumpKind.Field:
                    return DeserializeField(jObject);

                case MemberDumpKind.Property:
                    return DeserializeProperty(jObject);

                case MemberDumpKind.Method:
                    return DeserializeMethod(jObject);

                default:
                    throw new NotSupportedException();
            }
        }

        private FieldDumpModel DeserializeField(JObject jObject)
        {
            return new FieldDumpModel(DeserializeTypeReference((JObject)jObject["TypeRef"]),
                jObject.Value<bool>("IsInitOnly"), jObject.Value<bool>("IsStatic"));
        }

        private PropertyDumpModel DeserializeProperty(JObject jObject)
        {
            PropertyDumpModel dumpModel = new PropertyDumpModel(DeserializeTypeReference((JObject)jObject["TypeRef"]),
                jObject.Value<bool>("CanRead"), jObject.Value<bool>("CanWrite"), jObject.Value<bool>("IsStatic"));
            JObject parameters = (JObject)(jObject["Parameters"] ?? new JObject());
            foreach (var parameter in parameters)
            {
                dumpModel.Parameters[parameter.Key] = DeserializeParameter((JObject)parameter.Value);
            }

            return dumpModel;
        }

        private MethodDumpModel DeserializeMethod(JObject jObject)
        {
            MethodDumpModel dumpModel = new MethodDumpModel(DeserializeTypeReference((JObject)jObject["TypeRef"]),
                jObject.Value<bool>("IsStatic"));
            JObject parameters = (JObject)(jObject["Parameters"] ?? new JObject());
            foreach (var parameter in parameters)
            {
                dumpModel.Parameters[parameter.Key] = DeserializeParameter((JObject)parameter.Value);
            }

            return dumpModel;
        }

        private ParameterDumpModel DeserializeParameter(JObject jObject)
        {
            return new ParameterDumpModel(DeserializeTypeReference((JObject)jObject["TypeRef"]),
                jObject.Value<bool>("IsIn"), jObject.Value<bool>("IsOut"), jObject.Value<bool>("IsRef"),
                jObject.Value<object>("DefaultValue"));
        }
    }
}