using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.ParserCore
{
    public class ParseResult<T, I>
    {
        public IResult<T, Unit> Result { get; }
        public IInput<I> Remain { get; }
        public IEnumerable<ParseErrorInfo> ErrorStack { get; }

        public bool IsOk => Result.IsOk;

        public bool IsError => Result.IsError;

        private ParseResult(IResult<T, Unit> result, IInput<I> remain, IEnumerable<ParseErrorInfo> errorStack)
        {
            Result = result;
            Remain = remain;
            ErrorStack = errorStack;
        }

        public IResult<T, IEnumerable<ParseErrorInfo>> ToResult()
        {
            return Result.IsOk && (ErrorStack == null || !ErrorStack.Any()) ?
                MineUtil.Result.Ok<T, IEnumerable<ParseErrorInfo>>(Result.RawValue) :
                MineUtil.Result.Error<T, IEnumerable<ParseErrorInfo>>(ErrorStack);
        }

        public ParseResult<T, I> SetRemain(IInput<I> remain)
        {
            return new ParseResult<T, I>(Result, remain, ErrorStack);
        }

        public ParseResult<TT, I> Ok<TT>(TT value)
        {
            return new ParseResult<TT, I>(MineUtil.Result.Ok<TT, Unit>(value), Remain, ErrorStack);
        }

        public ParseResult<TT, I> CastError<TT>()
        {
            return new ParseResult<TT, I>(
                MineUtil.Result.Error<TT, Unit>(Unit.Value),
                Remain,
                ErrorStack
            );
        }

        public static ParseResult<T, I> NewError(IEnumerable<ParseErrorInfo> errors, IInput<I> remain)
        {
            return new ParseResult<T, I>(
                MineUtil.Result.Error<T, Unit>(Unit.Value),
                remain,
                errors
            );
        }

        public static ParseResult<T, I> NewOk(T value, IInput<I> remain)
        {
            return new ParseResult<T, I>(
                MineUtil.Result.Ok<T, Unit>(value),
                remain,
                null
            );
        }

        public ParseResult<T, I> Error(ParseErrorInfo error)
        {
            return new ParseResult<T, I>(
                MineUtil.Result.Error<T, Unit>(Unit.Value),
                Remain,
                ErrorStack == null ? new[] { error } : ErrorStack.Append(error)
            );
        }

        public ParseResult<TT, I> Error<TT>(ParseErrorInfo error)
        {
            return new ParseResult<TT, I>(
                MineUtil.Result.Error<TT, Unit>(Unit.Value),
                Remain,
                ErrorStack == null ? new[] { error } : ErrorStack.Append(error)
            );
        }

        public ParseResult<TT, I> MapErrorStack<TT>(ParseErrorInfo error)
        {
            return new ParseResult<TT, I>(
                MineUtil.Result.Error<TT, Unit>(Unit.Value),
                Remain,
                ErrorStack == null ? new[] { error } : ErrorStack.SkipLast(1).Append(error)
            );
        }

        public ParseResult<T, I> ChainLeft<TT>(ParseResult<TT, I> next)
        {
            return Bind(_ => next).Map(_=>Result.RawValue);
        }

        public ParseResult<TT, I> ChainRight<TT>(ParseResult<TT, I> next)
        {
            return Bind(_ => next);
        }

        public ParseResult<TT, I> Bind<TT>(Func<T, ParseResult<TT, I>> func)
        {

            IEnumerable<ParseErrorInfo> ConcatErrorStack(IEnumerable<ParseErrorInfo> a, IEnumerable<ParseErrorInfo> b)
            {
                if (a == null)
                    return b;
                if (b == null)
                    return a;
                return a.Concat(b);
            }

            if (IsOk)
            {
                var r = func(Result.RawValue);
                return new ParseResult<TT, I>(r.Result, r.Remain, ConcatErrorStack(ErrorStack, r.ErrorStack));
            }
            else
            {
                return CastError<TT>();
            }
        }

        public ParseResult<TT, I> Map<TT>(Func<T, TT> func)
        {
            return IsOk ? Ok(func(Result.RawValue)) : CastError<TT>();
        }
    }

    public static class ParseResult
    {
        public static ParseResult<T, I> FromResult<T, I>(IResult<T, IEnumerable<ParseErrorInfo>> result, IInput<I> remain)
        {
            return result.IsOk ? NewOk(result.RawValue, remain) : NewError<T, I>(result.RawError, remain);
        }

        public static ParseResult<T, I> NewOk<T, I>(T value, IInput<I> remain)
        {
            return ParseResult<T, I>.NewOk(value, remain);
        }

        public static ParseResult<T, I> NewError<T, I>(ParseErrorInfo error, IInput<I> remain)
        {
            return ParseResult<T, I>.NewError(new[] { error }, remain);
        }

        public static ParseResult<T, I> NewError<T, I>(IEnumerable<ParseErrorInfo> errors, IInput<I> remain)
        {
            return ParseResult<T, I>.NewError(errors, remain);
        }

        public static ParseResult<TTT, I> Merge<T,TT, TTT,I>(this ParseResult<T, I> result1, ParseResult<TT, I> result2, Func<T, TT, TTT> func)
        {
            return result1.Bind(
                t => result2.Bind(
                    tt => ParseResult<TTT, I>.NewOk(func(t, tt), result2.Remain)
                )
            );
        }

        public static ParseResult<IEnumerable<T>, I> Append<T, TList, I>(this ParseResult<TList, I> result1, ParseResult<T, I> result2)
            where TList : IEnumerable<T>
        {
            return result1.Merge(result2, (list, element) => list.Append(element));
        }
    }
}
