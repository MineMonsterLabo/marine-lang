using MarineLang.Models.Asts;

namespace MarineLang.VirtualMachines
{
    public class MarineProgramUnit
    {
        public readonly string[] namespaceStrings;
        public readonly ProgramAst programAst;

        public MarineProgramUnit(string[] namespaceStrings, ProgramAst programAst)
        {
            this.namespaceStrings = namespaceStrings;
            this.programAst = programAst;
        }
    }
}
