using System.Globalization;
using System.Linq;

namespace MarineLang.Utils
{
    public static class NameUtil
    {
        public static string GetUpperCamelName(in string name)
        {
            return
                string.Join("",
                    name
                        .Split('_')
                        .Select(CultureInfo.CurrentCulture.TextInfo.ToTitleCase)
                );
        }

        public static string GetLowerCamelName(in string name)
        {
            var splites = name.Split('_');
            if (splites.Length == 1)
                return name;
            return splites[0] +
                   string.Join("", splites.Skip(1).Select(CultureInfo.CurrentCulture.TextInfo.ToTitleCase));
        }

        public static string GetSnakeCase(in string str)
        {
            return string.Join("", str.Select(c => char.IsUpper(c) ? "_" + char.ToLower(c) : c.ToString()))
                .Substring(1);
        }
    }
}