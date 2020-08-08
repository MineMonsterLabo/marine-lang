namespace MarineLang
{
    public struct ParseResult<AST>
    {
        public readonly bool isError;
        public readonly string errorMessage;
        public readonly AST ast;

        public ParseResult(bool isError, string errorMessage, AST ast)
        {
            this.isError = isError;
            this.errorMessage = errorMessage;
            this.ast = ast;
        }

        public static ParseResult<AST> Success(AST ast)
        {
            return new ParseResult<AST>(false, "", ast);
        }

        public static ParseResult<AST> Error(string errorMessage)
        {
            return new ParseResult<AST>(true, errorMessage, default);
        }
    }
}
