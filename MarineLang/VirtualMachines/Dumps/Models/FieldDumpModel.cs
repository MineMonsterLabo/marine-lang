namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class FieldDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Field;

        public bool IsReadOnly { get; }

        public TypeDumpModel Type { get; }

        public FieldDumpModel(string name, TypeDumpModel type, bool isReadOnly, bool isStatic) : base(name, isStatic)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }
    }
}
