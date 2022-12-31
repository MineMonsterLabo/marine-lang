using MarineLang.Models;
using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.ParserCore
{
    public static class Parse<I>
    {
        public delegate ParseResult<T, I> Parser<T>(IInput<I> input);

        public static Parser<ACC> FoldIf<T, ACC>
            (IEnumerable<Parser<T>> parsers, ACC seed, Func<ParseResult<ACC, I>, Parser<T>, int, (ParseResult<ACC, I>, bool)> func)
        {
            return
                input =>
                {
                    var index = 0;

                    (ParseResult<ACC, I>, bool) Func(ParseResult<ACC, I> parseResult, Parser<T> parser)
                    {
                        var (acc, continueFlag) = func(parseResult, parser, index);
                        index++;
                        return (acc, continueFlag && acc.IsOk);
                    };

                    return parsers.AggregateIf(ParseResult.NewOk(seed, input), Func);
                };
        }

        public static Parser<ACC> FoldIf<T, ACC>
           (IEnumerable<Parser<T>> parsers, ACC seed, Func<ParseResult<ACC, I>, Parser<T>, (ParseResult<ACC, I>, bool)> func)
           => FoldIf(parsers, seed, (acc, x, _) => func(acc, x));

        public static Parser<ACC> FoldIfCheckEnd<T, ACC>
           (IEnumerable<Parser<T>> parsers, ACC seed, Func<ParseResult<ACC, I>, Parser<T>, int, (ParseResult<ACC, I>, bool)> func)
        {
            (ParseResult<ACC, I>, bool) Func(ParseResult<ACC, I> parseResult, Parser<T> parser, int index)
            {
                return parseResult.Remain.IsEnd ? (parseResult, false) : func(parseResult, parser, index);
            };

            return FoldIf(parsers, seed, Func);
        }

        public static Parser<ACC> FoldIfCheckEnd<T, ACC>
           (IEnumerable<Parser<T>> parsers, ACC seed, Func<ParseResult<ACC, I>, Parser<T>, (ParseResult<ACC, I>, bool)> func)
            => FoldIfCheckEnd(parsers, seed, (acc, x, _) => func(acc, x));

        public static Parser<IEnumerable<T>> Many<T>(Parser<T> parser)
        {
            (ParseResult<IEnumerable<T>, I>, bool) Func(ParseResult<IEnumerable<T>, I> parseResult, Parser<T> _)
            {
                var parseResult2 = parser(parseResult.Remain);

                if (parseResult2.IsError)
                {
                    return (parseResult.SetRemain(parseResult2.Remain), false);
                }
                return (parseResult.Append(parseResult2), true);
            };

            return FoldIfCheckEnd(parser.Infinity(), Enumerable.Empty<T>(), Func);
        }

        public static Parser<IEnumerable<T>> ManyUntilEnd<T>(Parser<T> parser)
        {
            return FoldIfCheckEnd(parser.Infinity(), Enumerable.Empty<T>(),
                (parseResult, _) => (parseResult.Append(parser(parseResult.Remain)), true));
        }

        public static Parser<IEnumerable<T>> ManyUntilEndStackError<T>(Parser<T> parser)
        {
            return FoldIfCheckEnd(parser.Infinity(), Enumerable.Empty<T>(),
                (parseResult, parser2) =>
                {
                    var parseResult2 = parseResult.Append(parser2(parseResult.Remain));

                    if (parseResult2.IsError && parseResult.Remain.Index == parseResult2.Remain.Index)
                    {
                        return (parseResult2.Ok(parseResult.Result.RawValue), false);
                    }

                    if (parseResult2.IsError)
                    {
                        return (parseResult2.Ok(parseResult.Result.RawValue), true);
                    }

                    return (parseResult2, true);
                });
        }

        public static Parser<IEnumerable<T>> OneMany<T>(Parser<T> parser)
        {
            var manyParser = Many(parser);

            return
                input =>
                {
                    var result = manyParser(input);
                    if (result.IsError == false && !result.Result.RawValue.Any())
                        return result.Error<IEnumerable<T>>(new ParseErrorInfo("OneMany", input.RangePosition));
                    return result;
                };
        }

        public static Parser<object[]> ParsersArray(params Parser<object>[] parsers)
        {
            return Parsers(parsers.AsEnumerable()).Map(objects => objects.ToArray());
        }

        public static Parser<IEnumerable<object>> Parsers(IEnumerable<Parser<object>> parsers)
        {
            return Positioned.Bind(pos =>
            {
                (ParseResult<IEnumerable<object>, I>, bool) Func(ParseResult<IEnumerable<object>, I> parseResult, Parser<object> parser)
                {
                    if (parseResult.Remain.IsEnd)
                        return (parseResult.Error(new ParseErrorInfo("Parsers", pos)), false);
                    return (parseResult.Append(parser(parseResult.Remain)), true);
                };

                return FoldIf(parsers, Enumerable.Empty<object>(), Func);
            });
        }

        public static Parser<(T1, T2)> Tuple<T1, T2>(Parser<T1> parser1, Parser<T2> parser2)
        {
            return ParsersArray(parser1.UpCast<I, T1, object>(), parser2.UpCast<I, T2, object>())
                .Map(objects => ((T1)objects[0], (T2)objects[1]));
        }

        public static Parser<(T1, T2, T3)> Tuple<T1, T2, T3>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
        {
            return ParsersArray(parser1.UpCast<I, T1, object>(), parser2.UpCast<I, T2, object>(), parser3.UpCast<I, T3, object>())
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2]));
        }

        public static Parser<(T1, T2, T3, T4)> Tuple<T1, T2, T3, T4>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4)
        {
            return
                ParsersArray(
                    parser1.UpCast<I, T1, object>(),
                    parser2.UpCast<I, T2, object>(),
                    parser3.UpCast<I, T3, object>(),
                    parser4.UpCast<I, T4, object>()
                )
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3]));
        }

        public static Parser<(T1, T2, T3, T4, T5)> Tuple<T1, T2, T3, T4, T5>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5)
        {
            return
                ParsersArray(
                    parser1.UpCast<I, T1, object>(),
                    parser2.UpCast<I, T2, object>(),
                    parser3.UpCast<I, T3, object>(),
                    parser4.UpCast<I, T4, object>(),
                    parser5.UpCast<I, T5, object>()
                )
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4]));
        }

        public static Parser<(T1, T2, T3, T4, T5, T6)> Tuple<T1, T2, T3, T4, T5, T6>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5, Parser<T6> parser6)
        {
            return
                ParsersArray(
                    parser1.UpCast<I, T1, object>(),
                    parser2.UpCast<I, T2, object>(),
                    parser3.UpCast<I, T3, object>(),
                    parser4.UpCast<I, T4, object>(),
                    parser5.UpCast<I, T5, object>(),
                    parser6.UpCast<I, T6, object>()
                )
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4], (T6)objects[5]));
        }

        public static Parser<T> Or<T>(params Parser<T>[] parsers)
        {
            (ParseResult<IOption<T>, I>, bool) Func(ParseResult<IOption<T>, I> parseResult, Parser<T> parser)
            {
                var parseResult2 = parser(parseResult.Remain);
                return parseResult2.IsError ? (parseResult2.Ok(Option.None<T>()), true) : (parseResult2.Map(Option.Some), false);
            };

            return FoldIf(parsers, Option.None<T>(), Func).WhereQuiet(option => option.IsSome).Map(option => option.RawValue);
        }

        //入力を消費するエラーが発生したら、即座に終了するOr
        public static Parser<T> OrConsumedError<T>(params Parser<T>[] parsers)
        {
            (ParseResult<IOption<T>, I>, bool) Func(ParseResult<IOption<T>, I> parseResult, Parser<T> parser)
            {
                var parseResult2 = parser(parseResult.Remain);
                if (parseResult2.Remain.Index != parseResult.Remain.Index && parseResult2.IsError)
                    return (parseResult2.Ok(Option.None<T>()), false);
                return parseResult2.IsError ? (parseResult2.Ok(Option.None<T>()), true) : (parseResult2.Map(Option.Some), false);
            };

            return FoldIf(parsers, Option.None<T>(), Func).WhereQuiet(option => option.IsSome).Map(option => option.RawValue);
        }

        public static Parser<IEnumerable<T>> Separated<T, TT>(Parser<T> parser, Parser<TT> separateParser)
        {
            (ParseResult<IEnumerable<T>, I>, bool) Func(ParseResult<IEnumerable<T>, I> parseResult, Parser<T> _, int index)
            {
                var parseResult2 = parseResult.ChainLeft(separateParser(parseResult.Remain));

                if (index != 0 && parseResult2.IsError)
                {
                    return (parseResult.SetRemain(parseResult2.Remain), false);
                }

                parseResult2 = parseResult.Append(parser(parseResult2.Remain));

                if (parseResult2.IsError && index != 0)
                    return (parseResult2, false);

                if (parseResult2.IsError)
                {
                    return (parseResult.SetRemain(parseResult2.Remain), false);
                }

                return (parseResult2, true);
            };

            return FoldIfCheckEnd(parser.Infinity(), Enumerable.Empty<T>(), Func);
        }

        public static Parser<I> Verify(Func<I, bool> test)
        {
            return input =>
            {
                if (input.IsEnd)
                {
                    return ParseResult.NewError<I, I>(new ParseErrorInfo("Input id End ", input.RangePosition), input);
                }

                var token = input.Current;
                if (test(token))
                {
                    return ParseResult.NewOk(token, input.Advance());
                }
                return ParseResult.NewError<I, I>(new ParseErrorInfo("Verify actual " + token, input.RangePosition), input);
            };
        }

        public static Parser<I> Expected<T>(Func<I, T> selector, T expect)
        {
            return input =>
            {
                if (input.IsEnd)
                {
                    return ParseResult.NewError<I, I>(new ParseErrorInfo("Input id End ", input.RangePosition), input);
                }

                var actual = selector(input.Current);

                if (expect.Equals(actual))
                {
                    return ParseResult.NewOk(input.Current, input.Advance());
                }
                return ParseResult.NewError<I, I>(
                    new ParseErrorInfo
                        ($"actual: {actual} expected: {expect}",
                        input.RangePosition
                    ),
                    input);
            };
        }

        public static Parser<IEnumerable<T>> Until<T, TT>(Parser<T> parser, Parser<TT> until)
        {
            (ParseResult<IEnumerable<T>, I>, bool) Func(ParseResult<IEnumerable<T>, I> parseResult, Parser<T> _)
            {
                var untilParseResult = parseResult.ChainLeft(until(parseResult.Remain));
                if (untilParseResult.IsOk)
                {
                    return (untilParseResult, false);
                }

                parseResult = parseResult
                    .SetRemain(untilParseResult.Remain)
                    .Append(parser(parseResult.Remain));

                return (parseResult, parseResult.IsOk);
            };

            return FoldIfCheckEnd(parser.Infinity(), Enumerable.Empty<T>(), Func);
        }

        public static Parser<IEnumerable<T>> UntilStackError<T, TT>(Parser<T> parser, Parser<TT> until)
        {
            (ParseResult<IEnumerable<T>, I>, bool) Func(ParseResult<IEnumerable<T>, I> parseResult, Parser<T> _)
            {
                var untilParseResult = parseResult.ChainLeft(until(parseResult.Remain));
                if (untilParseResult.IsOk)
                {
                    return (untilParseResult, false);
                }

                parseResult = parseResult.SetRemain(untilParseResult.Remain);
                var parseResult2 = parser(parseResult.Remain);

                if (parseResult2.IsError)
                {
                    var isContinue = parseResult.Remain.Index != parseResult2.Remain.Index;
                    return (parseResult.ChainRight(parseResult2).Ok(parseResult.Result.RawValue), isContinue);
                }

                return (parseResult.Append(parseResult2), true);
            };

            return FoldIfCheckEnd(parser.Infinity(), Enumerable.Empty<T>(), Func);
        }

        public static readonly Parser<Unit> End =
            input =>
                input.IsEnd ?
                    UnitReturn(input) :
                    ParseResult.NewError<Unit, I>(new ParseErrorInfo("End", input.RangePosition), input);

        public static Parser<Unit> Except<T>(Parser<T> except)
        {
            return input =>
            {
                var result = except(input);
                if (result.IsOk)
                    return result.Error<Unit>(new ParseErrorInfo("Except", input.RangePosition));
                return ParseResult.NewOk(Unit.Value, result.Remain);
            };
        }

        public static Parser<T> Return<T>(T t)
        {
            return input => ParseResult.NewOk(t, input);
        }

        public static Parser<IOption<T>> Optional<T>(Parser<T> parser)
        {
            return input =>
            {
                var result = parser(input);
                if (result.IsOk)
                    return ParseResult.NewOk(Option.Some(result.Result.RawValue), result.Remain);
                return ParseResult.NewOk(Option.None<T>(), input);
            };
        }

        public static Parser<T> ErrorReturn<T>(ParseErrorInfo parseErrorInfo)
        {
            return input => ParseResult.NewError<T, I>(parseErrorInfo, input);
        }

        public static readonly Parser<I> Any =
            input =>
                input.IsEnd ?
                    ParseResult.NewError<I, I>(new ParseErrorInfo("Any", input.RangePosition), input) :
                    ParseResult.NewOk(input.Current, input.Advance());

        public static Parse<char>.Parser<char> Char(char c)
        {
            return Parse<char>.Verify(inputChar => inputChar == c);
        }

        public static Parse<char>.Parser<string> String(string s)
        {
            return s.Select(Char).Concat().Map(string.Concat).Try();
        }

        public static readonly Parser<Unit> UnitReturn = Return(Unit.Value);

        public static readonly Parser<RangePosition> Positioned = input => ParseResult.NewOk(input.RangePosition, input);

        public static readonly Parser<I> Current = input => ParseResult.NewOk(input.Current, input.Advance());
        public static readonly Parser<I> LastCurrent = input => ParseResult.NewOk(input.LastCurrent, input.Advance());
        public static readonly Parser<IInput<I>> Remain = input => ParseResult.NewOk(input, input);
    }
}