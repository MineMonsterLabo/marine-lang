namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class MethodDumpModel : MemberDumpModel
    {
        public override MemberDumpKind Kind => MemberDumpKind.Method;

        public TypeDumpModel RetType { get; }
        public ParameterDumpModel[] Parameters { get; }

        public MethodDumpModel(string name, TypeDumpModel retType, ParameterDumpModel[] parameters, bool isStatic) : base(name, isStatic)
        {
            RetType = retType;
            Parameters = parameters;
        }
    }
}
