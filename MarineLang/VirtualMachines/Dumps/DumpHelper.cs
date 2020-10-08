using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps
{
    public static class DumpHelper
    {
        public static IReadOnlyDictionary<string, ClassDumper> FromJson(string json)
        {
            JObject jObject = JObject.Parse(json);
            Dictionary<string, ClassDumper> classDumpers = new Dictionary<string, ClassDumper>();
            foreach (KeyValuePair<string, JToken> pair in jObject)
            {
                classDumpers.Add(pair.Key, new ClassDumper(pair.Value as JObject));
            }

            return classDumpers;
        }
    }
}