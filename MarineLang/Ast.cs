namespace MarineLang
{
    public class ProgramAst
    {
        public FuncDefinitionAst[] funcDefinitionAsts;
    }

    public class FuncDefinitionAst
    {
        public string funcName;
        public FuncCallAst[] statementAsts;
    }

    public class FuncCallAst
    {
        public string funcName;
    }
}
