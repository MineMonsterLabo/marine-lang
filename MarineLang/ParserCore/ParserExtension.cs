using MarineLang.Models;
using MarineLang.Models.Errors;
using MineUtil;
using System;

namespace MarineLang.ParserCore
{
    public static class ParserExtension
    {
        public static Parser<T, I> InCompleteError<T, I>(this Parser<T, I> parser, Func<IInput<I>, ParseErrorInfo> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.TryGetError(out var parseErrorInfo) && parseErrorInfo.ErrorKind == ErrorKind.InComplete)
                    return ParseResult.Error<T, I>(func(result.Remain), result.Remain);
                return result;
            };
        }

        public static Parser<T, I> InCompleteError<T, I>
            (this Parser<T, I> parser, ErrorCode errorCode, RangePosition rangePosition, ErrorKind errorKind = ErrorKind.None, string prefixErrorMessage = "")
        {
            return parser.InCompleteError(input =>
               new ParseErrorInfo(prefixErrorMessage, errorCode, errorKind, rangePosition)
           );
        }

        public static Parser<T, I> InCompleteErrorWithPositionEnd<T, I>
            (this Parser<T, I> parser, ErrorCode errorCode, ErrorKind errorKind = ErrorKind.None, string prefixErrorMessage = "")
        {
            return parser.InCompleteError(input =>
                new ParseErrorInfo(prefixErrorMessage, errorCode, errorKind, input.RangePosition)
            );
        }

        public static Parser<T, I> InCompleteErrorWithPositionHead<T, I>
            (this Parser<T, I> parser, ErrorCode errorCode, ErrorKind errorKind = ErrorKind.None, string prefixErrorMessage = "")
        {
            return parser.InCompleteError(input =>
                new ParseErrorInfo(prefixErrorMessage, errorCode, errorKind, input.RangePosition)
            );
        }

        public static Parser<TT, I> Right<T, TT, I>(this Parser<T, I> parser, Parser<TT, I> parser2)
        {
            return input =>
            {
                var result = parser(input);
                input = result.Remain;
                if (result.TryGetError(out var parseErrorInfo))
                    return ParseResult.Error<TT, I>(parseErrorInfo, input);
                if (result.Remain.IsEnd)
                    return ParseResult.Error<TT, I>(new ParseErrorInfo(ErrorKind.InComplete), input);
                return parser2(input);
            };
        }

        public static Parser<T, I> Left<T, TT, I>(this Parser<T, I> parser, Parser<TT, I> parser2)
        {
            return input =>
            {
                var result = parser(input);
                input = result.Remain;
                if (result.Result.IsError == false)
                {
                    if (result.Remain.IsEnd)
                        return ParseResult.Error<T, I>(new ParseErrorInfo(ErrorKind.InComplete), input);
                    var result2 = parser2(result.Remain);
                    input = result2.Remain;
                    if (result2.Result.IsError)
                        return ParseResult.Error<T, I>(result2.Result.RawError, input);
                }
                return new ParseResult<T, I>(result.Result, input);
            };
        }

        public static Parser<T, I> ExpectCanMoveNext<T, I>(this Parser<T, I> parser)
        {
            return input =>
            {
                var result = parser(input);
                if (result.Result.IsError == false && result.Remain.IsEnd)
                    return ParseResult.Error<T, I>(new ParseErrorInfo(ErrorKind.InComplete), result.Remain);
                return result;
            };
        }

        public static Parser<TT, I> Bind<T, TT, I>(this Parser<T, I> parser, Func<T, Parser<TT, I>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.TryGetError(out var parseErrorInfo))
                    return ParseResult.Error<TT, I>(parseErrorInfo, result.Remain);
                return func(result.Result.RawValue)(result.Remain);
            };
        }

        public static Parser<V, I> SelectMany<T, U, V, I>(
                 this Parser<T, I> parser,
                 Func<T, Parser<U, I>> selector,
                 Func<T, U, V> projector)
        {
            return parser.Bind(t => selector(t).Select(u => projector(t, u)));
        }

        public static Parser<TT, I> BindResult<T, TT, I>(this Parser<T, I> parser, Func<T, IResult<TT, ParseErrorInfo>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.TryGetError(out var parseErrorInfo))
                    return ParseResult.Error<TT, I>(parseErrorInfo, result.Remain);
                return new ParseResult<TT, I>(func(result.Result.RawValue), result.Remain);
            };
        }

        public static Parser<TT, I> MapResult<T, TT, I>(this Parser<T, I> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => Result.Ok<TT, ParseErrorInfo>(func(t)));
        }

        public static Parser<TT, I> Select<T, TT, I>(this Parser<T, I> parser, Func<T, TT> selector)
        {
            return MapResult(parser, selector);
        }

        public static Parser<T, I> Try<T, I>(this Parser<T, I> parser)
        {
            return
                input =>
                {
                    var parseResult = parser(input);

                    if (parseResult.TryGetError(out var parseErrorInfo))
                        return ParseResult.Error<T, I>(parseErrorInfo, input);

                    return parseResult;
                };
        }

        public static Parser<T, I> Default<T, I>(this Parser<T, I> parser, T defaultValue)
        {
            return input =>
            {
                var parseResult = parser(input);
                if (parseResult.Result.IsError)
                {
                    return ParseResult.Ok(defaultValue, parseResult.Remain);
                }
                return parseResult;
            };
        }
    }
}
