using System;

namespace AssemblyToTest
{
    public static class PublicStaticClass
    {
        private static void PrivateMethod()
        {
            MethodInvocation.RegisterCall(typeof(PublicStaticClass));
        }

        internal static void InternalMethod()
        {
            MethodInvocation.RegisterCall(typeof(PublicStaticClass));
        }

        public static void PublicMethod()
        {
            MethodInvocation.RegisterCall(typeof(PublicStaticClass));
        }
    }
}
