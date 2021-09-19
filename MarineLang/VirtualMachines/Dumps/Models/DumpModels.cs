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

        public TypeNameDumpModel TypeName { get; }

        public bool IsInitOnly { get; }

        public FieldDumpModel(TypeNameDumpModel typeName, bool isInitOnly, bool isStatic) : base(isStatic)
        {
            TypeName = typeName;

            IsInitOnly = isInitOnly;
        }
    }

    public class PropertyDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Property;

        public TypeNameDumpModel TypeName { get; }

        public bool CanRead { get; }
        public bool CanWrite { get; }

        public bool IsIndexer => Parameters.Count > 0;

        public Dictionary<string, ParameterDumpModel> Parameters { get; } =
            new Dictionary<string, ParameterDumpModel>();

        public PropertyDumpModel(TypeNameDumpModel typeName, bool canRead, bool canWrite, bool isStatic) :
            base(isStatic)
        {
            TypeName = typeName;

            CanRead = canRead;
            CanWrite = canWrite;
        }
    }

    public class MethodDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Method;

        public TypeNameDumpModel TypeName { get; }

        public Dictionary<string, ParameterDumpModel> Parameters { get; } =
            new Dictionary<string, ParameterDumpModel>();

        public MethodDumpModel(TypeNameDumpModel typeName, bool isStatic) : base(isStatic)
        {
            TypeName = typeName;
        }
    }

    public class ParameterDumpModel
    {
        public bool IsIn { get; }
        public bool IsOut { get; }
        public bool IsRef { get; }

        public bool IsOptional => DefaultValue != null;
        public object DefaultValue { get; }

        public TypeNameDumpModel TypeName { get; }

        public ParameterDumpModel(TypeNameDumpModel typeName, bool isIn, bool isOut, bool isRef, object defaultValue)
        {
            TypeName = typeName;

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