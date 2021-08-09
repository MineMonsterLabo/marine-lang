namespace MarineLang.ParserCore
{
    public delegate IParseResult<T,I> Parser<out T, I>(IInput<I> input);
}
