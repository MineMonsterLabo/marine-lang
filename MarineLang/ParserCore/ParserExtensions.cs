using MarineLang.Models;
using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.ParserCore
{
    public static class ParserExtensions
    {
        public static Parse<I>.Parser<T> NamedError<T, I>(this Parse<I>.Parser<T> parser, Func<RangePosition, ParseErrorInfo> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsError)
                    return result.MapErrorStack<T>(func(result.Remain.RangePosition));
                return result;
            };
        }

        public static Parse<I>.Parser<T> NamedError<T, I>
            (this Parse<I>.Parser<T> parser, ErrorCode errorCode, RangePosition rangePosition, string prefixErrorMessage = "")
        {
            return parser.NamedError(_ =>
               new ParseErrorInfo(prefixErrorMessage, rangePosition, errorCode)
           );
        }

        public static Parse<I>.Parser<T> NamedError<T, I>
            (this Parse<I>.Parser<T> parser, ErrorCode errorCode, string prefixErrorMessage = "")
        {
            return parser.NamedError(rangePosition =>
                new ParseErrorInfo(prefixErrorMessage, rangePosition, errorCode)
            );
        }

        public static Parse<I>.Parser<TT> Right<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> parser2)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsError)
                    return result.CastError<TT>();
                return result.ChainRight(parser2(result.Remain));
            };
        }

        public static Parse<I>.Parser<T> Left<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> parser2)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsOk)
                {
                    return result.ChainLeft(parser2(result.Remain));
                }
                return result;
            };
        }

        public static Parse<I>.Parser<TT> Bind<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, IInput<I>, Parse<I>.Parser<TT>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsError)
                    return result.CastError<TT>();
                return result.ChainRight(func(result.Result.RawValue, result.Remain)(result.Remain));
            };
        }

        public static Parse<I>.Parser<TT> Bind<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, Parse<I>.Parser<TT>> func)
        {
            return Bind(parser, (value, _) => func(value));
        }

        public static Parse<I>.Parser<V> SelectMany<T, U, V, I>(
                 this Parse<I>.Parser<T> parser,
                 Func<T, Parse<I>.Parser<U>> selector,
                 Func<T, U, V> projector)
        {
            return parser.Bind((t, _) => selector(t).Select(u => projector(t, u)));
        }

        public static Parse<I>.Parser<TT> BindResult<T, TT, I>
            (this Parse<I>.Parser<T> parser, Func<T, IInput<I>, IResult<TT, IEnumerable<ParseErrorInfo>>> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsError)
                    return result.CastError<TT>();

                return result.Bind(v => ParseResult.FromResult(func(v, result.Remain), result.Remain));
            };
        }

        public static Parse<I>.Parser<TT> Map<T, TT, I>(this Parse<I>.Parser<T> parser, Func<T, TT> func)
        {
            return input => parser(input).Map(func);
        }

        public static Parse<I>.Parser<T> ErrorRetry<T, I>(this Parse<I>.Parser<T> parser, Func<ParseResult<T, I>, T> func)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsError)
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

                    if (parseResult.IsError)
                        return parseResult.SetRemain(input);

                    return parseResult;
                };
        }

        public static Parse<I>.Parser<T> Try<T, I>(this Parse<I>.Parser<T> parser, IInput<I> remain)
        {
            return
                input =>
                {
                    var parseResult = parser(input);

                    if (parseResult.IsError)
                        return parseResult.SetRemain(remain);

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
                if (parseResult.IsError)
                {
                    return ParseResult.NewOk(defaultValue, parseResult.Remain);
                }
                return parseResult;
            };
        }

        public static Parse<I>.Parser<IEnumerable<T>> Concat<T, I>(this IEnumerable<Parse<I>.Parser<T>> parsers)
        {
            return input =>
            {
                var parseResult = ParseResult.NewOk(Enumerable.Empty<T>(), input);

                foreach (var parser in parsers)
                {
                    parseResult = parseResult.Append(parser(parseResult.Remain));
                    if (parseResult.IsError)
                    {
                        return parseResult;
                    }
                }
                return parseResult;
            };
        }

        public static Parse<I>.Parser<T> Where<T, I>(this Parse<I>.Parser<T> parser, Func<T, bool> predicate)
        {
            return parser.WhereQuiet(predicate).NamedError(pos => new ParseErrorInfo("Where", pos));
        }

        public static Parse<I>.Parser<T> WhereQuiet<T, I>(this Parse<I>.Parser<T> parser, Func<T, bool> predicate)
        {
            return parser.BindResult(
                (t, _) => predicate(t) ?
                    Result.Ok<T, IEnumerable<ParseErrorInfo>>(t) :
                    Result.Error<T, IEnumerable<ParseErrorInfo>>(Enumerable.Empty<ParseErrorInfo>())
            );
        }

        public static Parse<I>.Parser<string> Text<I>(this Parse<I>.Parser<IEnumerable<char>> parser)
        {
            return parser.Map(string.Concat);
        }

        public static Parse<I>.Parser<T> StackError<T, I>(this Parse<I>.Parser<T> parser, T value)
        {
            return input =>
            {
                var result = parser(input);
                return result.IsError ? result.Ok(value) : result;
            };
        }

        public static Parse<I>.Parser<T> SwallowIfError<T, TT, I>(this Parse<I>.Parser<T> parser, Parse<I>.Parser<TT> swallowParser)
        {
            return input =>
            {
                var result = parser(input);
                return result.IsError ? result.SetRemain(swallowParser(result.Remain).Remain) : result;
            };
        }

        public static Parse<I>.Parser<TT> UpCast<I, T, TT>(this Parse<I>.Parser<T> parser)
            where T : TT
        {
            return parser.Map(t => (TT)t);
        }

        public static Parse<I>.Parser<T> DebugPrint<T, I>(this Parse<I>.Parser<T> parser,string tag)
        {
            return input =>
            {
                var result = parser(input);
                var nextToken = result.Remain.IsEnd ? "eof" : result.Remain.Current.ToString();
                result.ToResult().DoOk(v => System.Diagnostics.Debug.Print($"[{tag}] value: [{v}], nextToken: [{nextToken}]"));
                return result;
            };
        }
    }
}