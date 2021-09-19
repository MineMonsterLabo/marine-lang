using System.Collections.Generic;

namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class MarineDumpModel
    {
        public Dictionary<string, TypeNameDumpModel> StaticTypes { get; } =
            new Dictionary<string, TypeNameDumpModel>();

        public Dictionary<string, MethodDumpModel> GlobalMethods { get; } =
            new Dictionary<string, MethodDumpModel>();

        public Dictionary<string, TypeNameDumpModel> GlobalVariables { get; } =
            new Dictionary<string, TypeNameDumpModel>();

        public Dictionary<string, TypeDumpModel> Types { get; } = new Dictionary<string, TypeDumpModel>();
    }

    public class TypeNameDumpModel
    {
        public string QualifiedName { get; }
        public string FullName { get; }
        public string Name { get; }

        public TypeNameDumpModel(string qualifiedName, string fullName, string name)
        {
            QualifiedName = qualifiedName;
            FullName = fullName;
            Name = name;
        }
    }

    public enum TypeDumpKind
    {
        Class,
        AbstractClass,
        StaticType,
        Struct,
        Primitive,
        Interface,
        Enum
    }

    public class TypeDumpModel
    {
        public TypeDumpKind Kind { get; }

        public Dictionary<string, List<MemberDumpModel>> Members { get; } =
            new Dictionary<string, List<MemberDumpModel>>();

        public TypeDumpModel(TypeDumpKind kind)
        {
            Kind = kind;
        }
    }

    public enum MemberDumpKind
    {
        Field,
        Property,
        Method
    }

    public abstract class MemberDumpModel
    {
        public abstract MemberDumpKind Kind { get; }

        public bool IsStatic { get; }

        protected MemberDumpModel(bool isStatic)
        {
            IsStatic = isStatic;
        }
    }

    public class FieldDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Field;

        public TypeNameDumpModel TypeRef { get; }

        public bool IsInitOnly { get; }

        public FieldDumpModel(TypeNameDumpModel typeRef, bool isInitOnly, bool isStatic) : base(isStatic)
        {
            TypeRef = typeRef;

            IsInitOnly = isInitOnly;
        }
    }

    public class PropertyDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Property;

        public TypeNameDumpModel TypeRef { get; }

        public bool CanRead { get; }
        public bool CanWrite { get; }

        public bool IsIndexer => Parameters.Count > 0;

        public Dictionary<string, ParameterDumpModel> Parameters { get; } =
            new Dictionary<string, ParameterDumpModel>();

        public PropertyDumpModel(TypeNameDumpModel typeRef, bool canRead, bool canWrite, bool isStatic) :
            base(isStatic)
        {
            TypeRef = typeRef;

            CanRead = canRead;
            CanWrite = canWrite;
        }
    }

    public class MethodDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Method;

        public TypeNameDumpModel TypeRef { get; }

        public Dictionary<string, ParameterDumpModel> Parameters { get; } =
            new Dictionary<string, ParameterDumpModel>();

        public MethodDumpModel(TypeNameDumpModel typeRef, bool isStatic) : base(isStatic)
        {
            TypeRef = typeRef;
        }
    }

    public class ParameterDumpModel
    {
        public bool IsIn { get; }
        public bool IsOut { get; }
        public bool IsRef { get; }

        public bool IsOptional => DefaultValue != null;
        public object DefaultValue { get; }

        public TypeNameDumpModel TypeRef { get; }

        public ParameterDumpModel(TypeNameDumpModel typeRef, bool isIn, bool isOut, bool isRef, object defaultValue)
        {
            TypeRef = typeRef;

            IsIn = isIn;
            IsOut = isOut;
            IsRef = isRef;

            DefaultValue = defaultValue;
        }
    }

    public static class DumpModelExtensions
    {
        public static TypeDumpModel GetTypeDumpModel(this TypeNameDumpModel typeNameDumpModel,
            MarineDumpModel dumpModel)
        {
            return dumpModel.Types[typeNameDumpModel.FullName];
        }
    }
}