using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MarineLang.VirtualMachines.Attributes;
using MarineLang.VirtualMachines.Dumps.Members;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps
{
    public class ClassDumper
    {
        private object _value;

        public TypeDumper Type { get; }
        public IReadOnlyCollection<MemberDumper> MemberDumpers { get; }

        internal ClassDumper(object value)
        {
            _value = value;
        }

        public ClassDumper(JObject cls)
        {
            Type = new TypeDumper(cls["type"] as JObject);
            MemberDumpers = ((JArray) cls["members"]).Select<JToken, MemberDumper>(e =>
            {
                JObject jObject = (JObject) e;
                DumpMemberKind memberKind = jObject.Value<string>("kind").ToDumpKind();
                switch (memberKind)
                {
                    case DumpMemberKind.Field:
                        return new FieldDumper(jObject);

                    case DumpMemberKind.Property:
                        return new PropertyDumper(jObject);

                    case DumpMemberKind.Method:
                        return new MethodDumper(jObject);

                    default:
                        throw new NotSupportedException();
                }
            }).ToArray();
        }

        internal JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject["type"] = new TypeDumper(_value.GetType()).ToJObject();

            JArray memberArray = new JArray();
            var members = _value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(ClassAccessibilityChecker.CheckMember);
            foreach (MemberInfo memberInfo in members)
            {
                MemberDumper memberDumper = null;
                switch (memberInfo)
                {
                    case FieldInfo fieldInfo:
                        memberDumper = new FieldDumper(fieldInfo);
                        break;

                    case PropertyInfo propertyInfo:
                        memberDumper = new PropertyDumper(propertyInfo);
                        break;

                    case MethodInfo methodInfo:
                        memberDumper = new MethodDumper(methodInfo);
                        break;

                    default:
                        continue;
                }

                JObject member = memberDumper.ToJObject();
                if (member != null)
                    memberArray.Add(member);
            }

            jObject["members"] = memberArray;
            return jObject;
        }
    }
}