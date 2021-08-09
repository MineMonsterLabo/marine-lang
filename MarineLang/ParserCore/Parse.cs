using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;

namespace MarineLang.ParserCore
{
    public static class Parse<I>
    {
        public delegate IParseResult<T, I> Parser<out T>(IInput<I> input);

        public static Parser<List<T>> Many<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var list = new List<T>();
                    while (input.IsEnd == false)
                    {
                        var parseResult = parser(input);
                        input = parseResult.Remain;

                        if (parseResult.Result.IsError)
                            break;
                        list.Add(parseResult.Result.Unwrap());
                    }
                    return ParseResult.Ok(list, input);
                };
        }

        public static Parser<List<T>> ManyUntilEnd<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var list = new List<T>();
                    while (input.IsEnd == false)
                    {
                        var parseResult = parser(input);
                        input = parseResult.Remain;
                        if (parseResult.TryGetError(out var parseErrorInfo))
                            return parseResult.Error<List<T>>(parseErrorInfo);
                        list.Add(parseResult.Result.Unwrap());
                    }
                    return ParseResult.Ok(list, input);
                };
        }

        public static Parser<List<T>> OneMany<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var result = Many(parser)(input);
                    if (result.Result.IsError == false && result.Result.RawValue.Count == 0)
                        return result.Error<List<T>>(new ParseErrorInfo());
                    return result;
                };
        }

        public static Parser<object[]> Parsers(params Parser<object>[] parsers)
        {
            var values = new object[parsers.Length];

            return
                input =>
                {
                    for (var i = 0; i < parsers.Length; i++)
                    {
                        var result = parsers[i](input);
                        input = result.Remain;
                        if (result.Result.IsError)
                            return result.Error<object[]>(result.Result.RawError);
                        if (input.IsEnd && i + 1 != parsers.Length)
                            return result.Error<object[]>(new ParseErrorInfo());
                        values[i] = result.Result.RawValue;
                    }
                    return ParseResult.Ok(values, input);
                };
        }

        public static Parser<(T1, T2)> Tuple<T1, T2>(Parser<T1> parser1, Parser<T2> parser2)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1]));
        }

        public static Parser<(T1, T2, T3)> Tuple<T1, T2, T3>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2]));
        }

        public static Parser<(T1, T2, T3, T4)> Tuple<T1, T2, T3, T4>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3]));
        }

        public static Parser<(T1, T2, T3, T4, T5)> Tuple<T1, T2, T3, T4, T5>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>, parser5 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4]));
        }

        public static Parser<(T1, T2, T3, T4, T5, T6)> Tuple<T1, T2, T3, T4, T5, T6>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5, Parser<T6> parser6)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>, parser5 as Parser<object>, parser6 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4], (T6)objects[5]));
        }

        public static Parser<T> Or<T>(params Parser<T>[] parsers)
        {
            return
                input =>
                {
                    IParseResult<T, I> lastParseResult = null;
                    foreach (var parser in parsers)
                    {
                        lastParseResult = parser(input);
                        input = lastParseResult.Remain;
                        if (lastParseResult.Result.IsError == false)
                            return lastParseResult;
                    }
                    return lastParseResult;
                };
        }

        //入力を消費するエラーが発生したら、即座に終了するOr
        public static Parser<T> OrConsumedError<T>(params Parser<T>[] parsers)
        {
            return
                input =>
                {
                    IParseResult<T, I> lastParseResult = null;
                    foreach (var parser in parsers)
                    {
                        lastParseResult = parser(input);
                        if (lastParseResult.Remain.Index != input.Index && lastParseResult.Result.IsError)
                            return lastParseResult;
                        input = lastParseResult.Remain;
                        if (lastParseResult.Result.IsOk)
                            return lastParseResult;
                    }
                    return lastParseResult;
                };
        }

        public static Parser<T[]> Separated<T, TT>(Parser<T> parser, Parser<TT> separateParser)
        {
            return input =>
            {
                var isFirst = true;
                var list = new List<T>();

                while (input.IsEnd == false)
                {
                    var separateResult = separateParser(input);
                    input = separateResult.Remain;
                    if (isFirst == false && separateResult.Result.IsError)
                        break;
                    var result = parser(input);
                    input = result.Remain;
                    if (result.Result.IsError && isFirst == false)
                        return result.Error<T[]>(new ParseErrorInfo());
                    isFirst = false;
                    if (result.Result.IsError)
                        break;
                    list.Add(result.Result.Unwrap());
                }
                return ParseResult.Ok(list.ToArray(), input);
            };
        }

        public static Parser<I> TestOnce(Func<I, bool> test)
        {
            return input =>
            {
                if (input.IsEnd)
                {
                    return ParseResult.Error<I, I>(new ParseErrorInfo(), input);
                }

                var token = input.Current;
                if (test(token))
                {
                    return ParseResult.Ok(token, input.Advance());
                }
                return ParseResult.Error<I, I>(new ParseErrorInfo(), input);
            };
        }

        public static readonly Parser<Unit> End =
            input =>
                input.IsEnd ?
                    UnitReturn(input) :
                    ParseResult.Error<Unit, I>(new ParseErrorInfo(), input);

        public static Parser<Unit> Except<T>(Parser<T> except)
        {
            return input =>
            {
                var result = except(input);
                if (result.Result.IsOk)
                    return result.Error<Unit>(new ParseErrorInfo());
                return result.Ok(Unit.Value);
            };
        }

        public static Parser<T> Return<T>(T t)
        {
            return input => ParseResult.Ok(t, input);
        }

        public static readonly Parser<Unit> UnitReturn = Return(Unit.Value);
    }
}