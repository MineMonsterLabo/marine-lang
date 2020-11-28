namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class PropertyDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Property;

        public bool IsIndexer { get; }

        public ParameterDumpModel[] IndexerParameters { get; }

        public TypeDumpModel Type { get; }


        public bool CanRead { get; }
        public bool CanWrite { get; }

        public PropertyDumpModel(string name, TypeDumpModel type, bool canRead, bool canWrite, bool isStatic) : base(name, isStatic)
        {
            Type = type;

            CanRead = canRead;
            CanWrite = canWrite;
        }

        public PropertyDumpModel(string name, TypeDumpModel type, bool canRead, bool canWrite, ParameterDumpModel[] indexerParameters, bool isStatic) : this(name, type, canRead, canWrite, isStatic)
        {
            IsIndexer = true;

            IndexerParameters = indexerParameters;
        }
    }
}
