namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class ClassDumpModel
    {
        public TypeDumpModel Type { get; }
        public MemberDumpModel[] Members { get; }

        public ClassDumpModel(TypeDumpModel type, MemberDumpModel[] members)
        {
            Type = type;
            Members = members;
        }
    }
}
