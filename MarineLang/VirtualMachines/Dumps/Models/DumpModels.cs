using System.Collections.Generic;

namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class MarineDumpModel
    {
        public Dictionary<string, TypReferenceDumpModel> StaticTypes { get; } =
            new Dictionary<string, TypReferenceDumpModel>();

        public Dictionary<string, MethodDumpModel> GlobalMethods { get; } =
            new Dictionary<string, MethodDumpModel>();

        public Dictionary<string, TypReferenceDumpModel> GlobalVariables { get; } =
            new Dictionary<string, TypReferenceDumpModel>();

        public Dictionary<string, TypeDumpModel> Types { get; } = new Dictionary<string, TypeDumpModel>();
    }

    public class TypReferenceDumpModel
    {
        public string QualifiedName { get; }
        public string FullName { get; }
        public string Name { get; }

        public TypReferenceDumpModel(string qualifiedName, string fullName, string name)
        {
            QualifiedName = qualifiedName;
            FullName = fullName;
            Name = name;
        }
    }

    public class TypeDumpModel
    {
        public Dictionary<string, List<MemberDumpModel>> Members { get; } =
            new Dictionary<string, List<MemberDumpModel>>();
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

        public TypReferenceDumpModel TypeRef { get; }

        public bool IsInitOnly { get; }

        public FieldDumpModel(TypReferenceDumpModel typeRef, bool isInitOnly, bool isStatic) : base(isStatic)
        {
            TypeRef = typeRef;

            IsInitOnly = isInitOnly;
        }
    }

    public class PropertyDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Property;

        public TypReferenceDumpModel TypeRef { get; }

        public bool CanRead { get; }
        public bool CanWrite { get; }

        public bool IsIndexer => Parameters.Count > 0;

        public Dictionary<string, ParameterDumpModel> Parameters { get; } =
            new Dictionary<string, ParameterDumpModel>();

        public PropertyDumpModel(TypReferenceDumpModel typeRef, bool canRead, bool canWrite, bool isStatic) :
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

        public TypReferenceDumpModel TypeRef { get; }

        public Dictionary<string, ParameterDumpModel> Parameters { get; } =
            new Dictionary<string, ParameterDumpModel>();

        public MethodDumpModel(TypReferenceDumpModel typeRef, bool isStatic) : base(isStatic)
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

        public TypReferenceDumpModel TypeRef { get; }

        public ParameterDumpModel(TypReferenceDumpModel typeRef, bool isIn, bool isOut, bool isRef, object defaultValue)
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
        public static TypeDumpModel GetTypeDumpModel(this TypReferenceDumpModel model, MarineDumpModel dumpModel)
        {
            return dumpModel.Types[model.FullName];
        }
    }
}