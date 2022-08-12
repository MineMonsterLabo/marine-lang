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
        public string Serialize(IReadonlyCsharpFuncTable csharpFuncTable,
            IReadOnlyDictionary<string, Type> staticTypeDict, IReadOnlyDictionary<string, object> globalVariableDict)
        {
            MarineDumpModel dumpModel = new MarineDumpModel();
            foreach (var type in staticTypeDict)
            {
                dumpModel.StaticTypes.Add(type.Key, AnalyzeReference(dumpModel, type.Value));
            }

            foreach ((string FuncName, MethodInfo MethodInfo) in csharpFuncTable.GetFuncs())
            {
                dumpModel.GlobalMethods.Add(FuncName, Analyze(dumpModel, MethodInfo));
            }

            foreach (KeyValuePair<string, object> variable in globalVariableDict)
            {
                dumpModel.GlobalVariables.Add(variable.Key, AnalyzeReference(dumpModel, variable.Value.GetType()));
            }

            return JObject.FromObject(dumpModel).ToString(Formatting.Indented);
        }

        private TypeNameDumpModel AnalyzeReference(MarineDumpModel marineDumpModel, Type type)
        {
            if (type.FullName != null && !marineDumpModel.Types.ContainsKey(type.FullName))
                Analyze(marineDumpModel, type);

            return new TypeNameDumpModel(type.AssemblyQualifiedName, type.FullName, type.Name);
        }

        private void Analyze(MarineDumpModel marineDumpModel, Type type)
        {
            TypeDumpModel typeDumpModel = marineDumpModel.Types[type.FullName] = new TypeDumpModel(ConvertKind(type));
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

        private TypeDumpKind ConvertKind(Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) == null && type.IsClass && type.IsAbstract && type.IsSealed)
                return TypeDumpKind.StaticType;

            if (type.IsClass && type.IsAbstract)
                return TypeDumpKind.AbstractClass;

            if (type.IsClass)
                return TypeDumpKind.Class;

            if (type.IsInterface)
                return TypeDumpKind.Interface;

            if (type.IsPrimitive)
                return TypeDumpKind.Primitive;

            if (type.IsValueType)
                return TypeDumpKind.Struct;

            return TypeDumpKind.Enum;
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
            return new FieldDumpModel(AnalyzeReference(marineDumpModel, fieldInfo.FieldType),
                fieldInfo.IsInitOnly, fieldInfo.IsStatic);
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
            return new ParameterDumpModel(
                AnalyzeReference(marineDumpModel, parameterInfo.ParameterType), parameterInfo.IsIn, parameterInfo.IsOut,
                isRef, parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : null);
        }
    }
}