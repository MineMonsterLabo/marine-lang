using MarineLang.Models.Errors;
using MarineLang.Streams;
using MineUtil;

namespace MarineLang.ParserCore
{
    public delegate IResult<T, ParseErrorInfo> Parser<out T>(TokenStream stream);
}
