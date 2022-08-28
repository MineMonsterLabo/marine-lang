namespace MarineLang.Utils
{
    public static class CharUtil
    {
        static public char? ToEspaceChar(string str)
        {
            switch (str)
            {
                case "\\\\":
                    return '\\';
                case "\\r":
                    return '\r';
                case "\\n":
                    return '\n';
                case "\\t":
                    return '\t';
                case "\\'":
                    return '\'';
                case "\\\"":
                    return '\"';
            }
            return null;
        }

        static public bool IsLowerLetter(char c)
        {
            return char.IsLetter(c) && char.IsLower(c);
        }

        static public bool IsUpperLetter(char c)
        {
            return char.IsLetter(c) && char.IsUpper(c);
        }

        static public bool IsIdHeadChar(char c)
        {
            return char.IsLetter(c);
        }

        static public bool IsIdTailChar(char c)
        {
            return char.IsDigit(c) || char.IsLetter(c) || c == '_';
        }
    }
}