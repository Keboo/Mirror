namespace AssemblyToTest
{
    internal class MethodReturnValues
    {
        private string Get42()
        {
            MethodInvocation.RegisterCall<MethodReturnValues>();
            return "42";
        }

        private static int Get43()
        {
            MethodInvocation.RegisterCall<MethodReturnValues>();
            return 43;
        }

        public static MethodReturnValues CreateInstance()
        {
            MethodInvocation.RegisterCall<MethodReturnValues>();
            return new MethodReturnValues();
        }

        public override int GetHashCode()
        {
            MethodInvocation.RegisterCall<MethodReturnValues>();
            return 42;
        }
    }
}