using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MarineLang.VirtualMachines.Dumps.Members
{
    public abstract class MemberDumper
    {
        private MemberInfo _memberInfo;

        public DumpMemberKind MemberKind { get; }

        public string Name { get; }

        internal MemberDumper(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo;
        }

        protected MemberDumper(JObject member)
        {
            MemberKind = member.Value<string>("kind").ToDumpKind();
            Name = member.Value<string>("name");
        }

        internal abstract JObject ToJObject();
    }
}