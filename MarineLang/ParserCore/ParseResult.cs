using MarineLang.Models.Errors;
using MineUtil;

namespace MarineLang.ParserCore
{
    public class ParseResult
    {
        public static IResult<T, ParseErrorInfo> Ok<T>(T value)
        {
            return Result.Ok<T, ParseErrorInfo>(value);
        }

        public static IResult<T, ParseErrorInfo> Error<T>(ParseErrorInfo error)
        {
            return Result.Error<T, ParseErrorInfo>(error);
        }
    }
}
