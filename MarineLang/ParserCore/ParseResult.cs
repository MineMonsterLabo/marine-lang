using MarineLang.Models.Errors;
using MineUtil;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.ParserCore
{
    public interface IParseResult<out T, I>
    {
        IResult<T, ParseErrorInfo> Result { get; }
        IInput<I> Remain { get; }
        IParseResult<TT, I> Ok<TT>(TT value);
        IParseResult<TT, I> CastError<TT>();
        IParseResult<TT, I> Error<TT>(ParseErrorInfo error);
        IParseResult<TT, I> ReplaceError<TT>(ParseErrorInfo error);
        IEnumerable<ParseErrorInfo> ErrorStack { get; }
        IParseResult<T, I> ChainLeft<TT>(IParseResult<TT, I> next);
        IParseResult<TT, I> ChainRight<TT>(IParseResult<TT, I> next);
        IParseResult<T, I> SetRemain(IInput<I> remain);
        IParseResult<TT, I> SetResult<TT>(IResult<TT, ParseErrorInfo> result);
    }

    public class ParseResult<T, I> : IParseResult<T, I>
    {
        public IResult<T, ParseErrorInfo> Result { get; }
        public IInput<I> Remain { get; }
        public IEnumerable<ParseErrorInfo> ErrorStack { get; }

        private ParseResult(IResult<T, ParseErrorInfo> result, IInput<I> remain, IEnumerable<ParseErrorInfo> errorStack)
        {
            Result = result;
            Remain = remain;
            ErrorStack = errorStack;
        }

        public IParseResult<T, I> SetRemain(IInput<I> remain)
        {
            return new ParseResult<T, I>(Result, remain, ErrorStack);
        }

        public IParseResult<TT, I> SetResult<TT>(IResult<TT, ParseErrorInfo> result)
        {
            return new ParseResult<TT, I>(result, Remain, result.IsOk ? ErrorStack : ErrorStack.Concat(new[] { result.RawError }));
        }

        public IParseResult<TT, I> Ok<TT>(TT value)
        {
            return new ParseResult<TT, I>(MineUtil.Result.Ok<TT, ParseErrorInfo>(value), Remain, ErrorStack);
        }

        public IParseResult<TT, I> CastError<TT>()
        {
            return new ParseResult<TT, I>(
                MineUtil.Result.Error<TT, ParseErrorInfo>(Result.RawError),
                Remain,
                ErrorStack
            );
        }

        public static IParseResult<T, I> NewError(ParseErrorInfo error, IInput<I> remain)
        {
            return new ParseResult<T, I>(
                MineUtil.Result.Error<T, ParseErrorInfo>(error),
                remain,
                new[] { error }
            );
        }

        public static IParseResult<T, I> NewOk(T value, IInput<I> remain)
        {
            return new ParseResult<T, I>(
                MineUtil.Result.Ok<T, ParseErrorInfo>(value),
                remain,
                Enumerable.Empty<ParseErrorInfo>()
            );
        }

        public IParseResult<TT, I> Error<TT>(ParseErrorInfo error)
        {
            return new ParseResult<TT, I>(
                MineUtil.Result.Error<TT, ParseErrorInfo>(error),
                Remain,
                ErrorStack.Concat(new[] { error })
            );
        }

        public IParseResult<TT, I> ReplaceError<TT>(ParseErrorInfo error)
        {
            return new ParseResult<TT, I>(
                MineUtil.Result.Error<TT, ParseErrorInfo>(error),
                Remain,
                ErrorStack.Take(ErrorStack.Count()-1).Concat(new[] { error })
            );
        }

        public IParseResult<T, I> ChainLeft<TT>(IParseResult<TT, I> next)
        {
            return new ParseResult<T, I>(
                next.Result.IsOk ? Result : MineUtil.Result.Error<T, ParseErrorInfo>(next.Result.RawError),
                next.Remain,
                ErrorStack.Concat(next.ErrorStack)
            );
        }

        public IParseResult<TT, I> ChainRight<TT>(IParseResult<TT, I> next)
        {
            return new ParseResult<TT, I>(
                next.Result,
                next.Remain,
                ErrorStack.Concat(next.ErrorStack)
            );
        }
    }

    public static class ParseResult
    {
        public static IParseResult<T, I> NewOk<T, I>(T value, IInput<I> remain)
        {
            return ParseResult<T, I>.NewOk(value, remain);
        }

        public static IParseResult<T, I> NewError<T, I>(ParseErrorInfo error, IInput<I> remain)
        {
            return ParseResult<T, I>.NewError(error, remain);
        }
    }
}
