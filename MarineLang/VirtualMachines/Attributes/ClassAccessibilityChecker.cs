using System.Reflection;

namespace MarineLang.VirtualMachines.Attributes
{
    internal static class ClassAccessibilityChecker
    {
        public static bool CheckMember(this MemberInfo memberInfo)
        {
            DefaultMemberAllPublicAttribute defaultMemberAllPublic =
                memberInfo.DeclaringType?.GetCustomAttribute<DefaultMemberAllPublicAttribute>();
            DefaultMemberAllPrivateAttribute defaultMemberAllPrivate =
                memberInfo.DeclaringType?.GetCustomAttribute<DefaultMemberAllPrivateAttribute>();
            MemberPublicAttribute publicAttribute = memberInfo.GetCustomAttribute<MemberPublicAttribute>();
            MemberPrivateAttribute privateAttribute = memberInfo.GetCustomAttribute<MemberPrivateAttribute>();

            if (defaultMemberAllPublic != null)
            {
                return privateAttribute == null;
            }

            if (defaultMemberAllPrivate != null)
            {
                return publicAttribute != null;
            }

            return false;
        }
    }
}