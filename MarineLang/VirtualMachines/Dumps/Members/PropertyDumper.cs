using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps.Members
{
    public class PropertyDumper : MemberDumper
    {
        private PropertyInfo _propertyInfo;

        public bool CanRead { get; }
        public bool CanWrite { get; }

        public bool IsIndexer { get; }

        public TypeDumper Type { get; }

        public IReadOnlyCollection<ParameterDumper> IndexerParameters { get; }

        internal PropertyDumper(PropertyInfo propertyInfo) : base(propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public PropertyDumper(JObject member) : base(member)
        {
            CanRead = member.Value<bool>("can_read");
            CanWrite = member.Value<bool>("can_write");

            IsIndexer = member.Value<bool>("is_indexer");

            Type = new TypeDumper(member["type"] as JObject);

            if (IsIndexer)
                IndexerParameters = (member["parameters"] as JArray).Select(e => new ParameterDumper(e as JObject))
                    .ToArray();
        }

        internal override JObject ToJObject()
        {
            JObject property = new JObject();
            property["kind"] = "property";
            property["name"] = _propertyInfo.Name;
            property["can_read"] = _propertyInfo.CanRead;
            property["can_write"] = _propertyInfo.CanWrite;

            bool isIndexer = _propertyInfo.GetIndexParameters().Length > 0;
            property["is_indexer"] = isIndexer;

            property["type"] = new TypeDumper(_propertyInfo.PropertyType).ToJObject();

            if (isIndexer)
            {
                JArray indexerParameters = new JArray();
                foreach (ParameterInfo parameter in _propertyInfo.GetIndexParameters())
                {
                    indexerParameters.Add(new ParameterDumper(parameter).ToJObject());
                }

                property["index_parameters"] = indexerParameters;
            }

            return property;
        }
    }
}