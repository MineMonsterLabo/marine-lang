using MarineLang.Models.Errors;
using MineUtil;

namespace MarineLang.ParserCore
{
    public interface IParseResult<out T, I>
    {
        IResult<T, ParseErrorInfo> Result { get; }
        IInput<I> Remain { get; }
        IParseResult<TT, I> Ok<TT>(TT value);
        IParseResult<TT, I> Error<TT>(ParseErrorInfo error);
    }

    public class ParseResult<T, I> : IParseResult<T, I>
    {
        public IResult<T, ParseErrorInfo> Result { get; }
        public IInput<I> Remain { get; }

        public ParseResult(IResult<T, ParseErrorInfo> result, IInput<I> remain)
        {
            Result = result;
            Remain = remain;
        }

        public  IParseResult<TT, I> Ok<TT>(TT value)
        {
            return new ParseResult<TT, I>(MineUtil.Result.Ok<TT, ParseErrorInfo>(value), Remain);
        }

        public IParseResult<TT, I> Error<TT>(ParseErrorInfo error)
        {
            return new ParseResult<TT, I>(MineUtil.Result.Error<TT, ParseErrorInfo>(error), Remain);
        }
    }

    public static class ParseResult
    {
        public static IParseResult<T, I> Ok<T, I>(T value, IInput<I> remain)
        {
            return new ParseResult<T, I>(Result.Ok<T, ParseErrorInfo>(value), remain);
        }

        public static IParseResult<T, I> Error<T, I>(ParseErrorInfo error, IInput<I> remain)
        {
            return new ParseResult<T, I>(Result.Error<T, ParseErrorInfo>(error), remain);
        }

        public static bool TryGetError<T, I>(this IParseResult<T, I> parseResult, out ParseErrorInfo error)
        {
            error = parseResult.Result.IsError ? parseResult.Result.RawError : null;
            return parseResult.Result.IsError;
        }
    }
}
