using System;

namespace AssemblyToTest
{
    public static class PublicStaticClass
    {
        private static void PrivateMethod()
        {
            MethodInvocation.RegisterCall(typeof(PublicStaticClass));
        }
    }
}
