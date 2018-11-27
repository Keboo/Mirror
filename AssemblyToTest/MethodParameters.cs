namespace AssemblyToTest
{
    internal class MethodParameters
    {
        public MethodParameters()
        { }

        private MethodParameters(int intValue)
        {
            MethodInvocation.RegisterCall<MethodParameters>(new object[] { intValue });
        }

        private MethodParameters(string stringValue)
        {
            MethodInvocation.RegisterCall<MethodParameters>(new object[] { stringValue });
        }

        private void DoSomething(uint uintValue)
        {
            MethodInvocation.RegisterCall<MethodParameters>(new object[] { uintValue });
        }

        private void DoSomething(string optionalString)
        {
            MethodInvocation.RegisterCall<MethodParameters>(new object[] { optionalString });
        }

        private ReturnValue DoSomething(Parameter parameter)
        {
            MethodInvocation.RegisterCall<MethodParameters>(new object[] { parameter });
            return new ReturnValue();
        }
    }

    internal class Parameter
    { }

    internal class ReturnValue
    { }
}