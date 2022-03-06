using MarineLang.Models.Asts;

namespace MarineLang.VirtualMachines
{
    public class MarineProgramUnit
    {
        public string Name { get; }
        public string[] NamespaceStrings { get; }
        public ProgramAst ProgramAst { get; }

        public MarineProgramUnit(string name, string[] namespaceStrings, ProgramAst programAst)
        {
            Name = name;
            NamespaceStrings = namespaceStrings;
            ProgramAst = programAst;
        }

        public MarineProgramUnit(string name, ProgramAst programAst)
        {
            Name = name;
            NamespaceStrings = new string[] { };
            ProgramAst = programAst;
        }

        public MarineProgramUnit(string[] namespaceStrings, ProgramAst programAst)
        {
            Name = string.Empty;
            NamespaceStrings = namespaceStrings;
            ProgramAst = programAst;
        }

        public MarineProgramUnit(ProgramAst programAst)
        {
            Name = string.Empty;
            NamespaceStrings = new string[] { };
            ProgramAst = programAst;
        }
    }
}
