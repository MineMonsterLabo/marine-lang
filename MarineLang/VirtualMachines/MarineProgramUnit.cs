using MarineLang.Models.Asts;

namespace MarineLang.VirtualMachines
{
    public class MarineProgramUnit
    {
        public readonly string namespaceString;
        public readonly ProgramAst programAst;

        public MarineProgramUnit(string namespaceString, ProgramAst programAst)
        {
            this.namespaceString = namespaceString;
            this.programAst = programAst;
        }
    }
}
