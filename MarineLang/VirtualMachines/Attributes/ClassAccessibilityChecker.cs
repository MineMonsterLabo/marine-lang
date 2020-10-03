using System.Reflection;

namespace MarineLang.VirtualMachines.Attributes
{
    internal static class ClassAccessibilityChecker
    {
        public static bool CheckMember(MemberInfo memberInfo)
        {
            DefaultMemberAllPublicAttribute defaultMemberAllPublic =
                memberInfo.DeclaringType?.GetCustomAttribute<DefaultMemberAllPublicAttribute>();
            DefaultMemberAllPrivateAttribute defaultMemberAllPrivate =
                memberInfo.DeclaringType?.GetCustomAttribute<DefaultMemberAllPrivateAttribute>();

            if (defaultMemberAllPublic != null)
            {
                MemberPrivateAttribute privateAttribute = memberInfo.GetCustomAttribute<MemberPrivateAttribute>();
                return privateAttribute == null;
            }

            if (defaultMemberAllPrivate != null)
            {
                MemberPublicAttribute publicAttribute = memberInfo.GetCustomAttribute<MemberPublicAttribute>();
                return publicAttribute != null;
            }

            return false;
        }
    }
}