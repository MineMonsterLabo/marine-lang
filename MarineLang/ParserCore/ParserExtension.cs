using MarineLang.Models;
using MarineLang.Models.Errors;
using MineUtil;
using System;

namespace MarineLang.ParserCore
{
    public static class ParserExtension
    {
        public static Parse<I>.Parser<T> NamedError<T, I>(this Parse<I>.Parser<T> parser, Func<IInput<I>, ParseErrorInfo> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.TryGetError(out var parseErrorInfo))
                    return result.Error<T>(func(result.Remain));
                return result;
            };
        }

        public static Parse<I>.Parser<T> NamedError<T, I>
            (this Parse<I>.Parser<T> parser, ErrorCode errorCode, RangePosition rangePosition, string prefixErrorMessage = "")
        {
            return parser.NamedError(input =>
               new ParseErrorInfo(prefixErrorMessage, errorCode, rangePosition)
           );
        }

        public static Parse<I>.Parser<T> NamedError<T, I>
            (this Parse<I>.Parser<T> parser, ErrorCode errorCode, string prefixErrorMessage = "")
        {
            return parser.NamedError(input =>
                new ParseErrorInfo(prefixErrorMessage, errorCode, input.RangePosition)
            );
        }

        public static Parse<I>.Parser<TT> Right<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> parser2)
        {
            return input =>
            {
                var result = parser(input);
                input = result.Remain;
                if (result.TryGetError(out var parseErrorInfo))
                    return result.Error<TT>(parseErrorInfo);
                return parser2(input);
            };
        }

        public static Parse<I>.Parser<T> Left<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> parser2)
        {
            return input =>
            {
                var result = parser(input);
                input = result.Remain;
                if (result.Result.IsOk)
                {
                    var result2 = parser2(result.Remain);
                    input = result2.Remain;
                    if (result2.Result.IsError)
                        return result2.Error<T>(result2.Result.RawError);
                }
                return new ParseResult<T, I>(result.Result, input);
            };
        }

        public static Parse<I>.Parser<TT> Bind<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, Parse<I>.Parser<TT>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.TryGetError(out var parseErrorInfo))
                    return ParseResult.Error<TT, I>(parseErrorInfo, result.Remain);
                return func(result.Result.RawValue)(result.Remain);
            };
        }

        public static Parse<I>.Parser<V> SelectMany<T, U, V, I>(
                 this Parse<I>.Parser<T> parser,
                 Func<T, Parse<I>.Parser<U>> selector,
                 Func<T, U, V> projector)
        {
            return parser.Bind(t => selector(t).Select(u => projector(t, u)));
        }

        public static Parse<I>.Parser<TT> BindResult<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, IResult<TT, ParseErrorInfo>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.TryGetError(out var parseErrorInfo))
                    return result.Error<TT>(parseErrorInfo);
                return new ParseResult<TT, I>(func(result.Result.RawValue), result.Remain);
            };
        }

        public static Parse<I>.Parser<TT> MapResult<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => Result.Ok<TT, ParseErrorInfo>(func(t)));
        }

        public static Parse<I>.Parser<TT> Select<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, TT> selector)
        {
            return MapResult(parser, selector);
        }

        public static Parse<I>.Parser<T> Try<T, I>(this Parse<I>.Parser<T> parser)
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

        public static Parse<I>.Parser<T> Default<T, I>(this Parse<I>.Parser<T> parser, T defaultValue)
        {
            return input =>
            {
                var parseResult = parser(input);
                if (parseResult.Result.IsError)
                {
                    return parseResult.Ok(defaultValue);
                }
                return parseResult;
            };
        }
      
    }
}
