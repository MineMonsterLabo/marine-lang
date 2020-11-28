using MarineLang.VirtualMachines.Dumps.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarineLang.VirtualMachines.Dumps
{
    public class DumpDeserializer
    {
        public IReadOnlyDictionary<string, ClassDumpModel> Deserialize(string json)
        {
            Dictionary<string, ClassDumpModel> pairs = new Dictionary<string, ClassDumpModel>();

            JObject jObject = JObject.Parse(json);
            foreach (KeyValuePair<string, JToken> pair in jObject)
            {
                pairs.Add(pair.Key, DeserializeClass(pair.Value as JObject));
            }

            return new ReadOnlyDictionary<string, ClassDumpModel>(pairs);
        }

        private ClassDumpModel DeserializeClass(JObject classData)
        {
            return new ClassDumpModel(DeserializeType(classData["Type"] as JObject), DeserializeMembers(classData["Members"] as JArray));
        }

        private MemberDumpModel[] DeserializeMembers(JArray membersData)
        {
            List<MemberDumpModel> memberList = new List<MemberDumpModel>();
            foreach (JToken member in membersData)
            {
                MemberDumpKind kind = (MemberDumpKind) member.Value<int>("Kind");
                switch (kind)
                {
                    case MemberDumpKind.Field:
                        memberList.Add(DeserializeField(member as JObject));
                        break;

                    case MemberDumpKind.Property:
                        memberList.Add(DeserializeProperty(member as JObject));
                        break;

                    case MemberDumpKind.Method:
                        memberList.Add(DeserializeMethod(member as JObject));
                        break;
                }
            }

            return memberList.ToArray();
        }

        private FieldDumpModel DeserializeField(JObject fieldData)
        {
            return new FieldDumpModel(fieldData.Value<string>("Name"), DeserializeType(fieldData["Type"] as JObject), fieldData.Value<bool>("IsInitOnly"), fieldData.Value<bool>("IsStatic"));
        }

        private PropertyDumpModel DeserializeProperty(JObject propertyData)
        {
            bool isIndexer = propertyData.Value<bool>("IsIndexer");
            if (isIndexer)
                return new PropertyDumpModel(propertyData.Value<string>("Name"), DeserializeType(propertyData["Type"] as JObject), propertyData.Value<bool>("CanRead"), propertyData.Value<bool>("CanWrite"), DeserializeParameters(propertyData["IndexerParameter"] as JArray), propertyData.Value<bool>("IsStatic"));
            else
                return new PropertyDumpModel(propertyData.Value<string>("Name"), DeserializeType(propertyData["Type"] as JObject), propertyData.Value<bool>("CanRead"), propertyData.Value<bool>("CanWrite"), propertyData.Value<bool>("IsStatic"));
        }

        private MethodDumpModel DeserializeMethod(JObject methodData)
        {
            return new MethodDumpModel(methodData.Value<string>("Name"), DeserializeType(methodData["RetType"] as JObject), DeserializeParameters(methodData["Parameters"] as JArray), methodData.Value<bool>("IsStatic"));
        }

        private ParameterDumpModel[] DeserializeParameters(JArray parametersData)
        {
            List<ParameterDumpModel> parameterList = new List<ParameterDumpModel>();
            foreach (JToken parameter in parametersData)
            {
                bool isOptional = parameter.Value<bool>("IsOptional");
                if (isOptional)
                    parameterList.Add(new ParameterDumpModel(parameter.Value<string>("Name"), DeserializeType(parameter["Type"] as JObject), parameter.Value<bool>("IsIn"), parameter.Value<bool>("IsOut"), parameter.Value<bool>("IsRef"), parameter.Value<object>("DefaultValue")));
                else
                    parameterList.Add(new ParameterDumpModel(parameter.Value<string>("Name"), DeserializeType(parameter["Type"] as JObject), parameter.Value<bool>("IsIn"), parameter.Value<bool>("IsOut"), parameter.Value<bool>("IsRef")));
            }

            return parameterList.ToArray();
        }

        private TypeDumpModel DeserializeType(JObject typeData)
        {
            return new TypeDumpModel(typeData.Value<string>("QualifiedName"), typeData.Value<string>("FullName"), typeData.Value<string>("Name"));
        }
    }
}
