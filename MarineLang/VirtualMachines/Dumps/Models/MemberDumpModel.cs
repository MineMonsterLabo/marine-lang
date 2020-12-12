namespace MarineLang.VirtualMachines.Dumps.Models
{
    public abstract class MemberDumpModel
    {
        public abstract MemberDumpKind Kind { get; }
        public bool IsStatic { get; }
        public string Name { get; }

        public MemberDumpModel(string name, bool isStatic)
        {
            Name = name;
            IsStatic = isStatic;
        }
    }
}
