using MarineLang.Models;
using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;

namespace MarineLang.ParserCore
{
    public static class ParserExtensions
    {
        public static Parse<I>.Parser<T> NamedError<T, I>(this Parse<I>.Parser<T> parser, Func<RangePosition, ParseErrorInfo> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.Result.IsError)
                    return result.ReplaceError<T>(func(result.Remain.RangePosition));
                return result;
            };
        }

        public static Parse<I>.Parser<T> NamedError<T, I>
            (this Parse<I>.Parser<T> parser, ErrorCode errorCode, RangePosition rangePosition, string prefixErrorMessage = "")
        {
            return parser.NamedError(_ =>
               new ParseErrorInfo(prefixErrorMessage, errorCode, rangePosition)
           );
        }

        public static Parse<I>.Parser<T> NamedError<T, I>
            (this Parse<I>.Parser<T> parser, ErrorCode errorCode, string prefixErrorMessage = "")
        {
            return parser.NamedError(rangePosition =>
                new ParseErrorInfo(prefixErrorMessage, errorCode, rangePosition)
            );
        }

        public static Parse<I>.Parser<TT> Right<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> parser2)
        {
            return input =>
            {
                var result = parser(input);
                if (result.Result.IsError)
                    return result.CastError<TT>();
                return result.ChainRight(parser2(result.Remain));
            };
        }

        public static Parse<I>.Parser<T> Left<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> parser2)
        {
            return input =>
            {
                var result = parser(input);
                if (result.Result.IsOk)
                {
                    return result.ChainLeft(parser2(result.Remain));
                }
                return result;
            };
        }

        public static Parse<I>.Parser<TT> Bind<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, Parse<I>.Parser<TT>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.Result.IsError)
                    return result.CastError<TT>();
                return result.ChainRight(func(result.Result.RawValue)(result.Remain));
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
                if (result.Result.IsError)
                    return result.CastError<TT>();
                return result.SetResult(func(result.Result.RawValue));
            };
        }

        public static Parse<I>.Parser<TT> Map<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => Result.Ok<TT, ParseErrorInfo>(func(t)));
        }

        public static Parse<I>.Parser<T> ErrorRetry<T, I>(this Parse<I>.Parser<T> parser, Func<IParseResult<T, I>, T> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.Result.IsError)
                    return ParseResult.NewOk(func(result), result.Remain);
                return result;
            };
        }

        public static Parse<I>.Parser<TT> Select<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, TT> selector)
        {
            return Map(parser, selector);
        }

        public static Parse<I>.Parser<T> Try<T, I>(this Parse<I>.Parser<T> parser)
        {
            return
                input =>
                {
                    var parseResult = parser(input);

                    if (parseResult.Result.IsError)
                        return parseResult.SetRemain(input);

                    return parseResult;
                };
        }

        public static Parse<I>.Parser<T> NoConsume<T, I>(this Parse<I>.Parser<T> parser)
        {
            return input => parser(input).SetRemain(input);
        }

        public static Parse<I>.Parser<T> Default<T, I>(this Parse<I>.Parser<T> parser, T defaultValue)
        {
            return input =>
            {
                var parseResult = parser(input);
                if (parseResult.Result.IsError)
                {
                    return ParseResult.NewOk(defaultValue, parseResult.Remain);
                }
                return parseResult;
            };
        }

        public static Parse<I>.Parser<List<T>> Concat<T, I>(this IEnumerable<Parse<I>.Parser<T>> parsers)
        {
            return input =>
            {
                var list = new List<T>();
                var parseResult = ParseResult.NewOk(default(T), input);

                foreach (var parser in parsers)
                {
                    parseResult = parseResult.ChainRight(parser(parseResult.Remain));
                    if (parseResult.Result.IsError)
                    {
                        return parseResult.CastError<List<T>>();
                    }
                    list.Add(parseResult.Result.Unwrap());
                }
                return parseResult.Ok(list);
            };
        }

        public static Parse<I>.Parser<T> Where<T, I>(this Parse<I>.Parser<T> parser, Func<T, bool> predicate)
        {
            return parser.BindResult(
                t => predicate(t) ?
                    Result.Ok<T, ParseErrorInfo>(t) :
                    Result.Error<T, ParseErrorInfo>(new ParseErrorInfo("Where"))
            );
        }

        public static Parse<I>.Parser<string> Text<I>(this Parse<I>.Parser<IEnumerable<char>> parser)
        {
            return parser.Map(string.Concat);
        }

        public static Parse<I>.Parser<T> Debug<T, I>(this Parse<I>.Parser<T> parser)
        {
            return input =>
            {
                var result = parser(input);
                return result;
            };
        }
    }
}
