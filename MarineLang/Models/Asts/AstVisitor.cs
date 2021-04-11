namespace MarineLang.Models.Asts
{
    public abstract class AstVisitor<T>
    {
        public virtual T Visit(ProgramAst ast) { return default; }
        public virtual T Visit(FuncDefinitionAst ast) { return default; }
        public virtual T Visit(StatementAst ast) { return default; }
        public virtual T Visit(ExprStatementAst ast) { return default; }
        public virtual T Visit(ReturnAst ast) { return default; }
        public virtual T Visit(AssignmentVariableAst ast) { return default; }
        public virtual T Visit(ReAssignmentVariableAst ast) { return default; }
        public virtual T Visit(ReAssignmentIndexerAst ast) { return default; }
        public virtual T Visit(InstanceFieldAssignmentAst ast) { return default; }
        public virtual T Visit(StaticFieldAssignmentAst ast) { return default; }
        public virtual T Visit(WhileAst ast) { return default; }
        public virtual T Visit(ForAst ast) { return default; }
        public virtual T Visit(ForEachAst ast) { return default; }
        public virtual T Visit(YieldAst ast) { return default; }
        public virtual T Visit(BreakAst ast) { return default; }
        public virtual T Visit(ExprAst ast) { return default; }
        public virtual T Visit(ValueAst ast) { return default; }
        public virtual T Visit(VariableAst ast) { return default; }
        public virtual T Visit(BinaryOpAst ast) { return default; }
        public virtual T Visit(UnaryOpAst ast) { return default; }
        public virtual T Visit(InstanceFuncCallAst ast) { return default; }
        public virtual T Visit(InstanceFieldAst ast) { return default; }
        public virtual T Visit(StaticFuncCallAst ast) { return default; }
        public virtual T Visit(StaticFieldAst ast) { return default; }
        public virtual T Visit(AwaitAst ast) { return default; }
        public virtual T Visit(IfExprAst ast) { return default; }
        public virtual T Visit(GetIndexerAst ast) { return default; }
        public virtual T Visit(ArrayLiteralAst ast) { return default; }
        public virtual T Visit(ActionAst ast) { return default; }
        public virtual T Visit(FuncCallAst ast) { return default; }
    }
}
