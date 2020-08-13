using MarineLang.Streams;

namespace MarineLang.SyntaxAnalysis
{
    public delegate IParseResult<T> Parser<out T>(TokenStream stream);
}
