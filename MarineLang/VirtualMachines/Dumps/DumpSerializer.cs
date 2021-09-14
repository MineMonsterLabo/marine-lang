using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MarineLang.VirtualMachines.Dumps.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps
{
    public class DumpSerializer
    {
        public string Serialize(IDictionary<string, MethodInfo> methodInfoDict,
            IDictionary<string, Type> staticTypeDict, IDictionary<string, object> globalVariableDict)
        {
            MarineDumpModel dumpModel = new MarineDumpModel();
            foreach (var type in staticTypeDict)
            {
                dumpModel.StaticTypes.Add(type.Key, AnalyzeReference(dumpModel, type.Value));
            }

            foreach (KeyValuePair<string, MethodInfo> method in methodInfoDict)
            {
                dumpModel.GlobalMethods.Add(method.Key, Analyze(dumpModel, method.Value));
            }

            foreach (KeyValuePair<string, object> variable in globalVariableDict)
            {
                dumpModel.GlobalVariables.Add(variable.Key, AnalyzeReference(dumpModel, variable.Value.GetType()));
            }

            return JObject.FromObject(dumpModel).ToString(Formatting.Indented);
        }

        private TypReferenceDumpModel AnalyzeReference(MarineDumpModel marineDumpModel, Type type)
        {
            if (type.FullName != null && !marineDumpModel.Types.ContainsKey(type.FullName))
                Analyze(marineDumpModel, type);

            return new TypReferenceDumpModel(type.Assembly.GetName().Name, type.FullName);
        }

        private void Analyze(MarineDumpModel marineDumpModel, Type type)
        {
            TypeDumpModel typeDumpModel = marineDumpModel.Types[type.FullName] = new TypeDumpModel();
            foreach (MemberInfo memberInfo in type.GetMembers())
            {
                var member = Analyze(marineDumpModel, memberInfo);
                if (member == null)
                    continue;

                if (typeDumpModel.Members.ContainsKey(memberInfo.Name))
                    typeDumpModel.Members[memberInfo.Name].Add(member);
                else
                    typeDumpModel.Members[memberInfo.Name] = new List<MemberDumpModel> { member };
            }
        }

        private MemberDumpModel Analyze(MarineDumpModel marineDumpModel, MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    return Analyze(marineDumpModel, fieldInfo);

                case PropertyInfo propertyInfo:
                    return Analyze(marineDumpModel, propertyInfo);

                case MethodInfo methodInfo:
                    if (methodInfo.IsSpecialName)
                        return null;

                    return Analyze(marineDumpModel, methodInfo);

                default:
                    return null;
            }
        }

        private FieldDumpModel Analyze(MarineDumpModel marineDumpModel, FieldInfo fieldInfo)
        {
            FieldDumpModel fieldDumpModel = new FieldDumpModel(AnalyzeReference(marineDumpModel, fieldInfo.FieldType),
                fieldInfo.IsInitOnly, fieldInfo.IsStatic);
            return fieldDumpModel;
        }

        private PropertyDumpModel Analyze(MarineDumpModel marineDumpModel, PropertyInfo propertyInfo)
        {
            bool isStatic = propertyInfo.GetAccessors().Any(e => e.IsStatic);
            PropertyDumpModel propertyDumpModel = new PropertyDumpModel(
                AnalyzeReference(marineDumpModel, propertyInfo.PropertyType), propertyInfo.CanRead,
                propertyInfo.CanWrite, isStatic);
            foreach (var parameter in propertyInfo.GetIndexParameters())
            {
                propertyDumpModel.Parameters.Add(parameter.Name, Analyze(marineDumpModel, parameter));
            }

            return propertyDumpModel;
        }

        private MethodDumpModel Analyze(MarineDumpModel marineDumpModel, MethodInfo methodInfo)
        {
            MethodDumpModel methodDumpModel =
                new MethodDumpModel(AnalyzeReference(marineDumpModel, methodInfo.ReturnType), methodInfo.IsStatic);
            int idx = 0;
            foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
            {
                methodDumpModel.Parameters.Add(parameterInfo.Name ?? $"p{idx++}",
                    Analyze(marineDumpModel, parameterInfo));
            }

            return methodDumpModel;
        }

        private ParameterDumpModel Analyze(MarineDumpModel marineDumpModel, ParameterInfo parameterInfo)
        {
            bool isRef = parameterInfo.ParameterType.IsByRef && !parameterInfo.IsOut;
            ParameterDumpModel parameterDumpModel = new ParameterDumpModel(
                AnalyzeReference(marineDumpModel, parameterInfo.ParameterType), parameterInfo.IsIn, parameterInfo.IsOut,
                isRef, parameterInfo.RawDefaultValue);

            return parameterDumpModel;
        }
    }
}