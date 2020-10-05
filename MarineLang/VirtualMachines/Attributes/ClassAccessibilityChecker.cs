using System.Reflection;

namespace MarineLang.VirtualMachines.Attributes
{
    internal static class ClassAccessibilityChecker
    {
        public static bool CheckMember(MemberInfo memberInfo)
        {
            DefaultMemberAllPrivateAttribute defaultMemberAllPrivate =
                memberInfo.DeclaringType?.GetCustomAttribute<DefaultMemberAllPrivateAttribute>();

            if (defaultMemberAllPrivate != null)
            {
                MemberPublicAttribute publicAttribute = memberInfo.GetCustomAttribute<MemberPublicAttribute>();
                return publicAttribute != null;
            }

            MemberPrivateAttribute privateAttribute = memberInfo.GetCustomAttribute<MemberPrivateAttribute>();
            return privateAttribute == null;
        }
    }
}